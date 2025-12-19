using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for AddSalesInvoicePage.xaml
    /// </summary>
    public partial class AddSalesInvoicePage : BaseAnimatedPage
    {
        public AddSalesInvoicePage(AddSalesInvoicePageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
