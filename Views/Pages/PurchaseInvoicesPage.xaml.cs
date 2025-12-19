using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class PurchaseInvoicesPage : BaseAnimatedPage
    {
        public PurchaseInvoicesPage(PurchaseInvoicesPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
