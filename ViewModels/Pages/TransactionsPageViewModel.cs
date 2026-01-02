using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using PrimeAppBooks.Views.Pages.SubTransactionsPage;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class TransactionsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();
        private readonly IJournalNavigationService _journalNavigationService;

        public ObservableCollection<Bill> Bills { get; } = new();
        public ObservableCollection<JournalEntry> JournalEntries { get; } = new();
        public ObservableCollection<JournalEntry> FilteredJournalEntries { get; } = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private JournalEntry _selectedJournalEntry;

        private bool _showAllTransactions = true;

        public bool ShowAllTransactions
        {
            get => _showAllTransactions;
            set
            {
                if (SetProperty(ref _showAllTransactions, value) && value)
                {
                    ShowDraftsOnly = false;
                    ShowPostedOnly = false;
                    ApplyFilters();
                }
            }
        }

        private bool _showDraftsOnly = false;

        public bool ShowDraftsOnly
        {
            get => _showDraftsOnly;
            set
            {
                if (SetProperty(ref _showDraftsOnly, value) && value)
                {
                    ShowAllTransactions = false;
                    ShowPostedOnly = false;
                    ApplyFilters();
                }
            }
        }

        private bool _showPostedOnly = false;

        public bool ShowPostedOnly
        {
            get => _showPostedOnly;
            set
            {
                if (SetProperty(ref _showPostedOnly, value) && value)
                {
                    ShowAllTransactions = false;
                    ShowDraftsOnly = false;
                    ApplyFilters();
                }
            }
        }

        private string _searchText = string.Empty;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        private DateTime? _startDate;

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    ApplyFilters();
                }
            }
        }

        private DateTime? _endDate;

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    ApplyFilters();
                }
            }
        }

        [ObservableProperty]
        private int _draftEntriesCount = 0;

        [ObservableProperty]
        private int _postedTodayCount = 0;

        [ObservableProperty]
        private int _postedThisWeekCount = 0;

        [ObservableProperty]
        private int _monthlyTransactionCount = 0;

        [ObservableProperty]
        private int _unbalancedEntriesCount = 0;

        [ObservableProperty]
        private int _recentActivityCount = 0;

        [ObservableProperty]
        private string _balanceStatus = "BALANCED";

        [ObservableProperty]
        private System.Windows.Media.Brush _balanceStatusColor = System.Windows.Media.Brushes.LightGreen;

        [ObservableProperty]
        private ObservableCollection<PendingAction> _pendingActions = new();

        [ObservableProperty]
        private int _pendingActionsCount = 0;

        [ObservableProperty]
        private string _resultsSummary = "No entries found";

        private int? _selectedAccountId;

        public int? SelectedAccountId
        {
            get => _selectedAccountId;
            set
            {
                if (SetProperty(ref _selectedAccountId, value))
                {
                    ApplyFilters();
                }
            }
        }

        private ObservableCollection<ChartOfAccount> _accountFilters = new();

        public ObservableCollection<ChartOfAccount> AccountFilters
        {
            get => _accountFilters;
            set => SetProperty(ref _accountFilters, value);
        }

        public TransactionsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider, IJournalNavigationService journalNavigationService)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _journalNavigationService = journalNavigationService;
            _navigationService.PageNavigated += OnPageNavigated;
        }

        private async void LoadBillsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TransactionsServices>();

            try
            {
                IsLoading = true;
                var list = await service.GetAllBillsAsync();
                Bills.Clear();
                foreach (var bill in list)
                    Bills.Add(bill);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadJournalEntriesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var journalService = scope.ServiceProvider.GetRequiredService<JournalServices>();

            try
            {
                IsLoading = true;
                var entries = await journalService.GetAllJournalEntriesAsync();

                JournalEntries.Clear();
                foreach (var entry in entries)
                {
                    JournalEntries.Add(entry);
                }

                UpdateStatistics();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading journal entries: {ex.Message}", "Error", "ErrorOutline");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void LoadAccountFiltersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var journalService = scope.ServiceProvider.GetRequiredService<JournalServices>();

            try
            {
                var accounts = await journalService.GetAllAccountsAsync();
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AccountFilters.Clear();
                    
                    // Add "All Accounts" option
                    AccountFilters.Add(new ChartOfAccount 
                    { 
                        AccountId = 0, 
                        AccountName = "All Accounts", 
                        AccountNumber = "000" 
                    });
                    
                    foreach (var account in accounts)
                    {
                        AccountFilters.Add(account);
                    }
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading accounts: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        private void UpdateStatistics()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            // Count statistics
            DraftEntriesCount = JournalEntries.Count(e => e.Status == "DRAFT");
            PostedTodayCount = JournalEntries.Count(e => e.Status == "POSTED" && e.PostedAt?.Date == today);
            MonthlyTransactionCount = JournalEntries.Count(e => e.JournalDate >= monthStart);
            
            // More meaningful statistics
            PostedThisWeekCount = JournalEntries.Count(e => e.Status == "POSTED" && e.PostedAt >= weekStart);
            UnbalancedEntriesCount = JournalEntries.Count(e => e.Status == "DRAFT" && !IsEntryBalanced(e));
            RecentActivityCount = JournalEntries.Count(e => e.JournalDate >= today.AddDays(-7));
            
            // Balance status
            UpdateBalanceStatus();
            
            // Update pending actions
            UpdatePendingActions();
            PendingActionsCount = PendingActions.Count;
        }

        private bool IsEntryBalanced(JournalEntry entry)
        {
            if (entry.JournalLines == null || !entry.JournalLines.Any())
                return false;
                
            var totalDebits = entry.JournalLines.Sum(l => l.DebitAmount);
            var totalCredits = entry.JournalLines.Sum(l => l.CreditAmount);
            return Math.Abs(totalDebits - totalCredits) < 0.01m;
        }

        private void UpdateBalanceStatus()
        {
            var unbalancedDrafts = JournalEntries.Where(e => e.Status == "DRAFT" && !IsEntryBalanced(e)).Count();
            
            if (unbalancedDrafts == 0)
            {
                BalanceStatus = "BALANCED";
                BalanceStatusColor = System.Windows.Media.Brushes.LightGreen;
            }
            else
            {
                BalanceStatus = $"{unbalancedDrafts} UNBALANCED";
                BalanceStatusColor = System.Windows.Media.Brushes.LightCoral;
            }
        }

        private void UpdatePendingActions()
        {
            PendingActions.Clear();
            
            var draftCount = DraftEntriesCount;
            var unbalancedCount = UnbalancedEntriesCount;
            
            if (draftCount > 0)
            {
                PendingActions.Add(new PendingAction
                {
                    ActionType = "Post All Drafts",
                    Description = $"{draftCount} draft entries ready for posting",
                    ActionId = "POST_ALL_DRAFTS",
                    Priority = "High"
                });
            }
            
            if (unbalancedCount > 0)
            {
                PendingActions.Add(new PendingAction
                {
                    ActionType = "Fix Unbalanced Entries",
                    Description = $"{unbalancedCount} entries need balancing",
                    ActionId = "FIX_UNBALANCED",
                    Priority = "High"
                });
            }
            
            // Check for old drafts (older than 7 days)
            var oldDrafts = JournalEntries.Count(e => e.Status == "DRAFT" && e.JournalDate < DateTime.Today.AddDays(-7));
            if (oldDrafts > 0)
            {
                PendingActions.Add(new PendingAction
                {
                    ActionType = "Review Old Drafts",
                    Description = $"{oldDrafts} drafts older than 7 days",
                    ActionId = "REVIEW_OLD_DRAFTS",
                    Priority = "Medium"
                });
            }
            
            // Check for entries without reference numbers
            var noReferenceCount = JournalEntries.Count(e => string.IsNullOrWhiteSpace(e.Reference));
            if (noReferenceCount > 0)
            {
                PendingActions.Add(new PendingAction
                {
                    ActionType = "Add Reference Numbers",
                    Description = $"{noReferenceCount} entries missing reference numbers",
                    ActionId = "ADD_REFERENCES",
                    Priority = "Low"
                });
            }
        }

        private void ApplyFilters()
        {
            var filtered = JournalEntries.AsEnumerable();

            // Status filter
            if (ShowDraftsOnly)
                filtered = filtered.Where(e => e.Status == "DRAFT");
            else if (ShowPostedOnly)
                filtered = filtered.Where(e => e.Status == "POSTED");

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(e =>
                    e.Description.ToLower().Contains(searchLower) ||
                    (e.Reference != null && e.Reference.ToLower().Contains(searchLower)) ||
                    e.JournalNumber.ToLower().Contains(searchLower));
            }

            // Date range filter
            if (StartDate.HasValue)
                filtered = filtered.Where(e => e.JournalDate >= StartDate.Value);
            if (EndDate.HasValue)
                filtered = filtered.Where(e => e.JournalDate <= EndDate.Value);

            // Account filter
            if (SelectedAccountId.HasValue && SelectedAccountId.Value > 0)
            {
                filtered = filtered.Where(e => e.JournalLines.Any(l => l.AccountId == SelectedAccountId.Value));
            }

            FilteredJournalEntries.Clear();
            foreach (var entry in filtered.OrderByDescending(e => e.CreatedAt))
            {
                FilteredJournalEntries.Add(entry);
            }

            UpdateResultsSummary();
        }

        private void UpdateResultsSummary()
        {
            var count = FilteredJournalEntries.Count;
            ResultsSummary = count == 1 ? "1 entry found" : $"{count} entries found";
        }

        [RelayCommand]
        private void NavigateToJournalPage() => _navigationService.NavigateTo<JournalPage>();

        [RelayCommand]
        private async Task HandlePendingAction(PendingAction action)
        {
            if (action == null) return;

            try
            {
                switch (action.ActionId)
                {
                    case "POST_ALL_DRAFTS":
                        await PostAllDrafts();
                        break;
                    case "FIX_UNBALANCED":
                        ShowUnbalancedEntries();
                        break;
                    case "REVIEW_OLD_DRAFTS":
                        ShowOldDrafts();
                        break;
                    case "ADD_REFERENCES":
                        ShowMissingReferences();
                        break;
                }
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error handling action: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        private async Task PostAllDrafts()
        {
            var drafts = JournalEntries.Where(e => e.Status == "DRAFT" && IsEntryBalanced(e)).ToList();
            
            if (!drafts.Any())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("No balanced draft entries found to post.", "Info", "InfoOutline");
                });
                return;
            }

            var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return _messageBoxService.ShowConfirmation(
                    $"Are you sure you want to post {drafts.Count} draft entries?\n\nThis action cannot be undone.",
                    "Post All Drafts",
                    "CheckCircleOutline"
                );
            });

            if (result)
            {
                using var scope = _serviceProvider.CreateScope();
                var journalServices = scope.ServiceProvider.GetRequiredService<JournalServices>();
                
                int postedCount = 0;
                foreach (var draft in drafts)
                {
                    try
                    {
                        await journalServices.PostJournalEntryAsync(draft.JournalId, 1); // TODO: Get current user ID
                        postedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error posting journal {draft.JournalId}: {ex.Message}");
                    }
                }

                await LoadJournalEntriesAsync();
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Successfully posted {postedCount} out of {drafts.Count} draft entries.", "Success", "CheckCircleOutline");
                });
            }
        }

        private void ShowUnbalancedEntries()
        {
            ShowDraftsOnly = true;
            // TODO: Add additional filter for unbalanced entries
        }

        private void ShowOldDrafts()
        {
            ShowDraftsOnly = true;
            StartDate = DateTime.Today.AddDays(-30); // Show drafts from last 30 days
        }

        private void ShowMissingReferences()
        {
            ShowAllTransactions = true;
            // TODO: Add filter for entries without references
        }

        [RelayCommand]
        private void NavigateToJournalPageWithEntry(JournalEntry entry)
        {
            if (entry != null)
            {
                // Set the journal ID to edit using the injected navigation service
                _journalNavigationService.SetEditingJournalId(entry.JournalId);
                
                // Navigate to JournalPage
                _navigationService.NavigateTo<JournalPage>();
            }
        }

        [RelayCommand]
        private async Task EditJournalEntry(JournalEntry entry)
        {
            if (entry == null) return;

            // Block editing of posted journal entries
            if (entry.Status == "POSTED")
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Posted journal entries cannot be edited. Only draft entries can be modified.", "Cannot Edit", "Warning");
                });
                return;
            }

            try
            {
                // Navigate to JournalPage with the selected entry for editing
                NavigateToJournalPageWithEntry(entry);
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error editing journal entry: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        [RelayCommand]
        private async Task PostJournalEntry(JournalEntry entry)
        {
            if (entry == null || entry.Status != "DRAFT") return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var journalService = scope.ServiceProvider.GetRequiredService<JournalServices>();

                await journalService.PostJournalEntryAsync(entry.JournalId, 1); // TODO: Get current user ID

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Journal entry posted successfully!", "Success", "CheckCircleOutline");
                });

                await LoadJournalEntriesAsync(); // Refresh the list
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error posting journal entry: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        [RelayCommand]
        private async Task DeleteJournalEntry(JournalEntry entry)
        {
            if (entry == null || entry.Status != "DRAFT") return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var journalService = scope.ServiceProvider.GetRequiredService<JournalServices>();

                await journalService.DeleteJournalEntryAsync(entry.JournalId);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage("Journal entry deleted successfully!", "Success", "CheckCircleOutline");
                });

                await LoadJournalEntriesAsync(); // Refresh the list
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error deleting journal entry: {ex.Message}", "Error", "ErrorOutline");
                });
            }
        }

        [RelayCommand]
        private void ApplyFiltersCommand() => ApplyFilters();

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            StartDate = null;
            EndDate = null;
            SelectedAccountId = null;
            ShowAllTransactions = true;
            ShowDraftsOnly = false;
            ShowPostedOnly = false;
            ApplyFilters();
        }

        [RelayCommand]
        private async Task Refresh() => await LoadJournalEntriesAsync();

        private async void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));

            // Load data when TransactionsPage is navigated to
            if (page is TransactionsPage)
            {
                LoadBillsAsync();
                await LoadJournalEntriesAsync();
                LoadAccountFiltersAsync();
            }
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        public bool CanGoBack => _navigationService.CanGoBack;
    }

    public class PendingAction
    {
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActionId { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }
}