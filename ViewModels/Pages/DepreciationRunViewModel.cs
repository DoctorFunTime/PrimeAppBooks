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
    public partial class DepreciationRunViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();

        public ObservableCollection<DepreciationAssetRow> AssetRows { get; } = new();

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isPreviewed = false;
        [ObservableProperty] private DateTime _periodStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        [ObservableProperty] private DateTime _periodEndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month,
            DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month));
        [ObservableProperty] private decimal _totalDepreciation;
        [ObservableProperty] private int _eligibleAssetsCount;
        [ObservableProperty] private string _periodLabel = string.Empty;

        partial void OnPeriodEndDateChanged(DateTime value) => IsPreviewed = false;
        partial void OnPeriodStartDateChanged(DateTime value) => IsPreviewed = false;

        public class DepreciationAssetRow
        {
            public FixedAsset Asset { get; set; }
            public decimal CalculatedAmount { get; set; }
            public bool Include { get; set; } = true;
            public string Method => Asset?.DepreciationMethod == "STRAIGHT_LINE" ? "Straight Line" : "Reducing Balance";
        }

        public DepreciationRunViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
        }

        [RelayCommand]
        private async Task Preview()
        {
            try
            {
                IsLoading = true;
                IsPreviewed = false;
                AssetRows.Clear();

                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();

                var assets = await service.GetAllAssetsAsync();
                var activeAssets = assets.Where(a => a.Status == "ACTIVE").ToList();

                var startUtc = DateTime.SpecifyKind(PeriodStartDate, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(PeriodEndDate, DateTimeKind.Utc);

                foreach (var asset in activeAssets)
                {
                    var amount = service.CalculatePeriodDepreciation(asset, startUtc, endUtc);
                    AssetRows.Add(new DepreciationAssetRow { Asset = asset, CalculatedAmount = amount });
                }

                TotalDepreciation = AssetRows.Where(r => r.Include).Sum(r => r.CalculatedAmount);
                EligibleAssetsCount = AssetRows.Count(r => r.CalculatedAmount > 0);
                PeriodLabel = $"{PeriodStartDate:dd MMM yyyy} — {PeriodEndDate:dd MMM yyyy}";
                IsPreviewed = true;
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error generating preview: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void RecalcTotal()
        {
            TotalDepreciation = AssetRows.Where(r => r.Include).Sum(r => r.CalculatedAmount);
        }

        [RelayCommand]
        private async Task PostDepreciation()
        {
            if (!IsPreviewed)
            {
                _boxServices.ShowMessage("Please preview the depreciation run first.", "Validation", "Warning");
                return;
            }

            var includedIds = AssetRows.Where(r => r.Include && r.CalculatedAmount > 0)
                                       .Select(r => r.Asset.AssetId)
                                       .ToList();

            if (!includedIds.Any())
            {
                _boxServices.ShowMessage("No assets selected for this run.", "Validation", "Warning");
                return;
            }

            var confirm = _boxServices.ShowConfirmation(
                $"Post depreciation for {includedIds.Count} asset(s)?\nTotal: {TotalDepreciation:N2}\nPeriod: {PeriodLabel}\n\nThis will create a journal entry and cannot be undone.",
                "Confirm Depreciation Run", "Warning");

            if (!confirm) return;

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AssetService>();

                var startUtc = DateTime.SpecifyKind(PeriodStartDate, DateTimeKind.Utc);
                var endUtc = DateTime.SpecifyKind(PeriodEndDate, DateTimeKind.Utc);

                var (processed, total, journal) = await service.RunDepreciationAsync(startUtc, endUtc, includedIds);

                _boxServices.ShowMessage(
                    $"Depreciation posted successfully.\n{processed} asset(s) processed.\nTotal: {total:N2}\nJournal #{journal?.JournalNumber}",
                    "Success", "CheckCircleOutline");

                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error posting depreciation: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Cancel() => _navigationService.GoBack();
    }
}
