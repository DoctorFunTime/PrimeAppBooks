using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class BadDebtsPageViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;
        private readonly BoxServices _messageBoxService = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private CustomerBalanceModel _selectedCustomer;

        [ObservableProperty]
        private decimal _writeOffAmount;

        [ObservableProperty]
        private bool _markAsInactive = true;

        [ObservableProperty]
        private string _writeOffNotes;

        public ObservableCollection<CustomerBalanceModel> CustomersWithBalances { get; } = new();

        public BadDebtsPageViewModel(IServiceProvider serviceProvider, INavigationService navigationService)
        {
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;
            _ = LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Get Receivables Account(s) - Typically 1100
                var arAccountIds = await context.ChartOfAccounts
                    .Where(a => a.AccountSubtype == "CURRENT_ASSET" && a.AccountName.Contains("Receivable"))
                    .Select(a => a.AccountId)
                    .ToListAsync();

                // Get all active customers
                var customers = await context.Customers
                    .Where(c => c.IsActive)
                    .ToListAsync();

                var customerBalances = new System.Collections.Generic.List<CustomerBalanceModel>();

                foreach (var customer in customers)
                {
                    // Calculate Balance: Sum(Debts) - Sum(Credits) for this customer in AR accounts
                    var balance = await context.JournalLines
                        .Where(l => arAccountIds.Contains(l.AccountId) && 
                                    l.ContactType == "Customer" && 
                                    l.ContactId == customer.CustomerId)
                        .SumAsync(l => l.DebitAmount - l.CreditAmount);

                    if (balance > 0)
                    {
                        customerBalances.Add(new CustomerBalanceModel
                        {
                            Customer = customer,
                            Balance = balance
                        });
                    }
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CustomersWithBalances.Clear();
                    foreach (var cb in customerBalances.OrderByDescending(c => c.Balance))
                    {
                        CustomersWithBalances.Add(cb);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading bad debts data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSelectedCustomerChanged(CustomerBalanceModel value)
        {
            if (value != null)
            {
                WriteOffAmount = value.Balance;
                WriteOffNotes = $"Write off bad debt for {value.Customer.CustomerName}";
            }
        }

        [RelayCommand]
        private async Task ProcessWriteOff()
        {
            if (SelectedCustomer == null) return;
            if (WriteOffAmount <= 0)
            {
                _messageBoxService.ShowMessage("Write-off amount must be greater than zero.", "Invalid Amount", "Warning");
                return;
            }
            if (WriteOffAmount > SelectedCustomer.Balance)
            {
                _messageBoxService.ShowMessage("Write-off amount cannot exceed the current balance.", "Invalid Amount", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var journalServices = scope.ServiceProvider.GetRequiredService<PrimeAppBooks.Services.DbServices.JournalServices>();

                // 1. Get Accounts
                var badDebtsAccount = await context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountName == "Bad Debts Expense");
                var arAccount = await context.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountNumber == "1100"); // Default AR

                if (badDebtsAccount == null || arAccount == null)
                {
                    _messageBoxService.ShowMessage("Required accounts (Bad Debts Expense or AR) not found.", "Configuration Error", "Error");
                    return;
                }

                // 2. Create Write-off Journal via Service
                await journalServices.CreateBadDebtWriteOffJournalAsync(
                    SelectedCustomer.Customer.CustomerId,
                    WriteOffAmount,
                    WriteOffNotes,
                    arAccount.AccountId,
                    badDebtsAccount.AccountId,
                    SelectedCustomer.Customer.CustomerCode
                );

                // 3. Mark Customer as Inactive
                if (MarkAsInactive)
                {
                    var customerToUpdate = await context.Customers.FindAsync(SelectedCustomer.Customer.CustomerId);
                    if (customerToUpdate != null)
                    {
                        customerToUpdate.IsActive = false;
                        context.Customers.Update(customerToUpdate);
                        await context.SaveChangesAsync();
                    }
                }

                _messageBoxService.ShowMessage("Write-off processed successfully.", "Success", "CheckCircleOutline");

                // Refresh Data
                SelectedCustomer = null;
                await LoadData();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error processing write-off: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class CustomerBalanceModel
    {
        public Customer Customer { get; set; }
        public decimal Balance { get; set; }
    }
}
