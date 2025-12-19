using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class CustomersPage : BaseAnimatedPage
    {
        public CustomersPage(CustomersPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
