using PrimeAppBooks.ViewModels.Pages;

namespace PrimeAppBooks.Views.Pages
{
    public partial class DepreciationRunPage : BaseAnimatedPage
    {
        public DepreciationRunPage(DepreciationRunViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
