using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.ViewModels.Windows;
using System;
using System.Windows;

namespace PrimeAppBooks.Views.Windows
{
    public partial class WndImportExpenses : Window
    {
        private readonly WndImportExpensesViewModel _viewModel;
        private IServiceScope _serviceScope;

        public WndImportExpenses(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // Create a scoped context for this window's lifetime
            _serviceScope = serviceProvider.CreateScope();
            var scopedProvider = _serviceScope.ServiceProvider;

            var context = scopedProvider.GetRequiredService<AppDbContext>();
            var journalService = scopedProvider.GetRequiredService<JournalServices>();
            var coaService = scopedProvider.GetRequiredService<ChartOfAccountsServices>();
            var settingsService = scopedProvider.GetRequiredService<SettingsService>();

            _viewModel = new WndImportExpensesViewModel(
                context, journalService, coaService, settingsService);

            _viewModel.CloseAction = () => this.Close();
            _viewModel.MinimizeAction = () => this.WindowState = WindowState.Minimized;

            DataContext = _viewModel;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Dispose the service scope and its DbContext when window closes
            _serviceScope?.Dispose();
        }
    }
}
