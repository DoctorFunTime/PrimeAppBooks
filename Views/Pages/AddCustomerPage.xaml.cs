using PrimeAppBooks.ViewModels.Pages;

namespace PrimeAppBooks.Views.Pages
{
    public partial class AddCustomerPage : BaseAnimatedPage
    {
        public AddCustomerPage(AddCustomerPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
