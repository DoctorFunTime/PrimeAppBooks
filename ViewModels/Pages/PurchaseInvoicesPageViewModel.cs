using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class PurchaseInvoicesPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();

        public ObservableCollection<PurchaseInvoice> Invoices { get; } = new();
        public ObservableCollection<PurchaseInvoice> FilteredInvoices { get; } = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private PurchaseInvoice _selectedInvoice;

        // Statistics
        [ObservableProperty] private int _draftCount;
        [ObservableProperty] private int _postedThisWeekCount;
        [ObservableProperty] private int _overdueCount;
        [ObservableProperty] private decimal _totalPurchasesThisMonth;
        [ObservableProperty] private string _resultsSummary = "No invoices found";

        // Filters
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value)) ApplyFilters();
            }
        }

        private DateTime? _startDate;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value)) ApplyFilters();
            }
        }

        private DateTime? _endDate;
        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value)) ApplyFilters();
            }
        }

        private bool _showAllInvoices = true;
        public bool ShowAllInvoices
        {
            get => _showAllInvoices;
            set
            {
                if (SetProperty(ref _showAllInvoices, value) && value)
                {
                    _showDraftsOnly = false;
                    OnPropertyChanged(nameof(ShowDraftsOnly));
                    _showPostedOnly = false;
                    OnPropertyChanged(nameof(ShowPostedOnly));
                    ApplyFilters();
                }
            }
        }

        private bool _showDraftsOnly;
        public bool ShowDraftsOnly
        {
            get => _showDraftsOnly;
            set
            {
                if (SetProperty(ref _showDraftsOnly, value) && value)
                {
                    _showAllInvoices = false;
                    OnPropertyChanged(nameof(ShowAllInvoices));
                    _showPostedOnly = false;
                    OnPropertyChanged(nameof(ShowPostedOnly));
                    ApplyFilters();
                }
            }
        }

        private bool _showPostedOnly;
        public bool ShowPostedOnly
        {
            get => _showPostedOnly;
            set
            {
                if (SetProperty(ref _showPostedOnly, value) && value)
                {
                    _showAllInvoices = false;
                    OnPropertyChanged(nameof(ShowAllInvoices));
                    _showDraftsOnly = false;
                    OnPropertyChanged(nameof(ShowDraftsOnly));
                    ApplyFilters();
                }
            }
        }

        public PurchaseInvoicesPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _navigationService.PageNavigated += OnPageNavigated;
            _ = LoadInvoices();
        }

        private async void OnPageNavigated(object sender, System.Windows.Controls.Page page)
        {
            if (page is PurchaseInvoicesPage)
            {
                await LoadInvoices();
            }
        }

        [RelayCommand]
        public async Task LoadInvoices()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PurchaseServices>();

                var list = await service.GetAllInvoicesAsync();
                Invoices.Clear();
                foreach (var item in list)
                    Invoices.Add(item);

                UpdateStatistics();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading purchase invoices: {ex.Message}", "Error", "ErrorOutline");
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
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            DraftCount = Invoices.Count(i => i.Status == "DRAFT");
            PostedThisWeekCount = Invoices.Count(i => i.Status == "POSTED" && i.UpdatedAt >= weekStart);
            OverdueCount = Invoices.Count(i => i.Status == "POSTED" && i.DueDate < today);

            TotalPurchasesThisMonth = Invoices
                .Where(i => i.InvoiceDate >= monthStart && i.Status == "POSTED")
                .Sum(i => i.TotalAmount);
        }

        private void ApplyFilters()
        {
            var filtered = Invoices.AsEnumerable();

            if (ShowDraftsOnly) filtered = filtered.Where(i => i.Status == "DRAFT");
            else if (ShowPostedOnly) filtered = filtered.Where(i => i.Status == "POSTED");

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(i =>
                    (i.InvoiceNumber != null && i.InvoiceNumber.ToLower().Contains(search)) ||
                    (i.Vendor != null && i.Vendor.VendorName != null && i.Vendor.VendorName.ToLower().Contains(search)));
            }

            if (StartDate.HasValue) filtered = filtered.Where(i => i.InvoiceDate >= StartDate.Value);
            if (EndDate.HasValue) filtered = filtered.Where(i => i.InvoiceDate <= EndDate.Value);

            FilteredInvoices.Clear();
            foreach (var item in filtered.OrderByDescending(i => i.InvoiceDate))
            {
                FilteredInvoices.Add(item);
            }

            ResultsSummary = $"{FilteredInvoices.Count} invoices found";
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            StartDate = null;
            EndDate = null;
            ShowAllInvoices = true;
            ApplyFilters();
        }

        [RelayCommand]
        private void CreateNewInvoice()
        {
            _navigationService.NavigateTo<AddPurchaseInvoicePage>();
        }

        [RelayCommand]
        private void AddVendor()
        {
            _navigationService.NavigateTo<AddVendorPage>(0);
        }

        [RelayCommand]
        private async Task PostInvoice(PurchaseInvoice invoice)
        {
            if (invoice == null || invoice.Status != "DRAFT") return;

            var confirmed = _boxServices.ShowConfirmation(
                $"Are you sure you want to post Purchase Invoice {invoice.InvoiceNumber}?\nThis will create journal entries and cannot be easily undone.",
                "Confirm Post",
                "QuestionAnswer");

            if (!confirmed) return;

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PurchaseServices>();

                var success = await service.PostInvoiceAsync(invoice.PurchaseInvoiceId);

                if (success)
                {
                    _boxServices.ShowMessage("Invoice posted successfully!", "Success", "CheckCircle");
                    await LoadInvoices();
                    SelectedInvoice = null;
                }
                else
                {
                    _boxServices.ShowMessage("Failed to post invoice.", "Error", "ErrorOutline");
                }
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error posting invoice: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task DeleteInvoice(PurchaseInvoice invoice)
        {
            if (invoice == null || invoice.Status == "POSTED")
            {
                _boxServices.ShowMessage("Cannot delete a posted invoice.", "Warning", "Warning");
                return;
            }

            var confirmed = _boxServices.ShowConfirmation(
                $"Are you sure you want to delete Invoice {invoice.InvoiceNumber}?",
                "Confirm Delete",
                "Delete");

            if (!confirmed) return;

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PurchaseServices>();

                var success = await service.DeleteInvoiceAsync(invoice.PurchaseInvoiceId);

                if (success)
                {
                    _boxServices.ShowMessage("Invoice deleted successfully.", "Success", "Delete");
                    await LoadInvoices();
                    SelectedInvoice = null;
                }
                else
                {
                    _boxServices.ShowMessage("Failed to delete invoice.", "Error", "ErrorOutline");
                }
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error deleting invoice: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void EditInvoice(PurchaseInvoice invoice)
        {
            if (invoice != null)
            {
                _navigationService.NavigateTo<AddPurchaseInvoicePage>(invoice.PurchaseInvoiceId);
            }
        }
    }
}