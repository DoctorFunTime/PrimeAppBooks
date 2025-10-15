using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Views.Pages;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class TransactionsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly TransactionsServices _transactionsServices;
        public ObservableCollection<Bill> Bills { get; } = new();

        public TransactionsPageViewModel(INavigationService navigationService, TransactionsServices transactionsServices)
        {
            _navigationService = navigationService;
            _transactionsServices = transactionsServices;
            _navigationService.PageNavigated += OnPageNavigated;

            LoadBillsAsync();
        }

        private async void LoadBillsAsync()
        {
            var list = await _transactionsServices.GetAllBillsAsync();
            Bills.Clear();
            foreach (var bill in list)
                Bills.Add(bill);
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