using CommunityToolkit.Mvvm.ComponentModel;
using Npgsql;
using PrimeAppBooks.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.ViewModels.Windows
{
    public partial class WndSplashScreenViewModel : ObservableObject
    {
        private CancellationTokenSource _animationCts;

        private readonly Stopwatch _loadingStopwatch = new();
        private readonly DatabaseSetup _databaseInitializationService;

        [ObservableProperty]
        private string _currentLoadingMessage = "Initializing...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
        private double _progressValue;

        public double ProgressPercentage => Math.Round(_progressValue, 1);

        [ObservableProperty]
        private string _applicationVersion;

        // Action to be called when loading is complete (e.g., close splash, show main window)
        public Action? OnLoadingComplete { get; set; }

        public WndSplashScreenViewModel(DatabaseSetup databaseInitializationService)
        {
            _databaseInitializationService = databaseInitializationService;
            
            var exePath = Assembly.GetEntryAssembly()?.Location;
            if (!string.IsNullOrEmpty(exePath))
            {
                var fvi = FileVersionInfo.GetVersionInfo(exePath);
                ApplicationVersion = $"Version {fvi.FileVersion}";
            }
            else
            {
                ApplicationVersion = "Version N/A";
            }
        }

        /// <summary>
        /// Starts the asynchronous loading process.
        /// </summary>
        public async Task StartLoadingProcessAsync()
        {
            _loadingStopwatch.Start();

            var loadingSteps = new List<LoadingStep>
                {
                    new("Creating and verifying database...", DatabaseInitializations, 1),
                };

            double currentProgress = 0;
            foreach (var step in loadingSteps)
            {
                CurrentLoadingMessage = step.Message;

                // Optimized animation duration - faster and smoother
                var progressTask = AnimateProgressAsync(currentProgress, currentProgress + step.ProgressWeight,
                                                     (int)(step.ProgressWeight * 800));
                var workTask = ExecuteStepWithTimeout(step.AsyncAction, step.Message);

                await Task.WhenAll(progressTask, workTask);

                currentProgress += step.ProgressWeight;
            }

            // Ultra-fast final animation
            await AnimateProgressAsync(currentProgress, 1.0, 200);
            CurrentLoadingMessage = "Application loaded successfully!";
            await Task.Delay(100); // Minimal delay for user to see completion

            OnLoadingComplete?.Invoke();
            _loadingStopwatch.Stop();
        }

        private async Task AnimateProgressAsync(double from, double to, int durationMs)
        {
            var startTime = _loadingStopwatch.ElapsedMilliseconds;
            var endTime = startTime + durationMs;

            while (_loadingStopwatch.ElapsedMilliseconds < endTime)
            {
                var elapsed = _loadingStopwatch.ElapsedMilliseconds - startTime;
                var progress = Math.Min(elapsed / (double)durationMs, 1.0);

                // Use smoother easing function with higher frame rate
                ProgressValue = from + (to - from) * EaseOutCubic(progress);
                await Task.Delay(8); // Even higher frame rate for ultra-smooth animation
            }

            ProgressValue = to;
        }

        // Ultra-smooth easing function with better curve
        private double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

        // Additional easing function for variety
        private double EaseOutQuart(double t) => 1 - Math.Pow(1 - t, 4);

        // --- Example Asynchronous Loading Methods ---
        // Replace these with your actual asynchronous loading logic.

        private Task DatabaseInitializations()
            => _databaseInitializationService.InitializeAccountingDatabaseAsync(); // Pure task

        public record LoadingStep(string Message, Func<Task> AsyncAction, double ProgressWeight);

        private async Task ExecuteStepWithTimeout(Func<Task> asyncAction, string stepMessage, int delayAfterMs = 300)
        {
            try
            {
                var timeoutTask = Task.Delay(30000); // 30-second timeout
                var workTask = asyncAction();

                CurrentLoadingMessage = stepMessage; // Show original message immediately

                var completedTask = await Task.WhenAny(workTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    CurrentLoadingMessage = $"{stepMessage} (Timed out)";
                    await Task.Delay(1000);
                    return;
                }

                // Check for errors (even if completed before timeout)
                await workTask; // Will throw if task failed

                // Only show success if we get here - minimal delay
                CurrentLoadingMessage = $"{stepMessage} ✓";
                await Task.Delay(delayAfterMs);
            }
            catch (Exception ex)
            {
                string userFriendlyMessage = ex switch
                {
                    PostgresException pe => GetPostgresError(pe),
                    TimeoutException _ => "Timeout occurred",
                    IOException _ => "File access error",
                    _ => ex.Message.Split('\n')[0].Trim()
                };

                CurrentLoadingMessage = $"{stepMessage} failed: {userFriendlyMessage}";

                // Create unique header with timestamp and step context
                string uniqueHeader = $"{stepMessage} [{Guid.NewGuid().ToString()[..8]}]";
                //NotificationService.AddException(uniqueHeader, userFriendlyMessage);

                await Task.Delay(800); // Reduced error display time
            }
        }

        // Helper method for PostgreSQL errors
        private static string GetPostgresError(PostgresException pe)
        {
            return pe.SqlState switch
            {
                "42703" => "Database column error",
                "23505" => "Duplicate entry",
                "23503" => "Reference error (foreign key violation)",
                "42501" => "Permission denied",
                _ => $"Database error ({pe.SqlState}: {pe.Message.Split('\n')[0]})"
            };
        }
    }
}