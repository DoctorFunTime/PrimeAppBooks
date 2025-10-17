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

        [ObservableProperty]
        private bool _showAllTransactions = true;

        [ObservableProperty]
        private bool _showDraftsOnly = false;

        [ObservableProperty]
        private bool _showPostedOnly = false;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        [ObservableProperty]
        private int _draftEntriesCount = 0;

        [ObservableProperty]
        private int _postedTodayCount = 0;

        [ObservableProperty]
        private decimal _postedTodayAmount = 0;

        [ObservableProperty]
        private int _monthlyTransactionCount = 0;

        [ObservableProperty]
        private decimal _monthlyAmount = 0;

        [ObservableProperty]
        private string _resultsSummary = "No entries found";

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

        private async void LoadJournalEntriesAsync()
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

        private void UpdateStatistics()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            DraftEntriesCount = JournalEntries.Count(e => e.Status == "DRAFT");
            PostedTodayCount = JournalEntries.Count(e => e.Status == "POSTED" && e.PostedAt?.Date == today);
            PostedTodayAmount = JournalEntries.Where(e => e.Status == "POSTED" && e.PostedAt?.Date == today).Sum(e => e.Amount);
            MonthlyTransactionCount = JournalEntries.Count(e => e.JournalDate >= monthStart);
            MonthlyAmount = JournalEntries.Where(e => e.JournalDate >= monthStart).Sum(e => e.Amount);
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

                LoadJournalEntriesAsync(); // Refresh the list
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

                LoadJournalEntriesAsync(); // Refresh the list
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
            ShowAllTransactions = true;
            ShowDraftsOnly = false;
            ShowPostedOnly = false;
            ApplyFilters();
        }

        [RelayCommand]
        private void Refresh() => LoadJournalEntriesAsync();

        private void OnPageNavigated(object sender, Page page)
        {
            OnPropertyChanged(nameof(CanGoBack));

            // Load data when TransactionsPage is navigated to
            if (page is TransactionsPage)
            {
                LoadBillsAsync();
                LoadJournalEntriesAsync();
            }
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();

        public bool CanGoBack => _navigationService.CanGoBack;
    }
}