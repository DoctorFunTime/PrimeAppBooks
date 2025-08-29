using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.Services.APIs;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : BaseAnimatedPage
    {
        public DashboardPage()
        {
            InitializeComponent();

            // Set custom animation properties for the dashboard
            SetAnimationDirection(AnimationDirection.FromBottom);
            SetAnimationDuration(300);
            SetAnimationEasing(AnimationEasing.EaseIn);
            SetAnimateOut(true);

            DataContext = new DashboardPageViewModel(
                ServiceLocator.GetService<INavigationService>(),
                ServiceLocator.GetService<QuickBooksAuthService>(),
                ServiceLocator.GetService<QuickBooksService>());
        }
    }
}