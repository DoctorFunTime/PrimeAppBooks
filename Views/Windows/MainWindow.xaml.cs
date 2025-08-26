using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Windows;
using PrimeAppBooks.Views.Pages;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PrimeAppBooks.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Create navigation service once
            var navigationService = new NavigationService(MainContentFrame);

            // Register for dependency injection
            ServiceLocator.RegisterSingleton<INavigationService>(navigationService);

            // Create view model with injected navigation service
            var viewModel = new MainWindowViewModel(navigationService);
            DataContext = viewModel;

            // Navigate to default page
            navigationService.NavigateTo<DashboardPage>();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}