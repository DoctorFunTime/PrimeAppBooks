using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services.DbServices
{
    public class VendorAnalyticsService
    {
        private readonly AppDbContext _context;

        public VendorAnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VendorSummaryMetrics>> GetOverallAnalyticsAsync()
        {
            var today = DateTime.UtcNow;

            // 1. Identify AP Account
            var apAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountSubtype == "CURRENT_LIABILITY" && a.AccountName == "Accounts Payable");
            if (apAccount == null)
            {
                apAccount = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "2100");
            }

            if (apAccount == null) return new List<VendorSummaryMetrics>();

            var vendors = await _context.Vendors.ToListAsync();
            var allInvoices = await _context.PurchaseInvoices
                .Where(i => i.Status == "POSTED")
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();

            var allJournalLines = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.ContactType == "Vendor" && l.JournalEntry.Status == "POSTED")
                .OrderBy(l => l.LineDate)
                .ToListAsync();

            var metrics = new List<VendorSummaryMetrics>();

            foreach (var vendor in vendors)
            {
                var vendorInvoices = allInvoices.Where(i => i.VendorId == vendor.VendorId).ToList();
                var vendorApLines = allJournalLines.Where(l => l.ContactId == vendor.VendorId && l.AccountId == apAccount.AccountId).ToList();

                // Total Outstanding = Credits - Debits for AP (Liability)
                var totalCredits = vendorApLines.Sum(l => l.CreditAmount);
                var totalDebits = vendorApLines.Sum(l => l.DebitAmount);
                var totalOutstanding = totalCredits - totalDebits;

                if (totalOutstanding <= 0 && !vendorInvoices.Any()) continue;

                var m = new VendorSummaryMetrics
                {
                    VendorId = vendor.VendorId,
                    VendorName = vendor.VendorName,
                    TotalOutstanding = totalOutstanding,
                    TotalPurchasesYTD = vendorInvoices.Where(i => i.InvoiceDate.Year == today.Year).Sum(i => i.TotalAmount)
                };

                // FIFO Aging logic
                // For AP, totalDebits (payments) reduce the oldest Credits (invoices)
                var aging = CalculateFifoAging(vendorInvoices, totalDebits, today);
                m.AgingBuckets = aging.Buckets;
                m.OverdueAmount = aging.OverdueAmount;

                metrics.Add(m);
            }

            return metrics.OrderByDescending(x => x.TotalOutstanding).ToList();
        }

        private (List<VendorAgingBucket> Buckets, decimal OverdueAmount) CalculateFifoAging(List<PurchaseInvoice> invoices, decimal totalPayments, DateTime today)
        {
            var buckets = new List<VendorAgingBucket>
            {
                new VendorAgingBucket { BucketName = "0-30 Days", Amount = 0 },
                new VendorAgingBucket { BucketName = "31-60 Days", Amount = 0 },
                new VendorAgingBucket { BucketName = "61-90 Days", Amount = 0 },
                new VendorAgingBucket { BucketName = "90+ Days", Amount = 0 }
            };

            decimal remainingPayments = totalPayments;
            decimal overdueAmount = 0;

            // Sort invoices by date (oldest first) to apply payments FIFO
            foreach (var inv in invoices.OrderBy(i => i.InvoiceDate))
            {
                decimal unpaidAmount = inv.TotalAmount;

                // Apply payments (debits) to this invoice (credit)
                if (remainingPayments >= unpaidAmount)
                {
                    remainingPayments -= unpaidAmount;
                    unpaidAmount = 0;
                }
                else
                {
                    unpaidAmount -= remainingPayments;
                    remainingPayments = 0;
                }

                if (unpaidAmount > 0)
                {
                    var daysOld = (today - inv.InvoiceDate).Days;
                    if (daysOld <= 30) buckets[0].Amount += unpaidAmount;
                    else if (daysOld <= 60) buckets[1].Amount += unpaidAmount;
                    else if (daysOld <= 90) buckets[2].Amount += unpaidAmount;
                    else buckets[3].Amount += unpaidAmount;

                    if (inv.DueDate < today)
                    {
                        overdueAmount += unpaidAmount;
                    }
                }
            }

            var totalOutstanding = buckets.Sum(b => b.Amount);
            if (totalOutstanding > 0)
            {
                foreach (var b in buckets) b.Percentage = (double)(b.Amount / totalOutstanding * 100);
            }

            return (buckets, overdueAmount);
        }
    }
}
