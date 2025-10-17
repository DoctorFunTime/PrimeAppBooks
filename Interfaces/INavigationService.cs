using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PrimeAppBooks.Interfaces
{
    public interface INavigationService
    {
        void NavigateTo<T>() where T : Page;

        void NavigateTo<T>(object parameter) where T : Page;

        void GoBack();

        bool CanGoBack { get; }

        event EventHandler<Page> PageNavigated;
        
        event EventHandler<bool> LoadingStateChanged;

        // Enhanced animation registration
        void RegisterPageAnimation<T>(AnimationDirection direction, string? customStyle = null) where T : Page;

        // Page cache management
        void ClearPageCache();
        void RemovePageFromCache<T>() where T : Page;
    }
}