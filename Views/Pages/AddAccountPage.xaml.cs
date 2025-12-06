using PrimeAppBooks.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for AddAccountPage.xaml
    /// </summary>
    public partial class AddAccountPage : BaseAnimatedPage
    {
        private readonly AddAccountPageViewModel _viewModel;

        public AddAccountPage(AddAccountPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
            
            System.Diagnostics.Debug.WriteLine("AddAccountPage constructor called");
        }
    }
}