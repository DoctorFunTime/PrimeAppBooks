using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AddAccountPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        private ChartOfAccount _editingAccount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageTitle))]
        [NotifyPropertyChangedFor(nameof(PageSubtitle))]
        [NotifyPropertyChangedFor(nameof(SaveButtonText))]
        private bool _isEditing = false;

        #region Form Properties

        [ObservableProperty]
        private string _accountNumber;

        [ObservableProperty]
        private string _accountName;

        [ObservableProperty]
        private string _selectedAccountType;

        [ObservableProperty]
        private string _selectedAccountSubtype;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private ChartOfAccount _selectedParentAccount;

        [ObservableProperty]
        private string _selectedNormalBalance;

        [ObservableProperty]
        private decimal _openingBalance;

        [ObservableProperty]
        private DateTime _openingBalanceDate = DateTime.Today;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private bool _isSystemAccount = false;

        [ObservableProperty]
        private bool _isLoading = false;

        #endregion Form Properties

        #region Observable Properties

        public string PageTitle => IsEditing ? "Edit Account" : "Add New Account";
        public string PageSubtitle => IsEditing ? "Modify existing account details" : "Create a new account in your chart of accounts";
        public string SaveButtonText => IsEditing ? "💾 Update Account" : "💾 Save Account";

        #endregion Observable Properties

        #region Collections

        public ObservableCollection<string> AccountTypes { get; } = new();
        public ObservableCollection<string> AccountSubtypes { get; } = new();
        public ObservableCollection<ChartOfAccount> ParentAccounts { get; } = new();

        public ObservableCollection<string> NormalBalanceOptions { get; } = new()
        {
            "DEBIT",
            "CREDIT"
        };

        #endregion Collections

        #region Validation Properties

        [ObservableProperty]
        private string _accountNumberError;

        [ObservableProperty]
        private string _accountNameError;

        [ObservableProperty]
        private string _accountTypeError;

        [ObservableProperty]
        private bool _isValid = false;

        #endregion Validation Properties

        public AddAccountPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            // Initialize with default values
            LoadDataAsync();
        }

        public async Task Initialize(ChartOfAccount accountToEdit)
        {
            if (accountToEdit != null)
            {
                IsEditing = true;
                _editingAccount = accountToEdit;

                // Populate form with account data
                AccountNumber = accountToEdit.AccountNumber;
                AccountName = accountToEdit.AccountName;
                SelectedAccountType = accountToEdit.AccountType;
                SelectedAccountSubtype = accountToEdit.AccountSubtype;
                Description = accountToEdit.Description;
                SelectedNormalBalance = accountToEdit.NormalBalance;
                OpeningBalance = accountToEdit.OpeningBalance;
                OpeningBalanceDate = accountToEdit.OpeningBalanceDate ?? DateTime.Today;
                IsActive = accountToEdit.IsActive;
                IsSystemAccount = accountToEdit.IsSystemAccount;

                // Parent account selection needs to happen after data load
                await LoadDataAsync();
                SelectedParentAccount = ParentAccounts.FirstOrDefault(p => p.AccountId == accountToEdit.ParentAccountId);
            }
            else
            {
                ClearForm();
                await LoadDataAsync();
            }
        }

        private async Task LoadDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

            try
            {
                var types = await chartOfAccountsService.GetAccountTypesAsync();
                var subtypes = await chartOfAccountsService.GetAccountSubtypesAsync();
                var parents = await chartOfAccountsService.GetParentAccountsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AccountTypes.Clear();
                    foreach (var type in types) AccountTypes.Add(type);

                    AccountSubtypes.Clear();
                    foreach (var subtype in subtypes) AccountSubtypes.Add(subtype);

                    ParentAccounts.Clear();
                    ParentAccounts.Add(new ChartOfAccount { AccountId = 0, AccountName = "None", AccountNumber = "" });
                    foreach (var parent in parents)
                    {
                        // Don't allow selecting self as parent when editing
                        if (IsEditing && _editingAccount != null && parent.AccountId == _editingAccount.AccountId)
                            continue;

                        ParentAccounts.Add(parent);
                    }

                    // Set default parent if not set
                    if (SelectedParentAccount == null)
                    {
                        SelectedParentAccount = ParentAccounts.FirstOrDefault(p => p == null);
                    }

                    ValidateForm();
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading data: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        #region Commands

        [RelayCommand]
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }

        [RelayCommand]
        private async Task SaveAccountAsync()
        {
            ValidateForm();
            if (!IsValid) return;

            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var chartOfAccountsService = scope.ServiceProvider.GetRequiredService<ChartOfAccountsServices>();

                if (IsEditing && _editingAccount != null)
                {
                    // Update existing account
                    _editingAccount.AccountNumber = AccountNumber.Trim();
                    _editingAccount.AccountName = AccountName.Trim();
                    _editingAccount.AccountType = SelectedAccountType;
                    _editingAccount.AccountSubtype = string.IsNullOrEmpty(SelectedAccountSubtype) ? null : SelectedAccountSubtype;
                    _editingAccount.Description = string.IsNullOrEmpty(Description) ? null : Description.Trim();
                    _editingAccount.ParentAccountId = SelectedParentAccount?.AccountId > 0 ? SelectedParentAccount.AccountId : null;
                    _editingAccount.NormalBalance = SelectedNormalBalance;
                    _editingAccount.OpeningBalance = OpeningBalance;
                    _editingAccount.OpeningBalanceDate = OpeningBalanceDate;
                    _editingAccount.IsActive = IsActive;
                    _editingAccount.IsSystemAccount = IsSystemAccount;
                    _editingAccount.UpdatedAt = DateTime.UtcNow;
                    // TODO: Update ModifiedBy

                    await chartOfAccountsService.UpdateAccountAsync(_editingAccount);

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage("Account updated successfully!", "Success", "CheckCircleOutline");
                        _navigationService.GoBack();
                    });
                }
                else
                {
                    // Create new account
                    var newAccount = new ChartOfAccount
                    {
                        AccountNumber = AccountNumber.Trim(),
                        AccountName = AccountName.Trim(),
                        AccountType = SelectedAccountType,
                        AccountSubtype = string.IsNullOrEmpty(SelectedAccountSubtype) ? null : SelectedAccountSubtype,
                        Description = string.IsNullOrEmpty(Description) ? null : Description.Trim(),
                        ParentAccountId = SelectedParentAccount?.AccountId > 0 ? SelectedParentAccount.AccountId : null,
                        NormalBalance = SelectedNormalBalance,
                        OpeningBalance = OpeningBalance,
                        OpeningBalanceDate = OpeningBalanceDate,
                        IsActive = IsActive,
                        IsSystemAccount = IsSystemAccount,
                        CreatedBy = 1 // TODO: Get from current user
                    };

                    var createdAccount = await chartOfAccountsService.CreateAccountAsync(newAccount);

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _messageBoxService.ShowMessage($"Account '{createdAccount.AccountName}' created successfully!", "Success", "CheckCircleOutline");
                        _navigationService.GoBack();
                    });
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error saving account: {ex.Message}", "Error", "ErrorOutline");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ClearForm()
        {
            IsEditing = false;
            _editingAccount = null;
            AccountNumber = string.Empty;
            AccountName = string.Empty;
            SelectedAccountType = null;
            SelectedAccountSubtype = null;
            Description = string.Empty;
            SelectedParentAccount = ParentAccounts.FirstOrDefault();
            SelectedNormalBalance = null;
            OpeningBalance = 0;
            OpeningBalanceDate = DateTime.Today;
            IsActive = true;
            IsSystemAccount = false;
            ClearValidationErrors();
        }

        #endregion Commands

        #region Validation Methods

        private void ValidateForm()
        {
            ClearValidationErrors();
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(AccountNumber))
            {
                AccountNumberError = "Account Number is required";
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(AccountName))
            {
                AccountNameError = "Account Name is required";
                isValid = false;
            }

            if (string.IsNullOrEmpty(SelectedAccountType))
            {
                AccountTypeError = "Account Type is required";
                isValid = false;
            }

            IsValid = isValid;
        }

        private void ClearValidationErrors()
        {
            AccountNumberError = string.Empty;
            AccountNameError = string.Empty;
            AccountTypeError = string.Empty;
            IsValid = false;
        }

        #endregion Validation Methods

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(AccountNumber) ||
                e.PropertyName == nameof(AccountName) ||
                e.PropertyName == nameof(SelectedAccountType))
            {
                ValidateForm();
            }
        }
    }
}