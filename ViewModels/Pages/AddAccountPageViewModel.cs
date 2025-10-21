using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Controls;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AddAccountPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        #region Observable Properties

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _accountNumber = string.Empty;

        [ObservableProperty]
        private string _accountName = string.Empty;

        [ObservableProperty]
        private string _selectedAccountType = string.Empty;

        [ObservableProperty]
        private string _selectedAccountSubtype = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private ChartOfAccount _selectedParentAccount;

        [ObservableProperty]
        private string _selectedNormalBalance = "DEBIT";

        [ObservableProperty]
        private decimal _openingBalance = 0;

        [ObservableProperty]
        private DateTime _openingBalanceDate = DateTime.Today;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private bool _isSystemAccount = false;

        #endregion

        #region Collections

        public ObservableCollection<string> AccountTypes { get; } = new();
        public ObservableCollection<string> AccountSubtypes { get; } = new();
        public ObservableCollection<ChartOfAccount> ParentAccounts { get; } = new();
        public ObservableCollection<string> NormalBalanceOptions { get; } = new()
        {
            "DEBIT",
            "CREDIT"
        };

        #endregion

        #region Validation Properties

        [ObservableProperty]
        private string _accountNumberError = string.Empty;

        [ObservableProperty]
        private string _accountNameError = string.Empty;

        [ObservableProperty]
        private string _accountTypeError = string.Empty;

        [ObservableProperty]
        private bool _isValid = false;

        #endregion

        public AddAccountPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _navigationService.PageNavigated += OnPageNavigated;
        }

        #region Commands

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

                var accountTypes = await chartOfAccountsService.GetAccountTypesAsync();
                var accountSubtypes = await chartOfAccountsService.GetAccountSubtypesAsync();
                var parentAccounts = await chartOfAccountsService.GetParentAccountsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Load account types
                    AccountTypes.Clear();
                    foreach (var type in accountTypes)
                    {
                        AccountTypes.Add(type);
                    }

                    // Load account subtypes
                    AccountSubtypes.Clear();
                    AccountSubtypes.Add(string.Empty); // Empty option
                    foreach (var subtype in accountSubtypes)
                    {
                        AccountSubtypes.Add(subtype);
                    }

                    // Load parent accounts
                    ParentAccounts.Clear();
                    ParentAccounts.Add(null); // No parent option
                    foreach (var parent in parentAccounts)
                    {
                        ParentAccounts.Add(parent);
                    }

                    // Set default account type if available
                    if (AccountTypes.Count > 0 && string.IsNullOrEmpty(SelectedAccountType))
                    {
                        SelectedAccountType = AccountTypes.First();
                    }
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading data: {ex.Message}", "Error", "ErrorOutline");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveAccountAsync()
        {
            if (!ValidateForm())
            {
                _messageBoxService.ShowMessage("Please fix the validation errors before saving.", "Validation Error", "WarningOutline");
                return;
            }

            IsLoading = true;
            try
            {
                var newAccount = new ChartOfAccount
                {
                    AccountNumber = AccountNumber.Trim(),
                    AccountName = AccountName.Trim(),
                    AccountType = SelectedAccountType,
                    AccountSubtype = string.IsNullOrEmpty(SelectedAccountSubtype) ? null : SelectedAccountSubtype,
                    Description = string.IsNullOrEmpty(Description) ? null : Description.Trim(),
                    ParentAccountId = SelectedParentAccount?.AccountId,
                    NormalBalance = SelectedNormalBalance,
                    OpeningBalance = OpeningBalance,
                    OpeningBalanceDate = OpeningBalanceDate,
                    IsActive = IsActive,
                    IsSystemAccount = IsSystemAccount,
                    CreatedBy = 1 // TODO: Get from current user
                };

                using var scope = _serviceProvider.CreateScope();
                var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

                var createdAccount = await chartOfAccountsService.CreateAccountAsync(newAccount);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Account '{createdAccount.AccountName}' created successfully!", "Success", "CheckCircleOutline");
                });

                // Navigate back to Chart of Accounts page
                NavigateBack();
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error creating account: {ex.Message}", "Error", "ErrorOutline");
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
            _navigationService.NavigateTo<ChartOfAccountsPage>();
        }

        [RelayCommand]
        private void ClearForm()
        {
            AccountNumber = string.Empty;
            AccountName = string.Empty;
            SelectedAccountType = AccountTypes.FirstOrDefault() ?? string.Empty;
            SelectedAccountSubtype = string.Empty;
            Description = string.Empty;
            SelectedParentAccount = null;
            SelectedNormalBalance = "DEBIT";
            OpeningBalance = 0;
            OpeningBalanceDate = DateTime.Today;
            IsActive = true;
            IsSystemAccount = false;

            ClearValidationErrors();
        }

        #endregion

        #region Validation Methods

        private bool ValidateForm()
        {
            ClearValidationErrors();
            bool isValid = true;

            // Validate Account Number
            if (string.IsNullOrWhiteSpace(AccountNumber))
            {
                AccountNumberError = "Account number is required.";
                isValid = false;
            }
            else if (AccountNumber.Length > 20)
            {
                AccountNumberError = "Account number cannot exceed 20 characters.";
                isValid = false;
            }

            // Validate Account Name
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                AccountNameError = "Account name is required.";
                isValid = false;
            }
            else if (AccountName.Length > 255)
            {
                AccountNameError = "Account name cannot exceed 255 characters.";
                isValid = false;
            }

            // Validate Account Type
            if (string.IsNullOrWhiteSpace(SelectedAccountType))
            {
                AccountTypeError = "Account type is required.";
                isValid = false;
            }

            IsValid = isValid;
            return isValid;
        }

        private void ClearValidationErrors()
        {
            AccountNumberError = string.Empty;
            AccountNameError = string.Empty;
            AccountTypeError = string.Empty;
            IsValid = false;
        }

        #endregion

        #region Property Change Handlers

        partial void OnAccountNumberChanged(string value)
        {
            ValidateForm();
        }

        partial void OnAccountNameChanged(string value)
        {
            ValidateForm();
        }

        partial void OnSelectedAccountTypeChanged(string value)
        {
            ValidateForm();
        }

        #endregion

        #region Page Navigation Events

        private async void OnPageNavigated(object sender, Page page)
        {
            if (page is AddAccountPage)
            {
                await LoadDataAsync();
            }
        }

        #endregion
    }
}
