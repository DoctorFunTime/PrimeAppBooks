using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class InventoryListPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();

        public ObservableCollection<InventoryItem> InventoryItems { get; } = new();
        public ObservableCollection<InventoryItem> FilteredItems { get; } = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private InventoryItem _selectedItem;

        // Statistics
        [ObservableProperty] private int _totalItemsCount;
        [ObservableProperty] private int _lowStockCount;
        [ObservableProperty] private decimal _totalInventoryValue;
        [ObservableProperty] private string _resultsSummary = "No items found";

        // Filters
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value)) ApplyFilters();
            }
        }

        private bool _showLowStockOnly;
        public bool ShowLowStockOnly
        {
            get => _showLowStockOnly;
            set
            {
                if (SetProperty(ref _showLowStockOnly, value)) ApplyFilters();
            }
        }

        public InventoryListPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _navigationService.PageNavigated += OnPageNavigated;
            _ = LoadData();
        }

        private async void OnPageNavigated(object sender, System.Windows.Controls.Page page)
        {
            // Assuming we will create a page class named InventoryListPage
            if (page.GetType().Name == "InventoryListPage")
            {
                await LoadData();
            }
        }

        [RelayCommand]
        private async Task LoadData()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<InventoryService>();

                var list = await service.GetAllItemsAsync();
                
                InventoryItems.Clear();
                foreach (var item in list)
                    InventoryItems.Add(item);

                UpdateStatistics();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading inventory: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateStatistics()
        {
            TotalItemsCount = InventoryItems.Count;
            LowStockCount = InventoryItems.Count(i => i.QuantityOnHand <= i.LowStockThreshold);
            TotalInventoryValue = InventoryItems.Sum(i => i.QuantityOnHand * i.PurchaseCost);
        }

        private void ApplyFilters()
        {
            var filtered = InventoryItems.AsEnumerable();

            // Low Stock Filter
            if (ShowLowStockOnly)
            {
                filtered = filtered.Where(i => i.QuantityOnHand <= i.LowStockThreshold);
            }

            // Text Search
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(i =>
                    (i.ItemName != null && i.ItemName.ToLower().Contains(search)) ||
                    (i.SKU != null && i.SKU.ToLower().Contains(search)) ||
                    (i.Description != null && i.Description.ToLower().Contains(search)));
            }

            FilteredItems.Clear();
            foreach (var item in filtered.OrderBy(i => i.ItemName))
            {
                FilteredItems.Add(item);
            }

            ResultsSummary = $"{FilteredItems.Count} items found";
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            ShowLowStockOnly = false;
            ApplyFilters();
        }

        [RelayCommand]
        private void CreateNewItem()
        {
            _navigationService.NavigateTo<AddEditInventoryPage>();
        }

        [RelayCommand]
        private void EditItem(InventoryItem item)
        {
            if (item != null)
            {
                _navigationService.NavigateTo<AddEditInventoryPage>(item.ItemId);
            }
        }

        [RelayCommand]
        private async Task DeleteItem(InventoryItem item)
        {
            if (item == null) return;

            var confirmed = _boxServices.ShowConfirmation(
                $"Are you sure you want to delete {item.ItemName}?\nThis will hide it from new invoices but keep history.",
                "Confirm Delete",
                "Delete");

            if (!confirmed) return;

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<InventoryService>();

                await service.DeleteItemAsync(item.ItemId);
                
                _boxServices.ShowMessage("Item deleted successfully.", "Success", "Delete");
                await LoadData();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error deleting item: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
