using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using PrimeAppBooks.Services.APIs;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class DashboardPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        private readonly ChartOfAccountsServices _coaService;
        private readonly JournalServices _journalService;

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

        public SeriesCollection RevenueSeries { get; set; } = new();
        public SeriesCollection ExpensesSeries { get; set; } = new();
        public SeriesCollection CashFlowSeries { get; set; } = new();
        public string[] Months { get; set; }
        public string[] ProjectionMonths { get; set; }

        public ObservableCollection<object> RecentActivities { get; } = new();
        public ObservableCollection<object> OverdueItems { get; } = new();
        public ObservableCollection<object> UpcomingDueItems { get; } = new();
        public ObservableCollection<object> TopExpenses { get; } = new();

        public DashboardPageViewModel(
            INavigationService navigationService,
            ChartOfAccountsServices coaService,
            JournalServices journalService)
        {
            _navigationService = navigationService;
            _coaService = coaService;
            _journalService = journalService;

            _ = InitializeDashboardAsync();
        }

        private async Task InitializeDashboardAsync()
        {
            await LoadDashboardDataAsync();
        }

        [RelayCommand]
        public async Task LoadDashboardDataAsync()
        {
            try
            {
                // 1. Fetch KPI Balances by Subtype
                var accounts = await _coaService.GetAllAccountsAsync();

                CashBalance = accounts.Where(a => a.AccountName == "Cash").Sum(a => a.CurrentBalance);
                Receivables = accounts.Where(a => a.AccountName == "Accounts Receivable").Sum(a => a.CurrentBalance);
                Payables = accounts.Where(a => a.AccountName == "Accounts Payable").Sum(a => a.CurrentBalance);

                // 2. Fetch Monthly Revenue and Expenses
                var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var trialBalance = await _journalService.GetTrialBalanceAsync(); // This filters by POSTED

                MonthlyRevenue = accounts
                    .Where(a => a.AccountType == "REVENUE")
                    .Sum(a => a.CurrentBalance); // Normal balance for revenue is Credit, CurrentBalance is Debit-Credit

                // Revenue is usually Credit balance, so net balance will be negative if using Debit - Credit
                // Adjusting to absolute or normal balance
                MonthlyRevenue = Math.Abs(MonthlyRevenue);

                MonthlyExpenses = accounts
                    .Where(a => a.AccountType == "EXPENSE")
                    .Sum(a => a.CurrentBalance);

                NetIncome = MonthlyRevenue - MonthlyExpenses;

                if (MonthlyRevenue > 0)
                {
                    var margin = (NetIncome / MonthlyRevenue) * 100;
                    ProfitMargin = $"{margin:F1}%";
                }
                else
                {
                    ProfitMargin = "0.0%";
                }

                // 3. Load Recent Activity (Last 10 posted transactions)
                var journalEntries = await _journalService.GetAllJournalEntriesAsync();
                var recentPosted = journalEntries
                    .Where(j => j.Status == "POSTED")
                    .OrderByDescending(j => j.PostedAt)
                    .Take(10);

                // 4. Load Overdue and Upcoming Invoices (Simplified logic)
                var salesInvoices = await _coaService.FilterAccountsAsync(); // Need a Sales service call ideally
                                                                             // Assuming we use JournalEntries for now if full Sales service isn't yielding easy "Overdue" lists
                                                                             // Let's stick to JournalEntries filtering or placeholder for specific invoice models if available.
                                                                             // Wait, I saw SalesInvoice and PurchaseInvoice in TransactionsModels.cs.

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

                    // Placeholder for overdue/upcoming until full service methods exist
                    OverdueItems.Clear();
                    UpcomingDueItems.Clear();
                });

                UpdateCharts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }

        private async void UpdateCharts()
        {
            try
            {
                var now = DateTime.UtcNow;
                var monthLabels = new List<string>();
                var revenueValues = new ChartValues<decimal>();
                var expenseValues = new ChartValues<decimal>();

                // 1. Revenue/Expense Trends
                for (int i = 11; i >= 0; i--)
                {
                    var date = now.AddMonths(-i);
                    var startOfMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    monthLabels.Add(date.ToString("MMM"));

                    var lines = await _journalService.GetJournalLinesAsync(startOfMonth, endOfMonth);

                    var rev = lines
                        .Where(l => l.JournalEntry?.Status == "POSTED" && l.ChartOfAccount?.AccountType == "REVENUE")
                        .Sum(l => l.CreditAmount - l.DebitAmount);

                    var exp = lines
                        .Where(l => l.JournalEntry?.Status == "POSTED" && l.ChartOfAccount?.AccountType == "EXPENSE")
                        .Sum(l => l.DebitAmount - l.CreditAmount);

                    revenueValues.Add(Math.Max(0, rev));
                    expenseValues.Add(Math.Max(0, exp));
                }

                // 2. Expense Categories (Top 5 for current month)
                var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
                var currentMonthLines = await _journalService.GetJournalLinesAsync(currentMonthStart, currentMonthEnd);

                var topExpenses = currentMonthLines
                    .Where(l => l.JournalEntry?.Status == "POSTED" && l.ChartOfAccount?.AccountType == "EXPENSE")
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

                    // Cash Flow: Simple historical Net Income trend
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
        private void NavigateToJournalPage() => _navigationService.NavigateTo<JournalPage>();

        private void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        public bool CanGoBack => _navigationService.CanGoBack;
    }
}