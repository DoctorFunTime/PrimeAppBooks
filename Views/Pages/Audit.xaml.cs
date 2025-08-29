using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for Audit.xaml
    /// </summary>
    public partial class Audit : BaseAnimatedPage
    {
        public Audit()
        {
            InitializeComponent();

            // Set custom animation properties for audit page
            // Fade in for data-heavy pages
            SetAnimationDirection(Interfaces.AnimationDirection.FromBottom);
            SetAnimationDuration(300);
            SetAnimationEasing(Interfaces.AnimationEasing.EaseOut);
            SetAnimateOut(true);

            DataContext = new AuditPageViewModel(
                ServiceLocator.GetService<INavigationService>()
            );
        }
    }
}