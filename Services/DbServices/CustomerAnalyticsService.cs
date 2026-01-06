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
    public class CustomerAnalyticsService
    {
        private readonly AppDbContext _context;

        public CustomerAnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CustomerSummaryMetrics>> GetOverallAnalyticsAsync()
        {
            var today = DateTime.UtcNow;
            
            // 1. Identify AR Account
            var arAccount = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountSubtype == "CURRENT_ASSET" && a.AccountName == "Accounts Receivable");
            if (arAccount == null)
            {
                arAccount = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "1100");
            }
            
            if (arAccount == null) return new List<CustomerSummaryMetrics>();

            var customers = await _context.Customers.ToListAsync();
            var allInvoices = await _context.SalesInvoices
                .Where(i => i.Status == "POSTED")
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
            
            var allJournalLines = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.ContactType == "Customer" && l.JournalEntry.Status == "POSTED")
                .OrderBy(l => l.LineDate)
                .ToListAsync();

            var metrics = new List<CustomerSummaryMetrics>();

            foreach (var customer in customers)
            {
                var customerInvoices = allInvoices.Where(i => i.CustomerId == customer.CustomerId).ToList();
                var customerArLines = allJournalLines.Where(l => l.ContactId == customer.CustomerId && l.AccountId == arAccount.AccountId).ToList();

                // Total Outstanding = Net balance of AR account for this customer
                var totalDebits = customerArLines.Sum(l => l.DebitAmount);
                var totalCredits = customerArLines.Sum(l => l.CreditAmount);
                var totalOutstanding = totalDebits - totalCredits;

                if (totalOutstanding <= 0 && !customerInvoices.Any()) continue;

                var m = new CustomerSummaryMetrics
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    TotalOutstanding = totalOutstanding,
                    TotalInvoicedYTD = customerInvoices.Where(i => i.InvoiceDate.Year == today.Year).Sum(i => i.TotalAmount)
                };

                // FIFO Aging logic
                var aging = CalculateFifoAging(customerInvoices, totalCredits, today);
                m.AgingBuckets = aging.Buckets;
                m.OverdueAmount = aging.OverdueAmount;
                
                // DSO Calculation
                m.AvgDaysToPay = CalculateDSO(customerInvoices, customerArLines);

                metrics.Add(m);
            }

            return metrics.OrderByDescending(x => x.TotalOutstanding).ToList();
        }

        private (List<CustomerAgingBucket> Buckets, decimal OverdueAmount) CalculateFifoAging(List<SalesInvoice> invoices, decimal totalPayments, DateTime today)
        {
            var buckets = new List<CustomerAgingBucket>
            {
                new CustomerAgingBucket { BucketName = "0-30 Days", Amount = 0 },
                new CustomerAgingBucket { BucketName = "31-60 Days", Amount = 0 },
                new CustomerAgingBucket { BucketName = "61-90 Days", Amount = 0 },
                new CustomerAgingBucket { BucketName = "90+ Days", Amount = 0 }
            };

            decimal remainingPayments = totalPayments;
            decimal overdueAmount = 0;

            // Sort invoices by date (oldest first) to apply payments FIFO
            foreach (var inv in invoices.OrderBy(i => i.InvoiceDate))
            {
                decimal unpaidAmount = inv.TotalAmount;
                
                // Apply payments to this invoice
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

        private double CalculateDSO(List<SalesInvoice> invoices, List<JournalLine> arLines)
        {
            var payments = arLines.Where(l => l.CreditAmount > 0).OrderBy(l => l.LineDate).ToList();
            var debits = arLines.Where(l => l.DebitAmount > 0).OrderBy(l => l.LineDate).ToList();

            if (!payments.Any() || !debits.Any()) return 0;

            double totalDays = 0;
            int appliedCount = 0;

            // Very simple FIFO matching for DSO estimation
            var remainingDebits = debits.Select(d => new { Date = d.LineDate, Amount = d.DebitAmount }).ToList();
            
            foreach (var payment in payments)
            {
                decimal pAmt = payment.CreditAmount;
                while (pAmt > 0 && remainingDebits.Any())
                {
                    var firstDebit = remainingDebits[0];
                    decimal apply = Math.Min(pAmt, firstDebit.Amount);
                    
                    totalDays += (payment.LineDate - firstDebit.Date).TotalDays;
                    appliedCount++;

                    pAmt -= apply;
                    if (apply == firstDebit.Amount)
                    {
                        remainingDebits.RemoveAt(0);
                    }
                    else
                    {
                        remainingDebits[0] = new { Date = firstDebit.Date, Amount = firstDebit.Amount - apply };
                    }
                }
            }

            return appliedCount > 0 ? totalDays / appliedCount : 0;
        }

        public async Task<List<PaymentPlan>> GetPaymentPlansAsync(int? customerId = null)
        {
            var query = _context.PaymentPlans.AsQueryable();
            if (customerId.HasValue)
            {
                query = query.Where(p => p.CustomerId == customerId.Value);
            }
            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<List<CollectionFollowup>> GetFollowupHistoryAsync(int customerId)
        {
            return await _context.CollectionFollowups
                .Where(f => f.CustomerId == customerId)
                .OrderByDescending(f => f.FollowupDate)
                .ToListAsync();
        }

        public async Task SavePaymentPlanAsync(PaymentPlan plan)
        {
            if (plan.PaymentPlanId == 0)
                _context.PaymentPlans.Add(plan);
            else
                _context.PaymentPlans.Update(plan);

            await _context.SaveChangesAsync();
        }

        public async Task SaveFollowupAsync(CollectionFollowup followup)
        {
            if (followup.CollectionFollowupId == 0)
                _context.CollectionFollowups.Add(followup);
            else
                _context.CollectionFollowups.Update(followup);

            await _context.SaveChangesAsync();
        }
    }
}
