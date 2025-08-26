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

        // ==========================================================
        // == 1. ADD PROPERTIES TO TRACK THE SELECTED NAVIGATION ITEM ==
        // ==========================================================
        [ObservableProperty]
        private bool _isDashboardSelected;

        [ObservableProperty]
        private bool _isChartOfAccountsSelected;

        [ObservableProperty]
        private bool _isTransactionsSelected;

        [ObservableProperty]
        private bool _isReportsSelected;

        [ObservableProperty]
        private bool _isAuditTrailsSelected;

        [ObservableProperty]
        private bool _isSettingsSelected;

        [ObservableProperty]
        private bool _isUserManagementSelected;

        [ObservableProperty]
        private bool _isSecurityCenterSelected;

        [ObservableProperty]
        private bool _isHelpSupportSelected;

        // ==========================================================

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
            SidebarWidth = IsSidebarExpanded ? new GridLength(230) : new GridLength(64);
            SidebarMargin = IsSidebarExpanded ? new GridLength(20) : new GridLength(12);
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
        private void NavigateToAuditTrails() => _navigationService.NavigateTo<Audit>();

        [RelayCommand]
        private void NavigateToSettings() => _navigationService.NavigateTo<Settings>();

        private void OnPageNavigated(object sender, Page page)
        {
            // ==========================================================
            // == 2. UPDATE THE PROPERTIES BASED ON THE CURRENT PAGE   ==
            // ==========================================================
            IsDashboardSelected = page is DashboardPage;
            IsChartOfAccountsSelected = page is ChartOfAccountsPage;
            IsTransactionsSelected = page is TransactionsPage;
            IsReportsSelected = page is ReportsPage;
            IsAuditTrailsSelected = page is Audit;
            IsSettingsSelected = page is Settings;
            // Set other properties to false if they exist, e.g.:
            // IsUserManagementSelected = page is UserManagementPage;
            // IsSecurityCenterSelected = page is SecurityCenterPage;
            // IsHelpSupportSelected = page is HelpSupportPage;
            // ==========================================================

            OnPropertyChanged(nameof(CanGoBack));
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        public bool CanGoBack => _navigationService.CanGoBack;

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