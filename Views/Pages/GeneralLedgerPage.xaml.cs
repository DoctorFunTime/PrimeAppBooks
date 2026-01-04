using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class GeneralLedgerPage : BaseAnimatedPage
    {
        private readonly GeneralLedgerPageViewModel _viewModel;

        public GeneralLedgerPage(GeneralLedgerPageViewModel viewModel)
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