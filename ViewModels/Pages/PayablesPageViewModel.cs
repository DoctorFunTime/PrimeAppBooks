using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Wpf;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class PayablesPageViewModel : ObservableObject
    {
        private readonly VendorAnalyticsService _analyticsService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private decimal _totalPayables;

        [ObservableProperty]
        private decimal _totalOverdue;

        [ObservableProperty]
        private string _resultsSummary = "No data found";

        public SeriesCollection AgingSeries { get; set; } = new();
        public string[] AgingLabels { get; set; } = { "0-30", "31-60", "61-90", "90+" };

        public ObservableCollection<VendorSummaryMetrics> VendorList { get; } = new();

        public PayablesPageViewModel(VendorAnalyticsService analyticsService, INavigationService navigationService)
        {
            _analyticsService = analyticsService;
            _navigationService = navigationService;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                var metrics = await _analyticsService.GetOverallAnalyticsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    VendorList.Clear();
                    foreach (var m in metrics) VendorList.Add(m);

                    TotalPayables = metrics.Sum(m => m.TotalOutstanding);
                    TotalOverdue = metrics.Sum(m => m.OverdueAmount);
                    ResultsSummary = $"{VendorList.Count} vendors with balances";

                    UpdateAgingChart(metrics);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading payables analytics: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateAgingChart(List<VendorSummaryMetrics> metrics)
        {
            var b0 = metrics.Sum(m => m.AgingBuckets[0].Amount);
            var b1 = metrics.Sum(m => m.AgingBuckets[1].Amount);
            var b2 = metrics.Sum(m => m.AgingBuckets[2].Amount);
            var b3 = metrics.Sum(m => m.AgingBuckets[3].Amount);

            AgingSeries.Clear();
            AgingSeries.Add(new ColumnSeries
            {
                Title = "Payables Amount",
                Values = new ChartValues<decimal> { b0, b1, b2, b3 },
                Fill = Brushes.Crimson
            });
        }

        [RelayCommand]
        private void ViewVendorDetails(VendorSummaryMetrics vendor)
        {
            if (vendor != null)
            {
                // Future: Navigate to a Vendor History/Collection specialized page
                // For now, go to the AddVendorPage with the ID for editing
                _navigationService.NavigateTo<AddVendorPage>(vendor.VendorId);
            }
        }

        [RelayCommand]
        private void CreatePurchaseInvoice()
        {
            _navigationService.NavigateTo<AddPurchaseInvoicePage>();
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();
    }
}
