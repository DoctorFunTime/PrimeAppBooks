using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Models.Temp_Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.APIs;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Services.Temp_Service;
using PrimeAppBooks.Views.Pages;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
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
        private readonly BoxServices _boxServices = new();
        private Fetches fetches = new();
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;
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

        private List<StudentSelection> _students = new();

        public List<StudentSelection> Students
        {
            get => _students;
            set => SetProperty(ref _students, value);
        }

        private Customer _customers = new();

        public Customer StudentsToBeAdded
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

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

            _ = InitializeDashboardAsync();
        }

        private async Task InitializeDashboardAsync()
        {
            await LoadDashboardDataAsync();
        }

        [RelayCommand]
        public async Task ImportStudentData()
        {
            var random = new Random();
            var datePart = DateTime.Now.ToString("yyMMdd");
            var randomPart = random.Next(1000, 9999);

            List<StudentSelection> students = fetches.GetAllStudentsTable();
            List<Customer> customerList = new();
            int counter = 0;

            foreach (StudentSelection student in students)
            {
                Customer StudentsToBeAdded = new();

                StudentsToBeAdded.NationalId = student.IDNumber;
                StudentsToBeAdded.CustomerCode = $"C-{datePart}-{randomPart}";
                StudentsToBeAdded.Gender = student.Gender;
                StudentsToBeAdded.Email = string.Empty;
                StudentsToBeAdded.TaxId = string.Empty;
                StudentsToBeAdded.ContactPerson = student.ContactDetails;
                StudentsToBeAdded.BillingAddress = student.Address;
                StudentsToBeAdded.CustomerName = $"{student.Name} {student.Surname}";
                StudentsToBeAdded.ContactPerson = student.ContactDetails;
                StudentsToBeAdded.Phone = student.ContactDetails;
                StudentsToBeAdded.ShippingAddress = student.Address;
                StudentsToBeAdded.DefaultRevenueAccountId = 4000;
                StudentsToBeAdded.DateOfBirth = student.DOB.ToUniversalTime();
                StudentsToBeAdded.Gender = student.Gender;
                StudentsToBeAdded.StudentId = student.Id.ToString();
                StudentsToBeAdded.GradeLevel = student.StudentClass;
                StudentsToBeAdded.GuardianName = student.GuardianName;
                StudentsToBeAdded.NationalId = student.IDNumber;
                StudentsToBeAdded.CreatedAt = DateTime.UtcNow;
                StudentsToBeAdded.UpdatedAt = DateTime.UtcNow;

                _context.Customers.Add(StudentsToBeAdded);
                await _context.SaveChangesAsync();
            }
        }

        [RelayCommand]
        public async Task LoadDashboardDataAsync()
        {
            try
            {
                // 1. Fetch KPI Balances by Subtype/Type
                var accounts = await _coaService.GetAllAccountsAsync();

                // Assets & Expenses: Normal balance is DEBIT (Debit - Credit)
                CashBalance = accounts
                    .Where(a => a.AccountSubtype == "Cash" || a.AccountName.Contains("Cash"))
                    .Sum(a => a.CurrentBalance);

                Receivables = accounts
                    .Where(a => a.AccountSubtype == "Accounts Receivable" || a.AccountType == "ASSET" && a.AccountName.Contains("Receivable"))
                    .Sum(a => a.CurrentBalance);

                // Liabilities, Equity, Revenue: Normal balance is CREDIT (Credit - Debit)
                // CurrentBalance is stored as Debit - Credit, so we negate it for these types
                Payables = accounts
                    .Where(a => a.AccountSubtype == "Accounts Payable" || a.AccountType == "LIABILITY" && a.AccountName.Contains("Payable"))
                    .Sum(a => -a.CurrentBalance);

                // 2. Fetch Monthly Revenue and Expenses
                var startOfCurrentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfCurrentMonth = startOfCurrentMonth.AddMonths(1).AddDays(-1);
                var startOfPrevMonth = startOfCurrentMonth.AddMonths(-1);
                var endOfPrevMonth = startOfCurrentMonth.AddDays(-1);

                var currentLines = await _journalService.GetJournalLinesAsync(startOfCurrentMonth, endOfCurrentMonth);
                var prevLines = await _journalService.GetJournalLinesAsync(startOfPrevMonth, endOfPrevMonth);

                MonthlyRevenue = currentLines
                    .Where(l => l.JournalEntry?.Status == "POSTED" && l.ChartOfAccount?.AccountType == "REVENUE")
                    .Sum(l => l.CreditAmount - l.DebitAmount);
                MonthlyRevenue = Math.Abs(MonthlyRevenue);

                var prevRevenue = prevLines
                    .Where(l => l.JournalEntry?.Status == "POSTED" && l.ChartOfAccount?.AccountType == "REVENUE")
                    .Sum(l => l.CreditAmount - l.DebitAmount);
                prevRevenue = Math.Abs(prevRevenue);

                MonthlyExpenses = currentLines
                    .Where(l => l.JournalEntry?.Status == "POSTED" && l.ChartOfAccount?.AccountType == "EXPENSE")
                    .Sum(l => l.DebitAmount - l.CreditAmount);
                MonthlyExpenses = Math.Abs(MonthlyExpenses);

                var prevExpenses = prevLines
                    .Where(l => l.JournalEntry?.Status == "POSTED" && l.ChartOfAccount?.AccountType == "EXPENSE")
                    .Sum(l => l.DebitAmount - l.CreditAmount);
                prevExpenses = Math.Abs(prevExpenses);

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
                var journalEntries = await _journalService.GetAllJournalEntriesAsync();
                var recentPosted = journalEntries
                    .Where(j => j.Status == "POSTED")
                    .OrderByDescending(j => j.PostedAt)
                    .Take(10);

                // 4. Load Overdue and Upcoming Invoices
                var today = DateTime.UtcNow;
                var overdueInvoices = await _context.SalesInvoices
                    .Include(i => i.Customer)
                    .Where(i => i.Status != "VOID" && i.Balance > 0 && i.DueDate < today)
                    .OrderByDescending(i => i.Balance)
                    .ToListAsync();

                var upcomingDue = await _context.PurchaseInvoices
                    .Include(i => i.Vendor)
                    .Where(i => i.Status != "VOID" && i.Balance > 0 && i.DueDate >= today && i.DueDate <= today.AddDays(7))
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