using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services.DbServices
{
    public class AssetService
    {
        private readonly AppDbContext _context;
        private readonly JournalServices _journalServices;

        public AssetService(AppDbContext context, JournalServices journalServices)
        {
            _context = context;
            _journalServices = journalServices;
        }

        #region Asset Categories

        public async Task<List<AssetCategory>> GetAllCategoriesAsync()
        {
            return await _context.AssetCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        /// <summary>
        /// Seeds 6 default categories if the table is completely empty.
        /// Called automatically before the category list is returned to the UI.
        /// </summary>
        public async Task EnsureDefaultCategoriesAsync()
        {
            if (await _context.AssetCategories.AnyAsync()) return;

            var accumDepn    = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "1500");
            var depnExpense  = await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "5400");

            var defaults = new[]
            {
                (Name: "Buildings",          AccountNo: "1420", Life: 40m, Method: "STRAIGHT_LINE"),
                (Name: "Vehicles",           AccountNo: "1440", Life: 5m,  Method: "REDUCING_BALANCE"),
                (Name: "Computer Equipment", AccountNo: "1430", Life: 3m,  Method: "REDUCING_BALANCE"),
                (Name: "Office Furniture",   AccountNo: "1450", Life: 10m, Method: "STRAIGHT_LINE"),
                (Name: "Machinery",          AccountNo: "1430", Life: 10m, Method: "STRAIGHT_LINE"),
                (Name: "Other Equipment",    AccountNo: "1430", Life: 5m,  Method: "STRAIGHT_LINE"),
            };

            foreach (var d in defaults)
            {
                var assetAcct = await _context.ChartOfAccounts
                    .FirstOrDefaultAsync(a => a.AccountNumber == d.AccountNo);

                _context.AssetCategories.Add(new AssetCategory
                {
                    CategoryName              = d.Name,
                    DefaultUsefulLifeYears    = d.Life,
                    DefaultDepreciationMethod = d.Method,
                    DefaultAssetAccountId     = assetAcct?.AccountId,
                    DefaultAccumDepnAccountId = accumDepn?.AccountId,
                    DefaultDepnExpenseAccountId = depnExpense?.AccountId,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task SaveCategoryAsync(AssetCategory category)
        {
            if (category.CategoryId == 0)
                _context.AssetCategories.Add(category);
            else
                _context.AssetCategories.Update(category);

            await _context.SaveChangesAsync();
        }

        #endregion

        #region Asset CRUD

        public async Task<List<FixedAsset>> GetAllAssetsAsync()
        {
            return await _context.FixedAssets
                .Include(a => a.Category)
                .Include(a => a.AssetAccount)
                .Include(a => a.AccumDepnAccount)
                .Include(a => a.DepnExpenseAccount)
                .Where(a => a.IsActive)
                .OrderBy(a => a.AssetCode)
                .ToListAsync();
        }

        public async Task<FixedAsset> GetAssetByIdAsync(int assetId)
        {
            return await _context.FixedAssets
                .Include(a => a.Category)
                .Include(a => a.AssetAccount)
                .Include(a => a.AccumDepnAccount)
                .Include(a => a.DepnExpenseAccount)
                .Include(a => a.DepreciationEntries)
                .FirstOrDefaultAsync(a => a.AssetId == assetId);
        }

        public async Task<FixedAsset> CreateAssetAsync(FixedAsset asset, int acquisitionCreditAccountId, bool isStaged = false, int? cwipAccountId = null, int userId = 1)
        {
            if (acquisitionCreditAccountId <= 0)
                throw new Exception("Please select an acquisition offset account.");

            var assetAccount = await _context.ChartOfAccounts.FindAsync(asset.AssetAccountId);
            var offsetAccount = await _context.ChartOfAccounts.FindAsync(acquisitionCreditAccountId);

            if (assetAccount == null || !assetAccount.IsActive)
                throw new Exception("The selected fixed asset account is not active.");

            if (offsetAccount == null || !offsetAccount.IsActive)
                throw new Exception("The selected acquisition offset account is not active.");

            if (isStaged)
            {
                if (!cwipAccountId.HasValue || cwipAccountId <= 0)
                    throw new Exception("Please select a CWIP staging account.");

                var cwipAccount = await _context.ChartOfAccounts.FindAsync(cwipAccountId.Value);
                if (cwipAccount == null || !cwipAccount.IsActive)
                    throw new Exception("The selected CWIP account is not active.");
            }

            // Auto-generate asset code if not provided
            if (string.IsNullOrWhiteSpace(asset.AssetCode))
                asset.AssetCode = await GenerateAssetCodeAsync();

            asset.BookValue = asset.PurchaseCost;
            asset.AccumulatedDepreciation = 0;

            // CWIP mode: stage the asset — no depreciation until capitalised
            if (isStaged && cwipAccountId.HasValue)
            {
                asset.Status = "CWIP";
                asset.CwipAccountId = cwipAccountId;
            }
            else
            {
                asset.Status = "ACTIVE";
                asset.CwipAccountId = null;
            }

            asset.CreatedBy = userId;
            asset.CreatedAt = DateTime.UtcNow;
            asset.UpdatedAt = DateTime.UtcNow;

            if (asset.PurchaseDate.Kind != DateTimeKind.Utc)
                asset.PurchaseDate = DateTime.SpecifyKind(asset.PurchaseDate, DateTimeKind.Utc);

            _context.FixedAssets.Add(asset);
            await _context.SaveChangesAsync();

            if (asset.PurchaseCost > 0)
            {
                // Determine which account to debit on the acquisition journal
                // CWIP mode  : Dr CWIP Account (staging) / Cr Acquisition Offset
                // Normal mode: Dr Asset Account (fixed asset) / Cr Acquisition Offset
                var debitAccountId = (isStaged && cwipAccountId.HasValue)
                    ? cwipAccountId.Value
                    : asset.AssetAccountId;

                var debitDescription = (isStaged && cwipAccountId.HasValue)
                    ? $"Stage to CWIP - {asset.AssetName} ({asset.AssetCode})"
                    : $"Capitalise asset cost - {asset.AssetName}";

                var journal = new JournalEntry
                {
                    JournalDate = asset.PurchaseDate,
                    Description = $"Asset acquisition: {asset.AssetName} ({asset.AssetCode})",
                    JournalType = "ASSET_ACQUISITION",
                    Status = "POSTED",
                    PostedAt = DateTime.UtcNow,
                    PostedBy = userId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                journal.JournalLines.Add(new JournalLine
                {
                    AccountId = debitAccountId,
                    DebitAmount = asset.PurchaseCost,
                    CreditAmount = 0,
                    LineDate = asset.PurchaseDate,
                    Description = debitDescription,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });

                journal.JournalLines.Add(new JournalLine
                {
                    AccountId = acquisitionCreditAccountId,
                    DebitAmount = 0,
                    CreditAmount = asset.PurchaseCost,
                    LineDate = asset.PurchaseDate,
                    Description = $"Acquisition offset - {asset.AssetName}",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });

                await _journalServices.CreateJournalEntryAsync(journal);
            }
            return asset;
        }

        public async Task<FixedAsset> UpdateAssetAsync(FixedAsset updated)
        {
            var existing = await _context.FixedAssets.FindAsync(updated.AssetId);
            if (existing == null) throw new Exception("Asset not found");

            existing.AssetName = updated.AssetName;
            existing.Description = updated.Description;
            existing.CategoryId = updated.CategoryId;
            existing.AssetAccountId = updated.AssetAccountId;
            existing.AccumDepnAccountId = updated.AccumDepnAccountId;
            existing.DepnExpenseAccountId = updated.DepnExpenseAccountId;
            existing.ResidualValue = updated.ResidualValue;
            existing.UsefulLifeYears = updated.UsefulLifeYears;
            existing.DepreciationMethod = updated.DepreciationMethod;
            existing.Notes = updated.Notes;
            
            // Update purchase cost, date and recalculate Book Value
            existing.PurchaseCost = updated.PurchaseCost;
            if (updated.PurchaseDate.Kind != DateTimeKind.Utc)
                existing.PurchaseDate = DateTime.SpecifyKind(updated.PurchaseDate, DateTimeKind.Utc);
            else
                existing.PurchaseDate = updated.PurchaseDate;
                
            existing.BookValue = updated.PurchaseCost - existing.AccumulatedDepreciation;
            
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        /// <summary>
        /// Transfers a staged CWIP asset to the fixed asset account and activates it for depreciation.
        /// Posts: Dr AssetAccount / Cr CwipAccount
        /// Sets Status = ACTIVE and resets PurchaseDate to capitalizationDate so depreciation
        /// is pro-rated correctly from the date the asset was brought into use.
        /// </summary>
        public async Task<FixedAsset> CapitalizeAssetAsync(int assetId, DateTime capitalizationDate, int userId = 1)
        {
            var asset = await _context.FixedAssets
                .Include(a => a.AssetAccount)
                .Include(a => a.CwipAccount)
                .FirstOrDefaultAsync(a => a.AssetId == assetId);

            if (asset == null)
                throw new Exception("Asset not found.");
            if (asset.Status != "CWIP")
                throw new Exception("Only assets with status CWIP can be capitalised.");
            if (!asset.CwipAccountId.HasValue)
                throw new Exception("No CWIP account linked to this asset.");

            if (capitalizationDate.Kind != DateTimeKind.Utc)
                capitalizationDate = DateTime.SpecifyKind(capitalizationDate, DateTimeKind.Utc);

            // Correcting journal: clear CWIP and put cost on the real asset account
            var journal = new JournalEntry
            {
                JournalDate = capitalizationDate,
                Description = $"Capitalise from CWIP: {asset.AssetName} ({asset.AssetCode})",
                JournalType = "ASSET_ACQUISITION",
                Status = "POSTED",
                PostedAt = DateTime.UtcNow,
                PostedBy = userId,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Dr Asset Account (fixed asset account)
            journal.JournalLines.Add(new JournalLine
            {
                AccountId = asset.AssetAccountId,
                DebitAmount = asset.PurchaseCost,
                CreditAmount = 0,
                LineDate = capitalizationDate,
                Description = $"Transfer from CWIP - {asset.AssetName}",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            });

            // Cr CWIP Account
            journal.JournalLines.Add(new JournalLine
            {
                AccountId = asset.CwipAccountId.Value,
                DebitAmount = 0,
                CreditAmount = asset.PurchaseCost,
                LineDate = capitalizationDate,
                Description = $"Clear CWIP staging - {asset.AssetName}",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            });

            await _journalServices.CreateJournalEntryAsync(journal);

            // Activate asset — depreciation starts from capitalization date
            asset.Status = "ACTIVE";
            asset.PurchaseDate = capitalizationDate;
            asset.CwipAccountId = null;  // no longer staged
            asset.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return asset;
        }

        public async Task DeactivateAssetAsync(int assetId)
        {
            var asset = await _context.FixedAssets.FindAsync(assetId);
            if (asset != null)
            {
                asset.IsActive = false;
                asset.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Safely removes a CWIP asset record.
        /// - If no acquisition journal exists (legacy record) → hard deletes the asset row.
        /// - If an acquisition journal is found → reverses it first, then hard deletes.
        /// Only valid for assets with Status == "CWIP". Returns a description of what was done.
        /// </summary>
        public async Task<string> DeleteCwipAssetAsync(int assetId, int userId = 1)
        {
            var asset = await _context.FixedAssets
                .Include(a => a.DepreciationEntries)
                .FirstOrDefaultAsync(a => a.AssetId == assetId);

            if (asset == null)
                throw new Exception("Asset not found.");

            if (asset.Status != "CWIP")
                throw new Exception("Only CWIP (staged) assets can be deleted using this method.");

            // Look for a linked ASSET_ACQUISITION journal by matching the asset code in the description.
            // The journal description is always: "Asset acquisition: {AssetName} ({AssetCode})"
            var acquisitionJournal = await _context.JournalEntries
                .Include(j => j.JournalLines)
                .Where(j => j.JournalType == "ASSET_ACQUISITION"
                         && j.Description.Contains(asset.AssetCode))
                .FirstOrDefaultAsync();

            string resultMessage;

            if (acquisitionJournal == null)
            {
                // No journal found — legacy record with no ledger impact. Safe to hard delete.
                _context.FixedAssets.Remove(asset);
                await _context.SaveChangesAsync();
                resultMessage = $"CWIP asset \"{asset.AssetName}\" deleted. No acquisition journal was found — record removed cleanly with no ledger impact.";
            }
            else
            {
                // A journal exists — reverse it to clear the CWIP account balance, then delete.
                var reversal = new JournalEntry
                {
                    JournalDate  = DateTime.UtcNow,
                    Description  = $"Reverse CWIP acquisition: {asset.AssetName} ({asset.AssetCode})",
                    JournalType  = "ASSET_REVERSAL",
                    Status       = "POSTED",
                    PostedAt     = DateTime.UtcNow,
                    PostedBy     = userId,
                    CreatedBy    = userId,
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow
                };

                // Mirror each line with Dr/Cr swapped
                foreach (var line in acquisitionJournal.JournalLines)
                {
                    reversal.JournalLines.Add(new JournalLine
                    {
                        AccountId     = line.AccountId,
                        DebitAmount   = line.CreditAmount,   // swap
                        CreditAmount  = line.DebitAmount,    // swap
                        LineDate      = DateTime.UtcNow,
                        Description   = $"Reversal: {line.Description}",
                        CreatedBy     = userId,
                        CreatedAt     = DateTime.UtcNow
                    });
                }

                await _journalServices.CreateJournalEntryAsync(reversal);

                _context.FixedAssets.Remove(asset);
                await _context.SaveChangesAsync();

                resultMessage = $"CWIP asset \"{asset.AssetName}\" deleted. Acquisition journal reversed to clear the CWIP account balance.";
            }

            return resultMessage;
        }

        private async Task<string> GenerateAssetCodeAsync()
        {
            var count = await _context.FixedAssets.CountAsync();
            return $"ASSET-{(count + 1):D4}";
        }

        #endregion

        #region Depreciation

        /// <summary>
        /// Calculates the depreciation amount for one period for a given asset.
        /// Pro-rated from the purchase date for the first period.
        /// </summary>
        public decimal CalculatePeriodDepreciation(FixedAsset asset, DateTime periodStartDate, DateTime periodEndDate)
        {
            var depreciableAmount = asset.PurchaseCost - asset.ResidualValue;
            if (depreciableAmount <= 0 || asset.BookValue <= asset.ResidualValue)
                return 0;

            // Strip time parts to operate on pure calendar dates
            var pStart = periodStartDate.Date;
            var pEnd = periodEndDate.Date;
            var purchase = asset.PurchaseDate.Date;

            // Determine the effective start of depreciation (purchase date or period start, whichever is later)
            var effectiveStart = purchase > pStart ? purchase : pStart;

            // How many days in the full period vs how many days we actually depreciate (inclusive of start and end dates)
            var totalDaysInPeriod = (pEnd - pStart).TotalDays + 1;
            var effectiveDays = (pEnd - effectiveStart).TotalDays + 1;

            if (effectiveDays <= 0 || totalDaysInPeriod <= 0) return 0;

            var prorataFactor = (decimal)(effectiveDays / totalDaysInPeriod);

            decimal periodDepreciation;

            if (asset.DepreciationMethod == "STRAIGHT_LINE")
            {
                // Annual depreciation / 12 months, pro-rated for partial periods
                var annualDepreciation = depreciableAmount / asset.UsefulLifeYears;
                var fullPeriodDepreciation = annualDepreciation / 12; // assuming monthly runs
                periodDepreciation = fullPeriodDepreciation * prorataFactor;
            }
            else // REDUCING_BALANCE
            {
                // Annual rate = 1 - (residualValue/cost)^(1/life) — simplified: 2/UsefulLife for double-declining
                var annualRate = 2m / asset.UsefulLifeYears; // double-declining rate
                var annualDepreciation = asset.BookValue * annualRate;
                var fullPeriodDepreciation = annualDepreciation / 12;
                periodDepreciation = fullPeriodDepreciation * prorataFactor;
            }

            // Cannot depreciate below residual value
            var remainingDepreciable = asset.BookValue - asset.ResidualValue;
            return Math.Min(Math.Round(periodDepreciation, 2), remainingDepreciable);
        }

        /// <summary>
        /// Runs depreciation for the given period for all (or selected) active assets.
        /// Posts one combined journal entry per run.
        /// </summary>
        public async Task<(int assetsProcessed, decimal totalAmount, JournalEntry journal)> RunDepreciationAsync(
            DateTime periodStartDate,
            DateTime periodEndDate,
            List<int> assetIds = null,
            int userId = 1)
        {
            var query = _context.FixedAssets
                .Include(a => a.DepreciationEntries)
                .Where(a => a.IsActive && a.Status == "ACTIVE");

            if (assetIds != null && assetIds.Count > 0)
                query = query.Where(a => assetIds.Contains(a.AssetId));

            var assets = await query.ToListAsync();

            if (!assets.Any())
                return (0, 0, null);

            // Ensure UTC
            if (periodStartDate.Kind != DateTimeKind.Utc) periodStartDate = DateTime.SpecifyKind(periodStartDate, DateTimeKind.Utc);
            if (periodEndDate.Kind != DateTimeKind.Utc) periodEndDate = DateTime.SpecifyKind(periodEndDate, DateTimeKind.Utc);

            // Build one journal entry for all assets in this run
            var journal = new JournalEntry
            {
                JournalDate = periodEndDate,
                Description = $"Depreciation run for period ending {periodEndDate:MMM yyyy}",
                JournalType = "DEPRECIATION",
                Status = "POSTED",
                PostedAt = DateTime.UtcNow,
                PostedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var assetsProcessed = 0;
            var totalDepreciation = 0m;

            // Group lines by account so we don't post hundreds of individual lines
            var depnByExpenseAccount = new Dictionary<int, decimal>();
            var depnByAccumAccount = new Dictionary<int, decimal>();
            var assetEntries = new List<(FixedAsset asset, decimal amount)>();

            foreach (var asset in assets)
            {
                var amount = CalculatePeriodDepreciation(asset, periodStartDate, periodEndDate);
                if (amount <= 0) continue;

                assetEntries.Add((asset, amount));

                // Accumulate by account
                if (!depnByExpenseAccount.ContainsKey(asset.DepnExpenseAccountId))
                    depnByExpenseAccount[asset.DepnExpenseAccountId] = 0;
                depnByExpenseAccount[asset.DepnExpenseAccountId] += amount;

                if (!depnByAccumAccount.ContainsKey(asset.AccumDepnAccountId))
                    depnByAccumAccount[asset.AccumDepnAccountId] = 0;
                depnByAccumAccount[asset.AccumDepnAccountId] += amount;

                totalDepreciation += amount;
            }

            if (!assetEntries.Any())
                return (0, 0, null);

            // Debit lines: Depreciation Expense accounts
            foreach (var kv in depnByExpenseAccount)
            {
                journal.JournalLines.Add(new JournalLine
                {
                    AccountId = kv.Key,
                    DebitAmount = kv.Value,
                    CreditAmount = 0,
                    LineDate = periodEndDate,
                    Description = $"Depreciation expense - period ending {periodEndDate:MMM yyyy}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Credit lines: Accumulated Depreciation accounts
            foreach (var kv in depnByAccumAccount)
            {
                journal.JournalLines.Add(new JournalLine
                {
                    AccountId = kv.Key,
                    DebitAmount = 0,
                    CreditAmount = kv.Value,
                    LineDate = periodEndDate,
                    Description = $"Accumulated depreciation - period ending {periodEndDate:MMM yyyy}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Post the journal via the journal service (handles balance updates)
            var postedJournal = await _journalServices.CreateJournalEntryAsync(journal);

            // Now save individual DepreciationEntry records and update asset book values
            foreach (var (asset, amount) in assetEntries)
            {
                asset.AccumulatedDepreciation += amount;
                asset.BookValue -= amount;
                asset.UpdatedAt = DateTime.UtcNow;

                // Check if fully depreciated
                if (asset.BookValue <= asset.ResidualValue + 0.01m)
                {
                    asset.BookValue = asset.ResidualValue;
                    asset.Status = "FULLY_DEPRECIATED";
                }

                var entry = new DepreciationEntry
                {
                    AssetId = asset.AssetId,
                    PeriodDate = periodEndDate,
                    DepreciationAmount = amount,
                    BookValueAfter = asset.BookValue,
                    JournalId = postedJournal.JournalId,
                    Notes = $"Period: {periodStartDate:dd MMM yyyy} - {periodEndDate:dd MMM yyyy}",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DepreciationEntries.Add(entry);
                assetsProcessed++;
            }

            await _context.SaveChangesAsync();
            return (assetsProcessed, totalDepreciation, postedJournal);
        }

        /// <summary>
        /// Returns a projected year-by-year depreciation schedule for an asset.
        /// </summary>
        public List<(int year, decimal depreciation, decimal bookValue)> GetDepreciationSchedule(FixedAsset asset)
        {
            var schedule = new List<(int year, decimal depreciation, decimal bookValue)>();
            var bookValue = asset.PurchaseCost - asset.AccumulatedDepreciation;
            var residual = asset.ResidualValue;
            var depreciableAmount = asset.PurchaseCost - residual;
            var years = (int)Math.Ceiling(asset.UsefulLifeYears);

            for (int year = 1; year <= years; year++)
            {
                decimal depn;
                if (asset.DepreciationMethod == "STRAIGHT_LINE")
                    depn = depreciableAmount / asset.UsefulLifeYears;
                else
                    depn = bookValue * (2m / asset.UsefulLifeYears);

                var remaining = bookValue - residual;
                depn = Math.Min(Math.Round(depn, 2), remaining);
                if (depn <= 0) break;

                bookValue -= depn;
                if (bookValue < residual) bookValue = residual;
                schedule.Add((year, depn, bookValue));

                if (bookValue <= residual + 0.01m) break;
            }

            return schedule;
        }

        #endregion

        #region Disposal

        public async Task<AssetDisposal> DisposeAssetAsync(
            int assetId,
            DateTime disposalDate,
            decimal saleProceeds,
            string disposalType,
            int? proceedsAccountId,
            string notes,
            int userId = 1)
        {
            var asset = await _context.FixedAssets
                .Include(a => a.DepreciationEntries)
                .FirstOrDefaultAsync(a => a.AssetId == assetId);

            if (asset == null) throw new Exception("Asset not found");
            if (asset.Status == "DISPOSED") throw new Exception("Asset has already been disposed");

            if (disposalDate.Kind != DateTimeKind.Utc) disposalDate = DateTime.SpecifyKind(disposalDate, DateTimeKind.Utc);

            var bookValue = asset.BookValue;
            var gainOrLoss = saleProceeds - bookValue;

            // Build disposal journal
            // Dr Accumulated Depreciation (clear it)
            // Dr Cash/Proceeds Account (if any proceeds)
            // Dr Loss on Asset / Cr Gain on Asset (difference)
            // Cr Asset Cost Account

            var gainAccountId = await GetAccountByNumberAsync("4220"); // Gain on Sale of Assets
            var lossAccountId = await GetAccountByNumberAsync("6100");  // Loss on Sale of Assets

            var journal = new JournalEntry
            {
                JournalDate = disposalDate,
                Description = $"Disposal of {asset.AssetName} ({asset.AssetCode})" + (string.IsNullOrWhiteSpace(notes) ? "" : $" - {notes}"),
                JournalType = "ASSET_DISPOSAL",
                Status = "POSTED",
                PostedAt = DateTime.UtcNow,
                PostedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Dr Accumulated Depreciation
            journal.JournalLines.Add(new JournalLine
            {
                AccountId = asset.AccumDepnAccountId,
                DebitAmount = asset.AccumulatedDepreciation,
                CreditAmount = 0,
                LineDate = disposalDate,
                Description = $"Remove accumulated depreciation - {asset.AssetName}",
                CreatedAt = DateTime.UtcNow
            });

            // Cr Asset Cost Account
            journal.JournalLines.Add(new JournalLine
            {
                AccountId = asset.AssetAccountId,
                DebitAmount = 0,
                CreditAmount = asset.PurchaseCost,
                LineDate = disposalDate,
                Description = $"Remove asset cost - {asset.AssetName}",
                CreatedAt = DateTime.UtcNow
            });

            // Dr Cash/Proceeds if applicable
            if (saleProceeds > 0 && proceedsAccountId.HasValue)
            {
                journal.JournalLines.Add(new JournalLine
                {
                    AccountId = proceedsAccountId.Value,
                    DebitAmount = saleProceeds,
                    CreditAmount = 0,
                    LineDate = disposalDate,
                    Description = $"Proceeds from disposal of {asset.AssetName}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Gain or Loss
            if (gainOrLoss > 0 && gainAccountId.HasValue)
            {
                journal.JournalLines.Add(new JournalLine
                {
                    AccountId = gainAccountId.Value,
                    DebitAmount = 0,
                    CreditAmount = gainOrLoss,
                    LineDate = disposalDate,
                    Description = $"Gain on disposal of {asset.AssetName}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (gainOrLoss < 0 && lossAccountId.HasValue)
            {
                journal.JournalLines.Add(new JournalLine
                {
                    AccountId = lossAccountId.Value,
                    DebitAmount = Math.Abs(gainOrLoss),
                    CreditAmount = 0,
                    LineDate = disposalDate,
                    Description = $"Loss on disposal of {asset.AssetName}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var postedJournal = await _journalServices.CreateJournalEntryAsync(journal);

            // Record disposal
            var disposal = new AssetDisposal
            {
                AssetId = assetId,
                DisposalDate = disposalDate,
                SaleProceeds = saleProceeds,
                BookValueAtDisposal = bookValue,
                GainOrLoss = gainOrLoss,
                DisposalType = disposalType,
                ProceedsAccountId = proceedsAccountId,
                JournalId = postedJournal.JournalId,
                Notes = notes,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AssetDisposals.Add(disposal);

            // Update asset status
            asset.Status = "DISPOSED";
            asset.IsActive = false;
            asset.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return disposal;
        }

        private async Task<int?> GetAccountByNumberAsync(string accountNumber)
        {
            var account = await _context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
            return account?.AccountId;
        }

        #endregion

        #region Summary Stats

        public async Task<AssetSummaryStats> GetSummaryStatsAsync()
        {
            var assets = await _context.FixedAssets.Where(a => a.IsActive).ToListAsync();
            return new AssetSummaryStats
            {
                TotalAssets = assets.Count,
                TotalCost = assets.Sum(a => a.PurchaseCost),
                TotalAccumulatedDepreciation = assets.Sum(a => a.AccumulatedDepreciation),
                TotalBookValue = assets.Sum(a => a.BookValue),
                FullyDepreciatedCount = assets.Count(a => a.Status == "FULLY_DEPRECIATED")
            };
        }

        public class AssetSummaryStats
        {
            public int TotalAssets { get; set; }
            public decimal TotalCost { get; set; }
            public decimal TotalAccumulatedDepreciation { get; set; }
            public decimal TotalBookValue { get; set; }
            public int FullyDepreciatedCount { get; set; }
        }

        #endregion
    }
}
