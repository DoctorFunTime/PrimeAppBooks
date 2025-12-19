using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
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
    public partial class PurchaseInvoicesPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<PurchaseInvoice> Invoices { get; } = new();

        public PurchaseInvoicesPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _ = LoadInvoices();
        }

        [RelayCommand]
        private async Task LoadInvoices()
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

        [RelayCommand]
        private async Task CreateNewInvoice()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var purchaseServices = scope.ServiceProvider.GetRequiredService<PurchaseServices>();

                // Find or create an expense account
                var expenseAccount = await context.ChartOfAccounts
                    .FirstOrDefaultAsync(a => a.AccountType == "EXPENSE");

                if (expenseAccount == null)
                {
                    expenseAccount = new ChartOfAccount
                    {
                        AccountName = "Office Expenses",
                        AccountNumber = "5100",
                        AccountType = "EXPENSE",
                        NormalBalance = "DEBIT",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.ChartOfAccounts.Add(expenseAccount);
                    await context.SaveChangesAsync();
                }

                var testInvoice = new PurchaseInvoice
                {
                    InvoiceNumber = $"PUR-{DateTime.Now:yyyyMMdd-HHmm}",
                    VendorId = 1,
                    InvoiceDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(DateTime.Today.AddDays(30), DateTimeKind.Utc),
                    TotalAmount = 350,
                    NetAmount = 350,
                    Balance = 350,
                    Status = "POSTED",
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Lines = new List<PurchaseInvoiceLine>
                    {
                        new PurchaseInvoiceLine
                        {
                            Description = "Office Supplies",
                            AccountId = expenseAccount.AccountId,
                            Amount = 350
                        }
                    }
                };

                await purchaseServices.CreateInvoiceAsync(testInvoice);
                await LoadInvoices();
                _boxServices.ShowMessage("Test Purchase Invoice created and posted to Journal.\nPayables updated.", "Invoice Created", "Success");
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error creating invoice: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
