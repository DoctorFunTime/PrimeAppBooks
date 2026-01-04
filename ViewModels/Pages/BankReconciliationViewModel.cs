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
    public partial class BankReconciliationViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private ChartOfAccount _selectedAccount;

        [ObservableProperty]
        private DateTime _statementDate = DateTime.Today;

        [ObservableProperty]
        private decimal _statementEndingBalance = 0;

        [ObservableProperty]
        private decimal _statementStartingBalance = 0;

        [ObservableProperty]
        private decimal _clearedDebits = 0;

        [ObservableProperty]
        private decimal _clearedCredits = 0;

        [ObservableProperty]
        private decimal _clearedBalance = 0;

        [ObservableProperty]
        private decimal _difference = 0;

        public ObservableCollection<ChartOfAccount> AvailableAccounts { get; } = new();
        public ObservableCollection<ReconciliationLineWrapper> Transactions { get; } = new();

        public BankReconciliationViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
        }

        public async Task Initialize()
        {
            await LoadAccountsAsync();
        }

        private async Task LoadAccountsAsync()
        {
            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bankService = scope.ServiceProvider.GetRequiredService<BankServices>();
                var accounts = await bankService.GetBankAccountsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableAccounts.Clear();
                    foreach (var acc in accounts)
                    {
                        AvailableAccounts.Add(acc);
                    }
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedAccountChanged(ChartOfAccount value)
        {
            if (value != null)
            {
                LoadReconciliationDataAsync().ConfigureAwait(false);
            }
        }

        private async Task LoadReconciliationDataAsync()
        {
            if (SelectedAccount == null) return;

            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bankService = scope.ServiceProvider.GetRequiredService<BankServices>();
                
                StatementStartingBalance = await bankService.GetLastReconciledBalanceAsync(SelectedAccount.AccountId);
                
                var uncleared = await bankService.GetUnclearedTransactionsAsync(SelectedAccount.AccountId, StatementDate);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions.Clear();
                    foreach (var line in uncleared)
                    {
                        var wrapper = new ReconciliationLineWrapper(line);
                        wrapper.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(ReconciliationLineWrapper.IsSelected))
                            {
                                CalculateTotals();
                            }
                        };
                        Transactions.Add(wrapper);
                    }
                    CalculateTotals();
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateTotals()
        {
            ClearedDebits = Transactions.Where(t => t.IsSelected).Sum(t => t.Line.DebitAmount);
            ClearedCredits = Transactions.Where(t => t.IsSelected).Sum(t => t.Line.CreditAmount);
            
            // Adjusted balance = Starting Balance + Debits - Credits
            ClearedBalance = StatementStartingBalance + ClearedDebits - ClearedCredits;
            Difference = StatementEndingBalance - ClearedBalance;
        }

        [RelayCommand]
        private async Task SaveProgressAsync()
        {
            await SaveInternalAsync("DRAFT");
        }

        [RelayCommand]
        private async Task CompleteReconciliationAsync()
        {
            if (Difference != 0)
            {
                _messageBoxService.ShowMessage("Cannot complete reconciliation while there is a difference.", "Validation Error", "ErrorOutline");
                return;
            }

            await SaveInternalAsync("COMPLETED");
        }

        private async Task SaveInternalAsync(string status)
        {
            if (SelectedAccount == null) return;

            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bankService = scope.ServiceProvider.GetRequiredService<BankServices>();

                var reconciliation = new BankReconciliation
                {
                    AccountId = SelectedAccount.AccountId,
                    StatementDate = DateTime.SpecifyKind(StatementDate, DateTimeKind.Utc),
                    StatementStartingBalance = StatementStartingBalance,
                    StatementEndingBalance = StatementEndingBalance,
                    ClearedDifference = Difference,
                    Status = status
                };

                var selectedLines = Transactions.Where(t => t.IsSelected).Select(t => t.Line.LineId).ToList();
                await bankService.SaveReconciliationAsync(reconciliation, selectedLines);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Reconciliation saved as {status}.", "Success", "CheckCircleOutline");
                    if (status == "COMPLETED")
                    {
                         NavigateBack();
                    }
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error saving reconciliation: {ex.Message}", "Error", "ErrorOutline");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }

        partial void OnStatementEndingBalanceChanged(decimal value) => CalculateTotals();
        partial void OnStatementStartingBalanceChanged(decimal value) => CalculateTotals();
    }

    public partial class ReconciliationLineWrapper : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        public JournalLine Line { get; }

        public ReconciliationLineWrapper(JournalLine line)
        {
            Line = line;
            IsSelected = line.IsCleared;
        }
    }
}
