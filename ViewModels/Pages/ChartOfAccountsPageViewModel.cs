using CommunityToolkit.Mvvm.ComponentModel;
using PrimeAppBooks.Interfaces;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class ChartOfAccountsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public ChartOfAccountsPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }
    }
}