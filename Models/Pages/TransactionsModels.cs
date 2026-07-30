using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrimeAppBooks.Models;

namespace PrimeAppBooks.Models.Pages
{
    public class TransactionsModels
    {
        public class Bill
        {
            public int BillId { get; set; }
            public string BillNumber { get; set; }
            public int VendorId { get; set; }
            public DateTime BillDate { get; set; }
            public DateTime DueDate { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal TaxAmount { get; set; } = 0;
            public decimal DiscountAmount { get; set; } = 0;
            public decimal NetAmount { get; set; }
            public decimal AmountPaid { get; set; } = 0;
            public decimal Balance { get; set; }
            public string Status { get; set; } = "DRAFT";
            public string Terms { get; set; }
            public string Notes { get; set; }
            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<Payment> Payments { get; set; } = new List<Payment>();
            public Vendor Vendor { get; set; }
        }

        public class Payment
        {
            public int PaymentId { get; set; }
            public string PaymentNumber { get; set; }
            public DateTime PaymentDate { get; set; }
            public int VendorId { get; set; }
            public int BillId { get; set; }
            public string PaymentMethod { get; set; }
            public decimal Amount { get; set; }
            public string ReferenceNumber { get; set; }
            public string Memo { get; set; }
            public string Status { get; set; } = "PENDING";
            public int? BankAccountId { get; set; }
            public int? ProcessedBy { get; set; }
            public DateTime? ProcessedAt { get; set; }
            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public Bill Bill { get; set; }
            public Vendor Vendor { get; set; }
        }

        public class JournalEntry
        {
            public int JournalId { get; set; }
            public string JournalNumber { get; set; }
            public DateTime JournalDate { get; set; }
            public int? PeriodId { get; set; }
            public string Reference { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string JournalType { get; set; } = "GENERAL";
            public decimal Amount { get; set; }
            public string Status { get; set; } = "DRAFT";
            public int? PostedBy { get; set; }
            public DateTime? PostedAt { get; set; }
            public int? CurrencyId { get; set; }
            public decimal ExchangeRate { get; set; } = 1;
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<JournalLine> JournalLines { get; set; } = new List<JournalLine>();
        }

        public class JournalLine
        {
            public int LineId { get; set; }
            public int JournalId { get; set; }
            public int AccountId { get; set; }
            public DateTime LineDate { get; set; }
            public int? PeriodId { get; set; }
            public decimal DebitAmount { get; set; } = 0;
            public decimal CreditAmount { get; set; } = 0;
            public string Description { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public int? CostCenterId { get; set; }
            public int? ProjectId { get; set; }
            public int? ContactId { get; set; }
            public string? ContactType { get; set; } // "Customer", "Vendor", or null
            public int? CurrencyId { get; set; }
            public decimal ExchangeRate { get; set; } = 1;
            public decimal ForeignDebitAmount { get; set; } = 0;
            public decimal ForeignCreditAmount { get; set; } = 0;
            public bool IsCleared { get; set; } = false;
            public int? ReconciliationId { get; set; }
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public JournalEntry JournalEntry { get; set; }
            public ChartOfAccount ChartOfAccount { get; set; }
            public Currency Currency { get; set; }
            public BankReconciliation BankReconciliation { get; set; }
        }

        public class FixedAssetLineItem
        {
            public int AssetId { get; set; }
            public string AssetCode { get; set; }
            public string AssetName { get; set; }
            public decimal Cost { get; set; }
            public decimal AccumulatedDepreciation { get; set; }
            public decimal NetBookValue { get; set; }
        }

        public class FixedAssetGroup
        {
            public string CategoryName { get; set; }
            public List<FixedAssetLineItem> Assets { get; set; } = new();
            public decimal TotalCost => Assets.Sum(a => a.Cost);
            public decimal TotalAccumDep => Assets.Sum(a => a.AccumulatedDepreciation);
            public decimal TotalNBV => Assets.Sum(a => a.NetBookValue);
        }

