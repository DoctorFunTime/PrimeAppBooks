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
using static PrimeAppBooks.Models.Pages.TransactionsModels;
using Color = System.Windows.Media.Color;
using Colors = QuestPDF.Helpers.Colors;

namespace PrimeAppBooks.Services
{
    public class ReportPrintingService
    {
        public ReportPrintingService()
        {
        }

        #region FlowDocument Generation (for WPF Printing)

        public FlowDocument GenerateBalanceSheetDocument(BalanceSheetData data)
        {
            var doc = CreateBaseDocument(data.ReportTitle, data.CompanyName, data.EndDate.ToString("MMMM dd, yyyy"));

            // ASSETS SECTION
            AddSectionHeader(doc, "ASSETS");
            AddSpacer(doc, 5);

            // Fixed Assets
            if (data.FixedAssets.Any())
            {
                AddSubsectionHeader(doc, "Fixed Assets");
                foreach (var item in data.FixedAssets)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSubtotal(doc, "Total Fixed Assets", data.TotalFixedAssets);
                AddSpacer(doc, 8);
            }

            // Current Assets
            if (data.CurrentAssets.Any())
            {
                AddSubsectionHeader(doc, "Current Assets");
                foreach (var item in data.CurrentAssets)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 1);
                AddSubtotal(doc, "Total Current Assets", data.TotalCurrentAssets);
                AddSpacer(doc, 8);
            }

            AddTotal(doc, "TOTAL ASSETS", data.TotalAssets, isFinal: true);
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

            // OTHER INCOME (Subheader inside Revenue)
            if (data.OtherIncome.Any())
            {
                AddSpacer(doc, 5);
                AddSubsectionHeader(doc, "OTHER INCOME");
                foreach (var item in data.OtherIncome)
                    AddLineItem(doc, item.AccountName, item.Amount, indent: 2);
                AddSpacer(doc, 5);
                AddLineItem(doc, "Total Other Income", data.TotalOtherIncome, indent: 1, isBold: true);
            }

            if (!data.Revenue.Any() && !data.OtherIncome.Any())
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

