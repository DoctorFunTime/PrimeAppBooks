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

        public async Task<CustomerAnalyticsResult> GetOverallAnalyticsAsync()
        {
            var today = DateTime.UtcNow;
            
            // 1. Identify AR Accounts
            var arAccountIds = await _context.ChartOfAccounts
                .Where(a => a.IsActive && (a.AccountSubtype == "Accounts Receivable" || (a.AccountType == "ASSET" && a.AccountName.Contains("Receivable"))))
                .Select(a => a.AccountId)
                .ToListAsync();
            
            var customers = await _context.Customers.Where(c => c.IsActive).ToListAsync();
            var customerIds = customers.Select(c => c.CustomerId).ToList();

            // Bulk Fetch Payment Plans
            var activePlansLookup = (await _context.PaymentPlans
                .Where(p => p.Status == "ACTIVE" && customerIds.Contains(p.CustomerId))
                .Select(p => p.CustomerId)
                .ToListAsync())
                .ToHashSet();

            // Bulk Fetch Latest Follow-ups
            var latestFollowups = await _context.CollectionFollowups
                .Where(f => customerIds.Contains(f.CustomerId))
                .GroupBy(f => f.CustomerId)
                .Select(g => new { CustomerId = g.Key, NextFollowupDate = g.Max(f => f.NextFollowupDate) })
                .ToDictionaryAsync(x => x.CustomerId, x => (DateTime?)x.NextFollowupDate);

            var allInvoices = await _context.SalesInvoices
                .Where(i => i.Status == "POSTED" && customerIds.Contains(i.CustomerId))
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();
            
            var allJournalLines = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.ContactType == "Customer" && l.ContactId.HasValue && customerIds.Contains(l.ContactId.Value) && l.JournalEntry.Status == "POSTED" && arAccountIds.Contains(l.AccountId))
                .OrderBy(l => l.LineDate)
                .ToListAsync();

            var result = new CustomerAnalyticsResult();

            foreach (var customer in customers)
            {
                var customerInvoices = allInvoices.Where(i => i.CustomerId == customer.CustomerId).ToList();
                var customerArLines = allJournalLines.Where(l => l.ContactId == customer.CustomerId).ToList();

                // Total Outstanding = Net balance of AR account for this customer
                var totalDebits = customerArLines.Sum(l => l.DebitAmount);
                var totalCredits = customerArLines.Sum(l => l.CreditAmount);
                var totalOutstanding = totalDebits - totalCredits;

                // Accumulate overall totals for ALL active customers (Net amount)
                result.TotalOutstanding += totalOutstanding;

                if (totalOutstanding <= 0) continue; 

                var m = new CustomerSummaryMetrics
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    CustomerPhone = customer.Phone,
                    GradeLevel = customer.GradeLevel,
                    TotalOutstanding = totalOutstanding,
                    TotalInvoicedYTD = customerInvoices.Where(i => i.InvoiceDate.Year == today.Year).Sum(i => i.TotalAmount)
                };

                // FIFO Aging logic: Combine invoices and any other debits for accurate aging
                var debits = customerInvoices.Select(i => new DebitItem { Date = i.InvoiceDate, DueDate = i.DueDate, Amount = i.TotalAmount }).ToList();
                
                // Add any journal debits that aren't tied to these invoices (e.g. manual journals, debit notes)
                // Since JournalLine doesn't have a direct SalesInvoiceId, we use JournalType as a heuristic.
                // Invoices posted to journal use JournalType = "SALES".
                var otherDebits = customerArLines.Where(l => l.DebitAmount > 0 && l.JournalEntry.JournalType != "SALES")
                    .Select(l => new DebitItem { Date = l.LineDate, DueDate = l.LineDate, Amount = l.DebitAmount });
                
                debits.AddRange(otherDebits);

                var aging = CalculateFifoAging(debits, totalCredits, today);
                m.AgingBuckets = aging.Buckets;
                m.OverdueAmount = aging.OverdueAmount;
                
                result.TotalOverdue += m.OverdueAmount;

                // DSO Calculation
                m.AvgDaysToPay = CalculateDSO(customerInvoices, customerArLines);

                // Optimization: Use lookups instead of DB calls
                m.HasActivePaymentPlan = activePlansLookup.Contains(customer.CustomerId);

                if (latestFollowups.TryGetValue(customer.CustomerId, out var nextFollowup))
                {
                    m.NextFollowupDate = nextFollowup;
                    if (nextFollowup.HasValue)
                    {
                        var daysUntil = (nextFollowup.Value - today).TotalDays;
                        if (daysUntil < 0) m.FollowupUrgency = "Red"; // Overdue
                        else if (daysUntil <= 7) m.FollowupUrgency = "Orange"; // Within a week
                        else m.FollowupUrgency = "Normal";
                    }
                    else
                    {
                        m.FollowupUrgency = "None";
                    }
                }
                else
                {
                    m.FollowupUrgency = "None";
                }

                result.Metrics.Add(m);
            }

            result.Metrics = result.Metrics.OrderByDescending(x => x.TotalOutstanding).ToList();
            return result;
        }

        private struct DebitItem
        {
            public DateTime Date;
            public DateTime DueDate;
            public decimal Amount;
        }

        private (List<CustomerAgingBucket> Buckets, decimal OverdueAmount) CalculateFifoAging(List<DebitItem> debits, decimal totalPayments, DateTime today)
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

            // Sort debits by date (oldest first) to apply payments FIFO
            foreach (var debit in debits.OrderBy(d => d.Date))
            {
                decimal unpaidAmount = debit.Amount;
                
                // Apply payments to this debit
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

                if (unpaidAmount > 0.001m)
                {
                    var daysOld = (today - debit.Date).Days;
                    if (daysOld <= 30) buckets[0].Amount += unpaidAmount;
                    else if (daysOld <= 60) buckets[1].Amount += unpaidAmount;
                    else if (daysOld <= 90) buckets[2].Amount += unpaidAmount;
                    else buckets[3].Amount += unpaidAmount;

                    if (debit.DueDate < today)
                    {
                        overdueAmount += unpaidAmount;
                    }
                }
            }

            var totalCalculatedOutstanding = buckets.Sum(b => b.Amount);
            if (totalCalculatedOutstanding > 0)
            {
                foreach (var b in buckets) b.Percentage = (double)(b.Amount / totalCalculatedOutstanding * 100);
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
            var query = _context.PaymentPlans.Include(p => p.Customer).AsQueryable();
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
        public async Task<(int TotalCustomers, int TotalDebtors, int WrittenOffCustomers)> GetTotalStatsAsync()
        {
            var totalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
            var writtenOffCustomers = await _context.Customers.CountAsync(c => !c.IsActive);
            
            // 1. Identify AR Accounts
            var arAccountIds = await _context.ChartOfAccounts
                .Where(a => a.IsActive && (a.AccountSubtype == "Accounts Receivable" || (a.AccountType == "ASSET" && a.AccountName.Contains("Receivable"))))
                .Select(a => a.AccountId)
                .ToListAsync();

            var activeCustomerIds = await _context.Customers.Where(c => c.IsActive).Select(c => c.CustomerId).ToListAsync();
            
            var totalDebtors = await _context.JournalLines
                 .Where(l => l.JournalEntry.Status == "POSTED" && l.ContactType == "Customer" && l.ContactId.HasValue && activeCustomerIds.Contains(l.ContactId.Value) && arAccountIds.Contains(l.AccountId))
                 .GroupBy(l => l.ContactId)
                 .Select(g => new { CustomerId = g.Key, Balance = g.Sum(l => l.DebitAmount - l.CreditAmount) })
                 .CountAsync(x => x.Balance > 0); // Only include students who owe money (Debit balance)

            return (totalCustomers, totalDebtors, writtenOffCustomers);
        }

        /// <summary>
        /// Get comprehensive master summary data for analytics dashboard
        /// Includes collection rate, DSO, turnover ratio, and chart data
        /// </summary>
        public async Task<MasterSummaryData> GetMasterSummaryDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var today = DateTime.UtcNow;
            
            // Convert incoming dates to UTC to avoid PostgreSQL "Kind=Unspecified" error
            DateTime reportStartDate;
            DateTime reportEndDate;
            
            if (startDate.HasValue)
            {
                // Convert to UTC and set to start of day
                reportStartDate = startDate.Value.Kind == DateTimeKind.Utc 
                    ? startDate.Value.Date
                    : DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            }
            else
            {
                reportStartDate = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            
            if (endDate.HasValue)
            {
                // Ensure the end date is inclusive by setting it to 23:59:59 UTC
                reportEndDate = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            }
            else
            {
                reportEndDate = today;
            }
            
            var startOfYear = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var last12MonthsStart = today.AddMonths(-11);
            last12MonthsStart = new DateTime(last12MonthsStart.Year, last12MonthsStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = new MasterSummaryData();

            // 1. Core Data: Accounts and Customers
            var arAccountIds = await _context.ChartOfAccounts
                .Where(a => a.IsActive && (a.AccountSubtype == "Accounts Receivable" || a.AccountNumber == "1100" || (a.AccountType == "ASSET" && a.AccountName.Contains("Receivable"))))
                .Select(a => a.AccountId)
                .ToListAsync();

            if (!arAccountIds.Any()) return result;

            var arQuery = _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.ContactType == "Customer" && l.ContactId.HasValue && arAccountIds.Contains(l.AccountId) && l.JournalEntry.Status == "POSTED");

            // Get ALL customers who might have balances, not just active ones, 
            // especially for historical data and bad debt reporting
            var customerIds = await _context.Customers
                .Select(c => c.CustomerId)
                .ToListAsync();

            var activeCustomerIds = await _context.Customers
                .Where(c => c.IsActive)
                .Select(c => c.CustomerId)
                .ToListAsync();

            result.TotalActiveCustomers = activeCustomerIds.Count;

            // KPIs - Respect the report end date for historical accuracy
            var arQueryEnding = arQuery.Where(l => l.LineDate <= reportEndDate);
            result.CurrentARBalance = await arQueryEnding.SumAsync(l => l.DebitAmount - l.CreditAmount);
            
            var customerBalances = await arQueryEnding
                .GroupBy(l => l.ContactId)
                .Select(g => new { CustomerId = g.Key, Balance = g.Sum(l => l.DebitAmount - l.CreditAmount) })
                .Where(x => x.Balance > 0)
                .ToListAsync();

            result.TotalDebtors = customerBalances.Count;

            var invoicedFromInvoices = await _context.SalesInvoices
                .Where(i => i.Status == "POSTED" 
                    && i.InvoiceDate >= reportStartDate 
                    && i.InvoiceDate <= reportEndDate 
                    && customerIds.Contains(i.CustomerId))
                .SumAsync(i => i.TotalAmount);

            // Import often creates journals with Type "SALES_INVOICE" or "GENERAL" (for opening balances)
            // We count SALES_INVOICE journals as billings.
            var invoicedFromJournals = await arQuery
                .Where(l => l.LineDate >= reportStartDate 
                    && l.LineDate <= reportEndDate 
                    && l.DebitAmount > 0 
                    && (l.JournalEntry.JournalType == "SALES_INVOICE" || l.JournalEntry.JournalType == "SALES"))
                .SumAsync(l => l.DebitAmount);

            result.TotalInvoicedYTD = invoicedFromInvoices + invoicedFromJournals;

            result.TotalCollectedYTD = await arQuery
                .Where(l => l.LineDate >= reportStartDate 
                    && l.LineDate <= reportEndDate 
                    && l.CreditAmount > 0
                    && l.JournalEntry.JournalType != "GENERAL") // Exclude opening balance offsets
                .SumAsync(l => l.CreditAmount);

            result.CollectionRate = result.TotalInvoicedYTD > 0 ? (result.TotalCollectedYTD / result.TotalInvoicedYTD) * 100 : 0;

            // Annual Revenue should be for the 12 months ending at reportEndDate
            var annualRevenueStart = reportEndDate.AddMonths(-12);
            
            // Calculate Opening AR at the start of the 12-month period for averaging
            var openingAR = await arQuery
                .Where(l => l.LineDate < annualRevenueStart)
                .SumAsync(l => l.DebitAmount - l.CreditAmount);

            var annualRevenue = await _context.JournalLines
                .Where(l => l.JournalEntry.Status == "POSTED" 
                    && l.ChartOfAccount.AccountType == "REVENUE" 
                    && l.LineDate >= annualRevenueStart 
                    && l.LineDate <= reportEndDate)
                .SumAsync(l => l.CreditAmount - l.DebitAmount);

            if (annualRevenue > 0)
            {
                result.ReceivablesTurnover = annualRevenue / (result.CurrentARBalance > 0 ? result.CurrentARBalance : 1);
                
                // Use Average AR (Opening + Closing) / 2 to smooth out fluctuations
                var averageAR = (openingAR + result.CurrentARBalance) / 2;
                result.ARToRevenueRatio = (averageAR / annualRevenue) * 100;
            }

            // Calculate weighted average DSO from individual customer DSOs at reportEndDate
            var customerDSOs = await GetIndividualCustomerDSOsAsync(activeCustomerIds, arAccountIds, reportEndDate);
            if (customerDSOs.Any())
            {
                result.AverageDSO = (decimal)customerDSOs
                    .Where(c => c.Outstanding > 0)
                    .Select(c => c.DSO)
                    .DefaultIfEmpty(0)
                    .Average();
            }

            // 3. Bad Debt - Query actual bad debt expense entries
            var badDebtAccounts = await _context.ChartOfAccounts
                .Where(a => a.IsActive && (a.AccountName.Contains("Bad Debt") || a.AccountName.Contains("Write") || a.AccountName.Contains("Doubtful")))
                .Select(a => a.AccountId)
                .ToListAsync();

            if (badDebtAccounts.Any())
            {
                result.TotalWrittenOff = await _context.JournalLines
                    .Where(l => l.JournalEntry.Status == "POSTED" 
                        && badDebtAccounts.Contains(l.AccountId) 
                        && l.LineDate >= reportStartDate 
                        && l.LineDate <= reportEndDate)
                    .SumAsync(l => l.DebitAmount - l.CreditAmount);
            }
            
            result.BadDebtRatio = result.TotalInvoicedYTD > 0 ? (result.TotalWrittenOff / result.TotalInvoicedYTD) * 100 : 0;

            // 4. Monthly Trends - Aggregate on Server (use custom date range)
            var revTrends = await _context.JournalLines
                .Where(l => l.JournalEntry.Status == "POSTED" && l.ChartOfAccount.AccountType == "REVENUE" && l.LineDate >= reportStartDate && l.LineDate <= reportEndDate)
                .GroupBy(l => new { l.LineDate.Year, l.LineDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(l => l.CreditAmount - l.DebitAmount) })
                .ToListAsync();

            var collTrends = await arQuery
                .Where(l => l.LineDate >= reportStartDate && l.LineDate <= reportEndDate && l.CreditAmount > 0)
                .GroupBy(l => new { l.LineDate.Year, l.LineDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Collections = g.Sum(l => l.CreditAmount) })
                .ToListAsync();

            var arChangeTrends = await arQuery
                .GroupBy(l => new { l.LineDate.Year, l.LineDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, NetChange = g.Sum(l => l.DebitAmount - l.CreditAmount) })
                .ToListAsync();

            result.MonthlyTrends = new List<MonthlyTrendPoint>();
            var historicalAR = result.CurrentARBalance;

            for (int i = 0; i < 12; i++)
            {
                var mStart = reportEndDate.AddMonths(-i);
                mStart = new DateTime(mStart.Year, mStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                
                var rev = revTrends.FirstOrDefault(x => x.Year == mStart.Year && x.Month == mStart.Month)?.Revenue ?? 0;
                var coll = collTrends.FirstOrDefault(x => x.Year == mStart.Year && x.Month == mStart.Month)?.Collections ?? 0;
                
                // Balance at end of mStart = historicalAR (at reportEndDate) - (changes occurring after mStart but before reportEndDate)
                var changesAfter = arChangeTrends
                    .Where(x => {
                        var dt = new DateTime(x.Year, x.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        return dt > mStart && dt <= reportEndDate;
                    })
                    .Sum(x => x.NetChange);
                    
                var balanceAtEnd = historicalAR - changesAfter;

                result.MonthlyTrends.Add(new MonthlyTrendPoint
                {
                    Month = mStart.ToString("MMM yyyy"),
                    StartDate = mStart,
                    Revenue = rev,
                    Collections = coll,
                    ARBalance = balanceAtEnd
                });
            }
            result.MonthlyTrends = result.MonthlyTrends
                .Where(t => t.Revenue != 0 || t.Collections != 0)
                .OrderBy(t => t.StartDate)
                .ToList();

            // 5. Aging Distribution - Use Journal Lines for more robust results (matches CurrentARBalance source)
            var allArLines = await arQuery.Where(l => l.LineDate <= reportEndDate).Select(l => new { l.LineDate, l.DebitAmount, l.CreditAmount }).ToListAsync();
            
            var allDebits = allArLines.Where(l => l.DebitAmount > 0)
                .Select(l => new DebitItem { Date = l.LineDate, Amount = l.DebitAmount })
                .OrderBy(d => d.Date)
                .ToList();
            
            var totalArPayments = allArLines.Sum(l => l.CreditAmount);
            var agingResults = CalculateFifoAging(allDebits, totalArPayments, reportEndDate);
            
            result.AgingDistribution = new List<AgingDistributionBucket>();
            var totalBucketAmount = agingResults.Buckets.Sum(b => b.Amount);
            
            if (totalBucketAmount > 0)
            {
                foreach (var bucket in agingResults.Buckets)
                {
                    result.AgingDistribution.Add(new AgingDistributionBucket 
                    { 
                        Label = bucket.BucketName, 
                        Amount = bucket.Amount, 
                        Percentage = (decimal)(bucket.Amount / totalBucketAmount * 100) 
                    });
                }
            }
            else if (result.CurrentARBalance > 0)
            {
                // Fallback: If we have a balance but no clear debits (unlikely but possible with weird data), 
                // put everything in the oldest bucket to ensure the report shows it.
                result.AgingDistribution.Add(new AgingDistributionBucket { Label = "90+ Days", Amount = result.CurrentARBalance, Percentage = 100 });
            }

            // 6. Top 10 Debtors
            var top10Bal = customerBalances.OrderByDescending(x => x.Balance).Take(10).ToList();
            var top10Ids = top10Bal.Select(x => x.CustomerId).Where(id => id.HasValue).Select(id => id.Value).ToList();
            var top10Names = await _context.Customers.Where(c => top10Ids.Contains(c.CustomerId)).ToDictionaryAsync(c => c.CustomerId, c => c.CustomerName);
            
            result.TopDebtors = top10Bal.Select(x => new TopDebtorItem
            {
                CustomerId = x.CustomerId ?? 0,
                CustomerName = x.CustomerId.HasValue && top10Names.TryGetValue(x.CustomerId.Value, out var name) ? name : "Unknown",
                OutstandingAmount = x.Balance
            }).ToList();

            // 7. On-Time Payment Rate - Check if paid before due date (use custom date range)
            var invoicesForOnTimeCheck = await _context.SalesInvoices
                .Where(i => i.Status == "POSTED" && i.InvoiceDate >= reportStartDate && i.InvoiceDate <= reportEndDate && i.Balance == 0 && activeCustomerIds.Contains(i.CustomerId))
                .Select(i => new { i.SalesInvoiceId, i.CustomerId, i.DueDate, i.TotalAmount, i.InvoiceDate })
                .ToListAsync();

            var allPayments = await arQuery
                .Where(l => l.CreditAmount > 0 && l.LineDate >= reportStartDate && l.LineDate <= reportEndDate)
                .Select(l => new { l.ContactId, l.LineDate, l.CreditAmount })
                .ToListAsync();

            int onTimeCount = 0;
            foreach (var invoice in invoicesForOnTimeCheck)
            {
                // Get payments for this customer after invoice date
                var customerPayments = allPayments
                    .Where(p => p.ContactId == invoice.CustomerId && p.LineDate >= invoice.InvoiceDate)
                    .OrderBy(p => p.LineDate)
                    .ToList();

                if (customerPayments.Any())
                {
                    // Simple heuristic: if first payment was before due date, count as on-time
                    var firstPayment = customerPayments.First();
                    if (firstPayment.LineDate <= invoice.DueDate)
                    {
                        onTimeCount++;
                    }
                }
            }

            result.OnTimePaymentRate = invoicesForOnTimeCheck.Any() ? (decimal)onTimeCount / invoicesForOnTimeCheck.Count * 100 : 0;

            result.CollectionTrend = result.MonthlyTrends.Select(t => new MonthlyCollectionPoint
            {
                Month = t.Month,
                Invoiced = t.Revenue,
                Collected = t.Collections,
                CollectionRate = t.Revenue > 0 ? (t.Collections/t.Revenue)*100 : 0
            }).ToList();

            // 8. Payment Timing Statistics - When during the month do payments occur? (use custom date range)
            var paymentsWithTiming = await arQuery
                .Where(l => l.CreditAmount > 0 && l.LineDate >= reportStartDate && l.LineDate <= reportEndDate)
                .Select(l => new { l.LineDate, l.CreditAmount })
                .ToListAsync();

            if (paymentsWithTiming.Any())
            {
                var count1to10 = paymentsWithTiming.Count(p => p.LineDate.Day <= 10);
                var count11to20 = paymentsWithTiming.Count(p => p.LineDate.Day >= 11 && p.LineDate.Day <= 20);
                var count21toEnd = paymentsWithTiming.Count(p => p.LineDate.Day >= 21);

                result.PaymentTiming.TotalPayments = paymentsWithTiming.Count;
                result.PaymentTiming.Percent1to10 = (decimal)count1to10 / paymentsWithTiming.Count * 100;
                result.PaymentTiming.Percent11to20 = (decimal)count11to20 / paymentsWithTiming.Count * 100;
                result.PaymentTiming.Percent21toEnd = (decimal)count21toEnd / paymentsWithTiming.Count * 100;
                result.PaymentTiming.AveragePaymentDay = (decimal)paymentsWithTiming.Average(p => p.LineDate.Day);
            }

            // 9. Student-Specific Analytics
            // Average balance per student (only debtors)
            result.StudentStats.AverageBalancePerStudent = result.TotalDebtors > 0 ? result.CurrentARBalance / result.TotalDebtors : 0;


            // Students with consistent payments (3+ payments in last 3 months ending at reportEndDate)
            var threeMonthsAgo = reportEndDate.AddMonths(-3);
            var consistentPayers = await arQuery
                .Where(l => l.CreditAmount > 0 && l.LineDate >= threeMonthsAgo && l.ContactId.HasValue)
                .GroupBy(l => l.ContactId.Value)
                .Select(g => new { CustomerId = g.Key, PaymentCount = g.Count() })
                .Where(x => x.PaymentCount >= 3)
                .CountAsync();

            result.StudentStats.StudentsWithConsistentPayments = consistentPayers;

            // Students at risk (have outstanding balance but no payment in 60+ days as of reportEndDate)
            var sixtyDaysAgo = reportEndDate.AddDays(-60);
            var recentPayerIds = await arQuery
                .Where(l => l.CreditAmount > 0 && l.LineDate >= sixtyDaysAgo && l.ContactId.HasValue)
                .Select(l => l.ContactId.Value)
                .Distinct()
                .ToListAsync();

            var debtorIds = customerBalances.Where(cb => cb.CustomerId.HasValue).Select(cb => cb.CustomerId.Value).ToList();
            result.StudentStats.StudentsAtRisk = debtorIds.Count(id => !recentPayerIds.Contains(id));

            // Percent paid in full (customers with zero balance)
            var studentsWithZeroBalance = activeCustomerIds.Count - result.TotalDebtors;
            result.StudentStats.PercentPaidInFull = result.TotalActiveCustomers > 0 
                ? (decimal)studentsWithZeroBalance / result.TotalActiveCustomers * 100 
                : 0;

            // Average days to first payment for invoices created in date range
            var invoicesThisYear = await _context.SalesInvoices
                .Where(i => i.Status == "POSTED" && i.InvoiceDate >= reportStartDate && i.InvoiceDate <= reportEndDate && activeCustomerIds.Contains(i.CustomerId))
                .Select(i => new { i.SalesInvoiceId, i.CustomerId, i.InvoiceDate })
                .ToListAsync();

            if (invoicesThisYear.Any())
            {
                var daysToFirstPaymentList = new List<int>();
                foreach (var inv in invoicesThisYear)
                {
                    var firstPayment = await arQuery
                        .Where(l => l.ContactId == inv.CustomerId && l.CreditAmount > 0 && l.LineDate >= inv.InvoiceDate)
                        .OrderBy(l => l.LineDate)
                        .FirstOrDefaultAsync();

                    if (firstPayment != null)
                    {
                        var daysToPayment = (firstPayment.LineDate - inv.InvoiceDate).Days;
                        daysToFirstPaymentList.Add(daysToPayment);
                    }
                }

                result.StudentStats.AverageDaysToFirstPayment = daysToFirstPaymentList.Any() 
                    ? (decimal)daysToFirstPaymentList.Average() 
                    : 0;
            }

            // Set report date range for display
            result.ReportStartDate = reportStartDate;
            result.ReportEndDate = reportEndDate;

            return result;
        }

        /// <summary>
        /// Helper method to calculate individual customer DSOs for accurate averaging
        /// </summary>
        private async Task<List<(int CustomerId, double DSO, decimal Outstanding)>> GetIndividualCustomerDSOsAsync(List<int> customerIds, List<int> arAccountIds, DateTime endDate)
        {
            var result = new List<(int, double, decimal)>();

            var customerArLines = await _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.ContactType == "Customer" && l.ContactId.HasValue && customerIds.Contains(l.ContactId.Value) && l.JournalEntry.Status == "POSTED" && arAccountIds.Contains(l.AccountId) && l.LineDate <= endDate)
                .GroupBy(l => l.ContactId.Value)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    Lines = g.OrderBy(l => l.LineDate).ToList()
                })
                .ToListAsync();

            foreach (var customer in customerArLines)
            {
                var lines = customer.Lines;
                var debits = lines.Where(l => l.DebitAmount > 0).OrderBy(l => l.LineDate).ToList();
                var credits = lines.Where(l => l.CreditAmount > 0).OrderBy(l => l.LineDate).ToList();
                
                var outstanding = lines.Sum(l => l.DebitAmount - l.CreditAmount);
                var dso = CalculateDSO(new List<SalesInvoice>(), lines);

                result.Add((customer.CustomerId, dso, outstanding));
            }

            return result;
        }
    }
}
