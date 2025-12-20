using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Data;
using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Models;
using PrimeAppBooks.Views.Pages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class CustomersPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadCustomers();
                }
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }
        public ObservableCollection<Customer> Customers { get; } = new();

        public CustomersPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;

            _ = LoadCustomers();
        }

        private async Task LoadCustomers()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var query = context.Customers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    query = query.Where(c => c.CustomerName.ToLower().Contains(search) ||
                                           (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(search)));
                }

                var list = await query.OrderBy(c => c.CustomerName).ToListAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Customers.Clear();
                    foreach (var c in list) Customers.Add(c);
                    IsEmpty = !Customers.Any();
                });
            }
            catch (Exception)
            {
                // Log error
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddCustomer()
        {
            _navigationService.NavigateTo<AddCustomerPage>();
        }

        [RelayCommand]
        private void EditCustomer(Customer customer)
        {
            _navigationService.NavigateTo<AddCustomerPage>(customer.CustomerId);
        }
    }
}