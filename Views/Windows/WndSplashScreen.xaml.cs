using Microsoft.Extensions.DependencyInjection;
using PrimeAppBooks.ViewModels.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PrimeAppBooks.Views.Windows
{
    /// <summary>
    /// Interaction logic for WndSplashScreen.xaml
    /// </summary>
    public partial class WndSplashScreen : Window
    {
        private readonly WndSplashScreenViewModel _viewModel;

        public WndSplashScreen(WndSplashScreenViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.OnLoadingComplete = () =>
            {
                // Ensure this UI work runs on the UI thread.
                Dispatcher.Invoke(async () =>
                {
                    var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                    mainWindow.DataContext = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();

                    // Smooth fade-out transition before closing
                    var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(200))
                    {
                        EasingFunction = new PowerEase { EasingMode = EasingMode.EaseIn }
                    };
                    fadeOut.Completed += (s, e) => this.Close();
                    this.BeginAnimation(OpacityProperty, fadeOut);
                });
            };
        }

        private async void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Just await the ViewModel's method directly. It won't block the UI.
                await _viewModel.StartLoadingProcessAsync();
            }
            catch (Exception ex)
            {
                // This is a catastrophic failure, shutting down is appropriate.
                Debug.WriteLine($"A catastrophic error occurred during loading: {ex}");
                Application.Current.Shutdown();
            }
        }
    }
}