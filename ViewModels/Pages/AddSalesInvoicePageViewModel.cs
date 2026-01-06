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

        [ObservableProperty]
        private PaymentTerm _selectedPaymentTerm;

        private ChartOfAccount _currentDefaultRevenueAccount;
        private int? _existingInvoiceId;
        private string _existingInvoiceStatus;

        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<ChartOfAccount> Accounts { get; } = new();
        public ObservableCollection<Currency> Currencies { get; } = new();
        public ObservableCollection<PaymentTerm> PaymentTerms { get; } = new(); // New Collection
        public ObservableCollection<InvoiceLineViewModel> BillLines { get; } = new();
        public ObservableCollection<string> ValidationErrors { get; } = new();

        public AddSalesInvoicePageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
        }

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(SelectedPaymentTerm) && SelectedPaymentTerm != null)
            {
                DueDate = InvoiceDate.AddDays(SelectedPaymentTerm.Days);
            }
            else if (e.PropertyName == nameof(InvoiceDate) && SelectedPaymentTerm != null)
            {
                DueDate = InvoiceDate.AddDays(SelectedPaymentTerm.Days);
            }
            else if (e.PropertyName == nameof(SelectedCustomer))
            {
                if (SelectedCustomer?.DefaultRevenueAccountId != null)
                {
                    // Try to match by AccountId first
                    _currentDefaultRevenueAccount = Accounts.FirstOrDefault(a => a.AccountId == SelectedCustomer.DefaultRevenueAccountId);

                    // Fallback: If no match and the value looks like an account number (e.g. 4000), try to match by AccountNumber
                    if (_currentDefaultRevenueAccount == null && SelectedCustomer.DefaultRevenueAccountId > 0)
                    {
                        var accountCodeStr = SelectedCustomer.DefaultRevenueAccountId.ToString();
                        _currentDefaultRevenueAccount = Accounts.FirstOrDefault(a => a.AccountNumber == accountCodeStr);
                    }
                }
                else
                {
                    _currentDefaultRevenueAccount = null;
                }

                // Auto-update any lines that haven't been assigned an account yet
                foreach (var line in BillLines)
                {
                    if (line.SelectedAccount == null && _currentDefaultRevenueAccount != null)
                    {
                        line.SelectedAccount = _currentDefaultRevenueAccount;
                    }
                }
            }
        }

        [RelayCommand]
        private async Task AddPaymentTerm()
        {
            // Use existing Input Box
            string termName = _messageBoxService.ShowInputMessage("Enter name for new term (e.g. 'Net 60')", "Add Payment Term", "ClockOutline");

            if (!string.IsNullOrWhiteSpace(termName))
            {
                string daysStr = _messageBoxService.ShowInputMessage("Enter number of days (e.g. 60)", "Term Days", "CalendarClock");
                if (int.TryParse(daysStr, out int days))
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var newTerm = new PaymentTerm { TermName = termName, Days = days, IsActive = true };
                        context.PaymentTerms.Add(newTerm);
                        await context.SaveChangesAsync();

                        PaymentTerms.Add(newTerm);
                        SelectedPaymentTerm = newTerm;
                    }
                    catch (Exception ex)
                    {
                        _messageBoxService.ShowMessage($"Error saving term: {ex.Message}", "Error", "Error");
                    }
                }
            }
        }

        public void Initialize(int? invoiceId = null)
        {
            _ = LoadInitialData(invoiceId);
        }

        private async Task LoadInitialData(int? invoiceId)
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var customers = await context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
                var accounts = await context.ChartOfAccounts.Where(a => a.IsActive).OrderBy(a => a.AccountNumber).ToListAsync();
                var currencies = await context.Currencies.OrderBy(c => c.CurrencyCode).ToListAsync();
                var paymentTerms = await context.PaymentTerms.Where(t => t.IsActive).OrderBy(t => t.Days).ToListAsync();

                // Ensure default terms exist if empty
                if (!paymentTerms.Any())
                {
                    var defaultTerm = new PaymentTerm { TermName = "Net 30", Days = 30 };
                    context.PaymentTerms.Add(defaultTerm);
                    await context.SaveChangesAsync();
                    paymentTerms.Add(defaultTerm);
                }

                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                var baseCurrencyId = await settingsService.GetBaseCurrencyIdAsync();

                SalesInvoice existingInvoice = null;
                if (invoiceId.HasValue)
                {
                    existingInvoice = await context.SalesInvoices
                        .Include(i => i.Lines)
                        .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId.Value);
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Customers.Clear();
                    foreach (var c in customers) Customers.Add(c);

                    Accounts.Clear();
                    foreach (var a in accounts) Accounts.Add(a);

                    Currencies.Clear();
                    foreach (var cur in currencies) Currencies.Add(cur);

                    PaymentTerms.Clear();
                    foreach (var t in paymentTerms) PaymentTerms.Add(t);

                    if (existingInvoice != null)
                    {
                        // Store existing invoice info
                        _existingInvoiceId = existingInvoice.SalesInvoiceId;
                        _existingInvoiceStatus = existingInvoice.Status;

                        // Block editing of posted invoices
                        if (existingInvoice.Status == "POSTED")
                        {
                            _messageBoxService.ShowMessage("Posted invoices cannot be edited. Only draft invoices can be modified.", "Cannot Edit", "Warning");
                            _navigationService.GoBack();
                            return;
                        }

                        // Load existing
                        InvoiceNumber = existingInvoice.InvoiceNumber;
                        InvoiceDate = existingInvoice.InvoiceDate;
                        DueDate = existingInvoice.DueDate;
                        Notes = existingInvoice.Notes;

                        // Note: Setting SelectedCustomer triggers OnSelectedCustomerChanged, which sets _currentDefaultRevenueAccount
                        SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerId == existingInvoice.CustomerId);

                        SelectedCurrency = Currencies.FirstOrDefault(c => c.CurrencyId == existingInvoice.CurrencyId);
                        ExchangeRate = existingInvoice.ExchangeRate;

                        // Match Term by Name
                        SelectedPaymentTerm = PaymentTerms.FirstOrDefault(t => t.TermName == existingInvoice.Terms)
                                              ?? PaymentTerms.FirstOrDefault(t => t.Days == 30); // Fallback to Net 30

                        BillLines.Clear();
                        foreach (var line in existingInvoice.Lines)
                        {
                            var lineVm = new InvoiceLineViewModel
                            {
                                SelectedAccount = Accounts.FirstOrDefault(a => a.AccountId == line.AccountId),
                                Description = line.Description,
                                Quantity = line.Quantity,
                                UnitPrice = line.UnitPrice,
                                Amount = line.Amount
                            };
                            lineVm.PropertyChanged += (s, e) => CalculateTotals();
                            BillLines.Add(lineVm);
                        }
                    }
                    else
                    {
                        // New invoice - clear tracking fields
                        _existingInvoiceId = null;
                        _existingInvoiceStatus = null;

                        // New Invoice Defaults
                        SelectedCurrency = Currencies.FirstOrDefault(c => c.CurrencyId == baseCurrencyId);
                        ExchangeRate = 1.0m;
                        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
                        SelectedPaymentTerm = PaymentTerms.FirstOrDefault(t => t.Days == 30); // Default to Net 30
                        BillLines.Clear();
                        AddLine();
                    }

                    UpdateLineNumbers();
                    CalculateTotals();
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
            if (_currentDefaultRevenueAccount != null)
            {
                newLine.SelectedAccount = _currentDefaultRevenueAccount;
            }

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
            _navigationService.NavigateTo<AddCustomerPage>(0);
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

            //if (!BillLines.Any(l => l.SelectedAccount != null && !string.IsNullOrWhiteSpace(l.Description)))
            if (BillLines.Any(l => l.SelectedAccount == null && string.IsNullOrWhiteSpace(l.Description)))
            {
                _messageBoxService.ShowMessage("Please add at least one valid line item with a description.", "Validation Error", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var salesService = scope.ServiceProvider.GetRequiredService<SalesServices>();

                if (_existingInvoiceId.HasValue)
                {
                    // Update existing invoice
                    var invoice = new SalesInvoice
                    {
                        SalesInvoiceId = _existingInvoiceId.Value,
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
                        Terms = SelectedPaymentTerm?.TermName ?? "Net 30",
                        Notes = Notes ?? string.Empty,
                        CreatedBy = 1,
                        Lines = BillLines.Where(l => l.SelectedAccount != null && !string.IsNullOrWhiteSpace(l.Description)).Select(l => new SalesInvoiceLine
                        {
                            Description = l.Description ?? "No Description",
                            AccountId = l.SelectedAccount.AccountId,
                            Quantity = l.Quantity ?? 0,
                            UnitPrice = l.UnitPrice ?? 0,
                            Amount = l.Amount
                        }).ToList()
                    };

                    await salesService.UpdateInvoiceAsync(invoice);
                }
                else
                {
                    // Create new invoice
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
                        Terms = SelectedPaymentTerm?.TermName ?? "Net 30",
                        Notes = Notes ?? string.Empty,
                        CreatedBy = 1,
                        Lines = BillLines.Where(l => l.SelectedAccount != null && !string.IsNullOrWhiteSpace(l.Description)).Select(l => new SalesInvoiceLine
                        {
                            Description = l.Description ?? "No Description",
                            AccountId = l.SelectedAccount.AccountId,
                            Quantity = l.Quantity ?? 0,
                            UnitPrice = l.UnitPrice ?? 0,
                            Amount = l.Amount
                        }).ToList()
                    };

                    await salesService.CreateInvoiceAsync(invoice);
                }

                string successMsg = _existingInvoiceId.HasValue
                    ? (status == "POSTED" ? "Invoice updated and posted successfully!" : "Invoice updated successfully!")
                    : (status == "POSTED" ? "Invoice created and posted to journal successfully!" : "Invoice saved as draft successfully!");

                _messageBoxService.ShowMessage(successMsg, "Success", "CheckCircleOutline");
                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine(ex.Message);

                var inner = ex.InnerException;
                while (inner != null)
                {
                    sb.AppendLine($" --> {inner.Message}");
                    inner = inner.InnerException;
                }

                _messageBoxService.ShowMessage($"Error saving invoice: {sb}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}