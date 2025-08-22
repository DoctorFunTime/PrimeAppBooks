using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PrimeAppBooks.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public Visibility SidebarContentVisibility => IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CollapsedContentVisibility => IsSidebarExpanded ? Visibility.Collapsed : Visibility.Visible;

        [ObservableProperty]
        private bool _isSidebarExpanded = true;

        [ObservableProperty]
        private GridLength _sidebarWidth = new(230);

        [ObservableProperty]
        private GridLength _sidebarMargin = new(20);

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationService.PageNavigated += OnPageNavigated;
        }

        [RelayCommand]
        private void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;

            if (IsSidebarExpanded)
            {
                SidebarWidth = new GridLength(230);
                SidebarMargin = new GridLength(20);
            }
            else
            {
                SidebarWidth = new GridLength(64);
                SidebarMargin = new GridLength(12);
            }

            OnPropertyChanged(nameof(SidebarContentVisibility));
            OnPropertyChanged(nameof(CollapsedContentVisibility));
        }

        [RelayCommand]
        private void NavigateToDashboard() => _navigationService.NavigateTo<DashboardPage>();

        [RelayCommand]
        private void NavigateToChartOfAccounts() => _navigationService.NavigateTo<ChartOfAccountsPage>();

        [RelayCommand]
        private void NavigateToTransactions() => _navigationService.NavigateTo<TransactionsPage>();

        [RelayCommand]
        private void NavigateToReports() => _navigationService.NavigateTo<ReportsPage>();

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        public bool CanGoBack => _navigationService.CanGoBack;

        private void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));
        }

        [RelayCommand]
        private void Maximize(Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        [RelayCommand]
        private void Minimize(Window window)
        {
            window.WindowState = WindowState.Minimized;
        }

        [RelayCommand]
        private void Close(Window window)
        {
            window.Close();
        }
    }
}