        public class ImportSession
        {
            [System.ComponentModel.DataAnnotations.Key]
            public string SessionId { get; set; }
            public DateTime ImportDate { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int NewStudentsCount { get; set; }
            public int ExistingStudentsCount { get; set; }
            public int TransactionsCount { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = "COMPLETED"; // COMPLETED, REVERSED
            public bool IncludeOpeningBalances { get; set; }
            public string Notes { get; set; } = string.Empty;
        }

        public class BankReconciliation
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int ReconciliationId { get; set; }
            public int AccountId { get; set; }
            public DateTime StatementDate { get; set; }
            public decimal StatementStartingBalance { get; set; }
            public decimal StatementEndingBalance { get; set; }
            public decimal ClearedDifference { get; set; }
            public string Status { get; set; } = "DRAFT"; // DRAFT, COMPLETED
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime? CompletedAt { get; set; }

            public ChartOfAccount Account { get; set; }
            public ICollection<JournalLine> ReconciledLines { get; set; } = new List<JournalLine>();
        }

        public class ChartOfAccount
        {
            public int AccountId { get; set; }
            public string AccountNumber { get; set; }
            public string AccountName { get; set; }
            public string AccountType { get; set; }
            public string AccountSubtype { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int? ParentAccountId { get; set; }
            public bool IsActive { get; set; } = true;
            public bool IsSystemAccount { get; set; } = false;
            public string NormalBalance { get; set; }
            public decimal OpeningBalance { get; set; } = 0;
            public DateTime? OpeningBalanceDate { get; set; }
            public decimal CurrentBalance { get; set; } = 0;
            public int? CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ChartOfAccount ParentAccount { get; set; }
            public ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
            public ICollection<JournalLine> JournalLines { get; set; } = new List<JournalLine>();

            public string FullName => $"{AccountNumber} - {AccountName}";
        }

        public class JournalTemplate
        {
            public int TemplateId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; } = string.Empty;
            public string JournalType { get; set; }
            public string TemplateData { get; set; } = string.Empty;
            public bool IsActive { get; set; } = true;
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }

        public class JournalTemplateData
        {
            public string Description { get; set; }
            public string Reference { get; set; }
            public List<JournalTemplateLineData> Lines { get; set; } = new();
        }

        public class JournalTemplateLineData
        {
            public int AccountId { get; set; }
            public string Description { get; set; }
            public decimal DebitAmount { get; set; }
            public decimal CreditAmount { get; set; }
            public string Reference { get; set; }
        }

        #region Sales Invoices

        public class SalesInvoice
        {
            public int SalesInvoiceId { get; set; }
            public string InvoiceNumber { get; set; }
            public int CustomerId { get; set; }
            public DateTime InvoiceDate { get; set; }
            public DateTime DueDate { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal TaxAmount { get; set; } = 0;
            public decimal DiscountAmount { get; set; } = 0;
            public decimal NetAmount { get; set; }
            public decimal AmountReceived { get; set; } = 0;
            public decimal Balance { get; set; }
            public string Status { get; set; } = "DRAFT";
            public string Terms { get; set; }
            public string Notes { get; set; }
            public int? CurrencyId { get; set; }
            public decimal ExchangeRate { get; set; } = 1;
            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
            public Customer Customer { get; set; }
        }

        public class SalesInvoiceLine
        {
            public int LineId { get; set; }
            public int SalesInvoiceId { get; set; }
            public string Description { get; set; }
            public int AccountId { get; set; }
            public int? ItemId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }

            public SalesInvoice SalesInvoice { get; set; }
            public ChartOfAccount Account { get; set; }
            public InventoryItem Item { get; set; }
        }

        #endregion Sales Invoices

        #region Purchase Invoices

        public class PurchaseInvoice
        {
            public int PurchaseInvoiceId { get; set; }
            public string InvoiceNumber { get; set; }
            public int VendorId { get; set; }
            public DateTime InvoiceDate { get; set; }
            public DateTime DueDate { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal TaxAmount { get; set; } = 0;
            public decimal DiscountAmount { get; set; } = 0;
            public decimal NetAmount { get; set; }
            public decimal AmountPaid { get; set; } = 0;
            public decimal Balance { get; set; }
            public string Status { get; set; } = "DRAFT";
            public string Terms { get; set; }
            public string Notes { get; set; }
            public int? CurrencyId { get; set; }
            public decimal ExchangeRate { get; set; } = 1;
            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<PurchaseInvoiceLine> Lines { get; set; } = new List<PurchaseInvoiceLine>();
            public Vendor Vendor { get; set; }
        }

