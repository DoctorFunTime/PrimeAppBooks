using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services.DbServices
{
    public class BankServices
    {
        private readonly AppDbContext _context;

        public BankServices(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ChartOfAccount>> GetBankAccountsAsync()
        {
            // Broaden filter to include CURRENT_ASSET and accounts with "Bank" or "Cash" in title
            return await _context.ChartOfAccounts
                .Where(a => a.IsActive && a.AccountType == "ASSET" && 
                           (a.AccountSubtype == "Cash" || 
                            a.AccountSubtype == "Bank" || 
                            a.AccountSubtype == "CURRENT_ASSET" ||
                            a.AccountName.ToLower().Contains("bank") || 
                            a.AccountName.ToLower().Contains("cash")))
                .OrderBy(a => a.AccountNumber)
                .ToListAsync();
        }

        public async Task<List<JournalLine>> GetUnclearedTransactionsAsync(int accountId, DateTime? upToDate = null)
        {
            var query = _context.JournalLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId && !l.IsCleared && l.JournalEntry.Status == "POSTED");

            if (upToDate.HasValue)
            {
                var utcDate = DateTime.SpecifyKind(upToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(l => l.LineDate <= utcDate);
            }

            return await query
                .OrderBy(l => l.LineDate)
                .ToListAsync();
        }

        public async Task<BankReconciliation> GetDraftReconciliationAsync(int accountId)
        {
            return await _context.BankReconciliations
                .Include(r => r.ReconciledLines)
                .FirstOrDefaultAsync(r => r.AccountId == accountId && r.Status == "DRAFT");
        }

        public async Task<decimal> GetLastReconciledBalanceAsync(int accountId)
        {
            var lastReconciliation = await _context.BankReconciliations
                .Where(r => r.AccountId == accountId && r.Status == "COMPLETED")
                .OrderByDescending(r => r.StatementDate)
                .FirstOrDefaultAsync();

            return lastReconciliation?.StatementEndingBalance ?? 0;
        }

        public async Task<BankReconciliation> SaveReconciliationAsync(BankReconciliation reconciliation, List<int> clearedLineIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (reconciliation.ReconciliationId == 0)
                {
                    reconciliation.CreatedAt = DateTime.UtcNow;
                    _context.BankReconciliations.Add(reconciliation);
                }
                else
                {
                    _context.Entry(reconciliation).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();

                // Update journal lines
                // First, reset any lines previously linked to this reconciliation but not in the current selection
                var previouslyClearedLines = await _context.JournalLines
                    .Where(l => l.ReconciliationId == reconciliation.ReconciliationId)
                    .ToListAsync();

                foreach (var line in previouslyClearedLines)
                {
                    if (!clearedLineIds.Contains(line.LineId))
                    {
                        line.IsCleared = false;
                        line.ReconciliationId = null;
                    }
                }

                // Mark new lines as cleared
                var currentlyClearedLines = await _context.JournalLines
                    .Where(l => clearedLineIds.Contains(l.LineId))
                    .ToListAsync();

                foreach (var line in currentlyClearedLines)
                {
                    line.IsCleared = true;
                    line.ReconciliationId = reconciliation.ReconciliationId;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return reconciliation;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CompleteReconciliationAsync(int reconciliationId)
        {
            var reconciliation = await _context.BankReconciliations.FindAsync(reconciliationId);
            if (reconciliation == null) return false;

            reconciliation.Status = "COMPLETED";
            reconciliation.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
