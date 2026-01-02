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

        // Basic Information
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

        // Auto-Invoice Configuration
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

        private decimal _autoInvoiceAmount;

        public decimal AutoInvoiceAmount
        {
            get => _autoInvoiceAmount;
            set => SetProperty(ref _autoInvoiceAmount, value);
        }

        // Student/Personal Information
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

        // UI State
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

        private bool _isLoading;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Account Selection
        private ChartOfAccount _selectedRevenueAccount;

        public ChartOfAccount SelectedRevenueAccount
        {
            get => _selectedRevenueAccount;
            set => SetProperty(ref _selectedRevenueAccount, value);
        }

        // Collections
        public ObservableCollection<ChartOfAccount> RevenueAccounts { get; } = new();

        public ObservableCollection<string> Frequencies { get; } = new() { "Daily", "Weekly", "Monthly", "Quarterly", "Yearly" };
        public ObservableCollection<string> Genders { get; } = new() { "Male", "Female", "Other", "Prefer not to say" };

        public ObservableCollection<string> GradeLevels { get; } = new()
        {
            "Pre-K", "Kindergarten",
            "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6",
            "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12", "Form 1",
            "Form 2", "Form 3", "Form 4", "Form 5", "Form 6",
            "Undergraduate", "Graduate", "Postgraduate"
        };

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
                var accounts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                    System.Linq.Queryable.Where(context.ChartOfAccounts, a => a.AccountType == "REVENUE"));

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
            // Validate required fields
            if (string.IsNullOrWhiteSpace(CustomerName) || 
                string.IsNullOrWhiteSpace(CustomerCode) ||
                string.IsNullOrWhiteSpace(ContactPerson) || 
                string.IsNullOrWhiteSpace(Phone))
            {
                _messageBoxService.ShowMessage("Please fill in all required fields (marked with *).", "Validation Error", "Warning");
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

                    UpdateCustomerProperties(customer);
                    customer.UpdatedAt = DateTime.UtcNow;

                    context.Customers.Update(customer);
                    await context.SaveChangesAsync();
                    _messageBoxService.ShowMessage("Customer updated successfully!", "Success", "CheckCircleOutline");
                }
                else
                {
                    var customer = new Customer();
                    UpdateCustomerProperties(customer);
                    customer.CreatedAt = DateTime.UtcNow;
                    customer.UpdatedAt = DateTime.UtcNow;

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

        private void UpdateCustomerProperties(Customer customer)
        {
            // Basic Information - Use null-coalescing for non-nullable DB fields that are "optional" in UI
            customer.CustomerName = CustomerName;
            customer.CustomerCode = CustomerCode;
            customer.ContactPerson = ContactPerson;
            customer.Email = Email;
            customer.Phone = Phone;
            
            customer.TaxId = TaxId ?? string.Empty;
            customer.BillingAddress = BillingAddress ?? string.Empty;
            customer.ShippingAddress = ShippingAddress ?? string.Empty;
            customer.Notes = Notes ?? string.Empty;
            
            customer.DefaultRevenueAccountId = SelectedRevenueAccount?.AccountId;

            // Auto-Invoice Configuration
            customer.IsAutoInvoiceEnabled = IsAutoInvoiceEnabled;
            customer.AutoInvoiceFrequency = IsAutoInvoiceEnabled ? SelectedFrequency : null;
            customer.AutoInvoiceInterval = IsAutoInvoiceEnabled ? Interval : 1;
            customer.AutoInvoiceAmount = IsAutoInvoiceEnabled ? AutoInvoiceAmount : 0;
            customer.NextAutoInvoiceDate = IsAutoInvoiceEnabled ? DateTime.SpecifyKind(NextInvoiceDate, DateTimeKind.Utc) : null;

            // Student/Personal Information
            customer.DateOfBirth = DateOfBirth.HasValue ? DateTime.SpecifyKind(DateOfBirth.Value, DateTimeKind.Utc) : null;
            customer.Gender = Gender;
            customer.StudentId = StudentId;
            customer.GradeLevel = GradeLevel;
            customer.GuardianName = GuardianName;
            customer.GuardianPhone = GuardianPhone;
            customer.GuardianEmail = GuardianEmail;
            customer.Nationality = Nationality;
            customer.NationalId = NationalId;
        }

        public void Initialize(int customerId)
        {
            if (customerId <= 0)
            {
                InitializeNewCustomer();
            }
            else
            {
                IsEditMode = true;
                EditingCustomerId = customerId;
                PageTitle = "Edit Customer Details";
                _ = LoadCustomerData(customerId);
            }
        }

        private void InitializeNewCustomer()
        {
            IsEditMode = false;
            EditingCustomerId = null;
            PageTitle = "Customer Registration";
            
            // Generate Code
            GenerateNewCustomerCode();
            
            // Clear or Set Defaults
            CustomerName = string.Empty;
            ContactPerson = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            BillingAddress = string.Empty;
            ShippingAddress = string.Empty;
            TaxId = string.Empty;
            Notes = string.Empty;
            
            IsAutoInvoiceEnabled = false;
            SelectedFrequency = "Monthly";
            Interval = 1;
            AutoInvoiceAmount = 0;
            NextInvoiceDate = DateTime.Today.AddMonths(1);
            
            DateOfBirth = null;
            Gender = null;
            StudentId = string.Empty;
            GradeLevel = null;
            GuardianName = string.Empty;
            GuardianPhone = string.Empty;
            GuardianEmail = string.Empty;
            Nationality = string.Empty;
            NationalId = string.Empty;
            
            SelectedRevenueAccount = null;
        }

        private void GenerateNewCustomerCode()
        {
             // Generate a unique code (e.g., C-yyyyMMdd-XXXX)
             // Using a random part to minimize collision probability without a DB call for Max ID
             // Format: C-231222-1234
             var random = new Random();
             var datePart = DateTime.Now.ToString("yyMMdd");
             var randomPart = random.Next(1000, 9999);
             CustomerCode = $"C-{datePart}-{randomPart}";
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