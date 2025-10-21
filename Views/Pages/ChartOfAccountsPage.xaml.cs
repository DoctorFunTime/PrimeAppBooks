using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

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

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ChartOfAccountsPageViewModel viewModel && e.NewValue is ChartOfAccount selectedAccount)
            {
                viewModel.SelectedAccount = selectedAccount;
            }
        }
    }
}