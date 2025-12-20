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

        public ObservableCollection<JournalLine> Transactions { get; } = new();

        public AccountTransactionsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            // Default date range: All time
            StartDate = null;
            EndDate = null;
        }

        public async Task Initialize(ChartOfAccount account)
        {
            SelectedAccount = account;
            await LoadTransactionsAsync();
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

                var transactions = await journalServices.GetAccountTransactionsAsync(
                    SelectedAccount.AccountId,
                    StartDate,
                    EndDate);

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
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(StartDate) || e.PropertyName == nameof(EndDate))
            {
                if (SelectedAccount != null)
                {
                    LoadTransactionsAsync().ConfigureAwait(false);
                }
            }
        }
    }
}