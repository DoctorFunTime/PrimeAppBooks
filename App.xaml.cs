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
using PrimeAppBooks.Views.Pages;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
using PrimeAppBooks.Views.Windows;
using System.Windows;
using System.Windows.Controls;

namespace PrimeAppBooks
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnMainWindowClose;

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
            services.AddSingleton<IJournalNavigationService, JournalNavigationService>();
            services.AddSingleton<SplashscreenInitialisations>();
            services.AddTransient<DatabaseSetup>();
            
            // Register Report Services
            services.AddScoped<ReportGenerationService>();
            services.AddScoped<ReportPrintingService>();

            // Register Sales and Purchase Services
            services.AddScoped<SalesServices>();
            services.AddScoped<PurchaseServices>();

            // Register ALL ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TransactionsPageViewModel>();
            services.AddTransient<DashboardPageViewModel>();
            services.AddTransient<ChartOfAccountsPageViewModel>();
            services.AddTransient<AddAccountPageViewModel>();
            services.AddTransient<AccountTransactionsPageViewModel>();
            services.AddTransient<ReportsPageViewModel>();
            services.AddTransient<AuditPageViewModel>();
            services.AddTransient<SettingsPageViewModel>();
            services.AddTransient<WndSplashScreenViewModel>();

            // New Sales and Purchase ViewModels
            services.AddTransient<SalesInvoicesPageViewModel>();
            services.AddTransient<PurchaseInvoicesPageViewModel>();
            services.AddTransient<AddSalesInvoicePageViewModel>();
            services.AddTransient<AddPurchaseInvoicePageViewModel>();
            services.AddTransient<AddCustomerPageViewModel>();
            services.AddTransient<CustomersPageViewModel>();

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
            services.AddTransient<Settings>();
            services.AddTransient<WndSplashScreen>();

            // New Sales and Purchase Pages
            services.AddTransient<SalesInvoicesPage>();
            services.AddTransient<PurchaseInvoicesPage>();
            services.AddTransient<AddSalesInvoicePage>();
            services.AddTransient<AddPurchaseInvoicePage>();
            services.AddTransient<AddCustomerPage>();
            services.AddTransient<CustomersPage>();

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
                // Optionally show error to user or log it
                MessageBox.Show($"Database initialization error: {ex.Message}", "Error", MessageBoxButton.OK);
            }
            // === END MIGRATION CODE ===

            // Get MainWindow from DI container
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            Application.Current.MainWindow = mainWindow;

            // Set DataContext
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();

            // Show splash screen
            var splash = ServiceProvider.GetRequiredService<WndSplashScreen>();
            splash.Show();

            Exit += OnApplicationExit;
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            // Cleanup code here
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
            navigationService.RegisterPageAnimation<Settings>(AnimationDirection.FromBottom);

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
        }
    }
}