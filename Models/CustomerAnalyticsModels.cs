using System;
using System.Collections.Generic;
using PrimeAppBooks.Models;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Models
{
    public class PaymentPlan
    {
        public int PaymentPlanId { get; set; }
        public int CustomerId { get; set; }
        public int? SalesInvoiceId { get; set; }
        public string PlanName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal MonthlyInstallment { get; set; }
        public int NumberOfInstallments { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED, CANCELLED
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Customer Customer { get; set; }
        public SalesInvoice SalesInvoice { get; set; }
    }

    public class CustomerAgingBucket
    {
        public string BucketName { get; set; } // 0-30, 31-60, 61-90, 90+
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }

    public class CustomerSummaryMetrics
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }
        public double AvgDaysToPay { get; set; }
        public decimal TotalInvoicedYTD { get; set; }
        public decimal TotalPaidYTD { get; set; }
        public List<CustomerAgingBucket> AgingBuckets { get; set; } = new();
    }

    public class CollectionFollowup
    {
        public int CollectionFollowupId { get; set; }
        public int CustomerId { get; set; }
        public DateTime FollowupDate { get; set; }
        public string Method { get; set; } // Phone, Email, SMS, Visit
        public string Outcome { get; set; }
        public string Notes { get; set; }
        public DateTime NextFollowupDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Customer Customer { get; set; }
    }
}
