using HandyControl.Controls;
using PrimeAppBooks.Models.APIs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks
{
    public static class MyAppContext
    {
        private static TokenResponse _tokenResponse = new();

        public static TokenResponse TermSettings
        {
            get => _tokenResponse;
            set
            {
                _tokenResponse = value;
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