using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using PrimeAppBooks.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class UserManagementPageViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private readonly BoxServices _boxServices = new();

        // --- Current User Profile Properties ---
        [ObservableProperty]
        private string _myUsername = string.Empty;

        [ObservableProperty]
        private string _myFirstName = string.Empty;

        [ObservableProperty]
        private string _myLastName = string.Empty;

        [ObservableProperty]
        private string _myTitle = string.Empty;

        [ObservableProperty]
        private string _myDepartment = string.Empty;

        [ObservableProperty]
        private string _myRole = string.Empty;

        [ObservableProperty]
        private string _currentPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmNewPassword = string.Empty;

        // --- New User Registration Properties ---
        [ObservableProperty]
        private string _regUsername = string.Empty;

        [ObservableProperty]
        private string _regFirstName = string.Empty;

        [ObservableProperty]
        private string _regLastName = string.Empty;

        [ObservableProperty]
        private string _regTitle = string.Empty;

        [ObservableProperty]
        private string _regDepartment = string.Empty;

        [ObservableProperty]
        private string _regPassword = string.Empty;

        [ObservableProperty]
        private string _regConfirmPassword = string.Empty;

        [ObservableProperty]
        private string _selectedRole = "Client";

        public ObservableCollection<string> AvailableRoles { get; } = new();

        public ObservableCollection<User> UserDirectory { get; } = new();

        public bool IsCurrentAdmin => string.Equals(MyAppContext.CurrentLogin?.AccountType, "Admin", StringComparison.OrdinalIgnoreCase);

        public UserManagementPageViewModel(AppDbContext context)
        {
            _context = context;
            LoadCurrentUserData();
            ConfigureAvailableRoles();
            _ = LoadUsersAsync();

            MyAppContext.StaticPropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MyAppContext.CurrentLogin))
                {
                    LoadCurrentUserData();
                    ConfigureAvailableRoles();
                    OnPropertyChanged(nameof(IsCurrentAdmin));
                }
            };
        }

        private void LoadCurrentUserData()
        {
            var user = MyAppContext.CurrentLogin;
            if (user != null)
            {
                MyUsername = user.Username;
                MyFirstName = user.AccountName;
                MyLastName = user.AccountSurname;
                MyTitle = user.AccountTitle;
                MyDepartment = user.AccountDepartment;
                MyRole = user.AccountType;
            }
        }

        private void ConfigureAvailableRoles()
        {
            AvailableRoles.Clear();
            AvailableRoles.Add("Client");
            AvailableRoles.Add("Guest");

            if (IsCurrentAdmin)
            {
                AvailableRoles.Add("Admin");
            }

            if (!AvailableRoles.Contains(SelectedRole))
            {
                SelectedRole = "Client";
            }
        }

        [RelayCommand]
        public async Task LoadUsersAsync()
        {
            try
            {
                var users = await _context.Users.AsNoTracking().ToListAsync();
                UserDirectory.Clear();
                foreach (var u in users)
                {
                    UserDirectory.Add(u);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task SaveMyProfileAsync()
        {
            var currentLogin = MyAppContext.CurrentLogin;
            if (currentLogin == null)
            {
                _boxServices.ShowMessage("No logged in user found.", "Error", "ErrorOutline");
                return;
            }

            try
            {
                var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentLogin.UserId);
                if (dbUser == null)
                {
                    _boxServices.ShowMessage("User record not found in database.", "Error", "ErrorOutline");
                    return;
                }

                // Update profile details
                dbUser.AccountName = MyFirstName.Trim();
                dbUser.AccountSurname = MyLastName.Trim();
                dbUser.AccountTitle = MyTitle.Trim();
                dbUser.AccountDepartment = MyDepartment.Trim();

                // If password change attempted
                if (!string.IsNullOrWhiteSpace(NewPassword))
                {
                    if (string.IsNullOrWhiteSpace(CurrentPassword))
                    {
                        _boxServices.ShowMessage("Please enter your current password to set a new password.", "Validation Error", "WarningOutline");
                        return;
                    }

                    if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, dbUser.PasswordHash))
                    {
                        _boxServices.ShowMessage("Current password is incorrect.", "Validation Error", "ErrorOutline");
                        return;
                    }

                    if (NewPassword != ConfirmNewPassword)
                    {
                        _boxServices.ShowMessage("New passwords do not match.", "Validation Error", "WarningOutline");
                        return;
                    }

                    dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                }

                await _context.SaveChangesAsync();

                // Refresh MyAppContext
                MyAppContext.CurrentLogin = dbUser;
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;

                _boxServices.ShowMessage("Profile updated successfully!", "Success", "CheckCircleOutline");
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Failed to update profile: {ex.Message}", "Error", "ErrorOutline");
            }
        }

        [RelayCommand]
        public async Task RegisterUserAsync()
        {
            if (string.IsNullOrWhiteSpace(RegUsername) || string.IsNullOrWhiteSpace(RegPassword))
            {
                _boxServices.ShowMessage("Username and password are required.", "Validation Error", "WarningOutline");
                return;
            }

            if (RegPassword != RegConfirmPassword)
            {
                _boxServices.ShowMessage("Passwords do not match.", "Validation Error", "WarningOutline");
                return;
            }

            // Role restriction check
            if (string.Equals(SelectedRole, "Admin", StringComparison.OrdinalIgnoreCase) && !IsCurrentAdmin)
            {
                _boxServices.ShowMessage("Only an Admin account can create another Admin account.", "Permission Denied", "ErrorOutline");
                return;
            }

            try
            {
                bool exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == RegUsername.Trim().ToLower());
                if (exists)
                {
                    _boxServices.ShowMessage("A user with this username already exists.", "Validation Error", "WarningOutline");
                    return;
                }

                var newUser = new User
                {
                    Username = RegUsername.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(RegPassword),
                    AccountName = RegFirstName.Trim(),
                    AccountSurname = RegLastName.Trim(),
                    AccountTitle = RegTitle.Trim(),
                    AccountDepartment = RegDepartment.Trim(),
                    AccountType = SelectedRole,
                    AccountTasks = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _boxServices.ShowMessage($"User '{newUser.Username}' registered successfully as {newUser.AccountType}!", "User Registered", "CheckCircleOutline");

                // Clear registration fields
                RegUsername = string.Empty;
                RegFirstName = string.Empty;
                RegLastName = string.Empty;
                RegTitle = string.Empty;
                RegDepartment = string.Empty;
                RegPassword = string.Empty;
                RegConfirmPassword = string.Empty;
                SelectedRole = "Client";

                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                _boxServices.ShowMessage($"Failed to register user: {ex.Message}", "Error", "ErrorOutline");
            }
        }
    }
}
