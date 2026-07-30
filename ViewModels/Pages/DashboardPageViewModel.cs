using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class DashboardPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly AppDbContext _context;
        private readonly ChartOfAccountsServices _coaService;
        private readonly JournalServices _journalService;
        private static readonly System.Threading.SemaphoreSlim _dbLock = new(1, 1);

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _loadingMessage;

        [ObservableProperty]
        private decimal _cashBalance;

        [ObservableProperty]
        private decimal _receivables;

        [ObservableProperty]
        private decimal _payables;

        [ObservableProperty]
        private decimal _netIncome;

        [ObservableProperty]
        private decimal _monthlyRevenue;

        [ObservableProperty]
        private decimal _monthlyExpenses;

        [ObservableProperty]
        private string _profitMargin;

        [ObservableProperty] private string _cashBalanceTag = "Updating...";
        [ObservableProperty] private string _receivablesTag = "Updating...";
        [ObservableProperty] private string _payablesTag = "Updating...";
        [ObservableProperty] private string _netIncomeTag = "Updating...";
        [ObservableProperty] private string _monthlyRevenueTag = "Updating...";
        [ObservableProperty] private string _monthlyExpensesTag = "Updating...";
        [ObservableProperty] private string _profitMarginTag = "Updating...";

        public SeriesCollection RevenueSeries { get; set; } = new();
        public SeriesCollection ExpensesSeries { get; set; } = new();
        public SeriesCollection CashFlowSeries { get; set; } = new();
        public string[] Months { get; set; }
        public string[] ProjectionMonths { get; set; }

        public ObservableCollection<object> RecentActivities { get; } = new();
        public ObservableCollection<object> OverdueItems { get; } = new();
        public ObservableCollection<object> UpcomingDueItems { get; } = new();
        public ObservableCollection<object> TopExpenses { get; } = new();


        [ObservableProperty]
        private string _welcomeMessage = "Welcome back!";

        public DashboardPageViewModel(
            INavigationService navigationService,
            ChartOfAccountsServices coaService,
            AppDbContext context,
            JournalServices journalService)
        {
            _navigationService = navigationService;
            _coaService = coaService;
            _journalService = journalService;
            _context = context;

            UpdateWelcomeMessage();

            MyAppContext.StaticPropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MyAppContext.CurrentLogin))
                {
                    UpdateWelcomeMessage();
                }
            };

            // Re-initialize dashboard asynchronously but safely
            _ = Task.Run(async () => await InitializeDashboardAsync());
        }

        private void UpdateWelcomeMessage()
        {
            var user = MyAppContext.CurrentLogin;
            string name = !string.IsNullOrWhiteSpace(user?.AccountName) ? user.AccountName : (!string.IsNullOrWhiteSpace(user?.Username) ? user.Username : "User");
            WelcomeMessage = $"Welcome back, {name}!";
        }

        private async Task InitializeDashboardAsync()
        {
            await LoadDashboardDataAsync();
        }


        [RelayCommand]
        public async Task LoadDashboardDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            LoadingMessage = "Updating dashboard...";
            await _dbLock.WaitAsync();
            try
            {
                await LoadDashboardDataInternalAsync();
            }
            finally
            {
                IsLoading = false;
                _dbLock.Release();
            }
        }

        private async Task LoadDashboardDataInternalAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfCurrentMonth = startOfCurrentMonth.AddMonths(1).AddTicks(-1);
                var startOfPrevMonth = startOfCurrentMonth.AddMonths(-1);
                var endOfPrevMonth = startOfCurrentMonth.AddTicks(-1);
                var twelveMonthsAgo = startOfCurrentMonth.AddMonths(-11);

                // 1. Fetch KPI Balances by Subtype/Type
                var accounts = await _coaService.GetAllAccountsAsync();
                var activeCustomerIds = await _context.Customers.AsNoTracking().Where(c => c.IsActive).Select(c => (int?)c.CustomerId).ToListAsync();
                var activeVendorIds = await _context.Vendors.AsNoTracking().Where(v => v.IsActive).Select(v => (int?)v.VendorId).ToListAsync();

                // Assets & Expenses: Normal balance is DEBIT (Debit - Credit)
                CashBalance = accounts
                    .Where(a => a.AccountSubtype == "Cash" || a.AccountName.Contains("Cash"))
                    .Sum(a => a.CurrentBalance);

                var arAccountIds = accounts
                    .Where(a => a.AccountSubtype == "Accounts Receivable" || a.AccountType == "ASSET" && a.AccountName.Contains("Receivable"))
                    .Select(a => a.AccountId)
                    .ToList();

                Receivables = await _context.JournalLines
                    .AsNoTracking()
                    .Where(l => l.JournalEntry.Status == "POSTED" && arAccountIds.Contains(l.AccountId))
                    .Where(l => l.ContactId.HasValue && activeCustomerIds.Contains(l.ContactId))
                    .SumAsync(l => l.DebitAmount - l.CreditAmount);
                Receivables = Math.Max(0, Receivables);

                // Liabilities, Equity, Revenue: Normal balance is CREDIT (Credit - Debit)
                var apAccountIds = accounts
                    .Where(a => a.AccountSubtype == "Accounts Payable" || a.AccountType == "LIABILITY" && a.AccountName.Contains("Payable"))
                    .Select(a => a.AccountId)
                    .ToList();

                Payables = await _context.JournalLines
                    .AsNoTracking()
                    .Where(l => l.JournalEntry.Status == "POSTED" && apAccountIds.Contains(l.AccountId))
                    .Where(l => l.ContactId.HasValue && activeVendorIds.Contains(l.ContactId))
                    .SumAsync(l => l.CreditAmount - l.DebitAmount);
                Payables = Math.Max(0, Payables);

                // 2. Fetch 12-Month Journal Lines in a single query
                var allLines12Months = await _context.JournalLines
                    .AsNoTracking()
                    .Include(l => l.JournalEntry)
                    .Include(l => l.ChartOfAccount)
                    .Where(l => l.JournalEntry.Status == "POSTED" &&
                                l.LineDate >= twelveMonthsAgo &&
                                l.LineDate <= endOfCurrentMonth &&
                                (l.ChartOfAccount.AccountType == "REVENUE" || l.ChartOfAccount.AccountType == "EXPENSE"))
                    .ToListAsync();

                var currentLines = allLines12Months
                    .Where(l => l.LineDate >= startOfCurrentMonth && l.LineDate <= endOfCurrentMonth)
                    .ToList();
                var prevLines = allLines12Months
                    .Where(l => l.LineDate >= startOfPrevMonth && l.LineDate <= endOfPrevMonth)
                    .ToList();

                MonthlyRevenue = currentLines
                    .Where(l => l.ChartOfAccount?.AccountType == "REVENUE")
                    .Where(l => l.ContactType != "Customer" || (l.ContactId.HasValue && activeCustomerIds.Contains(l.ContactId.Value)))
                    .Sum(l => l.CreditAmount - l.DebitAmount);
                MonthlyRevenue = Math.Max(0, MonthlyRevenue);

                var prevRevenue = prevLines
                    .Where(l => l.ChartOfAccount?.AccountType == "REVENUE")
                    .Where(l => l.ContactType != "Customer" || (l.ContactId.HasValue && activeCustomerIds.Contains(l.ContactId.Value)))
                    .Sum(l => l.CreditAmount - l.DebitAmount);
                prevRevenue = Math.Max(0, prevRevenue);

                MonthlyExpenses = currentLines
                    .Where(l => l.ChartOfAccount?.AccountType == "EXPENSE")
                    .Where(l => l.ContactType != "Vendor" || (l.ContactId.HasValue && activeVendorIds.Contains(l.ContactId.Value)))
                    .Sum(l => l.DebitAmount - l.CreditAmount);
                MonthlyExpenses = Math.Max(0, MonthlyExpenses);

                var prevExpenses = prevLines
                    .Where(l => l.ChartOfAccount?.AccountType == "EXPENSE")
                    .Where(l => l.ContactType != "Vendor" || (l.ContactId.HasValue && activeVendorIds.Contains(l.ContactId.Value)))
                    .Sum(l => l.DebitAmount - l.CreditAmount);
                prevExpenses = Math.Max(0, prevExpenses);

                NetIncome = MonthlyRevenue - MonthlyExpenses;

                // Calculate Tags
                MonthlyRevenueTag = GetComparisonTag(MonthlyRevenue, prevRevenue, true);
                MonthlyExpensesTag = GetComparisonTag(MonthlyExpenses, prevExpenses, false);
                NetIncomeTag = $"Current Month: {startOfCurrentMonth:MMM yyyy}";

                if (MonthlyRevenue > 0)
                {
                    var margin = (NetIncome / MonthlyRevenue) * 100;
                    ProfitMargin = $"{margin:F1}%";
                    ProfitMarginTag = margin >= 30 ? "Healthy (Above 30% target)" : "Action Required (Below target)";
                }
                else
                {
                    ProfitMargin = "0.0%";
                    ProfitMarginTag = "No revenue recorded";
                }

                // Cash Balance Tag (Comparison vs last month)
                var prevCashBalance = await _journalService.GetAccountBalanceAsync(
                    accounts.FirstOrDefault(a => a.AccountName == "Cash")?.AccountId ?? 0,
                    endOfPrevMonth);
                CashBalanceTag = GetComparisonTag(CashBalance, prevCashBalance, true);

                // 3. Load Recent Activity (Last 10 posted transactions)
                var recentPosted = await _context.JournalEntries
                    .AsNoTracking()
                    .Where(j => j.Status == "POSTED")
                    .OrderByDescending(j => j.PostedAt)
                    .Take(10)
                    .ToListAsync();

                // 4. Load Overdue and Upcoming Invoices
                var today = DateTime.UtcNow;
                var overdueInvoices = await _context.SalesInvoices
                    .AsNoTracking()
                    .Include(i => i.Customer)
                    .Where(i => i.Status != "VOID" && i.Balance > 0 && i.DueDate < today)
                    .Where(i => i.Customer == null || i.Customer.IsActive)
                    .OrderByDescending(i => i.Balance)
                    .ToListAsync();

                var upcomingDue = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Include(i => i.Vendor)
                    .Where(i => i.Status != "VOID" && i.Balance > 0 && i.DueDate >= today && i.DueDate <= today.AddDays(7))
                    .Where(i => i.Vendor == null || i.Vendor.IsActive)
                    .OrderBy(i => i.DueDate)
                    .ToListAsync();

                ReceivablesTag = $"{overdueInvoices.Count} invoices overdue";
                PayablesTag = $"{upcomingDue.Count} bills due next 7 days";

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RecentActivities.Clear();
                    foreach (var entry in recentPosted)
                    {
                        RecentActivities.Add(new
                        {
                            Type = entry.JournalType,
                            Description = entry.Description,
                            Amount = entry.Amount.ToString("C"),
                            Time = entry.PostedAt?.ToString("g") ?? "N/A"
                        });
                    }

                    OverdueItems.Clear();
                    foreach (var inv in overdueInvoices.Take(3))
                    {
                        OverdueItems.Add(new
                        {
                            Description = $"{inv.InvoiceNumber} - {inv.Customer?.CustomerName ?? "Customer"}",
                            Amount = inv.Balance.ToString("C")
                        });
                    }

                    UpcomingDueItems.Clear();
                    foreach (var bill in upcomingDue.Take(3))
                    {
                        UpcomingDueItems.Add(new
                        {
                            Description = $"{bill.InvoiceNumber} - {bill.Vendor?.VendorName ?? "Vendor"}",
                            Amount = bill.Balance.ToString("C")
                        });
                    }
                });

                await UpdateChartsAsync(allLines12Months, now);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }

        private async Task UpdateChartsAsync(List<Models.Pages.TransactionsModels.JournalLine> allLines12Months, DateTime now)
        {
            try
            {
                var monthLabels = new List<string>();
                var revenueValues = new ChartValues<decimal>();
                var expenseValues = new ChartValues<decimal>();

                for (int i = 11; i >= 0; i--)
                {
                    var date = now.AddMonths(-i);
                    var monthStart = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                    monthLabels.Add(date.ToString("MMM"));

                    var linesForMonth = allLines12Months
                        .Where(l => l.LineDate >= monthStart && l.LineDate <= monthEnd)
                        .ToList();

                    var rev = linesForMonth
                        .Where(l => l.ChartOfAccount?.AccountType == "REVENUE")
                        .Sum(l => l.CreditAmount - l.DebitAmount);

                    var exp = linesForMonth
                        .Where(l => l.ChartOfAccount?.AccountType == "EXPENSE")
                        .Sum(l => l.DebitAmount - l.CreditAmount);

                    revenueValues.Add(Math.Max(0, rev));
                    expenseValues.Add(Math.Max(0, exp));
                }

                var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddTicks(-1);

                var topExpenses = allLines12Months
                    .Where(l => l.LineDate >= currentMonthStart && l.LineDate <= currentMonthEnd && l.ChartOfAccount?.AccountType == "EXPENSE")
                    .GroupBy(l => l.ChartOfAccount?.AccountName ?? "Unknown")
                    .Select(g => new { Name = g.Key, Amount = g.Sum(l => l.DebitAmount - l.CreditAmount) })
                    .OrderByDescending(x => x.Amount)
                    .Take(5)
                    .ToList();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Months = monthLabels.ToArray();

                    RevenueSeries.Clear();
                    RevenueSeries.Add(new LineSeries
                    {
                        Title = "Revenue",
                        Values = revenueValues,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10,
                        Stroke = System.Windows.Media.Brushes.DodgerBlue,
                        Fill = System.Windows.Media.Brushes.Transparent
                    });

                    ExpensesSeries.Clear();
                    TopExpenses.Clear();
                    foreach (var exp in topExpenses)
                    {
                        ExpensesSeries.Add(new PieSeries
                        {
                            Title = exp.Name,
                            Values = new ChartValues<decimal> { exp.Amount },
                            DataLabels = true
                        });

                        TopExpenses.Add(new
                        {
                            Name = exp.Name,
                            Amount = exp.Amount.ToString("C")
                        });
                    }

                    CashFlowSeries.Clear();
                    CashFlowSeries.Add(new LineSeries
                    {
                        Title = "Net Cash Flow",
                        Values = new ChartValues<decimal>(revenueValues.Zip(expenseValues, (r, e) => r - e)),
                        Stroke = System.Windows.Media.Brushes.MediumSeaGreen,
                        Fill = System.Windows.Media.Brushes.Transparent
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating charts: {ex.Message}");
            }
        }

        [RelayCommand]
        private void NavigateToTransactionsPage() => _navigationService.NavigateTo<TransactionsPage>();

        private string GetComparisonTag(decimal current, decimal previous, bool higherIsBetter)
        {
            if (previous == 0) return current > 0 ? "+100% vs last month" : "Stable vs last month";

            var percentage = ((current - previous) / Math.Abs(previous)) * 100;
            var direction = percentage >= 0 ? "+" : "";
            var isGood = higherIsBetter ? percentage >= 0 : percentage <= 0;

            return $"{direction}{percentage:F1}% vs last month";
        }

        private void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        public bool CanGoBack => _navigationService.CanGoBack;
    }
}