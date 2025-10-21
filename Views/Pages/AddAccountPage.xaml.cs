using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for AddAccountPage.xaml
    /// </summary>
    public partial class AddAccountPage : BaseAnimatedPage
    {
        public AddAccountPage(AddAccountPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}