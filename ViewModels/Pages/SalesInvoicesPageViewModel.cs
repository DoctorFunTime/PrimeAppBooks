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
    public partial class SalesInvoicesPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _boxServices = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        public ObservableCollection<SalesInvoice> Invoices { get; } = new();

        public SalesInvoicesPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
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
                var service = scope.ServiceProvider.GetRequiredService<SalesServices>();
                
                var list = await service.GetAllInvoicesAsync();
                Invoices.Clear();
                foreach (var item in list)
                    Invoices.Add(item);
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Error loading sales invoices: {ex.Message}", "Error", "ErrorOutline");
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
                var salesServices = scope.ServiceProvider.GetRequiredService<SalesServices>();

                // Find or create a revenue account
                var revenueAccount = await context.ChartOfAccounts
                    .FirstOrDefaultAsync(a => a.AccountType == "REVENUE");

                if (revenueAccount == null)
                {
                    revenueAccount = new ChartOfAccount
                    {
                        AccountName = "Sales Revenue",
                        AccountNumber = "4100",
                        AccountType = "REVENUE",
                        NormalBalance = "CREDIT",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.ChartOfAccounts.Add(revenueAccount);
                    await context.SaveChangesAsync();
                }

                var testInvoice = new SalesInvoice
                {
                    InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd-HHmm}",
                    CustomerId = 1,
                    InvoiceDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc),
                    DueDate = DateTime.SpecifyKind(DateTime.Today.AddDays(30), DateTimeKind.Utc),
                    TotalAmount = 750,
                    NetAmount = 750,
                    Balance = 750,
                    Status = "POSTED",
                    CreatedBy = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Lines = new List<SalesInvoiceLine>
                    {
                        new SalesInvoiceLine
                        {
                            Description = "Software Consultancy",
                            AccountId = revenueAccount.AccountId,
                            Quantity = 1,
                            UnitPrice = 750,
                            Amount = 750
                        }
                    }
                };

                await salesServices.CreateInvoiceAsync(testInvoice);
                await LoadInvoices();
                _boxServices.ShowMessage("Test Sales Invoice created and posted to Journal.\nReceivables updated.", "Invoice Created", "Success");
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
