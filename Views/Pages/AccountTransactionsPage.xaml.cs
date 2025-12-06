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
            
            System.Diagnostics.Debug.WriteLine("AccountTransactionsPage constructor called");
        }
    }
}
