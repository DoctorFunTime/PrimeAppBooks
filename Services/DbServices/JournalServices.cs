using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
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

        public async Task<JournalEntry> CreateBadDebtWriteOffJournalAsync(
            int customerId,
            decimal amount,
            string notes,
            int arAccountId,
            int badDebtsAccountId,
            string customerReference,
            DateTime writeOffDate,
            int userId = 1,
            string referencePrefix = "WO-")
        {
            var journal = new JournalEntry
            {
                JournalDate = writeOffDate.Kind == DateTimeKind.Utc ? writeOffDate : writeOffDate.ToUniversalTime(),
                Description = notes,
                Reference = $"{referencePrefix}{customerReference}",
                JournalType = "GENERAL",
                Status = "POSTED",
                PostedAt = DateTime.UtcNow,
                PostedBy = userId,
                Amount = amount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Debit Bad Debts Expense
            journal.JournalLines.Add(new JournalLine
            {
                AccountId = badDebtsAccountId,
                DebitAmount = amount,
                CreditAmount = 0,
                Description = notes,
                ContactId = customerId,
                ContactType = "Customer",
                LineDate = journal.JournalDate,
                CreatedAt = DateTime.UtcNow
            });

            // Credit Accounts Receivable
            journal.JournalLines.Add(new JournalLine
            {
                AccountId = arAccountId,
                DebitAmount = 0,
                CreditAmount = amount,
                Description = notes,
                ContactId = customerId,
                ContactType = "Customer",
                LineDate = journal.JournalDate,
                CreatedAt = DateTime.UtcNow
            });

            return await CreateJournalEntryAsync(journal);
        }

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

        public async Task<int> CreateJournalEntriesAsync(IEnumerable<JournalEntry> journalEntries)
        {
            var entries = journalEntries?.Where(j => j != null).ToList() ?? new List<JournalEntry>();
            if (!entries.Any()) return 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var nextJournalNumber = await GetNextJournalNumberAsync();
                var accountDeltas = new Dictionary<int, decimal>();

                foreach (var journalEntry in entries)
                {
                    journalEntry.CreatedAt = now;
                    journalEntry.UpdatedAt = now;

                    if (journalEntry.JournalDate.Kind != DateTimeKind.Utc)
                        journalEntry.JournalDate = DateTime.SpecifyKind(journalEntry.JournalDate, DateTimeKind.Utc);

                    if (journalEntry.PostedAt.HasValue && journalEntry.PostedAt.Value.Kind != DateTimeKind.Utc)
                        journalEntry.PostedAt = DateTime.SpecifyKind(journalEntry.PostedAt.Value, DateTimeKind.Utc);

                    if (string.IsNullOrEmpty(journalEntry.JournalNumber))
                    {
                        journalEntry.JournalNumber = FormatJournalNumber(nextJournalNumber++);
                    }

                    if (string.IsNullOrEmpty(journalEntry.Reference))
                    {
                        journalEntry.Reference = await GenerateReferenceNumberAsync();
                    }

                    foreach (var line in journalEntry.JournalLines)
                    {
                        line.CreatedAt = now;

                        if (line.LineDate.Kind != DateTimeKind.Utc)
                            line.LineDate = DateTime.SpecifyKind(line.LineDate, DateTimeKind.Utc);

                        var effectiveCurrencyId = line.CurrencyId ?? journalEntry.CurrencyId;
                        var effectiveExchangeRate = line.ExchangeRate > 0 ? line.ExchangeRate : (journalEntry.ExchangeRate > 0 ? journalEntry.ExchangeRate : 1.0m);

                        if (effectiveCurrencyId.HasValue && effectiveExchangeRate > 0)
                        {
                            if (line.ForeignDebitAmount > 0)
                                line.DebitAmount = Math.Round(line.ForeignDebitAmount * effectiveExchangeRate, 2);
                            else if (line.ForeignCreditAmount > 0)
                                line.CreditAmount = Math.Round(line.ForeignCreditAmount * effectiveExchangeRate, 2);
                        }

                        if (journalEntry.Status == "POSTED")
                        {
                            accountDeltas[line.AccountId] = accountDeltas.GetValueOrDefault(line.AccountId) + line.DebitAmount - line.CreditAmount;
                        }
                    }

                    journalEntry.Amount = journalEntry.JournalLines.Sum(l => l.DebitAmount);
                }

                if (accountDeltas.Any())
                {
                    var accountIds = accountDeltas.Keys.ToList();
                    var accounts = await _context.ChartOfAccounts
                        .Where(a => accountIds.Contains(a.AccountId))
                        .ToListAsync();

                    foreach (var account in accounts)
                    {
                        account.CurrentBalance += accountDeltas[account.AccountId];
                        account.UpdatedAt = now;
                    }
                }

                _context.JournalEntries.AddRange(entries);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return entries.Count;
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
            var journalEntry = await _context.JournalEntries
                .Include(j => j.JournalLines)
                .FirstOrDefaultAsync(j => j.JournalId == journalId);

            if (journalEntry == null) return false;

            // If it was posted, we need to reverse the balances before deleting
            if (journalEntry.Status == "POSTED")
            {
                await UpdateAccountBalancesAsync(journalEntry, false);
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
            var maxRetries = 100; // Prevent infinite loops
            var currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                var lastNumber = await _context.JournalEntries
                    .Where(j => j.JournalNumber != null && j.JournalNumber.StartsWith(prefix))
                    .OrderByDescending(j => j.JournalNumber)
                    .Select(j => j.JournalNumber)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (!string.IsNullOrEmpty(lastNumber))
                {
                    var numberPart = lastNumber.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int number))
                    {
                        nextNumber = number + 1;
                    }
                }

                var candidateNumber = $"{prefix}{nextNumber:D4}";

                // Double-check that this number doesn't already exist (handles race conditions)
                var exists = await _context.JournalEntries
                    .AnyAsync(j => j.JournalNumber == candidateNumber);

                if (!exists)
                {
                    return candidateNumber;
                }

                // If it exists, try again with next number
                currentRetry++;
            }

            // Fallback: use timestamp-based number as last resort
            return $"{prefix}{DateTime.Now.Ticks % 10000:D4}";
        }

        private async Task<int> GetNextJournalNumberAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"JE{year}";
            var lastNumber = await _context.JournalEntries
                .Where(j => j.JournalNumber != null && j.JournalNumber.StartsWith(prefix))
                .OrderByDescending(j => j.JournalNumber)
                .Select(j => j.JournalNumber)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(lastNumber) &&
                int.TryParse(lastNumber.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                return number + 1;
            }

            return 1;
        }

        private static string FormatJournalNumber(int number)
        {
            var year = DateTime.Now.Year;
            return $"JE{year}{number:D4}";
        }

        private async Task<string> GenerateReferenceNumberAsync()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var prefix = $"REF{year}{month:D2}";
            var maxRetries = 100; // Prevent infinite loops
            var currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                var lastReference = await _context.JournalEntries
                    .Where(j => j.Reference != null && j.Reference.StartsWith(prefix))
                    .OrderByDescending(j => j.Reference)
                    .Select(j => j.Reference)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (!string.IsNullOrEmpty(lastReference))
                {
                    var numberPart = lastReference.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int number))
                    {
                        nextNumber = number + 1;
                    }
                }

                var candidateReference = $"{prefix}{nextNumber:D4}";

                // Double-check that this reference doesn't already exist (handles race conditions)
                var exists = await _context.JournalEntries
                    .AnyAsync(j => j.Reference == candidateReference);

                if (!exists)
                {
                    return candidateReference;
                }

                // If it exists, try again with next number
                currentRetry++;
            }

            // Fallback: use timestamp-based reference as last resort
            return $"{prefix}{DateTime.Now.Ticks % 10000:D4}";
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

        public async Task<int> AlignJournalTemplateHeadersAsync()
        {
            var allTemplates = await _context.JournalTemplates.ToListAsync();
            int updatedCount = 0;

            foreach (var template in allTemplates)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(template.TemplateData)) continue;

                    bool isNewFormat = false;
                    try
                    {
                        var checkFormat = System.Text.Json.JsonSerializer.Deserialize<JournalTemplateData>(template.TemplateData);
                        if (checkFormat != null && checkFormat.Lines != null)
                        {
                            isNewFormat = true;
                        }
                    }
                    catch
                    {
                        // Not new format
                    }

                    if (!isNewFormat)
                    {
                        // Attempt to deserialize as old format (List<dynamic>)
                        try
                        {
                            var oldLines = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(template.TemplateData);
                            if (oldLines != null && oldLines.Any())
                            {
                                string description = "";
                                string reference = "";

                                // Iterate through ALL lines to find the first non-empty description/reference
                                foreach (var line in oldLines)
                                {
                                    if (string.IsNullOrWhiteSpace(description))
                                    {
                                        if (line.TryGetProperty("Description", out System.Text.Json.JsonElement d) || 
                                            line.TryGetProperty("description", out d))
                                        {
                                             var val = d.GetString();
                                             if (!string.IsNullOrWhiteSpace(val)) description = val;
                                        }
                                    }

                                    if (string.IsNullOrWhiteSpace(reference))
                                    {
                                        if (line.TryGetProperty("Reference", out System.Text.Json.JsonElement r) || 
                                            line.TryGetProperty("reference", out r))
                                        {
                                            var val = r.GetString();
                                            if (!string.IsNullOrWhiteSpace(val)) reference = val;
                                        }
                                    }

                                    if (!string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(reference))
                                        break; // Found both
                                }

                                var newTemplateData = new JournalTemplateData
                                {
                                    Description = description,
                                    Reference = reference,
                                    Lines = oldLines.Select(l => {
                                        // Helper to safely get property
                                        System.Text.Json.JsonElement prop;
                                        int accId = (l.TryGetProperty("AccountId", out prop) || l.TryGetProperty("accountId", out prop)) ? prop.GetInt32() : 0;
                                        string desc = (l.TryGetProperty("Description", out prop) || l.TryGetProperty("description", out prop)) ? prop.GetString() : "";
                                        decimal deb = (l.TryGetProperty("DebitAmount", out prop) || l.TryGetProperty("debitAmount", out prop)) ? prop.GetDecimal() : 0;
                                        decimal cred = (l.TryGetProperty("CreditAmount", out prop) || l.TryGetProperty("creditAmount", out prop)) ? prop.GetDecimal() : 0;
                                        string refVal = (l.TryGetProperty("Reference", out prop) || l.TryGetProperty("reference", out prop)) ? prop.GetString() : "";

                                        return new JournalTemplateLineData
                                        {
                                            AccountId = accId,
                                            Description = desc ?? "",
                                            DebitAmount = deb,
                                            CreditAmount = cred,
                                            Reference = refVal ?? ""
                                        };
                                    }).ToList()
                                };

                                template.TemplateData = System.Text.Json.JsonSerializer.Serialize(newTemplateData);
                                template.UpdatedAt = DateTime.UtcNow;
                                updatedCount++;
                            }
                        }
                        catch (Exception innerEx)
                        {
                             System.Diagnostics.Debug.WriteLine($"Failed to parse as old format for template {template.TemplateId}: {innerEx.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error migrating template {template.TemplateId}: {ex.Message}");
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return updatedCount;
        }

        public async Task<int> AlignJournalLineReferencesAsync()
        {
            // Fetch all journals with their lines
            // We optimize by filtering where lines might have matching template refs that differ from header
            var journals = await _context.JournalEntries
                .Include(j => j.JournalLines)
                .Where(j => j.JournalLines.Any(l => l.Reference != j.Reference))
                .ToListAsync();

            int updatedCount = 0;

            foreach (var journal in journals)
            {
                bool journalUpdated = false;
                foreach (var line in journal.JournalLines)
                {
                    // If line reference doesn't match journal reference, align it
                    if (line.Reference != journal.Reference)
                    {
                        // Optional: Only fix if it looks like a template reference (e.g. starts with REC/EXP)
                        // But user request implies they want them aligned period.
                        line.Reference = journal.Reference;
                        journalUpdated = true;
                    }
                }

                if (journalUpdated)
                {
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return updatedCount;
        }

        #endregion Journal Templates

        #region Reporting and Analysis

        public async Task<decimal> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null)
        {
            var account = await _context.ChartOfAccounts.FindAsync(accountId);
            if (account == null) return 0;

            var openingBalance = account.OpeningBalance;

            var query = _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId && l.JournalEntry.Status == "POSTED");

            if (asOfDate.HasValue)
            {
                var utcDate = asOfDate.Value.Kind == DateTimeKind.Utc ? asOfDate.Value : asOfDate.Value.ToUniversalTime();
                // Use < for "as of" opening balances to exclude transactions ON the start date
                query = query.Where(l => l.LineDate < utcDate);
            }

            var debitTotal = await query.SumAsync(l => l.DebitAmount);
            var creditTotal = await query.SumAsync(l => l.CreditAmount);

            return openingBalance + (debitTotal - creditTotal);
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
                .Include(l => l.Currency)
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

        public async Task<List<JournalLine>> GetCustomerTransactionsAsync(int customerId, int arAccountId = 1100, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.JournalLines
                .Include(l => l.JournalEntry)
                .Include(l => l.Currency)
                .Where(l => l.JournalEntry.Status == "POSTED" && 
                           (l.AccountId == arAccountId || (l.ContactId == customerId && l.ContactType == "Customer")));

            // Filter for lines that are either:
            // 1. Specifically tagged with this customer (ContactId)
            // 2. OR if searching generally in AR account, make sure we only get this customer's lines (if linked)
            // Ideally, lines in AR should have ContactId set.
            query = query.Where(l => l.ContactId == customerId && l.ContactType == "Customer");
            
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
                .OrderBy(l => l.LineDate)
                .ThenBy(l => l.CreatedAt)
                .ToListAsync();
        }

        #endregion Reporting and Analysis
    }
}
