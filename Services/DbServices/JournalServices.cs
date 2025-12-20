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
        private readonly SettingsService _settingsService;

        public JournalServices(AppDbContext context, SettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
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
                if (journalEntry.JournalDate.Kind != DateTimeKind.Utc)
                    journalEntry.JournalDate = DateTime.SpecifyKind(journalEntry.JournalDate, DateTimeKind.Utc);

                if (journalEntry.PostedAt.HasValue && journalEntry.PostedAt.Value.Kind != DateTimeKind.Utc)
                    journalEntry.PostedAt = DateTime.SpecifyKind(journalEntry.PostedAt.Value, DateTimeKind.Utc);

                // Generate journal number if not provided
                if (string.IsNullOrEmpty(journalEntry.JournalNumber))
                {
                    journalEntry.JournalNumber = await GenerateJournalNumberAsync();
                }

                // Generate reference number if not provided
                if (string.IsNullOrEmpty(journalEntry.Reference))
                {
                    journalEntry.Reference = await GenerateReferenceNumberAsync();
                }

                // Set timestamps for journal lines and ensure UTC
                foreach (var line in journalEntry.JournalLines)
                {
                    line.CreatedAt = DateTime.UtcNow;

                    // Ensure LineDate is UTC
                    if (line.LineDate.Kind != DateTimeKind.Utc)
                        line.LineDate = DateTime.SpecifyKind(line.LineDate, DateTimeKind.Utc);

                    // Handle Currency Conversion - ensure lines have parent's currency/rate if missing
                    var effectiveCurrencyId = line.CurrencyId ?? journalEntry.CurrencyId;
                    var effectiveExchangeRate = line.ExchangeRate > 0 ? line.ExchangeRate : (journalEntry.ExchangeRate > 0 ? journalEntry.ExchangeRate : 1.0m);

                    if (effectiveCurrencyId.HasValue && effectiveExchangeRate > 0)
                    {
                        // Always prioritize calculating base from foreign if foreign exists
                        if (line.ForeignDebitAmount > 0)
                            line.DebitAmount = Math.Round(line.ForeignDebitAmount * effectiveExchangeRate, 2);
                        else if (line.ForeignCreditAmount > 0)
                            line.CreditAmount = Math.Round(line.ForeignCreditAmount * effectiveExchangeRate, 2);
                    }
                }

                // Recalculate total amount from lines after conversion
                journalEntry.Amount = journalEntry.JournalLines.Sum(l => l.DebitAmount);

                _context.JournalEntries.Add(journalEntry);

                // Update account balances if creating directly as POSTED
                if (journalEntry.Status == "POSTED")
                {
                    await UpdateAccountBalancesAsync(journalEntry, true);
                }

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
                .ThenInclude(l => l.ChartOfAccount)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        public async Task<JournalEntry> GetJournalEntryByIdAsync(int journalId)
        {
            return await _context.JournalEntries
                .Include(j => j.JournalLines)
                .ThenInclude(l => l.ChartOfAccount)
                .FirstOrDefaultAsync(j => j.JournalId == journalId);
        }

        public async Task<JournalEntry> UpdateJournalEntryAsync(JournalEntry updatedJournalEntry)
        {
            var journalEntry = await _context.JournalEntries
                .Include(j => j.JournalLines)
                .FirstOrDefaultAsync(j => j.JournalId == updatedJournalEntry.JournalId);

            if (journalEntry == null) return null;

            // 1. Subtract OLD posted balances if it was POSTED
            var oldStatus = _context.Entry(journalEntry).Property(j => j.Status).OriginalValue?.ToString();
            if (oldStatus == "POSTED")
            {
                await UpdateAccountBalancesAsync(journalEntry, false);
            }

            // 2. Perform updates
            journalEntry.JournalNumber = updatedJournalEntry.JournalNumber;
            journalEntry.JournalDate = updatedJournalEntry.JournalDate;
            journalEntry.PeriodId = updatedJournalEntry.PeriodId;
            journalEntry.Description = updatedJournalEntry.Description;
            journalEntry.JournalType = updatedJournalEntry.JournalType;
            journalEntry.Status = updatedJournalEntry.Status;
            journalEntry.PostedBy = updatedJournalEntry.PostedBy;
            journalEntry.PostedAt = updatedJournalEntry.PostedAt;
            journalEntry.CurrencyId = updatedJournalEntry.CurrencyId;
            journalEntry.ExchangeRate = updatedJournalEntry.ExchangeRate;
            journalEntry.UpdatedAt = DateTime.UtcNow;

            if (updatedJournalEntry.JournalLines?.Any() == true)
            {
                _context.JournalLines.RemoveRange(journalEntry.JournalLines);
                foreach (var line in updatedJournalEntry.JournalLines)
                {
                    var effectiveCurrencyId = line.CurrencyId ?? journalEntry.CurrencyId;
                    var effectiveExchangeRate = line.ExchangeRate > 0 ? line.ExchangeRate : (journalEntry.ExchangeRate > 0 ? journalEntry.ExchangeRate : 1.0m);
                    if (effectiveCurrencyId.HasValue && effectiveExchangeRate > 0)
                    {
                        if (line.ForeignDebitAmount > 0)
                            line.DebitAmount = Math.Round(line.ForeignDebitAmount * effectiveExchangeRate, 2);
                        else if (line.ForeignCreditAmount > 0)
                            line.CreditAmount = Math.Round(line.ForeignCreditAmount * effectiveExchangeRate, 2);
                    }
                    _context.JournalLines.Add(line);
                }
                journalEntry.Amount = updatedJournalEntry.JournalLines.Sum(l => l.DebitAmount);
            }

            // 3. Add NEW posted balances if it is now POSTED
            if (journalEntry.Status == "POSTED")
            {
                await UpdateAccountBalancesAsync(journalEntry, true);
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

            if (journalEntry == null || journalEntry.Status == "POSTED") return false;

            // Validate journal entry before posting
            if (!await ValidateJournalEntryAsync(journalEntry))
            {
                throw new InvalidOperationException("Journal entry is not balanced or has validation errors.");
            }

            // Update account balances
            await UpdateAccountBalancesAsync(journalEntry, true);

            journalEntry.Status = "POSTED";
            journalEntry.PostedBy = userId;
            journalEntry.PostedAt = DateTime.UtcNow;
            journalEntry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VoidJournalEntryAsync(int journalId, int userId)
        {
            var journalEntry = await _context.JournalEntries
                .Include(j => j.JournalLines)
                .FirstOrDefaultAsync(j => j.JournalId == journalId);

            if (journalEntry == null || journalEntry.Status == "VOID") return false;

            // If it was posted, we need to reverse the balances
            if (journalEntry.Status == "POSTED")
            {
                await UpdateAccountBalancesAsync(journalEntry, false);
            }

            journalEntry.Status = "VOID";
            journalEntry.PostedBy = userId;
            journalEntry.PostedAt = DateTime.UtcNow;
            journalEntry.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task UpdateAccountBalancesAsync(JournalEntry journalEntry, bool isAdding)
        {
            foreach (var line in journalEntry.JournalLines)
            {
                var account = await _context.ChartOfAccounts.FindAsync(line.AccountId);
                if (account != null)
                {
                    var modifier = isAdding ? 1 : -1;
                    account.CurrentBalance += (line.DebitAmount - line.CreditAmount) * modifier;
                    account.UpdatedAt = DateTime.UtcNow;
                }
            }
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

        private async Task<string> GenerateReferenceNumberAsync()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var prefix = $"REF{year}{month:D2}";

            var lastReference = await _context.JournalEntries
                .Where(j => j.Reference != null && j.Reference.StartsWith(prefix))
                .OrderByDescending(j => j.Reference)
                .Select(j => j.Reference)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastReference))
            {
                return $"{prefix}0001";
            }

            var numberPart = lastReference.Substring(prefix.Length);
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
                .Where(a => a.IsActive && (a.AccountNumber.Contains(searchTerm) || a.AccountName.Contains(searchTerm)))
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
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId && l.JournalEntry.Status == "POSTED");

            if (asOfDate.HasValue)
            {
                var utcDate = asOfDate.Value.Kind == DateTimeKind.Utc ? asOfDate.Value : asOfDate.Value.ToUniversalTime();
                query = query.Where(l => l.LineDate <= utcDate);
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
                .Include(l => l.Currency)
                .Where(l => l.JournalEntry.Status == "POSTED")
                .OrderBy(l => l.LineDate)
                .ToListAsync();
        }

        public async Task<List<JournalLine>> GetJournalLinesAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.JournalLines.AsQueryable();

            if (fromDate.HasValue)
            {
                var utcFrom = fromDate.Value.Kind == DateTimeKind.Utc ? fromDate.Value : fromDate.Value.ToUniversalTime();
                query = query.Where(l => l.LineDate >= utcFrom);
            }

            if (toDate.HasValue)
            {
                var utcTo = toDate.Value.Kind == DateTimeKind.Utc ? toDate.Value : toDate.Value.ToUniversalTime();
                query = query.Where(l => l.LineDate <= utcTo);
            }

            return await query
                .Include(l => l.JournalEntry)
                .Include(l => l.ChartOfAccount)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetTrialBalanceAsync(DateTime? asOfDate = null)
        {
            var query = _context.JournalLines
                .Include(l => l.JournalEntry)
                .Include(l => l.ChartOfAccount)
                .Where(l => l.JournalEntry.Status == "POSTED");

            if (asOfDate.HasValue)
            {
                var utcDate = asOfDate.Value.Kind == DateTimeKind.Utc ? asOfDate.Value : asOfDate.Value.ToUniversalTime();
                query = query.Where(l => l.LineDate <= utcDate);
            }

            var trialBalance = await query
                .GroupBy(l => new { l.AccountId, l.ChartOfAccount.AccountNumber, l.ChartOfAccount.AccountName })
                .Select(g => new
                {
                    AccountKey = $"{g.Key.AccountNumber} - {g.Key.AccountName}",
                    DebitTotal = g.Sum(l => l.DebitAmount),
                    CreditTotal = g.Sum(l => l.CreditAmount)
                })
                .ToListAsync();

            var result = new Dictionary<string, decimal>();
            foreach (var item in trialBalance)
            {
                result[item.AccountKey] = item.DebitTotal - item.CreditTotal;
            }

            return result;
        }

        #endregion Reporting and Analysis
    }
}