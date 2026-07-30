using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using PrimeAppBooks.Models.Windows;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.Temp_Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Windows
{
    public partial class WndImportExpensesViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly JournalServices _journalService;
        private readonly ChartOfAccountsServices _coaService;
        private readonly SettingsService _settingsService;
        private readonly BoxServices _msgBox = new();
        private static readonly System.Threading.SemaphoreSlim _importLock = new(1, 1);

        // ── Import Type Selection ──────────────────────────────────────────────
        public string[] ImportTypes { get; } = new[] { "Expenses", "Specific Incomes & Liabilities" };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsExpenseImport))]
        private string _importType = "Expenses";

        public bool IsExpenseImport => ImportType == "Expenses";

        // Maps the "cash-equivalent" side of a transaction to its GL account,
        // based on which book/type it came from.
        private static readonly Dictionary<string, string> PaymentTypeAccountMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Cash", "1000" },
            { "Ecocash", "1040" },
            { "EFT", "1020" }
        };


        // ── Auto-Mapping for Incomes / Liabilities ─────────────────────────────
        private static readonly Dictionary<string, string> IncomeAccountMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Examination Fees", "4130" },
            { "Sports Fees", "4120" },
            { "Registration Fees", "4110" },
            { "Transport Fees", "4160" },
            { "Trip", "2540" },
            { "Bond Paper", "4100" },
            { "Report Book", "4140" },
            { "Blazer", "2550" },
            { "Tracksuit", "2550" },
            { "Anorak", "2550" },
            { "Jersey", "2550" },
            { "Half-Jersey", "2550" },
            { "Skirt", "2550" },
            { "Trousers", "2550" },
            { "Shirt", "2550" },
            { "T-Shirt", "2550" },
            { "Sunhat", "2550" },
            { "Sports Short", "2550" },
            { "Tie", "2550" },
            { "Woolen Hat", "2550" }
        };

        // ── Date range ─────────────────────────────────────────────────────────
        [ObservableProperty]
        private DateTime _startDate = DateTime.Today.AddMonths(-1);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        // ── Status / UI state ──────────────────────────────────────────────────
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _loadingMessage = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string _lastExpenseImportDateText = "Never";

        [ObservableProperty]
        private string _lastImportSessionSummary;

        // ── Grid data ──────────────────────────────────────────────────────────
        public ObservableCollection<CashbookExpenseRow> Rows { get; } = new();
        public ObservableCollection<ChartOfAccount> Accounts { get; } = new();

        // ── Totals ─────────────────────────────────────────────────────────────
        [ObservableProperty]
        private int _totalRows;

        [ObservableProperty]
        private int _selectedRows;

        [ObservableProperty]
        private decimal _selectedTotal;

        // ── Default Cash account (pre-filled credit side) ───────────────────
        private ChartOfAccount _cashAccount;

        // ── Connection settings (read from SettingsService) ────────────────────
        [ObservableProperty]
        private bool _useExternalConnection;

        [ObservableProperty]
        private string _externalHost = "localhost";

        [ObservableProperty]
        private string _externalPort = "5432";

        [ObservableProperty]
        private string _externalDatabase = "SchoolManagementSystem";

        [ObservableProperty]
        private string _externalUsername = "postgres";

        [ObservableProperty]
        private string _externalPassword = "";

        // ── Select-all helper ──────────────────────────────────────────────────
        [ObservableProperty]
        private bool _allSelected = true;

        // ── Close callback ─────────────────────────────────────────────────────
        public Action CloseAction { get; set; }
        public Action MinimizeAction { get; set; }

        // ======================================================================
        public WndImportExpensesViewModel(
            AppDbContext context,
            JournalServices journalService,
            ChartOfAccountsServices coaService,
            SettingsService settingsService)
        {
            _context = context;
            _journalService = journalService;
            _coaService = coaService;
            _settingsService = settingsService;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadConnectionSettingsAsync();
            await LoadAccountsAsync();

            // Load last expense import date
            var lastDate = await _settingsService.GetSettingAsync(SettingConstants.LastExpenseImportDate);
            if (DateTime.TryParse(lastDate, out var dt))
            {
                LastExpenseImportDateText = dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                StartDate = dt.AddDays(1).Date;
            }
        }

        private ChartOfAccount ResolvePaymentAccount(string paymentType)
        {
            if (!string.IsNullOrWhiteSpace(paymentType)
                && PaymentTypeAccountMapping.TryGetValue(paymentType, out var accountNumber))
            {
                var acct = Accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
                if (acct != null)
                    return acct;
            }

            return _cashAccount; // fallback if not configured yet
        }
        // ── Commands ───────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task LoadExpenses()
        {
            if (IsLoading) return;
            IsLoading = true;
            LoadingMessage = "Fetching cashbook and bank book transactions from Academy...";
            StatusMessage = string.Empty;

            try
            {
                // Save connection settings in case user modified them on the UI
                await _settingsService.SetSettingAsync(SettingConstants.UseExternalConnection, UseExternalConnection.ToString());
                if (UseExternalConnection)
                {
                    await _settingsService.SetSettingAsync(SettingConstants.ExternalHost, ExternalHost ?? "localhost");
                    await _settingsService.SetSettingAsync(SettingConstants.ExternalPort, ExternalPort ?? "5432");
                    await _settingsService.SetSettingAsync(SettingConstants.ExternalDatabase, ExternalDatabase ?? "SchoolManagementSystem");
                    await _settingsService.SetSettingAsync(SettingConstants.ExternalUsername, ExternalUsername ?? "postgres");
                    await _settingsService.SetSettingAsync(SettingConstants.ExternalPassword, ExternalPassword ?? "");
                }

                // Build connection string
                string connStr = BuildAcademyConnectionString();

                // Fetch from both sources
                var cashRows = await FetchCashbookExpensesAsync(StartDate, EndDate, connStr, ImportType);
                var bankRows = await FetchBankBookAsync(StartDate, EndDate, connStr, ImportType);

                // Normalize both into one shape so the mapping loop below is source-agnostic.
                // Cashbook rows are always "Cash"; bank_book rows carry their own type
                // ("Ecocash", "EFT", ...).
                var combined = cashRows
                    .Select(r => (
                        Id: r.cb_id,
                        Date: r.cb_date,
                        DocNumber: r.cb_doc_number,
                        Description: r.cb_description,
                        Amount: r.amount,
                        CurrencyCode: r.cb_currency_code,
                        Tag: r.cb_tag,
                        PaymentType: "Cash"))
                    .Concat(bankRows.Select(r => (
                        Id: r.bk_id,
                        Date: r.bk_date,
                        DocNumber: r.bk_doc_number,
                        Description: r.bk_description,
                        Amount: r.amount,
                        CurrencyCode: r.bk_currency_code,
                        Tag: r.bk_tag,
                        PaymentType: r.bk_type)))
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Id)
                    .ToList();

                // Map to observable staging rows
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Rows.Clear();
                    foreach (var r in combined)
                    {
                        // Historic rows (pre cb_tag/bk_tag rollout) carry tag = "default".
                        // For those, the description field still held the static category
                        // value at the time they were written, so fall back to it for both
                        // display and account matching.
                        bool isLegacyTag = string.IsNullOrEmpty(r.Tag) || r.Tag == "default";
                        var matchKey = isLegacyTag ? (r.Description ?? string.Empty) : r.Tag;

                        // The "cash-equivalent" side of the transaction: Cash, Ecocash, or EFT account.
                        var paymentAccount = ResolvePaymentAccount(r.PaymentType);

                        var row = new CashbookExpenseRow
                        {
                            CbId = r.Id,
                            Date = r.Date,
                            DocNumber = r.DocNumber ?? string.Empty,
                            Description = r.Description ?? string.Empty,
                            Tag = matchKey,
                            Amount = r.Amount,
                            CurrencyCode = r.CurrencyCode ?? "USD",
                            IsSelected = true
                        };

                        if (IsExpenseImport)
                        {
                            row.SelectedCreditAccount = paymentAccount;
                            Rows.Add(row);
                        }
                        else
                        {
                            if (!IncomeAccountMapping.TryGetValue(matchKey, out var accountNum))
                                continue;

                            var matchedAccount = Accounts.FirstOrDefault(a => a.AccountNumber == accountNum);
                            if (matchedAccount == null)
                                continue;

                            row.SelectedDebitAccount = paymentAccount;
                            row.SelectedCreditAccount = matchedAccount;
                            Rows.Add(row);
                        }
                    }

                    TotalRows = Rows.Count;
                    RefreshTotals();
                });

                StatusMessage = Rows.Count == 0
                    ? $"No {ImportType.ToLower()} found for the selected period."
                    : $"Loaded {Rows.Count} {ImportType.ToLower()}. Select accounts and click 'Import Selected'.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadExpenses error: {ex}");
                StatusMessage = $"Error loading expenses: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ToggleSelectAll()
        {
            bool newState = !AllSelected;
            foreach (var row in Rows)
                row.IsSelected = newState;
            AllSelected = newState;
            RefreshTotals();
        }

        [RelayCommand]
        private async Task ImportSelected()
        {
            var toImport = Rows.Where(r => r.IsSelected).ToList();
            if (!toImport.Any())
            {
                _msgBox.ShowMessage("No rows selected for import.", "Import Expenses", "InfoOutline");
                return;
            }

            // Validate accounts
            var missingAccount = toImport.FirstOrDefault(r => !r.DebitAccountId.HasValue);
            if (missingAccount != null)
            {
                _msgBox.ShowMessage(
                    $"Row '{missingAccount.DocNumber}' has no expense account selected.\nPlease assign an expense account to every selected row.",
                    "Validation Error", "Warning");
                return;
            }

            if (!toImport.All(r => r.CreditAccountId.HasValue))
            {
                _msgBox.ShowMessage("Some rows are missing a credit (cash) account.", "Validation Error", "Warning");
                return;
            }

            await _importLock.WaitAsync();
            IsLoading = true;
            LoadingMessage = "Importing expense journals...";

            var sessionId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            // Create import session record
            var session = new ImportSession
            {
                SessionId = $"EXP-{sessionId}",
                ImportDate = DateTime.UtcNow,
                StartDate = DateTime.SpecifyKind(StartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(EndDate, DateTimeKind.Utc),
                Status = "IN_PROGRESS",
                IncludeOpeningBalances = false
            };
            _context.ImportSessions.Add(session);
            await _context.SaveChangesAsync();

            int imported = 0;
            int skipped = 0;
            decimal totalAmount = 0m;

            try
            {
                foreach (var row in toImport)
                {
                    // Build fingerprint to prevent duplicates
                    var fingerprint = $"EXP-{sessionId}-{row.Fingerprint}";

                    // Skip if already imported (same doc number + date + amount)
                    if (await _context.JournalEntries.AnyAsync(j => j.Reference.EndsWith(row.Fingerprint)))
                    {
                        skipped++;
                        continue;
                    }

                    var julDate = row.Date.Kind == DateTimeKind.Utc
                        ? row.Date
                        : DateTime.SpecifyKind(row.Date, DateTimeKind.Utc);

                    var journal = new JournalEntry
                    {
                        JournalDate = julDate,
                        Description = row.Description,
                        Reference = fingerprint,
                        JournalNumber = $"EXP-{sessionId}-{imported + 1:D4}", // Pre-generate unique number
                        JournalType = "EXPENSE",
                        Status = "POSTED",
                        PostedAt = DateTime.UtcNow,
                        Amount = row.Amount,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // DR: Expense account
                    journal.JournalLines.Add(new JournalLine
                    {
                        AccountId = row.DebitAccountId!.Value,
                        DebitAmount = row.Amount,
                        CreditAmount = 0,
                        Description = row.Description,
                        Reference = fingerprint,
                        LineDate = julDate,
                        CreatedAt = DateTime.UtcNow
                    });

                    // CR: Cash account
                    journal.JournalLines.Add(new JournalLine
                    {
                        AccountId = row.CreditAccountId!.Value,
                        DebitAmount = 0,
                        CreditAmount = row.Amount,
                        Description = row.Description,
                        Reference = fingerprint,
                        LineDate = julDate,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _journalService.CreateJournalEntryAsync(journal);
                    imported++;
                    totalAmount += row.Amount;

                    LoadingMessage = $"Imported {imported}/{toImport.Count}...";
                }

                // Finalise session - reload from DB first to avoid context state conflicts
                var dbSession = await _context.ImportSessions.FirstOrDefaultAsync(s => s.SessionId == session.SessionId);
                if (dbSession != null)
                {
                    dbSession.TransactionsCount = imported;
                    dbSession.TotalAmount = totalAmount;
                    dbSession.Status = "COMPLETED";
                    _context.ImportSessions.Update(dbSession);
                    await _context.SaveChangesAsync();
                }

                // Persist last import date + session id
                await _settingsService.SetSettingAsync(SettingConstants.LastExpenseImportDate, EndDate.ToString("O"));
                await _settingsService.SetSettingAsync(SettingConstants.LastExpenseImportSessionId, session.SessionId);
                LastExpenseImportDateText = EndDate.ToString("yyyy-MM-dd");

                LastImportSessionSummary =
                    $"Expense Import Session {session.SessionId} Summary:\n" +
                    $"• Imported: {imported}\n" +
                    $"• Skipped (duplicates): {skipped}\n" +
                    $"• Total Amount: {totalAmount:N2}\n" +
                    $"• Range: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}\n" +
                    $"• Completed: {DateTime.Now:yyyy-MM-dd HH:mm}";

                StatusMessage = $"✅ Import complete — {imported} journal(s) posted, {skipped} skipped.";

                _msgBox.ShowMessage(
                    $"Cashbook expense import complete!\n\n" +
                    $"Journals posted:  {imported}\n" +
                    $"Duplicates skipped: {skipped}\n" +
                    $"Total amount: {totalAmount:N2}",
                    "Import Complete", "CheckCircleOutline");
            }
            catch (Exception ex)
            {
                // Reload session from DB to avoid context state conflicts before updating
                var dbSession = await _context.ImportSessions.FirstOrDefaultAsync(s => s.SessionId == session.SessionId);
                if (dbSession != null)
                {
                    dbSession.Status = "FAILED";
                    _context.ImportSessions.Update(dbSession);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception saveEx)
                    {
                        Debug.WriteLine($"Failed to mark session as failed: {saveEx}");
                    }
                }

                // Build detailed error information
                var errorDetails = new System.Text.StringBuilder();
                errorDetails.AppendLine("=== IMPORT FAILED ===");
                errorDetails.AppendLine($"Session ID: {session.SessionId}");
                errorDetails.AppendLine($"Imported So Far: {imported} / {toImport.Count}");
                errorDetails.AppendLine($"Total Amount So Far: {totalAmount:N2}");
                errorDetails.AppendLine();
                errorDetails.AppendLine("Exception Details:");
                errorDetails.AppendLine($"Type: {ex.GetType().Name}");
                errorDetails.AppendLine($"Message: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    errorDetails.AppendLine();
                    errorDetails.AppendLine("Inner Exception:");
                    errorDetails.AppendLine($"Type: {ex.InnerException.GetType().Name}");
                    errorDetails.AppendLine($"Message: {ex.InnerException.Message}");
                    
                    if (ex.InnerException.InnerException != null)
                    {
                        errorDetails.AppendLine();
                        errorDetails.AppendLine("Nested Inner Exception:");
                        errorDetails.AppendLine($"Type: {ex.InnerException.InnerException.GetType().Name}");
                        errorDetails.AppendLine($"Message: {ex.InnerException.InnerException.Message}");
                    }
                }
                
                errorDetails.AppendLine();
                errorDetails.AppendLine("Stack Trace:");
                errorDetails.AppendLine(ex.StackTrace);

                string fullErrorLog = errorDetails.ToString();
                Debug.WriteLine(fullErrorLog);

                // Show user-friendly message with more detail
                string userMessage = $"Import failed after processing {imported} of {toImport.Count} rows.\n\n" +
                    $"Error: {ex.GetType().Name}\n" +
                    $"{ex.Message}";

                if (ex.InnerException != null)
                {
                    userMessage += $"\n\nDetails: {ex.InnerException.Message}";
                }

                _msgBox.ShowMessage(userMessage, "Import Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
                _importLock.Release();
            }
        }

        [RelayCommand]
        private async Task UndoLastExpenseImport()
        {
            var lastSessionId = await _settingsService.GetSettingAsync(SettingConstants.LastExpenseImportSessionId);
            if (string.IsNullOrEmpty(lastSessionId))
            {
                _msgBox.ShowMessage("No recent expense import session found.", "Undo", "InfoOutline");
                return;
            }

            bool confirmed = _msgBox.ShowConfirmation(
                $"Undo expense import session '{lastSessionId}'?\n\nThis will delete all journal entries from that import. This cannot be undone.",
                "Undo Expense Import", "Warning");

            if (!confirmed) return;

            await _importLock.WaitAsync();
            IsLoading = true;
            LoadingMessage = "Reversing last expense import...";

            try
            {
                var journals = await _context.JournalEntries
                    .Include(j => j.JournalLines)
                    .Where(j => j.Reference.Contains(lastSessionId))
                    .ToListAsync();

                int deleted = journals.Count;
                if (journals.Any())
                {
                    var accountDeltas = new Dictionary<int, decimal>();
                    foreach (var entry in journals)
                    {
                        if (entry.Status == "POSTED")
                        {
                            foreach (var line in entry.JournalLines)
                            {
                                accountDeltas[line.AccountId] = accountDeltas.GetValueOrDefault(line.AccountId) + line.CreditAmount - line.DebitAmount;
                            }
                        }
                    }

                    if (accountDeltas.Any())
                    {
                        var accountIds = accountDeltas.Keys.ToList();
                        var accounts = await _context.ChartOfAccounts
                            .Where(a => accountIds.Contains(a.AccountId))
                            .ToListAsync();

                        foreach (var account in accounts)
                        {
                            account.CurrentBalance += accountDeltas[account.AccountId];
                            account.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    _context.JournalEntries.RemoveRange(journals);
                }

                // Mark session as reversed
                var session = await _context.ImportSessions.FindAsync(lastSessionId);
                if (session != null)
                {
                    session.Status = "REVERSED";
                    _context.ImportSessions.Update(session);
                }
                await _context.SaveChangesAsync();

                StatusMessage = $"↩️ Undo complete — {deleted} journal(s) removed.";
                _msgBox.ShowMessage($"Undo complete. {deleted} expense journal(s) removed.", "Undo Complete", "CheckCircleOutline");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UndoLastExpenseImport error: {ex}");
                _msgBox.ShowMessage($"Undo failed: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
                _importLock.Release();
            }
        }

        [RelayCommand]
        private void Close() => CloseAction?.Invoke();

        [RelayCommand]
        private void Minimize() => MinimizeAction?.Invoke();

        // ── Row totals tracking ────────────────────────────────────────────────
        public void RefreshTotals()
        {
            var selected = Rows.Where(r => r.IsSelected).ToList();
            SelectedRows = selected.Count;
            SelectedTotal = selected.Sum(r => r.Amount);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private async Task LoadAccountsAsync()
        {
            try
            {
                var accounts = await _context.ChartOfAccounts
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.AccountNumber)
                    .ToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Accounts.Clear();
                    foreach (var a in accounts) Accounts.Add(a);
                });

                // Find Cash account (usually 1000)
                var cash = accounts.FirstOrDefault(a => a.AccountNumber == "1000")
                        ?? accounts.FirstOrDefault(a => a.AccountName.Contains("Cash", StringComparison.OrdinalIgnoreCase));
                if (cash != null)
                {
                    _cashAccount = cash;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadAccountsAsync error: {ex.Message}");
            }
        }

        private async Task LoadConnectionSettingsAsync()
        {
            UseExternalConnection = await _settingsService.GetSettingAsync<bool>(SettingConstants.UseExternalConnection, false);
            ExternalHost          = await _settingsService.GetSettingAsync(SettingConstants.ExternalHost) ?? "localhost";
            ExternalPort          = await _settingsService.GetSettingAsync(SettingConstants.ExternalPort) ?? "5432";
            ExternalDatabase      = await _settingsService.GetSettingAsync(SettingConstants.ExternalDatabase) ?? "SchoolManagementSystem";
            ExternalUsername      = await _settingsService.GetSettingAsync(SettingConstants.ExternalUsername) ?? "postgres";
            ExternalPassword      = await _settingsService.GetSettingAsync(SettingConstants.ExternalPassword) ?? "";
        }

        private string BuildAcademyConnectionString()
        {
            string connStr;
            if (UseExternalConnection)
                connStr = $"Host={ExternalHost};Port={ExternalPort};Database={ExternalDatabase};" +
                          $"Username={ExternalUsername};Password={ExternalPassword}";
            else
                connStr = Configurations.AppConfig.GetConnectionString("SecondaryDatabaseV18");

            return Fetches.NormalizeConnectionString(connStr);
        }

        private async Task<List<CashbookExpenseRawRow>> FetchCashbookExpensesAsync(DateTime start, DateTime end, string connStr, string importType)
        {
            var results = new List<CashbookExpenseRawRow>();
            bool isExpense = importType == "Expenses";

            try
            {
                using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync();

                string amountField = isExpense ? "cb_credit" : "cb_debit";
                string debitCreditType = isExpense ? "CR" : "DR";

                var query = $@"
            SELECT cb_id, cb_date, cb_doc_number, cb_description, {amountField}, cb_currency_code, cb_tag
            FROM cashbook
            WHERE cb_date >= @start AND cb_date <= @end
              AND cb_debit_credit = '{debitCreditType}'
              AND cb_type = 'Cash'
            ORDER BY cb_date ASC, cb_id ASC";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.CommandTimeout = 120;
                cmd.Parameters.AddWithValue("start", DateTime.SpecifyKind(start.Date, DateTimeKind.Utc));
                cmd.Parameters.AddWithValue("end", DateTime.SpecifyKind(end.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new CashbookExpenseRawRow
                    {
                        cb_id = reader.GetInt64(0),
                        cb_date = reader.GetDateTime(1),
                        cb_doc_number = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        cb_description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        amount = reader.GetDecimal(4),
                        cb_currency_code = reader.IsDBNull(5) ? "USD" : reader.GetString(5),
                        cb_tag = reader.IsDBNull(6) ? "" : reader.GetString(6)
                    });
                }
            }
            catch (Exception ex) when (ex is NpgsqlException || ex is TimeoutException)
            {
                Debug.WriteLine($"FetchCashbookExpensesAsync connection error: {ex.Message}");
                _msgBox.ShowMessage(
                    "Could not connect to the Academy database within 10 seconds. Please check the host, port, database name, username/password, network/VPN, and whether PostgreSQL is running.",
                    "Academy Connection Timeout",
                    "ErrorOutline");
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FetchCashbookExpensesAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        private async Task<List<BankBookRawRow>> FetchBankBookAsync(DateTime start, DateTime end, string connStr, string importType)
        {
            var results = new List<BankBookRawRow>();
            bool isExpense = importType == "Expenses";

            try
            {
                using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync();

                string amountField = isExpense ? "bk_credit" : "bk_debit";
                string debitCreditType = isExpense ? "CR" : "DR";

                var query = $@"
                    SELECT bk_id, bk_date, bk_doc_number, bk_description, {amountField}, bk_currency_code, bk_tag, bk_type
                    FROM bank_book
                    WHERE bk_date >= @start AND bk_date <= @end
                      AND bk_debit_credit = '{debitCreditType}'
                    ORDER BY bk_date ASC, bk_id ASC";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.CommandTimeout = 120;
                cmd.Parameters.AddWithValue("start", DateTime.SpecifyKind(start.Date, DateTimeKind.Utc));
                cmd.Parameters.AddWithValue("end", DateTime.SpecifyKind(end.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new BankBookRawRow
                    {
                        bk_id = Convert.ToInt64(reader.GetValue(0)), // bk_id may be int4, not bigint
                        bk_date = reader.GetDateTime(1),
                        bk_doc_number = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        bk_description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        amount = reader.GetDecimal(4),
                        bk_currency_code = reader.IsDBNull(5) ? "USD" : reader.GetString(5),
                        bk_tag = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        bk_type = reader.IsDBNull(7) ? "" : reader.GetString(7)
                    });
                }
            }
            catch (Exception ex) when (ex is NpgsqlException || ex is TimeoutException)
            {
                Debug.WriteLine($"FetchBankBookAsync connection error: {ex.Message}");
                _msgBox.ShowMessage(
                    "Could not connect to the Academy database within 10 seconds. Please check the host, port, database name, username/password, network/VPN, and whether PostgreSQL is running.",
                    "Academy Connection Timeout",
                    "ErrorOutline");
                return results;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FetchBankBookAsync error: {ex.Message}");
                throw;
            }

            return results;
        }
    }

    public class CashbookExpenseRawRow
    {
        public long cb_id { get; set; }
        public DateTime cb_date { get; set; }
        public string cb_doc_number { get; set; }
        public string cb_description { get; set; }
        public string cb_tag { get; set; }
        public decimal amount { get; set; }
        public string cb_currency_code { get; set; }
    }

    public class BankBookRawRow
    {
        public long bk_id { get; set; }
        public DateTime bk_date { get; set; }
        public string bk_doc_number { get; set; }
        public string bk_description { get; set; }
        public string bk_tag { get; set; }
        public string bk_type { get; set; }
        public decimal amount { get; set; }
        public string bk_currency_code { get; set; }
    }
}
