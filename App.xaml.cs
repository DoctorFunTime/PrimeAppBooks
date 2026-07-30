using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Configurations;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.APIs;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.ViewModels.Pages;
using PrimeAppBooks.ViewModels.Pages.SubTransactionsPage;
using PrimeAppBooks.ViewModels.Windows;
using PrimeAppBooks.Views;
using PrimeAppBooks.Views.Pages;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
using PrimeAppBooks.Views.Windows;
using QuestPDF;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrimeAppBooks
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Set QuestPDF license as early as possible
            QuestPDF.Settings.License = LicenseType.Community;

            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show("UI Thread:\n\n" + ex.Exception.ToString());
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show("Background Thread:\n\n" + ex.ExceptionObject.ToString());
            };

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                MessageBox.Show("Task:\n\n" + ex.Exception.ToString());
                ex.SetObserved();
            };

            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            ApplyLaunchArgs(e.Args);


            // Check for diagnostic argument
            if (e.Args.Length > 0 && e.Args[0] == "--diag-search-accounts")
            {
                DiagnosticSearchAccountsAsync(e.Args).Wait(); // Call the diagnostic method
                Application.Current.Shutdown(); // Exit after diagnostic run
                return;
            }

            var services = new ServiceCollection();

            // Register DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(AppConfig.ConnectionString));

            // Register core services
            services.AddSingleton<QuickBooksAuthService>();
            services.AddSingleton<QuickBooksService>();
            services.AddScoped<SettingsService>();
            services.AddScoped<TransactionsServices>();
            services.AddScoped<JournalServices>();  // Changed from Singleton to Scoped
            services.AddScoped<ChartOfAccountsServices>();  // Add Chart of Accounts service
            services.AddSingleton<BankServices>(); // Register Bank Services
            services.AddSingleton<IJournalNavigationService, JournalNavigationService>();
            services.AddSingleton<SplashscreenInitialisations>();
            services.AddTransient<DatabaseSetup>();

            // Register Report Services
            services.AddScoped<ReportGenerationService>();
            services.AddScoped<ReportPrintingService>();

            // Register Sales and Purchase Services
            services.AddScoped<SalesServices>();
            services.AddScoped<PurchaseServices>();
            services.AddScoped<VendorAnalyticsService>();
            services.AddScoped<VendorServices>();
            services.AddScoped<CustomerAnalyticsService>();
            services.AddScoped<InventoryService>(); // Register Inventory Service
            services.AddScoped<AssetService>(); // Register Asset Service

            // Register ALL ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<BadDebtsPageViewModel>();
            services.AddTransient<TransactionsPageViewModel>();
            services.AddTransient<DashboardPageViewModel>();
            services.AddTransient<ChartOfAccountsPageViewModel>();
            services.AddTransient<AddAccountPageViewModel>();
            services.AddTransient<AccountTransactionsPageViewModel>();
            services.AddTransient<GeneralLedgerPageViewModel>();
            services.AddTransient<BankReconciliationViewModel>();
            services.AddTransient<ReportsPageViewModel>();
            services.AddTransient<AuditPageViewModel>();
            services.AddTransient<SettingsPageViewModel>();
            services.AddTransient<UserManagementPageViewModel>();
            services.AddTransient<WndSplashScreenViewModel>();

            // New Sales and Purchase ViewModels
            services.AddTransient<SalesInvoicesPageViewModel>();
            services.AddTransient<PurchaseInvoicesPageViewModel>();
            services.AddTransient<AddSalesInvoicePageViewModel>();
            services.AddTransient<AddPurchaseInvoicePageViewModel>();
            services.AddTransient<AddCustomerPageViewModel>();
            services.AddTransient<CustomersPageViewModel>();
            services.AddTransient<CustomerAnalyticsViewModel>();
            services.AddTransient<CustomerStatementPageViewModel>();
            services.AddTransient<CollectionManagementViewModel>();
            services.AddTransient<VendorsPageViewModel>();
            services.AddTransient<AddVendorPageViewModel>();
            services.AddTransient<PayablesPageViewModel>();
            services.AddTransient<InventoryListPageViewModel>(); // Register Inventory VM
            services.AddTransient<AddEditInventoryPageViewModel>(); // Register Add/Edit Inventory VM

            // Asset Register ViewModels
            services.AddTransient<AssetRegisterPageViewModel>();
            services.AddTransient<AddEditAssetPageViewModel>();
            services.AddTransient<DepreciationRunViewModel>();

            //Subpages
            services.AddTransient<JournalPageViewModel>();

            // Register ALL Pages
            services.AddTransient<TransactionsPage>();
            services.AddTransient<DashboardPage>();
            services.AddTransient<ChartOfAccountsPage>();
            services.AddTransient<AddAccountPage>();
            services.AddTransient<ReportsPage>();
            services.AddTransient<AccountTransactionsPage>();
            services.AddTransient<Audit>();
            services.AddTransient<Views.Pages.Settings>();
            services.AddTransient<UserManagementPage>();
            services.AddTransient<WndSplashScreen>();
            services.AddTransient<Wndlogin>();

            // New Sales and Purchase Pages
            services.AddTransient<SalesInvoicesPage>();
            services.AddTransient<PurchaseInvoicesPage>();
            services.AddTransient<AddSalesInvoicePage>();
            services.AddTransient<AddPurchaseInvoicePage>();
            services.AddTransient<AddCustomerPage>();
            services.AddTransient<CustomersPage>();
            services.AddTransient<CustomerAnalyticsPage>();
            services.AddTransient<CustomerStatementPage>();
            services.AddTransient<CollectionManagementPage>();
            services.AddTransient<VendorsPage>();
            services.AddTransient<AddVendorPage>();
            services.AddTransient<PayablesPage>();
            services.AddTransient<InventoryListPage>(); // Register Inventory Page
            services.AddTransient<AddEditInventoryPage>(); // Register Add/Edit Inventory Page

            // Asset Register Pages
            services.AddTransient<AssetRegisterPage>();
            services.AddTransient<AddEditAssetPage>();
            services.AddTransient<DepreciationRunPage>();

            // Sales & Receivables Sub-Pages
            services.AddTransient<ReceivablesPage>();
            services.AddTransient<BadDebtsPage>();
            services.AddTransient<CreditNotesPage>();

            // Purchases & Payables Sub-Pages
            services.AddTransient<PayablesPage>();
            services.AddTransient<DebitNotesPage>();

            // Transactions Sub-Pages
            services.AddTransient<GeneralLedgerPage>(provider => new GeneralLedgerPage(provider.GetRequiredService<GeneralLedgerPageViewModel>()));
            services.AddTransient<BankReconciliationPage>();

            //Sub pages
            services.AddTransient<JournalPage>();

            // Register MainWindow
            services.AddSingleton<MainWindow>();

            // Register NavigationService with animation configuration
            services.AddSingleton<INavigationService>(provider =>
            {
                var mainWindow = provider.GetRequiredService<MainWindow>();
                var navigationService = new NavigationService(mainWindow.MainContentFrame, provider);

                // Register page animations during service initialization
                RegisterPageAnimations(navigationService);

                return navigationService;
            });

            // Build the service provider
            ServiceProvider = services.BuildServiceProvider();

            // === ADD DATABASE MIGRATION HERE ===
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    dbContext.Database.Migrate();
                    System.Diagnostics.Debug.WriteLine("Database migration completed successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database migration failed: {ex.Message}");
                MessageBox.Show($"Database initialization error: {ex.Message}", "Error", MessageBoxButton.OK);
            }
            // === END MIGRATION CODE ===

            // Show splash screen
            var splash = ServiceProvider.GetRequiredService<WndSplashScreen>();
            Application.Current.MainWindow = splash;
            splash.Show();

            Exit += OnApplicationExit;
        }

        private void ApplyLaunchArgs(string[] args)
        {
            string env = GetArgValue(args, "--env=") ?? "secondary";
            bool isV18 = GetArgValue(args, "--v18=") == "true";
            string? token = GetArgValue(args, "--token=");

            string connectionName = env switch
            {
                "secondary" => isV18 ? "DefaultConnectionV18" : "DefaultConnection",
                "primary" => "PrimaryConnection",
                _ => isV18 ? "DefaultConnectionV18" : "DefaultConnection"
            };
            Configurations.AppConfig.SwitchConnectionString(connectionName);

            if (SessionTokenService.TryValidateToken(token, out string username))
            {
                var loginRepo = new Repositories.LoginRepository();
                var loginDetails = loginRepo.GetLoginDetails(username);

                if (loginDetails != null)
                {
                    MyAppContext.CurrentLogin = loginDetails;
                }
            }
        }

        private string? GetArgValue(string[] args, string prefix)
        {
            return args.FirstOrDefault(a => a.StartsWith(prefix))?.Substring(prefix.Length);
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            // Cleanup code here
        }

        private async Task DiagnosticSearchAccountsAsync(string[] args)
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine("Starting diagnostic search...");

            try
            {
                var services = new ServiceCollection();
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(AppConfig.ConnectionString));
                services.AddScoped<ChartOfAccountsServices>();
                var provider = services.BuildServiceProvider();

                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                decimal targetBalance = 40.00m;
                if (args.Length > 1 && decimal.TryParse(args[1], out var parsed)) targetBalance = parsed;

                output.AppendLine($"--- Diagnostic Search for Balance: {targetBalance} ---");

                var accounts = await context.ChartOfAccounts
                    .Include(a => a.JournalLines)
                    .ThenInclude(l => l.JournalEntry)
                    .ToListAsync();

                decimal totalAssets = 0;
                decimal totalLiab = 0;
                decimal totalEquity = 0;
                decimal totalRevenue = 0;
                decimal totalExpenses = 0;

                foreach (var account in accounts)
                {
                    var postedLines = account.JournalLines.Where(l => l.JournalEntry != null && l.JournalEntry.Status == "POSTED").ToList();
                    var debits = postedLines.Sum(l => l.DebitAmount);
                    var credits = postedLines.Sum(l => l.CreditAmount);
                    var opening = account.OpeningBalance;

                    decimal balance;
                    if (account.NormalBalance == "CREDIT") balance = opening + (credits - debits);
                    else balance = opening + (debits - credits);

                    if (Math.Abs(balance - targetBalance) < 0.01m || Math.Abs(balance + targetBalance) < 0.01m || Math.Abs(opening - targetBalance) < 0.01m)
                    {
                        output.AppendLine($"MATCH: {account.AccountNumber} - {account.AccountName} ({account.AccountType})");
                        output.AppendLine($"  Normal: {account.NormalBalance}, Opening: {opening}, Debits: {debits}, Credits: {credits}, Balance: {balance}");
                    }

                    if (account.AccountType == "ASSET") totalAssets += (account.NormalBalance == "CREDIT" ? -balance : balance);
                    else if (account.AccountType == "LIABILITY") totalLiab += (account.NormalBalance == "DEBIT" ? -balance : balance);
                    else if (account.AccountType == "EQUITY") totalEquity += (account.NormalBalance == "DEBIT" ? -balance : balance);
                    else if (account.AccountType == "REVENUE") totalRevenue += (account.NormalBalance == "DEBIT" ? -balance : balance);
                    else if (account.AccountType == "EXPENSE") totalExpenses += (account.NormalBalance == "CREDIT" ? -balance : balance);
                }

                var netIncome = totalRevenue - totalExpenses;
                var calculatedEquity = totalEquity + netIncome;

                output.AppendLine("\n--- GL Totals ---");
                output.AppendLine($"Total Assets:    {totalAssets:F2}");
                output.AppendLine($"Total Liab:      {totalLiab:F2}");
                output.AppendLine($"Total Equity:    {totalEquity:F2}");
                output.AppendLine($"Net Income:      {netIncome:F2}");
                output.AppendLine($"L + E + NI:      {totalLiab + calculatedEquity:F2}");
                output.AppendLine($"Difference:      {totalAssets - (totalLiab + calculatedEquity):F2}");
            }
            catch (Exception ex)
            {
                output.AppendLine($"FATAL ERROR: {ex.Message}");
                output.AppendLine(ex.StackTrace);
            }

            System.IO.File.WriteAllText("diag_output.txt", output.ToString());
        }

        /// <summary>
        /// Register page animations for improved navigation experience
        /// </summary>
        private static void RegisterPageAnimations(INavigationService navigationService)
        {
            // Dashboard - smooth fade in with slight slide
            navigationService.RegisterPageAnimation<DashboardPage>(AnimationDirection.FadeIn);

            // Chart of Accounts - smooth slide from bottom
            navigationService.RegisterPageAnimation<ChartOfAccountsPage>(AnimationDirection.FromBottom);

            // Transactions - smooth slide from bottom
            navigationService.RegisterPageAnimation<TransactionsPage>(AnimationDirection.FromBottom);

            // Reports - smooth slide from bottom
            navigationService.RegisterPageAnimation<ReportsPage>(AnimationDirection.FromBottom);

            // Audit - smooth slide from bottom
            navigationService.RegisterPageAnimation<Audit>(AnimationDirection.FromBottom);

            // Settings - smooth slide from bottom
            navigationService.RegisterPageAnimation<Views.Pages.Settings>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<UserManagementPage>(AnimationDirection.FromBottom);

            // SubPages - smooth slide from left
            navigationService.RegisterPageAnimation<JournalPage>(AnimationDirection.FromRight);

            //Subpages
            navigationService.RegisterPageAnimation<AccountTransactionsPage>(AnimationDirection.FromBottom);

            // New Pages Animations
            navigationService.RegisterPageAnimation<SalesInvoicesPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<PurchaseInvoicesPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<AddSalesInvoicePage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<AddPurchaseInvoicePage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<AddCustomerPage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<CustomersPage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<CustomerAnalyticsPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<CustomerStatementPage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<CollectionManagementPage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<VendorsPage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<AddVendorPage>(AnimationDirection.FromRight);

            // Sales & Receivables Sub-Pages
            navigationService.RegisterPageAnimation<ReceivablesPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<BadDebtsPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<CreditNotesPage>(AnimationDirection.FromBottom);

            // Purchases & Payables Sub-Pages
            navigationService.RegisterPageAnimation<PayablesPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<DebitNotesPage>(AnimationDirection.FromBottom);

            // Inventory Pages
            navigationService.RegisterPageAnimation<InventoryListPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<AddEditInventoryPage>(AnimationDirection.FromRight);

            // Asset Register Pages
            navigationService.RegisterPageAnimation<AssetRegisterPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<AddEditAssetPage>(AnimationDirection.FromRight);
            navigationService.RegisterPageAnimation<DepreciationRunPage>(AnimationDirection.FromRight);

            // Transactions Sub-Pages
            navigationService.RegisterPageAnimation<GeneralLedgerPage>(AnimationDirection.FromBottom);
            navigationService.RegisterPageAnimation<BankReconciliationPage>(AnimationDirection.FromBottom);
        }
    }
}