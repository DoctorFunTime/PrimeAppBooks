using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class AssetRegisterPage : BaseAnimatedPage
    {
        public AssetRegisterPage(AssetRegisterPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
