using PrimeAppBooks.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace PrimeAppBooks.Services
{
    public class ReportPrintingService
    {
        public ReportPrintingService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        #region FlowDocument Generation (for WPF Printing)

        public FlowDocument GenerateBalanceSheetDocument(BalanceSheetData data)
        {
            var doc = CreateBaseDocument(data.ReportTitle, data.CompanyName, data.EndDate.ToString("MMMM dd, yyyy"));

            // ASSETS SECTION
            AddSectionHeader(doc, "ASSETS");
            AddSpacer(doc, 5);

            // Current Assets
            if (data.CurrentAssets.Any())
            {
                AddSubsectionHeader(doc, "Current Assets");
                foreach (var item in data.CurrentAssets)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSubtotal(doc, "Total Current Assets", data.TotalCurrentAssets);
                AddSpacer(doc, 8);
            }

            // Fixed Assets
            if (data.FixedAssets.Any())
            {
                AddSubsectionHeader(doc, "Fixed Assets");
                foreach (var item in data.FixedAssets)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSubtotal(doc, "Total Fixed Assets", data.TotalFixedAssets);
                AddSpacer(doc, 8);
            }

            AddTotal(doc, "TOTAL ASSETS", data.TotalAssets);
            AddSpacer(doc, 15);

            // LIABILITIES SECTION
            AddSectionHeader(doc, "LIABILITIES");
            AddSpacer(doc, 5);

            if (data.CurrentLiabilities.Any())
            {
                AddSubsectionHeader(doc, "Current Liabilities");
                foreach (var item in data.CurrentLiabilities)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSubtotal(doc, "Total Current Liabilities", data.TotalCurrentLiabilities);
                AddSpacer(doc, 8);
            }

            if (data.LongTermLiabilities.Any())
            {
                AddSubsectionHeader(doc, "Long-term Liabilities");
                foreach (var item in data.LongTermLiabilities)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSubtotal(doc, "Total Long-term Liabilities", data.TotalLongTermLiabilities);
                AddSpacer(doc, 8);
            }

            if (!data.CurrentLiabilities.Any() && !data.LongTermLiabilities.Any())
            {
                var emptyPara = new Paragraph(new Run("No liabilities"));
                emptyPara.Foreground = Brushes.Gray;
                emptyPara.FontStyle = FontStyles.Italic;
                emptyPara.Margin = new Thickness(20, 5, 0, 5);
                doc.Blocks.Add(emptyPara);
                AddSpacer(doc, 8);
            }

            AddTotal(doc, "TOTAL LIABILITIES", data.TotalLiabilities);
            AddSpacer(doc, 15);

            // EQUITY SECTION
            AddSectionHeader(doc, "EQUITY");
            AddSpacer(doc, 5);

            if (data.Equity.Any())
            {
                foreach (var item in data.Equity)
                {
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                }
            }
            else
            {
                var emptyPara = new Paragraph(new Run("No equity accounts"));
                emptyPara.Foreground = Brushes.Gray;
                emptyPara.FontStyle = FontStyles.Italic;
                emptyPara.Margin = new Thickness(20, 5, 0, 5);
                doc.Blocks.Add(emptyPara);
            }

            AddSpacer(doc, 5);
            AddTotal(doc, "TOTAL EQUITY", data.TotalEquity);
            AddSpacer(doc, 15);

            // Final total with verification
            AddTotal(doc, "TOTAL LIABILITIES & EQUITY", data.TotalLiabilitiesAndEquity, isFinal: true);

            // Add balance verification
            AddSpacer(doc, 10);
            var isBalanced = Math.Abs(data.TotalAssets - data.TotalLiabilitiesAndEquity) < 0.01m;
            var verificationPara = new Paragraph(new Run(isBalanced ?
                "✓ Balance Sheet is balanced" :
                $"⚠ Balance Sheet does NOT balance! Difference: {Math.Abs(data.TotalAssets - data.TotalLiabilitiesAndEquity):N2}"));
            verificationPara.FontWeight = FontWeights.Bold;
            verificationPara.Foreground = isBalanced ? Brushes.Green : Brushes.Red;
            verificationPara.FontSize = 12;
            verificationPara.TextAlignment = TextAlignment.Center;
            doc.Blocks.Add(verificationPara);

            return doc;
        }

        public FlowDocument GenerateIncomeStatementDocument(IncomeStatementData data)
        {
            var doc = CreateBaseDocument(data.ReportTitle, data.CompanyName, data.DateRangeText);

            // REVENUE SECTION
            AddSectionHeader(doc, "REVENUE");
            AddSpacer(doc, 5);

            if (data.Revenue.Any())
            {
                foreach (var item in data.Revenue)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
            }
            else
            {
                var emptyPara = new Paragraph(new Run("No revenue recorded"));
                emptyPara.Foreground = Brushes.Gray;
                emptyPara.FontStyle = FontStyles.Italic;
                emptyPara.Margin = new Thickness(20, 5, 0, 5);
                doc.Blocks.Add(emptyPara);
            }

            AddSpacer(doc, 5);
            AddTotal(doc, "Net Revenue", data.TotalRevenue);
            AddSpacer(doc, 12);

            // COST OF GOODS SOLD
            if (data.CostOfGoodsSold.Any())
            {
                AddSectionHeader(doc, "COST OF GOODS SOLD");
                AddSpacer(doc, 5);
                foreach (var item in data.CostOfGoodsSold)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSpacer(doc, 5);
                AddTotal(doc, "Total Cost of Goods Sold", data.TotalCOGS);
                AddSpacer(doc, 12);
            }

            AddSubtotal(doc, "GROSS PROFIT", data.GrossProfit);
            AddSpacer(doc, 12);

            // OPERATING EXPENSES
            if (data.OperatingExpenses.Any())
            {
                AddSectionHeader(doc, "OPERATING EXPENSES");
                AddSpacer(doc, 5);
                foreach (var item in data.OperatingExpenses)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSpacer(doc, 5);
                AddTotal(doc, "Total Operating Expenses", data.TotalOperatingExpenses);
                AddSpacer(doc, 12);

                if (data.CostOfGoodsSold.Any())
                {
                    AddSubtotal(doc, "OPERATING INCOME", data.OperatingIncome);
                    AddSpacer(doc, 12);
                }
            }

            // OTHER INCOME & EXPENSES
            bool hasOtherItems = data.OtherIncome.Any() || data.OtherExpenses.Any();

            if (data.OtherIncome.Any())
            {
                AddSectionHeader(doc, "OTHER INCOME");
                AddSpacer(doc, 5);
                foreach (var item in data.OtherIncome)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSpacer(doc, 5);
                AddSubtotal(doc, "Total Other Income", data.TotalOtherIncome);
                AddSpacer(doc, 8);
            }

            if (data.OtherExpenses.Any())
            {
                AddSectionHeader(doc, "OTHER EXPENSES");
                AddSpacer(doc, 5);
                foreach (var item in data.OtherExpenses)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSpacer(doc, 5);
                AddSubtotal(doc, "Total Other Expenses", data.TotalOtherExpenses);
                AddSpacer(doc, 8);
            }

            if (hasOtherItems)
            {
                AddSpacer(doc, 5);
            }

            // NET INCOME (Final)
            AddTotal(doc, "NET INCOME", data.NetIncome, isFinal: true);

            return doc;
        }

        public FlowDocument GenerateTrialBalanceDocument(TrialBalanceData data)
        {
            var doc = CreateBaseDocument(data.ReportTitle, data.CompanyName, data.EndDate.ToString("MMMM dd, yyyy"));

            // Create table with better styling
            var table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Black;
            table.BorderThickness = new Thickness(1);

            // Define columns with better proportions
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });   // Account #
            table.Columns.Add(new TableColumn { Width = new GridLength(280) });  // Account Name
            table.Columns.Add(new TableColumn { Width = new GridLength(110) });  // Debit
            table.Columns.Add(new TableColumn { Width = new GridLength(110) });  // Credit

            // Header row with better styling
            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Background = new SolidColorBrush(Color.FromRgb(41, 128, 185));

            AddTableCell(headerRow, "Account #", true, TextAlignment.Center);
            AddTableCell(headerRow, "Account Name", true, TextAlignment.Left);
            AddTableCell(headerRow, "Debit", true, TextAlignment.Right);
            AddTableCell(headerRow, "Credit", true, TextAlignment.Right);

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // Data rows with alternating colors
            var dataGroup = new TableRowGroup();
            bool isEvenRow = false;

            foreach (var account in data.Accounts)
            {
                var row = new TableRow();
                if (isEvenRow)
                {
                    row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                }

                AddTableCell(row, account.AccountNumber, false, TextAlignment.Center);
                AddTableCell(row, account.AccountName, false, TextAlignment.Left);
                AddTableCell(row, account.DebitAmount > 0 ? account.DebitAmount.ToString("N2") : "-", false, TextAlignment.Right);
                AddTableCell(row, account.CreditAmount > 0 ? account.CreditAmount.ToString("N2") : "-", false, TextAlignment.Right);

                dataGroup.Rows.Add(row);
                isEvenRow = !isEvenRow;
            }
            table.RowGroups.Add(dataGroup);

            // Total row with emphasis
            var totalGroup = new TableRowGroup();
            var totalRow = new TableRow();
            totalRow.FontWeight = FontWeights.Bold;
            totalRow.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            totalRow.Foreground = Brushes.White;

            AddTableCell(totalRow, "", false, TextAlignment.Center);
            var totalCell = new TableCell(new Paragraph(new Run("TOTALS")));
            totalCell.Padding = new Thickness(8);
            totalCell.BorderBrush = Brushes.Black;
            totalCell.BorderThickness = new Thickness(0.5);
            totalCell.FontWeight = FontWeights.Bold;
            totalCell.Foreground = Brushes.White;
            totalRow.Cells.Add(totalCell);

            AddTableCell(totalRow, data.TotalDebits.ToString("N2"), false, TextAlignment.Right);
            AddTableCell(totalRow, data.TotalCredits.ToString("N2"), false, TextAlignment.Right);

            totalGroup.Rows.Add(totalRow);
            table.RowGroups.Add(totalGroup);

            doc.Blocks.Add(table);

            // Balance verification
            AddSpacer(doc, 12);
            var balanceText = data.IsBalanced ?
                "✓ Trial Balance is balanced" :
                $"⚠ Trial Balance is NOT balanced! Difference: {Math.Abs(data.TotalDebits - data.TotalCredits):N2}";
            var balancePara = new Paragraph(new Run(balanceText));
            balancePara.FontWeight = FontWeights.Bold;
            balancePara.Foreground = data.IsBalanced ? Brushes.Green : Brushes.Red;
            balancePara.FontSize = 13;
            balancePara.TextAlignment = TextAlignment.Center;
            doc.Blocks.Add(balancePara);

            return doc;
        }

        public FlowDocument GenerateCashFlowDocument(CashFlowData data)
        {
            var doc = CreateBaseDocument(data.ReportTitle, data.CompanyName, data.DateRangeText);

            // OPERATING ACTIVITIES
            AddSectionHeader(doc, "CASH FLOWS FROM OPERATING ACTIVITIES");
            AddSpacer(doc, 5);

            if (data.OperatingActivities.Any())
            {
                foreach (var item in data.OperatingActivities)
                    AddLineItem(doc, item.Description, item.Amount, indent: 1);
            }
            else
            {
                var emptyPara = new Paragraph(new Run("No operating activities"));
                emptyPara.Foreground = Brushes.Gray;
                emptyPara.FontStyle = FontStyles.Italic;
                emptyPara.Margin = new Thickness(20, 5, 0, 5);
                doc.Blocks.Add(emptyPara);
            }

            AddSpacer(doc, 5);
            AddSubtotal(doc, "Net Cash from Operating Activities", data.NetCashFromOperating);
            AddSpacer(doc, 12);

            // INVESTING ACTIVITIES
            if (data.InvestingActivities.Any())
            {
                AddSectionHeader(doc, "CASH FLOWS FROM INVESTING ACTIVITIES");
                AddSpacer(doc, 5);
                foreach (var item in data.InvestingActivities)
                    AddLineItem(doc, item.Description, item.Amount, indent: 1);
                AddSpacer(doc, 5);
                AddSubtotal(doc, "Net Cash from Investing Activities", data.NetCashFromInvesting);
                AddSpacer(doc, 12);
            }

            // FINANCING ACTIVITIES
            if (data.FinancingActivities.Any())
            {
                AddSectionHeader(doc, "CASH FLOWS FROM FINANCING ACTIVITIES");
                AddSpacer(doc, 5);
                foreach (var item in data.FinancingActivities)
                    AddLineItem(doc, item.Description, item.Amount, indent: 1);
                AddSpacer(doc, 5);
                AddSubtotal(doc, "Net Cash from Financing Activities", data.NetCashFromFinancing);
                AddSpacer(doc, 12);
            }

            // SUMMARY
            AddTotal(doc, "NET CHANGE IN CASH", data.NetChangeInCash);
            AddSpacer(doc, 8);
            AddLineItem(doc, "Cash at Beginning of Period", data.BeginningCashBalance);
            AddSpacer(doc, 5);
            AddTotal(doc, "CASH AT END OF PERIOD", data.EndingCashBalance, isFinal: true);

            return doc;
        }

        #endregion FlowDocument Generation (for WPF Printing)

        #region PDF Export (QuestPDF) - TEMPORARILY DISABLED

        public string ExportBalanceSheetToPdf(BalanceSheetData data, string filePath)
        {
            var document = Document.Create(container =>
             {
                 container.Page(page =>
                 {
                     page.Size(PageSizes.A4);
                     page.Margin(1, Unit.Inch);

                     page.Content().Column(col =>
                     {
                         // Header
                         col.Item().Text(data.CompanyName).Bold().FontSize(14);
                         col.Item().Text("Balance Sheet").Bold().FontSize(12);
                         col.Item().Text($"As of {data.EndDate:MMMM dd, yyyy}");
                         col.Item().PaddingBottom(20);

                         // Assets - Simple listing
                         col.Item().Text("ASSETS").Bold();
                         foreach (var asset in data.CurrentAssets)
                             col.Item().PaddingLeft(10).Text($"{asset.AccountName} {asset.Amount:N2}");
                         foreach (var asset in data.FixedAssets)
                             col.Item().PaddingLeft(10).Text($"{asset.AccountName} {asset.Amount:N2}");
                         col.Item().Text($"Total Assets: {data.TotalAssets:N2}").Bold();

                         // Liabilities - Simple listing
                         col.Item().PaddingTop(10).Text("LIABILITIES").Bold();
                         foreach (var liability in data.CurrentLiabilities)
                             col.Item().PaddingLeft(10).Text($"{liability.AccountName} {liability.Amount:N2}");
                         foreach (var liability in data.LongTermLiabilities)
                             col.Item().PaddingLeft(10).Text($"{liability.AccountName} {liability.Amount:N2}");
                         col.Item().Text($"Total Liabilities: {data.TotalLiabilities:N2}").Bold();

                         // Equity - Simple listing
                         col.Item().PaddingTop(10).Text("EQUITY").Bold();
                         foreach (var equity in data.Equity)
                             col.Item().PaddingLeft(10).Text($"{equity.AccountName} {equity.Amount:N2}");
                         col.Item().Text($"Total Equity: {data.TotalEquity:N2}").Bold();
                     });
                 });
             });

            document.GeneratePdf(filePath);
            return filePath;
        }

        public string ExportIncomeStatementToPdf(IncomeStatementData data, string filePath)
        {
            throw new NotImplementedException("PDF export requires QuestPDF package. Please install it first or use Print instead.");
        }

        public string ExportTrialBalanceToPdf(TrialBalanceData data, string filePath)
        {
            throw new NotImplementedException("PDF export requires QuestPDF package. Please install it first or use Print instead.");
        }

        #endregion PDF Export (QuestPDF) - TEMPORARILY DISABLED

        #region WPF Printing

        public void PrintDocument(FlowDocument document, string documentName)
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                document.PageHeight = printDialog.PrintableAreaHeight;
                document.PageWidth = printDialog.PrintableAreaWidth;
                document.PagePadding = new Thickness(50);
                document.ColumnGap = 0;
                document.ColumnWidth = printDialog.PrintableAreaWidth;

                var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                printDialog.PrintDocument(paginator, documentName);
            }
        }

        public void OpenPdfFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }

        #endregion WPF Printing

        #region Helper Methods - FlowDocument

        private FlowDocument CreateBaseDocument(string title, string companyName, string dateInfo)
        {
            var doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Calibri");
            doc.FontSize = 11;
            doc.PagePadding = new Thickness(60);
            doc.ColumnWidth = double.PositiveInfinity;
            doc.Background = Brushes.White;

            // Company Name - Prominent
            var companyPara = new Paragraph(new Run(companyName));
            companyPara.FontSize = 20;
            companyPara.FontWeight = FontWeights.Bold;
            companyPara.TextAlignment = TextAlignment.Center;
            companyPara.Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80));
            companyPara.Margin = new Thickness(0, 0, 0, 5);
            doc.Blocks.Add(companyPara);

            // Report Title
            var titlePara = new Paragraph(new Run(title));
            titlePara.FontSize = 15;
            titlePara.FontWeight = FontWeights.SemiBold;
            titlePara.TextAlignment = TextAlignment.Center;
            titlePara.Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            titlePara.Margin = new Thickness(0, 0, 0, 3);
            doc.Blocks.Add(titlePara);

            // Date
            var datePara = new Paragraph(new Run(dateInfo));
            datePara.FontSize = 11;
            datePara.TextAlignment = TextAlignment.Center;
            datePara.Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141));
            datePara.Margin = new Thickness(0, 0, 0, 0);
            doc.Blocks.Add(datePara);

            // Add decorative line
            var linePara = new Paragraph();
            linePara.BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199));
            linePara.BorderThickness = new Thickness(0, 0, 0, 2);
            linePara.Margin = new Thickness(0, 15, 0, 15);
            doc.Blocks.Add(linePara);

            return doc;
        }

        private void AddSectionHeader(FlowDocument doc, string text)
        {
            var para = new Paragraph(new Run(text));
            para.FontWeight = FontWeights.Bold;
            para.FontSize = 13;
            para.Margin = new Thickness(0, 8, 0, 0);
            para.Padding = new Thickness(0, 0, 0, 5);
            para.BorderBrush = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            para.BorderThickness = new Thickness(0, 0, 0, 2);
            para.Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            doc.Blocks.Add(para);
        }

        private void AddSubsectionHeader(FlowDocument doc, string text)
        {
            var para = new Paragraph(new Run(text));
            para.FontWeight = FontWeights.SemiBold;
            para.FontSize = 11;
            para.Margin = new Thickness(0, 6, 0, 3);
            para.Foreground = new SolidColorBrush(Color.FromRgb(41, 128, 185));
            doc.Blocks.Add(para);
        }

        private void AddLineItem(FlowDocument doc, string label, decimal amount, int indent = 0)
        {
            // Use BlockUIContainer with Grid for proper layout
            var grid = new System.Windows.Controls.Grid();
            grid.Margin = new Thickness(indent * 15, 1, 0, 1);

            // Define two columns: one for label (auto-width), one for amount (right-aligned)
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            // Label TextBlock
            var labelBlock = new System.Windows.Controls.TextBlock();
            labelBlock.Text = label;
            labelBlock.FontSize = 10.5;
            labelBlock.FontFamily = new FontFamily("Calibri");
            System.Windows.Controls.Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            // Amount TextBlock
            var amountBlock = new System.Windows.Controls.TextBlock();
            amountBlock.Text = amount.ToString("N2");
            amountBlock.FontSize = 10.5;
            amountBlock.FontFamily = new FontFamily("Consolas");
            amountBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            amountBlock.TextAlignment = TextAlignment.Right;
            amountBlock.MinWidth = 120;
            amountBlock.Margin = new Thickness(10, 0, 0, 0);

            if (amount < 0)
            {
                amountBlock.Foreground = Brushes.Red;
            }

            System.Windows.Controls.Grid.SetColumn(amountBlock, 1);
            grid.Children.Add(amountBlock);

            var container = new BlockUIContainer(grid);
            container.Margin = new Thickness(0);
            doc.Blocks.Add(container);
        }

        private void AddSubtotal(FlowDocument doc, string label, decimal amount)
        {
            // Create a section for the border
            var section = new Section();
            section.Margin = new Thickness(0, 3, 0, 3);
            section.BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199));
            section.BorderThickness = new Thickness(0, 1, 0, 0);
            section.Padding = new Thickness(0, 4, 0, 0);

            // Use BlockUIContainer with Grid for proper layout
            var grid = new System.Windows.Controls.Grid();

            // Define two columns
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            // Label TextBlock
            var labelBlock = new System.Windows.Controls.TextBlock();
            labelBlock.Text = label;
            labelBlock.FontSize = 11;
            labelBlock.FontWeight = FontWeights.SemiBold;
            labelBlock.FontFamily = new FontFamily("Calibri");
            System.Windows.Controls.Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            // Amount TextBlock
            var amountBlock = new System.Windows.Controls.TextBlock();
            amountBlock.Text = amount.ToString("N2");
            amountBlock.FontSize = 11;
            amountBlock.FontWeight = FontWeights.SemiBold;
            amountBlock.FontFamily = new FontFamily("Consolas");
            amountBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            amountBlock.TextAlignment = TextAlignment.Right;
            amountBlock.MinWidth = 120;
            amountBlock.Margin = new Thickness(10, 0, 0, 0);

            if (amount < 0)
            {
                amountBlock.Foreground = Brushes.Red;
            }

            System.Windows.Controls.Grid.SetColumn(amountBlock, 1);
            grid.Children.Add(amountBlock);

            var container = new BlockUIContainer(grid);
            container.Margin = new Thickness(0);
            section.Blocks.Add(container);
            doc.Blocks.Add(section);
        }

        private void AddTotal(FlowDocument doc, string label, decimal amount, bool isFinal = false)
        {
            // Create a section for the border and background
            var section = new Section();
            section.Margin = new Thickness(0, 5, 0, 5);
            section.BorderBrush = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            section.BorderThickness = isFinal ? new Thickness(0, 3, 0, 3) : new Thickness(0, 2, 0, 1);
            section.Padding = new Thickness(0, 5, 0, 5);
            section.Background = isFinal ? new SolidColorBrush(Color.FromRgb(236, 240, 241)) : Brushes.Transparent;

            // Use BlockUIContainer with Grid for proper layout
            var grid = new System.Windows.Controls.Grid();

            // Define two columns
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            // Label TextBlock
            var labelBlock = new System.Windows.Controls.TextBlock();
            labelBlock.Text = label;
            labelBlock.FontSize = 12;
            labelBlock.FontWeight = FontWeights.Bold;
            labelBlock.FontFamily = new FontFamily("Calibri");
            System.Windows.Controls.Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            // Amount TextBlock
            var amountBlock = new System.Windows.Controls.TextBlock();
            amountBlock.Text = amount.ToString("N2");
            amountBlock.FontSize = 12;
            amountBlock.FontWeight = FontWeights.Bold;
            amountBlock.FontFamily = new FontFamily("Consolas");
            amountBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            amountBlock.TextAlignment = TextAlignment.Right;
            amountBlock.MinWidth = 120;
            amountBlock.Margin = new Thickness(10, 0, 0, 0);

            if (amount < 0)
            {
                amountBlock.Foreground = Brushes.Red;
            }

            System.Windows.Controls.Grid.SetColumn(amountBlock, 1);
            grid.Children.Add(amountBlock);

            var container = new BlockUIContainer(grid);
            container.Margin = new Thickness(0);
            section.Blocks.Add(container);
            doc.Blocks.Add(section);
        }

        private void AddSpacer(FlowDocument doc, double height = 10)
        {
            var para = new Paragraph();
            para.Margin = new Thickness(0, height, 0, 0);
            doc.Blocks.Add(para);
        }

        private void AddTableCell(TableRow row, string text, bool isHeader = false, TextAlignment alignment = TextAlignment.Left)
        {
            var cell = new TableCell(new Paragraph(new Run(text)));
            cell.Padding = new Thickness(8, 6, 8, 6);
            cell.BorderBrush = Brushes.Black;
            cell.BorderThickness = new Thickness(0.5);
            cell.TextAlignment = alignment;

            if (isHeader)
            {
                cell.Foreground = Brushes.White;
                cell.FontWeight = FontWeights.Bold;
            }

            row.Cells.Add(cell);
        }

        #endregion Helper Methods - FlowDocument
    }
}