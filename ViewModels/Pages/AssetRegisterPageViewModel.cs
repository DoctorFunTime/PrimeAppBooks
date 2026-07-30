using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AssetRegisterPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();

        public ObservableCollection<FixedAsset> AllAssets { get; } = new();
        public ObservableCollection<FixedAsset> FilteredAssets { get; } = new();
        public ObservableCollection<AssetCategory> Categories { get; } = new();

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private FixedAsset _selectedAsset;

        // Summary stats
        [ObservableProperty] private int _totalAssetsCount;
        [ObservableProperty] private decimal _totalCost;
        [ObservableProperty] private decimal _totalBookValue;
        [ObservableProperty] private int _fullyDepreciatedCount;

        // Filters
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) ApplyFilters(); }
        }

        private string _statusFilter = "ALL";
        public string StatusFilter
        {
            get => _statusFilter;
            set { if (SetProperty(ref _statusFilter, value)) ApplyFilters(); }
        }

        public string[] StatusOptions { get; } = { "ALL", "ACTIVE", "CWIP", "FULLY_DEPRECIATED", "DISPOSED" };

        public AssetRegisterPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _navigationService.PageNavigated += OnPageNavigated;
            _ = LoadData();
        }

        private async void OnPageNavigated(object sender, System.Windows.Controls.Page page)
        {
            if (page is AssetRegisterPage)
                await LoadData();
        }

        [RelayCommand]
        private async Task LoadData()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();

                var assets = await service.GetAllAssetsAsync();
                AllAssets.Clear();
                foreach (var a in assets) AllAssets.Add(a);

                await service.EnsureDefaultCategoriesAsync();
                var cats = await service.GetAllCategoriesAsync();
                Categories.Clear();
                foreach (var c in cats) Categories.Add(c);

                var stats = await service.GetSummaryStatsAsync();
                TotalAssetsCount = stats.TotalAssets;
                TotalCost = stats.TotalCost;
                TotalBookValue = stats.TotalBookValue;
                FullyDepreciatedCount = stats.FullyDepreciatedCount;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading assets: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            FilteredAssets.Clear();
            var query = AllAssets.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(a =>
                    a.AssetName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    a.AssetCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (a.Category?.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

            if (StatusFilter != "ALL")
                query = query.Where(a => a.Status == StatusFilter);

            foreach (var a in query) FilteredAssets.Add(a);
        }

        [RelayCommand]
        private void AddNewAsset()
        {
            _navigationService.NavigateTo<AddEditAssetPage>(null);
        }

        [RelayCommand]
        private void EditAsset(FixedAsset asset)
        {
            if (asset == null) return;
            _navigationService.NavigateTo<AddEditAssetPage>(asset.AssetId);
        }

        [RelayCommand]
        private void RunDepreciation()
        {
            _navigationService.NavigateTo<DepreciationRunPage>(null);
        }

        [RelayCommand]
        private async Task DisposeAsset(FixedAsset asset)
        {
            if (asset == null) return;

            // CWIP assets have never been activated — they must not go through the
            // standard disposal flow. Handle them with a dedicated delete path.
            if (asset.Status == "CWIP")
            {
                var confirm = _boxServices.ShowConfirmation(
                    $"Delete CWIP asset \"{asset.AssetName}\"?\n\n" +
                    $"This asset has not been capitalised yet.\n" +
                    $"• If a staging journal was posted, it will be automatically reversed.\n" +
                    $"• If no journal exists (legacy record), the record will simply be removed.\n\n" +
                    $"The asset will be permanently deleted from the register.",
                    "Delete CWIP Asset", "Warning");

                if (!confirm) return;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<AssetService>();
                    var message = await service.DeleteCwipAssetAsync(
                        asset.AssetId,
                        MyAppContext.CurrentLogin?.UserId ?? 1);
                    _boxServices.ShowMessage(message, "Deleted", "CheckCircleOutline");
                    await LoadData();
                }
                catch (Exception ex)
                {
                    _boxServices.ShowMessage($"Error: {ex.Message}", "Error", "ErrorOutline");
                }
                return;
            }

            // Standard disposal flow for ACTIVE / FULLY_DEPRECIATED assets
            var confirmDispose = _boxServices.ShowConfirmation(
                $"Are you sure you want to dispose of \"{asset.AssetName}\"?\n\nThis will post a journal entry and mark the asset as DISPOSED.",
                "Confirm Disposal", "Warning");

            if (!confirmDispose) return;

            _navigationService.NavigateTo<AddEditAssetPage>($"DISPOSE:{asset.AssetId}");
        }

        [RelayCommand]
        private async Task DeactivateAsset(FixedAsset asset)
        {
            if (asset == null) return;

            var confirm = _boxServices.ShowConfirmation(
                $"Remove \"{asset.AssetName}\" from the register?",
                "Confirm Remove", "Warning");

            if (!confirm) return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();
                await service.DeactivateAssetAsync(asset.AssetId);
                _boxServices.ShowMessage("Asset removed.", "Success", "CheckCircleOutline");
                await LoadData();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        [RelayCommand]
        private async Task CapitalizeAsset(FixedAsset asset)
        {
            if (asset == null || asset.Status != "CWIP") return;

            var confirm = _boxServices.ShowConfirmation(
                $"Capitalise \"{asset.AssetName}\" from CWIP?\n\n" +
                $"This will:\n" +
                $"  • Post: Dr {asset.AssetAccount?.AccountNumber} {asset.AssetAccount?.AccountName} / Cr CWIP\n" +
                $"  • Set status to ACTIVE\n" +
                $"  • Begin depreciation from today ({DateTime.Today:dd MMM yyyy})\n\n" +
                $"Continue?",
                "Confirm Capitalisation", "Information");

            if (!confirm) return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();
                await service.CapitalizeAssetAsync(asset.AssetId, DateTime.Today);
                _boxServices.ShowMessage(
                    $"\"{asset.AssetName}\" has been capitalised and is now ACTIVE.",
                    "Capitalised", "CheckCircleOutline");
                await LoadData();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error capitalising asset: {ex.Message}", "Error", "ErrorOutline");
            }
        }
    }
}