        public FlowDocument GenerateDebtorReportDocument(System.Collections.Generic.List<CustomerSummaryMetrics> customers)
        {
            var doc = CreateBaseDocument("Customer Debtor Report (By Grade)", "PrimeApp Books", DateTime.Now.ToString("MMMM dd, yyyy"));

            // Create table with better styling
            var table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Black;
            table.BorderThickness = new Thickness(1);

            // Define columns
            table.Columns.Add(new TableColumn { Width = new GridLength(200) });   // Customer Name
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });   // Phone
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });   // Outstanding
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });   // Overdue

            // Header row with better styling
            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Background = new SolidColorBrush(Color.FromRgb(41, 128, 185));

            AddTableCell(headerRow, "Customer Name", true, TextAlignment.Left);
            AddTableCell(headerRow, "Phone", true, TextAlignment.Left);
            AddTableCell(headerRow, "Outstanding", true, TextAlignment.Right);
            AddTableCell(headerRow, "Overdue", true, TextAlignment.Right);

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // Group by Grade
            var groupedCustomers = customers
                .GroupBy(c => string.IsNullOrEmpty(c.GradeLevel) ? "Unassigned" : c.GradeLevel)
                .OrderBy(g => g.Key);

            var dataGroup = new TableRowGroup();

            foreach (var group in groupedCustomers)
            {
                // Grade Header
                var groupHeaderRow = new TableRow();
                groupHeaderRow.Background = new SolidColorBrush(Color.FromRgb(220, 230, 240));

                var groupHeaderCell = new TableCell(new Paragraph(new Run($"{group.Key} ({group.Count()} Students)")));
                groupHeaderCell.ColumnSpan = 4;
                groupHeaderCell.Padding = new Thickness(8, 6, 8, 6);
                groupHeaderCell.FontWeight = FontWeights.Bold;
                groupHeaderCell.BorderBrush = Brushes.Black;
                groupHeaderCell.BorderThickness = new Thickness(0.5);
                groupHeaderRow.Cells.Add(groupHeaderCell);

                dataGroup.Rows.Add(groupHeaderRow);

                bool isEvenRow = false;
                decimal groupOutstanding = 0;
                decimal groupOverdue = 0;

                foreach (var customer in group)
                {
                    groupOutstanding += customer.TotalOutstanding;
                    groupOverdue += customer.OverdueAmount;

                    var row = new TableRow();
                    if (isEvenRow)
                    {
                        row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    }

                    // Indent customer name slightly to show grouping
                    var namePara = new Paragraph(new Run(customer.CustomerName));
                    namePara.Margin = new Thickness(10, 0, 0, 0);

                    var nameCell = new TableCell(namePara);
                    nameCell.Padding = new Thickness(8, 6, 8, 6);
                    nameCell.BorderBrush = Brushes.Black;
                    nameCell.BorderThickness = new Thickness(0.5);
                    row.Cells.Add(nameCell);

                    AddTableCell(row, customer.CustomerPhone ?? "-", false, TextAlignment.Left);
                    AddTableCell(row, customer.TotalOutstanding.ToString("N2"), false, TextAlignment.Right);
                    AddTableCell(row, customer.OverdueAmount.ToString("N2"), false, TextAlignment.Right);

                    dataGroup.Rows.Add(row);
                    isEvenRow = !isEvenRow;
                }

                // Group Total Row
                var groupTotalRow = new TableRow();
                groupTotalRow.FontWeight = FontWeights.SemiBold;
                groupTotalRow.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));

                var groupTotalLabel = new TableCell(new Paragraph(new Run($"Total {group.Key}")));
                groupTotalLabel.ColumnSpan = 2;
                groupTotalLabel.Padding = new Thickness(8, 6, 8, 6);
                groupTotalLabel.BorderBrush = Brushes.Black;
                groupTotalLabel.BorderThickness = new Thickness(0.5);
                groupTotalLabel.TextAlignment = TextAlignment.Right;
                groupTotalRow.Cells.Add(groupTotalLabel);

                AddTableCell(groupTotalRow, groupOutstanding.ToString("N2"), false, TextAlignment.Right);
                AddTableCell(groupTotalRow, groupOverdue.ToString("N2"), false, TextAlignment.Right);

                dataGroup.Rows.Add(groupTotalRow);
            }

            table.RowGroups.Add(dataGroup);

            // Grand Total row with emphasis
            var totalGroup = new TableRowGroup();
            var totalRow = new TableRow();
            totalRow.FontWeight = FontWeights.Bold;
            totalRow.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            totalRow.Foreground = Brushes.White;

            var totalLabelCell = new TableCell(new Paragraph(new Run("GRAND TOTAL")));
            totalLabelCell.Padding = new Thickness(8);
            totalLabelCell.BorderBrush = Brushes.Black;
            totalLabelCell.BorderThickness = new Thickness(0.5);
            totalLabelCell.FontWeight = FontWeights.Bold;
            totalLabelCell.Foreground = Brushes.White;
            totalLabelCell.ColumnSpan = 2;
            totalRow.Cells.Add(totalLabelCell);

            AddTableCell(totalRow, customers.Sum(c => c.TotalOutstanding).ToString("N2"), false, TextAlignment.Right);
            AddTableCell(totalRow, customers.Sum(c => c.OverdueAmount).ToString("N2"), false, TextAlignment.Right);

            totalGroup.Rows.Add(totalRow);
            table.RowGroups.Add(totalGroup);

            doc.Blocks.Add(table);

            return doc;
        }

        public FlowDocument GenerateSimpleDebtorReportDocument(System.Collections.Generic.List<CustomerSummaryMetrics> customers)
        {
            var doc = CreateBaseDocument("Customer Debtor Report (Simple List)", "PrimeApp Books", DateTime.Now.ToString("MMMM dd, yyyy"));

            // Create table
            var table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Black;
            table.BorderThickness = new Thickness(1);

            // Define columns
            table.Columns.Add(new TableColumn { Width = new GridLength(200) });   // Customer Name
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });   // Phone
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });   // Outstanding
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });   // Overdue

            // Header row
            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Background = new SolidColorBrush(Color.FromRgb(41, 128, 185));

            AddTableCell(headerRow, "Customer Name", true, TextAlignment.Left);
            AddTableCell(headerRow, "Phone", true, TextAlignment.Left);
            AddTableCell(headerRow, "Outstanding", true, TextAlignment.Right);
            AddTableCell(headerRow, "Overdue", true, TextAlignment.Right);

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            // Data rows
            var dataGroup = new TableRowGroup();
            bool isEvenRow = false;

            foreach (var customer in customers)
            {
                var row = new TableRow();
                if (isEvenRow) row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

                AddTableCell(row, customer.CustomerName, false, TextAlignment.Left);
                AddTableCell(row, customer.CustomerPhone ?? "-", false, TextAlignment.Left);
                AddTableCell(row, customer.TotalOutstanding.ToString("N2"), false, TextAlignment.Right);
                AddTableCell(row, customer.OverdueAmount.ToString("N2"), false, TextAlignment.Right);

                dataGroup.Rows.Add(row);
                isEvenRow = !isEvenRow;
            }
            table.RowGroups.Add(dataGroup);

            // Grand Total
            var totalGroup = new TableRowGroup();
            var totalRow = new TableRow();
            totalRow.FontWeight = FontWeights.Bold;
            totalRow.Background = new SolidColorBrush(Color.FromRgb(52, 73, 94));
            totalRow.Foreground = Brushes.White;

            var totalLabelCell = new TableCell(new Paragraph(new Run("GRAND TOTAL")));
            totalLabelCell.Padding = new Thickness(8);
            totalLabelCell.BorderBrush = Brushes.Black;
            totalLabelCell.BorderThickness = new Thickness(0.5);
            totalLabelCell.FontWeight = FontWeights.Bold;
            totalLabelCell.Foreground = Brushes.White;
            totalLabelCell.ColumnSpan = 2;
            totalRow.Cells.Add(totalLabelCell);

            AddTableCell(totalRow, customers.Sum(c => c.TotalOutstanding).ToString("N2"), false, TextAlignment.Right);
            AddTableCell(totalRow, customers.Sum(c => c.OverdueAmount).ToString("N2"), false, TextAlignment.Right);

            totalGroup.Rows.Add(totalRow);
            table.RowGroups.Add(totalGroup);

            doc.Blocks.Add(table);
            return doc;
        }

        public FlowDocument GeneratePaymentPlansReportDocument(System.Collections.Generic.List<PaymentPlan> plans)
        {
            var doc = CreateBaseDocument("Payment Plans Report", "PrimeApp Books", DateTime.Now.ToString("MMMM dd, yyyy"));

            var table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Black;
            table.BorderThickness = new Thickness(1);

            // Customer, Plan Name, Monthly, Start, Status
            table.Columns.Add(new TableColumn { Width = new GridLength(180) });
            table.Columns.Add(new TableColumn { Width = new GridLength(150) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });

            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow();
            headerRow.Background = new SolidColorBrush(Color.FromRgb(41, 128, 185));

            AddTableCell(headerRow, "Customer", true);
            AddTableCell(headerRow, "Plan Name", true);
            AddTableCell(headerRow, "Monthly", true, TextAlignment.Right);
            AddTableCell(headerRow, "Start Date", true, TextAlignment.Center);
            AddTableCell(headerRow, "Status", true, TextAlignment.Center);

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            var dataGroup = new TableRowGroup();
            bool isEvenRow = false;

            foreach (var plan in plans)
            {
                var row = new TableRow();
                if (isEvenRow) row.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));

                AddTableCell(row, plan.Customer?.CustomerName ?? "-", false);
                AddTableCell(row, plan.PlanName, false);
                AddTableCell(row, plan.MonthlyInstallment.ToString("N2"), false, TextAlignment.Right);
                AddTableCell(row, plan.StartDate.ToString("d"), false, TextAlignment.Center);
                AddTableCell(row, plan.Status, false, TextAlignment.Center);

                dataGroup.Rows.Add(row);
                isEvenRow = !isEvenRow;
            }
            table.RowGroups.Add(dataGroup);
            doc.Blocks.Add(table);

            return doc;
        }

        public FlowDocument GenerateImportSummaryDocument(ImportSession session, string companyName)
        {
            var doc = CreateBaseDocument("Import Session Summary Report", companyName, $"Processed on {session.ImportDate:f}");
            doc.FontFamily = new FontFamily("Century Gothic");

            // Session Info Group
            AddSectionHeader(doc, "SESSION INFORMATION");
            AddLineItem(doc, "Session ID", session.SessionId, indent: 1);
            AddLineItem(doc, "Import Status", session.Status, indent: 1, isBold: true);
            AddLineItem(doc, "Date Range", $"{session.StartDate:d} to {session.EndDate:d}", indent: 1);
            AddSpacer(doc, 10);

            // Statistics Group
            AddSectionHeader(doc, "IMPORT STATISTICS");
            AddLineItem(doc, "New Students Added", session.NewStudentsCount.ToString(), indent: 1);
            AddLineItem(doc, "Existing Students Updated", session.ExistingStudentsCount.ToString(), indent: 1);
            AddLineItem(doc, "Transactions Imported", session.TransactionsCount.ToString(), indent: 1);
            AddSpacer(doc, 5);
            AddTotal(doc, "TOTAL FINANCIAL IMPACT", session.TotalAmount, isFinal: true);
            AddSpacer(doc, 15);

            // Notes Section
            if (!string.IsNullOrEmpty(session.Notes))
            {
                AddSectionHeader(doc, "NOTES");
                var notesPara = new Paragraph(new Run(session.Notes));
                notesPara.Margin = new Thickness(20, 5, 20, 5);
                notesPara.FontStyle = FontStyles.Italic;
                doc.Blocks.Add(notesPara);
            }

            // Disclaimer/Footer info
            var footerPara = new Paragraph(new Run("This report summarizes the data synchronization between PrimeApp Academy and PrimeApp Books. " +
                "All imported journal entries are prefixed with the Session ID for auditing and reversal purposes."));
            footerPara.FontSize = 9;
            footerPara.Foreground = Brushes.Gray;
            footerPara.Margin = new Thickness(0, 30, 0, 0);
            footerPara.TextAlignment = TextAlignment.Center;
            doc.Blocks.Add(footerPara);

            return doc;
        }

        public string GenerateImportSummaryPdf(ImportSession session, string companyName)
        {
            var filePath = GetTempPdfPath($"ImportSummary_{session.SessionId}");
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, "Import Session Summary Report", companyName, $"Processed on {session.ImportDate:f}"));

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().Text("SESSION INFORMATION").Bold().FontSize(13).Underline();
                        column.Item().PaddingLeft(10).Text($"Session ID: {session.SessionId}");
                        column.Item().PaddingLeft(10).Text($"Import Status: {session.Status}").Bold();
                        column.Item().PaddingLeft(10).Text($"Date Range: {session.StartDate:d} to {session.EndDate:d}");

                        column.Item().PaddingTop(15).Text("IMPORT STATISTICS").Bold().FontSize(13).Underline();
                        column.Item().PaddingLeft(10).Text($"New Students Added: {session.NewStudentsCount}");
                        column.Item().PaddingLeft(10).Text($"Existing Students Updated: {session.ExistingStudentsCount}");
                        column.Item().PaddingLeft(10).Text($"Transactions Imported: {session.TransactionsCount}");

                        column.Item().PaddingTop(10).AlignRight().Text($"TOTAL FINANCIAL IMPACT: {session.TotalAmount:N2}").Bold().FontSize(12);

                        if (!string.IsNullOrEmpty(session.Notes))
                        {
                            column.Item().PaddingTop(15).Text("NOTES").Bold().FontSize(13).Underline();
                            column.Item().PaddingLeft(10).Text(session.Notes).Italic();
                        }
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);

            return filePath;
        }

        public string ExportImportSummaryToCsv(ImportSession session)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimeAppBooks", "Exports");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, $"ImportSummary_{session.SessionId}_{DateTime.Now:yyyyMMddHHmmss}.csv");

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Metric,Value");
            csv.AppendLine($"Session ID,{session.SessionId}");
            csv.AppendLine($"Import Date,{session.ImportDate:f}");
            csv.AppendLine($"Status,{session.Status}");
            csv.AppendLine($"Start Date,{session.StartDate:d}");
            csv.AppendLine($"End Date,{session.EndDate:d}");
            csv.AppendLine($"New Students Added,{session.NewStudentsCount}");
            csv.AppendLine($"Existing Students Updated,{session.ExistingStudentsCount}");
            csv.AppendLine($"Transactions Imported,{session.TransactionsCount}");
            csv.AppendLine($"Total Financial Impact,{session.TotalAmount:N2}");
            csv.AppendLine($"Notes,\"{session.Notes?.Replace("\"", "\"\"")}\"");

            File.WriteAllText(filePath, csv.ToString());
            return filePath;
        }


        public string ExportBalanceSheetToPdf(BalanceSheetData data, string filePath)
        {
            var tempFile = GenerateBalanceSheetPdf(data);
            if (File.Exists(filePath)) File.Delete(filePath);
            File.Move(tempFile, filePath);
            return filePath;
        }

        public string GenerateAccountTransactionsPdf(string accountName, DateTime? startDate, DateTime? endDate, System.Collections.Generic.List<JournalLine> transactions, decimal openingBalance = 0)
        {
            var dateRangeText = (startDate.HasValue || endDate.HasValue)
                ? $"Period: {(startDate.HasValue ? startDate.Value.ToShortDateString() : "Beginning")} - {(endDate.HasValue ? endDate.Value.ToShortDateString() : "Today")}"
                : "All Time";

            var filePath = GetTempPdfPath($"Journal_{accountName}");
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, $"Account Transactions: {accountName}", "PrimeApp Books", dateRangeText));

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // Summary Section
                        var totalDebits = transactions.Sum(t => t.DebitAmount);
                        var totalCredits = transactions.Sum(t => t.CreditAmount);
                        var netChange = totalDebits - totalCredits;
                        var closingBalance = openingBalance + netChange;

                        column.Item().PaddingBottom(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120);
                                columns.ConstantColumn(100);
                            });

                            table.Cell().Text("Opening Balance:").SemiBold();
                            table.Cell().AlignRight().Text(openingBalance.ToString("N2"));

                            table.Cell().Text("Total Debits:").SemiBold();
                            table.Cell().AlignRight().Text(totalDebits.ToString("N2")).FontColor(Colors.Green.Medium);

                            table.Cell().Text("Total Credits:").SemiBold();
                            table.Cell().AlignRight().Text(totalCredits.ToString("N2")).FontColor(Colors.Red.Medium);

                            table.Cell().PaddingTop(5).Text("Closing Balance:").Bold();
                            table.Cell().PaddingTop(5).AlignRight().Text(closingBalance.ToString("N2")).Bold().FontColor(Colors.Blue.Medium);
                        });

                        // Transactions Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);  // Date
                                columns.ConstantColumn(80);  // Journal #
                                columns.ConstantColumn(100); // Reference
                                columns.RelativeColumn();   // Description
                                columns.ConstantColumn(80);  // Debit
                                columns.ConstantColumn(80);  // Credit
                                columns.ConstantColumn(90);  // Balance
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date");
                                header.Cell().Element(CellStyle).Text("Journal #");
                                header.Cell().Element(CellStyle).Text("Reference");
                                header.Cell().Element(CellStyle).Text("Description");
                                header.Cell().Element(CellStyle).AlignRight().Text("Debit");
                                header.Cell().Element(CellStyle).AlignRight().Text("Credit");
                                header.Cell().Element(CellStyle).AlignRight().Text("Balance");

                                static IContainer CellStyle(IContainer container) => container.Background(Colors.Blue.Medium).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White)).Padding(5);
                            });

                            decimal runningBalance = openingBalance;
                            foreach (var line in transactions.OrderBy(t => t.LineDate))
                            {
                                runningBalance += (line.DebitAmount - line.CreditAmount);

                                table.Cell().Element(Padding).Text(line.LineDate.ToShortDateString());
                                table.Cell().Element(Padding).Text(line.JournalEntry?.JournalNumber ?? "");
                                table.Cell().Element(Padding).Text(line.JournalEntry?.Reference ?? line.Reference ?? "");
                                table.Cell().Element(Padding).Text(line.JournalEntry?.Description ?? line.Description ?? "");
                                table.Cell().Element(Padding).AlignRight().Text(line.DebitAmount > 0 ? line.DebitAmount.ToString("N2") : "-");
                                table.Cell().Element(Padding).AlignRight().Text(line.CreditAmount > 0 ? line.CreditAmount.ToString("N2") : "-");
                                table.Cell().Element(Padding).AlignRight().Text(runningBalance.ToString("N2")).SemiBold();

                                static IContainer Padding(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(5).BorderBottom(0.5f, Unit.Point).BorderColor(Colors.Grey.Lighten2);
                            }
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);

            return filePath;
        }

        public FlowDocument GenerateStatementDocument(string customerName, DateTime startDate, DateTime endDate, System.Collections.ObjectModel.ObservableCollection<StatementItem> transactions, decimal openingBalance, decimal closingBalance)
        {
            var doc = CreateBaseDocument($"Statement of Account", "PrimeApp Books", "");
            doc.FontFamily = new FontFamily("Century Gothic");
            doc.FontSize = 10;
            doc.ColumnWidth = double.PositiveInfinity;

            // Customer Details Block
            var customerPara = new Paragraph(new Run($"Customer: {customerName}"));
            customerPara.FontSize = 14;
            customerPara.Margin = new Thickness(0, 0, 0, 15);
            doc.Blocks.Add(customerPara);

            // Add Summary Section at Top
            var totalDebits = transactions.Sum(t => t.Debit);
            var totalCredits = transactions.Sum(t => t.Credit);
            var netChange = totalDebits - totalCredits;

            var summaryTable = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 25) };
            summaryTable.Columns.Add(new TableColumn { Width = new GridLength(160) });
            summaryTable.Columns.Add(new TableColumn { Width = new GridLength(120) });

            var summaryGroup = new TableRowGroup();

            // Header Row
            var summaryTitleRow = new TableRow();
            summaryTitleRow.Cells.Add(new TableCell(new Paragraph(new Run("STATEMENT SUMMARY")) { FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(52, 73, 94)) }) { ColumnSpan = 2, Padding = new Thickness(0, 0, 0, 10) });
            summaryGroup.Rows.Add(summaryTitleRow);

            // Opening Balance
            var opRow = new TableRow();
            opRow.Cells.Add(new TableCell(new Paragraph(new Run("Opening Balance:"))) { Padding = new Thickness(0, 2, 0, 2) });
            opRow.Cells.Add(new TableCell(new Paragraph(new Run(openingBalance.ToString("N2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(0, 2, 0, 2) });
            summaryGroup.Rows.Add(opRow);

            // Total Debits
            var debRow = new TableRow();
            debRow.Cells.Add(new TableCell(new Paragraph(new Run("Total Debits:"))) { Padding = new Thickness(0, 2, 0, 2) });
            debRow.Cells.Add(new TableCell(new Paragraph(new Run(totalDebits.ToString("N2"))) { Foreground = Brushes.Green }) { TextAlignment = TextAlignment.Right, Padding = new Thickness(0, 2, 0, 2) });
            summaryGroup.Rows.Add(debRow);

            // Total Credits
            var credRow = new TableRow();
            credRow.Cells.Add(new TableCell(new Paragraph(new Run("Total Credits:"))) { Padding = new Thickness(0, 2, 0, 2) });
            credRow.Cells.Add(new TableCell(new Paragraph(new Run(totalCredits.ToString("N2"))) { Foreground = Brushes.Red }) { TextAlignment = TextAlignment.Right, Padding = new Thickness(0, 2, 0, 2) });
            summaryGroup.Rows.Add(credRow);

            // Net Change
            var netRow = new TableRow();
            netRow.Cells.Add(new TableCell(new Paragraph(new Run("Net Change:"))) { Padding = new Thickness(0, 2, 0, 2) });
            netRow.Cells.Add(new TableCell(new Paragraph(new Run(netChange.ToString("N2"))) { FontWeight = FontWeights.SemiBold }) { TextAlignment = TextAlignment.Right, Padding = new Thickness(0, 2, 0, 2) });
            summaryGroup.Rows.Add(netRow);

            // Closing Balance
            var closeRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)) };
            closeRow.Cells.Add(new TableCell(new Paragraph(new Run("Closing Balance:"))) { Padding = new Thickness(5, 5, 0, 5) });
            closeRow.Cells.Add(new TableCell(new Paragraph(new Run(closingBalance.ToString("N2"))) { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)) }) { TextAlignment = TextAlignment.Right, Padding = new Thickness(0, 5, 5, 5) });
            summaryGroup.Rows.Add(closeRow);

            summaryTable.RowGroups.Add(summaryGroup);
            doc.Blocks.Add(summaryTable);

            // Table
            var table = new Table();
            table.CellSpacing = 0;
            table.BorderBrush = Brushes.Black;
            table.BorderThickness = new Thickness(1);

            // Date, Description, Reference, Debit, Credit, Balance
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });
            table.Columns.Add(new TableColumn { Width = new GridLength(280) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });

            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(52, 73, 94)), Foreground = Brushes.White, FontWeight = FontWeights.Bold };

            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Date"))) { Padding = new Thickness(5) });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Description"))) { Padding = new Thickness(5) });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Reference"))) { Padding = new Thickness(5) });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Debit"))) { Padding = new Thickness(5), TextAlignment = TextAlignment.Right });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Credit"))) { Padding = new Thickness(5), TextAlignment = TextAlignment.Right });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Balance"))) { Padding = new Thickness(5), TextAlignment = TextAlignment.Right });

            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            var dataGroup = new TableRowGroup();
            DateTime? lastDate = null;

            foreach (var item in transactions.OrderBy(t => t.Date))
            {
                // Delineate per day
                if (lastDate.HasValue && item.Date.Date != lastDate.Value.Date)
                {
                    var separatorRow = new TableRow();
                    var separatorCell = new TableCell(new Paragraph()) { ColumnSpan = 6, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 0.5), Padding = new Thickness(0, 2, 0, 2) };
                    separatorRow.Cells.Add(separatorCell);
                    dataGroup.Rows.Add(separatorRow);
                }
                lastDate = item.Date;

                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Date.ToShortDateString()))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Description))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Reference))) { Padding = new Thickness(5) });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Debit > 0 ? item.Debit.ToString("N2") : "-"))) { Padding = new Thickness(5), TextAlignment = TextAlignment.Right });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Credit > 0 ? item.Credit.ToString("N2") : "-"))) { Padding = new Thickness(5), TextAlignment = TextAlignment.Right });
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.RunningBalance.ToString("N2")))) { Padding = new Thickness(5), TextAlignment = TextAlignment.Right, FontWeight = FontWeights.SemiBold });

                dataGroup.Rows.Add(row);
            }

            table.RowGroups.Add(dataGroup);
            doc.Blocks.Add(table);

            return doc;
        }


        public string ExportIncomeStatementToPdf(IncomeStatementData data, string filePath)
        {
            var tempFile = GenerateIncomeStatementPdf(data);
            if (File.Exists(filePath)) File.Delete(filePath);
            File.Move(tempFile, filePath);
            return filePath;
        }

        public string ExportTrialBalanceToPdf(TrialBalanceData data, string filePath)
        {
            var tempFile = GenerateTrialBalancePdf(data);
            if (File.Exists(filePath)) File.Delete(filePath);
            File.Move(tempFile, filePath);
            return filePath;
        }

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
            doc.FontFamily = new FontFamily("Century Gothic");
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

        private void AddLineItem(FlowDocument doc, string label, decimal amount, int indent = 0, bool isBold = false)
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
            labelBlock.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
            labelBlock.FontFamily = new FontFamily("Century Gothic");
            System.Windows.Controls.Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            // Amount TextBlock
            var amountBlock = new System.Windows.Controls.TextBlock();
            amountBlock.Text = amount.ToString("N2");
            amountBlock.FontSize = 10.5;
            amountBlock.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
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

        private void AddLineItem(FlowDocument doc, string label, string value, int indent = 0, bool isBold = false)
        {
            var section = new Section();
            section.Margin = new Thickness(indent * 20, 2, 20, 2);

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            var labelBlock = new System.Windows.Controls.TextBlock();
            labelBlock.Text = label;
            labelBlock.FontSize = 10;
            if (isBold) labelBlock.FontWeight = FontWeights.Bold;
            System.Windows.Controls.Grid.SetColumn(labelBlock, 0);
            grid.Children.Add(labelBlock);

            var valueBlock = new System.Windows.Controls.TextBlock();
            valueBlock.Text = value;
            valueBlock.FontSize = 10;
            if (isBold) valueBlock.FontWeight = FontWeights.Bold;
            valueBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            valueBlock.MinWidth = 120;
            System.Windows.Controls.Grid.SetColumn(valueBlock, 1);
            grid.Children.Add(valueBlock);

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
            labelBlock.FontFamily = new FontFamily("Century Gothic");
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
            labelBlock.FontFamily = new FontFamily("Century Gothic");
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

        /// <summary>
        /// Generate Analytics Master Summary FlowDocument for printing
        /// </summary>
        public FlowDocument GenerateAnalyticsSummaryDocument(MasterSummaryData data)
        {
            var dateRangeText = $"{data.ReportStartDate:MMM dd, yyyy} - {data.ReportEndDate:MMM dd, yyyy}";
            var doc = CreateBaseDocument("Analytics Master Summary", "PrimeApp Books", dateRangeText);
            doc.FontSize = 11;

            // KPI Summary Section
            AddSectionHeader(doc, "KEY PERFORMANCE INDICATORS");
            AddSpacer(doc, 10);

            // Create KPI table
            var kpiTable = new Table();
            kpiTable.CellSpacing = 0;
            kpiTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
            kpiTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
            kpiTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
            kpiTable.Columns.Add(new TableColumn { Width = new GridLength(150) });

            var kpiRowGroup = new TableRowGroup();

            // Row 1
            var row1 = new TableRow();
            AddTableCell(row1, "Collection Rate:", true);
            AddTableCell(row1, $"{data.CollectionRate:F1}%", false, TextAlignment.Right);
            AddTableCell(row1, "Average DSO:", true);
            AddTableCell(row1, $"{data.AverageDSO:F0} Days", false, TextAlignment.Right);
            kpiRowGroup.Rows.Add(row1);

            // Row 2
            var row2 = new TableRow();
            AddTableCell(row2, "Receivables Turnover:", true);
            AddTableCell(row2, $"{data.ReceivablesTurnover:F2}x", false, TextAlignment.Right);
            AddTableCell(row2, "Bad Debt Ratio:", true);
            AddTableCell(row2, $"{data.BadDebtRatio:F2}%", false, TextAlignment.Right);
            kpiRowGroup.Rows.Add(row2);

            // Row 3
            var row3 = new TableRow();
            AddTableCell(row3, "On-Time Payment Rate:", true);
            AddTableCell(row3, $"{data.OnTimePaymentRate:F1}%", false, TextAlignment.Right);
            AddTableCell(row3, "AR to Revenue Ratio:", true);
            AddTableCell(row3, $"{data.ARToRevenueRatio:F2}%", false, TextAlignment.Right);
            kpiRowGroup.Rows.Add(row3);

            kpiTable.RowGroups.Add(kpiRowGroup);
            doc.Blocks.Add(kpiTable);
            AddSpacer(doc, 20);

            // YTD Summary Section
            AddSectionHeader(doc, "YEAR-TO-DATE SUMMARY");
            AddSpacer(doc, 10);

            var ytdTable = new Table();
            ytdTable.CellSpacing = 0;
            ytdTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
            ytdTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
            ytdTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
            ytdTable.Columns.Add(new TableColumn { Width = new GridLength(150) });

            var ytdRowGroup = new TableRowGroup();

            var ytdRow1 = new TableRow();
            AddTableCell(ytdRow1, "Total Invoiced:", true);
            AddTableCell(ytdRow1, data.TotalInvoicedYTD.ToString("C"), false, TextAlignment.Right);
            AddTableCell(ytdRow1, "Total Collected:", true);
            AddTableCell(ytdRow1, data.TotalCollectedYTD.ToString("C"), false, TextAlignment.Right);
            ytdRowGroup.Rows.Add(ytdRow1);

            var ytdRow2 = new TableRow();
            AddTableCell(ytdRow2, "Current AR Balance:", true);
            AddTableCell(ytdRow2, data.CurrentARBalance.ToString("C"), false, TextAlignment.Right);
            AddTableCell(ytdRow2, "Active Customers:", true);
            AddTableCell(ytdRow2, data.TotalActiveCustomers.ToString(), false, TextAlignment.Right);
            ytdRowGroup.Rows.Add(ytdRow2);

            ytdTable.RowGroups.Add(ytdRowGroup);
            doc.Blocks.Add(ytdTable);
            AddSpacer(doc, 20);

            // Aging Distribution
            if (data.AgingDistribution.Any())
            {
                AddSectionHeader(doc, "AGING DISTRIBUTION");
                AddSpacer(doc, 10);

                var agingTable = new Table();
                agingTable.CellSpacing = 0;
                agingTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
                agingTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
                agingTable.Columns.Add(new TableColumn { Width = new GridLength(100) });

                var agingHeader = new TableRowGroup();
                var agingHeaderRow = new TableRow();
                agingHeaderRow.Background = new SolidColorBrush(Color.FromRgb(68, 114, 196));
                AddTableCell(agingHeaderRow, "Category", true);
                AddTableCell(agingHeaderRow, "Amount", true, TextAlignment.Right);
                AddTableCell(agingHeaderRow, "Percentage", true, TextAlignment.Right);
                agingHeader.Rows.Add(agingHeaderRow);
                agingTable.RowGroups.Add(agingHeader);

                var agingBody = new TableRowGroup();
                foreach (var bucket in data.AgingDistribution)
                {
                    var agingRow = new TableRow();
                    AddTableCell(agingRow, bucket.Label);
                    AddTableCell(agingRow, bucket.Amount.ToString("C"), false, TextAlignment.Right);
                    AddTableCell(agingRow, $"{bucket.Percentage:F1}%", false, TextAlignment.Right);
                    agingBody.Rows.Add(agingRow);
                }
                agingTable.RowGroups.Add(agingBody);
                doc.Blocks.Add(agingTable);
                AddSpacer(doc, 20);
            }

            // Top 10 Debtors
            if (data.TopDebtors.Any())
            {
                AddSectionHeader(doc, "TOP 10 DEBTORS");
                AddSpacer(doc, 10);

                var debtorTable = new Table();
                debtorTable.CellSpacing = 0;
                debtorTable.Columns.Add(new TableColumn { Width = new GridLength(250) });
                debtorTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
                debtorTable.Columns.Add(new TableColumn { Width = new GridLength(150) });

                var debtorHeader = new TableRowGroup();
                var debtorHeaderRow = new TableRow();
                debtorHeaderRow.Background = new SolidColorBrush(Color.FromRgb(68, 114, 196));
                AddTableCell(debtorHeaderRow, "Customer Name", true);
                AddTableCell(debtorHeaderRow, "Outstanding", true, TextAlignment.Right);
                AddTableCell(debtorHeaderRow, "Overdue", true, TextAlignment.Right);
                debtorHeader.Rows.Add(debtorHeaderRow);
                debtorTable.RowGroups.Add(debtorHeader);

                var debtorBody = new TableRowGroup();
                foreach (var debtor in data.TopDebtors)
                {
                    var debtorRow = new TableRow();
                    AddTableCell(debtorRow, debtor.CustomerName);
                    AddTableCell(debtorRow, debtor.OutstandingAmount.ToString("C"), false, TextAlignment.Right);
                    AddTableCell(debtorRow, debtor.OverdueAmount.ToString("C"), false, TextAlignment.Right);
                    debtorBody.Rows.Add(debtorRow);
                }
                debtorTable.RowGroups.Add(debtorBody);
                doc.Blocks.Add(debtorTable);
                AddSpacer(doc, 20);
            }

            // Monthly Trends Table
            if (data.MonthlyTrends != null && data.MonthlyTrends.Any())
            {
                AddSectionHeader(doc, "MONTHLY TRENDS");
                AddSpacer(doc, 10);

                var trendTable = new Table();
                trendTable.CellSpacing = 0;
                trendTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
                trendTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
                trendTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
                trendTable.Columns.Add(new TableColumn { Width = new GridLength(150) });

                var trendHeader = new TableRowGroup();
                var trendHeaderRow = new TableRow();
                trendHeaderRow.Background = new SolidColorBrush(Color.FromRgb(68, 114, 196));
                AddTableCell(trendHeaderRow, "Month", true);
                AddTableCell(trendHeaderRow, "Revenue", true, TextAlignment.Right);
                AddTableCell(trendHeaderRow, "Collections", true, TextAlignment.Right);
                AddTableCell(trendHeaderRow, "AR Balance", true, TextAlignment.Right);
                trendHeader.Rows.Add(trendHeaderRow);
                trendTable.RowGroups.Add(trendHeader);

                var trendBody = new TableRowGroup();
                foreach (var trend in data.MonthlyTrends)
                {
                    var trendRow = new TableRow();
                    AddTableCell(trendRow, trend.Month);
                    AddTableCell(trendRow, trend.Revenue.ToString("C"), false, TextAlignment.Right);
                    AddTableCell(trendRow, trend.Collections.ToString("C"), false, TextAlignment.Right);
                    AddTableCell(trendRow, trend.ARBalance.ToString("C"), false, TextAlignment.Right);
                    trendBody.Rows.Add(trendRow);
                }
                trendTable.RowGroups.Add(trendBody);
                doc.Blocks.Add(trendTable);
            }

            return doc;
        }

        #endregion Helper Methods - FlowDocument

        #region QuestPDF Generation

        private string GetTempPdfPath(string reportName)
        {
            var fileName = $"{reportName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return Path.Combine(Path.GetTempPath(), fileName);
        }

        public string GenerateAnalyticsSummaryPdf(MasterSummaryData data)
        {
            var filePath = GetTempPdfPath("Master_Summary");
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Century Gothic"));

                    var dateRangeText = $"{data.ReportStartDate:MMM dd, yyyy} - {data.ReportEndDate:MMM dd, yyyy}";
                    page.Header().Element(h => ComposeHeader(h, "Analytics Master Summary", "PrimeApp Books", dateRangeText));

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // KPI Section
                        column.Item().PaddingTop(5).Text("KEY PERFORMANCE INDICATORS").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        column.Item().PaddingVertical(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            AddKpiRow(table, "Collection Rate", $"{data.CollectionRate:F1}%", "Average DSO", $"{data.AverageDSO:F0} Days");
                            AddKpiRow(table, "Turnover", $"{data.ReceivablesTurnover:F2}x", "Bad Debt Ratio", $"{data.BadDebtRatio:F2}%");
                            AddKpiRow(table, "On-Time Rate", $"{data.OnTimePaymentRate:F1}%", "AR to Revenue", $"{data.ARToRevenueRatio:F2}%");
                        });

                        // YTD Summary
                        column.Item().PaddingTop(15).Text("YEAR-TO-DATE SUMMARY").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        column.Item().PaddingVertical(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                            });

                            table.Cell().Text("Total Invoiced YTD").SemiBold();
                            table.Cell().AlignRight().Text(data.TotalInvoicedYTD.ToString("C"));
                            table.Cell().PaddingLeft(20).Text("Total Collected YTD").SemiBold();
                            table.Cell().AlignRight().Text(data.TotalCollectedYTD.ToString("C"));

                            table.Cell().Text("Current AR Balance").SemiBold();
                            table.Cell().AlignRight().Text(data.CurrentARBalance.ToString("C"));
                            table.Cell().PaddingLeft(20).Text("Active Customers").SemiBold();
                            table.Cell().AlignRight().Text(data.TotalActiveCustomers.ToString());
                        });

                        // Two column section for Top Debtors and Aging
                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("TOP DEBTORS").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Customer");
                                        header.Cell().Element(CellStyle).AlignRight().Text("Balance");
                                        static IContainer CellStyle(IContainer container) => container.Background(Colors.Blue.Medium).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White)).Padding(5);
                                    });

                                    foreach (var debtor in data.TopDebtors)
                                    {
                                        table.Cell().Element(Padding).Text(debtor.CustomerName);
                                        table.Cell().Element(Padding).AlignRight().Text(debtor.OutstandingAmount.ToString("N2"));
                                        static IContainer Padding(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
                                    }
                                });
                            });

                            row.Spacing(20);

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("AGING DISTRIBUTION").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                                col.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(80);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Category");
                                        header.Cell().Element(CellStyle).AlignRight().Text("Amount");
                                        static IContainer CellStyle(IContainer container) => container.Background(Colors.Blue.Medium).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White)).Padding(5);
                                    });

                                    foreach (var bucket in data.AgingDistribution)
                                    {
                                        table.Cell().Element(Padding).Text(bucket.Label);
                                        table.Cell().Element(Padding).AlignRight().Text(bucket.Amount.ToString("N2"));
                                        static IContainer Padding(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3);
                                    }
                                });
                            });
                        });

                        // Payment Timing Statistics
                        if (data.PaymentTiming.TotalPayments > 0)
                        {
                            column.Item().PaddingTop(15).Text("PAYMENT TIMING DISTRIBUTION").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                            column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                            column.Item().PaddingVertical(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(120);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(120);
                                });

                                table.Cell().Text("1st-10th of Month:").SemiBold();
                                table.Cell().AlignRight().Text($"{data.PaymentTiming.Percent1to10:F1}%");
                                table.Cell().PaddingLeft(20).Text("11th-20th of Month:").SemiBold();
                                table.Cell().AlignRight().Text($"{data.PaymentTiming.Percent11to20:F1}%");

                                table.Cell().Text("21st-End of Month:").SemiBold();
                                table.Cell().AlignRight().Text($"{data.PaymentTiming.Percent21toEnd:F1}%");
                                table.Cell().PaddingLeft(20).Text("Avg Payment Day:").SemiBold();
                                table.Cell().AlignRight().Text($"{data.PaymentTiming.AveragePaymentDay:F0}");
                            });
                        }

                        // Student-Specific Analytics
                        column.Item().PaddingTop(15).Text("STUDENT ANALYTICS").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        column.Item().PaddingVertical(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                                columns.RelativeColumn();
                                columns.ConstantColumn(120);
                            });

                            table.Cell().Text("Avg Balance/Student:").SemiBold();
                            table.Cell().AlignRight().Text(data.StudentStats.AverageBalancePerStudent.ToString("C"));
                            table.Cell().PaddingLeft(20).Text("% Paid in Full:").SemiBold();
                            table.Cell().AlignRight().Text($"{data.StudentStats.PercentPaidInFull:F1}%");

                            table.Cell().Text("Consistent Payers:").SemiBold();
                            table.Cell().AlignRight().Text(data.StudentStats.StudentsWithConsistentPayments.ToString());
                            table.Cell().PaddingLeft(20).Text("At Risk (60+ days):").SemiBold();
                            table.Cell().AlignRight().Text(data.StudentStats.StudentsAtRisk.ToString());

                            table.Cell().Text("Avg Days to 1st Payment:").SemiBold();
                            table.Cell().AlignRight().Text($"{data.StudentStats.AverageDaysToFirstPayment:F0} days");
                            table.Cell().PaddingLeft(20).Text("");
                            table.Cell().Text("");
                        });
                    }); // This closes the Column lambda

                    page.Footer().Element(ComposeFooter);
                }); // This closes the Page lambda
            }).GeneratePdf(filePath); // This closes the Create lambda

            return filePath;
        }

        public string GenerateBalanceSheetPdf(BalanceSheetData data)
        {
            var filePath = GetTempPdfPath(data.ReportTitle);
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, data.ReportTitle, data.CompanyName, data.EndDate.ToString("MMMM dd, yyyy")));

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // ASSETS
                        column.Item().PaddingTop(5).Text("ASSETS").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        // NON-CURRENT ASSETS — Fixed Asset Register (grouped by category, 4 columns)
                        if (data.FixedAssetGroups.Any())
                        {
                            column.Item().PaddingTop(8).Text("Non-Current Assets").SemiBold().Italic();

                            // Column header row
                            column.Item().PaddingLeft(10).PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);    // Asset Name
                                    cols.ConstantColumn(90);   // Cost
                                    cols.ConstantColumn(90);   // Accum. Dep.
                                    cols.ConstantColumn(90);   // NBV
                                });

                                // Header
                                table.Header(header =>
                                {
                                    static IContainer HeaderCell(IContainer c) =>
                                        c.BorderBottom(0.5f).PaddingVertical(3).PaddingHorizontal(4)
                                         .DefaultTextStyle(x => x.FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2));

                                    header.Cell().Element(HeaderCell).Text("");
                                    header.Cell().Element(HeaderCell).AlignRight().Text("Cost");
                                    header.Cell().Element(HeaderCell).AlignRight().Text("Accum. Dep.");
                                    header.Cell().Element(HeaderCell).AlignRight().Text("NBV");
                                });

                                // Asset rows grouped by category
                                foreach (var group in data.FixedAssetGroups)
                                {
                                    // Category sub-header
                                    table.Cell().ColumnSpan(4)
                                         .PaddingTop(6).PaddingLeft(2).PaddingBottom(1)
                                         .Text(group.CategoryName)
                                         .FontSize(9).SemiBold().Italic()
                                         .FontColor(Colors.Blue.Medium);

                                    // Individual assets
                                    foreach (var asset in group.Assets)
                                    {
                                        static IContainer DataCell(IContainer c) =>
                                            c.PaddingVertical(2).PaddingHorizontal(4);

                                        table.Cell().Element(DataCell).PaddingLeft(8).Text(asset.AssetName).FontSize(9);
                                        table.Cell().Element(DataCell).AlignRight().Text(asset.Cost.ToString("N2")).FontSize(9);
                                        table.Cell().Element(DataCell).AlignRight()
                                             .Text($"({asset.AccumulatedDepreciation.ToString("N2")})").FontSize(9)
                                             .FontColor(Colors.Red.Medium);
                                        table.Cell().Element(DataCell).AlignRight().Text(asset.NetBookValue.ToString("N2")).FontSize(9).SemiBold();
                                    }

                                    // Category subtotal
                                    static IContainer SubtotalCell(IContainer c) =>
                                        c.BorderTop(0.5f).PaddingVertical(3).PaddingHorizontal(4);

                                    table.Cell().Element(SubtotalCell).PaddingLeft(4).Text($"Total {group.CategoryName}").FontSize(9).Italic();
                                    table.Cell().Element(SubtotalCell).AlignRight().Text(group.TotalCost.ToString("N2")).FontSize(9).SemiBold();
                                    table.Cell().Element(SubtotalCell).AlignRight()
                                         .Text($"({group.TotalAccumDep.ToString("N2")})").FontSize(9).SemiBold()
                                         .FontColor(Colors.Red.Medium);
                                    table.Cell().Element(SubtotalCell).AlignRight().Text(group.TotalNBV.ToString("N2")).FontSize(9).SemiBold();
                                }

                                // Grand total row for all fixed assets
                                static IContainer TotalCell(IContainer c) =>
                                    c.BorderTop(1).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4);

                                table.Cell().Element(TotalCell).Text("Total Non-Current Assets").SemiBold().FontSize(9);
                                table.Cell().Element(TotalCell).AlignRight().Text(data.TotalFixedAssetsCost.ToString("N2")).SemiBold().FontSize(9);
                                table.Cell().Element(TotalCell).AlignRight()
                                     .Text($"({data.TotalAccumDepreciation.ToString("N2")})").SemiBold().FontSize(9)
                                     .FontColor(Colors.Red.Medium);
                                table.Cell().Element(TotalCell).AlignRight().Text(data.TotalFixedAssets.ToString("N2")).SemiBold().FontSize(9);
                            });

                            column.Item().PaddingBottom(8);
                        }

                        // CURRENT ASSETS
                        ComposeAssetSubsection(column, "Current Assets", data.CurrentAssets, data.TotalCurrentAssets);

                        column.Item().PaddingTop(10).BorderTop(2).BorderBottom(2).Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL ASSETS").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.TotalAssets.ToString("N2")).SemiBold();
                        });

                        // LIABILITIES
                        column.Item().PaddingTop(25).Text("LIABILITIES").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        ComposeAssetSubsection(column, "Current Liabilities", data.CurrentLiabilities, data.TotalCurrentLiabilities);
                        ComposeAssetSubsection(column, "Long-term Liabilities", data.LongTermLiabilities, data.TotalLongTermLiabilities);

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL LIABILITIES").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.TotalLiabilities.ToString("N2")).SemiBold();
                        });

                        // EQUITY
                        column.Item().PaddingTop(25).Text("EQUITY").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);

                        foreach (var item in data.Equity)
                        {
                            column.Item().Row(row => {
                                row.RelativeItem().PaddingLeft(10).Text(item.AccountName);
                                row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                            });
                        }

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL EQUITY").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.TotalEquity.ToString("N2")).SemiBold();
                        });

                        // FINAL TOTAL
                        column.Item().PaddingTop(20).BorderTop(2).BorderBottom(2).Background(Colors.Grey.Lighten4).Padding(5).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL LIABILITIES & EQUITY").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.TotalLiabilitiesAndEquity.ToString("N2")).SemiBold();
                        });

                        var isBalanced = Math.Abs(data.TotalAssets - data.TotalLiabilitiesAndEquity) < 0.01m;
                        column.Item().PaddingTop(15).AlignCenter().Text(isBalanced ? "✓ Balance Sheet is balanced" : "⚠ Balance Sheet does NOT balance!").FontColor(isBalanced ? Colors.Green.Medium : Colors.Red.Medium).Bold();
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);
            return filePath;
        }

        private void ComposeAssetSubsection(ColumnDescriptor column, string title, List<AccountLineItem> items, decimal total)
        {
            if (!items.Any()) return;
            column.Item().PaddingTop(8).Text(title).SemiBold().Italic();
            foreach (var item in items)
            {
                column.Item().PaddingLeft(10).Row(row =>
                {
                    row.RelativeItem().Text(item.AccountName);
                    row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                });
            }
            column.Item().PaddingLeft(5).BorderTop(0.5f).Row(row =>
            {
                row.RelativeItem().Text($"Total {title}").Italic();
                row.RelativeItem().AlignRight().Text(total.ToString("N2")).SemiBold();
            });
            column.Item().PaddingBottom(10);
        }

        public string GenerateAssetRegisterPdf(AssetRegisterReportData data)
        {
            var filePath = GetTempPdfPath(data.ReportTitle);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(0.35f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, data.ReportTitle, data.CompanyName, $"As of {data.EndDate:MMMM dd, yyyy}"));

                    page.Content().PaddingVertical(8).Column(column =>
                    {
                        column.Item().PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Text($"Total Assets: {data.TotalAssets}").SemiBold();
                            row.RelativeItem().AlignCenter().Text($"Cost: {data.TotalCost:N2}").SemiBold();
                            row.RelativeItem().AlignCenter().Text($"Accum. Dep.: {data.TotalAccumulatedDepreciation:N2}").SemiBold();
                            row.RelativeItem().AlignRight().Text($"NBV: {data.TotalBookValue:N2}").SemiBold();
                        });

                        if (!data.CategoryGroups.Any())
                        {
                            column.Item().PaddingTop(20).AlignCenter().Text("No assets found in the register.").Italic();
                            return;
                        }

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(55);
                                columns.RelativeColumn(2.2f);
                                columns.ConstantColumn(62);
                                columns.RelativeColumn(1.5f);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(70);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(42);
                                columns.ConstantColumn(78);
                                columns.ConstantColumn(65);
                            });

                            table.Header(header =>
                            {
                                static IContainer HeaderCell(IContainer c) =>
                                    c.Background(Colors.Blue.Medium)
                                     .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(7))
                                     .PaddingVertical(4)
                                     .PaddingHorizontal(3);

                                header.Cell().Element(HeaderCell).Text("Code");
                                header.Cell().Element(HeaderCell).Text("Asset");
                                header.Cell().Element(HeaderCell).Text("Date");
                                header.Cell().Element(HeaderCell).Text("Asset Account");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Cost");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Accum. Dep.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("NBV");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Residual");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Life");
                                header.Cell().Element(HeaderCell).Text("Method");
                                header.Cell().Element(HeaderCell).Text("Status");
                            });

                            foreach (var group in data.CategoryGroups)
                            {
                                table.Cell().ColumnSpan(11)
                                     .Background(Colors.Grey.Lighten4)
                                     .PaddingVertical(4)
                                     .PaddingHorizontal(4)
                                     .Text(group.CategoryName)
                                     .SemiBold()
                                     .FontColor(Colors.Blue.Medium);

                                foreach (var asset in group.Assets)
                                {
                                    static IContainer Cell(IContainer c) =>
                                        c.BorderBottom(0.25f)
                                         .BorderColor(Colors.Grey.Lighten2)
                                         .PaddingVertical(3)
                                         .PaddingHorizontal(3);

                                    table.Cell().Element(Cell).Text(asset.AssetCode);
                                    table.Cell().Element(Cell).Text(asset.AssetName);
                                    table.Cell().Element(Cell).Text(asset.PurchaseDate.ToLocalTime().ToString("dd MMM yyyy"));
                                    table.Cell().Element(Cell).Text(asset.AssetAccountName);
                                    table.Cell().Element(Cell).AlignRight().Text(asset.PurchaseCost.ToString("N2"));
                                    table.Cell().Element(Cell).AlignRight().Text(asset.AccumulatedDepreciation.ToString("N2"));
                                    table.Cell().Element(Cell).AlignRight().Text(asset.BookValue.ToString("N2")).SemiBold();
                                    table.Cell().Element(Cell).AlignRight().Text(asset.ResidualValue.ToString("N2"));
                                    table.Cell().Element(Cell).AlignRight().Text(asset.UsefulLifeYears.ToString("N1"));
                                    table.Cell().Element(Cell).Text(asset.DepreciationMethod);
                                    table.Cell().Element(Cell).Text(asset.Status);
                                }

                                static IContainer SubtotalCell(IContainer c) =>
                                    c.BorderTop(0.75f)
                                     .Background(Colors.Grey.Lighten5)
                                     .PaddingVertical(3)
                                     .PaddingHorizontal(3);

                                table.Cell().ColumnSpan(4).Element(SubtotalCell).Text($"Total {group.CategoryName}").Italic();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(group.TotalCost.ToString("N2")).SemiBold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(group.TotalAccumulatedDepreciation.ToString("N2")).SemiBold();
                                table.Cell().Element(SubtotalCell).AlignRight().Text(group.TotalBookValue.ToString("N2")).SemiBold();
                                table.Cell().ColumnSpan(4).Element(SubtotalCell).Text("");
                            }

                            static IContainer TotalCell(IContainer c) =>
                                c.BorderTop(1)
                                 .Background(Colors.Blue.Lighten5)
                                 .PaddingVertical(4)
                                 .PaddingHorizontal(3);

                            table.Cell().ColumnSpan(4).Element(TotalCell).Text("Grand Total").SemiBold();
                            table.Cell().Element(TotalCell).AlignRight().Text(data.TotalCost.ToString("N2")).SemiBold();
                            table.Cell().Element(TotalCell).AlignRight().Text(data.TotalAccumulatedDepreciation.ToString("N2")).SemiBold();
                            table.Cell().Element(TotalCell).AlignRight().Text(data.TotalBookValue.ToString("N2")).SemiBold();
                            table.Cell().ColumnSpan(4).Element(TotalCell).Text("");
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);

            return filePath;
        }

        public string GenerateIncomeStatementPdf(IncomeStatementData data)
        {
            var filePath = GetTempPdfPath(data.ReportTitle);
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, data.ReportTitle, data.CompanyName, data.DateRangeText));

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        // REVENUE
                        column.Item().PaddingTop(5).Text("REVENUE").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);
                        foreach (var item in data.Revenue)
                        {
                            column.Item().PaddingLeft(10).Row(row => {
                                row.RelativeItem().Text(item.AccountName);
                                row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                            });
                        }

                        // Net Revenue = core sales revenue only (Other Income excluded here)
                        column.Item().PaddingBottom(5).BorderTop(1).Row(row => {
                            row.RelativeItem().Text("Net Revenue").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.TotalRevenue.ToString("N2")).SemiBold();
                        });

                        // COGS
                        if (data.CostOfGoodsSold.Any())
                        {
                            column.Item().PaddingTop(15).Text("COST OF GOODS SOLD").FontSize(12).SemiBold();
                            foreach (var item in data.CostOfGoodsSold)
                            {
                                column.Item().PaddingLeft(10).Row(row => {
                                    row.RelativeItem().Text(item.AccountName);
                                    row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                                });
                            }
                            column.Item().PaddingBottom(5).BorderTop(0.5f).Row(row => {
                                row.RelativeItem().Text("Total COGS").Italic();
                                row.RelativeItem().AlignRight().Text(data.TotalCOGS.ToString("N2")).SemiBold();
                            });
                        }

                        column.Item().PaddingTop(10).Background(Colors.Grey.Lighten4).Padding(5).Row(row => {
                            row.RelativeItem().Text("GROSS PROFIT").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.GrossProfit.ToString("N2")).SemiBold();
                        });

                        // EXPENSES
                        column.Item().PaddingTop(25).Text("OPERATING EXPENSES").FontSize(14).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);
                        foreach (var item in data.OperatingExpenses)
                        {
                            column.Item().PaddingLeft(10).Row(row => {
                                row.RelativeItem().Text(item.AccountName);
                                row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                            });
                        }
                        column.Item().PaddingBottom(5).BorderTop(1).Row(row => {
                            row.RelativeItem().Text("Total Operating Expenses").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.TotalOperatingExpenses.ToString("N2")).SemiBold();
                        });

                        // OPERATING INCOME subtotal
                        column.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(5).Row(row => {
                            row.RelativeItem().Text("OPERATING INCOME").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.OperatingIncome.ToString("N2")).SemiBold();
                        });

                        // OTHER INCOME — shown after Operating Income (correct waterfall position)
                        if (data.OtherIncome.Any())
                        {
                            column.Item().PaddingTop(15).Text("OTHER INCOME").FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
                            column.Item().LineHorizontal(0.5f).LineColor(Colors.Blue.Medium);
                            foreach (var item in data.OtherIncome)
                            {
                                column.Item().PaddingLeft(10).Row(row => {
                                    row.RelativeItem().Text(item.AccountName);
                                    row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                                });
                            }
                            column.Item().PaddingBottom(5).BorderTop(0.5f).Row(row => {
                                row.RelativeItem().Text("Total Other Income").Italic();
                                row.RelativeItem().AlignRight().Text(data.TotalOtherIncome.ToString("N2")).SemiBold();
                            });
                        }

                        // OTHER EXPENSES
                        if (data.OtherExpenses.Any())
                        {
                            column.Item().PaddingTop(15).Text("OTHER EXPENSES").FontSize(12).SemiBold();
                            foreach (var item in data.OtherExpenses)
                            {
                                column.Item().PaddingLeft(10).Row(row => {
                                    row.RelativeItem().Text(item.AccountName);
                                    row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                                });
                            }
                            column.Item().PaddingBottom(5).BorderTop(0.5f).Row(row => {
                                row.RelativeItem().Text("Total Other Expenses").Italic();
                                row.RelativeItem().AlignRight().Text(data.TotalOtherExpenses.ToString("N2")).SemiBold();
                            });
                        }

                        // FINAL NET INCOME
                        column.Item().PaddingTop(20).BorderTop(2).BorderBottom(2).Background(Colors.Blue.Lighten5).Padding(5).Row(row =>
                        {
                            row.RelativeItem().Text("NET INCOME").FontSize(12).Bold();
                            row.RelativeItem().AlignRight().Text(data.NetIncome.ToString("N2")).FontSize(12).Bold();
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);
            return filePath;
        }

        public string GenerateTrialBalancePdf(TrialBalanceData data)
        {
            var filePath = GetTempPdfPath(data.ReportTitle);
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, data.ReportTitle, data.CompanyName, data.EndDate.ToString("MMMM dd, yyyy")));

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).AlignCenter().Text("Account #");
                            header.Cell().Element(CellStyle).Text("Account Name");
                            header.Cell().Element(CellStyle).AlignRight().Text("Debit");
                            header.Cell().Element(CellStyle).AlignRight().Text("Credit");

                            static IContainer CellStyle(IContainer container) => container.Background(QuestPDF.Helpers.Colors.Blue.Medium).DefaultTextStyle(x => x.SemiBold().FontColor(QuestPDF.Helpers.Colors.White)).Padding(5);
                        });

                        foreach (var account in data.Accounts)
                        {
                            table.Cell().Element(Padding).AlignCenter().Text(account.AccountNumber);
                            table.Cell().Element(Padding).Text(account.AccountName);
                            table.Cell().Element(Padding).AlignRight().Text(account.DebitAmount > 0 ? account.DebitAmount.ToString("N2") : "-");
                            table.Cell().Element(Padding).AlignRight().Text(account.CreditAmount > 0 ? account.CreditAmount.ToString("N2") : "-");

                            static IContainer Padding(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(5).BorderBottom(0.5f, Unit.Point).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        }

                        table.Footer(footer =>
                        {
                            footer.Cell().ColumnSpan(2).Background(QuestPDF.Helpers.Colors.Blue.Darken2).Padding(5).AlignRight().Text("TOTALS").SemiBold().FontColor(QuestPDF.Helpers.Colors.White);
                            footer.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken2).Padding(5).AlignRight().Text(data.TotalDebits.ToString("N2")).SemiBold().FontColor(QuestPDF.Helpers.Colors.White);
                            footer.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken2).Padding(5).AlignRight().Text(data.TotalCredits.ToString("N2")).SemiBold().FontColor(QuestPDF.Helpers.Colors.White);
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);
            return filePath;
        }

        public string GenerateStatementPdf(string customerName, DateTime startDate, DateTime endDate, System.Collections.Generic.IEnumerable<StatementItem> transactions, decimal openingBalance, decimal closingBalance)
        {
            var filePath = GetTempPdfPath($"Statement_{customerName}");
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, "Statement of Account", "PrimeApp Books", $"Period: {startDate:d} - {endDate:d}"));

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().PaddingBottom(10).Text(x => {
                            x.Span("Customer: ").SemiBold();
                            x.Span(customerName);
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(90);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Date");
                                header.Cell().Element(CellStyle).Text("Description");
                                header.Cell().Element(CellStyle).AlignRight().Text("Debit");
                                header.Cell().Element(CellStyle).AlignRight().Text("Credit");
                                header.Cell().Element(CellStyle).AlignRight().Text("Balance");

                                static IContainer CellStyle(IContainer container) => container.Background(QuestPDF.Helpers.Colors.Grey.Darken3).DefaultTextStyle(x => x.SemiBold().FontColor(QuestPDF.Helpers.Colors.White)).Padding(5);
                            });

                            table.Cell().Element(Padding).Text(startDate.ToShortDateString());
                            table.Cell().Element(Padding).Text("Opening Balance").Italic();
                            table.Cell().Element(Padding).Text("");
                            table.Cell().Element(Padding).Text("");
                            table.Cell().Element(Padding).AlignRight().Text(openingBalance.ToString("N2"));

                            decimal runningBalance = openingBalance;
                            foreach (var item in transactions.OrderBy(x => x.Date))
                            {
                                runningBalance += (item.Debit - item.Credit);
                                table.Cell().Element(Padding).Text(item.Date.ToShortDateString());
                                table.Cell().Element(Padding).Text(item.Description);
                                table.Cell().Element(Padding).AlignRight().Text(item.Debit > 0 ? item.Debit.ToString("N2") : "-");
                                table.Cell().Element(Padding).AlignRight().Text(item.Credit > 0 ? item.Credit.ToString("N2") : "-");
                                table.Cell().Element(Padding).AlignRight().Text(runningBalance.ToString("N2"));
                            }

                            static IContainer Padding(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(5).BorderBottom(0.5f, Unit.Point).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        });

                        column.Item().PaddingTop(20).AlignRight().Width(200).Column(col => {
                            col.Item().Row(r => {
                                r.RelativeItem().Text("Closing Balance:").SemiBold();
                                r.RelativeItem().AlignRight().Text(closingBalance.ToString("N2")).Bold();
                            });
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);
            return filePath;
        }

        public string GenerateDebtorReportPdf(List<CustomerSummaryMetrics> customers, string title)
        {
            var filePath = GetTempPdfPath(title);
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, title, "PrimeApp Books", DateTime.Now.ToShortDateString()));

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Customer Name");
                            header.Cell().Element(CellStyle).Text("Phone");
                            header.Cell().Element(CellStyle).AlignRight().Text("Outstanding");
                            header.Cell().Element(CellStyle).AlignRight().Text("Overdue");

                            static IContainer CellStyle(IContainer container) => container.Background(QuestPDF.Helpers.Colors.Blue.Medium).DefaultTextStyle(x => x.SemiBold().FontColor(QuestPDF.Helpers.Colors.White)).Padding(5);
                        });

                        foreach (var c in customers.OrderBy(x => x.CustomerName))
                        {
                            table.Cell().Element(Padding).Text(c.CustomerName);
                            table.Cell().Element(Padding).Text(c.CustomerPhone ?? "-");
                            table.Cell().Element(Padding).AlignRight().Text(c.TotalOutstanding.ToString("N2"));
                            table.Cell().Element(Padding).AlignRight().Text(c.OverdueAmount.ToString("N2"));

                            static IContainer Padding(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(5).BorderBottom(0.5f, Unit.Point).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        }

                        table.Footer(footer =>
                        {
                            footer.Cell().ColumnSpan(2).Background(QuestPDF.Helpers.Colors.Blue.Darken2).Padding(5).AlignRight().Text("GRAND TOTAL").SemiBold().FontColor(QuestPDF.Helpers.Colors.White);
                            footer.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken2).Padding(5).AlignRight().Text(customers.Sum(x => x.TotalOutstanding).ToString("N2")).SemiBold().FontColor(QuestPDF.Helpers.Colors.White);
                            footer.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken2).Padding(5).AlignRight().Text(customers.Sum(x => x.OverdueAmount).ToString("N2")).SemiBold().FontColor(QuestPDF.Helpers.Colors.White);
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);
            return filePath;
        }

        public string GeneratePaymentPlansPdf(List<PaymentPlan> plans)
        {
            var filePath = GetTempPdfPath("Active_Payment_Plans");
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, "Active Payment Plans Report", "PrimeApp Books", DateTime.Now.ToShortDateString()));

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Customer");
                            header.Cell().Element(CellStyle).Text("Plan Name");
                            header.Cell().Element(CellStyle).AlignRight().Text("Monthly");
                            header.Cell().Element(CellStyle).AlignCenter().Text("Start Date");
                            header.Cell().Element(CellStyle).AlignCenter().Text("Status");

                            static IContainer CellStyle(IContainer container) => container.Background(QuestPDF.Helpers.Colors.Blue.Medium).DefaultTextStyle(x => x.SemiBold().FontColor(QuestPDF.Helpers.Colors.White)).Padding(5);
                        });

                        foreach (var p in plans)
                        {
                            table.Cell().Element(Padding).Text(p.Customer?.CustomerName ?? "No Customer");
                            table.Cell().Element(Padding).Text(p.PlanName);
                            table.Cell().Element(Padding).AlignRight().Text(p.MonthlyInstallment.ToString("N2"));
                            table.Cell().Element(Padding).AlignCenter().Text(p.StartDate.ToShortDateString());
                            table.Cell().Element(Padding).AlignCenter().Text(p.Status);

                            static IContainer Padding(IContainer container) => container.PaddingVertical(2).PaddingHorizontal(5).BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        }
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);
            return filePath;
        }

        public string GenerateCashFlowPdf(CashFlowData data)
        {
            var filePath = GetTempPdfPath("Cash_Flow");
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.5f, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Century Gothic"));

                    page.Header().Element(h => ComposeHeader(h, "Cash Flow Statement", "PrimeApp Books", $"{data.StartDate:d} - {data.EndDate:d}"));

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().PaddingBottom(10).Row(row => {
                            row.RelativeItem().Text("Cash at Beginning of Period").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.BeginningCashBalance.ToString("N2")).SemiBold();
                        });

                        ComposeCashFlowSection(column, "OPERATING ACTIVITIES", data.OperatingActivities, data.NetCashFromOperating);
                        ComposeCashFlowSection(column, "INVESTING ACTIVITIES", data.InvestingActivities, data.NetCashFromInvesting);
                        ComposeCashFlowSection(column, "FINANCING ACTIVITIES", data.FinancingActivities, data.NetCashFromFinancing);

                        column.Item().PaddingTop(15).Row(row => {
                            row.RelativeItem().Text("NET CHANGE IN CASH").SemiBold();
                            row.RelativeItem().AlignRight().Text(data.NetChangeInCash.ToString("N2")).SemiBold();
                        });

                        column.Item().PaddingTop(10).BorderTop(2).BorderBottom(2).Background(Colors.Blue.Lighten5).Padding(5).Row(row => {
                            row.RelativeItem().Text("CASH AT END OF PERIOD").Bold();
                            row.RelativeItem().AlignRight().Text(data.EndingCashBalance.ToString("N2")).Bold();
                        });
                    });

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf(filePath);
            return filePath;
        }

        private void ComposeCashFlowSection(ColumnDescriptor column, string title, List<CashFlowLineItem> items, decimal total)
        {
            column.Item().PaddingTop(10).Text(title).FontSize(12).SemiBold().FontColor(Colors.Blue.Medium);
            column.Item().LineHorizontal(1).LineColor(Colors.Blue.Medium);
            column.Item().PaddingLeft(10).Column(col =>
            {
                foreach (var item in items)
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text(item.Description);
                        row.RelativeItem().AlignRight().Text(item.Amount.ToString("N2"));
                    });
                }

                col.Item().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Text($"Net Cash from {title}").Italic();
                    row.RelativeItem().AlignRight().Text(total.ToString("N2")).SemiBold();
                });
            });
        }

        // --- PDF Helpers ---

        private void ComposeHeader(IContainer container, string title, string companyName, string dateInfo)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(companyName).FontSize(22).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);
                    col.Item().Text(title).FontSize(16).SemiBold().FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                    col.Item().Text(dateInfo).FontSize(10).Italic().FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(10).BorderTop(1).Row(row =>
            {
                row.RelativeItem().Text(x =>
                {
                    x.Span("Printed on: ").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    x.Span($"{DateTime.Now:f}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                });

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    x.Span(" of ").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    x.TotalPages().FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                });
            });
        }

        private void ComposeAssetSection(ColumnDescriptor column, string title, List<AccountLineItem> items, decimal total)
        {
            if (!items.Any()) return;

            column.Item().PaddingTop(10).Text(title).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();
                    cols.ConstantColumn(100);
                });

                foreach (var item in items)
                {
                    table.Cell().PaddingLeft(10).Text(item.AccountName);
                    table.Cell().AlignRight().Text(item.Amount.ToString("N2"));
                }

                table.Cell().BorderTop(0.5f).PaddingLeft(5).Text($"Total {title}").Italic();
                table.Cell().BorderTop(0.5f).AlignRight().Text(total.ToString("N2")).SemiBold();
            });
        }

        private void AddClosingBalanceSection(ColumnDescriptor column, string label, decimal amount)
        {
            column.Item().PaddingTop(20).AlignRight().Width(250).Border(1).Padding(10).Column(col =>
            {
                col.Item().Text(label).FontSize(10).SemiBold();
                col.Item().Text(amount.ToString("C")).FontSize(16).Bold().FontColor(amount < 0 ? Colors.Red.Medium : Colors.Green.Medium);
            });
        }

        private void AddKpiRow(TableDescriptor table, string label1, string value1, string label2, string value2)
        {
            table.Cell().PaddingVertical(2).Text(label1).SemiBold();
            table.Cell().PaddingVertical(2).AlignRight().Text(value1);
            table.Cell().PaddingVertical(2).PaddingLeft(20).Text(label2).SemiBold();
            table.Cell().PaddingVertical(2).AlignRight().Text(value2);
        }

        #endregion QuestPDF Generation
    }
}
