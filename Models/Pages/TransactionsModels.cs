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
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }

            public SalesInvoice SalesInvoice { get; set; }
            public ChartOfAccount Account { get; set; }
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
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount { get; set; }

            public PurchaseInvoice PurchaseInvoice { get; set; }
            public ChartOfAccount Account { get; set; }
        }

        #endregion Purchase Invoices
    }
}