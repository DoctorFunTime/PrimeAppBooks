using System.Windows;
using PrimeAppBooks.ViewModels.Pages;

namespace PrimeAppBooks.Views.Pages
{
    public partial class AddEditAssetPage : BaseAnimatedPage
    {
        private readonly AddEditAssetPageViewModel _viewModel;
        private object _pendingParameter;
        private bool _initialized;

        public AddEditAssetPage(AddEditAssetPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;

            // Loaded fires after OnNavigatedTo, so this is the safe fallback
            // for new-asset navigation where parameter is null (NavigationService
            // skips OnNavigatedTo when parameter is null).
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (!_initialized)
            {
                _initialized = true;
                _ = _viewModel.InitializeAsync(_pendingParameter);
            }
        }

        public override void OnNavigatedTo(object parameter)
        {
            base.OnNavigatedTo(parameter);
            _pendingParameter = parameter;
            _initialized = true;
            _ = _viewModel.InitializeAsync(parameter);
        }
    }
}
