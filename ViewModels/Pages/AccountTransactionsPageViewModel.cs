using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AccountTransactionsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private ChartOfAccount _selectedAccount;

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        [ObservableProperty]
        private decimal _totalDebits;

        [ObservableProperty]
        private decimal _totalCredits;

        [ObservableProperty]
        private decimal _netChange;

        [ObservableProperty]
        private decimal _openingBalance;

        public AccountTransactionsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            // Default date range: All time
            StartDate = null;
            EndDate = null;
        }

        [RelayCommand]
        public async Task LoadAccountsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var coaServices = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();
                var accounts = await coaServices.GetAllAccountsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableAccounts.Clear();
                    foreach (var account in accounts)
                    {
                        AvailableAccounts.Add(account);
                    }
                });
            }
            catch (Exception ex)
            {
                // Silent error or simple notification
            }
        }

        public ObservableCollection<ChartOfAccount> AvailableAccounts { get; } = new();
        public ObservableCollection<JournalLine> Transactions { get; } = new();

        public async Task Initialize(ChartOfAccount account = null)
        {
            if (AvailableAccounts.Count == 0)
            {
                await LoadAccountsAsync();
            }

            if (account != null)
            {
                // Find matching account in AvailableAccounts if possible to maintain reference
                SelectedAccount = AvailableAccounts.FirstOrDefault(a => a.AccountId == account.AccountId) ?? account;
            }
            
            if (SelectedAccount != null)
            {
                await LoadTransactionsAsync();
            }
        }

        [RelayCommand]
        private async Task LoadTransactionsAsync()
        {
            if (SelectedAccount == null)
            {
                return;
            }

            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var journalServices = scope.ServiceProvider.GetRequiredService<JournalServices>();
                
                // Ensure UTC for PostgreSQL
                var utcStart = StartDate.HasValue ? DateTime.SpecifyKind(StartDate.Value.Date, DateTimeKind.Utc) : (DateTime?)null;
                var utcEnd = EndDate.HasValue ? DateTime.SpecifyKind(EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

                // Calculate opening balance if a starting date is provided
                if (utcStart.HasValue)
                {
                    OpeningBalance = await journalServices.GetAccountBalanceAsync(SelectedAccount.AccountId, utcStart.Value);
                }
                else
                {
                    OpeningBalance = 0; // If all time, opening is 0 relative to history
                }

                var transactions = await journalServices.GetAccountTransactionsAsync(
                    SelectedAccount.AccountId,
                    utcStart,
                    utcEnd);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions.Clear();
                    foreach (var transaction in transactions)
                    {
                        Transactions.Add(transaction);
                    }

                    CalculateTotals();
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading transactions: {ex.Message}", "Error", "ErrorOutline");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateTotals()
        {
            // Totals should only reflect POSTED transactions to match COA balance
            var postedTransactions = Transactions.Where(t => t.JournalEntry?.Status == "POSTED").ToList();

            TotalDebits = postedTransactions.Sum(t => t.DebitAmount);
            TotalCredits = postedTransactions.Sum(t => t.CreditAmount);
            NetChange = TotalDebits - TotalCredits;
        }

        [RelayCommand]
        private void PrintReport()
        {
            if (SelectedAccount == null || !Transactions.Any())
            {
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();
                
                var filePath = printService.GenerateAccountTransactionsPdf(
                    SelectedAccount.AccountName,
                    StartDate,
                    EndDate,
                    Transactions.ToList(),
                    OpeningBalance);

                printService.OpenPdfFile(filePath);
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error printing report: {ex.Message}", "Print Error", "ErrorOutline");
            }
        }

        [RelayCommand]
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(StartDate) || e.PropertyName == nameof(EndDate) || e.PropertyName == nameof(SelectedAccount))
            {
                if (SelectedAccount != null && !IsLoading)
                {
                    LoadTransactionsAsync().ConfigureAwait(false);
                }
            }
        }
    }
}