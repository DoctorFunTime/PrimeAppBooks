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
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Services;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class CustomerAnalyticsViewModel : ObservableObject
    {
        private readonly CustomerAnalyticsService _analyticsService;
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private bool _isLoading;


        [ObservableProperty]
        private int _totalCustomers;

        [ObservableProperty]
        private int _totalDebtors;

        [ObservableProperty]
        private int _writtenOffCustomers;

        [ObservableProperty]
        private bool _isReportsPanelVisible;

        [ObservableProperty]
        private decimal _totalReceivables;

        [ObservableProperty]
        private decimal _totalOverdue;

        [ObservableProperty]
        private double _averageDso;

        // Formatter for currency on axes
        public Func<double, string> Formatter { get; } = value => value.ToString("C0");
        public Func<double, string> PercentageFormatter { get; } = value => value.ToString("F1") + "%";

        public SeriesCollection AgingSeries { get; set; } = new();
        public string[] AgingLabels { get; set; } = { "0-30", "31-60", "61-90", "90+" };

        public ObservableCollection<CustomerSummaryMetrics> DebtorList { get; } = new();

        public CustomerAnalyticsViewModel(CustomerAnalyticsService analyticsService, INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _analyticsService = analyticsService;
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                var result = await _analyticsService.GetOverallAnalyticsAsync();
                var countStats = await _analyticsService.GetTotalStatsAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    DebtorList.Clear();
                    foreach (var m in result.Metrics) DebtorList.Add(m);

                    TotalCustomers = countStats.TotalCustomers;
                    TotalDebtors = countStats.TotalDebtors; // Should theoretically match metrics.Count if filtered correctly
                    WrittenOffCustomers = countStats.WrittenOffCustomers;
                    TotalReceivables = result.TotalOutstanding;
                    TotalOverdue = result.TotalOverdue;
                    AverageDso = result.Metrics.Where(m => m.AvgDaysToPay > 0).Select(m => m.AvgDaysToPay).DefaultIfEmpty(0).Average();

                    UpdateAgingChart(result.Metrics);
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
        private void ToggleReportsPanel()
        {
            IsReportsPanelVisible = !IsReportsPanelVisible;
        }

        [RelayCommand]
        private async Task OpenMasterSummary()
        {
            try
            {
                IsLoading = true;
                IsReportsPanelVisible = false;

                using var scope = _serviceProvider.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<CustomerAnalyticsService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await analyticsService.GetMasterSummaryDataAsync(null, null);
                var filePath = printService.GenerateAnalyticsSummaryPdf(data);

                printService.OpenPdfFile(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error printing master summary: {ex.Message}");
                System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Report Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task PrintReport(string reportType)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();
                var analyticsService = scope.ServiceProvider.GetRequiredService<CustomerAnalyticsService>();

                string filePath = null;

                switch (reportType)
                {
                    case "Simple":
                    case "Grouped":
                        var sortedList = DebtorList.OrderBy(c => c.CustomerName).ToList();
                        filePath = printService.GenerateDebtorReportPdf(sortedList, $"Customer Report ({reportType})");
                        break;
                    case "Plans":
                        var plans = await analyticsService.GetPaymentPlansAsync();
                        var activePlans = plans.Where(p => p.Status == "ACTIVE").ToList();
                        filePath = printService.GeneratePaymentPlansPdf(activePlans);
                        break;
                    case "Statement":
                        // Generic instruction: pick a customer first.
                        System.Windows.MessageBox.Show("Please select a customer from the list and use 'Print Statement' on their row.", "Instruction", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        return;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    printService.OpenPdfFile(filePath);
                }
                
                IsReportsPanelVisible = false;
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Error printing report: {ex.Message}");
                 System.Windows.MessageBox.Show($"Error generating report: {ex.Message}", "Report Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        [RelayCommand]
        private async Task PrintStudentStatement(CustomerSummaryMetrics customer)
        {
             if (customer == null) return;
             
             try
             {
                 IsLoading = true;
                 using var scope = _serviceProvider.CreateScope();
                 var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();
                 
                 // Reuse the Statement ViewModel logic to fetch data
                 var vm = scope.ServiceProvider.GetRequiredService<CustomerStatementPageViewModel>();
                 
                 // Initialize with default range (Year to Date) or last 30 days? 
                 // Default to current year or all time? The VM defaults to current year.
                 // Let's use current year as default.
                 await vm.Initialize(customer.CustomerId);

                  if (vm.Transactions.Any())
                  {
                      var filePath = printService.GenerateStatementPdf(
                          customer.CustomerName, 
                          vm.StartDate, 
                          vm.EndDate, 
                          vm.Transactions, 
                          0, // Opening balance is implicitly handled in the transactions list
                          vm.ClosingBalance);
 
                      printService.OpenPdfFile(filePath);
                  }
                 else
                 {
                     System.Windows.MessageBox.Show("No transactions found for this customer for the current period.", "No Data", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                 }
             }
             catch(Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Error printing statement: {ex.Message}");
                 System.Windows.MessageBox.Show($"Error printing statement: {ex.Message}", "Print Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
             }
             finally
             {
                 IsLoading = false;
             }
        }


        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();
    }
}