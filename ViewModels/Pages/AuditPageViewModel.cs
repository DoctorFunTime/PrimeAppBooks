using CommunityToolkit.Mvvm.ComponentModel;
using PrimeAppBooks.Interfaces;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AuditPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public AuditPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }
    }
}