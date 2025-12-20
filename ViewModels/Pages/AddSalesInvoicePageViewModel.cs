using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
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
    public partial class AddSalesInvoicePageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty]
        private string _invoiceNumber;

        [ObservableProperty]
        private Customer _selectedCustomer;

        [ObservableProperty]
        private DateTime _invoiceDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _dueDate = DateTime.Today.AddDays(30);

        [ObservableProperty]
        private string _notes;

        [ObservableProperty]
        private decimal _totalAmount;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private Currency _selectedCurrency;

        [ObservableProperty]
        private decimal _exchangeRate = 1.0m;

        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<ChartOfAccount> Accounts { get; } = new();
        public ObservableCollection<Currency> Currencies { get; } = new();
        public ObservableCollection<InvoiceLineViewModel> BillLines { get; } = new();
        public ObservableCollection<string> ValidationErrors { get; } = new();

        public AddSalesInvoicePageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            _ = LoadInitialData();

            // Add initial empty line
            AddLine();
        }

        private async Task LoadInitialData()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var customers = await context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
                var accounts = await context.ChartOfAccounts.Where(a => a.IsActive).OrderBy(a => a.AccountNumber).ToListAsync();
                var currencies = await context.Currencies.OrderBy(c => c.CurrencyCode).ToListAsync();
                
                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                var baseCurrencyId = await settingsService.GetBaseCurrencyIdAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Customers.Clear();
                    foreach (var c in customers) Customers.Add(c);

                    Accounts.Clear();
                    foreach (var a in accounts) Accounts.Add(a);

                    Currencies.Clear();
                    foreach (var cur in currencies) Currencies.Add(cur);

                    SelectedCurrency = Currencies.FirstOrDefault(c => c.CurrencyId == baseCurrencyId);
                    ExchangeRate = 1.0m;

                    InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
                });
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error loading data: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
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
            if (SelectedCustomer == null) ValidationErrors.Add("• Customer must be selected.");

            int invalidLines = BillLines.Count(l => !l.IsValid);
            if (invalidLines > 0) ValidationErrors.Add($"• There are {invalidLines} invalid line(s). Check Account and Quantity.");

            // Allow zero amount for drafts, but require positive for posting
            // Actually, keep it more flexible as per user request
            // if (TotalAmount <= 0) ValidationErrors.Add("• Total invoice amount must be greater than zero.");

            OnPropertyChanged(nameof(HasValidationErrors));
        }

        [RelayCommand]
        private void AddCustomer()
        {
            _navigationService.NavigateTo<AddCustomerPage>();
        }

        [RelayCommand]
        private void NavigateBack()
        {
            _navigationService.GoBack();
        }

        [RelayCommand]
        private async Task SaveDraft() => await SaveInternal("DRAFT");

        [RelayCommand]
        private async Task SaveAndPost() => await SaveInternal("POSTED");

        private async Task SaveInternal(string status)
        {
            if (SelectedCustomer == null)
            {
                _messageBoxService.ShowMessage("Please select a customer.", "Validation Error", "Warning");
                return;
            }

            if (!BillLines.Any(l => l.SelectedAccount != null && l.Amount > 0))
            {
                _messageBoxService.ShowMessage("Please add at least one valid line item.", "Validation Error", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var salesService = scope.ServiceProvider.GetRequiredService<SalesServices>();

                var invoice = new SalesInvoice
                {
                    InvoiceNumber = InvoiceNumber,
                    CustomerId = SelectedCustomer.CustomerId,
                    InvoiceDate = DateTime.SpecifyKind(InvoiceDate, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(DueDate, DateTimeKind.Utc),
                    TotalAmount = TotalAmount,
                    NetAmount = TotalAmount,
                    Balance = TotalAmount,
                    CurrencyId = SelectedCurrency?.CurrencyId,
                    ExchangeRate = ExchangeRate,
                    Status = status,
                    Notes = Notes,
                    CreatedBy = 1,
                    Lines = BillLines.Where(l => l.SelectedAccount != null && l.Amount > 0).Select(l => new SalesInvoiceLine
                    {
                        Description = l.Description ?? "No Description",
                        AccountId = l.SelectedAccount.AccountId,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice,
                        Amount = l.Amount
                    }).ToList()
                };

                await salesService.CreateInvoiceAsync(invoice);

                string successMsg = status == "POSTED"
                    ? "Invoice created and posted to journal successfully!"
                    : "Invoice saved as draft successfully!";

                _messageBoxService.ShowMessage(successMsg, "Success", "CheckCircleOutline");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error saving invoice: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}