        public class PurchaseInvoiceLine
        {
            public int LineId { get; set; }
            public int PurchaseInvoiceId { get; set; }
            public string Description { get; set; }
            public int AccountId { get; set; }
            public int? ItemId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }

            public PurchaseInvoice PurchaseInvoice { get; set; }
            public ChartOfAccount Account { get; set; }
            public InventoryItem Item { get; set; }
        }

        #endregion Purchase Invoices

        #region Fixed Assets

        public class AssetCategory
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal DefaultUsefulLifeYears { get; set; } = 5;
            public string DefaultDepreciationMethod { get; set; } = "STRAIGHT_LINE";
            public int? DefaultAssetAccountId { get; set; }
            public int? DefaultAccumDepnAccountId { get; set; }
            public int? DefaultDepnExpenseAccountId { get; set; }
            public bool IsActive { get; set; } = true;

            public ICollection<FixedAsset> Assets { get; set; } = new List<FixedAsset>();
        }

        public class FixedAsset
        {
            public int AssetId { get; set; }
            public string AssetCode { get; set; } = string.Empty;
            public string AssetName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int CategoryId { get; set; }

            // GL Accounts
            public int AssetAccountId { get; set; }          // e.g. 1430 Equipment
            public int AccumDepnAccountId { get; set; }      // e.g. 1500 Accumulated Depreciation
            public int DepnExpenseAccountId { get; set; }    // e.g. 5400 Depreciation Expense
            public int? CwipAccountId { get; set; }          // e.g. 1470 Capital Work in Progress (null when not staged)

            // Acquisition
            public DateTime PurchaseDate { get; set; }
            public decimal PurchaseCost { get; set; }
            public decimal ResidualValue { get; set; } = 0;

            // Depreciation Setup
            public decimal UsefulLifeYears { get; set; } = 5;
            public string DepreciationMethod { get; set; } = "STRAIGHT_LINE"; // STRAIGHT_LINE | REDUCING_BALANCE

            // Running Totals (updated after each depreciation run)
            public decimal AccumulatedDepreciation { get; set; } = 0;
            public decimal BookValue { get; set; } = 0; // = PurchaseCost - AccumulatedDepreciation

            // Status
            public string Status { get; set; } = "ACTIVE"; // ACTIVE | CWIP | FULLY_DEPRECIATED | DISPOSED
            public string? Notes { get; set; }
            public bool IsActive { get; set; } = true;
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            // Navigation
            public AssetCategory Category { get; set; }
            public ChartOfAccount AssetAccount { get; set; }
            public ChartOfAccount AccumDepnAccount { get; set; }
            public ChartOfAccount DepnExpenseAccount { get; set; }
            public ChartOfAccount CwipAccount { get; set; }
            public ICollection<DepreciationEntry> DepreciationEntries { get; set; } = new List<DepreciationEntry>();
            public AssetDisposal Disposal { get; set; }
        }

        public class DepreciationEntry
        {
            public int EntryId { get; set; }
            public int AssetId { get; set; }
            public DateTime PeriodDate { get; set; }         // The date of the depreciation run (end of period)
            public decimal DepreciationAmount { get; set; }
            public decimal BookValueAfter { get; set; }      // Book value after this entry
            public int? JournalId { get; set; }              // Link back to posted journal
            public string Notes { get; set; } = string.Empty;
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public FixedAsset Asset { get; set; }
            public JournalEntry Journal { get; set; }
        }

        public class AssetDisposal
        {
            public int DisposalId { get; set; }
            public int AssetId { get; set; }
            public DateTime DisposalDate { get; set; }
            public decimal SaleProceeds { get; set; } = 0;    // Cash received (0 if scrapped)
            public decimal BookValueAtDisposal { get; set; }
            public decimal GainOrLoss { get; set; }            // Positive = Gain, Negative = Loss
            public string DisposalType { get; set; } = "SALE"; // SALE | SCRAP
            public int? ProceedsAccountId { get; set; }        // Bank/Cash account for proceeds
            public int? JournalId { get; set; }
            public string? Notes { get; set; }
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public FixedAsset Asset { get; set; }
            public JournalEntry Journal { get; set; }
        }

        #endregion Fixed Assets
    }
}