using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class CollectionManagementViewModel : ObservableObject
    {
        private readonly CustomerAnalyticsService _analyticsService;
        private readonly INavigationService _navigationService;
        private readonly BoxServices _messageBoxService = new();
        private int _customerId;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private CustomerSummaryMetrics _customerMetrics;

        public ObservableCollection<CollectionFollowup> FollowupHistory { get; } = new();
        public ObservableCollection<PaymentPlan> ActivePlans { get; } = new();

        // New Follow-up form properties
        [ObservableProperty] private DateTime _newFollowupDate = DateTime.Today;

        [ObservableProperty] private string _newFollowupMethod = "Phone";
        [ObservableProperty] private string _newFollowupOutcome = string.Empty;
        [ObservableProperty] private string _newFollowupNotes = string.Empty;
        [ObservableProperty] private DateTime _nextFollowupDate = DateTime.Today.AddDays(7);

        // New Payment Plan form properties
        [ObservableProperty] private string _newPlanName = string.Empty;

        [ObservableProperty] private decimal _planTotalAmount;
        [ObservableProperty] private int _numberOfInstallments = 3;
        [ObservableProperty] private decimal _monthlyInstallment;

        public CollectionManagementViewModel(CustomerAnalyticsService analyticsService, INavigationService navigationService)
        {
            _analyticsService = analyticsService;
            _navigationService = navigationService;
        }

        public async Task Initialize(int customerId)
        {
            _customerId = customerId;
            await LoadDataAsync();
        }

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // Get analytics for this specific customer
                var allMetrics = await _analyticsService.GetOverallAnalyticsAsync();
                CustomerMetrics = allMetrics.FirstOrDefault(m => m.CustomerId == _customerId);

                if (CustomerMetrics != null)
                {
                    PlanTotalAmount = CustomerMetrics.TotalOutstanding;
                    UpdateMonthlyInstallment();
                }

                var followups = await _analyticsService.GetFollowupHistoryAsync(_customerId);
                var plans = await _analyticsService.GetPaymentPlansAsync(_customerId);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FollowupHistory.Clear();
                    foreach (var f in followups) FollowupHistory.Add(f);

                    ActivePlans.Clear();
                    foreach (var p in plans) ActivePlans.Add(p);
                });
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error loading history: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddFollowup()
        {
            if (string.IsNullOrWhiteSpace(NewFollowupOutcome))
            {
                _messageBoxService.ShowMessage("Please enter an outcome for the follow-up.", "Validation Error", "Warning");
                return;
            }

            try
            {
                var followup = new CollectionFollowup
                {
                    CustomerId = _customerId,
                    FollowupDate = DateTime.SpecifyKind(NewFollowupDate, DateTimeKind.Utc),
                    Method = NewFollowupMethod,
                    Outcome = NewFollowupOutcome,
                    Notes = NewFollowupNotes ?? string.Empty,
                    NextFollowupDate = DateTime.SpecifyKind(NextFollowupDate, DateTimeKind.Utc),
                    CreatedBy = "System User"
                };

                await _analyticsService.SaveFollowupAsync(followup);
                _messageBoxService.ShowMessage("Follow-up logged successfully.", "Success", "CheckCircleOutline");

                // Reset form
                NewFollowupOutcome = string.Empty;
                NewFollowupNotes = string.Empty;

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error saving follow-up: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        [RelayCommand]
        private async Task CreatePaymentPlan()
        {
            if (string.IsNullOrWhiteSpace(NewPlanName) || PlanTotalAmount <= 0)
            {
                _messageBoxService.ShowMessage("Please enter a plan name and valid total amount.", "Validation Error", "Warning");
                return;
            }

            try
            {
                var plan = new PaymentPlan
                {
                    CustomerId = _customerId,
                    PlanName = NewPlanName,
                    TotalAmount = PlanTotalAmount,
                    NumberOfInstallments = NumberOfInstallments,
                    MonthlyInstallment = MonthlyInstallment,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(NumberOfInstallments),
                    Status = "ACTIVE",
                    Notes = $"Automated plan for {PlanTotalAmount:C}" ?? string.Empty
                };

                await _analyticsService.SavePaymentPlanAsync(plan);
                _messageBoxService.ShowMessage("Payment plan created successfully.", "Success", "CheckCircleOutline");

                // Reset form
                NewPlanName = string.Empty;

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error creating plan: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(NumberOfInstallments) || e.PropertyName == nameof(PlanTotalAmount))
            {
                UpdateMonthlyInstallment();
            }
        }

        private void UpdateMonthlyInstallment()
        {
            if (NumberOfInstallments > 0)
            {
                MonthlyInstallment = Math.Round(PlanTotalAmount / NumberOfInstallments, 2);
            }
        }

        [RelayCommand]
        private void GoBack() => _navigationService.GoBack();
    }
}