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
            
            // Register custom animations for different pages to improve navigation experience
            RegisterPageAnimations();
            
            // Navigate to dashboard by default
            NavigateToDashboard();
        }

        private void RegisterPageAnimations()
        {
            // Dashboard - smooth slide from bottom
            _navigationService.RegisterPageAnimation<DashboardPage>(AnimationDirection.FromBottom);
            
            // Chart of Accounts - slide from right (feels like opening a drawer)
            _navigationService.RegisterPageAnimation<ChartOfAccountsPage>(AnimationDirection.FromRight);
            
            // Transactions - slide from right (consistent with accounts)
            _navigationService.RegisterPageAnimation<TransactionsPage>(AnimationDirection.FromRight);
            
            // Reports - slide from top (feels like opening a report)
            _navigationService.RegisterPageAnimation<ReportsPage>(AnimationDirection.FromTop);
            
            // Audit - fade in (more subtle for data-heavy pages)
            _navigationService.RegisterPageAnimation<Audit>(AnimationDirection.FadeIn);
            
            // Settings - slide from left (feels like opening a side panel)
            _navigationService.RegisterPageAnimation<Settings>(AnimationDirection.FromLeft);
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
            SetAllNavigationToFalse();
            IsDashboardSelected = true;
            _navigationService.NavigateTo<DashboardPage>();
        }

        [RelayCommand]
        private void NavigateToChartOfAccounts()
        {
            SetAllNavigationToFalse();
            IsChartOfAccountsSelected = true;
            _navigationService.NavigateTo<ChartOfAccountsPage>();
        }

        [RelayCommand]
        private void NavigateToTransactions()
        {
            SetAllNavigationToFalse();
            IsTransactionsSelected = true;
            _navigationService.NavigateTo<TransactionsPage>();
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            SetAllNavigationToFalse();
            IsReportsSelected = true;
            _navigationService.NavigateTo<ReportsPage>();
        }

        [RelayCommand]
        private void NavigateToAuditTrails()
        {
            SetAllNavigationToFalse();
            IsAuditTrailsSelected = true;
            _navigationService.NavigateTo<Audit>();
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            SetAllNavigationToFalse();
            IsSettingsSelected = true;
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
            }
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