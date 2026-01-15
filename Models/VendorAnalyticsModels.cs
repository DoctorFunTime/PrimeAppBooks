using System;
using System.Collections.Generic;
using PrimeAppBooks.Models;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Models
{
    public class VendorAgingBucket
    {
        public string BucketName { get; set; } // 0-30, 31-60, 61-90, 90+
        public decimal Amount { get; set; }
        public double Percentage { get; set; }
    }

    public class VendorSummaryMetrics
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal OverdueAmount { get; set; }
        public double AvgDaysToPay { get; set; }
        public decimal TotalPurchasesYTD { get; set; }
        public decimal TotalPaidYTD { get; set; }
        public List<VendorAgingBucket> AgingBuckets { get; set; } = new();
    }
}
