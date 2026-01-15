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
    public class PurchaseServices
    {
        private readonly AppDbContext _context;

        public PurchaseServices(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PurchaseInvoice> CreateInvoiceAsync(PurchaseInvoice invoice)
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

                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Only automate journal posting if Status is POSTED
                if (invoice.Status == "POSTED")
                {
                    await PostToJournalInternalAsync(invoice);
                    await _context.SaveChangesAsync();
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

        public async Task<PurchaseInvoice> UpdateInvoiceAsync(PurchaseInvoice invoice)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.PurchaseInvoices
                    .Include(i => i.Lines)
                    .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == invoice.PurchaseInvoiceId);

                if (existing == null) throw new Exception("Invoice not found");
                if (existing.Status == "POSTED") throw new Exception("Posted invoices cannot be modified.");

                // Update Header
                existing.InvoiceNumber = invoice.InvoiceNumber;
                existing.VendorId = invoice.VendorId;
                existing.InvoiceDate = DateTime.SpecifyKind(invoice.InvoiceDate, DateTimeKind.Utc);
                existing.DueDate = DateTime.SpecifyKind(invoice.DueDate, DateTimeKind.Utc);
                existing.TotalAmount = invoice.TotalAmount;
                existing.NetAmount = invoice.NetAmount;
                existing.Balance = invoice.Balance;
                existing.CurrencyId = invoice.CurrencyId;
                existing.ExchangeRate = invoice.ExchangeRate;
                existing.Status = invoice.Status;
                existing.Notes = invoice.Notes;
                existing.UpdatedAt = DateTime.UtcNow;

                // Remove old lines
                _context.PurchaseInvoiceLines.RemoveRange(existing.Lines);

                // Add new lines
                foreach (var line in invoice.Lines)
                {
                    existing.Lines.Add(new PurchaseInvoiceLine
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
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == invoiceId);

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

        public async Task<List<PurchaseInvoice>> GetAllInvoicesAsync()
        {
            return await _context.PurchaseInvoices
                .Include(i => i.Lines)
                .Include(i => i.Vendor)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<PurchaseInvoice> GetInvoiceByIdAsync(int id)
        {
            return await _context.PurchaseInvoices
                .Include(i => i.Lines)
                    .ThenInclude(l => l.Account)
                .Include(i => i.Vendor)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == id);
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.PurchaseInvoices.FindAsync(id);
            if (invoice == null) return false;

            if (invoice.Status == "POSTED") throw new Exception("Cannot delete a posted invoice.");

            _context.PurchaseInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task PostToJournalInternalAsync(PurchaseInvoice invoice)
        {
            // Identify AP Account
            var apAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountSubtype == "CURRENT_LIABILITY" && a.AccountName == "Accounts Payable");

            if (apAccount == null)
            {
                apAccount = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "2100");
            }

            if (apAccount == null)
            {
                throw new Exception("Core account 'Accounts Payable' (2100) not found in Chart of Accounts. Please run database setup.");
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
                Description = $"Purchase Invoice: {invoice.InvoiceNumber}",
                Reference = invoice.InvoiceNumber,
                JournalType = "PURCHASES",
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

            // Debit Expense/Asset Accounts from Lines
            foreach (var line in invoice.Lines)
            {
                journalEntry.JournalLines.Add(new JournalLine
                {
                    AccountId = line.AccountId,
                    Description = line.Description,
                    DebitAmount = line.Amount,
                    CreditAmount = 0,
                    ForeignDebitAmount = line.Amount / (invoice.ExchangeRate > 0 ? invoice.ExchangeRate : 1),
                    ForeignCreditAmount = 0,
                    LineDate = invoice.InvoiceDate,
                    ContactId = invoice.VendorId,
                    ContactType = "Vendor",
                    CurrencyId = invoice.CurrencyId,
                    ExchangeRate = invoice.ExchangeRate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Credit Accounts Payable
            journalEntry.JournalLines.Add(new JournalLine
            {
                AccountId = apAccount.AccountId,
                Description = $"Payable for Invoice {invoice.InvoiceNumber}",
                DebitAmount = 0,
                CreditAmount = invoice.TotalAmount,
                ForeignDebitAmount = 0,
                ForeignCreditAmount = invoice.TotalAmount / (invoice.ExchangeRate > 0 ? invoice.ExchangeRate : 1),
                LineDate = invoice.InvoiceDate,
                ContactId = invoice.VendorId,
                ContactType = "Vendor",
                CurrencyId = invoice.CurrencyId,
                ExchangeRate = invoice.ExchangeRate,
                CreatedAt = DateTime.UtcNow
            });

            _context.JournalEntries.Add(journalEntry);
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
