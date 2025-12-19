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

                // Automated Journal Posting
                var arAccount = await _context.ChartOfAccounts
                    .FirstOrDefaultAsync(a => a.AccountSubtype == "CURRENT_ASSET" && a.AccountName == "Accounts Receivable");

                if (arAccount == null)
                {
                    // If for some reason 1100 isn't there, try by number
                    arAccount = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "1100");
                }

                if (arAccount == null)
                {
                    throw new Exception("Core account 'Accounts Receivable' (1100) not found in Chart of Accounts. Please run database setup.");
                }

                var journalEntry = new JournalEntry
                {
                    JournalDate = invoice.InvoiceDate,
                    Description = $"Sales Invoice: {invoice.InvoiceNumber}",
                    Reference = invoice.InvoiceNumber,
                    JournalType = "SALES",
                    Status = "POSTED",
                    Amount = invoice.TotalAmount,
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
                    LineDate = invoice.InvoiceDate,
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
                        LineDate = invoice.InvoiceDate,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _context.JournalEntries.Add(journalEntry);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                return invoice;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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
    }
}
