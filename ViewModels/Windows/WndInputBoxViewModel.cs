using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimeAppBooks.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PrimeAppBooks.ViewModels.Windows
{
    public partial class WndInputBoxViewModel : ObservableObject
    {
        private string _userInput = string.Empty;

        public string UserInput
        {
            get => _userInput;
            set => SetProperty(ref _userInput, value);
        }

        // Add a property to track if the user clicked "OK"
        public bool IsConfirmed { get; private set; }

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

        public WndInputBoxViewModel(string message, string title, string iconType)
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
        private void Ok(WndInputBox mnWnd)
        {
            IsConfirmed = true;
            mnWnd.Close();
        }

        [RelayCommand]
        private void Cancel(WndInputBox mnWnd)
        {
            IsConfirmed = false;
            mnWnd.Close();
        }

        [RelayCommand]
        private void Close(WndInputBox mnWnd)
        {
            mnWnd.Close();
        }

        [RelayCommand]
        private void Minimize(WndInputBox mnWnd)
        {
            mnWnd.WindowState = System.Windows.WindowState.Minimized;
        }

        [RelayCommand]
        private void Maximize(WndInputBox mnWnd)
        {
            mnWnd.WindowState = System.Windows.WindowState.Maximized;
        }
    }
}