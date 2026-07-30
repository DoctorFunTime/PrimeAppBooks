using PrimeAppBooks.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    public partial class UserManagementPage : BaseAnimatedPage
    {
        private readonly UserManagementPageViewModel _viewModel;

        public UserManagementPage(UserManagementPageViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void TxtCurrentPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox box)
            {
                _viewModel.CurrentPassword = box.Password;
            }
        }

        private void TxtNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox box)
            {
                _viewModel.NewPassword = box.Password;
            }
        }

        private void TxtConfirmNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox box)
            {
                _viewModel.ConfirmNewPassword = box.Password;
            }
        }

        private void TxtRegPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox box)
            {
                _viewModel.RegPassword = box.Password;
            }
        }

        private void TxtRegConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox box)
            {
                _viewModel.RegConfirmPassword = box.Password;
            }
        }
    }
}
