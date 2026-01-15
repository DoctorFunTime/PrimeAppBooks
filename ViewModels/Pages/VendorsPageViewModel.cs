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
using System.Threading.Tasks;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class VendorsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showInactive;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isEmpty;

        [ObservableProperty]
        private Vendor _selectedVendor;

        public ObservableCollection<Vendor> Vendors { get; } = new();

        public VendorsPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            
            _ = LoadVendors();
        }

        partial void OnSearchTextChanged(string value) => _ = LoadVendors();
        partial void OnShowInactiveChanged(bool value) => _ = LoadVendors();

        [RelayCommand]
        private async Task LoadVendors()
        {
            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var query = context.Vendors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    query = query.Where(v => v.VendorName.ToLower().Contains(search) ||
                                           (v.VendorCode != null && v.VendorCode.ToLower().Contains(search)));
                }

                if (!ShowInactive)
                {
                    query = query.Where(v => v.IsActive);
                }

                var list = await query.OrderBy(v => v.VendorName).ToListAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Vendors.Clear();
                    foreach (var v in list) Vendors.Add(v);
                    IsEmpty = !Vendors.Any();
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
            ShowInactive = false;
        }

        [RelayCommand]
        private void AddVendor()
        {
            _navigationService.NavigateTo<AddVendorPage>(0);
        }

        [RelayCommand]
        private void EditVendor(Vendor vendor)
        {
            if (vendor != null)
            {
                _navigationService.NavigateTo<AddVendorPage>(vendor.VendorId);
            }
        }
    }
}
