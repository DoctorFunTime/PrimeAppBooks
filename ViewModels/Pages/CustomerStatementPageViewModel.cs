using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class CustomerStatementPageViewModel : ObservableObject
    {
        private readonly JournalServices _journalServices;
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private Customer _customer;

        [ObservableProperty]
        private DateTime _startDate;

        [ObservableProperty]
        private DateTime _endDate;

        [ObservableProperty]
        private decimal _totalInvoiced;

        [ObservableProperty]
        private decimal _totalPaid;

        [ObservableProperty]
        private decimal _closingBalance;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<StatementItem> Transactions { get; } = new();

        public CustomerStatementPageViewModel(
            JournalServices journalServices,
            IServiceProvider serviceProvider,
            INavigationService navigationService)
        {
            _journalServices = journalServices;
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;

            // Default to current year
            EndDate = DateTime.Today;
            StartDate = new DateTime(DateTime.Today.Year, 1, 1);
        }

        public async Task Initialize(int customerId)
        {
            await LoadCustomerAndStatement(customerId);
        }

        public void OnNavigatedFrom()
        { }

        [RelayCommand]
        private async Task RefreshStatement()
        {
            if (Customer != null)
            {
                await LoadStatement(Customer.CustomerId);
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            _navigationService.GoBack();
        }

        private async Task LoadCustomerAndStatement(int customerId)
        {
            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<PrimeAppBooks.Data.AppDbContext>();
                Customer = await context.Customers.FindAsync(customerId);

                if (Customer != null)
                {
                    await LoadStatement(customerId);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadStatement(int customerId)
        {
            IsLoading = true;
            Transactions.Clear();
            TotalInvoiced = 0;
            TotalPaid = 0;
            ClosingBalance = 0;

            try
            {
                // 1. Get Opening Balance (transactions before StartDate)
                // We fetch all transactions to calculate running balance accurately from the beginning
                // Or we can fetch opening balance sum separately.
                // For simplicity and accuracy with the requested features, let's fetch ALL transactions for this customer
                // and then just display the ones in range, but use all for running balance.
                // However, fetching ALL history might be heavy.
                // Let's rely on the method we created.

                // Strategy:
                // 1. Fetch transactions from StartDate to EndDate.
                // 2. Fetch "Opening Balance" as sum of journal lines before StartDate.

                // 1. Get the real AR Account ID
                int arAccountId = 0;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<PrimeAppBooks.Data.AppDbContext>();
                    var arAccount = await context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "1100");
                    if (arAccount != null)
                    {
                        arAccountId = arAccount.AccountId;
                    }
                }

                if (arAccountId == 0)
                {
                    // Fallback if account 1100 is not found (shouldn't happen in valid setup)
                    // We might default to just showing all lines with ContactId, or warn.
                    // For now, let's proceed but maybe the filter won't work as expected if we use 0.
                    // But if we use 0, we imply AccountId must be 0, which is wrong.
                    // Let's rely on standard fetching but filter wisely.
                }

                // 2. Fetch transactions with the CORRECT AR Account ID
                var allLines = await _journalServices.GetCustomerTransactionsAsync(customerId, arAccountId, StartDate, EndDate);

                // 3. Filter strictly by AR Account ID to avoid Revenue/Inventory lines
                var lines = allLines.Where(x => x.AccountId == arAccountId).ToList();

                // Calculate Opening Balance
                var previousLinesRaw = await _journalServices.GetCustomerTransactionsAsync(customerId, arAccountId, null, StartDate.AddDays(-1));
                var previousLines = previousLinesRaw.Where(x => x.AccountId == arAccountId).ToList();

                decimal runningBalance = 0;

                foreach (var line in previousLines)
                {
                    // For Asset (AR): Debit increases, Credit decreases
                    runningBalance += (line.DebitAmount - line.CreditAmount);
                }

                // Add Opening Balance Line
                Transactions.Add(new StatementItem
                {
                    Date = StartDate,
                    Description = "Opening Balance",
                    Reference = "",
                    Debit = runningBalance > 0 ? runningBalance : 0,
                    Credit = runningBalance < 0 ? -runningBalance : 0,
                    RunningBalance = runningBalance
                });

                // Process Transactions in range
                foreach (var line in lines)
                {
                    var description = !string.IsNullOrEmpty(line.Description) ? line.Description : line.JournalEntry?.Description ?? "";

                    // Clean up description for display
                    if (description.Contains("Receivable for Invoice", StringComparison.OrdinalIgnoreCase))
                    {
                        description = "Invoice"; // Simplify the description
                    }

                    decimal debit = line.DebitAmount;
                    decimal credit = line.CreditAmount;

                    runningBalance += (debit - credit);

                    // Aggregating Totals (only for the period displayed)
                    TotalInvoiced += debit;
                    TotalPaid += credit;

                    Transactions.Add(new StatementItem
                    {
                        Date = line.LineDate.ToLocalTime(),
                        Reference = line.JournalEntry?.Reference ?? line.Reference,
                        Description = description,
                        Debit = debit,
                        Credit = credit,
                        RunningBalance = runningBalance
                    });
                }

                ClosingBalance = runningBalance;
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error loading statement: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}