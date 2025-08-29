using PrimeAppBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PrimeAppBooks.Services
{
    /// <summary>
    /// Manages smooth page transitions and reduces navigation lag
    /// </summary>
    public class PageTransitionManager
    {
        private readonly Dictionary<Type, Page> _pageCache = new();
        private readonly Dictionary<Type, WeakReference<Page>> _weakPageCache = new();
        private readonly DispatcherTimer _cleanupTimer;
        private const int MAX_CACHE_SIZE = 10;
        private const int CLEANUP_INTERVAL_MS = 30000; // 30 seconds

        public PageTransitionManager()
        {
            _cleanupTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(CLEANUP_INTERVAL_MS), DispatcherPriority.Background, CleanupCache, Dispatcher.CurrentDispatcher);
            _cleanupTimer.Start();
        }

        /// <summary>
        /// Get or create a page instance with caching
        /// </summary>
        public T GetOrCreatePage<T>() where T : Page, new()
        {
            var pageType = typeof(T);

            // Check strong cache first
            if (_pageCache.TryGetValue(pageType, out var cachedPage))
            {
                return (T)cachedPage;
            }

            // Check weak cache
            if (_weakPageCache.TryGetValue(pageType, out var weakRef) && weakRef.TryGetTarget(out var weakPage))
            {
                // Move to strong cache
                _pageCache[pageType] = weakPage;
                _weakPageCache.Remove(pageType);
                return (T)weakPage;
            }

            // Create new page
            var newPage = new T();
            
            // Add to cache if not too full
            if (_pageCache.Count < MAX_CACHE_SIZE)
            {
                _pageCache[pageType] = newPage;
            }
            else
            {
                // Use weak reference for overflow
                _weakPageCache[pageType] = new WeakReference<Page>(newPage);
            }

            return newPage;
        }

        /// <summary>
        /// Preload a page type to improve navigation performance
        /// </summary>
        public void PreloadPage<T>() where T : Page, new()
        {
            var pageType = typeof(T);
            
            if (!_pageCache.ContainsKey(pageType) && !_weakPageCache.ContainsKey(pageType))
            {
                var page = new T();
                
                if (_pageCache.Count < MAX_CACHE_SIZE)
                {
                    _pageCache[pageType] = page;
                }
                else
                {
                    _weakPageCache[pageType] = new WeakReference<Page>(page);
                }
            }
        }

        /// <summary>
        /// Preload multiple page types for better performance
        /// </summary>
        public void PreloadPages(params Type[] pageTypes)
        {
            foreach (var pageType in pageTypes)
            {
                if (typeof(Page).IsAssignableFrom(pageType))
                {
                    var page = Activator.CreateInstance(pageType) as Page;
                    if (page != null)
                    {
                        if (_pageCache.Count < MAX_CACHE_SIZE)
                        {
                            _pageCache[pageType] = page;
                        }
                        else
                        {
                            _weakPageCache[pageType] = new WeakReference<Page>(page);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all cached pages
        /// </summary>
        public void ClearCache()
        {
            _pageCache.Clear();
            _weakPageCache.Clear();
        }

        /// <summary>
        /// Remove a specific page type from cache
        /// </summary>
        public void RemoveFromCache<T>() where T : Page
        {
            var pageType = typeof(T);
            _pageCache.Remove(pageType);
            _weakPageCache.Remove(pageType);
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int StrongCacheCount, int WeakCacheCount) GetCacheStats()
        {
            return (_pageCache.Count, _weakPageCache.Count);
        }

        private void CleanupCache(object sender, EventArgs e)
        {
            // Clean up weak references that are no longer alive
            var keysToRemove = new List<Type>();
            
            foreach (var kvp in _weakPageCache)
            {
                if (!kvp.Value.TryGetTarget(out _))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _weakPageCache.Remove(key);
            }

            // Move some pages from strong cache to weak cache if we have too many
            if (_pageCache.Count > MAX_CACHE_SIZE / 2)
            {
                var pagesToMove = new List<Type>();
                var moveCount = 0;
                var targetMoveCount = _pageCache.Count - MAX_CACHE_SIZE / 2;

                foreach (var kvp in _pageCache)
                {
                    if (moveCount < targetMoveCount)
                    {
                        pagesToMove.Add(kvp.Key);
                        moveCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var pageType in pagesToMove)
                {
                    if (_pageCache.TryGetValue(pageType, out var page))
                    {
                        _weakPageCache[pageType] = new WeakReference<Page>(page);
                        _pageCache.Remove(pageType);
                    }
                }
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Stop();
            ClearCache();
        }
    }
}

