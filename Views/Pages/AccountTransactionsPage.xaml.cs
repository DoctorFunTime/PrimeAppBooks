using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for AccountTransactionsPage.xaml
    /// </summary>
    public partial class AccountTransactionsPage : BaseAnimatedPage
    {
        private readonly AccountTransactionsPageViewModel _viewModel;

        public AccountTransactionsPage(AccountTransactionsPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
            
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // If no account is selected (e.g. navigation from sidebar), initialize without an account
            if (_viewModel.SelectedAccount == null)
            {
                await _viewModel.Initialize();
            }
        }
    }
}
