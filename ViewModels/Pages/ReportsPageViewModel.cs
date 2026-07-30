using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class ReportsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty]
        private bool _isGenerating = false;

        [ObservableProperty]
        private DateTime? _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [ObservableProperty]
        private DateTime? _endDate = DateTime.Now;

        private string _selectedDatePreset;
        private bool _isApplyingPreset;

        public string SelectedDatePreset
        {
            get => _selectedDatePreset;
            set
            {
                if (SetProperty(ref _selectedDatePreset, value))
                {
                    if (value != "Custom" && !string.IsNullOrEmpty(value))
                    {
                        _isApplyingPreset = true;
                        try
                        {
                            ApplyDatePreset(value);
                        }
                        finally
                        {
                            _isApplyingPreset = false;
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> DatePresets { get; } = new()
        {
            "Custom",
            "This Month",
            "Last Month",
            "This Quarter",
            "This Year"
        };

        partial void OnStartDateChanged(DateTime? value)
        {
            if (!_isApplyingPreset) SelectedDatePreset = "Custom";
        }

        partial void OnEndDateChanged(DateTime? value)
        {
            if (!_isApplyingPreset) SelectedDatePreset = "Custom";
        }

        public ObservableCollection<RecentReport> RecentReports { get; } = new();

        public ReportsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            // Set default date range to current month
            SelectedDatePreset = "This Month";

            // Load recent reports
            LoadRecentReports();
        }

        #region Date Range Management

        [RelayCommand]
        private void ApplyDateFilter()
        {
            // Date filter is already applied via bindings
            _messageBoxService.ShowMessage($"Date range set to {StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}", "Date Filter Applied", "CheckCircleOutline");
        }

        private void ApplyDatePreset(string preset)
        {
            var today = DateTime.Today;

            switch (preset)
            {
                case "This Month":
                    StartDate = new DateTime(today.Year, today.Month, 1);
                    EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
                    break;

                case "Last Month":
                    var lastMonth = today.AddMonths(-1);
                    StartDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
                    break;

                case "This Quarter":
                    var quarter = (today.Month - 1) / 3;
                    StartDate = new DateTime(today.Year, quarter * 3 + 1, 1);
                    EndDate = StartDate.Value.AddMonths(3).AddDays(-1);
                    break;

                case "This Year":
                    StartDate = new DateTime(today.Year, 1, 1);
                    EndDate = new DateTime(today.Year, 12, 31);
                    break;

                default:
                    // Custom - don't change dates
                    break;
            }
        }

        #endregion Date Range Management

        #region Report Generation Commands

        [RelayCommand]
        private async Task GenerateBalanceSheet()
        {
            await GenerateReportAsync("Balance Sheet", async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var reportService = scope.ServiceProvider.GetRequiredService<ReportGenerationService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await reportService.GenerateBalanceSheetAsync(EndDate ?? DateTime.Now);
                var filePath = printService.GenerateBalanceSheetPdf(data);
                
                printService.OpenPdfFile(filePath);
                AddRecentReport("Balance Sheet", "📊", filePath);
            });
        }

        [RelayCommand]
        private async Task GenerateIncomeStatement()
        {
            await GenerateReportAsync("Income Statement", async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var reportService = scope.ServiceProvider.GetRequiredService<ReportGenerationService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await reportService.GenerateIncomeStatementAsync(StartDate ?? DateTime.Now.AddMonths(-1), EndDate ?? DateTime.Now);
                var filePath = printService.GenerateIncomeStatementPdf(data);

                printService.OpenPdfFile(filePath);
                AddRecentReport("Income Statement", "💰", filePath);
            });
        }

        [RelayCommand]
        private async Task GenerateCashFlow()
        {
            await GenerateReportAsync("Cash Flow Statement", async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var reportService = scope.ServiceProvider.GetRequiredService<ReportGenerationService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await reportService.GenerateCashFlowAsync(StartDate ?? DateTime.Now.AddMonths(-1), EndDate ?? DateTime.Now);
                var filePath = printService.GenerateCashFlowPdf(data);

                printService.OpenPdfFile(filePath);
                AddRecentReport("Cash Flow Statement", "🌊", filePath);
            });
        }

        [RelayCommand]
        private async Task GenerateTrialBalance()
        {
            await GenerateReportAsync("Trial Balance", async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var reportService = scope.ServiceProvider.GetRequiredService<ReportGenerationService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await reportService.GenerateTrialBalanceAsync(EndDate ?? DateTime.Now);
                var filePath = printService.GenerateTrialBalancePdf(data);

                printService.OpenPdfFile(filePath);
                AddRecentReport("Trial Balance", "⚖️", filePath);
            });
        }

        [RelayCommand]
        private async Task GenerateArAging()
        {
            await GenerateReportAsync("A/R Aging", async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<CustomerAnalyticsService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await analyticsService.GetOverallAnalyticsAsync();
                var filePath = printService.GenerateDebtorReportPdf(data.Metrics, "A/R Aging Summary");

                printService.OpenPdfFile(filePath);
                AddRecentReport("A/R Aging Summary", "📊", filePath);
            });
        }

        [RelayCommand]
        private async Task GenerateAssetRegister()
        {
            await GenerateReportAsync("Asset Register", async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var reportService = scope.ServiceProvider.GetRequiredService<ReportGenerationService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await reportService.GenerateAssetRegisterAsync(EndDate ?? DateTime.Now);
                var filePath = printService.GenerateAssetRegisterPdf(data);

                printService.OpenPdfFile(filePath);
                AddRecentReport("Asset Register", "Asset", filePath);
            });
        }

        [RelayCommand]
        private async Task GenerateApAging()
        {
             _messageBoxService.ShowMessage("A/P Aging coming soon!", "Info", "InformationOutline");
        }

        [RelayCommand]
        private async Task GenerateReceivablesSummary()
        {
            await GenerateReportAsync("Receivables Master Summary", async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var analyticsService = scope.ServiceProvider.GetRequiredService<CustomerAnalyticsService>();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();

                var data = await analyticsService.GetMasterSummaryDataAsync(StartDate, EndDate);
                var filePath = printService.GenerateAnalyticsSummaryPdf(data);

                printService.OpenPdfFile(filePath);
                AddRecentReport("Receivables Master Summary", "📊", filePath);
            });
        }

        [RelayCommand]
        private async Task GenerateTaxReport()
        {
            await GenerateReportAsync("Tax Summary", async () =>
            {
                 _messageBoxService.ShowMessage("VAT / Tax Summary parsing logic is being finalized. Please check back shortly.", "Coming Soon", "InformationOutline");
            });
        }

        [RelayCommand]
        private void OpenRecent(RecentReport report)
        {
            if (report != null && File.Exists(report.FilePath))
            {
                using var scope = _serviceProvider.CreateScope();
                var printService = scope.ServiceProvider.GetRequiredService<ReportPrintingService>();
                printService.OpenPdfFile(report.FilePath);
            }
            else
            {
                _messageBoxService.ShowMessage("File no longer exists.", "Error", "ErrorOutline");
            }
        }

        [RelayCommand]
        private async Task PrintAll()
        {
            _messageBoxService.ShowMessage("Print All feature coming soon!", "Info", "InformationOutline");
        }

        #endregion Report Generation Commands

        #region Helper Methods

        private async Task GenerateReportAsync(string reportName, Func<Task> generateAction)
        {
            IsGenerating = true;
            try
            {
                await generateAction();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error generating {reportName}: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private string GetSaveFilePath(string defaultFileName)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = defaultFileName,
                DefaultExt = ".pdf",
                AddExtension = true
            };

            if (saveDialog.ShowDialog() == true)
            {
                return saveDialog.FileName;
            }

            return null;
        }

        private void AddRecentReport(string name, string icon, string filePath)
        {
            var report = new RecentReport
            {
                Name = name,
                Icon = icon,
                GeneratedDate = DateTime.Now,
                PageCount = 1,
                FilePath = filePath,
                ReportType = name
            };

            // Add to beginning of list
            RecentReports.Insert(0, report);

            // Keep only last 10 reports
            while (RecentReports.Count > 10)
            {
                RecentReports.RemoveAt(RecentReports.Count - 1);
            }
        }

        private void LoadRecentReports()
        {
            // In a real app, load from database or settings file
            // For now, just show empty list
        }

        #endregion Helper Methods
    }
}
