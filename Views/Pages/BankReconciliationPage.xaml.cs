using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class BankReconciliationPage : BaseAnimatedPage
    {
        private readonly BankReconciliationViewModel _viewModel;

        public BankReconciliationPage(BankReconciliationViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await _viewModel.Initialize();
        }
    }
}
