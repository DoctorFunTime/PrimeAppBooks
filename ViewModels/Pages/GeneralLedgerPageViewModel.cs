using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class GeneralLedgerPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        [ObservableProperty]
        private decimal _totalDebits;

        [ObservableProperty]
        private decimal _totalCredits;

        [ObservableProperty]
        private decimal _netChange;

        public ObservableCollection<JournalLine> Transactions { get; } = new();

        public GeneralLedgerPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            // Default date range: Last 30 days
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
        }

        public async Task Initialize()
        {
            await LoadTransactionsAsync();
        }

        [RelayCommand]
        private async Task LoadTransactionsAsync()
        {
            IsLoading = true;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var journalServices = scope.ServiceProvider.GetRequiredService<JournalServices>();

                // Convert to UTC for database consistency if needed
                var utcStart = StartDate.HasValue ? DateTime.SpecifyKind(StartDate.Value, DateTimeKind.Utc) : (DateTime?)null;
                var utcEnd = EndDate.HasValue ? DateTime.SpecifyKind(EndDate.Value.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

                var lines = await journalServices.GetJournalLinesAsync(utcStart, utcEnd);

                // Filter to only POSTED status for General Ledger view
                var postedLines = lines.Where(l => l.JournalEntry?.Status == "POSTED")
                                      .OrderByDescending(l => l.LineDate)
                                      .ToList();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions.Clear();
                    foreach (var line in postedLines)
                    {
                        Transactions.Add(line);
                    }

                    CalculateTotals();
                });
            }
            catch (Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _messageBoxService.ShowMessage($"Error loading General Ledger: {ex.Message}", "Error", "ErrorOutline");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateTotals()
        {
            TotalDebits = Transactions.Sum(t => t.DebitAmount);
            TotalCredits = Transactions.Sum(t => t.CreditAmount);
            NetChange = TotalDebits - TotalCredits;
        }

        [RelayCommand]
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(StartDate) || e.PropertyName == nameof(EndDate))
            {
                if (!IsLoading)
                {
                    LoadTransactionsAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
