using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for ChartOfAccountsPage.xaml
    /// </summary>
    public partial class ChartOfAccountsPage : BaseAnimatedPage
    {
        public ChartOfAccountsPage(ChartOfAccountsPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}