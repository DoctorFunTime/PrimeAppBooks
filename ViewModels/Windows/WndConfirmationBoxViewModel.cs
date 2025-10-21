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
    public partial class WndConfirmationBoxViewModel : ObservableObject
    {
        private readonly WndConfirmationBox _window;
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

        public WndConfirmationBoxViewModel(string message, string title, string iconType, WndConfirmationBox window)
        {
            Message = message;
            Title = title;
            IconType = iconType;
            _window = window;

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

                case "WarningOutline":
                    IconColor = Brushes.Orange;
                    break;

                case "DeleteForever":
                    IconColor = Brushes.LightCoral;
                    break;

                case "QuestionMark":
                    IconColor = Brushes.CornflowerBlue;
                    break;

                default:
                    IconColor = Brushes.White;
                    break;
            }
        }

        [RelayCommand]
        private void Yes(WndConfirmationBox window)
        {
            window.SetResult(true);
            window.Close();
        }

        [RelayCommand]
        private void No(WndConfirmationBox window)
        {
            window.SetResult(false);
            window.Close();
        }

        [RelayCommand]
        private void Close(WndConfirmationBox window)
        {
            window.SetResult(false);
            window.Close();
        }
    }
}
