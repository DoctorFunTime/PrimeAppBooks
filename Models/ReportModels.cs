using System;
using System.Collections.Generic;

namespace PrimeAppBooks.Models
{
    // Base class for all reports
    public class ReportData
    {
        public string ReportTitle { get; set; }
        public string CompanyName { get; set; } = "PrimeAppBooks";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string DateRangeText => $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
    }

    // Balance Sheet Data
    public class BalanceSheetData : ReportData
    {
        public List<AccountLineItem> CurrentAssets { get; set; } = new();
        public List<AccountLineItem> FixedAssets { get; set; } = new();
        public List<AccountLineItem> CurrentLiabilities { get; set; } = new();
        public List<AccountLineItem> LongTermLiabilities { get; set; } = new();
        public List<AccountLineItem> Equity { get; set; } = new();

        public decimal TotalCurrentAssets { get; set; }
        public decimal TotalFixedAssets { get; set; }
        public decimal TotalAssets { get; set; }
        public decimal TotalCurrentLiabilities { get; set; }
        public decimal TotalLongTermLiabilities { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalEquity { get; set; }
        public decimal TotalLiabilitiesAndEquity { get; set; }

        // Validation properties
        public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.01m;

        public decimal BalanceDifference => TotalAssets - TotalLiabilitiesAndEquity;
    }

    // Income Statement Data
    public class IncomeStatementData : ReportData
    {
        public List<AccountLineItem> Revenue { get; set; } = new();
        public List<AccountLineItem> CostOfGoodsSold { get; set; } = new();
        public List<AccountLineItem> OperatingExpenses { get; set; } = new();
        public List<AccountLineItem> OtherIncome { get; set; } = new();
        public List<AccountLineItem> OtherExpenses { get; set; } = new();

        public decimal TotalRevenue { get; set; }
        public decimal TotalCOGS { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal TotalOperatingExpenses { get; set; }
        public decimal OperatingIncome { get; set; }
        public decimal TotalOtherIncome { get; set; }
        public decimal TotalOtherExpenses { get; set; }
        public decimal NetIncome { get; set; }
    }

    // Cash Flow Statement Data
    public class CashFlowData : ReportData
    {
        public List<CashFlowLineItem> OperatingActivities { get; set; } = new();
        public List<CashFlowLineItem> InvestingActivities { get; set; } = new();
        public List<CashFlowLineItem> FinancingActivities { get; set; } = new();

        public decimal NetCashFromOperating { get; set; }
        public decimal NetCashFromInvesting { get; set; }
        public decimal NetCashFromFinancing { get; set; }
        public decimal NetChangeInCash { get; set; }
        public decimal BeginningCashBalance { get; set; }
        public decimal EndingCashBalance { get; set; }

        // Validation property
        public bool IsBalanced => Math.Abs(EndingCashBalance - (BeginningCashBalance + NetChangeInCash)) < 0.01m;
    }

    // Trial Balance Data
    public class TrialBalanceData : ReportData
    {
        public List<TrialBalanceLineItem> Accounts { get; set; } = new();
        public decimal TotalDebits { get; set; }
        public decimal TotalCredits { get; set; }
        public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.01m;
        public decimal BalanceDifference => TotalDebits - TotalCredits;
    }

    // Line item models
    public class AccountLineItem
    {
        // Basic properties (original)
        public string AccountNumber { get; set; }

        public string AccountName { get; set; }
        public decimal Amount { get; set; }
        public int IndentLevel { get; set; } = 0;

        // Extended properties (added for better tracking)
        public int AccountId { get; set; }

        public string AccountType { get; set; }         // ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE
        public string AccountSubtype { get; set; }      // CURRENT_ASSET, FIXED_ASSET, CAPITAL, etc.
        public string NormalBalance { get; set; }       // DEBIT or CREDIT
    }

    public class CashFlowLineItem
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsSubtotal { get; set; }
        public string Category { get; set; }            // OPERATING, INVESTING, FINANCING
    }

    public class TrialBalanceLineItem
    {
        // Basic properties (original)
        public string AccountNumber { get; set; }

        public string AccountName { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }

        // Extended properties (added for better tracking)
        public int AccountId { get; set; }

        public string AccountType { get; set; }
        public string NormalBalance { get; set; }

        // Calculated balance
        public decimal Balance => NormalBalance == "DEBIT"
            ? DebitAmount - CreditAmount
            : CreditAmount - DebitAmount;
    }

    // Recent Report Model (for UI)
    public class RecentReport
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int PageCount { get; set; }
        public string FilePath { get; set; }
        public string ReportType { get; set; }
    }

    // Report Generation Options
    public class ReportOptions
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IncludeZeroBalances { get; set; } = false;
        public bool IncludeInactiveAccounts { get; set; } = false;
        public string Basis { get; set; } = "ACCRUAL";  // ACCRUAL or CASH
        public int? PeriodId { get; set; }
    }
}