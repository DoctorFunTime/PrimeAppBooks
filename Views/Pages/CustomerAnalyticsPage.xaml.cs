using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class CustomerAnalyticsPage : BaseAnimatedPage
    {
        public CustomerAnalyticsPage(CustomerAnalyticsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
