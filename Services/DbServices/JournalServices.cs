using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services.DbServices
{
    public class JournalServices
    {
        private readonly AppDbContext _context;

        public JournalServices(AppDbContext context)
        {
            _context = context;
        }

        #region Journal Entries

        public async Task<JournalEntry> CreateJournalEntryAsync(JournalEntry journalEntry)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                journalEntry.CreatedAt = DateTime.UtcNow;
                journalEntry.UpdatedAt = DateTime.UtcNow;
                
                // Ensure all DateTime properties are UTC
                if (journalEntry.JournalDate.Kind == DateTimeKind.Local)
                    journalEntry.JournalDate = journalEntry.JournalDate.ToUniversalTime();
                
                if (journalEntry.PostedAt.HasValue && journalEntry.PostedAt.Value.Kind == DateTimeKind.Local)
                    journalEntry.PostedAt = journalEntry.PostedAt.Value.ToUniversalTime();

                // Generate journal number if not provided
                if (string.IsNullOrEmpty(journalEntry.JournalNumber))
                {
                    journalEntry.JournalNumber = await GenerateJournalNumberAsync();
                }

                // Calculate total amount from lines
                if (journalEntry.JournalLines?.Any() == true)
                {
                    journalEntry.Amount = journalEntry.JournalLines.Sum(l => l.DebitAmount + l.CreditAmount);
                    
                    // Set timestamps for journal lines and ensure UTC
                    foreach (var line in journalEntry.JournalLines)
                    {
                        line.CreatedAt = DateTime.UtcNow;
                        
                        // Ensure LineDate is UTC
                        if (line.LineDate.Kind == DateTimeKind.Local)
                            line.LineDate = line.LineDate.ToUniversalTime();
                    }
                }

                _context.JournalEntries.Add(journalEntry);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return journalEntry;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<JournalEntry>> GetAllJournalEntriesAsync()
        {
            return await _context.JournalEntries
                .Include(j => j.JournalLines)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<JournalEntry> GetJournalEntryByIdAsync(int journalId)
        {
            return await _context.JournalEntries
                .Include(j => j.JournalLines)
                .FirstOrDefaultAsync(j => j.JournalId == journalId);
        }

        public async Task<JournalEntry> UpdateJournalEntryAsync(JournalEntry updatedJournalEntry)
        {
            var journalEntry = await _context.JournalEntries
                .Include(j => j.JournalLines)
                .FirstOrDefaultAsync(j => j.JournalId == updatedJournalEntry.JournalId);

            if (journalEntry == null) return null;

            // Update journal entry properties
            journalEntry.JournalNumber = updatedJournalEntry.JournalNumber;
            journalEntry.JournalDate = updatedJournalEntry.JournalDate;
            journalEntry.PeriodId = updatedJournalEntry.PeriodId;
            journalEntry.Reference = updatedJournalEntry.Reference;
            journalEntry.Description = updatedJournalEntry.Description;
            journalEntry.JournalType = updatedJournalEntry.JournalType;
            journalEntry.Status = updatedJournalEntry.Status;
            journalEntry.PostedBy = updatedJournalEntry.PostedBy;
            journalEntry.PostedAt = updatedJournalEntry.PostedAt;
            journalEntry.UpdatedAt = DateTime.UtcNow;
            
            // Ensure all DateTime properties are UTC
            if (journalEntry.JournalDate.Kind == DateTimeKind.Local)
                journalEntry.JournalDate = journalEntry.JournalDate.ToUniversalTime();
            
            if (journalEntry.PostedAt.HasValue && journalEntry.PostedAt.Value.Kind == DateTimeKind.Local)
                journalEntry.PostedAt = journalEntry.PostedAt.Value.ToUniversalTime();

            // Update journal lines
            if (updatedJournalEntry.JournalLines?.Any() == true)
            {
                // Remove existing lines
                _context.JournalLines.RemoveRange(journalEntry.JournalLines);

                // Add updated lines
                foreach (var line in updatedJournalEntry.JournalLines)
                {
                    line.JournalId = journalEntry.JournalId;
                    line.CreatedAt = DateTime.UtcNow;
                    
                    // Ensure LineDate is UTC
                    if (line.LineDate.Kind == DateTimeKind.Local)
                        line.LineDate = line.LineDate.ToUniversalTime();
                    
                    _context.JournalLines.Add(line);
                }

                // Recalculate total amount
                journalEntry.Amount = updatedJournalEntry.JournalLines.Sum(l => l.DebitAmount + l.CreditAmount);
            }

            await _context.SaveChangesAsync();
            return journalEntry;
        }

        public async Task<bool> DeleteJournalEntryAsync(int journalId)
        {
            var journalEntry = await _context.JournalEntries.FindAsync(journalId);
            if (journalEntry == null) return false;

            // Only allow deletion of DRAFT entries
            if (journalEntry.Status != "DRAFT")
            {
                throw new InvalidOperationException("Only DRAFT journal entries can be deleted.");
            }

            _context.JournalEntries.Remove(journalEntry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PostJournalEntryAsync(int journalId, int userId)
        {
            var journalEntry = await _context.JournalEntries
                .Include(j => j.JournalLines)
                .FirstOrDefaultAsync(j => j.JournalId == journalId);

            if (journalEntry == null) return false;

            // Validate journal entry before posting
            if (!await ValidateJournalEntryAsync(journalEntry))
            {
                throw new InvalidOperationException("Journal entry is not balanced or has validation errors.");
            }

            journalEntry.Status = "POSTED";
            journalEntry.PostedBy = userId;
            journalEntry.PostedAt = DateTime.UtcNow;
            journalEntry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VoidJournalEntryAsync(int journalId, int userId)
        {
            var journalEntry = await _context.JournalEntries.FindAsync(journalId);
            if (journalEntry == null) return false;

            journalEntry.Status = "VOID";
            journalEntry.PostedBy = userId;
            journalEntry.PostedAt = DateTime.UtcNow;
            journalEntry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsJournalNumberUniqueAsync(string journalNumber)
        {
            return !await _context.JournalEntries.AnyAsync(j => j.JournalNumber == journalNumber);
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

        private async Task<bool> ValidateJournalEntryAsync(JournalEntry journalEntry)
        {
            if (journalEntry.JournalLines?.Any() != true)
            {
                return false;
            }

            // Check if debits equal credits
            var totalDebits = journalEntry.JournalLines.Sum(l => l.DebitAmount);
            var totalCredits = journalEntry.JournalLines.Sum(l => l.CreditAmount);

            if (totalDebits != totalCredits)
            {
                return false;
            }

            // Check if all lines have valid accounts
            var accountIds = journalEntry.JournalLines.Select(l => l.AccountId).Distinct();
            var validAccounts = await _context.ChartOfAccounts
                .Where(a => accountIds.Contains(a.AccountId) && a.IsActive)
                .CountAsync();

            return validAccounts == accountIds.Count();
        }

        #endregion Journal Entries

        #region Chart of Accounts

        public async Task<List<ChartOfAccount>> GetAllAccountsAsync()
        {
            return await _context.ChartOfAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<List<ChartOfAccount>> GetAccountsByTypeAsync(string accountType)
        {
            return await _context.ChartOfAccounts
                .Where(a => a.IsActive && a.AccountType == accountType)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<ChartOfAccount> GetAccountByIdAsync(int accountId)
        {
            return await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<List<ChartOfAccount>> SearchAccountsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAccountsAsync();
            }

            return await _context.ChartOfAccounts
                .Where(a => a.IsActive && 
                           (a.AccountNumber.Contains(searchTerm) || 
                            a.AccountName.Contains(searchTerm)))
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        #endregion Chart of Accounts

        #region Journal Templates

        public async Task<JournalTemplate> CreateTemplateAsync(JournalTemplate template)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedAt = DateTime.UtcNow;

                _context.JournalTemplates.Add(template);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return template;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<JournalTemplate>> GetAllTemplatesAsync()
        {
            return await _context.JournalTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<JournalTemplate>> GetTemplatesByTypeAsync(string journalType)
        {
            return await _context.JournalTemplates
                .Where(t => t.IsActive && t.JournalType == journalType)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<JournalTemplate> GetTemplateByIdAsync(int templateId)
        {
            return await _context.JournalTemplates
                .FirstOrDefaultAsync(t => t.TemplateId == templateId);
        }

        public async Task<JournalTemplate> UpdateTemplateAsync(JournalTemplate updatedTemplate)
        {
            var template = await _context.JournalTemplates.FindAsync(updatedTemplate.TemplateId);
            if (template == null) return null;

            template.Name = updatedTemplate.Name;
            template.Description = updatedTemplate.Description;
            template.JournalType = updatedTemplate.JournalType;
            template.TemplateData = updatedTemplate.TemplateData;
            template.IsActive = updatedTemplate.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            var template = await _context.JournalTemplates.FindAsync(templateId);
            if (template == null) return false;

            _context.JournalTemplates.Remove(template);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion Journal Templates

        #region Reporting and Analysis

        public async Task<decimal> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null)
        {
            var query = _context.JournalLines
                .Where(l => l.AccountId == accountId);

            if (asOfDate.HasValue)
            {
                query = query.Where(l => l.LineDate <= asOfDate.Value);
            }

            var debitTotal = await query.SumAsync(l => l.DebitAmount);
            var creditTotal = await query.SumAsync(l => l.CreditAmount);

            return debitTotal - creditTotal;
        }

        public async Task<List<JournalLine>> GetAccountTransactionsAsync(int accountId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.JournalLines
                .Where(l => l.AccountId == accountId);

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.LineDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(l => l.LineDate <= toDate.Value);
            }

            return await query
                .Include(l => l.JournalEntry)
                .OrderBy(l => l.LineDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetTrialBalanceAsync(DateTime? asOfDate = null)
        {
            var query = _context.JournalLines.AsQueryable();

            if (asOfDate.HasValue)
            {
                query = query.Where(l => l.LineDate <= asOfDate.Value);
            }

            var trialBalance = await query
                .GroupBy(l => new { l.AccountId, l.JournalEntry.Description })
                .Select(g => new
                {
                    AccountId = g.Key.AccountId,
                    AccountName = g.Key.Description,
                    DebitTotal = g.Sum(l => l.DebitAmount),
                    CreditTotal = g.Sum(l => l.CreditAmount)
                })
                .ToListAsync();

            var result = new Dictionary<string, decimal>();
            foreach (var item in trialBalance)
            {
                var balance = item.DebitTotal - item.CreditTotal;
                result[$"{item.AccountId} - {item.AccountName}"] = balance;
            }

            return result;
        }

        #endregion Reporting and Analysis
    }
}
