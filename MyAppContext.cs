using HandyControl.Controls;
using PrimeAppBooks.Models;
using PrimeAppBooks.Models.APIs;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrimeAppBooks
{
    public static class MyAppContext
    {
        private static TokenResponse _tokenResponse = new();

        public static TokenResponse TermSettings
        {
            get => _tokenResponse;
            set { _tokenResponse = value; OnStaticPropertyChanged(); }
        }

        private static User? _currentLogin;
        public static User? CurrentLogin
        {
            get => _currentLogin;
            set
            {
                _currentLogin = value;
                OnStaticPropertyChanged();
            }
        }

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

        private static void OnStaticPropertyChanged([CallerMemberName] string propertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}