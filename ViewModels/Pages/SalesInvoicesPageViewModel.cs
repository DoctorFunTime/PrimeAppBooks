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
        private void CreateNewInvoice()
        {
            _navigationService.NavigateTo<AddSalesInvoicePage>();
        }

        [RelayCommand]
        private void AddCustomer()
        {
            _navigationService.NavigateTo<AddCustomerPage>();
        }
    }
}