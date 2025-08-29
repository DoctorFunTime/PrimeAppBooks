using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for ReportsPage.xaml
    /// </summary>
    public partial class ReportsPage : BaseAnimatedPage
    {
        public ReportsPage()
        {
            InitializeComponent();

            // Set custom animation properties for reports page
            // Slide from top to feel like opening a report
            SetAnimationDirection(Interfaces.AnimationDirection.FromBottom);
            SetAnimationDuration(300);
            SetAnimationEasing(Interfaces.AnimationEasing.EaseIn);
            SetAnimateOut(true);

            DataContext = new ReportsPageViewModel(
                ServiceLocator.GetService<INavigationService>()
            );
        }
    }
}