using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AddVendorPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty]
        private string _vendorName;

        [ObservableProperty]
        private string _vendorCode;

        [ObservableProperty]
        private string _contactPerson;

        [ObservableProperty]
        private string _email;

        [ObservableProperty]
        private string _phone;

        [ObservableProperty]
        private string _address;

        [ObservableProperty]
        private string _taxId;

        [ObservableProperty]
        private string _notes;

        [ObservableProperty]
        private bool _isActive = true;

        [ObservableProperty]
        private ChartOfAccount _selectedExpenseAccount;

        [ObservableProperty]
        private string _pageTitle = "Vendor Registration";

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private int? _editingVendorId;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<ChartOfAccount> ExpenseAccounts { get; } = new();

        public AddVendorPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            
            _ = LoadAccounts();
        }

        private async Task LoadAccounts()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var accounts = await context.ChartOfAccounts
                    .Where(a => a.AccountType == "EXPENSE" || a.AccountType == "ASSET")
                    .OrderBy(a => a.AccountNumber)
                    .ToListAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ExpenseAccounts.Clear();
                    foreach (var account in accounts) ExpenseAccounts.Add(account);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading accounts: {ex.Message}");
            }
        }

        public void Initialize(int vendorId)
        {
            if (vendorId <= 0)
            {
                InitializeNewVendor();
            }
            else
            {
                IsEditMode = true;
                EditingVendorId = vendorId;
                PageTitle = "Edit Vendor Details";
                _ = LoadVendorData(vendorId);
            }
        }

        private async void InitializeNewVendor()
        {
            IsEditMode = false;
            EditingVendorId = null;
            PageTitle = "Vendor Registration";

            using var scope = _serviceProvider.CreateScope();
            var vendorServices = scope.ServiceProvider.GetRequiredService<VendorServices>();
            VendorCode = await vendorServices.GenerateVendorCodeAsync();

            VendorName = string.Empty;
            ContactPerson = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Address = string.Empty;
            TaxId = string.Empty;
            Notes = string.Empty;
            IsActive = true;
            SelectedExpenseAccount = null;
        }

        private async Task LoadVendorData(int id)
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var vendorServices = scope.ServiceProvider.GetRequiredService<VendorServices>();
                var vendor = await vendorServices.GetVendorByIdAsync(id);

                if (vendor != null)
                {
                    VendorName = vendor.VendorName;
                    VendorCode = vendor.VendorCode;
                    ContactPerson = vendor.ContactPerson;
                    Email = vendor.Email;
                    Phone = vendor.Phone;
                    Address = vendor.Address;
                    TaxId = vendor.TaxId;
                    Notes = vendor.Notes;
                    IsActive = vendor.IsActive;

                    if (ExpenseAccounts.Count == 0) await LoadAccounts();
                    SelectedExpenseAccount = ExpenseAccounts.FirstOrDefault(a => a.AccountId == vendor.DefaultExpenseAccountId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading vendor data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void NavigateBack() => _navigationService.GoBack();

        [RelayCommand]
        private async Task SaveVendor()
        {
            if (string.IsNullOrWhiteSpace(VendorName) || string.IsNullOrWhiteSpace(VendorCode))
            {
                _messageBoxService.ShowMessage("Please fill in the required fields (Name and Code).", "Validation Error", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var vendorServices = scope.ServiceProvider.GetRequiredService<VendorServices>();

                var vendor = new Vendor
                {
                    VendorId = EditingVendorId ?? 0,
                    VendorName = VendorName,
                    VendorCode = VendorCode,
                    ContactPerson = ContactPerson,
                    Email = Email,
                    Phone = Phone,
                    Address = Address,
                    TaxId = TaxId,
                    Notes = Notes,
                    IsActive = IsActive,
                    DefaultExpenseAccountId = SelectedExpenseAccount?.AccountId
                };

                if (IsEditMode)
                {
                    await vendorServices.UpdateVendorAsync(vendor);
                    _messageBoxService.ShowMessage("Vendor updated successfully!", "Success", "CheckCircleOutline");
                }
                else
                {
                    await vendorServices.CreateVendorAsync(vendor);
                    _messageBoxService.ShowMessage("Vendor registered successfully!", "Success", "CheckCircleOutline");
                }

                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error saving vendor: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
