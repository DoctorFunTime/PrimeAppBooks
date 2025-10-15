using CommunityToolkit.Mvvm.ComponentModel;
using PrimeAppBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class SettingsPageViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;

        public SettingsPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }
    }
}