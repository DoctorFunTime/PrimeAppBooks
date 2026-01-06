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

        private string _searchText = string.Empty;
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

        private string _searchStudentId = string.Empty;
        public string SearchStudentId
        {
            get => _searchStudentId;
            set
            {
                if (SetProperty(ref _searchStudentId, value))
                {
                    _ = LoadCustomers();
                }
            }
        }

        private string _selectedGrade;
        public string SelectedGrade
        {
            get => _selectedGrade;
            set
            {
                if (SetProperty(ref _selectedGrade, value))
                {
                    _ = LoadCustomers();
                }
            }
        }

        public ObservableCollection<string> GradeLevels { get; } = new()
        {
            "All Grades", "Pre-K", "Kindergarten",
            "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6",
            "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12", "Form 1",
            "Form 2", "Form 3", "Form 4", "Form 5", "Form 6",
            "Undergraduate", "Graduate", "Postgraduate"
        };

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

        private Customer _selectedCustomer;
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }

        public ObservableCollection<Customer> Customers { get; } = new();

        public CustomersPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _selectedGrade = "All Grades";

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

                // Name/Code Filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    query = query.Where(c => c.CustomerName.ToLower().Contains(search) ||
                                           (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(search)));
                }

                // Student ID Filter
                if (!string.IsNullOrWhiteSpace(SearchStudentId))
                {
                    var searchId = SearchStudentId.ToLower();
                    query = query.Where(c => c.StudentId != null && c.StudentId.ToLower().Contains(searchId));
                }

                // Grade Level Filter
                if (!string.IsNullOrEmpty(SelectedGrade) && SelectedGrade != "All Grades")
                {
                    query = query.Where(c => c.GradeLevel == SelectedGrade);
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
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SearchStudentId = string.Empty;
            SelectedGrade = "All Grades";
            _ = LoadCustomers();
        }

        [RelayCommand]
        private void NavigateToAnalytics()
        {
            _navigationService.NavigateTo<CustomerAnalyticsPage>();
        }

        [RelayCommand]
        private void AddCustomer()
        {
            _navigationService.NavigateTo<AddCustomerPage>(0);
        }

        [RelayCommand]
        private void EditCustomer(Customer customer)
        {
            _navigationService.NavigateTo<AddCustomerPage>(customer.CustomerId);
        }
    }
}