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
    }
}
