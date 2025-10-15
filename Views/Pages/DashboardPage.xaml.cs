using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.APIs;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : BaseAnimatedPage
    {
        public DashboardPage(DashboardPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}