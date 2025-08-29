using CommunityToolkit.Mvvm.ComponentModel;
using PrimeAppBooks.Interfaces;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class ReportsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public ReportsPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        // Add your reports-specific properties and commands here
    }
}
