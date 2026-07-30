using System;
using System.Collections.Generic;

namespace PrimeAppBooks.Models
{
    /// <summary>
    /// Comprehensive analytics summary data for Master Report
    /// </summary>
    public class MasterSummaryData
    {
        // Report Metadata
        public DateTime ReportStartDate { get; set; }
        public DateTime ReportEndDate { get; set; }
        
        // Core KPI Metrics
        public decimal CollectionRate { get; set; } // Percentage of billed amount collected
        public decimal AverageDSO { get; set; } // Days Sales Outstanding
        public decimal ReceivablesTurnover { get; set; } // Annual Revenue / Average AR
        public decimal BadDebtRatio { get; set; } // Written-off / Total Invoiced
        public decimal ARToRevenueRatio { get; set; } // Current AR / Annual Revenue
        public decimal OnTimePaymentRate { get; set; } // % of invoices paid before due date

        // Trend Data
        public List<MonthlyTrendPoint> MonthlyTrends { get; set; } = new();
        public List<AgingDistributionBucket> AgingDistribution { get; set; } = new();
        public List<TopDebtorItem> TopDebtors { get; set; } = new();
        public List<MonthlyCollectionPoint> CollectionTrend { get; set; } = new();
        
        // Summary Stats
        public decimal TotalInvoicedYTD { get; set; }
        public decimal TotalCollectedYTD { get; set; }
        public decimal CurrentARBalance { get; set; }
        public decimal TotalWrittenOff { get; set; }
        public int TotalActiveCustomers { get; set; }
        public int TotalDebtors { get; set; }

        // New Analytics
        public PaymentTimingStats PaymentTiming { get; set; } = new();
        public StudentSpecificStats StudentStats { get; set; } = new();
    }

    /// <summary>
    /// Monthly trend point for line charts (Revenue, Collections, AR)
    /// </summary>
    public class MonthlyTrendPoint
    {
        public string Month { get; set; } // e.g., "Jan 2024"
        public DateTime StartDate { get; set; }
        public decimal Revenue { get; set; }
        public decimal Collections { get; set; }
        public decimal ARBalance { get; set; }
        public decimal Invoiced { get; set; }
    }

    /// <summary>
    /// Aging bucket for pie/donut chart distribution
    /// </summary>
    public class AgingDistributionBucket
    {
        public string Label { get; set; } // "Current", "1-30 Days", "31-60 Days", etc.
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public int DaysMin { get; set; }
        public int? DaysMax { get; set; }
    }

    /// <summary>
    /// Top debtor item for horizontal bar chart
    /// </summary>
    public class TopDebtorItem
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal OutstandingAmount { get; set; }
        public decimal OverdueAmount { get; set; }
        public int DaysPastDue { get; set; }
    }

    /// <summary>
    /// Monthly collection vs invoiced for stacked area chart
    /// </summary>
    public class MonthlyCollectionPoint
    {
        public string Month { get; set; }
        public DateTime StartDate { get; set; }
        public decimal Invoiced { get; set; }
        public decimal Collected { get; set; }
        public decimal CollectionRate { get; set; } // Percentage
    }

    /// <summary>
    /// Payment timing statistics - when during the month do students pay?
    /// </summary>
    public class PaymentTimingStats
    {
        public decimal Percent1to10 { get; set; } // 1st-10th of month
        public decimal Percent11to20 { get; set; } // 11th-20th of month
        public decimal Percent21toEnd { get; set; } // 21st-end of month
        public int TotalPayments { get; set; }
        public decimal AveragePaymentDay { get; set; } // Average day of month
    }

    /// <summary>
    /// Student-specific receivables analytics
    /// </summary>
    public class StudentSpecificStats
    {
        public decimal AverageBalancePerStudent { get; set; }
        public int StudentsWithConsistentPayments { get; set; } // Paid last 3 months consistently
        public int StudentsAtRisk { get; set; } // No payment in 60+ days
        public decimal PercentPaidInFull { get; set; } // % of students with zero balance
        public decimal AverageDaysToFirstPayment { get; set; } // For invoices created this year
    }
}
