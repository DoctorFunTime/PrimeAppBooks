using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class AddVendorPage : BaseAnimatedPage
    {
        public AddVendorPage(AddVendorPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public override void OnNavigatedTo(object parameter)
        {
            if (parameter is int vendorId && DataContext is AddVendorPageViewModel viewModel)
            {
                viewModel.Initialize(vendorId);
            }
        }
    }
}
