using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Models.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class TransactionsModels
    {
        public class Bill
        {
            [Key]
            public int BillId { get; set; }

            [Required]
            [MaxLength(100)]
            public string BillNumber { get; set; }

            public int VendorId { get; set; }

            [Required]
            public DateTime BillDate { get; set; }

            [Required]
            public DateTime DueDate { get; set; }

            [Required]
            public decimal TotalAmount { get; set; }

            public decimal TaxAmount { get; set; } = 0;

            public decimal DiscountAmount { get; set; } = 0;

            [Required]
            public decimal NetAmount { get; set; }

            public decimal AmountPaid { get; set; } = 0;

            [Required]
            public decimal Balance { get; set; }

            [MaxLength(20)]
            public string Status { get; set; } = "DRAFT";

            public string Terms { get; set; }
            public string Notes { get; set; }

            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        }

        public class Payment
        {
            [Key]
            public int PaymentId { get; set; }

            [Required]
            [MaxLength(100)]
            public string PaymentNumber { get; set; }

            [Required]
            public DateTime PaymentDate { get; set; }

            public int VendorId { get; set; }
            public int BillId { get; set; }

            [Required]
            [MaxLength(50)]
            public string PaymentMethod { get; set; }

            [Required]
            public decimal Amount { get; set; }

            [MaxLength(100)]
            public string ReferenceNumber { get; set; }

            public string Memo { get; set; }

            [MaxLength(20)]
            public string Status { get; set; } = "PENDING";

            public int? BankAccountId { get; set; }
            public int? ProcessedBy { get; set; }
            public DateTime? ProcessedAt { get; set; }

            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public Bill Bill { get; set; }
        }

        public class JournalEntry
        {
            [Key]
            public int JournalId { get; set; }

            [Required]
            [MaxLength(50)]
            public string JournalNumber { get; set; }

            [Required]
            public DateTime JournalDate { get; set; }

            public int? PeriodId { get; set; }

            [MaxLength(100)]
            public string Reference { get; set; } = string.Empty;

            [Required]
            public string Description { get; set; } = string.Empty;

            [Required]
            [MaxLength(50)]
            public string JournalType { get; set; } = "GENERAL";

            [Required]
            public decimal Amount { get; set; }

            [MaxLength(20)]
            public string Status { get; set; } = "DRAFT";

            public int? PostedBy { get; set; }
            public DateTime? PostedAt { get; set; }

            [Required]
            public int CreatedBy { get; set; } = 1;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<JournalLine> JournalLines { get; set; } = new List<JournalLine>();
        }

        public class JournalLine
        {
            [Key]
            public int LineId { get; set; }

            [Required]
            public int JournalId { get; set; }

            [Required]
            public int AccountId { get; set; }

            [Required]
            public DateTime LineDate { get; set; }

            public int? PeriodId { get; set; }
            public decimal DebitAmount { get; set; } = 0;
            public decimal CreditAmount { get; set; } = 0;
            public string Description { get; set; } = string.Empty;

            [MaxLength(100)]
            public string Reference { get; set; } = string.Empty;

            public int? CostCenterId { get; set; }
            public int? ProjectId { get; set; }
            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            // Navigation properties
            public JournalEntry JournalEntry { get; set; }

            public ChartOfAccount ChartOfAccount { get; set; }  // ADD THIS
        }

        public class ChartOfAccount
        {
            [Key]
            public int AccountId { get; set; }

            [Required]
            [MaxLength(20)]
            public string AccountNumber { get; set; }

            [Required]
            [MaxLength(255)]
            public string AccountName { get; set; }

            [Required]
            [MaxLength(50)]
            public string AccountType { get; set; }

            [MaxLength(50)]
            public string AccountSubtype { get; set; } = string.Empty;

            public string? Description { get; set; }

            public int? ParentAccountId { get; set; }

            public bool IsActive { get; set; } = true;

            public bool IsSystemAccount { get; set; } = false;

            [MaxLength(10)]
            public string NormalBalance { get; set; }

            public decimal OpeningBalance { get; set; } = 0;

            public DateTime? OpeningBalanceDate { get; set; }

            public decimal CurrentBalance { get; set; } = 0;

            public int? CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            // Navigation properties
            public ChartOfAccount ParentAccount { get; set; }

            public ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
            public ICollection<JournalLine> JournalLines { get; set; } = new List<JournalLine>();

            // Computed property for display
            public string FullName => $"{AccountNumber} - {AccountName}";
        }

        public class JournalTemplate
        {
            [Key]
            public int TemplateId { get; set; }

            [Required]
            [MaxLength(255)]
            public string Name { get; set; }

            public string Description { get; set; } = string.Empty;

            [Required]
            [MaxLength(50)]
            public string JournalType { get; set; }

            public string TemplateData { get; set; } = string.Empty; // JSON string containing template lines

            public bool IsActive { get; set; } = true;

            public int CreatedBy { get; set; } = 1;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }

        #region Sales Invoices

        public class SalesInvoice
        {
            [Key]
            public int SalesInvoiceId { get; set; }

            [Required]
            [MaxLength(100)]
            public string InvoiceNumber { get; set; }

            public int CustomerId { get; set; }

            [Required]
            public DateTime InvoiceDate { get; set; }

            [Required]
            public DateTime DueDate { get; set; }

            [Required]
            public decimal TotalAmount { get; set; }

            public decimal TaxAmount { get; set; } = 0;

            public decimal DiscountAmount { get; set; } = 0;

            [Required]
            public decimal NetAmount { get; set; }

            public decimal AmountReceived { get; set; } = 0;

            [Required]
            public decimal Balance { get; set; }

            [MaxLength(20)]
            public string Status { get; set; } = "DRAFT";

            public string Terms { get; set; }
            public string Notes { get; set; }

            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<SalesInvoiceLine> Lines { get; set; } = new List<SalesInvoiceLine>();
        }

        public class SalesInvoiceLine
        {
            [Key]
            public int LineId { get; set; }

            [Required]
            public int SalesInvoiceId { get; set; }

            [Required]
            public string Description { get; set; }

            [Required]
            public int AccountId { get; set; }

            [Required]
            public decimal Quantity { get; set; }

            [Required]
            public decimal UnitPrice { get; set; }

            [Required]
            public decimal Amount { get; set; }

            public SalesInvoice SalesInvoice { get; set; }
            public ChartOfAccount Account { get; set; }
        }

        #endregion

        #region Purchase Invoices

        public class PurchaseInvoice
        {
            [Key]
            public int PurchaseInvoiceId { get; set; }

            [Required]
            [MaxLength(100)]
            public string InvoiceNumber { get; set; }

            public int VendorId { get; set; }

            [Required]
            public DateTime InvoiceDate { get; set; }

            [Required]
            public DateTime DueDate { get; set; }

            [Required]
            public decimal TotalAmount { get; set; }

            public decimal TaxAmount { get; set; } = 0;

            public decimal DiscountAmount { get; set; } = 0;

            [Required]
            public decimal NetAmount { get; set; }

            public decimal AmountPaid { get; set; } = 0;

            [Required]
            public decimal Balance { get; set; }

            [MaxLength(20)]
            public string Status { get; set; } = "DRAFT";

            public string Terms { get; set; }
            public string Notes { get; set; }

            public int CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

            public ICollection<PurchaseInvoiceLine> Lines { get; set; } = new List<PurchaseInvoiceLine>();
        }

        public class PurchaseInvoiceLine
        {
            [Key]
            public int LineId { get; set; }

            [Required]
            public int PurchaseInvoiceId { get; set; }

            [Required]
            public string Description { get; set; }

            [Required]
            public int AccountId { get; set; }

            [Required]
            public decimal Quantity { get; set; }

            [Required]
            public decimal UnitPrice { get; set; }

            [Required]
            public decimal Amount { get; set; }

            public PurchaseInvoice PurchaseInvoice { get; set; }
            public ChartOfAccount Account { get; set; }
        }

        #endregion
    }
}