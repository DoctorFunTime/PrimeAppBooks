using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrimeAppBooks.Models
{
    public class InventoryItem
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; }

        public string Description { get; set; }

        // Pricing
        //[Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; } // Default selling price

        //[Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseCost { get; set; } // Average cost per unit

        // Stock Tracking
        public decimal QuantityOnHand { get; set; }

        public decimal LowStockThreshold { get; set; } = 5;

        // GL Mapping which helps finding the Accounts
        public int IncomeAccountId { get; set; } // e.g. 4000 Sales
        public int ExpenseAccountId { get; set; } // e.g. 5000 COGS
        public int AssetAccountId { get; set; } // e.g. 1400 Inventory Asset

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
