using PrimeAppBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace PrimeAppBooks.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Frame _frame;
        private Page _currentPage;

        public event EventHandler<Page> PageNavigated;

        public NavigationService(Frame frame)
        {
            _frame = frame;
            _frame.Navigated += OnFrameNavigated;
        }

        public void NavigateTo<T>() where T : Page
        {
            NavigateTo<T>(null);
        }

        public void NavigateTo<T>(object parameter) where T : Page
        {
            var page = Activator.CreateInstance<T>();
            _frame.Navigate(page, parameter);
        }

        public void GoBack()
        {
            if (_frame.CanGoBack)
                _frame.GoBack();
        }

        public bool CanGoBack => _frame.CanGoBack;

        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            _currentPage = e.Content as Page;
            PageNavigated?.Invoke(this, _currentPage);

            if (_currentPage != null)
            {
                ApplyEntranceAnimation(_currentPage);
            }
        }

        private void ApplyEntranceAnimation(Page page)
        {
            page.RenderTransform = new TranslateTransform();

            var storyboard = new Storyboard();
            var animation = new DoubleAnimation
            {
                From = 300,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4),
                DecelerationRatio = 0.7
            };

            Storyboard.SetTarget(animation, page);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(animation);
            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }
    }
}