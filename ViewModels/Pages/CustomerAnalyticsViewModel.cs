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
    public partial class CustomerAnalyticsViewModel : ObservableObject
    {
        private readonly CustomerAnalyticsService _analyticsService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private decimal _totalReceivables;

        [ObservableProperty]
        private decimal _totalOverdue;

        [ObservableProperty]
        private double _averageDso;

        public SeriesCollection AgingSeries { get; set; } = new();
        public string[] AgingLabels { get; set; } = { "0-30", "31-60", "61-90", "90+" };

        public ObservableCollection<CustomerSummaryMetrics> DebtorList { get; } = new();

        public CustomerAnalyticsViewModel(CustomerAnalyticsService analyticsService, INavigationService navigationService)
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
                    DebtorList.Clear();
                    foreach (var m in metrics) DebtorList.Add(m);

                    TotalReceivables = metrics.Sum(m => m.TotalOutstanding);
                    TotalOverdue = metrics.Sum(m => m.OverdueAmount);
                    AverageDso = metrics.Where(m => m.AvgDaysToPay > 0).Select(m => m.AvgDaysToPay).DefaultIfEmpty(0).Average();

                    UpdateAgingChart(metrics);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading analytics: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateAgingChart(List<CustomerSummaryMetrics> metrics)
        {
            var b0 = metrics.Sum(m => m.AgingBuckets[0].Amount);
            var b1 = metrics.Sum(m => m.AgingBuckets[1].Amount);
            var b2 = metrics.Sum(m => m.AgingBuckets[2].Amount);
            var b3 = metrics.Sum(m => m.AgingBuckets[3].Amount);

            AgingSeries.Clear();
            AgingSeries.Add(new ColumnSeries
            {
                Title = "Amount",
                Values = new ChartValues<decimal> { b0, b1, b2, b3 },
                Fill = Brushes.DodgerBlue
            });
        }

        [RelayCommand]
        private void ViewCustomerDetails(CustomerSummaryMetrics customer)
        {
            if (customer != null)
            {
                _navigationService.NavigateTo<CollectionManagementPage>(customer.CustomerId);
            }
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();
    }
}