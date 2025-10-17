using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PrimeAppBooks.ViewModels.Windows
{
    public partial class WndMessageBoxViewModel : ObservableObject
    {
        private Brush _iconColor = Brushes.White;

        public Brush IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        private string _message = string.Empty;

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private string _title = string.Empty;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string _iconType = string.Empty;

        public string IconType
        {
            get => _iconType;
            set => SetProperty(ref _iconType, value);
        }

        public WndMessageBoxViewModel(string message, string title, string iconType)
        {
            Message = message;
            Title = title;
            IconType = iconType;

            IconColorInitialization();
        }

        private void IconColorInitialization()
        {
            switch (IconType)
            {
                case "ErrorOutline":
                    IconColor = Brushes.IndianRed;
                    break;

                case "CheckCircleOutline":
                    IconColor = Brushes.LightSeaGreen;
                    break;

                case "InfoOutline":
                    IconColor = Brushes.DeepSkyBlue;
                    break;
            }
        }

        [RelayCommand]
        private void Ok(WndMessageBox mnWnd)
        {
            mnWnd.Close();
        }

        [RelayCommand]
        private void Close(WndMessageBox mnWnd)
        {
            mnWnd.Close();
        }

        [RelayCommand]
        private void Minimize(WndMessageBox mnWnd)
        {
            mnWnd.WindowState = System.Windows.WindowState.Minimized;
        }

        [RelayCommand]
        private void Maximize(WndMessageBox mnWnd)
        {
            mnWnd.WindowState = System.Windows.WindowState.Maximized;
        }

        [RelayCommand]
        private void CopyMessage()
        {
            try
            {
                Clipboard.SetText(Message);
            }
            catch (Exception ex)
            {
                // Handle clipboard access issues gracefully
                System.Diagnostics.Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
            }
        }
    }
}