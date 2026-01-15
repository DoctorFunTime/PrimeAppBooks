using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.DbServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class AddPurchaseInvoicePageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty] private string _pageTitle = "New Purchase Invoice";
        [ObservableProperty] private string _invoiceNumber;
        [ObservableProperty] private Vendor _selectedVendor;
        [ObservableProperty] private DateTime _invoiceDate = DateTime.Today;
        [ObservableProperty] private DateTime _dueDate = DateTime.Today.AddDays(30);
        [ObservableProperty] private string _notes;
        [ObservableProperty] private decimal _totalAmount;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private int _editingInvoiceId;
        [ObservableProperty] private Currency _selectedCurrency;
        [ObservableProperty] private decimal _exchangeRate = 1.0m;

        public ObservableCollection<Vendor> Vendors { get; } = new();
        public ObservableCollection<ChartOfAccount> Accounts { get; } = new();
        public ObservableCollection<Currency> Currencies { get; } = new();
        public ObservableCollection<InvoiceLineViewModel> BillLines { get; } = new();
        public ObservableCollection<string> ValidationErrors { get; } = new();

        public AddPurchaseInvoicePageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _ = LoadInitialData();
        }

        public void Initialize(int invoiceId)
        {
            if (invoiceId <= 0)
            {
                PageTitle = "New Purchase Invoice";
                IsEditMode = false;
                InitializeNewInvoice();
            }
            else
            {
                PageTitle = "Edit Purchase Invoice";
                IsEditMode = true;
                EditingInvoiceId = invoiceId;
                _ = LoadInvoiceData(invoiceId);
            }
        }

        private void InitializeNewInvoice()
        {
            InvoiceNumber = $"PUR-{DateTime.Now:yyyyMMddHHmmss}";
            BillLines.Clear();
            AddLine();
        }

        private async Task LoadInvoiceData(int id)
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PurchaseServices>();
                var invoice = await service.GetInvoiceByIdAsync(id);

                if (invoice == null)
                {
                    _messageBoxService.ShowMessage("Invoice not found.", "Error", "ErrorOutline");
                    _navigationService.GoBack();
                    return;
                }

                if (invoice.Status == "POSTED")
                {
                    _messageBoxService.ShowMessage("Posted invoices cannot be edited.", "Notice", "Info");
                    _navigationService.GoBack();
                    return;
                }

                InvoiceNumber = invoice.InvoiceNumber;
                InvoiceDate = invoice.InvoiceDate;
                DueDate = invoice.DueDate;
                Notes = invoice.Notes;
                SelectedVendor = Vendors.FirstOrDefault(v => v.VendorId == invoice.VendorId);
                SelectedCurrency = Currencies.FirstOrDefault(c => c.CurrencyId == invoice.CurrencyId);
                ExchangeRate = invoice.ExchangeRate;

                BillLines.Clear();
                foreach (var line in invoice.Lines)
                {
                    var vm = new InvoiceLineViewModel
                    {
                        Description = line.Description,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        SelectedAccount = Accounts.FirstOrDefault(a => a.AccountId == line.AccountId)
                    };
                    vm.PropertyChanged += (s, e) => CalculateTotals();
                    BillLines.Add(vm);
                }
                UpdateLineNumbers();
                CalculateTotals();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error loading invoice: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadInitialData()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var vendors = await context.Vendors.Where(v => v.IsActive).OrderBy(v => v.VendorName).ToListAsync();
                var accounts = await context.ChartOfAccounts.Where(a => a.IsActive).OrderBy(a => a.AccountNumber).ToListAsync();
                var currencies = await context.Currencies.OrderBy(c => c.CurrencyCode).ToListAsync();

                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                var baseCurrencyId = await settingsService.GetBaseCurrencyIdAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Vendors.Clear();
                    foreach (var v in vendors) Vendors.Add(v);

                    Accounts.Clear();
                    foreach (var a in accounts) Accounts.Add(a);

                    Currencies.Clear();
                    foreach (var cur in currencies) Currencies.Add(cur);

                    SelectedCurrency = Currencies.FirstOrDefault(c => c.CurrencyId == baseCurrencyId);
                    ExchangeRate = 1.0m;
                    
                    if (!IsEditMode) InitializeNewInvoice();
                });
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error loading basic data: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedVendorChanged(Vendor value)
        {
            if (value != null && !IsEditMode)
            {
                // Pre-fill default account if available
                if (value.DefaultExpenseAccountId.HasValue)
                {
                    var account = Accounts.FirstOrDefault(a => a.AccountId == value.DefaultExpenseAccountId);
                    if (account != null && BillLines.Count == 1 && BillLines[0].SelectedAccount == null)
                    {
                        BillLines[0].SelectedAccount = account;
                        BillLines[0].Description = $"Services from {value.VendorName}";
                    }
                }
            }
            ValidateInvoice();
        }

        [RelayCommand]
        private void AddLine()
        {
            var newLine = new InvoiceLineViewModel();
            newLine.PropertyChanged += (s, e) => CalculateTotals();
            BillLines.Add(newLine);
            UpdateLineNumbers();
        }

        [RelayCommand]
        private void RemoveLine(InvoiceLineViewModel line)
        {
            if (BillLines.Count > 1)
            {
                BillLines.Remove(line);
                UpdateLineNumbers();
                CalculateTotals();
            }
        }

        private void UpdateLineNumbers()
        {
            for (int i = 0; i < BillLines.Count; i++)
            {
                BillLines[i].LineNumber = i + 1;
            }
        }

        private void CalculateTotals()
        {
            TotalAmount = BillLines.Sum(l => l.Amount);
            ValidateInvoice();
        }

        public bool HasValidationErrors => ValidationErrors.Any();

        private void ValidateInvoice()
        {
            ValidationErrors.Clear();
            if (SelectedVendor == null) ValidationErrors.Add("• Vendor must be selected.");
            if (string.IsNullOrWhiteSpace(InvoiceNumber)) ValidationErrors.Add("• Bill Number is required.");

            int invalidLines = BillLines.Count(l => !l.IsValid);
            if (invalidLines > 0) ValidationErrors.Add($"• {invalidLines} lines are incomplete (Select Account & Quantity).");

            if (TotalAmount <= 0) ValidationErrors.Add("• Total amount must be greater than zero.");

            OnPropertyChanged(nameof(HasValidationErrors));
        }

        [RelayCommand]
        private void NavigateBack() => _navigationService.GoBack();

        [RelayCommand]
        private async Task SaveDraft() => await SaveInternal("DRAFT");

        [RelayCommand]
        private async Task SaveAndPost() => await SaveInternal("POSTED");

        private async Task SaveInternal(string status)
        {
            ValidateInvoice();
            if (HasValidationErrors)
            {
                _messageBoxService.ShowMessage("Please correct the validation errors before saving.", "Wait", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var purchaseService = scope.ServiceProvider.GetRequiredService<PurchaseServices>();

                var invoice = new PurchaseInvoice
                {
                    PurchaseInvoiceId = IsEditMode ? EditingInvoiceId : 0,
                    InvoiceNumber = InvoiceNumber,
                    VendorId = SelectedVendor.VendorId,
                    InvoiceDate = DateTime.SpecifyKind(InvoiceDate, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(DueDate, DateTimeKind.Utc),
                    TotalAmount = TotalAmount,
                    NetAmount = TotalAmount,
                    Balance = TotalAmount,
                    CurrencyId = SelectedCurrency?.CurrencyId,
                    ExchangeRate = ExchangeRate,
                    Status = status,
                    Notes = Notes,
                    CreatedBy = 1, // System User
                    Lines = BillLines.Where(l => l.SelectedAccount != null && l.Amount > 0).Select(l => new PurchaseInvoiceLine
                    {
                        Description = l.Description ?? $"Service from {SelectedVendor.VendorName}",
                        AccountId = l.SelectedAccount.AccountId,
                        Quantity = l.Quantity ?? 0,
                        UnitPrice = l.UnitPrice ?? 0,
                        Amount = l.Amount
                    }).ToList()
                };

                if (IsEditMode)
                    await purchaseService.UpdateInvoiceAsync(invoice);
                else
                    await purchaseService.CreateInvoiceAsync(invoice);

                _messageBoxService.ShowMessage($"Bill {(IsEditMode ? "updated" : "created")} and {(status == "POSTED" ? "posted" : "saved as draft")} successfully!", "Success", "CheckCircle");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error saving: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}