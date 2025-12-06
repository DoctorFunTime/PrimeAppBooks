using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using System;
using System.Collections.ObjectModel;
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
        private DateTime? _startDate = DateTime.Now.AddMonths(-1);

        [ObservableProperty]
        private DateTime? _endDate = DateTime.Now;

        [ObservableProperty]
        private string _selectedDatePreset = "This Month";

        [ObservableProperty]
        private string _selectedFormat = "Print";

        [ObservableProperty]
        private int _copies = 1;

        public ObservableCollection<RecentReport> RecentReports { get; } = new();

        public ReportsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            // Set default date range to current month
            ApplyDatePreset("This Month");

            // Load recent reports (in a real app, this would be from a database or file)
            LoadRecentReports();
        }

        #region Date Range Management

        partial void OnSelectedDatePresetChanged(string value)
        {
            if (!string.IsNullOrEmpty(value) && value != "Custom")
            {
                ApplyDatePreset(value);
            }
        }

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

        #endregion

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

                if (SelectedFormat == "PDF")
                {
                    var filePath = GetSaveFilePath("Balance_Sheet.pdf");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        printService.ExportBalanceSheetToPdf(data, filePath);
                        printService.OpenPdfFile(filePath);
                        AddRecentReport("Balance Sheet", "📊", filePath);
                    }
                }
                else // Print
                {
                    var document = printService.GenerateBalanceSheetDocument(data);
                    printService.PrintDocument(document, "Balance Sheet");
                }
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

                if (SelectedFormat == "PDF")
                {
                    var filePath = GetSaveFilePath("Income_Statement.pdf");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        printService.ExportIncomeStatementToPdf(data, filePath);
                        printService.OpenPdfFile(filePath);
                        AddRecentReport("Income Statement", "💰", filePath);
                    }
                }
                else // Print
                {
                    var document = printService.GenerateIncomeStatementDocument(data);
                    printService.PrintDocument(document, "Income Statement");
                }
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

                if (SelectedFormat == "PDF")
                {
                    var filePath = GetSaveFilePath("Cash_Flow.pdf");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        // Note: Need to add PDF export for Cash Flow in ReportPrintingService
                        _messageBoxService.ShowMessage("Cash Flow PDF export coming soon!", "Info", "InformationOutline");
                    }
                }
                else // Print
                {
                    var document = printService.GenerateCashFlowDocument(data);
                    printService.PrintDocument(document, "Cash Flow Statement");
                }
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

                if (SelectedFormat == "PDF")
                {
                    var filePath = GetSaveFilePath("Trial_Balance.pdf");
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        printService.ExportTrialBalanceToPdf(data, filePath);
                        printService.OpenPdfFile(filePath);
                        AddRecentReport("Trial Balance", "⚖️", filePath);
                    }
                }
                else // Print
                {
                    var document = printService.GenerateTrialBalanceDocument(data);
                    printService.PrintDocument(document, "Trial Balance");
                }
            });
        }

        [RelayCommand]
        private async Task GenerateReport()
        {
            // Generic generate based on selected report type
            await GenerateBalanceSheet();
        }

        [RelayCommand]
        private async Task PrintAll()
        {
            _messageBoxService.ShowMessage("Print All feature coming soon!", "Info", "InformationOutline");
        }

        [RelayCommand]
        private async Task GenerateSelected()
        {
            // Generate the currently selected report
            await GenerateBalanceSheet();
        }

        #endregion

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

        #endregion
    }
}
