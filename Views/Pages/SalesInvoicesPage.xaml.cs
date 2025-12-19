using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class SalesInvoicesPage : BaseAnimatedPage
    {
        public SalesInvoicesPage(SalesInvoicesPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
