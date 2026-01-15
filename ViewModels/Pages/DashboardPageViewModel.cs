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
using static PrimeAppBooks.Models.Pages.TransactionsModels;

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

            // Re-initialize dashboard asynchronously but safely
            _ = Task.Run(async () => await InitializeDashboardAsync());
        }

        private async Task InitializeDashboardAsync()
        {
            await LoadDashboardDataAsync();
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        [RelayCommand]
        public async Task ImportStudentData()
        {
            if (IsLoading) return;

            await _dbLock.WaitAsync();
            IsLoading = true;
            LoadingMessage = "Initializing import...";
            try
            {
                // Re-fetch everything inside the lock to ensure we have fresh data
                var students = fetches.GetAllStudentsTable();
                var count = students.Count;
                int current = 0;

                // Get transactions starting from Jan 1, 2026
                var detailedTransactions = fetches.GetStudentTransactions(new DateTime(2026, 1, 1));

                // Get necessary accounts
                var arAccount = await _coaService.GetAccountByNumberAsync("1100"); // Accounts Receivable
                var cashAccount = await _coaService.GetAccountByNumberAsync("1000"); // Cash
                var bankAccount = await _coaService.GetAccountByNumberAsync("1020"); // Bank
                var equityAccount = await _coaService.GetAccountByNumberAsync("3100"); // Retained Earnings
                var salesAccount = await _coaService.GetAccountByNumberAsync("4000"); // Sales Revenue
                var badDebtsAccount = await _coaService.GetAccountByNumberAsync("5150"); // Bad Debts Expense

                if (arAccount == null || equityAccount == null || salesAccount == null)
                {
                    System.Windows.MessageBox.Show(
                        "Required accounting accounts (1100, 3100, or 4000) not found in the Chart of Accounts.\n\n" +
                        "Please restart the application to allow the system to automatically create these missing accounts.", 
                        "Missing Configuration", 
                        System.Windows.MessageBoxButton.OK, 
                        System.Windows.MessageBoxImage.Warning);
                    
                    IsLoading = false;
                    return;
                }

                // Get existing grades to avoid redundant DB checks
                var existingGrades = await _context.StudentGrades.OrderBy(g => g.SortOrder).ToListAsync();
                var gradeList = existingGrades.Select(g => g.GradeName).ToHashSet();

                foreach (var student in students)
                {
                    current++;
                    LoadingMessage = $"Importing student {current} of {count}: {student.FullName}";

                    // Sync Grade/Class if it doesn't exist
                    if (!string.IsNullOrWhiteSpace(student.StudentClass) && !gradeList.Contains(student.StudentClass))
                    {
                        var newGrade = new StudentGrade
                        {
                            GradeName = student.StudentClass,
                            IsActive = true,
                            SortOrder = gradeList.Count + 1
                        };
                        _context.StudentGrades.Add(newGrade);
                        await _context.SaveChangesAsync();
                        gradeList.Add(student.StudentClass);
                    }

                    Customer customerRecord;
                    var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.StudentId == student.Id.ToString());

                    if (existingCustomer != null)
                    {
                        customerRecord = existingCustomer;
                    }
                    else
                    {
                        var datePart = DateTime.Now.ToString("yyMMdd");
                        var randomPart = new Random().Next(1000, 9999);

                        customerRecord = new Customer();
                        customerRecord.NationalId = Truncate(student.IDNumber, 50);
                        customerRecord.CustomerCode = $"C-{datePart}-{randomPart}";
                        customerRecord.Gender = Truncate(student.Gender, 10);
                        customerRecord.Email = string.Empty;
                        customerRecord.TaxId = string.Empty;
                        customerRecord.ContactPerson = Truncate(student.ContactDetails, 255);
                        customerRecord.BillingAddress = student.Address;
                        customerRecord.CustomerName = Truncate($"{student.Name} {student.Surname}", 255);
                        customerRecord.Phone = Truncate(student.ContactDetails, 50);
                        customerRecord.ShippingAddress = student.Address;
                        customerRecord.DefaultRevenueAccountId = 4000;
                        if (student.DOB != DateTime.MinValue)
                            customerRecord.DateOfBirth = student.DOB.ToUniversalTime();

                        customerRecord.StudentId = Truncate(student.Id.ToString(), 50);
                        customerRecord.GradeLevel = Truncate(student.StudentClass, 50);
                        customerRecord.GuardianName = Truncate(student.GuardianName, 255);
                        customerRecord.CreatedAt = DateTime.UtcNow;
                        customerRecord.UpdatedAt = DateTime.UtcNow;

                        _context.Customers.Add(customerRecord);
                        await _context.SaveChangesAsync();

                        // Create opening balance journal entry only for new customers (to avoid dupes)
                        if (student.OpeningBalance != 0)
                        {
                            var journalEntry = new JournalEntry
                            {
                                JournalDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                Description = $"Opening Balance Import - {student.FullName}",
                                Reference = $"OB-{student.Id}",
                                JournalType = "GENERAL",
                                Status = "POSTED",
                                Amount = Math.Abs(student.OpeningBalance),
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            journalEntry.JournalLines.Add(new JournalLine
                            {
                                AccountId = arAccount.AccountId,
                                DebitAmount = student.OpeningBalance > 0 ? student.OpeningBalance : 0,
                                CreditAmount = student.OpeningBalance < 0 ? Math.Abs(student.OpeningBalance) : 0,
                                Description = $"Opening Balance for {student.FullName}",
                                ContactId = customerRecord.CustomerId,
                                ContactType = "Customer",
                                LineDate = journalEntry.JournalDate,
                                CreatedAt = DateTime.UtcNow
                            });

                            journalEntry.JournalLines.Add(new JournalLine
                            {
                                AccountId = equityAccount.AccountId,
                                DebitAmount = student.OpeningBalance < 0 ? Math.Abs(student.OpeningBalance) : 0,
                                CreditAmount = student.OpeningBalance > 0 ? student.OpeningBalance : 0,
                                Description = "Opening Balance Offset",
                                LineDate = journalEntry.JournalDate,
                                CreatedAt = DateTime.UtcNow
                            });

                            await _journalService.CreateJournalEntryAsync(journalEntry);
                        }
                    }

                    // Process Detailed Transactions for this student
                    var studentTransactions = detailedTransactions.Where(t => t.StudentId == student.Id).ToList();
                    foreach (var trans in studentTransactions)
                    {
                        // Check if transaction already exists by reference to avoid duplicates
                        var refId = $"IMP-{student.Id}-{trans.TransactionDate:yyyyMMdd}-{trans.DocNumber ?? Math.Abs(trans.Amount).GetHashCode().ToString()}";
                        if (await _context.JournalEntries.AnyAsync(j => j.Reference == refId)) continue;

                        var transDateUtc = trans.TransactionDate.Kind == DateTimeKind.Utc ? trans.TransactionDate : trans.TransactionDate.ToUniversalTime();

                        if (trans.DebitCredit == "DR") // Sales Invoice (Debit AR, Credit Revenue)
                        {
                            var invoiceJournal = new JournalEntry
                            {
                                JournalDate = transDateUtc,
                                Description = string.IsNullOrWhiteSpace(trans.Description) ? $"Invoice for {student.FullName}" : trans.Description,
                                Reference = refId,
                                JournalType = "SALES_INVOICE",
                                Status = "POSTED",
                                Amount = trans.Amount,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            // Debit AR
                            invoiceJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = arAccount.AccountId,
                                DebitAmount = trans.Amount,
                                CreditAmount = 0,
                                Description = invoiceJournal.Description,
                                ContactId = customerRecord.CustomerId,
                                ContactType = "Customer",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            // Credit Income
                            invoiceJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = salesAccount.AccountId,
                                DebitAmount = 0,
                                CreditAmount = trans.Amount,
                                Description = "Tuition/Services",
                                ContactId = customerRecord.CustomerId, // Optional for income line
                                ContactType = "Customer",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            await _journalService.CreateJournalEntryAsync(invoiceJournal);
                        }
                        else if (trans.DebitCredit == "CR") // Payment (Debit Cash, Credit AR)
                        {
                            var paymentJournal = new JournalEntry
                            {
                                JournalDate = transDateUtc,
                                Description = string.IsNullOrWhiteSpace(trans.Description) ? $"Payment from {student.FullName}" : trans.Description,
                                Reference = refId,
                                JournalType = "PAYMENT",
                                Status = "POSTED",
                                Amount = trans.Amount,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            // Debit Cash
                            paymentJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = cashAccount.AccountId,
                                DebitAmount = trans.Amount,
                                CreditAmount = 0,
                                Description = "Cash Receipt",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            // Credit AR
                            paymentJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = arAccount.AccountId,
                                DebitAmount = 0,
                                CreditAmount = trans.Amount,
                                Description = paymentJournal.Description,
                                ContactId = customerRecord.CustomerId,
                                ContactType = "Customer",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            await _journalService.CreateJournalEntryAsync(paymentJournal);
                        }
                    }

                    // AUTOMATED WRITE-OFF FOR TRANSFERRED STUDENTS
                    if (student.isTransferred && student.OpeningBalance > 0 && badDebtsAccount != null)
                    {
                        await _journalService.CreateBadDebtWriteOffJournalAsync(
                            customerRecord.CustomerId,
                            student.OpeningBalance,
                            $"Automated Write-off: Transferred Student - {student.FullName}",
                            arAccount.AccountId,
                            badDebtsAccount.AccountId,
                            customerRecord.CustomerCode
                        );

                        customerRecord.IsActive = false;
                        _context.Customers.Update(customerRecord);
                        await _context.SaveChangesAsync();
                    }
                }

                // Import Cash Opening Balance
                LoadingMessage = "Importing Cash Opening Balance...";
                var cashBalance = fetches.GetCashOpeningBalance(new DateTime(2026, 1, 1));
                if (cashBalance != 0 && cashAccount != null && equityAccount != null)
                {
                    var cashEntry = new JournalEntry
                    {
                        JournalDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Description = "Cash Opening Balance Import",
                        Reference = "OB-CASH",
                        JournalType = "GENERAL",
                        Status = "POSTED",
                        Amount = Math.Abs(cashBalance),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    cashEntry.JournalLines.Add(new JournalLine
                    {
                        AccountId = cashAccount.AccountId,
                        DebitAmount = cashBalance > 0 ? cashBalance : 0,
                        CreditAmount = cashBalance < 0 ? Math.Abs(cashBalance) : 0,
                        Description = "Cash Opening Balance",
                        LineDate = cashEntry.JournalDate,
                        CreatedAt = DateTime.UtcNow
                    });

                    cashEntry.JournalLines.Add(new JournalLine
                    {
                        AccountId = equityAccount.AccountId,
                        DebitAmount = cashBalance < 0 ? Math.Abs(cashBalance) : 0,
                        CreditAmount = cashBalance > 0 ? cashBalance : 0,
                        Description = "Cash Opening Balance Offset",
                        LineDate = cashEntry.JournalDate,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _journalService.CreateJournalEntryAsync(cashEntry);
                }

                // Import Bank Opening Balance
                LoadingMessage = "Importing Bank Opening Balance...";
                var bankBalance = fetches.GetBankOpeningBalance(new DateTime(2026, 1, 1));
                if (bankBalance != 0 && bankAccount != null && equityAccount != null)
                {
                    var bankEntry = new JournalEntry
                    {
                        JournalDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Description = "Bank Opening Balance Import",
                        Reference = "OB-BANK",
                        JournalType = "GENERAL",
                        Status = "POSTED",
                        Amount = Math.Abs(bankBalance),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    bankEntry.JournalLines.Add(new JournalLine
                    {
                        AccountId = bankAccount.AccountId,
                        DebitAmount = bankBalance > 0 ? bankBalance : 0,
                        CreditAmount = bankBalance < 0 ? Math.Abs(bankBalance) : 0,
                        Description = "Bank Opening Balance",
                        LineDate = bankEntry.JournalDate,
                        CreatedAt = DateTime.UtcNow
                    });

                    bankEntry.JournalLines.Add(new JournalLine
                    {
                        AccountId = equityAccount.AccountId,
                        DebitAmount = bankBalance < 0 ? Math.Abs(bankBalance) : 0,
                        CreditAmount = bankBalance > 0 ? bankBalance : 0,
                        Description = "Bank Opening Balance Offset",
                        LineDate = bankEntry.JournalDate,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _journalService.CreateJournalEntryAsync(bankEntry);
                }

                LoadingMessage = "Finalizing dashboard data...";
                await LoadDashboardDataInternalAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Import failed: {ex}");
                System.Windows.MessageBox.Show($"Import failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                _dbLock.Release();
            }
        }

        [RelayCommand]
        public async Task LoadDashboardDataAsync()
        {
            if (IsLoading) return;
            
            await _dbLock.WaitAsync();
            IsLoading = true;
            LoadingMessage = "Updating dashboard...";
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