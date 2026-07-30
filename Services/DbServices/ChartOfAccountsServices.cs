using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services.DbServices
{
    public class ChartOfAccountsServices
    {
        private readonly AppDbContext _context;

        public ChartOfAccountsServices(AppDbContext context)
        {
            _context = context;
        }

        #region Chart of Accounts CRUD Operations

        public async Task<ChartOfAccount> CreateAccountAsync(ChartOfAccount account)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate account data
                await ValidateAccountAsync(account);

                // Generate account number if not provided
                if (string.IsNullOrEmpty(account.AccountNumber))
                {
                    account.AccountNumber = await GenerateAccountNumberAsync(account.AccountType);
                }

                // Check if account number is unique
                if (await IsAccountNumberUniqueAsync(account.AccountNumber, null))
                {
                    throw new InvalidOperationException($"Account number '{account.AccountNumber}' already exists.");
                }

                // Check if account name is unique
                if (await IsAccountNameUniqueAsync(account.AccountName, null))
                {
                    throw new InvalidOperationException($"Account name '{account.AccountName}' already exists.");
                }

                // Set timestamps
                account.CreatedAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;

                // Set default values
                if (string.IsNullOrEmpty(account.NormalBalance))
                {
                    account.NormalBalance = GetDefaultNormalBalance(account.AccountType);
                }

                _context.ChartOfAccounts.Add(account);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return account;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // CORRECTED: Show ALL active accounts, but only include POSTED journal lines
        public async Task<List<ChartOfAccount>> GetAllAccountsAsync()
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .Include(a => a.ChildAccounts.Where(c => c.IsActive))
                .Include(a => a.JournalLines.Where(jl => jl.JournalEntry.Status == "POSTED"))  // Filter journal lines, not accounts
                    .ThenInclude(jl => jl.JournalEntry)
                .Where(a => a.IsActive)  // Only filter accounts by IsActive
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        // CORRECTED: Show ALL accounts (including inactive), but only include POSTED journal lines
        public async Task<List<ChartOfAccount>> GetAllAccountsIncludingInactiveAsync()
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .Include(a => a.ChildAccounts)
                .Include(a => a.JournalLines.Where(jl => jl.JournalEntry.Status == "POSTED"))  // Filter journal lines, not accounts
                    .ThenInclude(jl => jl.JournalEntry)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<ChartOfAccount> GetAccountByIdAsync(int accountId)
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .Include(a => a.ChildAccounts.Where(c => c.IsActive))
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<ChartOfAccount> GetAccountByNumberAsync(string accountNumber)
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .Include(a => a.ChildAccounts.Where(c => c.IsActive))
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task<ChartOfAccount> UpdateAccountAsync(ChartOfAccount updatedAccount)
        {
            var account = await _context.ChartOfAccounts
                .Include(a => a.ChildAccounts)
                .Include(a => a.JournalLines)
                .FirstOrDefaultAsync(a => a.AccountId == updatedAccount.AccountId);

            if (account == null) return null;

            // Validate account data
            await ValidateAccountAsync(updatedAccount);

            // Check if account number is unique (excluding current account)
            if (await IsAccountNumberUniqueAsync(updatedAccount.AccountNumber, updatedAccount.AccountId))
            {
                throw new InvalidOperationException($"Account number '{updatedAccount.AccountNumber}' already exists.");
            }

            // Check if account name is unique (excluding current account)
            if (await IsAccountNameUniqueAsync(updatedAccount.AccountName, updatedAccount.AccountId))
            {
                throw new InvalidOperationException($"Account name '{updatedAccount.AccountName}' already exists.");
            }

            // Prevent risky Account Type changes if transactions exist. Allow a narrow
            // metadata repair when the current type conflicts with the account subtype
            // and the new type matches that subtype.
            if (account.JournalLines.Any() && account.AccountType != updatedAccount.AccountType)
            {
                var isMetadataCorrection =
                    !IsSubtypeValidForAccountType(account.AccountType, account.AccountSubtype) &&
                    IsSubtypeValidForAccountType(updatedAccount.AccountType, updatedAccount.AccountSubtype) &&
                    string.Equals(account.AccountSubtype, updatedAccount.AccountSubtype, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(account.NormalBalance, updatedAccount.NormalBalance, StringComparison.OrdinalIgnoreCase);

                if (!isMetadataCorrection)
                {
                    throw new InvalidOperationException("Cannot change Account Type because this account has existing transactions. Changing the type would affect historical financial reports.");
                }
            }

            // Update properties
            account.AccountNumber = updatedAccount.AccountNumber;
            account.AccountName = updatedAccount.AccountName;
            account.AccountType = updatedAccount.AccountType;
            account.AccountSubtype = updatedAccount.AccountSubtype;
            account.Description = updatedAccount.Description;
            account.ParentAccountId = updatedAccount.ParentAccountId;
            account.IsActive = updatedAccount.IsActive;
            account.IsSystemAccount = updatedAccount.IsSystemAccount;
            account.NormalBalance = updatedAccount.NormalBalance;
            account.OpeningBalance = updatedAccount.OpeningBalance;
            account.OpeningBalanceDate = updatedAccount.OpeningBalanceDate;
            account.CurrentBalance = updatedAccount.CurrentBalance;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<bool> DeleteAccountAsync(int accountId)
        {
            var account = await _context.ChartOfAccounts
                .Include(a => a.ChildAccounts)
                .Include(a => a.JournalLines)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null) return false;

            // Check if account can be deleted
            if (account.IsSystemAccount)
            {
                throw new InvalidOperationException("System accounts cannot be deleted.");
            }

            if (account.ChildAccounts.Any(c => c.IsActive))
            {
                throw new InvalidOperationException("Cannot delete account with active child accounts.");
            }

            if (account.JournalLines.Any())
            {
                throw new InvalidOperationException("Cannot delete account with existing transactions.");
            }

            _context.ChartOfAccounts.Remove(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAccountAsync(int accountId)
        {
            var account = await _context.ChartOfAccounts.FindAsync(accountId);
            if (account == null) return false;

            if (account.IsSystemAccount)
            {
                throw new InvalidOperationException("System accounts cannot be deactivated.");
            }

            account.IsActive = false;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReactivateAccountAsync(int accountId)
        {
            var account = await _context.ChartOfAccounts.FindAsync(accountId);
            if (account == null) return false;

            account.IsActive = true;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion Chart of Accounts CRUD Operations

        #region Account Type Operations

        public async Task<List<ChartOfAccount>> GetAccountsByTypeAsync(string accountType)
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .Where(a => a.IsActive && a.AccountType == accountType)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<List<ChartOfAccount>> GetAccountsBySubtypeAsync(string accountSubtype)
        {
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .Where(a => a.IsActive && a.AccountSubtype == accountSubtype)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<List<string>> GetAccountTypesAsync()
        {
            // Return standard account types regardless of what's in the DB
            return new List<string> { "ASSET", "LIABILITY", "EQUITY", "REVENUE", "EXPENSE" };
        }

        public async Task<List<string>> GetAccountSubtypesAsync()
        {
            // Get existing subtypes from DB
            var existingSubtypes = await _context.ChartOfAccounts
                .Where(a => a.IsActive && !string.IsNullOrEmpty(a.AccountSubtype))
                .Select(a => a.AccountSubtype)
                .Distinct()
                .ToListAsync();

            // Add standard subtypes
            var standardSubtypes = new List<string>
            {
                "Cash",
                "Accounts Receivable",
                "Inventory",
                "Prepaid Expenses",
                "Fixed Assets",
                "Accumulated Depreciation",
                "Accounts Payable",
                "Accrued Liabilities",
                "Long Term Debt",
                "Owner's Equity",
                "Retained Earnings",
                "Sales Revenue",
                "Service Revenue",
                "Cost of Goods Sold",
                "Operating Expenses",
                "Other Income",
                "Other Expenses"
            };

            // Combine and sort
            return existingSubtypes
                .Union(standardSubtypes)
                .OrderBy(s => s)
                .ToList();
        }

        #endregion Account Type Operations

        #region Parent-Child Account Operations

        public async Task<List<ChartOfAccount>> GetParentAccountsAsync()
        {
            return await _context.ChartOfAccounts
                .Where(a => a.IsActive && a.ParentAccountId == null)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<List<ChartOfAccount>> GetChildAccountsAsync(int parentAccountId)
        {
            return await _context.ChartOfAccounts
                .Where(a => a.IsActive && a.ParentAccountId == parentAccountId)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<bool> CanSetParentAccountAsync(int accountId, int? parentAccountId)
        {
            if (!parentAccountId.HasValue) return true;

            // Check if parent account exists and is active
            var parentAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountId == parentAccountId.Value && a.IsActive);

            if (parentAccount == null) return false;

            // Check for circular reference
            if (accountId == parentAccountId.Value) return false;

            // Check if setting this parent would create a circular reference
            var currentAccount = await _context.ChartOfAccounts.FindAsync(accountId);
            if (currentAccount == null) return true; // New account cannot have descendants yet

            // Check if the proposed parent is a descendant of the current account
            return !await IsDescendantAsync(parentAccountId.Value, accountId);
        }

        public async Task<List<ChartOfAccount>> GetAccountHierarchyAsync()
        {
            var accounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();

            return BuildAccountHierarchy(accounts);
        }

        #endregion Parent-Child Account Operations

        #region Search and Filter Operations

        public async Task<List<ChartOfAccount>> SearchAccountsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllAccountsAsync();
            }

            var searchLower = searchTerm.ToLower();
            return await _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .Where(a => a.IsActive &&
                           (a.AccountNumber.Contains(searchLower) ||
                            a.AccountName.ToLower().Contains(searchLower) ||
                            (a.Description != null && a.Description.ToLower().Contains(searchLower))))
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<List<ChartOfAccount>> FilterAccountsAsync(string accountType = null, string accountSubtype = null, bool? isActive = null, int? parentAccountId = null)
        {
            var query = _context.ChartOfAccounts
                .Include(a => a.ParentAccount)
                .AsQueryable();

            if (!string.IsNullOrEmpty(accountType))
            {
                query = query.Where(a => a.AccountType == accountType);
            }

            if (!string.IsNullOrEmpty(accountSubtype))
            {
                query = query.Where(a => a.AccountSubtype == accountSubtype);
            }

            if (isActive.HasValue)
            {
                query = query.Where(a => a.IsActive == isActive.Value);
            }

            if (parentAccountId.HasValue)
            {
                query = query.Where(a => a.ParentAccountId == parentAccountId.Value);
            }

            return await query
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        #endregion Search and Filter Operations

        #region Account Balance Operations

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

            // Calculate balance based on normal balance type (with fallback to account type)
            var normalBalance = account.NormalBalance;
            if (string.IsNullOrEmpty(normalBalance))
            {
                normalBalance = account.AccountType.ToUpper() switch
                {
                    "ASSET" or "EXPENSE" => "DEBIT",
                    "LIABILITY" or "EQUITY" or "REVENUE" => "CREDIT",
                    _ => "DEBIT"
                };
            }

            if (normalBalance == "DEBIT")
            {
                // Assets, Expenses: OpeningBalance + Debit - Credit
                return openingBalance + (debitTotal - creditTotal);
            }
            else
            {
                // Liabilities, Equity, Revenue: OpeningBalance + Credit - Debit
                return openingBalance + (creditTotal - debitTotal);
            }
        }

        public async Task<Dictionary<int, decimal>> GetAccountBalancesAsync(List<int> accountIds, DateTime? asOfDate = null)
        {
            var query = _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => accountIds.Contains(l.AccountId) && l.JournalEntry.Status == "POSTED");

            if (asOfDate.HasValue)
            {
                var utcDate = asOfDate.Value.Kind == DateTimeKind.Utc ? asOfDate.Value : asOfDate.Value.ToUniversalTime();
                query = query.Where(l => l.LineDate <= utcDate);
            }

            var balances = await query
                .GroupBy(l => l.AccountId)
                .Select(g => new
                {
                    AccountId = g.Key,
                    DebitTotal = g.Sum(l => l.DebitAmount),
                    CreditTotal = g.Sum(l => l.CreditAmount)
                })
                .ToListAsync();

            var accounts = await _context.ChartOfAccounts
                .Where(a => accountIds.Contains(a.AccountId))
                .ToListAsync();

            var result = new Dictionary<int, decimal>();
            foreach (var account in accounts)
            {
                var balanceInfo = balances.FirstOrDefault(b => b.AccountId == account.AccountId);
                var debitTotal = balanceInfo?.DebitTotal ?? 0;
                var creditTotal = balanceInfo?.CreditTotal ?? 0;

                if (account.NormalBalance == "DEBIT")
                {
                    result[account.AccountId] = account.OpeningBalance + (debitTotal - creditTotal);
                }
                else
                {
                    result[account.AccountId] = account.OpeningBalance + (creditTotal - debitTotal);
                }
            }

            // Add accounts with zero balance
            foreach (var accountId in accountIds)
            {
                if (!result.ContainsKey(accountId))
                {
                    result[accountId] = 0;
                }
            }

            return result;
        }

        public async Task<bool> UpdateAccountBalanceAsync(int accountId)
        {
            var account = await _context.ChartOfAccounts.FindAsync(accountId);
            if (account == null) return false;

            var balance = await GetAccountBalanceAsync(accountId);
            account.CurrentBalance = balance;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAllAccountBalancesAsync()
        {
            var accounts = await _context.ChartOfAccounts.ToListAsync();
            var accountIds = accounts.Select(a => a.AccountId).ToList();

            var balances = await GetAccountBalancesAsync(accountIds);

            foreach (var account in accounts)
            {
                if (balances.ContainsKey(account.AccountId))
                {
                    account.CurrentBalance = balances[account.AccountId];
                    account.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion Account Balance Operations

        #region Validation and Helper Methods

        private async Task ValidateAccountAsync(ChartOfAccount account)
        {
            if (string.IsNullOrWhiteSpace(account.AccountName))
            {
                throw new ArgumentException("Account name is required.");
            }

            if (string.IsNullOrWhiteSpace(account.AccountType))
            {
                throw new ArgumentException("Account type is required.");
            }

            // Validate account type
            var validTypes = new[] { "ASSET", "LIABILITY", "EQUITY", "REVENUE", "EXPENSE" };
            if (!validTypes.Contains(account.AccountType.ToUpper()))
            {
                throw new ArgumentException($"Invalid account type. Must be one of: {string.Join(", ", validTypes)}");
            }

            // Validate normal balance
            if (!string.IsNullOrEmpty(account.NormalBalance))
            {
                var validBalances = new[] { "DEBIT", "CREDIT" };
                if (!validBalances.Contains(account.NormalBalance.ToUpper()))
                {
                    throw new ArgumentException("Invalid normal balance. Must be 'DEBIT' or 'CREDIT'.");
                }
            }

            // Validate parent account if specified
            if (account.ParentAccountId.HasValue)
            {
                var canSetParent = await CanSetParentAccountAsync(account.AccountId, account.ParentAccountId.Value);
                if (!canSetParent)
                {
                    throw new ArgumentException("Cannot set parent account. This would create a circular reference or parent account is invalid.");
                }
            }
        }

        private async Task<string> GenerateAccountNumberAsync(string accountType)
        {
            var typePrefix = GetAccountTypePrefix(accountType);
            var lastNumber = await _context.ChartOfAccounts
                .Where(a => a.AccountNumber.StartsWith(typePrefix))
                .OrderByDescending(a => a.AccountNumber)
                .Select(a => a.AccountNumber)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastNumber))
            {
                return $"{typePrefix}0001";
            }

            var numberPart = lastNumber.Substring(typePrefix.Length);
            if (int.TryParse(numberPart, out int number))
            {
                return $"{typePrefix}{(number + 1):D4}";
            }

            return $"{typePrefix}0001";
        }

        private string GetAccountTypePrefix(string accountType)
        {
            return accountType.ToUpper() switch
            {
                "ASSET" => "1",
                "LIABILITY" => "2",
                "EQUITY" => "3",
                "REVENUE" => "4",
                "EXPENSE" => "5",
                _ => "0"
            };
        }

        private string GetDefaultNormalBalance(string accountType)
        {
            return accountType.ToUpper() switch
            {
                "ASSET" or "EXPENSE" => "DEBIT",
                "LIABILITY" or "EQUITY" or "REVENUE" => "CREDIT",
                _ => "DEBIT"
            };
        }

        private static bool IsSubtypeValidForAccountType(string accountType, string accountSubtype)
        {
            if (string.IsNullOrWhiteSpace(accountType) || string.IsNullOrWhiteSpace(accountSubtype))
                return true;

            var subtype = accountSubtype.ToUpperInvariant();

            return accountType.ToUpperInvariant() switch
            {
                "ASSET" => subtype is "CURRENT_ASSET" or "FIXED_ASSET" or "INTANGIBLE_ASSET" or
                           "CASH" or "BANK" or "ACCOUNTS RECEIVABLE" or "INVENTORY" or "PREPAID EXPENSES",
                "LIABILITY" => subtype is "CURRENT_LIABILITY" or "LONG_TERM_LIABILITY" or
                               "ACCOUNTS PAYABLE" or "ACCRUED LIABILITIES" or "LONG TERM DEBT",
                "EQUITY" => subtype is "CAPITAL" or "OWNER'S EQUITY" or "RETAINED_EARNINGS" or
                            "NET_INCOME" or "DIVIDENDS" or "TREASURY_STOCK",
                "REVENUE" => subtype is "OPERATING_REVENUE" or "OTHER_INCOME" or "OTHER REVENUE" or
                             "OTHER INCOME" or "CONTRA_REVENUE" or "FEE INCOME" or "MISCELLANEOUS INCOME",
                "EXPENSE" => subtype is "COGS" or "OPERATING_EXPENSE" or "OTHER_EXPENSE" or
                             "FINANCIAL_EXPENSE" or "TAX_EXPENSE",
                _ => true
            };
        }

        private async Task<bool> IsAccountNumberUniqueAsync(string accountNumber, int? excludeAccountId)
        {
            var query = _context.ChartOfAccounts.Where(a => a.AccountNumber == accountNumber);

            if (excludeAccountId.HasValue)
            {
                query = query.Where(a => a.AccountId != excludeAccountId.Value);
            }

            return await query.AnyAsync();
        }

        private async Task<bool> IsAccountNameUniqueAsync(string accountName, int? excludeAccountId)
        {
            var query = _context.ChartOfAccounts.Where(a => a.AccountName == accountName);

            if (excludeAccountId.HasValue)
            {
                query = query.Where(a => a.AccountId != excludeAccountId.Value);
            }

            return await query.AnyAsync();
        }

        private async Task<bool> IsDescendantAsync(int ancestorId, int descendantId)
        {
            var currentAccount = await _context.ChartOfAccounts.FindAsync(descendantId);
            if (currentAccount?.ParentAccountId == null) return false;

            if (currentAccount.ParentAccountId == ancestorId) return true;

            return await IsDescendantAsync(ancestorId, currentAccount.ParentAccountId.Value);
        }

        private List<ChartOfAccount> BuildAccountHierarchy(List<ChartOfAccount> accounts)
        {
            var accountDict = accounts.ToDictionary(a => a.AccountId);
            var rootAccounts = new List<ChartOfAccount>();

            foreach (var account in accounts)
            {
                if (account.ParentAccountId.HasValue && accountDict.ContainsKey(account.ParentAccountId.Value))
                {
                    var parent = accountDict[account.ParentAccountId.Value];
                    if (parent.ChildAccounts == null)
                    {
                        parent.ChildAccounts = new List<ChartOfAccount>();
                    }
                    parent.ChildAccounts.Add(account);
                }
                else
                {
                    rootAccounts.Add(account);
                }
            }

            return rootAccounts;
        }

        #endregion Validation and Helper Methods

        #region Reporting Operations

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

        public async Task<List<ChartOfAccount>> GetAccountsWithTransactionsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.ChartOfAccounts
                .Where(a => a.JournalLines.Any());

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.JournalLines.Any(l => l.LineDate >= fromDate.Value));
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.JournalLines.Any(l => l.LineDate <= toDate.Value));
            }

            return await query
                .Include(a => a.JournalLines)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        #endregion Reporting Operations
    }
}
