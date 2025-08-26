using PrimeAppBooks.Interfaces;
using System;
using System.Collections.Generic;
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
        private Page? _currentPage;
        private readonly Dictionary<Type, string> _pageAnimations = new();

        public event EventHandler<Page> PageNavigated = delegate { };

        public NavigationService(Frame frame)
        {
            _frame = frame;
            _frame.Navigated += OnFrameNavigated;
        }

        // Optional: Method to register animations (keep it decoupled)
        public void RegisterPageAnimation<T>(string animationStyle) where T : Page
        {
            _pageAnimations[typeof(T)] = animationStyle;
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
            // Check if page implements IAnimatedPage (decoupled approach)
            if (page is IAnimatedPage animatedPage)
            {
                ApplyCustomAnimation(page, animatedPage.AnimationStyle);
                return;
            }

            // Check registered animations
            if (_pageAnimations.TryGetValue(page.GetType(), out var animationStyle))
            {
                ApplyCustomAnimation(page, animationStyle);
                return;
            }

            // Default animation
            ApplySlideFromBottomAnimation(page);
        }

        private void ApplyCustomAnimation(Page page, string animationStyle)
        {
            page.RenderTransform = new TranslateTransform();
            var storyboard = new Storyboard();

            switch (animationStyle)
            {
                case "SlideFromRight":
                    ApplySlideFromRightAnimation(page, storyboard);
                    break;

                case "SlideFromBottom":
                    ApplySlideFromBottomAnimation(page, storyboard);
                    break;

                case "FadeIn":
                    ApplyFadeInAnimation(page, storyboard);
                    break;

                default:
                    ApplySlideFromBottomAnimation(page, storyboard);
                    break;
            }

            storyboard.Begin();
        }

        private void ApplySlideFromRightAnimation(Page page, Storyboard storyboard)
        {
            var animation = new DoubleAnimation
            {
                From = 400,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.35),
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 }
            };

            Storyboard.SetTarget(animation, page);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(animation);
            storyboard.Children.Add(fadeAnimation);
        }

        private void ApplySlideFromBottomAnimation(Page page, Storyboard storyboard = null)
        {
            storyboard ??= new Storyboard();

            var animation = new DoubleAnimation
            {
                From = 200,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            Storyboard.SetTarget(animation, page);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(animation);
            storyboard.Children.Add(fadeAnimation);

            if (storyboard != null)
                storyboard.Begin();
        }

        private void ApplyFadeInAnimation(Page page, Storyboard storyboard)
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.4),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(fadeAnimation);
        }
    }
}