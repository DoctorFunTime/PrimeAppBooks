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
    }
}