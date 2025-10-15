using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace PrimeAppBooks.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Frame _frame;
        private Page? _currentPage;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Page> _pageCache = new();
        private readonly Dictionary<Type, string> _pageAnimations = new();
        private readonly Dictionary<Type, AnimationDirection> _pageAnimationDirections = new();
        private bool _isNavigating = false;

        public event EventHandler<Page> PageNavigated = delegate { };

        public NavigationService(Frame frame, IServiceProvider serviceProvider)
        {
            _frame = frame;
            _serviceProvider = serviceProvider;
            _frame.Navigated += OnFrameNavigated;
            _frame.NavigationStopped += OnNavigationStopped;

            // Pre-warm the frame to reduce initial navigation delay
            _frame.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => { }));
        }

        // Enhanced animation registration with direction support
        public void RegisterPageAnimation<T>(AnimationDirection direction, string? customStyle = null) where T : Page
        {
            _pageAnimationDirections[typeof(T)] = direction;
            if (!string.IsNullOrEmpty(customStyle))
            {
                _pageAnimations[typeof(T)] = customStyle;
            }
        }

        public void NavigateTo<T>() where T : Page
        {
            NavigateTo<T>(null);
        }

        public void NavigateTo<T>(object parameter) where T : Page
        {
            if (_isNavigating) return;

            _isNavigating = true;

            try
            {
                // Get page from DI container - this will inject the ViewModel automatically
                var page = _serviceProvider.GetRequiredService<T>();

                // Apply exit animation to current page if it supports it
                if (_currentPage != null && _currentPage is IAnimatedPage currentAnimatedPage && currentAnimatedPage.AnimateOut)
                {
                    ApplyExitAnimation(_currentPage, () => 
                    {
                        _frame.Navigate(page, parameter);
                        // Don't reset _isNavigating here - let OnFrameNavigated handle it
                    });
                }
                else
                {
                    _frame.Navigate(page, parameter);
                    // Reset _isNavigating after navigation completes
                }
            }
            catch
            {
                _isNavigating = false;
                throw;
            }
        }

        public void GoBack()
        {
            if (_frame.CanGoBack && !_isNavigating)
            {
                _isNavigating = true;
                _frame.GoBack();
            }
        }

        public bool CanGoBack => _frame.CanGoBack;

        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            _currentPage = e.Content as Page;
            
            if (_currentPage != null)
            {
                // Apply entrance animation immediately without delay to prevent double loading
                ApplyEntranceAnimation(_currentPage);
                
                // Fire the PageNavigated event after animation is set up
                PageNavigated?.Invoke(this, _currentPage);
            }
            
            // Reset navigation flag after everything is complete
            _isNavigating = false;
        }

        private void OnNavigationStopped(object sender, NavigationEventArgs e)
        {
            _isNavigating = false;
        }

        private void ApplyEntranceAnimation(Page page)
        {
            // Check if page implements enhanced IAnimatedPage
            if (page is IAnimatedPage animatedPage)
            {
                ApplyEnhancedAnimation(page, animatedPage);
                return;
            }

            // Check registered animations
            if (_pageAnimationDirections.TryGetValue(page.GetType(), out var direction))
            {
                ApplyDirectionalAnimation(page, direction);
                return;
            }

            // Default smooth animation
            ApplySmoothSlideFromBottomAnimation(page);
        }

        private void ApplyEnhancedAnimation(Page page, IAnimatedPage animatedPage)
        {
            var direction = animatedPage.AnimationDirection;
            var duration = animatedPage.AnimationDuration > 0 ? animatedPage.AnimationDuration : 400;
            var easing = animatedPage.AnimationEasing;

            ApplyDirectionalAnimation(page, direction, duration, easing);
        }

        private void ApplyDirectionalAnimation(Page page, AnimationDirection direction, int duration = 400, AnimationEasing easing = AnimationEasing.EaseOut)
        {
            page.RenderTransform = new TranslateTransform();
            var storyboard = new Storyboard();
            var easingFunction = CreateEasingFunction(easing);

            switch (direction)
            {
                case AnimationDirection.FromLeft:
                    ApplySlideAnimation(page, storyboard, -400, 0, 0, 0, duration, easingFunction);
                    break;

                case AnimationDirection.FromRight:
                    ApplySlideAnimation(page, storyboard, 400, 0, 0, 0, duration, easingFunction);
                    break;

                case AnimationDirection.FromTop:
                    ApplySlideAnimation(page, storyboard, 0, -300, 0, 0, duration, easingFunction);
                    break;

                case AnimationDirection.FromBottom:
                    ApplySlideAnimation(page, storyboard, 0, 300, 0, 0, duration, easingFunction);
                    break;

                case AnimationDirection.FadeIn:
                    ApplyFadeAnimation(page, storyboard, duration, easingFunction);
                    break;

                case AnimationDirection.ZoomIn:
                    ApplyZoomAnimation(page, storyboard, duration, easingFunction);
                    break;

                case AnimationDirection.SlideIn:
                    ApplySmoothSlideFromBottomAnimation(page);
                    return;

                default:
                    ApplySmoothSlideFromBottomAnimation(page);
                    return;
            }

            // Add fade-in effect for smoother appearance
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(duration * 0.8),
                EasingFunction = easingFunction
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }

        private void ApplySlideAnimation(Page page, Storyboard storyboard, double fromX, double fromY, double toX, double toY, int duration, IEasingFunction easing)
        {
            var animation = new DoubleAnimation
            {
                From = fromX,
                To = toX,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };

            Storyboard.SetTarget(animation, page);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

            var animationY = new DoubleAnimation
            {
                From = fromY,
                To = toY,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };

            Storyboard.SetTarget(animationY, page);
            Storyboard.SetTargetProperty(animationY, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            storyboard.Children.Add(animation);
            storyboard.Children.Add(animationY);
        }

        private void ApplyFadeAnimation(Page page, Storyboard storyboard, int duration, IEasingFunction easing)
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(fadeAnimation);
        }

        private void ApplyZoomAnimation(Page page, Storyboard storyboard, int duration, IEasingFunction easing)
        {
            page.RenderTransform = new ScaleTransform();

            var scaleXAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };

            var scaleYAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(duration),
                EasingFunction = easing
            };

            Storyboard.SetTarget(scaleXAnimation, page);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

            Storyboard.SetTarget(scaleYAnimation, page);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
        }

        private void ApplySmoothSlideFromBottomAnimation(Page page)
        {
            page.RenderTransform = new TranslateTransform();
            var storyboard = new Storyboard();

            var animation = new DoubleAnimation
            {
                From = 200,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(350),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.2 }
            };

            Storyboard.SetTarget(animation, page);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(animation);
            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }

        private void ApplyExitAnimation(Page page, Action onComplete)
        {
            var storyboard = new Storyboard();

            var fadeAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(fadeAnimation);

            storyboard.Completed += (s, e) => onComplete?.Invoke();
            storyboard.Begin();
        }

        private IEasingFunction CreateEasingFunction(AnimationEasing easing)
        {
            return easing switch
            {
                AnimationEasing.Linear => null,
                AnimationEasing.EaseIn => new QuadraticEase { EasingMode = EasingMode.EaseIn },
                AnimationEasing.EaseOut => new QuadraticEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.EaseInOut => new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                AnimationEasing.Bounce => new BounceEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.Elastic => new ElasticEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.Back => new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 },
                _ => new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
        }

        // Clean up cached pages when needed
        public void ClearPageCache()
        {
            _pageCache.Clear();
        }

        public void RemovePageFromCache<T>() where T : Page
        {
            _pageCache.Remove(typeof(T));
        }
    }
}