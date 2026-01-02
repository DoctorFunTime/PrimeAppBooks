using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services.DbServices
{
    public class SalesServices
    {
        private readonly AppDbContext _context;

        public SalesServices(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SalesInvoice> CreateInvoiceAsync(SalesInvoice invoice)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.CreatedAt = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;

                if (invoice.InvoiceDate.Kind != DateTimeKind.Utc)
                    invoice.InvoiceDate = DateTime.SpecifyKind(invoice.InvoiceDate, DateTimeKind.Utc);
                if (invoice.DueDate.Kind != DateTimeKind.Utc)
                    invoice.DueDate = DateTime.SpecifyKind(invoice.DueDate, DateTimeKind.Utc);

                _context.SalesInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Only automate journal posting if Status is POSTED
                if (invoice.Status == "POSTED")
                {
                    await PostToJournalInternalAsync(invoice);
                    await _context.SaveChangesAsync(); // Save journal entries
                }

                await transaction.CommitAsync();
                return invoice;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SalesInvoice> UpdateInvoiceAsync(SalesInvoice invoice)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.SalesInvoices
                    .Include(i => i.Lines)
                    .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoice.SalesInvoiceId);

                if (existing == null)
                    throw new Exception("Invoice not found");

                if (existing.Status == "POSTED")
                    throw new Exception("Posted invoices cannot be edited");

                // Update invoice properties
                existing.InvoiceNumber = invoice.InvoiceNumber;
                existing.CustomerId = invoice.CustomerId;
                existing.InvoiceDate = DateTime.SpecifyKind(invoice.InvoiceDate, DateTimeKind.Utc);
                existing.DueDate = DateTime.SpecifyKind(invoice.DueDate, DateTimeKind.Utc);
                existing.TotalAmount = invoice.TotalAmount;
                existing.NetAmount = invoice.NetAmount;
                existing.Balance = invoice.Balance;
                existing.CurrencyId = invoice.CurrencyId;
                existing.ExchangeRate = invoice.ExchangeRate;
                existing.Status = invoice.Status;
                existing.Terms = invoice.Terms;
                existing.Notes = invoice.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                // Remove old lines
                _context.SalesInvoiceLines.RemoveRange(existing.Lines);

                // Add new lines
                foreach (var line in invoice.Lines)
                {
                    existing.Lines.Add(new SalesInvoiceLine
                    {
                        Description = line.Description,
                        AccountId = line.AccountId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        Amount = line.Amount
                    });
                }

                await _context.SaveChangesAsync();

                // Post to journal if status changed to POSTED
                if (invoice.Status == "POSTED")
                {
                    await PostToJournalInternalAsync(existing);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return existing;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> PostInvoiceAsync(int invoiceId)
        {
            var invoice = await _context.SalesInvoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId);

            if (invoice == null || invoice.Status == "POSTED") return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                invoice.Status = "POSTED";
                invoice.UpdatedAt = DateTime.UtcNow;

                await PostToJournalInternalAsync(invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task PostToJournalInternalAsync(SalesInvoice invoice)
        {
            // Automated Journal Posting
            var arAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountSubtype == "CURRENT_ASSET" && a.AccountName == "Accounts Receivable");

            if (arAccount == null)
            {
                arAccount = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "1100");
            }

            if (arAccount == null)
            {
                throw new Exception("Core account 'Accounts Receivable' (1100) not found in Chart of Accounts. Please run database setup.");
            }

            // Get current accounting period
            var currentPeriod = await _context.AccountingPeriods
                .FirstOrDefaultAsync(p => p.StartDate <= invoice.InvoiceDate && p.EndDate >= invoice.InvoiceDate);

            // Generate Journal Number
            var journalNumber = await GenerateJournalNumberAsync();

            var journalEntry = new JournalEntry
            {
                JournalNumber = journalNumber,
                JournalDate = invoice.InvoiceDate,
                Description = $"Sales Invoice: {invoice.InvoiceNumber}",
                Reference = invoice.InvoiceNumber,
                JournalType = "SALES",
                Status = "POSTED",
                Amount = invoice.TotalAmount,
                CurrencyId = invoice.CurrencyId,
                ExchangeRate = invoice.ExchangeRate,
                PeriodId = currentPeriod?.PeriodId,
                CreatedBy = invoice.CreatedBy,
                PostedBy = invoice.CreatedBy,
                PostedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                JournalLines = new List<JournalLine>()
            };

            // Debit Accounts Receivable
            journalEntry.JournalLines.Add(new JournalLine
            {
                AccountId = arAccount.AccountId,
                Description = $"Receivable for Invoice {invoice.InvoiceNumber}",
                DebitAmount = invoice.TotalAmount,
                CreditAmount = 0,
                ForeignDebitAmount = invoice.TotalAmount / (invoice.ExchangeRate > 0 ? invoice.ExchangeRate : 1),
                ForeignCreditAmount = 0,
                LineDate = invoice.InvoiceDate,
                ContactId = invoice.CustomerId,
                ContactType = "Customer",
                CurrencyId = invoice.CurrencyId,
                ExchangeRate = invoice.ExchangeRate,
                CreatedAt = DateTime.UtcNow
            });

            // Credit Revenue Accounts from Lines
            foreach (var line in invoice.Lines)
            {
                journalEntry.JournalLines.Add(new JournalLine
                {
                    AccountId = line.AccountId,
                    Description = line.Description,
                    DebitAmount = 0,
                    CreditAmount = line.Amount,
                    ForeignDebitAmount = 0,
                    ForeignCreditAmount = line.Amount / (invoice.ExchangeRate > 0 ? invoice.ExchangeRate : 1),
                    LineDate = invoice.InvoiceDate,
                    ContactId = invoice.CustomerId,
                    ContactType = "Customer",
                    CurrencyId = invoice.CurrencyId,
                    ExchangeRate = invoice.ExchangeRate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.JournalEntries.Add(journalEntry);
        }

        public async Task<List<SalesInvoice>> GetAllInvoicesAsync()
        {
            return await _context.SalesInvoices
                .Include(i => i.Lines)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<SalesInvoice> GetInvoiceByIdAsync(int id)
        {
            return await _context.SalesInvoices
                .Include(i => i.Lines)
                    .ThenInclude(l => l.Account)
                .FirstOrDefaultAsync(i => i.SalesInvoiceId == id);
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.SalesInvoices.FindAsync(id);
            if (invoice == null) return false;

            _context.SalesInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> GenerateJournalNumberAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"JE{year}";

            var lastNumber = await _context.JournalEntries
                .Where(j => j.JournalNumber.StartsWith(prefix))
                .OrderByDescending(j => j.JournalNumber)
                .Select(j => j.JournalNumber)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastNumber))
            {
                return $"{prefix}0001";
            }

            var numberPart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int number))
            {
                return $"{prefix}{(number + 1):D4}";
            }

            return $"{prefix}0001";
        }
    }
}