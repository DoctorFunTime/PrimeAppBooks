using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Views.Pages;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
using System.Windows;
using System.Windows.Controls;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class TransactionsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public TransactionsPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationService.PageNavigated += OnPageNavigated;
        }

        [RelayCommand]
        private void NavigateToJournalPage() => _navigationService.NavigateTo<JournalPage>();

        private void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        public bool CanGoBack => _navigationService.CanGoBack;
    }
}