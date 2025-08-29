using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for ChartOfAccountsPage.xaml
    /// </summary>
    public partial class ChartOfAccountsPage : BaseAnimatedPage
    {
        public ChartOfAccountsPage()
        {
            InitializeComponent();

            // Set custom animation properties for chart of accounts page
            // Slide from right to feel like opening a drawer
            SetAnimationDirection(Interfaces.AnimationDirection.FromRight);
            SetAnimationDuration(300);
            SetAnimationEasing(Interfaces.AnimationEasing.EaseOut);
            SetAnimateOut(true);

            DataContext = new ChartOfAccountsPageViewModel(
                ServiceLocator.GetService<INavigationService>()
            );
        }
    }
}