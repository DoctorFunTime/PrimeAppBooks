using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;
using System.Diagnostics;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AddEditInventoryPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();
        private int _itemId = 0; // 0 = New Mode

        public string PageTitle => _itemId == 0 ? "New Inventory Item" : "Edit Inventory Item";

        [ObservableProperty]
        private bool _isLoading;

        // Form Properties
        [ObservableProperty] private string _sku;
        [ObservableProperty] private string _itemName;
        [ObservableProperty] private string _description;
        [ObservableProperty] private decimal _salePrice;
        [ObservableProperty] private decimal _purchaseCost;
        [ObservableProperty] private decimal _quantityOnHand;
        [ObservableProperty] private decimal _lowStockThreshold = 5;

        // Account Selection
        public ObservableCollection<ChartOfAccount> IncomeAccounts { get; } = new();
        public ObservableCollection<ChartOfAccount> ExpenseAccounts { get; } = new();
        public ObservableCollection<ChartOfAccount> AssetAccounts { get; } = new();

        [ObservableProperty] private ChartOfAccount _selectedIncomeAccount;
        [ObservableProperty] private ChartOfAccount _selectedExpenseAccount;
        [ObservableProperty] private ChartOfAccount _selectedAssetAccount;

        public AddEditInventoryPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _= LoadAccounts();
        }

        public async Task InitializeAsync(object parameter)
        {
            if (parameter is int id && id > 0)
            {
                _itemId = id;
            }
            else
            {
                _itemId = 0;
                ClearForm();
            }

            await LoadAccounts();

            if (_itemId > 0) await LoadItem(_itemId);

            OnPropertyChanged(nameof(PageTitle));
        }

        private async Task LoadAccounts()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var allAccounts = await context.ChartOfAccounts.Where(a => a.IsActive).ToListAsync();

                IncomeAccounts.Clear();
                foreach (var a in allAccounts.Where(a => a.AccountType == "REVENUE")) IncomeAccounts.Add(a);

                ExpenseAccounts.Clear();
                foreach (var a in allAccounts.Where(a => a.AccountType == "EXPENSE")) ExpenseAccounts.Add(a);

                AssetAccounts.Clear();
                foreach (var a in allAccounts.Where(a => a.AccountType == "ASSET")) AssetAccounts.Add(a);

                // Default Selections if creating new
                if (_itemId == 0)
                {
                    SelectedIncomeAccount = IncomeAccounts.FirstOrDefault(a => a.AccountName.Contains("Sales") || a.AccountNumber.StartsWith("4"));
                    SelectedExpenseAccount = ExpenseAccounts.FirstOrDefault(a => a.AccountSubtype == "COGS" || a.AccountNumber.StartsWith("5"));
                    SelectedAssetAccount = AssetAccounts.FirstOrDefault(a => a.AccountName.Contains("Inventory") || a.AccountNumber.StartsWith("1"));
                }
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading accounts: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        private async Task LoadItem(int id)
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<InventoryService>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>(); // Need context for account objects lookup

                var item = await service.GetItemByIdAsync(id);
                if (item != null)
                {
                    Sku = item.SKU;
                    ItemName = item.ItemName;
                    Description = item.Description;
                    SalePrice = item.SalePrice;
                    PurchaseCost = item.PurchaseCost;
                    QuantityOnHand = item.QuantityOnHand;
                    LowStockThreshold = item.LowStockThreshold;

                    SelectedIncomeAccount = IncomeAccounts.FirstOrDefault(a => a.AccountId == item.IncomeAccountId);
                    SelectedExpenseAccount = ExpenseAccounts.FirstOrDefault(a => a.AccountId == item.ExpenseAccountId);
                    SelectedAssetAccount = AssetAccounts.FirstOrDefault(a => a.AccountId == item.AssetAccountId);
                }
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading item: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearForm()
        {
            Sku = string.Empty;
            ItemName = string.Empty;
            Description = string.Empty;
            SalePrice = 0;
            PurchaseCost = 0;
            QuantityOnHand = 0;
            LowStockThreshold = 5;
            // Accounts remain detailed defaults
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Sku) || string.IsNullOrWhiteSpace(ItemName))
            {
                _boxServices.ShowMessage("SKU and Name are required.", "Validation Error", "Warning");
                return;
            }

            if (SelectedIncomeAccount == null || SelectedExpenseAccount == null || SelectedAssetAccount == null)
            {
                _boxServices.ShowMessage("Please select all mapped accounts (Sales, COGS, Inventory Asset).", "Validation Error", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<InventoryService>();

                var item = new InventoryItem
                {
                    ItemId = _itemId,
                    SKU = Sku,
                    ItemName = ItemName,
                    Description = Description,
                    SalePrice = SalePrice,
                    PurchaseCost = PurchaseCost,
                    QuantityOnHand = QuantityOnHand, // Note: Direct edit of Qty is allowed here for setup, but in production strictly via transaction
                    LowStockThreshold = LowStockThreshold,
                    IncomeAccountId = SelectedIncomeAccount.AccountId,
                    ExpenseAccountId = SelectedExpenseAccount.AccountId,
                    AssetAccountId = SelectedAssetAccount.AccountId,
                    IsActive = true
                };

                if (_itemId == 0)
                {
                    var created = await service.CreateItemAsync(item);

                    // If opening quantity is set, write an opening stock journal entry
                    // Dr Inventory Asset / Cr COGS (adjustment account as opening equity proxy)
                    if (QuantityOnHand > 0 && PurchaseCost > 0)
                    {
                        await service.RecordOpeningStockAsync(
                            itemId: created.ItemId,
                            userId: 1
                        );
                    }
                }
                else
                {
                    await service.UpdateItemAsync(item);
                }

                _boxServices.ShowMessage("Item saved successfully.", "Success", "CheckCircleOutline");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error saving item: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _navigationService.GoBack();
        }
    }
}
