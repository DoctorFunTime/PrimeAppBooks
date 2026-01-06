using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Views.Pages;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
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
        private bool _isGeneralLedgerSelected;

        [ObservableProperty]
        private bool _isJournalEntriesSelected;

        [ObservableProperty]
        private bool _isAccountTransactionsSelected;

        [ObservableProperty]
        private bool _isBankReconciliationSelected;

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

        [ObservableProperty]
        private bool _isCustomersSelected;

        [ObservableProperty]
        private bool _isVendorsSelected;

        [ObservableProperty]
        private bool _isReceivablesSelected;

        [ObservableProperty]
        private bool _isBadDebtsSelected;

        [ObservableProperty]
        private bool _isCreditNotesSelected;

        [ObservableProperty]
        private bool _isPayablesSelected;

        [ObservableProperty]
        private bool _isDebitNotesSelected;

        [ObservableProperty]
        private bool _isCustomerAnalyticsSelected;

        // Expansion state properties
        private bool _isSalesExpanded = false;
        public bool IsSalesExpanded
        {
            get => _isSalesExpanded;
            set
            {
                if (SetProperty(ref _isSalesExpanded, value))
                {
                    if (value)
                    {
                        IsPurchasesExpanded = false;
                        IsTransactionsExpanded = false;
                    }
                    OnPropertyChanged(nameof(SalesExpandedVisibility));
                }
            }
        }

        private bool _isPurchasesExpanded = false;
        public bool IsPurchasesExpanded
        {
            get => _isPurchasesExpanded;
            set
            {
                if (SetProperty(ref _isPurchasesExpanded, value))
                {
                    if (value)
                    {
                        IsSalesExpanded = false;
                        IsTransactionsExpanded = false;
                    }
                    OnPropertyChanged(nameof(PurchasesExpandedVisibility));
                }
            }
        }

        private bool _isTransactionsExpanded = false;
        public bool IsTransactionsExpanded
        {
            get => _isTransactionsExpanded;
            set
            {
                if (SetProperty(ref _isTransactionsExpanded, value))
                {
                    if (value)
                    {
                        IsSalesExpanded = false;
                        IsPurchasesExpanded = false;
                    }
                    OnPropertyChanged(nameof(TransactionsExpandedVisibility));
                }
            }
        }

        public Visibility SalesExpandedVisibility => IsSalesExpanded ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PurchasesExpandedVisibility => IsPurchasesExpanded ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TransactionsExpandedVisibility => IsTransactionsExpanded ? Visibility.Visible : Visibility.Collapsed;

        // ==========================================================

        [ObservableProperty]
        private bool _isSidebarExpanded = true;

        [ObservableProperty]
        private GridLength _sidebarWidth = new(260);

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
            SidebarWidth = IsSidebarExpanded ? new GridLength(260) : new GridLength(80);
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
        private void NavigateToGeneralLedger()
        {
            _navigationService.NavigateTo<GeneralLedgerPage>();
        }

        [RelayCommand]
        private void NavigateToJournalEntries()
        {
            _navigationService.NavigateTo<TransactionsPage>();
        }

        [RelayCommand]
        private void NavigateToAccountTransactions()
        {
            _navigationService.NavigateTo<AccountTransactionsPage>();
        }

        [RelayCommand]
        private void NavigateToBankReconciliation()
        {
            _navigationService.NavigateTo<BankReconciliationPage>();
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

        [RelayCommand]
        private void NavigateToCustomers()
        {
            _navigationService.NavigateTo<CustomersPage>();
        }

        [RelayCommand]
        private void NavigateToCustomerAnalytics()
        {
            _navigationService.NavigateTo<CustomerAnalyticsPage>();
        }

        [RelayCommand]
        private void NavigateToVendors()
        {
            // _navigationService.NavigateTo<VendorsPage>();
        }

        [RelayCommand]
        private void NavigateToReceivables()
        {
            _navigationService.NavigateTo<ReceivablesPage>();
        }

        [RelayCommand]
        private void NavigateToBadDebts()
        {
            _navigationService.NavigateTo<BadDebtsPage>();
        }

        [RelayCommand]
        private void NavigateToCreditNotes()
        {
            _navigationService.NavigateTo<CreditNotesPage>();
        }

        [RelayCommand]
        private void NavigateToPayables()
        {
            _navigationService.NavigateTo<PayablesPage>();
        }

        [RelayCommand]
        private void NavigateToDebitNotes()
        {
            _navigationService.NavigateTo<DebitNotesPage>();
        }

        private void SetAllNavigationToFalse()
        {
            IsDashboardSelected = false;
            IsChartOfAccountsSelected = false;
            IsTransactionsSelected = false;
            IsGeneralLedgerSelected = false;
            IsJournalEntriesSelected = false;
            IsAccountTransactionsSelected = false;
            IsBankReconciliationSelected = false;
            IsReportsSelected = false;
            IsAuditTrailsSelected = false;
            IsSettingsSelected = false;
            IsUserManagementSelected = false;
            IsSecurityCenterSelected = false;
            IsHelpSupportSelected = false;
            IsSalesSelected = false;
            IsPurchasesSelected = false;
            IsCustomersSelected = false;
            IsVendorsSelected = false;
            IsReceivablesSelected = false;
            IsBadDebtsSelected = false;
            IsCreditNotesSelected = false;
            IsPayablesSelected = false;
            IsDebitNotesSelected = false;
            IsCustomerAnalyticsSelected = false;
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

                case GeneralLedgerPage:
                    IsGeneralLedgerSelected = true;
                    IsTransactionsExpanded = true;
                    OnPropertyChanged(nameof(TransactionsExpandedVisibility));
                    break;

                case JournalPage:
                    IsJournalEntriesSelected = true;
                    IsTransactionsExpanded = true;
                    OnPropertyChanged(nameof(TransactionsExpandedVisibility));
                    break;

                case AccountTransactionsPage:
                    IsAccountTransactionsSelected = true;
                    IsTransactionsExpanded = true;
                    OnPropertyChanged(nameof(TransactionsExpandedVisibility));
                    break;

                case BankReconciliationPage:
                    IsBankReconciliationSelected = true;
                    IsTransactionsExpanded = true;
                    OnPropertyChanged(nameof(TransactionsExpandedVisibility));
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

                case CustomersPage:
                    IsCustomersSelected = true;
                    break;

                case CustomerAnalyticsPage:
                    IsCustomerAnalyticsSelected = true;
                    IsSalesExpanded = true;
                    OnPropertyChanged(nameof(SalesExpandedVisibility));
                    break;

                case ReceivablesPage:
                    IsReceivablesSelected = true;
                    IsSalesExpanded = true;
                    OnPropertyChanged(nameof(SalesExpandedVisibility));
                    break;

                case BadDebtsPage:
                    IsBadDebtsSelected = true;
                    IsSalesExpanded = true;
                    OnPropertyChanged(nameof(SalesExpandedVisibility));
                    break;

                case CreditNotesPage:
                    IsCreditNotesSelected = true;
                    IsSalesExpanded = true;
                    OnPropertyChanged(nameof(SalesExpandedVisibility));
                    break;

                case PayablesPage:
                    IsPayablesSelected = true;
                    IsPurchasesExpanded = true;
                    OnPropertyChanged(nameof(PurchasesExpandedVisibility));
                    break;

                case DebitNotesPage:
                    IsDebitNotesSelected = true;
                    IsPurchasesExpanded = true;
                    OnPropertyChanged(nameof(PurchasesExpandedVisibility));
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