using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Views.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace PrimeAppBooks.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly SplashscreenInitialisations _splashscreenInitialisations;
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
        private bool _isSalesSelected;

        [ObservableProperty]
        private bool _isPurchasesSelected;

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

        [ObservableProperty]
        private bool _isLoading = false;

        public MainWindowViewModel(INavigationService navigationService, SplashscreenInitialisations splashscreenInitialisations)
        {
            _navigationService = navigationService;
            _splashscreenInitialisations = splashscreenInitialisations;
            _navigationService.PageNavigated += OnPageNavigated;
            _navigationService.LoadingStateChanged += OnLoadingStateChanged;

            // Navigate to dashboard by default - but don't set navigation state here
            // The OnPageNavigated event handler will handle the state setting
            _navigationService.NavigateTo<DashboardPage>();

            TestConnection();
        }

        private void TestConnection()
        {
            _splashscreenInitialisations.TestConnectionToDatabase();
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
        private void NavigateToDashboard()
        {
            _navigationService.NavigateTo<DashboardPage>();
        }

        [RelayCommand]
        private void NavigateToChartOfAccounts()
        {
            _navigationService.NavigateTo<ChartOfAccountsPage>();
        }

        [RelayCommand]
        private void NavigateToTransactions()
        {
            _navigationService.NavigateTo<TransactionsPage>();
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            _navigationService.NavigateTo<ReportsPage>();
        }

        [RelayCommand]
        private void NavigateToAuditTrails()
        {
            _navigationService.NavigateTo<Audit>();
        }

        [RelayCommand]
        private void NavigateToSales()
        {
            _navigationService.NavigateTo<SalesInvoicesPage>();
        }

        [RelayCommand]
        private void NavigateToPurchases()
        {
            _navigationService.NavigateTo<PurchaseInvoicesPage>();
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            _navigationService.NavigateTo<Settings>();
        }

        private void SetAllNavigationToFalse()
        {
            IsDashboardSelected = false;
            IsChartOfAccountsSelected = false;
            IsTransactionsSelected = false;
            IsReportsSelected = false;
            IsAuditTrailsSelected = false;
            IsSettingsSelected = false;
            IsUserManagementSelected = false;
            IsSecurityCenterSelected = false;
            IsHelpSupportSelected = false;
            IsSalesSelected = false;
            IsPurchasesSelected = false;
        }

        private void OnPageNavigated(object sender, Page page)
        {
            // Update navigation selection based on the page type
            SetAllNavigationToFalse();

            switch (page)
            {
                case DashboardPage:
                    IsDashboardSelected = true;
                    break;

                case ChartOfAccountsPage:
                    IsChartOfAccountsSelected = true;
                    break;

                case TransactionsPage:
                    IsTransactionsSelected = true;
                    break;

                case ReportsPage:
                    IsReportsSelected = true;
                    break;

                case Audit:
                    IsAuditTrailsSelected = true;
                    break;

                case Settings:
                    IsSettingsSelected = true;
                    break;

                case SalesInvoicesPage:
                    IsSalesSelected = true;
                    break;

                case PurchaseInvoicesPage:
                    IsPurchasesSelected = true;
                    break;
            }
        }

        private void OnLoadingStateChanged(object sender, bool isLoading)
        {
            IsLoading = isLoading;
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