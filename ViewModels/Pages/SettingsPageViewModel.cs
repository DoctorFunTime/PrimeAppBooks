using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

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

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<Currency> Currencies { get; } = new();
        public ObservableCollection<string> DateFormats { get; } = new() { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" };
        public ObservableCollection<string> NumberFormats { get; } = new() { "1,234.56", "1.234,56", "1 234,56" };
        public ObservableCollection<string> TimeZones { get; } = new(TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.DisplayName));
        public ObservableCollection<string> FiscalYearStarts { get; } = new() { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        public ObservableCollection<string> BackupFrequencies { get; } = new() { "Daily", "Weekly", "Monthly", "On Exit" };
        public ObservableCollection<string> RetentionPeriods { get; } = new() { "30 Days", "90 Days", "1 Year", "Forever" };
        public ObservableCollection<string> ReportFormats { get; } = new() { "PDF", "Excel", "CSV", "HTML" };
        public ObservableCollection<string> RefreshIntervals { get; } = new() { "Manual", "5 Minutes", "15 Minutes", "30 Minutes", "Hourly" };

        public SettingsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadCurrenciesAsync();
            await LoadSettingsAsync();
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

                CompanyName = await settingsService.GetSettingAsync(SettingConstants.CompanyName);
                CompanyLegalName = await settingsService.GetSettingAsync(SettingConstants.CompanyLegalName);
                CompanyEmail = await settingsService.GetSettingAsync(SettingConstants.CompanyEmail);
                CompanyPhone = await settingsService.GetSettingAsync(SettingConstants.CompanyPhone);
                CompanyAddress = await settingsService.GetSettingAsync(SettingConstants.CompanyAddress);
                TaxId = await settingsService.GetSettingAsync(SettingConstants.TaxId);
                BusinessNumber = await settingsService.GetSettingAsync(SettingConstants.BusinessNumber);

                // Regional
                DateFormat = await settingsService.GetSettingAsync(SettingConstants.DateFormat) ?? "dd/MM/yyyy";
                NumberFormat = await settingsService.GetSettingAsync(SettingConstants.NumberFormat) ?? "1,234.56";
                TimeZone = await settingsService.GetSettingAsync(SettingConstants.TimeZone) ?? TimeZoneInfo.Local.DisplayName;

                // Interface
                DarkMode = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.DarkMode) ?? "False");
                EnableAnimations = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.EnableAnimations) ?? "True");
                ShowTooltips = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.ShowTooltips) ?? "True");
                CompactView = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.CompactView) ?? "False");
                ShowGridLines = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.ShowGridLines) ?? "True");
                AutoSave = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoSave) ?? "False");

                // Accounting
                FiscalYearStart = await settingsService.GetSettingAsync(SettingConstants.FiscalYearStart) ?? "January";
                AutoCloseFiscalYear = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoCloseFiscalYear) ?? "False");
                LockClosedPeriods = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.LockClosedPeriods) ?? "False");
                WarnClosedPeriods = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.WarnClosedPeriods) ?? "True");
                InvoicePrefix = await settingsService.GetSettingAsync(SettingConstants.InvoicePrefix) ?? "INV-";
                ReceiptPrefix = await settingsService.GetSettingAsync(SettingConstants.ReceiptPrefix) ?? "RCP-";
                StartingNumber = await settingsService.GetSettingAsync(SettingConstants.StartingNumber) ?? "1000";
                AutoNumberTransactions = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoNumberTransactions) ?? "True");
                RequireJournalApproval = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.RequireJournalApproval) ?? "False");
                AllowNegativeInventory = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AllowNegativeInventory) ?? "False");
                TrackCostCenter = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.TrackCostCenter) ?? "False");
                MultiCurrency = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.MultiCurrency) ?? "True");

                // Tax
                DefaultSalesTaxRate = await settingsService.GetSettingAsync(SettingConstants.DefaultSalesTaxRate) ?? "15.00";
                DefaultPurchaseTaxRate = await settingsService.GetSettingAsync(SettingConstants.DefaultPurchaseTaxRate) ?? "15.00";
                TaxInclusive = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.TaxInclusive) ?? "False");
                AutoCalculateTaxes = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoCalculateTaxes) ?? "True");
                TrackTaxByLine = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.TrackTaxByLine) ?? "False");
                AutoGenerateTaxReports = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoGenerateTaxReports) ?? "True");

                // Security
                SessionTimeout = await settingsService.GetSettingAsync(SettingConstants.SessionTimeout) ?? "30";
                PasswordExpirationDays = await settingsService.GetSettingAsync(SettingConstants.PasswordExpirationDays) ?? "90";
                RequireStrongPasswords = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.RequireStrongPasswords) ?? "True");
                TwoFactorAuth = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.TwoFactorAuth) ?? "False");
                AutoLogout = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoLogout) ?? "False");
                LockAfterFailedAttempts = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.LockAfterFailedAttempts) ?? "True");

                // Backup
                BackupFrequency = await settingsService.GetSettingAsync(SettingConstants.BackupFrequency) ?? "Daily";
                RetentionPeriod = await settingsService.GetSettingAsync(SettingConstants.RetentionPeriod) ?? "90 Days";
                BackupLocation = await settingsService.GetSettingAsync(SettingConstants.BackupLocation) ?? "";
                AutoBackupEnabled = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoBackupEnabled) ?? "True");
                CompressBackups = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.CompressBackups) ?? "True");
                EncryptBackups = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.EncryptBackups) ?? "False");
                VerifyBackups = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.VerifyBackups) ?? "True");
                EmailBackupNotifications = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.EmailBackupNotifications) ?? "True");
                CloudBackupSync = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.CloudBackupSync) ?? "False");

                // Reports
                DefaultReportFormat = await settingsService.GetSettingAsync(SettingConstants.DefaultReportFormat) ?? "PDF";
                ReportRefreshInterval = await settingsService.GetSettingAsync(SettingConstants.ReportRefreshInterval) ?? "Manual";
                ShowLogoOnReports = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.ShowLogoOnReports) ?? "True");
                IncludePageNumbers = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.IncludePageNumbers) ?? "True");
                ShowGenerationTime = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.ShowGenerationTime) ?? "True");
                EnableReportDrillDown = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.EnableReportDrillDown) ?? "True");
                AutoEmailReports = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoEmailReports) ?? "False");
                AutoSaveReports = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoSaveReports) ?? "False");

                // Notifications
                ShowDesktopNotifications = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.ShowDesktopNotifications) ?? "True");
                PlayNotificationSounds = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.PlayNotificationSounds) ?? "True");
                ShowTrayNotifications = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.ShowTrayNotifications) ?? "False");
                NotifyOnUpdates = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.NotifyOnUpdates) ?? "True");

                // Integrations
                EnableBankFeeds = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.EnableBankFeeds) ?? "False");
                AutoMatchTransactions = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.AutoMatchTransactions) ?? "True");
                EnableOnlinePayments = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.EnableOnlinePayments) ?? "False");
                SyncPaymentConfirmations = bool.Parse(await settingsService.GetSettingAsync(SettingConstants.SyncPaymentConfirmations) ?? "True");

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

                if (SelectedBaseCurrency != null)
                {
                    await settingsService.SetSettingAsync(SettingConstants.BaseCurrencyId, SelectedBaseCurrency.CurrencyId.ToString());
                    
                    // Also update the IsBaseCurrency flag in the Currencies table
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var allCurrencies = await context.Currencies.ToListAsync();
                    foreach (var c in allCurrencies)
                    {
                        c.IsBaseCurrency = (c.CurrencyId == SelectedBaseCurrency.CurrencyId);
                    }
                    await context.SaveChangesAsync();
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
            AutoBackupEnabled = true;
            CompressBackups = true;
            EncryptBackups = false;
            VerifyBackups = true;
            EmailBackupNotifications = true;
            CloudBackupSync = false;

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
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }
    }
}