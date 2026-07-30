using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class AddEditInventoryPage : BaseAnimatedPage
    {
        public AddEditInventoryPage(AddEditInventoryPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public override async void OnNavigatedTo(object parameter)
        {
            if (DataContext is AddEditInventoryPageViewModel viewModel)
            {
                int id = 0;
                if (parameter is int intId) id = intId;
                await viewModel.InitializeAsync(id);
            }
        }
    }
}
