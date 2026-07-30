using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services.DbServices
{
    public class InventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        #region Item Management

        public async Task<List<InventoryItem>> GetAllItemsAsync()
        {
            return await _context.InventoryItems
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();
        }

        public async Task<InventoryItem> GetItemByIdAsync(int id)
        {
            return await _context.InventoryItems.FindAsync(id);
        }

        public async Task<InventoryItem> CreateItemAsync(InventoryItem item)
        {
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<InventoryItem> UpdateItemAsync(InventoryItem item)
        {
            var existing = await _context.InventoryItems.FindAsync(item.ItemId);
            if (existing == null) throw new Exception("Item not found");

            existing.ItemName = item.ItemName;
            existing.SKU = item.SKU;
            existing.Description = item.Description;
            existing.SalePrice = item.SalePrice;
            existing.PurchaseCost = item.PurchaseCost; // Updating this manually is risky, usually done via Purchase, but allowed for edits
            existing.IncomeAccountId = item.IncomeAccountId;
            existing.ExpenseAccountId = item.ExpenseAccountId;
            existing.AssetAccountId = item.AssetAccountId;
            existing.LowStockThreshold = item.LowStockThreshold;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteItemAsync(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item != null)
            {
                item.IsActive = false; // Soft delete
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Stock Management

        /// <summary>
        /// Records an opening stock balance for a newly created item.
        /// Writes: Dr Inventory Asset / Cr COGS (used as adjustment account for opening entry).
        /// Call this once after CreateItemAsync when QuantityOnHand > 0.
        /// </summary>
        public async Task RecordOpeningStockAsync(int itemId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.InventoryItems.FindAsync(itemId);
                if (item == null) throw new Exception("Item not found");

                if (item.QuantityOnHand <= 0 || item.PurchaseCost <= 0)
                    return; // Nothing to record

                if (item.AssetAccountId <= 0)
                    throw new Exception($"Item '{item.ItemName}' has no Inventory Asset account mapped.");
                if (item.ExpenseAccountId <= 0)
                    throw new Exception($"Item '{item.ItemName}' has no COGS/Expense account mapped.");

                decimal openingValue = item.QuantityOnHand * item.PurchaseCost;

                // Record transaction history
                var invTransaction = new InventoryTransaction
                {
                    ItemId = itemId,
                    TransactionType = "OPENING",
                    QuantityChange = item.QuantityOnHand,
                    UnitCost = item.PurchaseCost,
                    TotalCost = openingValue,
                    TransactionDate = DateTime.UtcNow,
                    Notes = "Opening stock balance",
                    CreatedBy = userId
                };
                _context.InventoryTransactions.Add(invTransaction);

                // Journal entry: Dr Inventory Asset / Cr COGS (adjustment)
                var journal = new JournalEntry
                {
                    JournalNumber = await GenerateJournalNumberAsync(),
                    JournalDate = DateTime.UtcNow,
                    Description = $"Opening Stock: {item.ItemName} x{item.QuantityOnHand} @ {item.PurchaseCost:N4}",
                    JournalType = "INVENTORY",
                    Status = "POSTED",
                    CreatedBy = userId,
                    PostedBy = userId,
                    PostedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    JournalLines = new List<JournalLine>
                    {
                        new JournalLine
                        {
                            AccountId = item.AssetAccountId,
                            Description = $"Opening stock: {item.ItemName}",
                            DebitAmount = openingValue,
                            CreditAmount = 0,
                            LineDate = DateTime.UtcNow
                        },
                        new JournalLine
                        {
                            AccountId = item.ExpenseAccountId,
                            Description = $"Opening stock offset: {item.ItemName}",
                            DebitAmount = 0,
                            CreditAmount = openingValue,
                            LineDate = DateTime.UtcNow
                        }
                    }
                };
                _context.JournalEntries.Add(journal);

                item.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        /// <summary>
        /// Adjusts stock level manually (e.g. for stocktake variance).
        /// Creates an InventoryTransaction and a Journal Entry.
        /// </summary>
        public async Task AdjustStockAsync(int itemId, decimal quantityChange, decimal unitCost, string reason, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.InventoryItems.FindAsync(itemId);
                if (item == null) throw new Exception("Item not found");

                // Update Quantity
                item.QuantityOnHand += quantityChange;
                item.UpdatedAt = DateTime.UtcNow;

                // Record History
                var invTransaction = new InventoryTransaction
                {
                    ItemId = itemId,
                    TransactionType = "ADJUSTMENT",
                    QuantityChange = quantityChange,
                    UnitCost = unitCost,
                    TotalCost = quantityChange * unitCost,
                    TransactionDate = DateTime.UtcNow,
                    Notes = reason,
                    CreatedBy = userId
                };
                _context.InventoryTransactions.Add(invTransaction);

                // Create Journal Entry
                // If Qty increases (Asset Up): Dr Inventory, Cr Expense (Adjustment Account)
                // If Qty decreases (Asset Down): Dr Expense (Adjustment Account), Cr Inventory

                // We need an "Inventory Adjustment" expense account. 
                // For now, we will use the Item's Expense Account (COGS) or a specific defined account.
                // Assuming we use the item's COGS account for simplicity in this version.

                var journal = new JournalEntry
                {
                    JournalNumber = await GenerateJournalNumberAsync(),
                    JournalDate = DateTime.UtcNow,
                    Description = $"Inventory Adj: {item.ItemName} ({reason})",
                    JournalType = "INVENTORY",
                    Status = "POSTED",
                    CreatedBy = userId,
                    PostedBy = userId,
                    PostedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    JournalLines = new List<JournalLine>()
                };

                // Calculate absolute amount
                decimal amount = Math.Abs(quantityChange * unitCost);

                if (quantityChange > 0)
                {
                    // Debit Inventory (Asset increases)
                    journal.JournalLines.Add(new JournalLine
                    {
                        AccountId = item.AssetAccountId,
                        Description = $"Stock Increase: {item.ItemName}",
                        DebitAmount = amount,
                        CreditAmount = 0,
                        LineDate = DateTime.UtcNow
                    });

                    // Credit COGS/Adjustment (Expense decreases, effectively income)
                    // Or Credit Opening Balance Equity if initial setup
                    journal.JournalLines.Add(new JournalLine
                    {
                        AccountId = item.ExpenseAccountId,
                        Description = $"Adjustment: {reason}",
                        DebitAmount = 0,
                        CreditAmount = amount,
                        LineDate = DateTime.UtcNow
                    });
                }
                else
                {
                    // Debit COGS/Adjustment (Expense increases, loss)
                    journal.JournalLines.Add(new JournalLine
                    {
                        AccountId = item.ExpenseAccountId,
                        Description = $"Adjustment: {reason}",
                        DebitAmount = amount,
                        CreditAmount = 0,
                        LineDate = DateTime.UtcNow
                    });

                    // Credit Inventory (Asset decreases)
                    journal.JournalLines.Add(new JournalLine
                    {
                        AccountId = item.AssetAccountId,
                        Description = $"Stock Decrease: {item.ItemName}",
                        DebitAmount = 0,
                        CreditAmount = amount,
                        LineDate = DateTime.UtcNow
                    });
                }

                _context.JournalEntries.Add(journal);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<InventoryTransaction>> GetItemHistoryAsync(int itemId)
        {
            return await _context.InventoryTransactions
                .Where(t => t.ItemId == itemId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        private async Task<string> GenerateJournalNumberAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"JE{year}";
            var maxRetries = 100; // Prevent infinite loops
            var currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                var lastNumber = await _context.JournalEntries
                    .Where(j => j.JournalNumber != null && j.JournalNumber.StartsWith(prefix))
                    .OrderByDescending(j => j.JournalNumber)
                    .Select(j => j.JournalNumber)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (!string.IsNullOrEmpty(lastNumber))
                {
                    var numberPart = lastNumber.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int number))
                    {
                        nextNumber = number + 1;
                    }
                }

                var candidateNumber = $"{prefix}{nextNumber:D4}";

                // Double-check that this number doesn't already exist (handles race conditions)
                var exists = await _context.JournalEntries
                    .AnyAsync(j => j.JournalNumber == candidateNumber);

                if (!exists)
                {
                    return candidateNumber;
                }

                // If it exists, try again with next number
                currentRetry++;
            }

            return $"{prefix}{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        #endregion
    }
}
