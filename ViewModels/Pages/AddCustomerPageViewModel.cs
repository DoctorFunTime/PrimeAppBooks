using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class AddCustomerPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly BoxServices _messageBoxService = new();

        private string _customerName;
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        private string _customerCode;
        public string CustomerCode
        {
            get => _customerCode;
            set => SetProperty(ref _customerCode, value);
        }

        private string _contactPerson;
        public string ContactPerson
        {
            get => _contactPerson;
            set => SetProperty(ref _contactPerson, value);
        }

        private string _email;
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        private string _billingAddress;
        public string BillingAddress
        {
            get => _billingAddress;
            set => SetProperty(ref _billingAddress, value);
        }

        private string _shippingAddress;
        public string ShippingAddress
        {
            get => _shippingAddress;
            set => SetProperty(ref _shippingAddress, value);
        }

        private string _taxId;
        public string TaxId
        {
            get => _taxId;
            set => SetProperty(ref _taxId, value);
        }

        private string _notes;
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        private bool _isAutoInvoiceEnabled;
        public bool IsAutoInvoiceEnabled
        {
            get => _isAutoInvoiceEnabled;
            set => SetProperty(ref _isAutoInvoiceEnabled, value);
        }

        private string _selectedFrequency;
        public string SelectedFrequency
        {
            get => _selectedFrequency;
            set => SetProperty(ref _selectedFrequency, value);
        }

        private int _interval = 1;
        public int Interval
        {
            get => _interval;
            set => SetProperty(ref _interval, value);
        }

        private DateTime _nextInvoiceDate = DateTime.Today.AddMonths(1);
        public DateTime NextInvoiceDate
        {
            get => _nextInvoiceDate;
            set => SetProperty(ref _nextInvoiceDate, value);
        }

        private string _pageTitle = "Customer Registration";
        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private int? _editingCustomerId;
        public int? EditingCustomerId
        {
            get => _editingCustomerId;
            set => SetProperty(ref _editingCustomerId, value);
        }

        private decimal _autoInvoiceAmount;
        public decimal AutoInvoiceAmount
        {
            get => _autoInvoiceAmount;
            set => SetProperty(ref _autoInvoiceAmount, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Student/Detailed Properties
        private DateTime? _dateOfBirth;
        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set => SetProperty(ref _dateOfBirth, value);
        }

        private string _gender;
        public string Gender
        {
            get => _gender;
            set => SetProperty(ref _gender, value);
        }

        private string _studentId;
        public string StudentId
        {
            get => _studentId;
            set => SetProperty(ref _studentId, value);
        }

        private string _gradeLevel;
        public string GradeLevel
        {
            get => _gradeLevel;
            set => SetProperty(ref _gradeLevel, value);
        }

        private string _guardianName;
        public string GuardianName
        {
            get => _guardianName;
            set => SetProperty(ref _guardianName, value);
        }

        private string _guardianPhone;
        public string GuardianPhone
        {
            get => _guardianPhone;
            set => SetProperty(ref _guardianPhone, value);
        }

        private string _guardianEmail;
        public string GuardianEmail
        {
            get => _guardianEmail;
            set => SetProperty(ref _guardianEmail, value);
        }

        private string _nationality;
        public string Nationality
        {
            get => _nationality;
            set => SetProperty(ref _nationality, value);
        }

        private string _nationalId;
        public string NationalId
        {
            get => _nationalId;
            set => SetProperty(ref _nationalId, value);
        }

        private ChartOfAccount _selectedRevenueAccount;
        public ChartOfAccount SelectedRevenueAccount
        {
            get => _selectedRevenueAccount;
            set => SetProperty(ref _selectedRevenueAccount, value);
        }

        public ObservableCollection<ChartOfAccount> RevenueAccounts { get; } = new();

        public ObservableCollection<string> Frequencies { get; } = new() { "Weekly", "Monthly", "Quarterly", "Yearly" };

        public AddCustomerPageViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            SelectedFrequency = "Monthly";
            _ = LoadAccounts();
        }

        private async Task LoadAccounts()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var accounts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(context.ChartOfAccounts);
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    RevenueAccounts.Clear();
                    foreach (var account in accounts) RevenueAccounts.Add(account);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading accounts: {ex.Message}");
            }
        }

        [RelayCommand]
        private void NavigateBack() => _navigationService.GoBack();

        [RelayCommand]
        private async Task SaveCustomer()
        {
            if (string.IsNullOrWhiteSpace(CustomerName))
            {
                _messageBoxService.ShowMessage("Please enter a customer name.", "Validation Error", "Warning");
                return;
            }

            try
            {
                IsLoading = true;
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (IsEditMode && EditingCustomerId.HasValue)
                {
                    var customer = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Customers, c => c.CustomerId == EditingCustomerId.Value);
                    if (customer == null)
                    {
                        _messageBoxService.ShowMessage("Customer not found.", "Error", "ErrorOutline");
                        _navigationService.GoBack();
                        return;
                    }

                    // Update existing customer
                    customer.CustomerName = CustomerName;
                    customer.CustomerCode = CustomerCode;
                    customer.ContactPerson = ContactPerson;
                    customer.Email = Email;
                    customer.Phone = Phone;
                    customer.TaxId = TaxId;
                    customer.BillingAddress = BillingAddress;
                    customer.ShippingAddress = ShippingAddress;
                    customer.DefaultRevenueAccountId = SelectedRevenueAccount?.AccountId;
                    customer.Notes = Notes;
                    customer.IsAutoInvoiceEnabled = IsAutoInvoiceEnabled;
                    customer.AutoInvoiceFrequency = IsAutoInvoiceEnabled ? SelectedFrequency : null;
                    customer.AutoInvoiceInterval = IsAutoInvoiceEnabled ? Interval : 1;
                    customer.AutoInvoiceAmount = IsAutoInvoiceEnabled ? AutoInvoiceAmount : 0;
                    customer.NextAutoInvoiceDate = IsAutoInvoiceEnabled ? DateTime.SpecifyKind(NextInvoiceDate, DateTimeKind.Utc) : null;
                    
                    // Update Detailed Info
                    customer.DateOfBirth = DateOfBirth.HasValue ? DateTime.SpecifyKind(DateOfBirth.Value, DateTimeKind.Utc) : null;
                    customer.Gender = Gender;
                    customer.StudentId = StudentId;
                    customer.GradeLevel = GradeLevel;
                    customer.GuardianName = GuardianName;
                    customer.GuardianPhone = GuardianPhone;
                    customer.GuardianEmail = GuardianEmail;
                    customer.Nationality = Nationality;
                    customer.NationalId = NationalId;
                    
                    customer.UpdatedAt = DateTime.UtcNow;

                    context.Customers.Update(customer);
                    await context.SaveChangesAsync();
                    _messageBoxService.ShowMessage("Customer updated successfully!", "Success", "CheckCircleOutline");
                }
                else
                {
                    // Create new customer
                    var customer = new Customer
                    {
                        CustomerName = CustomerName,
                        CustomerCode = CustomerCode,
                        ContactPerson = ContactPerson,
                        Email = Email,
                        Phone = Phone,
                        BillingAddress = BillingAddress,
                        ShippingAddress = ShippingAddress,
                        TaxId = TaxId,
                        Notes = Notes,
                        DefaultRevenueAccountId = SelectedRevenueAccount?.AccountId,
                        IsAutoInvoiceEnabled = IsAutoInvoiceEnabled,
                        AutoInvoiceFrequency = IsAutoInvoiceEnabled ? SelectedFrequency : null,
                        AutoInvoiceInterval = IsAutoInvoiceEnabled ? Interval : 1,
                        AutoInvoiceAmount = IsAutoInvoiceEnabled ? AutoInvoiceAmount : 0,
                        NextAutoInvoiceDate = IsAutoInvoiceEnabled ? DateTime.SpecifyKind(NextInvoiceDate, DateTimeKind.Utc) : null,
                        
                        // New Detailed Info
                        DateOfBirth = DateOfBirth.HasValue ? DateTime.SpecifyKind(DateOfBirth.Value, DateTimeKind.Utc) : null,
                        Gender = Gender,
                        StudentId = StudentId,
                        GradeLevel = GradeLevel,
                        GuardianName = GuardianName,
                        GuardianPhone = GuardianPhone,
                        GuardianEmail = GuardianEmail,
                        Nationality = Nationality,
                        NationalId = NationalId,
                        
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Customers.Add(customer);
                    await context.SaveChangesAsync();
                    _messageBoxService.ShowMessage("Customer registered successfully!", "Success", "CheckCircleOutline");
                }

                _navigationService.GoBack();
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage($"Error saving customer: {ex.Message}", "Error", "ErrorOutline");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void Initialize(int customerId)
        {
            IsEditMode = true;
            EditingCustomerId = customerId;
            PageTitle = "Edit Customer Details";
            _ = LoadCustomerData(customerId);
        }

        private async Task LoadCustomerData(int id)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var customer = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.Customers, c => c.CustomerId == id);

                if (customer != null)
                {
                    CustomerName = customer.CustomerName;
                    CustomerCode = customer.CustomerCode;
                    ContactPerson = customer.ContactPerson;
                    Email = customer.Email;
                    Phone = customer.Phone;
                    TaxId = customer.TaxId;
                    BillingAddress = customer.BillingAddress;
                    ShippingAddress = customer.ShippingAddress;
                    Notes = customer.Notes;
                    IsAutoInvoiceEnabled = customer.IsAutoInvoiceEnabled;
                    SelectedFrequency = customer.AutoInvoiceFrequency ?? "Monthly";
                    Interval = customer.AutoInvoiceInterval;
                    AutoInvoiceAmount = customer.AutoInvoiceAmount;
                    if (customer.NextAutoInvoiceDate.HasValue)
                        NextInvoiceDate = customer.NextAutoInvoiceDate.Value;

                    DateOfBirth = customer.DateOfBirth;
                    Gender = customer.Gender;
                    StudentId = customer.StudentId;
                    GradeLevel = customer.GradeLevel;
                    GuardianName = customer.GuardianName;
                    GuardianPhone = customer.GuardianPhone;
                    GuardianEmail = customer.GuardianEmail;
                    Nationality = customer.Nationality;
                    NationalId = customer.NationalId;

                    // Ensure accounts are loaded and match the selection
                    if (RevenueAccounts.Count == 0) await LoadAccounts();
                    SelectedRevenueAccount = RevenueAccounts.FirstOrDefault(a => a.AccountId == customer.DefaultRevenueAccountId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading customer data: {ex.Message}");
            }
        }
    }
}
