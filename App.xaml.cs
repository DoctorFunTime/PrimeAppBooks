using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Configurations;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.APIs;
using PrimeAppBooks.ViewModels.Pages;
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
            services.AddTransient<TransactionsServices>();
            services.AddSingleton<SplashscreenInitialisations>();
            services.AddTransient<DatabaseSetup>();

            // Register ALL ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TransactionsPageViewModel>();
            services.AddTransient<DashboardPageViewModel>();
            services.AddTransient<ChartOfAccountsPageViewModel>();
            services.AddTransient<ReportsPageViewModel>();
            services.AddTransient<AuditPageViewModel>();
            services.AddTransient<SettingsPageViewModel>();
            services.AddTransient<WndSplashScreenViewModel>();

            // Register ALL Pages (IMPORTANT!)
            services.AddTransient<TransactionsPage>();
            services.AddTransient<DashboardPage>();
            services.AddTransient<ChartOfAccountsPage>();
            services.AddTransient<ReportsPage>();
            services.AddTransient<Audit>();
            services.AddTransient<Settings>();
            services.AddTransient<WndSplashScreen>();

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
            // Dashboard - smooth slide from bottom
            navigationService.RegisterPageAnimation<DashboardPage>(AnimationDirection.FadeIn);

            // Chart of Accounts - slide from right (feels like opening a drawer)
            navigationService.RegisterPageAnimation<ChartOfAccountsPage>(AnimationDirection.FromBottom);

            // Transactions - slide from right (consistent with accounts)
            navigationService.RegisterPageAnimation<TransactionsPage>(AnimationDirection.FromBottom);

            // Reports - slide from top (feels like opening a report)
            navigationService.RegisterPageAnimation<ReportsPage>(AnimationDirection.FromBottom);

            // Audit - fade in (more subtle for data-heavy pages)
            navigationService.RegisterPageAnimation<Audit>(AnimationDirection.FromBottom);

            // Settings - slide from left (feels like opening a side panel)
            navigationService.RegisterPageAnimation<Settings>(AnimationDirection.FromBottom);

            //SubPages
            // Settings - slide from left (feels like opening a side panel)
            navigationService.RegisterPageAnimation<JournalPage>(AnimationDirection.FromLeft);
        }
    }
}