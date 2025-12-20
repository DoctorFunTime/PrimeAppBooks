namespace PrimeAppBooks.Models
{
    public static class SettingConstants
    {
        public const string BaseCurrencyId = "BaseCurrencyId";
        public const string CompanyName = "CompanyName";
        public const string CompanyLegalName = "CompanyLegalName";
        public const string CompanyEmail = "CompanyEmail";
        public const string CompanyPhone = "CompanyPhone";
        public const string CompanyAddress = "CompanyAddress";
        public const string TaxId = "TaxId";
        public const string BusinessNumber = "BusinessNumber";

        // Regional & Format Settings
        public const string DateFormat = "DateFormat";
        public const string NumberFormat = "NumberFormat";
        public const string TimeZone = "TimeZone";

        // Interface Preferences
        public const string DarkMode = "DarkMode";
        public const string EnableAnimations = "EnableAnimations";
        public const string ShowTooltips = "ShowTooltips";
        public const string CompactView = "CompactView";
        public const string ShowGridLines = "ShowGridLines";
        public const string AutoSave = "AutoSave";

        // Accounting Rules
        public const string FiscalYearStart = "FiscalYearStart";
        public const string AutoCloseFiscalYear = "AutoCloseFiscalYear";
        public const string LockClosedPeriods = "LockClosedPeriods";
        public const string WarnClosedPeriods = "WarnClosedPeriods";
        public const string InvoicePrefix = "InvoicePrefix";
        public const string ReceiptPrefix = "ReceiptPrefix";
        public const string StartingNumber = "StartingNumber";
        public const string AutoNumberTransactions = "AutoNumberTransactions";
        public const string RequireJournalApproval = "RequireJournalApproval";
        public const string AllowNegativeInventory = "AllowNegativeInventory";
        public const string TrackCostCenter = "TrackCostCenter";
        public const string MultiCurrency = "MultiCurrency";

        // Tax Configuration
        public const string DefaultSalesTaxRate = "DefaultSalesTaxRate";
        public const string DefaultPurchaseTaxRate = "DefaultPurchaseTaxRate";
        public const string TaxInclusive = "TaxInclusive";
        public const string AutoCalculateTaxes = "AutoCalculateTaxes";
        public const string TrackTaxByLine = "TrackTaxByLine";
        public const string AutoGenerateTaxReports = "AutoGenerateTaxReports";

        // Security & Access
        public const string SessionTimeout = "SessionTimeout";
        public const string PasswordExpirationDays = "PasswordExpirationDays";
        public const string RequireStrongPasswords = "RequireStrongPasswords";
        public const string TwoFactorAuth = "TwoFactorAuth";
        public const string AutoLogout = "AutoLogout";
        public const string LockAfterFailedAttempts = "LockAfterFailedAttempts";

        // Backup & Data
        public const string BackupFrequency = "BackupFrequency";
        public const string RetentionPeriod = "RetentionPeriod";
        public const string BackupLocation = "BackupLocation";
        public const string AutoBackupEnabled = "AutoBackupEnabled";
        public const string CompressBackups = "CompressBackups";
        public const string EncryptBackups = "EncryptBackups";
        public const string VerifyBackups = "VerifyBackups";
        public const string EmailBackupNotifications = "EmailBackupNotifications";
        public const string CloudBackupSync = "CloudBackupSync";

        // Reports & Print
        public const string DefaultReportFormat = "DefaultReportFormat";
        public const string ReportRefreshInterval = "ReportRefreshInterval";
        public const string ShowLogoOnReports = "ShowLogoOnReports";
        public const string IncludePageNumbers = "IncludePageNumbers";
        public const string ShowGenerationTime = "ShowGenerationTime";
        public const string EnableReportDrillDown = "EnableReportDrillDown";
        public const string AutoEmailReports = "AutoEmailReports";
        public const string AutoSaveReports = "AutoSaveReports";

        // Notifications
        public const string ShowDesktopNotifications = "ShowDesktopNotifications";
        public const string PlayNotificationSounds = "PlayNotificationSounds";
        public const string ShowTrayNotifications = "ShowTrayNotifications";
        public const string NotifyOnUpdates = "NotifyOnUpdates";

        // Integrations
        public const string EnableBankFeeds = "EnableBankFeeds";
        public const string AutoMatchTransactions = "AutoMatchTransactions";
        public const string EnableOnlinePayments = "EnableOnlinePayments";
        public const string SyncPaymentConfirmations = "SyncPaymentConfirmations";
    }
}
