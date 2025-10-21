using PrimeAppBooks.ViewModels.Windows;
using PrimeAppBooks.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PrimeAppBooks.Services
{
    public class BoxServices
    {
        public void ShowMessage(string message, string title, string iconType)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                WndMessageBox wndMessageBox = new WndMessageBox(message, title, iconType);
                wndMessageBox.ShowDialog();
            });
        }

        public string ShowInputMessage(string message, string title, string iconType)
        {
            string? userInput = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var inputBox = new WndInputBox(message, title, iconType);
                inputBox.ShowDialog();

                // Retrieve the result from the ViewModel
                if (inputBox.DataContext is WndInputBoxViewModel vm && vm.IsConfirmed)
                {
                    userInput = vm.UserInput;
                }
            });

            return userInput;
        }

        public bool ShowConfirmation(string message, string title, string iconType)
        {
            bool result = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var confirmationBox = new WndConfirmationBox(message, title, iconType);
                confirmationBox.ShowDialog();
                result = confirmationBox.Result;
            });

            return result;
        }
    }
}