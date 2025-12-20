using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class ChartOfAccountsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        public ObservableCollection<ChartOfAccount> Accounts { get; } = new();
        public ObservableCollection<ChartOfAccount> FilteredAccounts { get; } = new();
        public ObservableCollection<ChartOfAccount> AccountHierarchy { get; } = new();

        [ObservableProperty]
        private bool _isLoading = false;

        private ChartOfAccount _selectedAccount;

        public ChartOfAccount SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value))
                {
                    OnPropertyChanged(nameof(SelectedAccountPostedBalance));
                }
            }
        }

        public decimal SelectedAccountPostedBalance
        {
            get
            {
                if (SelectedAccount == null)
                    return 0;

                return CalculateAccountBalance(SelectedAccount);
            }
        }

        [ObservableProperty]
        private bool _showInactiveAccounts = false;

        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _selectedAccountType = string.Empty;

        public string SelectedAccountType
        {
            get => _selectedAccountType;
            set
            {
                if (SetProperty(ref _selectedAccountType, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _selectedAccountSubtype = string.Empty;

        public string SelectedAccountSubtype
        {
            get => _selectedAccountSubtype;
            set
            {
                if (SetProperty(ref _selectedAccountSubtype, value))
                {
                    ApplyFilters();
                }
            }
        }

        private ChartOfAccount _selectedParentAccount;

        public ChartOfAccount SelectedParentAccount
        {
            get => _selectedParentAccount;
            set
            {
                if (SetProperty(ref _selectedParentAccount, value))
                {
                    ApplyFilters();
                }
            }
        }

        private bool _showHierarchyView = true;

        public bool ShowHierarchyView
        {
            get => _showHierarchyView;
            set
            {
                if (SetProperty(ref _showHierarchyView, value))
                {
                    UpdateView();
                }
            }
        }

        private bool _showListView = false;

        public bool ShowListView
        {
            get => _showListView;
            set
            {
                if (SetProperty(ref _showListView, value))
                {
                    UpdateView();
                }
            }
        }

        [ObservableProperty]
        private int _totalAccountsCount = 0;

        [ObservableProperty]
        private int _activeAccountsCount = 0;

        [ObservableProperty]
        private int _inactiveAccountsCount = 0;

        [ObservableProperty]
        private int _accountsWithTransactionsCount = 0;

        [ObservableProperty]
        private decimal _totalAssetsBalance = 0;

        [ObservableProperty]
        private decimal _totalLiabilitiesBalance = 0;

        [ObservableProperty]
        private decimal _totalEquityBalance = 0;

        [ObservableProperty]
        private decimal _totalRevenueBalance = 0;

        [ObservableProperty]
        private decimal _totalExpensesBalance = 0;

        [ObservableProperty]
        private string _resultsSummary = "No accounts found";

        public ObservableCollection<string> AccountTypes { get; } = new();
        public ObservableCollection<string> AccountSubtypes { get; } = new();
        public ObservableCollection<ChartOfAccount> ParentAccounts { get; } = new();

        public ChartOfAccountsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _navigationService.PageNavigated += OnPageNavigated;
        }

        #region Commands

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        [RelayCommand]
        private async Task Refresh() => await LoadAccountsAsync();

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedAccountType = string.Empty;
            SelectedAccountSubtype = string.Empty;
            SelectedParentAccount = null;
            ShowInactiveAccounts = false;
            ApplyFilters();
        }

        [RelayCommand]
        private void ApplyFiltersCommand() => ApplyFilters();

        [RelayCommand]
        private void ShowHierarchyViewCommand()
        {
            ShowHierarchyView = true;
            ShowListView = false;
        }

        [RelayCommand]
        private void ShowListViewCommand()
        {
            ShowHierarchyView = false;
            ShowListView = true;
        }

        [RelayCommand]
        private void AddNewAccount()
        {
            try
            {
                _navigationService.NavigateTo<AddAccountPage>();
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _messageBoxService.ShowMessage($"Error navigating to add account page: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        [RelayCommand]
        private void EditAccount(ChartOfAccount account)
        {
            if (account == null) return;

            _navigationService.NavigateTo<AddAccountPage>(account);
        }

        [RelayCommand]
        private void ViewAccountTransactions(ChartOfAccount account)
        {
            if (account == null) return;

            _navigationService.NavigateTo<AccountTransactionsPage>(account);
        }

        [RelayCommand]
        private async Task DeleteAccount(ChartOfAccount account)
        {
            if (account == null) return;

            try
            {
                if (account.IsSystemAccount)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage("System accounts cannot be deleted.", "Warning", "WarningOutline");
                    });
                    return;
                }

                var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    return _messageBoxService.ShowConfirmation(
                        $"Are you sure you want to delete account '{account.AccountName}'?\n\nThis action cannot be undone.",
                        "Delete Account",
                        "DeleteForever"
                    );
                });

                if (result)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

                    var success = await chartOfAccountsService.DeleteAccountAsync(account.AccountId);

                    if (success)
                    {
                        await LoadAccountsAsync();

                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _messageBoxService.ShowMessage("Account deleted successfully!", "Success", "CheckCircleOutline");
                        });
                    }
                    else
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _messageBoxService.ShowMessage("Failed to delete account. Account may have dependencies.", "Error", "ErrorOutline");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error deleting account: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        [RelayCommand]
        private async Task ToggleAccountStatus(ChartOfAccount account)
        {
            if (account == null) return;

            try
            {
                if (account.IsSystemAccount)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage("System accounts cannot be deactivated.", "Warning", "WarningOutline");
                    });
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

                bool success;
                if (account.IsActive)
                {
                    success = await chartOfAccountsService.DeactivateAccountAsync(account.AccountId);
                }
                else
                {
                    success = await chartOfAccountsService.ReactivateAccountAsync(account.AccountId);
                }

                if (success)
                {
                    await LoadAccountsAsync();

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage($"Account {(account.IsActive ? "deactivated" : "reactivated")} successfully!", "Success", "CheckCircleOutline");
                    });
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error updating account status: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        [RelayCommand]
        private async Task UpdateAccountBalances()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

                await chartOfAccountsService.UpdateAllAccountBalancesAsync();
                await LoadAccountsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Account balances updated successfully!", "Success", "CheckCircleOutline");
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error updating account balances: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        #endregion Commands

        #region Methods

        private async Task LoadAccountsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

            try
            {
                IsLoading = true;

                var accounts = ShowInactiveAccounts
                    ? await chartOfAccountsService.GetAllAccountsIncludingInactiveAsync()
                    : await chartOfAccountsService.GetAllAccountsAsync();

                Accounts.Clear();
                foreach (var account in accounts)
                {
                    Accounts.Add(account);
                }

                await LoadFilterOptionsAsync();
                UpdateStatistics();
                ApplyFilters();
                UpdateView(); // Ensure hierarchy is built after loading accounts
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading accounts: {ex.Message}", "Error", "ErrorOutline");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadFilterOptionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

            try
            {
                var accountTypes = await chartOfAccountsService.GetAccountTypesAsync();
                var accountSubtypes = await chartOfAccountsService.GetAccountSubtypesAsync();
                var parentAccounts = await chartOfAccountsService.GetParentAccountsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AccountTypes.Clear();
                    AccountTypes.Add("All Types");
                    foreach (var type in accountTypes)
                    {
                        AccountTypes.Add(type);
                    }

                    AccountSubtypes.Clear();
                    AccountSubtypes.Add("All Subtypes");
                    foreach (var subtype in accountSubtypes)
                    {
                        AccountSubtypes.Add(subtype);
                    }

                    ParentAccounts.Clear();
                    ParentAccounts.Add(new ChartOfAccount { AccountId = 0, AccountName = "All Accounts", AccountNumber = "000" });
                    foreach (var parent in parentAccounts)
                    {
                        ParentAccounts.Add(parent);
                    }
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading filter options: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        /// <summary>
        /// Calculate account balance from POSTED journal lines only
        /// </summary>
        private decimal CalculateAccountBalance(ChartOfAccount account)
        {
            if (account.JournalLines == null || !account.JournalLines.Any())
                return 0;

            // Only calculate from POSTED journal entries
            var postedLines = account.JournalLines
                .Where(jl => jl.JournalEntry?.Status == "POSTED")
                .ToList();

            if (!postedLines.Any())
                return 0;

            var debitTotal = postedLines.Sum(jl => jl.DebitAmount);
            var creditTotal = postedLines.Sum(jl => jl.CreditAmount);

            // Calculate balance based on normal balance type
            if (account.NormalBalance == "DEBIT")
            {
                // Assets, Expenses: Debit increases, Credit decreases
                return debitTotal - creditTotal;
            }
            else
            {
                // Liabilities, Equity, Revenue: Credit increases, Debit decreases
                return creditTotal - debitTotal;
            }
        }

        private void UpdateStatistics()
        {
            TotalAccountsCount = Accounts.Count;
            ActiveAccountsCount = Accounts.Count(a => a.IsActive);
            InactiveAccountsCount = Accounts.Count(a => !a.IsActive);
            AccountsWithTransactionsCount = Accounts.Count(a => a.JournalLines?.Any() == true);

            // Calculate balances by account type using POSTED transactions only
            TotalAssetsBalance = Accounts
                .Where(a => a.AccountType == "ASSET" && a.IsActive)
                .Sum(a => CalculateAccountBalance(a));

            TotalLiabilitiesBalance = Accounts
                .Where(a => a.AccountType == "LIABILITY" && a.IsActive)
                .Sum(a => CalculateAccountBalance(a));

            var equity = Accounts
                .Where(a => a.AccountType == "EQUITY" && a.IsActive)
                .Sum(a => CalculateAccountBalance(a));

            TotalRevenueBalance = Accounts
                .Where(a => a.AccountType == "REVENUE" && a.IsActive)
                .Sum(a => CalculateAccountBalance(a));

            TotalExpensesBalance = Accounts
                .Where(a => a.AccountType == "EXPENSE" && a.IsActive)
                .Sum(a => CalculateAccountBalance(a));

            // Equity should include Net Income (Revenue - Expenses)
            // Based on the accounting equation: Assets = Liabilities + Equity + (Revenue - Expenses)
            TotalEquityBalance = equity + TotalRevenueBalance - TotalExpensesBalance;
        }

        private void ApplyFilters()
        {
            var filtered = Accounts.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(a =>
                    a.AccountNumber.ToLower().Contains(searchLower) ||
                    a.AccountName.ToLower().Contains(searchLower) ||
                    (a.Description != null && a.Description.ToLower().Contains(searchLower)));
            }

            // Account type filter
            if (!string.IsNullOrWhiteSpace(SelectedAccountType) && SelectedAccountType != "All Types")
            {
                filtered = filtered.Where(a => a.AccountType == SelectedAccountType);
            }

            // Account subtype filter
            if (!string.IsNullOrWhiteSpace(SelectedAccountSubtype) && SelectedAccountSubtype != "All Subtypes")
            {
                filtered = filtered.Where(a => a.AccountSubtype == SelectedAccountSubtype);
            }

            // Parent account filter
            if (SelectedParentAccount != null && SelectedParentAccount.AccountId > 0)
            {
                filtered = filtered.Where(a => a.ParentAccountId == SelectedParentAccount.AccountId);
            }

            // Active status filter
            if (!ShowInactiveAccounts)
            {
                filtered = filtered.Where(a => a.IsActive);
            }

            FilteredAccounts.Clear();
            foreach (var account in filtered.OrderBy(a => a.AccountNumber))
            {
                FilteredAccounts.Add(account);
            }

            UpdateResultsSummary();
            UpdateView();
        }

        private void UpdateView()
        {
            if (ShowHierarchyView)
            {
                BuildAccountHierarchy();
            }
        }

        private void BuildAccountHierarchy()
        {
            AccountHierarchy.Clear();

            var accountIds = new HashSet<int>(FilteredAccounts.Select(a => a.AccountId));

            var rootAccounts = FilteredAccounts
                .Where(a => a.ParentAccountId == null || !accountIds.Contains(a.ParentAccountId.Value))
                .OrderBy(a => a.AccountNumber)
                .ToList();

            foreach (var rootAccount in rootAccounts)
            {
                var hierarchyAccount = BuildAccountHierarchyRecursive(rootAccount);
                AccountHierarchy.Add(hierarchyAccount);
            }
        }

        private ChartOfAccount BuildAccountHierarchyRecursive(ChartOfAccount account)
        {
            // Create a copy of the account for the hierarchy
            var hierarchyAccount = new ChartOfAccount
            {
                AccountId = account.AccountId,
                AccountNumber = account.AccountNumber,
                AccountName = account.AccountName,
                AccountType = account.AccountType,
                AccountSubtype = account.AccountSubtype,
                Description = account.Description,
                ParentAccountId = account.ParentAccountId,
                IsActive = account.IsActive,
                IsSystemAccount = account.IsSystemAccount,
                NormalBalance = account.NormalBalance,
                OpeningBalance = account.OpeningBalance,
                OpeningBalanceDate = account.OpeningBalanceDate,

                // IMPORTANT: Calculate current balance from POSTED journal lines only
                CurrentBalance = CalculateAccountBalance(account),

                CreatedBy = account.CreatedBy,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,

                // Copy the filtered journal lines (already filtered to POSTED in the query)
                JournalLines = account.JournalLines,

                ChildAccounts = new List<ChartOfAccount>()
            };

            // Find and add child accounts
            var childAccounts = FilteredAccounts
                .Where(a => a.ParentAccountId == account.AccountId)
                .OrderBy(a => a.AccountNumber)
                .ToList();

            foreach (var childAccount in childAccounts)
            {
                var childHierarchyAccount = BuildAccountHierarchyRecursive(childAccount);
                hierarchyAccount.ChildAccounts.Add(childHierarchyAccount);
            }

            return hierarchyAccount;
        }

        private void UpdateResultsSummary()
        {
            var count = FilteredAccounts.Count;
            ResultsSummary = count == 1 ? "1 account found" : $"{count} accounts found";
        }

        private async void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));

            // Load data when ChartOfAccountsPage is navigated to
            if (page is ChartOfAccountsPage)
            {
                await LoadAccountsAsync();
            }
        }

        #endregion Methods

        public bool CanGoBack => _navigationService.CanGoBack;
    }
}