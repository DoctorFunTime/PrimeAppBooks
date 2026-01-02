using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
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

                // Convert to proper sign based on normal balance
                // For display on Balance Sheet, we want positive values showing the amount
                decimal displayBalance;

                if (account.AccountType == "ASSET")
                {
                    // Assets have DEBIT normal balance, so debit-credit is positive for assets
                    displayBalance = balance;
                }
                else if (account.AccountType == "LIABILITY" || account.AccountType == "EQUITY")
                {
                    // Liabilities & Equity have CREDIT normal balance
                    // We stored debit-credit, so flip it to show credit-debit
                    displayBalance = -balance;
                }
                else
                {
                    displayBalance = balance;
                }

                accountBalances[account.AccountId] = (displayBalance, account);
            }

            // === ASSETS ===
            var assets = accounts.Where(a => a.AccountType == "ASSET").ToList();

            // Current Assets
            foreach (var account in assets.Where(a => a.AccountSubtype == "CURRENT_ASSET"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) && Math.Abs(data.Balance) > 0.01m)
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

            // Fixed Assets (includes intangible)
            foreach (var account in assets.Where(a =>
                a.AccountSubtype == "FIXED_ASSET" || a.AccountSubtype == "INTANGIBLE_ASSET"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) && Math.Abs(data.Balance) > 0.01m)
                {
                    report.FixedAssets.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance
                    });
                    report.TotalFixedAssets += data.Balance;
                }
            }

            report.TotalAssets = report.TotalCurrentAssets + report.TotalFixedAssets;

            // === LIABILITIES ===
            var liabilities = accounts.Where(a => a.AccountType == "LIABILITY").ToList();

            // Current Liabilities
            foreach (var account in liabilities.Where(a => a.AccountSubtype == "CURRENT_LIABILITY"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) && Math.Abs(data.Balance) > 0.01m)
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

            // Long-term Liabilities
            foreach (var account in liabilities.Where(a => a.AccountSubtype == "LONG_TERM_LIABILITY"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) && Math.Abs(data.Balance) > 0.01m)
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

            report.TotalLiabilities = report.TotalCurrentLiabilities + report.TotalLongTermLiabilities;

            // === EQUITY ===
            var equity = accounts.Where(a => a.AccountType == "EQUITY").ToList();

            // Capital accounts (Common Stock, Preferred Stock, APIC)
            foreach (var account in equity.Where(a => a.AccountSubtype == "CAPITAL"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) && Math.Abs(data.Balance) > 0.01m)
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

            // Calculate Net Income for previous years (which should be in Retained Earnings)
            // If books haven't been closed, we need to calculate this manually
            var fiscalYearStart = GetFiscalYearStart(asOfDate);
            var historicalNetIncome = await CalculateNetIncomeAsync(new DateTime(1900, 1, 1), fiscalYearStart.AddDays(-1));
            
            bool retainedEarningsAdded = false;

            // Retained Earnings
            foreach (var account in equity.Where(a => a.AccountSubtype == "RETAINED_EARNINGS"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data))
                {
                    // Even if balance is 0, we might need to show it if we have historical net income to add
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

            // If we have historical net income but no Retained Earnings account was found (or added), add a line for it
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

            // Calculate Net Income for current fiscal year
            // fiscalYearStart is already calculated above
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

            // Dividends (reduces equity)
            foreach (var account in equity.Where(a => a.AccountSubtype == "DIVIDENDS"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) && Math.Abs(data.Balance) > 0.01m)
                {
                    report.Equity.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance // Will be negative, reducing equity
                    });
                    report.TotalEquity += data.Balance;
                }
            }

            // Treasury Stock (reduces equity)
            foreach (var account in equity.Where(a => a.AccountSubtype == "TREASURY_STOCK"))
            {
                if (accountBalances.TryGetValue(account.AccountId, out var data) && Math.Abs(data.Balance) > 0.01m)
                {
                    report.Equity.Add(new AccountLineItem
                    {
                        AccountId = account.AccountId,
                        AccountNumber = account.AccountNumber,
                        AccountName = account.AccountName,
                        AccountType = account.AccountType,
                        AccountSubtype = account.AccountSubtype,
                        NormalBalance = account.NormalBalance,
                        Amount = data.Balance // Will be negative, reducing equity
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
                .Where(a => a.IsActive && (a.AccountType == "REVENUE" || a.AccountType == "EXPENSE"))
                .ToListAsync();

            // Calculate activity for the period
            var accountBalances = new Dictionary<int, decimal>();
            foreach (var account in accounts)
            {
                var balance = await GetAccountBalanceForPeriodAsync(account.AccountId, startDate, endDate);

                // Convert based on account type
                decimal displayBalance;
                if (account.AccountType == "REVENUE")
                {
                    // Revenue has CREDIT normal balance
                    // We stored debit-credit, so flip to show credit-debit (positive revenue)
                    displayBalance = -balance;
                }
                else // EXPENSE
                {
                    // Expenses have DEBIT normal balance
                    // debit-credit is already correct (positive expenses)
                    displayBalance = balance;
                }

                accountBalances[account.AccountId] = displayBalance;
            }

            // === REVENUE ===
            var revenueAccounts = accounts.Where(a => a.AccountType == "REVENUE" &&
                                                      a.AccountSubtype != "CONTRA_REVENUE").ToList();
            foreach (var account in revenueAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) && Math.Abs(balance) > 0.01m)
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

            // Contra Revenue (Sales Returns, Discounts)
            var contraRevenue = accounts.Where(a => a.AccountType == "REVENUE" &&
                                                   a.AccountSubtype == "CONTRA_REVENUE").ToList();
            foreach (var account in contraRevenue)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) && Math.Abs(balance) > 0.01m)
                {
                    // Contra revenue reduces total revenue
                    report.TotalRevenue -= Math.Abs(balance);
                }
            }

            // === COST OF GOODS SOLD ===
            var cogsAccounts = accounts.Where(a => a.AccountType == "EXPENSE" &&
                                                  a.AccountSubtype == "COGS").ToList();
            foreach (var account in cogsAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) && Math.Abs(balance) > 0.01m)
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

            report.GrossProfit = report.TotalRevenue - report.TotalCOGS;

            // === OPERATING EXPENSES ===
            var opeAccounts = accounts.Where(a => a.AccountType == "EXPENSE" &&
                                                  a.AccountSubtype == "OPERATING_EXPENSE").ToList();
            foreach (var account in opeAccounts)
            {
                //if (account.AccountId == 62) _messageBoxService.ShowMessage($"Captured account data: {account.AccountName} : Account type {account.AccountType} Account amount {account.NormalBalance}", "Error", "ErrorOutline");
                if (accountBalances.TryGetValue(account.AccountId, out var balance) && Math.Abs(balance) > 0.01m)
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

            // === OTHER INCOME ===
            var otherIncomeAccounts = accounts.Where(a => a.AccountType == "REVENUE" &&
                                                         a.AccountSubtype == "OTHER_INCOME").ToList();
            foreach (var account in otherIncomeAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) && Math.Abs(balance) > 0.01m)
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

            // === OTHER EXPENSES ===
            var otherExpenseAccounts = accounts.Where(a => a.AccountType == "EXPENSE" &&
                                                          (a.AccountSubtype == "OTHER_EXPENSE" ||
                                                           a.AccountSubtype == "FINANCIAL_EXPENSE" ||
                                                           a.AccountSubtype == "TAX_EXPENSE")).ToList();
            foreach (var account in otherExpenseAccounts)
            {
                if (accountBalances.TryGetValue(account.AccountId, out var balance) && Math.Abs(balance) > 0.01m)
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
            report.NetIncome = report.TotalRevenue - report.TotalCOGS - report.TotalOperatingExpenses +
                             report.TotalOtherIncome - report.TotalOtherExpenses;

            return report;
        }

        #endregion Income Statement

        #region Trial Balance

        public async Task<TrialBalanceData> GenerateTrialBalanceAsync(DateTime asOfDate)
        {
            var asOfDateUtc = ToUtc(asOfDate);

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
                // Get opening balance
                var openingBalance = account.OpeningBalance;

                // Get period activity
                var debitActivity = await _context.JournalLines
                    .Include(l => l.JournalEntry)
                    .Where(l => l.AccountId == account.AccountId &&
                               l.LineDate <= asOfDateUtc &&
                               l.JournalEntry.Status == "POSTED")
                    .SumAsync(l => l.DebitAmount);

                var creditActivity = await _context.JournalLines
                    .Include(l => l.JournalEntry)
                    .Where(l => l.AccountId == account.AccountId &&
                               l.LineDate <= asOfDateUtc &&
                               l.JournalEntry.Status == "POSTED")
                    .SumAsync(l => l.CreditAmount);

                // Calculate total debits and credits including opening balance
                decimal totalDebits = debitActivity;
                decimal totalCredits = creditActivity;

                // Add opening balance to appropriate side
                if (account.NormalBalance == "DEBIT")
                {
                    if (openingBalance >= 0)
                        totalDebits += openingBalance;
                    else
                        totalCredits += Math.Abs(openingBalance);
                }
                else // CREDIT
                {
                    if (openingBalance >= 0)
                        totalCredits += openingBalance;
                    else
                        totalDebits += Math.Abs(openingBalance);
                }

                // Only include if there's activity
                if (Math.Abs(totalDebits) > 0.01m || Math.Abs(totalCredits) > 0.01m)
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

        #region Cash Flow Statement

        public async Task<CashFlowData> GenerateCashFlowAsync(DateTime startDate, DateTime endDate)
        {
            var report = new CashFlowData
            {
                ReportTitle = "Cash Flow Statement",
                StartDate = startDate,
                EndDate = endDate
            };

            // Get cash accounts
            var cashAccounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive &&
                           a.AccountType == "ASSET" &&
                           (a.AccountName.Contains("Cash") || a.AccountName.Contains("Bank")))
                .ToListAsync();

            // Calculate beginning and ending cash
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

            // Start with Net Income
            var netIncome = await CalculateNetIncomeAsync(startDate, endDate);
            report.OperatingActivities.Add(new CashFlowLineItem
            {
                Description = "Net Income",
                Amount = netIncome,
                Category = "OPERATING"
            });

            var operatingTotal = netIncome;

            // Add back non-cash expenses
            var depreciation = await GetAccountActivitySumAsync("Depreciation Expense", startDate, endDate);
            if (Math.Abs(depreciation) > 0.01m)
            {
                report.OperatingActivities.Add(new CashFlowLineItem
                {
                    Description = "Depreciation & Amortization",
                    Amount = depreciation,
                    Category = "OPERATING"
                });
                operatingTotal += depreciation;
            }

            // Changes in working capital
            var arChange = await GetAccountBalanceChangeAsync("Accounts Receivable", startDate, endDate);
            if (Math.Abs(arChange) > 0.01m)
            {
                report.OperatingActivities.Add(new CashFlowLineItem
                {
                    Description = "Change in Accounts Receivable",
                    Amount = -arChange, // Increase in AR decreases cash
                    Category = "OPERATING"
                });
                operatingTotal -= arChange;
            }

            var inventoryChange = await GetAccountBalanceChangeAsync("Inventory", startDate, endDate);
            if (Math.Abs(inventoryChange) > 0.01m)
            {
                report.OperatingActivities.Add(new CashFlowLineItem
                {
                    Description = "Change in Inventory",
                    Amount = -inventoryChange,
                    Category = "OPERATING"
                });
                operatingTotal -= inventoryChange;
            }

            var apChange = await GetAccountBalanceChangeAsync("Accounts Payable", startDate, endDate);
            if (Math.Abs(apChange) > 0.01m)
            {
                report.OperatingActivities.Add(new CashFlowLineItem
                {
                    Description = "Change in Accounts Payable",
                    Amount = apChange, // Increase in AP increases cash
                    Category = "OPERATING"
                });
                operatingTotal += apChange;
            }

            report.NetCashFromOperating = operatingTotal;

            // === INVESTING ACTIVITIES ===
            // (Simplified - in production, analyze fixed asset purchases/sales)

            report.NetCashFromInvesting = report.InvestingActivities.Sum(a => a.Amount);

            // === FINANCING ACTIVITIES ===
            // (Simplified - in production, analyze loan proceeds, repayments, dividends)

            report.NetCashFromFinancing = report.FinancingActivities.Sum(a => a.Amount);

            // Calculate net change
            report.NetChangeInCash = report.NetCashFromOperating +
                                    report.NetCashFromInvesting +
                                    report.NetCashFromFinancing;

            return report;
        }

        #endregion Cash Flow Statement

        #region Helper Methods

        private async Task<decimal> GetAccountBalanceAsync(int accountId, DateTime asOfDate)
        {
            var asOfDateUtc = ToUtc(asOfDate);

            var account = await _context.ChartOfAccounts.FindAsync(accountId);
            var openingBalance = account?.OpeningBalance ?? 0;

            var debitTotal = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                           l.LineDate <= asOfDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => l.DebitAmount);

            var creditTotal = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                           l.LineDate <= asOfDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => l.CreditAmount);

            return openingBalance + (debitTotal - creditTotal);
        }

        private async Task<decimal> GetAccountBalanceForPeriodAsync(int accountId, DateTime startDate, DateTime endDate)
        {
            var startDateUtc = ToUtc(startDate);
            var endDateUtc = ToUtc(endDate);

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
            var endDateUtc = ToUtc(endDate);

            // Revenue (CREDIT normal balance) - we want credit - debit to show positive revenue
            var revenue = await _context.JournalLines
                .Include(l => l.ChartOfAccount)
                .Include(l => l.JournalEntry)
                .Where(l => l.ChartOfAccount.AccountType == "REVENUE" &&
                           l.LineDate >= startDateUtc &&
                           l.LineDate <= endDateUtc &&
                           l.JournalEntry.Status == "POSTED")
                .SumAsync(l => l.CreditAmount - l.DebitAmount);

            // Expenses (DEBIT normal balance) - we want debit - credit to show positive expenses
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
            var startDateUtc = ToUtc(startDate);
            var endDateUtc = ToUtc(endDate);

            var account = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountName.Contains(accountName));

            if (account == null) return 0;

            return await GetAccountBalanceForPeriodAsync(account.AccountId, startDate, endDate);
        }

        private async Task<decimal> GetAccountBalanceChangeAsync(string accountName, DateTime startDate, DateTime endDate)
        {
            var account = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountName.Contains(accountName));

            if (account == null) return 0;

            var endBalance = await GetAccountBalanceAsync(account.AccountId, endDate);
            var startBalance = await GetAccountBalanceAsync(account.AccountId, startDate.AddDays(-1));

            return endBalance - startBalance;
        }

        private DateTime GetFiscalYearStart(DateTime date)
        {
            // Assuming fiscal year = calendar year
            // Adjust if your fiscal year starts on a different month
            return new DateTime(date.Year, 1, 1);
        }

        private DateTime ToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        }

        #endregion Helper Methods
    }
}