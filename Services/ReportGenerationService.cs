using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services
{
    public class ReportGenerationService
    {
        private readonly AppDbContext _context;
        private readonly BoxServices _messageBoxService = new();

        public ReportGenerationService(AppDbContext context)
        {
            _context = context;
        }

        #region Balance Sheet

        public async Task<BalanceSheetData> GenerateBalanceSheetAsync(DateTime asOfDate)
        {
            var report = new BalanceSheetData
            {
                ReportTitle = "Statement of Financial Position",
                StartDate = asOfDate,
                EndDate = asOfDate
            };

            // Get all active accounts
            var accounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive)
                .ToListAsync();

            // Calculate balances for each account (respecting normal balance)
            var accountBalances = new Dictionary<int, (decimal Balance, ChartOfAccount Account)>();
            foreach (var account in accounts)
            {
                var balance = await GetAccountBalanceAsync(account.AccountId, asOfDate);

                decimal displayBalance;
                if (account.AccountType == "ASSET")
                {
                    // Assets have DEBIT normal balance, so debit-credit is positive
                    displayBalance = balance;
                }
                else if (account.AccountType == "LIABILITY" || account.AccountType == "EQUITY")
                {
                    // Liabilities & Equity have CREDIT normal balance — flip sign for display
                    displayBalance = -balance;
                }
                else
                {
                    displayBalance = balance;
                }

                accountBalances[account.AccountId] = (displayBalance, account);
            }

            // === ASSETS ===
            var assets = accounts
                .Where(a => string.Equals(a.AccountType, "ASSET", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // --- Fixed Assets — register provides the schedule structure (name, grouping),
            // but Cost and Accum Dep are read from the actual GL account balances so the
            // balance sheet stays tied to the double-entry and will always balance.
            // Fixed asset reporting must be driven by the GL, not only by active
            // asset-register rows. If an asset register row is deleted while its
            // acquisition journal remains posted, the fixed asset account still
            // belongs on the balance sheet.
            var fixedAssetGlAccountIds = new HashSet<int>(
                assets
                    .Where(a => string.Equals(a.AccountSubtype, "FIXED_ASSET", StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.AccountId)
            );

            var fixedAssetAccountGroup = new FixedAssetGroup { CategoryName = "Fixed Asset Accounts" };

            foreach (var account in assets
                .Where(a => fixedAssetGlAccountIds.Contains(a.AccountId) &&
                            string.Equals(a.NormalBalance, "DEBIT", StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.AccountNumber))
            {
                var cost = await GetAccountBalanceAsync(account.AccountId, asOfDate);

                if (Math.Abs(cost) <= 0.01m)
                    continue;

                fixedAssetAccountGroup.Assets.Add(new FixedAssetLineItem
                {
                    AssetId = 0,
                    AssetCode = account.AccountNumber,
                    AssetName = account.AccountName,
                    Cost = cost,
                    AccumulatedDepreciation = 0,
                    NetBookValue = cost
                });
            }

            foreach (var account in assets
                .Where(a => fixedAssetGlAccountIds.Contains(a.AccountId) &&
                            string.Equals(a.NormalBalance, "CREDIT", StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.AccountNumber))
            {
                var accumDep = -await GetAccountBalanceAsync(account.AccountId, asOfDate);

                if (Math.Abs(accumDep) <= 0.01m)
                    continue;

                fixedAssetAccountGroup.Assets.Add(new FixedAssetLineItem
                {
                    AssetId = 0,
                    AssetCode = account.AccountNumber,
                    AssetName = account.AccountName,
                    Cost = 0,
                    AccumulatedDepreciation = accumDep,
                    NetBookValue = -accumDep
                });
            }

            if (fixedAssetAccountGroup.Assets.Any())
            {
                report.FixedAssetGroups.Add(fixedAssetAccountGroup);
                report.TotalFixedAssetsCost = fixedAssetAccountGroup.TotalCost;
                report.TotalAccumDepreciation = fixedAssetAccountGroup.TotalAccumDep;
                report.TotalFixedAssets = fixedAssetAccountGroup.TotalNBV;
            }

            // --- Current Assets ---
            var currentAssetSubtypes = new[]
            {
                "CURRENT_ASSET", "Cash", "Accounts Receivable", "Inventory", "Prepaid Expenses"
            };

            foreach (var account in assets.Where(a =>
                currentAssetSubtypes.Contains(a.AccountSubtype, StringComparer.OrdinalIgnoreCase) &&
                !fixedAssetGlAccountIds.Contains(a.AccountId)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.CurrentAssets.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance
                    });
                    report.TotalCurrentAssets += data.Balance;
                }
            }

            // Exclude fixed asset GL accounts from the account balances dictionary
            // so they cannot be picked up by any other section (current assets, etc.)
            foreach (var id in fixedAssetGlAccountIds)
                accountBalances.Remove(id);

            report.TotalAssets = report.TotalCurrentAssets + report.TotalFixedAssets;

            // === LIABILITIES ===
            var liabilities = accounts
                .Where(a => string.Equals(a.AccountType, "LIABILITY", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // --- Current Liabilities ---
            var currentLiabilitySubtypes = new[]
            {
                "CURRENT_LIABILITY", "Accounts Payable", "Accrued Liabilities"
            };

            foreach (var account in liabilities.Where(a =>
                currentLiabilitySubtypes.Contains(a.AccountSubtype, StringComparer.OrdinalIgnoreCase)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.CurrentLiabilities.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance
                    });
                    report.TotalCurrentLiabilities += data.Balance;
                }
            }

            // --- Long-term Liabilities ---
            var longTermLiabilitySubtypes = new[] { "LONG_TERM_LIABILITY", "Long Term Debt" };

            foreach (var account in liabilities.Where(a =>
                longTermLiabilitySubtypes.Contains(a.AccountSubtype, StringComparer.OrdinalIgnoreCase)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.LongTermLiabilities.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance
                    });
                    report.TotalLongTermLiabilities += data.Balance;
                }
            }

            // --- Other Liabilities (catch-all for any unclassified liability accounts) ---
            foreach (var account in liabilities)
            {
                if (report.CurrentLiabilities.Any(i => i.AccountId == account.AccountId) ||
                    report.LongTermLiabilities.Any(i => i.AccountId == account.AccountId))
                    continue;

                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.LongTermLiabilities.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName + " (Other Liability)",
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance
                    });
                    report.TotalLongTermLiabilities += data.Balance;
                }
            }

            report.TotalLiabilities = report.TotalCurrentLiabilities + report.TotalLongTermLiabilities;

            // === EQUITY ===
            var equity = accounts
                .Where(a => string.Equals(a.AccountType, "EQUITY", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // --- Capital ---
            foreach (var account in equity.Where(a =>
                string.Equals(a.AccountSubtype, "CAPITAL", StringComparison.OrdinalIgnoreCase)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.Equity.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance
                    });
                    report.TotalEquity += data.Balance;
                }
            }

            // --- Owner's Equity / Drawings ---
            foreach (var account in equity.Where(a =>
                string.Equals(a.AccountSubtype, "Owner's Equity", StringComparison.OrdinalIgnoreCase)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.Equity.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance
                    });
                    report.TotalEquity += data.Balance;
                }
            }

            // --- Retained Earnings ---
            // Historical net income (all years before the current fiscal year) is folded
            // into the Retained Earnings account balance because books are not formally closed.
            var fiscalYearStart = GetFiscalYearStart(asOfDate);
            var historicalNetIncome = await CalculateNetIncomeAsync(new DateTime(1900, 1, 1), fiscalYearStart.AddDays(-1));
            bool retainedEarningsAdded = false;

            foreach (var account in equity.Where(a =>
                string.Equals(a.AccountSubtype, "RETAINED_EARNINGS", StringComparison.OrdinalIgnoreCase)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data))
                {
                    decimal finalAmount = data.Balance;

                    if (!retainedEarningsAdded)
                    {
                        finalAmount += historicalNetIncome;
                        retainedEarningsAdded = true;
                    }

                    if (Math.Abs(finalAmount) > 0.01m)
                    {
                        report.Equity.Add(new AccountLineItem
                        {
                            AccountId = account.AccountId,
                            AccountNumber = account.AccountNumber,
                            AccountName = account.AccountName,
                            AccountType = account.AccountType,
                            AccountSubtype = account.AccountSubtype,
                            NormalBalance = account.NormalBalance,
                            Amount = finalAmount
                        });
                        report.TotalEquity += finalAmount;
                    }
                }
            }

            // If no Retained Earnings account exists but we have prior-year income, show a calculated line
            if (!retainedEarningsAdded && Math.Abs(historicalNetIncome) > 0.01m)
            {
                report.Equity.Add(new AccountLineItem
                {
                    AccountNumber = "",
                    AccountName = "Retained Earnings (Calculated)",
                    AccountType = "EQUITY",
                    AccountSubtype = "RETAINED_EARNINGS",
                    NormalBalance = "CREDIT",
                    Amount = historicalNetIncome
                });
                report.TotalEquity += historicalNetIncome;
            }

            // --- Net Income (current fiscal year, books not yet closed) ---
            var netIncome = await CalculateNetIncomeAsync(fiscalYearStart, asOfDate);
            if (Math.Abs(netIncome) > 0.01m)
            {
                report.Equity.Add(new AccountLineItem
                {
                    AccountNumber = "",
                    AccountName = "Net Income (Current Period)",
                    AccountType = "EQUITY",
                    AccountSubtype = "NET_INCOME",
                    NormalBalance = "CREDIT",
                    Amount = netIncome
                });
                report.TotalEquity += netIncome;
            }

            // --- Dividends (reduces equity) ---
            foreach (var account in equity.Where(a =>
                string.Equals(a.AccountSubtype, "DIVIDENDS", StringComparison.OrdinalIgnoreCase)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.Equity.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance // negative — reduces equity
                    });
                    report.TotalEquity += data.Balance;
                }
            }

            // --- Treasury Stock (reduces equity) ---
            foreach (var account in equity.Where(a =>
                string.Equals(a.AccountSubtype, "TREASURY_STOCK", StringComparison.OrdinalIgnoreCase)))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) &&
                    Math.Abs(data.Balance) > 0.01m)
                {
                    report.Equity.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance // negative — reduces equity
                    });
                    report.TotalEquity += data.Balance;
                }
            }

            report.TotalLiabilitiesAndEquity = report.TotalLiabilities + report.TotalEquity;

            return report;
        }

        #endregion Balance Sheet

        #region Income Statement

        public async Task<IncomeStatementData> GenerateIncomeStatementAsync(DateTime startDate, DateTime endDate)
        {
            var report = new IncomeStatementData
            {
                ReportTitle = "Income Statement",
                StartDate = startDate,
                EndDate = endDate
            };

            // Get all revenue and expense accounts
            var accounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive &&
                           (a.AccountType == "REVENUE" || a.AccountType == "EXPENSE"))
                .ToListAsync();

            // Calculate activity for the period and convert to display sign
            var accountBalances = new Dictionary<int, decimal>();
            foreach (var account in accounts)
            {
                var balance = await GetAccountBalanceForPeriodAsync(account.AccountId, startDate, endDate);

                decimal displayBalance;
                if (account.AccountType == "REVENUE")
                {
                    // Revenue: CREDIT normal balance — flip debit-credit to show positive
                    displayBalance = -balance;
                }
                else
                {
                    // Expense: DEBIT normal balance — debit-credit is already positive
                    displayBalance = balance;
                }

                accountBalances[account.AccountId] = displayBalance;
            }

            // === REVENUE ===
            // Other Income subtypes are excluded here; they appear below Gross Profit
            var otherIncomeSubtypes = new[]
            {
                "OTHER_INCOME", "Other Income", "Other Revenue",
                "Uncategorized Revenue", "Fee Income", "Miscellaneous Income"
            };

            var revenueAccounts = accounts
                .Where(a => a.AccountType == "REVENUE" &&
                            !string.Equals(a.AccountSubtype, "CONTRA_REVENUE", StringComparison.OrdinalIgnoreCase) &&
                            !otherIncomeSubtypes.Contains(a.AccountSubtype, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var account in revenueAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) &&
                    Math.Abs(balance) > 0.01m)
                {
                    report.Revenue.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        Amount = balance
                    });
                    report.TotalRevenue += balance;
                }
            }

            // Contra Revenue (Sales Returns, Discounts) — reduces TotalRevenue
            var contraRevenue = accounts
                .Where(a => a.AccountType == "REVENUE" &&
                            string.Equals(a.AccountSubtype, "CONTRA_REVENUE", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var account in contraRevenue)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) &&
                    Math.Abs(balance) > 0.01m)
                {
                    report.TotalRevenue -= Math.Abs(balance);
                }
            }

            // === OTHER INCOME ===
            // Collected separately — added to income AFTER Gross Profit (below operating expenses)
            var otherIncomeAccounts = accounts
                .Where(a => a.AccountType == "REVENUE" &&
                            otherIncomeSubtypes.Contains(a.AccountSubtype, StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var account in otherIncomeAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) &&
                    Math.Abs(balance) > 0.01m)
                {
                    report.OtherIncome.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        Amount = balance
                    });
                    report.TotalOtherIncome += balance;
                }
            }

            // === COST OF GOODS SOLD ===
            var cogsAccounts = accounts
                .Where(a => a.AccountType == "EXPENSE" && a.AccountSubtype == "COGS")
                .ToList();

            foreach (var account in cogsAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) &&
                    Math.Abs(balance) > 0.01m)
                {
                    report.CostOfGoodsSold.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        Amount = balance
                    });
                    report.TotalCOGS += balance;
                }
            }

            // Gross Profit = Core Revenue - COGS only.
            // Other Income is intentionally excluded here; it belongs below Operating Income.
            report.GrossProfit = report.TotalRevenue - report.TotalCOGS;

            // === OPERATING EXPENSES ===
            var opeAccounts = accounts
                .Where(a => a.AccountType == "EXPENSE" && a.AccountSubtype == "OPERATING_EXPENSE")
                .ToList();

            foreach (var account in opeAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) &&
                    Math.Abs(balance) > 0.01m)
                {
                    report.OperatingExpenses.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        Amount = balance
                    });
                    report.TotalOperatingExpenses += balance;
                }
            }

            report.OperatingIncome = report.GrossProfit - report.TotalOperatingExpenses;

            // === OTHER EXPENSES ===
            var otherExpenseAccounts = accounts
                .Where(a => a.AccountType == "EXPENSE" &&
                           (a.AccountSubtype == "OTHER_EXPENSE" ||
                            a.AccountSubtype == "FINANCIAL_EXPENSE" ||
                            a.AccountSubtype == "TAX_EXPENSE"))
                .ToList();

            foreach (var account in otherExpenseAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) &&
                    Math.Abs(balance) > 0.01m)
                {
                    report.OtherExpenses.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        Amount = balance
                    });
                    report.TotalOtherExpenses += balance;
                }
            }

            // === NET INCOME ===
            // Waterfall: Revenue - COGS = Gross Profit
            //            Gross Profit - Operating Expenses = Operating Income
            //            Operating Income + Other Income - Other Expenses = Net Income
            report.NetIncome = report.OperatingIncome + report.TotalOtherIncome - report.TotalOtherExpenses;

            return report;
        }

        #endregion Income Statement

        #region Trial Balance

        public async Task<TrialBalanceData> GenerateTrialBalanceAsync(DateTime asOfDate)
        {
            var asOfDateUtc = ToUtc(asOfDate, isEndDate: true);

            var report = new TrialBalanceData
            {
                ReportTitle = "Trial Balance",
                StartDate = asOfDate,
                EndDate = asOfDate
            };

            var accounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();

            foreach (var account in accounts)
            {
                var openingBalance = account.OpeningBalance;

                var debitActivity = await _context.JournalLines
                    .Include(l => l.JournalEntry)
                    .Where(l => l.AccountId == account.AccountId &&
                               l.LineDate <= asOfDateUtc &&
                               l.JournalEntry.Status == "POSTED")
                    .SumAsync(l => (decimal?)l.DebitAmount) ?? 0;

                var creditActivity = await _context.JournalLines
                    .Include(l => l.JournalEntry)
                    .Where(l => l.AccountId == account.AccountId &&
                               l.LineDate <= asOfDateUtc &&
                               l.JournalEntry.Status == "POSTED")
                    .SumAsync(l => (decimal?)l.CreditAmount) ?? 0;

                decimal totalDebits = debitActivity;
                decimal totalCredits = creditActivity;

                if (account.NormalBalance == "CREDIT")
                    totalCredits += openingBalance;
                else
                    totalDebits += openingBalance;

                if (Math.Abs(totalDebits) > 0.001m || Math.Abs(totalCredits) > 0.001m)
                {
                    report.Accounts.Add(new TrialBalanceLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        NormalBalance = account.NormalBalance,
                        DebitAmount = totalDebits,
                        CreditAmount = totalCredits
                    });

                    report.TotalDebits += totalDebits;
                    report.TotalCredits += totalCredits;
                }
            }

            return report;
        }

        #endregion Trial Balance

        #region Asset Register

        public async Task<AssetRegisterReportData> GenerateAssetRegisterAsync(DateTime asOfDate)
        {
            var report = new AssetRegisterReportData
            {
                ReportTitle = "Asset Register",
                StartDate = asOfDate,
                EndDate = asOfDate
            };

            var assets = await _context.FixedAssets
                .Include(a => a.Category)
                .Include(a => a.AssetAccount)
                .Where(a => a.IsActive && a.PurchaseDate <= ToUtc(asOfDate, true))
                .OrderBy(a => a.Category.CategoryName)
                .ThenBy(a => a.AssetCode)
                .ThenBy(a => a.AssetName)
                .ToListAsync();

            foreach (var group in assets.GroupBy(a => a.Category?.CategoryName ?? "Uncategorised"))
            {
                var categoryGroup = new AssetRegisterCategoryGroup
                {
                    CategoryName = group.Key
                };

                foreach (var asset in group)
                {
                    categoryGroup.Assets.Add(new AssetRegisterLineItem
                    {
                        AssetCode = asset.AssetCode,
                        AssetName = asset.AssetName,
                        PurchaseDate = asset.PurchaseDate,
                        PurchaseCost = asset.PurchaseCost,
                        AccumulatedDepreciation = asset.AccumulatedDepreciation,
                        BookValue = asset.BookValue,
                        ResidualValue = asset.ResidualValue,
                        UsefulLifeYears = asset.UsefulLifeYears,
                        DepreciationMethod = asset.DepreciationMethod,
                        Status = asset.Status,
                        AssetAccountName = asset.AssetAccount?.AccountName ?? string.Empty
                    });
                }

                report.CategoryGroups.Add(categoryGroup);
            }

            report.TotalAssets = assets.Count;
            report.TotalCost = report.CategoryGroups.Sum(g => g.TotalCost);
            report.TotalAccumulatedDepreciation = report.CategoryGroups.Sum(g => g.TotalAccumulatedDepreciation);
            report.TotalBookValue = report.CategoryGroups.Sum(g => g.TotalBookValue);

            return report;
        }

        #endregion Asset Register

        #region Cash Flow Statement

        public async Task<CashFlowData> GenerateCashFlowAsync(DateTime startDate, DateTime endDate)
        {
            var report = new CashFlowData
            {
                ReportTitle = "Cash Flow Statement",
                StartDate = startDate,
                EndDate = endDate
            };

            // Identify cash and bank accounts
            var cashAccounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive &&
                           a.AccountType == "ASSET" &&
                           (a.AccountName.Contains("Cash") || a.AccountName.Contains("Bank")))
                .ToListAsync();

            decimal beginningCash = 0;
            decimal endingCash = 0;

            foreach (var cashAccount in cashAccounts)
            {
                beginningCash += await GetAccountBalanceAsync(cashAccount.AccountId, startDate.AddDays(-1));
                endingCash += await GetAccountBalanceAsync(cashAccount.AccountId, endDate);
            }

            report.BeginningCashBalance = beginningCash;
            report.EndingCashBalance = endingCash;

            // === OPERATING ACTIVITIES (Indirect Method) ===

            var netIncome = await CalculateNetIncomeAsync(startDate, endDate);
            report.OperatingActivities.Add(new CashFlowLineItem
            {
                Description = "Net Income",
                Amount = netIncome,
                Category = "OPERATING"
            });

            var operatingTotal = netIncome;

            // Add back non-cash charges (depreciation / amortisation)
            var depreciationAccounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive &&
                           a.AccountType == "EXPENSE" &&
                           (a.AccountName.Contains("Depreciation") || a.AccountName.Contains("Amortization")))
                .ToListAsync();

            foreach (var depAcc in depreciationAccounts)
            {
                var amount = await GetAccountBalanceForPeriodAsync(depAcc.AccountId, startDate, endDate);
                if (Math.Abs(amount) > 0.01m)
                {
                    report.OperatingActivities.Add(new CashFlowLineItem
                    {
                        Description = $"Add back: {depAcc.AccountName}",
                        Amount = amount,
                        Category = "OPERATING"
                    });
                    operatingTotal += amount;
                }
            }

            // Changes in working capital
            // Include current assets & liabilities; exclude fixed assets, intangibles, and long-term debt
            var workingCapitalAccounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive &&
                           (a.AccountType == "ASSET" || a.AccountType == "LIABILITY") &&
                           a.AccountSubtype != "FIXED_ASSET" &&
                           a.AccountSubtype != "INTANGIBLE_ASSET" &&
                           a.AccountSubtype != "LONG_TERM_LIABILITY" &&
                           a.AccountSubtype != "Fixed Assets" &&
                           a.AccountSubtype != "Long Term Debt")
                .ToListAsync();

            foreach (var acc in workingCapitalAccounts)
            {
                // Cash/bank accounts are the target — exclude from adjustments
                if (cashAccounts.Any(c => c.AccountId == acc.AccountId)) continue;

                var change = await GetAccountBalanceChangeAsync(acc.AccountId, startDate, endDate);
                if (Math.Abs(change) > 0.01m)
                {
                    // Indirect method sign logic:
                    // Asset increases use cash (outflow)  → Amount = -change
                    // Liability increases free cash (inflow) → Amount = -change (change is negative for liabilities)
                    report.OperatingActivities.Add(new CashFlowLineItem
                    {
                        Description = $"Change in {acc.AccountName}",
                        Amount = -change,
                        Category = "OPERATING"
                    });
                    operatingTotal -= change;
                }
            }

            report.NetCashFromOperating = operatingTotal;

            // === INVESTING ACTIVITIES ===
            report.NetCashFromInvesting = report.InvestingActivities.Sum(a => a.Amount);

            // === FINANCING ACTIVITIES ===
            report.NetCashFromFinancing = report.FinancingActivities.Sum(a => a.Amount);

            report.NetChangeInCash = report.NetCashFromOperating +
                                     report.NetCashFromInvesting +
                                     report.NetCashFromFinancing;

            return report;
        }

        #endregion Cash Flow Statement

        #region Helper Methods

        private async Task<decimal> GetAccountBalanceAsync(int accountId, DateTime asOfDate)
        {
            var asOfDateUtc = ToUtc(asOfDate, isEndDate: true);

            var account = await _context.ChartOfAccounts.FindAsync(accountId);
            var openingBalance = account?.OpeningBalance ?? 0;

            var debitTotal = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                           l.LineDate <= asOfDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => (decimal?)l.DebitAmount) ?? 0;

            var creditTotal = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                           l.LineDate <= asOfDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => (decimal?)l.CreditAmount) ?? 0;

            if (account?.NormalBalance == "CREDIT")
            {
                // Opening balance is a credit; return debit-credit net minus opening
                return (debitTotal - creditTotal) - openingBalance;
            }
            else
            {
                // Opening balance is a debit; add it to debit-credit net
                return openingBalance + (debitTotal - creditTotal);
            }
        }

        private async Task<decimal> GetAccountBalanceForPeriodAsync(int accountId, DateTime startDate, DateTime endDate)
        {
            var startDateUtc = ToUtc(startDate);
            var endDateUtc = ToUtc(endDate, isEndDate: true);

            var debitTotal = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                           l.LineDate >= startDateUtc &&
                           l.LineDate <= endDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => l.DebitAmount);

            var creditTotal = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                           l.LineDate >= startDateUtc &&
                           l.LineDate <= endDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => l.CreditAmount);

            return debitTotal - creditTotal;
        }

        private async Task<decimal> CalculateNetIncomeAsync(DateTime startDate, DateTime endDate)
        {
            var startDateUtc = ToUtc(startDate);
            var endDateUtc = ToUtc(endDate, isEndDate: true);

            var revenue = await _context.JournalLines
                .Include(l => l.ChartOfAccount)
                .Include(l => l.JournalEntry)
                .Where(l => l.ChartOfAccount.AccountType == "REVENUE" &&
                           l.LineDate >= startDateUtc &&
                           l.LineDate <= endDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => l.CreditAmount - l.DebitAmount);

            var expenses = await _context.JournalLines
                .Include(l => l.ChartOfAccount)
                .Include(l => l.JournalEntry)
                .Where(l => l.ChartOfAccount.AccountType == "EXPENSE" &&
                           l.LineDate >= startDateUtc &&
                           l.LineDate <= endDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => l.DebitAmount - l.CreditAmount);

            return revenue - expenses;
        }

        private async Task<decimal> GetAccountActivitySumAsync(string accountName, DateTime startDate, DateTime endDate)
        {
            var account = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountName.Contains(accountName));

            if (account == null) return 0;

            return await GetAccountBalanceForPeriodAsync(account.AccountId, startDate, endDate);
        }

        private async Task<decimal> GetAccountBalanceChangeAsync(int accountId, DateTime startDate, DateTime endDate)
        {
            var endBalance = await GetAccountBalanceAsync(accountId, endDate);
            var startBalance = await GetAccountBalanceAsync(accountId, startDate.AddDays(-1));

            return endBalance - startBalance;
        }

        private async Task<decimal> GetAccountBalanceChangeAsync(string accountName, DateTime startDate, DateTime endDate)
        {
            var account = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountName.Contains(accountName));

            if (account == null) return 0;

            return await GetAccountBalanceChangeAsync(account.AccountId, startDate, endDate);
        }

        private DateTime GetFiscalYearStart(DateTime date)
        {
            // Calendar year assumed. Adjust month if your fiscal year differs.
            return new DateTime(date.Year, 1, 1);
        }

        private DateTime ToUtc(DateTime dateTime, bool isEndDate = false)
        {
            // For end-date queries, push to 23:59:59.9999999 so the full day is included
            if (isEndDate && dateTime.TimeOfDay == TimeSpan.Zero)
                dateTime = dateTime.Date.AddDays(1).AddTicks(-1);

            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        }

        #endregion Helper Methods
    }
}
