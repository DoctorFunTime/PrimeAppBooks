using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for PayablesPage.xaml
    /// </summary>
    public partial class PayablesPage : BaseAnimatedPage
    {
        public PayablesPage(PayablesPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public override async void OnNavigatedTo(object parameter)
        {
            if (DataContext is PayablesPageViewModel viewModel)
            {
                await viewModel.LoadDataAsync();
            }
        }
    }
}
