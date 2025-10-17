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
        public event EventHandler<bool> LoadingStateChanged = delegate { };

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
            
            // Show loading overlay IMMEDIATELY before any page creation
            LoadingStateChanged?.Invoke(this, true);

            try
            {
                // Force UI update before heavy operations with highest priority
                _frame.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
                
                // Give UI a moment to process the loading overlay
                System.Threading.Thread.Sleep(10);

                // Get page from DI container - this will inject the ViewModel automatically
                var page = _serviceProvider.GetRequiredService<T>();

                // Apply smooth crossfade transition
                if (_currentPage != null)
                {
                    ApplySmoothCrossfadeTransition(_currentPage, page, parameter);
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
                // Hide loading overlay on error
                LoadingStateChanged?.Invoke(this, false);
                throw;
            }
        }

        public void GoBack()
        {
            if (_frame.CanGoBack && !_isNavigating)
            {
                _isNavigating = true;
                // Show loading overlay
                LoadingStateChanged?.Invoke(this, true);
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
            
            // Hide loading overlay immediately after navigation completes
            // Use a small delay to ensure smooth transition
            _frame.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                LoadingStateChanged?.Invoke(this, false);
            }));
        }

        private void OnNavigationStopped(object sender, NavigationEventArgs e)
        {
            _isNavigating = false;
            // Hide loading overlay if navigation was stopped
            LoadingStateChanged?.Invoke(this, false);
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
            var duration = animatedPage.AnimationDuration > 0 ? animatedPage.AnimationDuration : 600; // Increased default duration
            var easing = animatedPage.AnimationEasing;

            ApplyDirectionalAnimation(page, direction, duration, easing);
        }

        private void ApplyDirectionalAnimation(Page page, AnimationDirection direction, int duration = 600, AnimationEasing easing = AnimationEasing.EaseOut)
        {
            // Enable hardware acceleration for smooth animations
            RenderOptions.SetBitmapScalingMode(page, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(page, EdgeMode.Aliased);
            
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
            // Enable hardware acceleration for smooth animations
            RenderOptions.SetBitmapScalingMode(page, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(page, EdgeMode.Aliased);
            
            page.RenderTransform = new TranslateTransform();
            var storyboard = new Storyboard();

            var animation = new DoubleAnimation
            {
                From = 150,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(animation, page);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
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
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

            storyboard.Children.Add(fadeAnimation);

            storyboard.Completed += (s, e) => onComplete?.Invoke();
            storyboard.Begin();
        }

        private void ApplySmoothCrossfadeTransition(Page currentPage, Page newPage, object parameter)
        {
            // Enable hardware acceleration for smooth animations
            RenderOptions.SetBitmapScalingMode(currentPage, BitmapScalingMode.HighQuality);
            RenderOptions.SetBitmapScalingMode(newPage, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(currentPage, EdgeMode.Aliased);
            RenderOptions.SetEdgeMode(newPage, EdgeMode.Aliased);
            
            // Set initial state for new page
            newPage.Opacity = 0;
            newPage.RenderTransform = new TranslateTransform(0, 50); // Slight upward offset
            
            // Navigate to new page first (but it's invisible)
            _frame.Navigate(newPage, parameter);
            
            // Create exit animation for current page
            var exitStoryboard = new Storyboard();
            var exitFadeAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var exitSlideAnimation = new DoubleAnimation
            {
                From = 0,
                To = -30,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            Storyboard.SetTarget(exitFadeAnimation, currentPage);
            Storyboard.SetTargetProperty(exitFadeAnimation, new PropertyPath("Opacity"));
            Storyboard.SetTarget(exitSlideAnimation, currentPage);
            Storyboard.SetTargetProperty(exitSlideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            
            exitStoryboard.Children.Add(exitFadeAnimation);
            exitStoryboard.Children.Add(exitSlideAnimation);
            
            // Create entrance animation for new page
            var entranceStoryboard = new Storyboard();
            var entranceFadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var entranceSlideAnimation = new DoubleAnimation
            {
                From = 50,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            Storyboard.SetTarget(entranceFadeAnimation, newPage);
            Storyboard.SetTargetProperty(entranceFadeAnimation, new PropertyPath("Opacity"));
            Storyboard.SetTarget(entranceSlideAnimation, newPage);
            Storyboard.SetTargetProperty(entranceSlideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            
            entranceStoryboard.Children.Add(entranceFadeAnimation);
            entranceStoryboard.Children.Add(entranceSlideAnimation);
            
            // Start exit animation first, then entrance animation with slight delay
            exitStoryboard.Begin();
            
            // Start entrance animation after a short delay for smooth crossfade
            _frame.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                entranceStoryboard.Begin();
            }));
        }

        private IEasingFunction CreateEasingFunction(AnimationEasing easing)
        {
            return easing switch
            {
                AnimationEasing.Linear => null,
                AnimationEasing.EaseIn => new CubicEase { EasingMode = EasingMode.EaseIn },
                AnimationEasing.EaseOut => new CubicEase { EasingMode = EasingMode.EaseOut },
                AnimationEasing.EaseInOut => new CubicEase { EasingMode = EasingMode.EaseInOut },
                AnimationEasing.Bounce => new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 0.8 },
                AnimationEasing.Elastic => new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 0.8 },
                AnimationEasing.Back => new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 },
                _ => new CubicEase { EasingMode = EasingMode.EaseOut } // Smoother default
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