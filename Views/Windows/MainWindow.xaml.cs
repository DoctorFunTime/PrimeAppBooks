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

            // Create navigation service with the frame
            var navigationService = new NavigationService(MainContentFrame);

            // Create view model with navigation service dependency
            var viewModel = new MainWindowViewModel(navigationService);
            DataContext = viewModel;

            // Navigate to default page
            navigationService.NavigateTo<DashboardPage>();
        }
    }
}