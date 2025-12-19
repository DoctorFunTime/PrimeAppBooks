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

        private async Task PostToJournalInternalAsync(PurchaseInvoice invoice)
        {
            // Automated Journal Posting
            var apAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountSubtype == "CURRENT_LIABILITY" && a.AccountName == "Accounts Payable");

            if (apAccount == null)
            {
                apAccount = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "2000");
            }

            if (apAccount == null)
            {
                throw new Exception("Core account 'Accounts Payable' (2000) not found in Chart of Accounts. Please run database setup.");
            }

            // Generate Journal Number
            var journalNumber = await GenerateJournalNumberAsync();

            var journalEntry = new JournalEntry
            {
                JournalNumber = journalNumber,
                JournalDate = invoice.InvoiceDate,
                Description = $"Purchase Invoice: {invoice.InvoiceNumber}",
                Reference = invoice.InvoiceNumber,
                JournalType = "PURCHASE",
                Status = "POSTED",
                Amount = invoice.TotalAmount,
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
                    LineDate = invoice.InvoiceDate,
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
                LineDate = invoice.InvoiceDate,
                CreatedAt = DateTime.UtcNow
            });

            _context.JournalEntries.Add(journalEntry);
        }

        public async Task<List<PurchaseInvoice>> GetAllInvoicesAsync()
        {
            return await _context.PurchaseInvoices
                .Include(i => i.Lines)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<PurchaseInvoice> GetInvoiceByIdAsync(int id)
        {
            return await _context.PurchaseInvoices
                .Include(i => i.Lines)
                    .ThenInclude(l => l.Account)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == id);
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.PurchaseInvoices.FindAsync(id);
            if (invoice == null) return false;

            _context.PurchaseInvoices.Remove(invoice);
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
