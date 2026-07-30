using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeAppBooks.Models
{
    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int ItemId { get; set; }
        [ForeignKey("ItemId")]
        public InventoryItem Item { get; set; }

        public string TransactionType { get; set; } // PURCHASE, SALE, ADJUSTMENT

        // Link to Source Document
        public int? InvoiceId { get; set; } // If Sale
        public int? BillId { get; set; } // If Purchase

        [Column(TypeName = "decimal(18,4)")]
        public decimal QuantityChange { get; set; } // +10 or -5

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; } // Cost at time of transaction

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalCost { get; set; } // Quantity * UnitCost

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
    }
}
