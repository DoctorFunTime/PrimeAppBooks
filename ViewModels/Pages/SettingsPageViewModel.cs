using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Models.Temp_Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Services.Temp_Service;
using PrimeAppBooks.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();
        private readonly ChartOfAccountsServices _coaService;
        private readonly JournalServices _journalService;
        private readonly AppDbContext _context;
        private readonly SettingsService _settingsService;
        private readonly Fetches fetches = new();
        private static readonly System.Threading.SemaphoreSlim _dbLock = new(1, 1);

        [ObservableProperty]
        private string _companyName;

        [ObservableProperty]
        private string _companyLegalName;

        [ObservableProperty]
        private string _companyEmail;

        [ObservableProperty]
        private string _companyPhone;

        [ObservableProperty]
        private string _companyAddress;

        [ObservableProperty]
        private string _taxId;

        [ObservableProperty]
        private string _businessNumber;

        [ObservableProperty]
        private Currency _selectedBaseCurrency;

        // Regional & Format
        [ObservableProperty]
        private string _dateFormat;
        [ObservableProperty]
        private string _numberFormat;
        [ObservableProperty]
        private string _timeZone;

        // Interface Preferences
        [ObservableProperty]
        private bool _darkMode;
        [ObservableProperty]
        private bool _enableAnimations;
        [ObservableProperty]
        private bool _showTooltips;
        [ObservableProperty]
        private bool _compactView;
        [ObservableProperty]
        private bool _showGridLines;
        [ObservableProperty]
        private bool _autoSave;

        // Accounting Rules
        [ObservableProperty]
        private string _fiscalYearStart;
        [ObservableProperty]
        private bool _autoCloseFiscalYear;
        [ObservableProperty]
        private bool _lockClosedPeriods;
        [ObservableProperty]
        private bool _warnClosedPeriods;
        [ObservableProperty]
        private string _invoicePrefix;
        [ObservableProperty]
        private string _receiptPrefix;
        [ObservableProperty]
        private string _startingNumber;
        [ObservableProperty]
        private bool _autoNumberTransactions;
        [ObservableProperty]
        private bool _requireJournalApproval;
        [ObservableProperty]
        private bool _allowNegativeInventory;
        [ObservableProperty]
        private bool _trackCostCenter;
        [ObservableProperty]
        private bool _multiCurrency;

        // Tax Configuration
        [ObservableProperty]
        private string _defaultSalesTaxRate;
        [ObservableProperty]
        private string _defaultPurchaseTaxRate;
        [ObservableProperty]
        private bool _taxInclusive;
        [ObservableProperty]
        private bool _autoCalculateTaxes;
        [ObservableProperty]
        private bool _trackTaxByLine;
        [ObservableProperty]
        private bool _autoGenerateTaxReports;

        // Security & Access
        [ObservableProperty]
        private string _sessionTimeout;
        [ObservableProperty]
        private string _passwordExpirationDays;
        [ObservableProperty]
        private bool _requireStrongPasswords;
        [ObservableProperty]
        private bool _twoFactorAuth;
        [ObservableProperty]
        private bool _autoLogout;
        [ObservableProperty]
        private bool _lockAfterFailedAttempts;

        // Backup & Data
        [ObservableProperty]
        private string _backupFrequency;
        [ObservableProperty]
        private string _retentionPeriod;
        [ObservableProperty]
        private string _backupLocation;
        [ObservableProperty]
        private bool _autoBackupEnabled;
        [ObservableProperty]
        private bool _compressBackups;
        [ObservableProperty]
        private bool _encryptBackups;
        [ObservableProperty]
        private bool _verifyBackups;
        [ObservableProperty]
        private bool _emailBackupNotifications;
        [ObservableProperty]
        private bool _cloudBackupSync;

        // Reports & Print
        [ObservableProperty]
        private string _defaultReportFormat;
        [ObservableProperty]
        private string _reportRefreshInterval;
        [ObservableProperty]
        private bool _showLogoOnReports;
        [ObservableProperty]
        private bool _includePageNumbers;
        [ObservableProperty]
        private bool _showGenerationTime;
        [ObservableProperty]
        private bool _enableReportDrillDown;
        [ObservableProperty]
        private bool _autoEmailReports;
        [ObservableProperty]
        private bool _autoSaveReports;

        // Notifications
        [ObservableProperty]
        private bool _showDesktopNotifications;
        [ObservableProperty]
        private bool _playNotificationSounds;
        [ObservableProperty]
        private bool _showTrayNotifications;
        [ObservableProperty]
        private bool _notifyOnUpdates;

        // Integrations
        [ObservableProperty]
        private bool _enableBankFeeds;
        [ObservableProperty]
        private bool _autoMatchTransactions;
        [ObservableProperty]
        private bool _enableOnlinePayments;
        [ObservableProperty]
        private bool _syncPaymentConfirmations;

        // External Import Connection
        [ObservableProperty]
        private bool _useExternalConnection;
        [ObservableProperty]
        private string _externalHost;
        [ObservableProperty]
        private string _externalPort = "5432";
        [ObservableProperty]
        private string _externalDatabase;
        [ObservableProperty]
        private string _externalUsername;
        [ObservableProperty]
        private string _externalPassword;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _loadingMessage;

        [ObservableProperty]
        private DateTime _importStartDate = DateTime.Today;

        [ObservableProperty]
        private string _lastImportDateText = "Never";

        [ObservableProperty]
        private bool _includeOpeningBalances = false;

        [ObservableProperty]
        private DateTime _importEndDate = DateTime.Today;

        [ObservableProperty]
        private string _lastImportSessionSummary;

        // Expense Import
        [ObservableProperty]
        private string _lastExpenseImportDateText = "Never";

        public ObservableCollection<Currency> Currencies { get; } = new();
        public ObservableCollection<string> DateFormats { get; } = new() { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" };
        public ObservableCollection<string> NumberFormats { get; } = new() { "1,234.56", "1.234,56", "1 234,56" };
        public ObservableCollection<string> TimeZones { get; } = new(TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.DisplayName));
        public ObservableCollection<string> FiscalYearStarts { get; } = new() { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        public ObservableCollection<string> BackupFrequencies { get; } = new() { "Daily", "Weekly", "Monthly", "On Exit" };
        public ObservableCollection<string> RetentionPeriods { get; } = new() { "30 Days", "90 Days", "1 Year", "Forever" };
        public ObservableCollection<string> ReportFormats { get; } = new() { "PDF", "Excel", "CSV", "HTML" };
        public ObservableCollection<string> RefreshIntervals { get; } = new() { "Manual", "5 Minutes", "15 Minutes", "30 Minutes", "Hourly" };

        public SettingsPageViewModel(
            INavigationService navigationService, 
            IServiceProvider serviceProvider,
            ChartOfAccountsServices coaService,
            JournalServices journalService,
            AppDbContext context,
            SettingsService settingsService)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _coaService = coaService;
            _journalService = journalService;
            _context = context;
            _settingsService = settingsService;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCurrenciesAsync();
            await LoadSettingsAsync();

            // Load last student import date
            var lastImportDateStr = await _settingsService.GetSettingAsync(SettingConstants.LastStudentImportDate);
            if (DateTime.TryParse(lastImportDateStr, out var lastDate))
            {
                LastImportDateText = lastDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                ImportStartDate = lastDate.AddDays(1).Date;
            }

            // Load last expense import date
            var lastExpDateStr = await _settingsService.GetSettingAsync(SettingConstants.LastExpenseImportDate);
            if (DateTime.TryParse(lastExpDateStr, out var lastExpDate))
                LastExpenseImportDateText = lastExpDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        private async Task LoadCurrenciesAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var currencies = await context.Currencies.OrderBy(c => c.CurrencyCode).ToListAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Currencies.Clear();
                    foreach (var c in currencies) Currencies.Add(c);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading currencies: {ex.Message}");
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();

                // Company Info
                CompanyName = await settingsService.GetSettingAsync(SettingConstants.CompanyName) ?? "";
                CompanyLegalName = await settingsService.GetSettingAsync(SettingConstants.CompanyLegalName) ?? "";
                CompanyEmail = await settingsService.GetSettingAsync(SettingConstants.CompanyEmail) ?? "";
                CompanyPhone = await settingsService.GetSettingAsync(SettingConstants.CompanyPhone) ?? "";
                CompanyAddress = await settingsService.GetSettingAsync(SettingConstants.CompanyAddress) ?? "";
                TaxId = await settingsService.GetSettingAsync(SettingConstants.TaxId) ?? "";
                BusinessNumber = await settingsService.GetSettingAsync(SettingConstants.BusinessNumber) ?? "";

                // Regional
                DateFormat = await settingsService.GetSettingAsync(SettingConstants.DateFormat) ?? "dd/MM/yyyy";
                NumberFormat = await settingsService.GetSettingAsync(SettingConstants.NumberFormat) ?? "1,234.56";
                TimeZone = await settingsService.GetSettingAsync(SettingConstants.TimeZone) ?? TimeZoneInfo.Local.DisplayName;

                // Interface Preferences
                DarkMode = await settingsService.GetSettingAsync<bool>(SettingConstants.DarkMode, false);
                EnableAnimations = await settingsService.GetSettingAsync<bool>(SettingConstants.EnableAnimations, true);
                ShowTooltips = await settingsService.GetSettingAsync<bool>(SettingConstants.ShowTooltips, true);
                CompactView = await settingsService.GetSettingAsync<bool>(SettingConstants.CompactView, false);
                ShowGridLines = await settingsService.GetSettingAsync<bool>(SettingConstants.ShowGridLines, true);
                AutoSave = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoSave, false);

                // Accounting Rules
                FiscalYearStart = await settingsService.GetSettingAsync(SettingConstants.FiscalYearStart) ?? "January";
                AutoCloseFiscalYear = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoCloseFiscalYear, false);
                LockClosedPeriods = await settingsService.GetSettingAsync<bool>(SettingConstants.LockClosedPeriods, false);
                WarnClosedPeriods = await settingsService.GetSettingAsync<bool>(SettingConstants.WarnClosedPeriods, true);
                InvoicePrefix = await settingsService.GetSettingAsync(SettingConstants.InvoicePrefix) ?? "INV-";
                ReceiptPrefix = await settingsService.GetSettingAsync(SettingConstants.ReceiptPrefix) ?? "RCP-";
                StartingNumber = await settingsService.GetSettingAsync(SettingConstants.StartingNumber) ?? "1000";
                AutoNumberTransactions = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoNumberTransactions, true);
                RequireJournalApproval = await settingsService.GetSettingAsync<bool>(SettingConstants.RequireJournalApproval, false);
                AllowNegativeInventory = await settingsService.GetSettingAsync<bool>(SettingConstants.AllowNegativeInventory, false);
                TrackCostCenter = await settingsService.GetSettingAsync<bool>(SettingConstants.TrackCostCenter, false);
                MultiCurrency = await settingsService.GetSettingAsync<bool>(SettingConstants.MultiCurrency, true);

                // Tax Configuration
                DefaultSalesTaxRate = await settingsService.GetSettingAsync(SettingConstants.DefaultSalesTaxRate) ?? "15.00";
                DefaultPurchaseTaxRate = await settingsService.GetSettingAsync(SettingConstants.DefaultPurchaseTaxRate) ?? "15.00";
                TaxInclusive = await settingsService.GetSettingAsync<bool>(SettingConstants.TaxInclusive, false);
                AutoCalculateTaxes = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoCalculateTaxes, true);
                TrackTaxByLine = await settingsService.GetSettingAsync<bool>(SettingConstants.TrackTaxByLine, false);
                AutoGenerateTaxReports = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoGenerateTaxReports, true);

                // Security & Access
                SessionTimeout = await settingsService.GetSettingAsync(SettingConstants.SessionTimeout) ?? "30";
                PasswordExpirationDays = await settingsService.GetSettingAsync(SettingConstants.PasswordExpirationDays) ?? "90";
                RequireStrongPasswords = await settingsService.GetSettingAsync<bool>(SettingConstants.RequireStrongPasswords, true);
                TwoFactorAuth = await settingsService.GetSettingAsync<bool>(SettingConstants.TwoFactorAuth, false);
                AutoLogout = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoLogout, false);
                LockAfterFailedAttempts = await settingsService.GetSettingAsync<bool>(SettingConstants.LockAfterFailedAttempts, true);

                // Backup & Data
                BackupFrequency = await settingsService.GetSettingAsync(SettingConstants.BackupFrequency) ?? "Daily";
                RetentionPeriod = await settingsService.GetSettingAsync(SettingConstants.RetentionPeriod) ?? "90 Days";
                BackupLocation = await settingsService.GetSettingAsync(SettingConstants.BackupLocation) ?? "";
                AutoBackupEnabled = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoBackupEnabled, true);
                CompressBackups = await settingsService.GetSettingAsync<bool>(SettingConstants.CompressBackups, true);
                EncryptBackups = await settingsService.GetSettingAsync<bool>(SettingConstants.EncryptBackups, false);
                VerifyBackups = await settingsService.GetSettingAsync<bool>(SettingConstants.VerifyBackups, true);
                EmailBackupNotifications = await settingsService.GetSettingAsync<bool>(SettingConstants.EmailBackupNotifications, true);
                CloudBackupSync = await settingsService.GetSettingAsync<bool>(SettingConstants.CloudBackupSync, false);

                // Reports & Print
                DefaultReportFormat = await settingsService.GetSettingAsync(SettingConstants.DefaultReportFormat) ?? "PDF";
                ReportRefreshInterval = await settingsService.GetSettingAsync(SettingConstants.ReportRefreshInterval) ?? "Manual";
                ShowLogoOnReports = await settingsService.GetSettingAsync<bool>(SettingConstants.ShowLogoOnReports, true);
                IncludePageNumbers = await settingsService.GetSettingAsync<bool>(SettingConstants.IncludePageNumbers, true);
                ShowGenerationTime = await settingsService.GetSettingAsync<bool>(SettingConstants.ShowGenerationTime, true);
                EnableReportDrillDown = await settingsService.GetSettingAsync<bool>(SettingConstants.EnableReportDrillDown, true);
                AutoEmailReports = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoEmailReports, false);
                AutoSaveReports = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoSaveReports, false);

                // Notifications
                ShowDesktopNotifications = await settingsService.GetSettingAsync<bool>(SettingConstants.ShowDesktopNotifications, true);
                PlayNotificationSounds = await settingsService.GetSettingAsync<bool>(SettingConstants.PlayNotificationSounds, true);
                ShowTrayNotifications = await settingsService.GetSettingAsync<bool>(SettingConstants.ShowTrayNotifications, false);
                NotifyOnUpdates = await settingsService.GetSettingAsync<bool>(SettingConstants.NotifyOnUpdates, true);

                // Integrations
                EnableBankFeeds = await settingsService.GetSettingAsync<bool>(SettingConstants.EnableBankFeeds, false);
                AutoMatchTransactions = await settingsService.GetSettingAsync<bool>(SettingConstants.AutoMatchTransactions, true);
                EnableOnlinePayments = await settingsService.GetSettingAsync<bool>(SettingConstants.EnableOnlinePayments, false);
                SyncPaymentConfirmations = await settingsService.GetSettingAsync<bool>(SettingConstants.SyncPaymentConfirmations, true);

                // External Connection
                UseExternalConnection = await settingsService.GetSettingAsync<bool>(SettingConstants.UseExternalConnection, false);
                ExternalHost = await settingsService.GetSettingAsync(SettingConstants.ExternalHost);
                if (string.IsNullOrEmpty(ExternalHost)) ExternalHost = "localhost";

                ExternalPort = await settingsService.GetSettingAsync(SettingConstants.ExternalPort);
                if (string.IsNullOrEmpty(ExternalPort)) ExternalPort = "5432";

                ExternalDatabase = await settingsService.GetSettingAsync(SettingConstants.ExternalDatabase);
                if (string.IsNullOrEmpty(ExternalDatabase)) ExternalDatabase = "SchoolManagementSystem";

                ExternalUsername = await settingsService.GetSettingAsync(SettingConstants.ExternalUsername);
                if (string.IsNullOrEmpty(ExternalUsername)) ExternalUsername = "postgres";

                ExternalPassword = await settingsService.GetSettingAsync(SettingConstants.ExternalPassword) ?? "";

                var baseCurrencyId = await settingsService.GetBaseCurrencyIdAsync();
                SelectedBaseCurrency = Currencies.FirstOrDefault(c => c.CurrencyId == baseCurrencyId);
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error loading settings: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveSettings()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();

                await settingsService.SetSettingAsync(SettingConstants.CompanyName, CompanyName ?? "");
                await settingsService.SetSettingAsync(SettingConstants.CompanyLegalName, CompanyLegalName ?? "");
                await settingsService.SetSettingAsync(SettingConstants.CompanyEmail, CompanyEmail ?? "");
                await settingsService.SetSettingAsync(SettingConstants.CompanyPhone, CompanyPhone ?? "");
                await settingsService.SetSettingAsync(SettingConstants.CompanyAddress, CompanyAddress ?? "");
                await settingsService.SetSettingAsync(SettingConstants.TaxId, TaxId ?? "");
                await settingsService.SetSettingAsync(SettingConstants.BusinessNumber, BusinessNumber ?? "");

                // Regional
                await settingsService.SetSettingAsync(SettingConstants.DateFormat, DateFormat ?? "dd/MM/yyyy");
                await settingsService.SetSettingAsync(SettingConstants.NumberFormat, NumberFormat ?? "1,234.56");
                await settingsService.SetSettingAsync(SettingConstants.TimeZone, TimeZone ?? "");

                // Interface
                await settingsService.SetSettingAsync(SettingConstants.DarkMode, DarkMode.ToString());
                await settingsService.SetSettingAsync(SettingConstants.EnableAnimations, EnableAnimations.ToString());
                await settingsService.SetSettingAsync(SettingConstants.ShowTooltips, ShowTooltips.ToString());
                await settingsService.SetSettingAsync(SettingConstants.CompactView, CompactView.ToString());
                await settingsService.SetSettingAsync(SettingConstants.ShowGridLines, ShowGridLines.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AutoSave, AutoSave.ToString());

                // Accounting
                await settingsService.SetSettingAsync(SettingConstants.FiscalYearStart, FiscalYearStart ?? "January");
                await settingsService.SetSettingAsync(SettingConstants.AutoCloseFiscalYear, AutoCloseFiscalYear.ToString());
                await settingsService.SetSettingAsync(SettingConstants.LockClosedPeriods, LockClosedPeriods.ToString());
                await settingsService.SetSettingAsync(SettingConstants.WarnClosedPeriods, WarnClosedPeriods.ToString());
                await settingsService.SetSettingAsync(SettingConstants.InvoicePrefix, InvoicePrefix ?? "");
                await settingsService.SetSettingAsync(SettingConstants.ReceiptPrefix, ReceiptPrefix ?? "");
                await settingsService.SetSettingAsync(SettingConstants.StartingNumber, StartingNumber ?? "");
                await settingsService.SetSettingAsync(SettingConstants.AutoNumberTransactions, AutoNumberTransactions.ToString());
                await settingsService.SetSettingAsync(SettingConstants.RequireJournalApproval, RequireJournalApproval.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AllowNegativeInventory, AllowNegativeInventory.ToString());
                await settingsService.SetSettingAsync(SettingConstants.TrackCostCenter, TrackCostCenter.ToString());
                await settingsService.SetSettingAsync(SettingConstants.MultiCurrency, MultiCurrency.ToString());

                // Tax
                await settingsService.SetSettingAsync(SettingConstants.DefaultSalesTaxRate, DefaultSalesTaxRate ?? "0");
                await settingsService.SetSettingAsync(SettingConstants.DefaultPurchaseTaxRate, DefaultPurchaseTaxRate ?? "0");
                await settingsService.SetSettingAsync(SettingConstants.TaxInclusive, TaxInclusive.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AutoCalculateTaxes, AutoCalculateTaxes.ToString());
                await settingsService.SetSettingAsync(SettingConstants.TrackTaxByLine, TrackTaxByLine.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AutoGenerateTaxReports, AutoGenerateTaxReports.ToString());

                // Security
                await settingsService.SetSettingAsync(SettingConstants.SessionTimeout, SessionTimeout ?? "30");
                await settingsService.SetSettingAsync(SettingConstants.PasswordExpirationDays, PasswordExpirationDays ?? "90");
                await settingsService.SetSettingAsync(SettingConstants.RequireStrongPasswords, RequireStrongPasswords.ToString());
                await settingsService.SetSettingAsync(SettingConstants.TwoFactorAuth, TwoFactorAuth.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AutoLogout, AutoLogout.ToString());
                await settingsService.SetSettingAsync(SettingConstants.LockAfterFailedAttempts, LockAfterFailedAttempts.ToString());

                // Backup
                await settingsService.SetSettingAsync(SettingConstants.BackupFrequency, BackupFrequency ?? "Daily");
                await settingsService.SetSettingAsync(SettingConstants.RetentionPeriod, RetentionPeriod ?? "90 Days");
                await settingsService.SetSettingAsync(SettingConstants.BackupLocation, BackupLocation ?? "");
                await settingsService.SetSettingAsync(SettingConstants.AutoBackupEnabled, AutoBackupEnabled.ToString());
                await settingsService.SetSettingAsync(SettingConstants.CompressBackups, CompressBackups.ToString());
                await settingsService.SetSettingAsync(SettingConstants.EncryptBackups, EncryptBackups.ToString());
                await settingsService.SetSettingAsync(SettingConstants.VerifyBackups, VerifyBackups.ToString());
                await settingsService.SetSettingAsync(SettingConstants.EmailBackupNotifications, EmailBackupNotifications.ToString());
                await settingsService.SetSettingAsync(SettingConstants.CloudBackupSync, CloudBackupSync.ToString());

                // Reports
                await settingsService.SetSettingAsync(SettingConstants.DefaultReportFormat, DefaultReportFormat ?? "PDF");
                await settingsService.SetSettingAsync(SettingConstants.ReportRefreshInterval, ReportRefreshInterval ?? "Manual");
                await settingsService.SetSettingAsync(SettingConstants.ShowLogoOnReports, ShowLogoOnReports.ToString());
                await settingsService.SetSettingAsync(SettingConstants.IncludePageNumbers, IncludePageNumbers.ToString());
                await settingsService.SetSettingAsync(SettingConstants.ShowGenerationTime, ShowGenerationTime.ToString());
                await settingsService.SetSettingAsync(SettingConstants.EnableReportDrillDown, EnableReportDrillDown.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AutoEmailReports, AutoEmailReports.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AutoSaveReports, AutoSaveReports.ToString());

                // Notifications
                await settingsService.SetSettingAsync(SettingConstants.ShowDesktopNotifications, ShowDesktopNotifications.ToString());
                await settingsService.SetSettingAsync(SettingConstants.PlayNotificationSounds, PlayNotificationSounds.ToString());
                await settingsService.SetSettingAsync(SettingConstants.ShowTrayNotifications, ShowTrayNotifications.ToString());
                await settingsService.SetSettingAsync(SettingConstants.NotifyOnUpdates, NotifyOnUpdates.ToString());

                // Integrations
                await settingsService.SetSettingAsync(SettingConstants.EnableBankFeeds, EnableBankFeeds.ToString());
                await settingsService.SetSettingAsync(SettingConstants.AutoMatchTransactions, AutoMatchTransactions.ToString());
                await settingsService.SetSettingAsync(SettingConstants.EnableOnlinePayments, EnableOnlinePayments.ToString());
                await settingsService.SetSettingAsync(SettingConstants.SyncPaymentConfirmations, SyncPaymentConfirmations.ToString());

                // External Connection
                await settingsService.SetSettingAsync(SettingConstants.UseExternalConnection, UseExternalConnection.ToString());
                await settingsService.SetSettingAsync(SettingConstants.ExternalHost, ExternalHost ?? "");
                await settingsService.SetSettingAsync(SettingConstants.ExternalPort, ExternalPort ?? "5432");
                await settingsService.SetSettingAsync(SettingConstants.ExternalDatabase, ExternalDatabase ?? "");
                await settingsService.SetSettingAsync(SettingConstants.ExternalUsername, ExternalUsername ?? "");
                await settingsService.SetSettingAsync(SettingConstants.ExternalPassword, ExternalPassword ?? "");

                if (SelectedBaseCurrency != null)
                {
                    await settingsService.SetBaseCurrencyIdAsync(SelectedBaseCurrency.CurrencyId);
                }

                _messageBoxService.ShowMessage("Settings saved successfully!", "Success", "CheckCircleOutline");
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error saving settings: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task TestConnection()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Testing connection...";

                string connStr;
                if (UseExternalConnection)
                {
                    connStr = $"Host={ExternalHost};Port={ExternalPort};Database={ExternalDatabase};Username={ExternalUsername};Password={ExternalPassword}";
                }
                else
                {
                    connStr = Configurations.AppConfig.GetConnectionString("SecondaryDatabaseV18");
                }

                using var conn = new Npgsql.NpgsqlConnection(connStr);
                await conn.OpenAsync();
                
                _messageBoxService.ShowMessage("Connection successful!", "Test Result", "CheckCircleOutline");
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Connection failed: {ex.Message}", "Test Result", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RestoreDefaults()
        {
            var result = System.Windows.MessageBox.Show("Are you sure you want to reset all settings to their default values? This action cannot be undone.", "Reset Defaults", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    using var scope = _serviceProvider.CreateScope();
                    var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();

                    // Clear all settings in the database (or just set defaults)
                    // For now, let's just re-initialize the properties with defaults and save them
                    SetDefaultValues();
                    await SaveSettings();

                    _messageBoxService.ShowMessage("Settings have been reset to defaults.", "Success", "CheckCircleOutline");
                }
                catch (Exception ex)
                {
                    _messageBoxService.ShowMessage($"Error resetting settings: {ex.Message}", "Error", "ErrorOutline");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void SetDefaultValues()
        {
            CompanyName = "";
            CompanyLegalName = "";
            CompanyEmail = "";
            CompanyPhone = "";
            CompanyAddress = "";
            TaxId = "";
            BusinessNumber = "";

            DateFormat = "dd/MM/yyyy";
            NumberFormat = "1,234.56";
            TimeZone = TimeZoneInfo.Local.DisplayName;

            DarkMode = false;
            EnableAnimations = true;
            ShowTooltips = true;
            CompactView = false;
            ShowGridLines = true;
            AutoSave = false;

            FiscalYearStart = "January";
            AutoCloseFiscalYear = false;
            LockClosedPeriods = false;
            WarnClosedPeriods = true;
            InvoicePrefix = "INV-";
            ReceiptPrefix = "RCP-";
            StartingNumber = "1000";
            AutoNumberTransactions = true;
            RequireJournalApproval = false;
            AllowNegativeInventory = false;
            TrackCostCenter = false;
            MultiCurrency = true;

            DefaultSalesTaxRate = "15.00";
            DefaultPurchaseTaxRate = "15.00";
            TaxInclusive = false;
            AutoCalculateTaxes = true;
            TrackTaxByLine = false;
            AutoGenerateTaxReports = true;

            SessionTimeout = "30";
            PasswordExpirationDays = "90";
            RequireStrongPasswords = true;
            TwoFactorAuth = false;
            AutoLogout = false;
            LockAfterFailedAttempts = true;

            BackupFrequency = "Daily";
            RetentionPeriod = "90 Days";
            BackupLocation = "";
            AutoBackupEnabled = false;
            CompressBackups = true;
            EncryptBackups = false;
            VerifyBackups = true;
            EmailBackupNotifications = false;
            CloudBackupSync = false;

            // External Connection Defaults
            UseExternalConnection = false;
            ExternalHost = "localhost";
            ExternalPort = "5433";
            ExternalDatabase = "SchoolManagementSystem";
            ExternalUsername = "postgres";
            ExternalPassword = "";
        
            DefaultReportFormat = "PDF";
            ReportRefreshInterval = "Manual";
            ShowLogoOnReports = true;
            IncludePageNumbers = true;
            ShowGenerationTime = true;
            EnableReportDrillDown = true;
            AutoEmailReports = false;
            AutoSaveReports = false;

            ShowDesktopNotifications = true;
            PlayNotificationSounds = true;
            ShowTrayNotifications = false;
            NotifyOnUpdates = true;

            EnableBankFeeds = false;
            AutoMatchTransactions = true;
            EnableOnlinePayments = false;
            SyncPaymentConfirmations = true;
        }

        [RelayCommand]
        private void ExportSettings()
        {
            // Simple export to a JSON file (standard behavior)
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "BusinessSettings_Export.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // For now, just a placeholder message as actual JSON serialization requires a DTO
                    _messageBoxService.ShowMessage("Settings export feature coming soon!", "Information", "InfoOutline");
                }
                catch (Exception ex)
                {
                    _messageBoxService.ShowMessage($"Error exporting settings: {ex.Message}", "Error", "ErrorOutline");
                }
            }
        }

        [RelayCommand]
        private async Task FixJournalTemplateDiscrepancies()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var journalServices = scope.ServiceProvider.GetRequiredService<JournalServices>();

                int fixedCount = await journalServices.AlignJournalTemplateHeadersAsync();

                _messageBoxService.ShowMessage($"Successfully aligned {fixedCount} journal templates.", "Success", "CheckCircleOutline");
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error fixing templates: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task FixJournalLineReferences()
        {
             try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var journalServices = scope.ServiceProvider.GetRequiredService<JournalServices>();

                int fixedCount = await journalServices.AlignJournalLineReferencesAsync();

                _messageBoxService.ShowMessage($"Successfully aligned lines for {fixedCount} journal entries.", "Success", "CheckCircleOutline");
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error fixing journal lines: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ImportStudentData()
        {
            if (IsLoading) return;

            await _dbLock.WaitAsync();
            IsLoading = true;
            LoadingMessage = "Initializing import...";

            var sessionId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var session = new ImportSession
            {
                SessionId = sessionId,
                ImportDate = DateTime.UtcNow,
                StartDate = DateTime.SpecifyKind(ImportStartDate, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(ImportEndDate, DateTimeKind.Utc),
                Status = "IN_PROGRESS",
                IncludeOpeningBalances = IncludeOpeningBalances
            };

            _context.ImportSessions.Add(session);
            await _context.SaveChangesAsync();

            int newStudentsCount = 0;
            int existingStudentsCount = 0;
            int transfersCount = 0;
            int writeOffsCount = 0;
            int transactionsImported = 0;
            decimal totalAmount = 0;

            try
            {
                // Set connection string for import
                if (UseExternalConnection)
                {
                    fetches.ConnectionString = $"Host={ExternalHost};Port={ExternalPort};Database={ExternalDatabase};Username={ExternalUsername};Password={ExternalPassword}";
                }
                else
                {
                    fetches.ConnectionString = null;
                }

                // Determine if we should calculate opening balances
                DateTime? openingBalanceDate = IncludeOpeningBalances ? ImportStartDate : null;

                // Fetch students with optional opening balance calculation
                LoadingMessage = "Fetching student roster from Academy...";
                var students = fetches.GetAllStudentsTable(openingBalanceDate);
                var count = students.Count;
                int current = 0;

                // Get transactions starting from the selected date range
                LoadingMessage = "Fetching transactions from Academy...";
                var detailedTransactions = fetches.GetStudentTransactions(ImportStartDate)
                    .Where(t => t.TransactionDate.Date <= ImportEndDate.Date)
                    .ToList();

                // Get student plans from Academy
                LoadingMessage = "Fetching payment plans from Academy...";
                var academyPlans = fetches.GetStudentPlans();

                // Get necessary accounts
                var arAccount = await _coaService.GetAccountByNumberAsync("1100"); // Accounts Receivable
                var cashAccount = await _coaService.GetAccountByNumberAsync("1000"); // Cash
                var bankAccount = await _coaService.GetAccountByNumberAsync("1020"); // Bank
                var equityAccount = await _coaService.GetAccountByNumberAsync("3100"); // Retained Earnings
                var salesAccount = await _coaService.GetAccountByNumberAsync("4000"); // Sales Revenue
                var badDebtsAccount = await _coaService.GetAccountByNumberAsync("5150"); // Bad Debts Expense

                if (arAccount == null || equityAccount == null || salesAccount == null || badDebtsAccount == null)
                {
                    var missing = new List<string>();
                    if (arAccount == null) missing.Add("1100 (AR)");
                    if (equityAccount == null) missing.Add("3100 (Equity)");
                    if (salesAccount == null) missing.Add("4000 (Sales)");
                    if (badDebtsAccount == null) missing.Add("5150 (Bad Debts)");

                    _messageBoxService.ShowMessage(
                        $"Required accounts ({string.Join(", ", missing)}) not found in the Chart of Accounts.\n\n" +
                        "Please ensure these accounts exist to complete the import and write-off process.",
                        "Missing Configuration",
                        "Warning");

                    IsLoading = false;
                    _dbLock.Release();
                    return;
                }

                // Get existing grades to avoid redundant DB checks
                var existingGrades = await _context.StudentGrades.OrderBy(g => g.SortOrder).ToListAsync();
                var gradeList = existingGrades.Select(g => g.GradeName).ToHashSet();

                LoadingMessage = "Pre-processing customer profiles...";
                // Load existing customers in bulk to avoid per-student queries
                var customers = await _context.Customers.ToListAsync();
                var customersMap = customers.Where(c => c.StudentId != null).ToDictionary(c => c.StudentId!);

                // Phase 1: Sync profile and status in bulk
                int profileCount = 0;
                foreach (var student in students)
                {
                    profileCount++;
                    if (profileCount % 100 == 0)
                        LoadingMessage = $"Syncing profiles: {profileCount} of {count}...";

                    // Sync Grade/Class if it doesn't exist
                    if (!string.IsNullOrWhiteSpace(student.StudentClass) && !gradeList.Contains(student.StudentClass))
                    {
                        var newGrade = new StudentGrade
                        {
                            GradeName = student.StudentClass,
                            IsActive = true,
                            SortOrder = gradeList.Count + 1
                        };
                        _context.StudentGrades.Add(newGrade);
                        await _context.SaveChangesAsync();
                        gradeList.Add(student.StudentClass);
                    }

                    var studentIdStr = student.Id.ToString();
                    Customer customerRecord;

                    if (customersMap.TryGetValue(studentIdStr, out customerRecord))
                    {
                        existingStudentsCount++;
                        // Sync profile data
                        customerRecord.NationalId = Truncate(student.IDNumber, 50);
                        customerRecord.Gender = Truncate(student.Gender, 10);
                        customerRecord.ContactPerson = Truncate(student.ContactDetails, 255);
                        customerRecord.BillingAddress = student.Address;
                        customerRecord.CustomerName = Truncate($"{student.Name} {student.Surname}", 255);
                        customerRecord.Phone = Truncate(student.ContactDetails, 50);
                        customerRecord.GradeLevel = Truncate(student.StudentClass, 50);
                        customerRecord.GuardianName = Truncate(student.GuardianName, 255);
                        customerRecord.UpdatedAt = DateTime.UtcNow;
                        customerRecord.IsActive = !student.isTransferred;
                        _context.Customers.Update(customerRecord);
                    }
                    else
                    {
                        newStudentsCount++;
                        // Create customer record
                        var datePart = DateTime.Now.ToString("yyMMdd");
                        var randomPart = new Random().Next(1000, 9999);

                        customerRecord = new Customer
                        {
                            NationalId = Truncate(student.IDNumber, 50),
                            CustomerCode = $"C-{datePart}-{randomPart}",
                            Gender = Truncate(student.Gender, 10),
                            Email = string.Empty,
                            TaxId = string.Empty,
                            ContactPerson = Truncate(student.ContactDetails, 255),
                            BillingAddress = student.Address,
                            CustomerName = Truncate($"{student.Name} {student.Surname}", 255),
                            Phone = Truncate(student.ContactDetails, 50),
                            ShippingAddress = student.Address,
                            DefaultRevenueAccountId = 4000,
                            StudentId = Truncate(studentIdStr, 50),
                            GradeLevel = Truncate(student.StudentClass, 50),
                            GuardianName = Truncate(student.GuardianName, 255),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            IsActive = !student.isTransferred
                        };

                        if (student.DOB != DateTime.MinValue)
                            customerRecord.DateOfBirth = student.DOB.ToUniversalTime();

                        _context.Customers.Add(customerRecord);
                        customersMap[studentIdStr] = customerRecord;
                    }
                }

                LoadingMessage = "Saving customer profiles to database...";
                await _context.SaveChangesAsync();

                // Phase 2: Load bulk data for fast in-memory lookups
                LoadingMessage = "Loading transaction tracking logs...";
                var existingReferences = await _context.JournalEntries
                    .Where(j => j.Reference.StartsWith("IMP-") || j.Reference.StartsWith("WO-") || j.Reference.StartsWith("OB-"))
                    .Select(j => j.Reference)
                    .ToListAsync();
                var existingRefHash = new HashSet<string>(existingReferences);

                var existingImportFingerprints = existingReferences
                    .Where(r => r.StartsWith("IMP-") && r.Length > 18)
                    .Select(r => r.Substring(18))
                    .ToHashSet();

                var existingWriteOffStudentIds = existingReferences
                    .Where(r => r.StartsWith("WO-IMP-SID"))
                    .Select(r => TryExtractStudentId(r, "SID"))
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToHashSet();

                var obJournals = await _context.JournalEntries
                    .Where(j => j.Reference.StartsWith("OB-"))
                    .Include(j => j.JournalLines)
                    .ToListAsync();
                var oldOBsByStudentId = obJournals
                    .Select(j => new { Journal = j, StudentId = TryExtractStudentId(j.Reference, "SID") })
                    .Where(x => x.StudentId.HasValue)
                    .GroupBy(x => x.StudentId!.Value)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Journal).ToList());

                var activePlans = await _context.PaymentPlans
                    .Where(p => p.Status == "ACTIVE")
                    .ToListAsync();
                var activePlansByCustomerId = activePlans
                    .GroupBy(p => p.CustomerId)
                    .ToDictionary(g => g.Key, g => g.First());

                var allFollowups = await _context.CollectionFollowups.ToListAsync();
                var latestFollowupByCustomerId = allFollowups
                    .GroupBy(f => f.CustomerId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.FollowupDate).First());

                var arBalances = await _context.JournalLines
                    .Where(l => l.JournalEntry.Status == "POSTED" &&
                                l.AccountId == arAccount.AccountId &&
                                l.ContactId.HasValue &&
                                l.ContactType == "Customer")
                    .GroupBy(l => l.ContactId!.Value)
                    .Select(g => new { CustomerId = g.Key, Balance = g.Sum(l => l.DebitAmount - l.CreditAmount) })
                    .ToDictionaryAsync(x => x.CustomerId, x => x.Balance);

                LoadingMessage = "Processing transactions in memory...";
                var pendingJournals = new List<JournalEntry>();
                current = 0;

                foreach (var student in students)
                {
                    current++;
                    if (current % 100 == 0)
                        LoadingMessage = $"Processing transactions: {current} of {count}...";

                    var studentIdStr = student.Id.ToString();
                    if (!customersMap.TryGetValue(studentIdStr, out var customerRecord))
                        continue;

                    var studentTransactions = detailedTransactions.Where(t => t.StudentId == student.Id).ToList();
                    var currentBalance = arBalances.GetValueOrDefault(customerRecord.CustomerId, 0m);

                    // Step 1: Process Opening Balance
                    if (student.OpeningBalance != 0)
                    {
                        var obRefId = $"OB-{sessionId}-SID{student.Id}";

                        if (oldOBsByStudentId.TryGetValue(student.Id, out var studentOldOBs))
                        {
                            foreach (var oldOb in studentOldOBs)
                            {
                                var arLine = oldOb.JournalLines.FirstOrDefault(l => l.AccountId == arAccount.AccountId);
                                if (arLine != null)
                                {
                                    currentBalance -= (arLine.DebitAmount - arLine.CreditAmount);
                                }
                                _context.JournalEntries.Remove(oldOb);
                            }
                        }

                        var obJournal = new JournalEntry
                        {
                            JournalDate = DateTime.SpecifyKind(ImportStartDate, DateTimeKind.Utc),
                            Description = $"Opening Balance - {student.FullName}",
                            Reference = obRefId,
                            JournalType = "GENERAL",
                            Status = "POSTED",
                            PostedAt = DateTime.UtcNow,
                            Amount = Math.Abs(student.OpeningBalance),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        obJournal.JournalLines.Add(new JournalLine
                        {
                            AccountId = arAccount.AccountId,
                            DebitAmount = student.OpeningBalance > 0 ? student.OpeningBalance : 0,
                            CreditAmount = student.OpeningBalance < 0 ? Math.Abs(student.OpeningBalance) : 0,
                            Description = $"Opening Balance for {student.FullName}",
                            ContactId = customerRecord.CustomerId,
                            ContactType = "Customer",
                            LineDate = obJournal.JournalDate,
                            CreatedAt = DateTime.UtcNow
                        });

                        obJournal.JournalLines.Add(new JournalLine
                        {
                            AccountId = equityAccount.AccountId,
                            DebitAmount = student.OpeningBalance < 0 ? Math.Abs(student.OpeningBalance) : 0,
                            CreditAmount = student.OpeningBalance > 0 ? student.OpeningBalance : 0,
                            Description = "Opening Balance Offset",
                            LineDate = obJournal.JournalDate,
                            CreatedAt = DateTime.UtcNow
                        });

                        pendingJournals.Add(obJournal);
                        currentBalance += student.OpeningBalance;
                        transactionsImported++;
                    }

                    // Step 2: Process Detailed Transactions
                    foreach (var trans in studentTransactions)
                    {
                        var transFingerprint = $"-SID{student.Id}-{trans.TransactionDate:yyyyMMdd}-{trans.DocNumber ?? trans.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}";
                        var refId = $"IMP-{sessionId}{transFingerprint}";

                        if (existingImportFingerprints.Contains(transFingerprint))
                            continue;

                        var transDateUtc = trans.TransactionDate.Kind == DateTimeKind.Utc
                            ? trans.TransactionDate
                            : DateTime.SpecifyKind(trans.TransactionDate, DateTimeKind.Utc);

                        if (trans.DebitCredit == "DR")
                        {
                            var invoiceJournal = new JournalEntry
                            {
                                JournalDate = transDateUtc,
                                Description = string.IsNullOrWhiteSpace(trans.Description)
                                    ? $"Invoice for {student.FullName}"
                                    : trans.Description,
                                Reference = refId,
                                JournalType = "SALES_INVOICE",
                                Status = "POSTED",
                                PostedAt = DateTime.UtcNow,
                                Amount = trans.Amount,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            invoiceJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = arAccount.AccountId,
                                DebitAmount = trans.Amount,
                                CreditAmount = 0,
                                Description = invoiceJournal.Description,
                                ContactId = customerRecord.CustomerId,
                                ContactType = "Customer",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            invoiceJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = salesAccount.AccountId,
                                DebitAmount = 0,
                                CreditAmount = trans.Amount,
                                Description = "Tuition/Services",
                                ContactId = customerRecord.CustomerId,
                                ContactType = "Customer",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            pendingJournals.Add(invoiceJournal);
                            existingImportFingerprints.Add(transFingerprint);
                            currentBalance += trans.Amount;
                            transactionsImported++;
                        }
                        else if (trans.DebitCredit == "CR")
                        {
                            var paymentJournal = new JournalEntry
                            {
                                JournalDate = transDateUtc,
                                Description = string.IsNullOrWhiteSpace(trans.Description)
                                    ? $"Payment from {student.FullName}"
                                    : trans.Description,
                                Reference = refId,
                                JournalType = "PAYMENT",
                                Status = "POSTED",
                                PostedAt = DateTime.UtcNow,
                                Amount = trans.Amount,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            paymentJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = cashAccount.AccountId,
                                DebitAmount = trans.Amount,
                                CreditAmount = 0,
                                Description = "Cash Receipt",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            paymentJournal.JournalLines.Add(new JournalLine
                            {
                                AccountId = arAccount.AccountId,
                                DebitAmount = 0,
                                CreditAmount = trans.Amount,
                                Description = paymentJournal.Description,
                                ContactId = customerRecord.CustomerId,
                                ContactType = "Customer",
                                LineDate = transDateUtc,
                                CreatedAt = DateTime.UtcNow
                            });

                            pendingJournals.Add(paymentJournal);
                            existingImportFingerprints.Add(transFingerprint);
                            currentBalance -= trans.Amount;
                            transactionsImported++;
                        }
                    }

                    // Step 2.5: Sync Payment Plans
                    var studentPlan = academyPlans.FirstOrDefault(p => p.StudentId == student.Id);
                    if (studentPlan != null)
                    {
                        activePlansByCustomerId.TryGetValue(customerRecord.CustomerId, out var existingPlan);

                        if (existingPlan == null)
                        {
                            var newPlan = new PaymentPlan
                            {
                                CustomerId = customerRecord.CustomerId,
                                PlanName = "Imported from Academy",
                                Status = "ACTIVE",
                                Notes = (studentPlan.Description ?? string.Empty) + $" [SID:{sessionId}]",
                                StartDate = DateTime.UtcNow,
                                EndDate = studentPlan.FollowUpDate.HasValue 
                                    ? DateTime.SpecifyKind(studentPlan.FollowUpDate.Value, DateTimeKind.Utc) 
                                    : DateTime.UtcNow.AddMonths(3),
                                TotalAmount = Math.Abs(student.OpeningBalance),
                                MonthlyInstallment = Math.Abs(student.OpeningBalance),
                                NumberOfInstallments = 1
                            };
                            _context.PaymentPlans.Add(newPlan);
                            activePlansByCustomerId[customerRecord.CustomerId] = newPlan;
                        }
                        else
                        {
                            existingPlan.Notes = (studentPlan.Description ?? existingPlan.Notes) + $" [SID:{sessionId}]";
                            existingPlan.UpdatedAt = DateTime.UtcNow;
                            _context.PaymentPlans.Update(existingPlan);
                        }

                        if (studentPlan.FollowUpDate.HasValue)
                        {
                            var utcFollowupDate = DateTime.SpecifyKind(studentPlan.FollowUpDate.Value, DateTimeKind.Utc);
                            latestFollowupByCustomerId.TryGetValue(customerRecord.CustomerId, out var lastFollowup);

                            if (lastFollowup == null || lastFollowup.NextFollowupDate != utcFollowupDate)
                            {
                                var followup = new CollectionFollowup
                                {
                                    CustomerId = customerRecord.CustomerId,
                                    FollowupDate = DateTime.UtcNow,
                                    Method = "Import",
                                    Outcome = "Plan Status Sync",
                                    Notes = (studentPlan.Description ?? "Imported follow-up from Academy") + $" [SID:{sessionId}]",
                                    NextFollowupDate = utcFollowupDate,
                                    CreatedBy = "System Import"
                                };
                                _context.CollectionFollowups.Add(followup);
                                latestFollowupByCustomerId[customerRecord.CustomerId] = followup;
                            }
                        }
                    }

                    // Step 3: Bad debt write offs
                    if (student.isTransferred)
                    {
                        transfersCount++;
                        if (badDebtsAccount != null)
                        {
                            if (!existingWriteOffStudentIds.Contains(student.Id))
                            {
                                if (currentBalance > 0)
                                {
                                    var writeOffDate = new DateTime(ImportStartDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                    
                                    var writeOffJournal = new JournalEntry
                                    {
                                        JournalDate = writeOffDate,
                                        Description = $"Automated Write-off: Transferred Student - {student.FullName}",
                                        Reference = $"WO-IMP-SID{student.Id}-" + sessionId,
                                        JournalType = "GENERAL",
                                        Status = "POSTED",
                                        PostedAt = DateTime.UtcNow,
                                        PostedBy = 1,
                                        Amount = currentBalance,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow
                                    };

                                    writeOffJournal.JournalLines.Add(new JournalLine
                                    {
                                        AccountId = badDebtsAccount.AccountId,
                                        DebitAmount = currentBalance,
                                        CreditAmount = 0,
                                        Description = writeOffJournal.Description,
                                        ContactId = customerRecord.CustomerId,
                                        ContactType = "Customer",
                                        LineDate = writeOffDate,
                                        CreatedAt = DateTime.UtcNow
                                    });

                                    writeOffJournal.JournalLines.Add(new JournalLine
                                    {
                                        AccountId = arAccount.AccountId,
                                        DebitAmount = 0,
                                        CreditAmount = currentBalance,
                                        Description = writeOffJournal.Description,
                                        ContactId = customerRecord.CustomerId,
                                        ContactType = "Customer",
                                        LineDate = writeOffDate,
                                        CreatedAt = DateTime.UtcNow
                                    });

                                    pendingJournals.Add(writeOffJournal);
                                    existingWriteOffStudentIds.Add(student.Id);
                                    transactionsImported++;
                                    writeOffsCount++;
                                }
                            }
                        }
                    }

                    totalAmount += Math.Abs(student.OpeningBalance) + studentTransactions.Sum(t => t.Amount);
                }

                // Bulk insert all journal entries
                if (pendingJournals.Any())
                {
                    LoadingMessage = $"Saving {pendingJournals.Count} journal entries in bulk...";
                    await _journalService.CreateJournalEntriesAsync(pendingJournals);
                }

                // Final save changes for payment plans and followups
                LoadingMessage = "Saving payment plans and followups...";
                await _context.SaveChangesAsync();

                // Import Cash and Bank Opening Balances ONLY if enabled
                if (IncludeOpeningBalances)
                {
                    // Import Cash Opening Balance
                    LoadingMessage = "Importing Cash Opening Balance...";
                    var cashBalance = fetches.GetCashOpeningBalance(ImportStartDate);
                    if (cashBalance != 0 && cashAccount != null && equityAccount != null)
                    {
                        var cashFingerprint = "OB-CASH";
                        var cashRefId = $"OB-{sessionId}-CASH";
                        if (!await _context.JournalEntries.AnyAsync(j => j.Reference.Contains(cashFingerprint)))
                        {
                            var cashEntry = new JournalEntry
                            {
                                JournalDate = ImportStartDate.ToUniversalTime(),
                                Description = "Cash Opening Balance Import",
                                Reference = cashRefId,
                                JournalType = "GENERAL",
                                Status = "POSTED",
                                PostedAt = DateTime.UtcNow,
                                Amount = Math.Abs(cashBalance),
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            cashEntry.JournalLines.Add(new JournalLine
                            {
                                AccountId = cashAccount.AccountId,
                                DebitAmount = cashBalance > 0 ? cashBalance : 0,
                                CreditAmount = cashBalance < 0 ? Math.Abs(cashBalance) : 0,
                                Description = "Cash Opening Balance",
                                LineDate = cashEntry.JournalDate,
                                CreatedAt = DateTime.UtcNow
                            });

                            cashEntry.JournalLines.Add(new JournalLine
                            {
                                AccountId = equityAccount.AccountId,
                                DebitAmount = cashBalance < 0 ? Math.Abs(cashBalance) : 0,
                                CreditAmount = cashBalance > 0 ? cashBalance : 0,
                                Description = "Cash Opening Balance Offset",
                                LineDate = cashEntry.JournalDate,
                                CreatedAt = DateTime.UtcNow
                            });

                            await _journalService.CreateJournalEntryAsync(cashEntry);
                        }
                    }

                    // Import Bank Opening Balance
                    LoadingMessage = "Importing Bank Opening Balance...";
                    var bankBalance = fetches.GetBankOpeningBalance(ImportStartDate);
                    if (bankBalance != 0 && bankAccount != null && equityAccount != null)
                    {
                        var bankFingerprint = "OB-BANK";
                        var bankRefId = $"OB-{sessionId}-BANK";
                        if (!await _context.JournalEntries.AnyAsync(j => j.Reference.Contains(bankFingerprint)))
                        {
                            var bankEntry = new JournalEntry
                            {
                                JournalDate = ImportStartDate.ToUniversalTime(),
                                Description = "Bank Opening Balance Import",
                                Reference = bankRefId,
                                JournalType = "GENERAL",
                                Status = "POSTED",
                                PostedAt = DateTime.UtcNow,
                                Amount = Math.Abs(bankBalance),
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            bankEntry.JournalLines.Add(new JournalLine
                            {
                                AccountId = bankAccount.AccountId,
                                DebitAmount = bankBalance > 0 ? bankBalance : 0,
                                CreditAmount = bankBalance < 0 ? Math.Abs(bankBalance) : 0,
                                Description = "Bank Opening Balance",
                                LineDate = bankEntry.JournalDate,
                                CreatedAt = DateTime.UtcNow
                            });

                            bankEntry.JournalLines.Add(new JournalLine
                            {
                                AccountId = equityAccount.AccountId,
                                DebitAmount = bankBalance < 0 ? Math.Abs(bankBalance) : 0,
                                CreditAmount = bankBalance > 0 ? bankBalance : 0,
                                Description = "Bank Opening Balance Offset",
                                LineDate = bankEntry.JournalDate,
                                CreatedAt = DateTime.UtcNow
                            });

                            await _journalService.CreateJournalEntryAsync(bankEntry);
                        }
                    }
                }

                // Update session record
                session.NewStudentsCount = newStudentsCount;
                session.ExistingStudentsCount = existingStudentsCount;
                session.TransactionsCount = transactionsImported;
                session.TotalAmount = totalAmount;
                session.Status = "COMPLETED";
                _context.ImportSessions.Update(session);
                await _context.SaveChangesAsync();

                // Update settings with the transaction EndDate as last import date
                await _settingsService.SetSettingAsync(SettingConstants.LastStudentImportDate, ImportEndDate.ToString("O"));
                await _settingsService.SetSettingAsync(SettingConstants.LastImportSessionId, sessionId);
                
                LastImportDateText = ImportEndDate.ToString("yyyy-MM-dd");
                LastImportSessionSummary = $"Import Session {sessionId} Summary:\n" +
                                          $"• Sync Date: {LastImportDateText}\n" +
                                          $"• New Students Added: {newStudentsCount}\n" +
                                          $"• Existing Students Updated: {existingStudentsCount}\n" +
                                          $"• Transferred Students: {transfersCount}\n" +
                                          $"• Bad Debts Written Off: {writeOffsCount}\n" +
                                          $"• Total Transactions Imported: {transactionsImported}\n" +
                                          $"• Range: {ImportStartDate:yyyy-MM-dd} to {ImportEndDate:yyyy-MM-dd}\n\n" +
                                          $"Transferred students found in this batch have been automatically written off to Bad Debt Expense (5150).";

                LoadingMessage = "Finalizing...";

                _messageBoxService.ShowMessage(
                    $"Import completed successfully!\n\n" +
                    $"New students: {newStudentsCount}\n" +
                    $"Existing students: {existingStudentsCount}\n" +
                    $"Transfers found: {transfersCount}\n" +
                    $"Write-offs processed: {writeOffsCount}\n" +
                    $"Transactions processed: {transactionsImported}\n\n" +
                    $"Last Sync: {LastImportDateText}",
                    "Import Complete",
                    "CheckCircleOutline");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Import failed: {ex}");
                _messageBoxService.ShowMessage($"Import failed: {ex.Message}", "Import Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
                _dbLock.Release();
            }
        }

        [RelayCommand]
        public async Task UndoImport()
        {
            var lastSessionId = await _settingsService.GetSettingAsync<string>(SettingConstants.LastImportSessionId);
            if (string.IsNullOrEmpty(lastSessionId))
            {
                _messageBoxService.ShowMessage("No recent import session found to undo.", "Undo Import", "InfoOutline");
                return;
            }

            var confirmed = _messageBoxService.ShowConfirmation(
                $"Are you sure you want to undo the last import session ({lastSessionId})?\n\n" +
                "This will delete ONLY the transactions imported in that specific session.\n\n" +
                "This action cannot be undone.",
                "Undo Last Import",
                "Warning");

            if (!confirmed) return;

            await _dbLock.WaitAsync();
            IsLoading = true;
            LoadingMessage = "Reversing last import session...";

            try
            {
                // Find journal entries belonging to this session ID
                var journalEntries = await _context.JournalEntries
                    .Include(j => j.JournalLines)
                    .Where(j => j.Reference.Contains(lastSessionId))
                    .ToListAsync();

                // Get affected customer IDs to restore status
                var affectedCustomerIds = journalEntries
                    .SelectMany(j => j.JournalLines)
                    .Where(l => l.ContactId.HasValue && l.ContactType == "Customer")
                    .Select(l => l.ContactId.Value)
                    .Distinct()
                    .ToList();

                int deletedCount = journalEntries.Count;
                if (journalEntries.Any())
                {
                    LoadingMessage = "Reversing account balances...";
                    var accountDeltas = new Dictionary<int, decimal>();
                    foreach (var entry in journalEntries)
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

                    _context.JournalEntries.RemoveRange(journalEntries);
                }

                // Delete Payment Plans created/updated in this session
                var plansToDelete = await _context.PaymentPlans
                    .Where(p => p.Notes.Contains($"[SID:{lastSessionId}]"))
                    .ToListAsync();
                if (plansToDelete.Any()) _context.PaymentPlans.RemoveRange(plansToDelete);

                // Delete Follow-ups created in this session
                var followupsToDelete = await _context.CollectionFollowups
                    .Where(f => f.Notes.Contains($"[SID:{lastSessionId}]"))
                    .ToListAsync();
                if (followupsToDelete.Any()) _context.CollectionFollowups.RemoveRange(followupsToDelete);

                // Restore Customer active status
                if (affectedCustomerIds.Any())
                {
                    var affectedCustomers = await _context.Customers
                        .Where(c => affectedCustomerIds.Contains(c.CustomerId))
                        .ToListAsync();
                    
                    foreach (var customer in affectedCustomers)
                    {
                        customer.IsActive = true; 
                        customer.UpdatedAt = DateTime.UtcNow;
                    }
                    _context.Customers.UpdateRange(affectedCustomers);
                }

                // Mark session as reversed in database
                var session = await _context.ImportSessions.FirstOrDefaultAsync(s => s.SessionId == lastSessionId);
                if (session != null)
                {
                    session.Status = "REVERSED";
                    _context.ImportSessions.Update(session);
                }

                await _settingsService.SetSettingAsync(SettingConstants.LastImportSessionId, "");
                await _context.SaveChangesAsync();

                _messageBoxService.ShowMessage(
                    $"Import reversed successfully!\n\n" +
                    $"Deleted {deletedCount} journal entries and updated session status.",
                    "Reversal Complete",
                    "CheckCircleOutline");
                
                LastImportSessionSummary = string.Empty;
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error reversing import: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
                _dbLock.Release();
            }
        }

        // ── Cashbook Expense Import ───────────────────────────────────────────

        [RelayCommand]
        private async Task OpenImportExpenses()
        {
            try
            {
                // Pass IServiceProvider to window so it manages its own DbContext scope
                var window = new WndImportExpenses(_serviceProvider);
                window.ShowDialog();

                // Refresh last import date label after window closes
                await RefreshExpenseImportDateAsync();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Could not open Import Expenses window: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        private async Task RefreshExpenseImportDateAsync()
        {
            var dateStr = await _settingsService.GetSettingAsync(SettingConstants.LastExpenseImportDate);
            if (DateTime.TryParse(dateStr, out var dt))
                LastExpenseImportDateText = dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        [RelayCommand]
        public async Task SyncStudentStatesAsync()
        {
            if (IsLoading) return;
            await _dbLock.WaitAsync();

            try
            {
                IsLoading = true;
                LoadingMessage = "Fetching student states from Academy...";

                var students = await Task.Run(() => fetches.GetAllStudentsTable(null));
                if (students == null || !students.Any())
                {
                    _messageBoxService.ShowMessage("No students found to sync.", "Sync Finished", "InfoCircle");
                    return;
                }

                int count = students.Count;
                int current = 0;
                int updatedCount = 0;

                var existingGrades = await _context.StudentGrades.OrderBy(g => g.SortOrder).ToListAsync();
                var gradeList = existingGrades.Select(g => g.GradeName).ToHashSet();

                var existingCustomerStudentIds = (await _context.Customers
                    .Where(c => c.StudentId != null)
                    .Select(c => c.StudentId)
                    .ToListAsync())
                    .ToHashSet();

                foreach (var student in students)
                {
                    current++;
                    LoadingMessage = $"Syncing state {current} of {count}: {student.FullName}";

                    await SyncStudentProfileAndStatusInternalAsync(student, gradeList, existingCustomerStudentIds);
                    updatedCount++;
                }

                _messageBoxService.ShowMessage($"Successfully synced states for {updatedCount} students.\n\nNote: This did not import any financial transactions.", "Sync Complete", "CheckCircle");
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Sync failed: {ex.Message}", "Sync Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
                _dbLock.Release();
            }
        }

        private async Task<Customer> SyncStudentProfileAndStatusInternalAsync(
            StudentSelection student,
            HashSet<string> gradeList,
            HashSet<string> existingCustomerStudentIds)
        {
            // Sync Grade/Class if it doesn't exist
            if (!string.IsNullOrWhiteSpace(student.StudentClass) && !gradeList.Contains(student.StudentClass))
            {
                var newGrade = new StudentGrade
                {
                    GradeName = student.StudentClass,
                    IsActive = true,
                    SortOrder = gradeList.Count + 1
                };
                _context.StudentGrades.Add(newGrade);
                await _context.SaveChangesAsync();
                gradeList.Add(student.StudentClass);
            }

            Customer customerRecord;
            var studentIdStr = student.Id.ToString();

            // Check if customer already exists using the pre-fetched hash set for better performance
            if (existingCustomerStudentIds.Contains(studentIdStr))
            {
                customerRecord = await _context.Customers.FirstOrDefaultAsync(c => c.StudentId == studentIdStr);

                // Sync profile data
                customerRecord.NationalId = Truncate(student.IDNumber, 50);
                customerRecord.Gender = Truncate(student.Gender, 10);
                customerRecord.ContactPerson = Truncate(student.ContactDetails, 255);
                customerRecord.BillingAddress = student.Address;
                customerRecord.CustomerName = Truncate($"{student.Name} {student.Surname}", 255);
                customerRecord.Phone = Truncate(student.ContactDetails, 50);
                customerRecord.GradeLevel = Truncate(student.StudentClass, 50);
                customerRecord.GuardianName = Truncate(student.GuardianName, 255);
                customerRecord.UpdatedAt = DateTime.UtcNow;

                _context.Customers.Update(customerRecord);
            }
            else
            {
                // NEW STUDENT - Create customer record
                var datePart = DateTime.Now.ToString("yyMMdd");
                var randomPart = new Random().Next(1000, 9999);

                customerRecord = new Customer();
                customerRecord.NationalId = Truncate(student.IDNumber, 50);
                customerRecord.CustomerCode = $"C-{datePart}-{randomPart}";
                customerRecord.Gender = Truncate(student.Gender, 10);
                customerRecord.Email = string.Empty;
                customerRecord.TaxId = string.Empty;
                customerRecord.ContactPerson = Truncate(student.ContactDetails, 255);
                customerRecord.BillingAddress = student.Address;
                customerRecord.CustomerName = Truncate($"{student.Name} {student.Surname}", 255);
                customerRecord.Phone = Truncate(student.ContactDetails, 50);
                customerRecord.ShippingAddress = student.Address;
                customerRecord.DefaultRevenueAccountId = 4000;
                if (student.DOB != DateTime.MinValue)
                    customerRecord.DateOfBirth = student.DOB.ToUniversalTime();

                customerRecord.StudentId = Truncate(student.Id.ToString(), 50);
                customerRecord.GradeLevel = Truncate(student.StudentClass, 50);
                customerRecord.GuardianName = Truncate(student.GuardianName, 255);
                customerRecord.CreatedAt = DateTime.UtcNow;
                customerRecord.UpdatedAt = DateTime.UtcNow;

                _context.Customers.Add(customerRecord);
            }

            // Sync transfer status
            customerRecord.IsActive = !student.isTransferred;
            await _context.SaveChangesAsync();
            return customerRecord;
        }

        [RelayCommand]
        private async Task UndoAllImportsAsync()
        {
            var confirmed = _messageBoxService.ShowConfirmation(
                "Are you sure you want to undo ALL imported student data?\n\n" +
                "This will delete ALL journal entries with 'IMP-', 'OB-', or 'WO-IMP-' references.\n\n" +
                "This action is extreme and cannot be undone.",
                "Undo ALL Imports",
                "Warning");

            if (!confirmed) return;

            await _dbLock.WaitAsync();
            IsLoading = true;
            LoadingMessage = "Reversing ALL imports...";

            try
            {
                var journalEntries = await _context.JournalEntries
                    .Include(j => j.JournalLines)
                    .Where(j => j.Reference.StartsWith("IMP-") || 
                                j.Reference.StartsWith("OB-") || 
                                (j.Reference.StartsWith("WO-") && j.Description.Contains("Automated")))
                    .ToListAsync();

                int deletedCount = journalEntries.Count;
                if (journalEntries.Any())
                {
                    LoadingMessage = "Reversing account balances...";
                    var accountDeltas = new Dictionary<int, decimal>();
                    foreach (var entry in journalEntries)
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

                    _context.JournalEntries.RemoveRange(journalEntries);
                }

                // Delete ALL Imported Payment Plans and Follow-ups
                var plansToDelete = await _context.PaymentPlans
                    .Where(p => p.Notes.Contains("[SID:") || p.PlanName == "Imported from Academy")
                    .ToListAsync();
                if (plansToDelete.Any()) _context.PaymentPlans.RemoveRange(plansToDelete);

                var followupsToDelete = await _context.CollectionFollowups
                    .Where(f => f.Notes.Contains("[SID:") || f.CreatedBy == "System Import")
                    .ToListAsync();
                if (followupsToDelete.Any()) _context.CollectionFollowups.RemoveRange(followupsToDelete);

                // Reactivate ALL customers who were potentially inactivated by import
                var students = await _context.Customers.Where(c => c.StudentId != null).ToListAsync();
                foreach (var s in students) s.IsActive = true;
                _context.Customers.UpdateRange(students);

                // Update all session statuses
                var sessions = await _context.ImportSessions.ToListAsync();
                foreach (var s in sessions) s.Status = "REVERSED";

                await _settingsService.SetSettingAsync(SettingConstants.LastImportSessionId, "");
                await _settingsService.SetSettingAsync(SettingConstants.LastStudentImportDate, "");
                await _context.SaveChangesAsync();

                _messageBoxService.ShowMessage(
                    $"Global reversal completed!\n\n" +
                    $"Deleted {deletedCount} journal entries across all sessions.",
                    "Global Reversal Complete",
                    "CheckCircleOutline");

                LastImportDateText = "Never";
                LastImportSessionSummary = string.Empty;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during global reversal: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _dbLock.Release();
            }
        }

        [RelayCommand]
        private async Task ViewLastImportReportAsync()
        {
            try
            {
                var lastSessionId = await _settingsService.GetSettingAsync<string>(SettingConstants.LastImportSessionId);
                if (string.IsNullOrEmpty(lastSessionId))
                {
                    _messageBoxService.ShowMessage("No import history found.", "Information", "InfoOutline");
                    return;
                }

                var session = await _context.ImportSessions.FirstOrDefaultAsync(s => s.SessionId == lastSessionId);
                if (session == null)
                {
                    System.Windows.MessageBox.Show($"Session {lastSessionId} not found in database.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var reportService = _serviceProvider.GetRequiredService<ReportPrintingService>();
                var doc = reportService.GenerateImportSummaryDocument(session, CompanyName ?? "PrimeApp Books");

                // Create a container for the viewer and a toolbar
                var grid = new System.Windows.Controls.Grid();
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

                var toolbar = new System.Windows.Controls.StackPanel 
                { 
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                // Style for buttons
                var btnStyle = new System.Windows.Style(typeof(System.Windows.Controls.Button));
                btnStyle.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, new Thickness(12, 5, 12, 5)));
                btnStyle.Setters.Add(new Setter(System.Windows.Controls.Control.MarginProperty, new Thickness(5, 0, 0, 0)));

                var printBtn = new System.Windows.Controls.Button { Content = "Print", Style = btnStyle, Background = System.Windows.Media.Brushes.DodgerBlue, Foreground = System.Windows.Media.Brushes.White };
                var pdfBtn = new System.Windows.Controls.Button { Content = "Export PDF", Style = btnStyle, Background = System.Windows.Media.Brushes.DarkRed, Foreground = System.Windows.Media.Brushes.White };
                var csvBtn = new System.Windows.Controls.Button { Content = "Export CSV", Style = btnStyle, Background = System.Windows.Media.Brushes.SeaGreen, Foreground = System.Windows.Media.Brushes.White };

                // Use FlowDocumentReader for built-in Search (Ctrl+F), Zoom, and Viewing Modes
                var viewer = new System.Windows.Controls.FlowDocumentReader 
                { 
                    Document = doc,
                    ViewingMode = System.Windows.Controls.FlowDocumentReaderViewingMode.Scroll
                };

                printBtn.Click += (s, e) =>
                {
                    var printDialog = new System.Windows.Controls.PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        printDialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "Import Report");
                    }
                };

                pdfBtn.Click += (s, e) =>
                {
                    try
                    {
                        var pdfPath = reportService.GenerateImportSummaryPdf(session, CompanyName ?? "PrimeApp Books");
                        reportService.OpenPdfFile(pdfPath);
                    }
                    catch (Exception ex)
                    {
                        _messageBoxService.ShowMessage($"PDF Export failed: {ex.Message}", "Error", "ErrorOutline");
                    }
                };

                csvBtn.Click += (s, e) =>
                {
                    try
                    {
                        var csvPath = reportService.ExportImportSummaryToCsv(session);
                        Process.Start(new ProcessStartInfo { FileName = csvPath, UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        _messageBoxService.ShowMessage($"CSV Export failed: {ex.Message}", "Error", "ErrorOutline");
                    }
                };

                toolbar.Children.Add(printBtn);
                toolbar.Children.Add(pdfBtn);
                toolbar.Children.Add(csvBtn);

                System.Windows.Controls.Grid.SetRow(toolbar, 0);
                System.Windows.Controls.Grid.SetRow(viewer, 1);
                grid.Children.Add(toolbar);
                grid.Children.Add(viewer);

                // Show in a rich preview window
                var window = new System.Windows.Window
                {
                    Title = $"Import Session {lastSessionId} - Rich Report View",
                    Width = 850,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = grid
                };
                window.Show();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error viewing report: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        private int? TryExtractStudentId(string reference, string marker)
        {
            if (string.IsNullOrEmpty(reference)) return null;
            int index = reference.IndexOf(marker);
            if (index == -1) return null;
            
            int start = index + marker.Length;
            int end = start;
            while (end < reference.Length && char.IsDigit(reference[end]))
            {
                end++;
            }
            
            if (end > start && int.TryParse(reference.Substring(start, end - start), out var id))
            {
                return id;
            }
            return null;
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        [RelayCommand]
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }
    }
}