using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for AddPurchaseInvoicePage.xaml
    /// </summary>
    public partial class AddPurchaseInvoicePage : BaseAnimatedPage
    {
        public AddPurchaseInvoicePage(AddPurchaseInvoicePageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public override void OnNavigatedTo(object parameter)
        {
            if (DataContext is AddPurchaseInvoicePageViewModel viewModel)
            {
                int id = 0;
                if (parameter is int intId) id = intId;
                viewModel.Initialize(id);
            }
        }
    }
}