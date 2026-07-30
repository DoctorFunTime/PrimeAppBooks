using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Repositories;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Windows;
using PrimeAppBooks.Views.Windows;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PrimeAppBooks.Views
{
    public partial class Wndlogin : Window
    {
        private readonly LoginRepository _loginRepository = new();

        public Wndlogin()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnLogin.IsEnabled = false;

                string username = txtAccountName.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Please enter your username and password.", "Sign in");
                    btnLogin.IsEnabled = true;
                    return;
                }

                var loginDetails = await Task.Run(() => _loginRepository.GetLoginDetails(username));

                if (loginDetails == null)
                {
                    MessageBox.Show("The username you entered is incorrect.", "Sign in");
                    txtPassword.Clear();
                    btnLogin.IsEnabled = true;
                    return;
                }

                bool isPasswordValid = await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, loginDetails.PasswordHash));
                if (!isPasswordValid)
                {
                    MessageBox.Show("The password you entered is incorrect.", "Sign in");
                    txtPassword.Clear();
                    btnLogin.IsEnabled = true;
                    return;
                }

                MyAppContext.CurrentLogin = loginDetails;

                var main = App.ServiceProvider.GetRequiredService<MainWindow>();
                main.DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
                Application.Current.MainWindow = main;
                main.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("We couldn't sign you in due to an unexpected error.\n\n" + ex.Message, "Sign in");
                btnLogin.IsEnabled = true;
            }
        }
    }
}