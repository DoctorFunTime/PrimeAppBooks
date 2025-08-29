using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services.APIs;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class DashboardPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly QuickBooksAuthService _authService;
        private readonly QuickBooksService _quickBooksService;

        [ObservableProperty]
        private string _address;

        [ObservableProperty]
        private string _companyName;

        [ObservableProperty]
        private string _legalName;

        public DashboardPageViewModel(
            INavigationService navigationService,
            QuickBooksAuthService authService,
            QuickBooksService quickBooksService)
        {
            _navigationService = navigationService;
            _authService = authService;
            _quickBooksService = quickBooksService;
        }

        [RelayCommand]
        private async Task ExportTransactionsAsync()
        {
            try
            {
                var token = await _authService.AuthenticateAsync();
                var companyInfo = await _quickBooksService.GetCompanyInfoAsync(token.access_token, token.realmId);

                if (token == null)
                {
                    CompanyName = "Token is null";
                    return;
                }

                if (string.IsNullOrEmpty(token.realmId))
                {
                    CompanyName = "Realm ID is missing";
                    return;
                }

                CompanyName = companyInfo.CompanyName;
                LegalName = companyInfo.LegalName;
                Address = companyInfo.CompanyAddr?.Line1;
            }
            catch (Exception ex)
            {
                // Log or show error in UI
                CompanyName = "Error fetching company info";
            }
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