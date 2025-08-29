using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using PrimeAppBooks.ViewModels.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for TransactionsPage.xaml
    /// </summary>
    public partial class TransactionsPage : BaseAnimatedPage
    {
        public TransactionsPage()
        {
            InitializeComponent();

            // Set custom animation properties for transactions page
            // Slide from right to feel like opening a drawer/panel
            SetAnimationDirection(AnimationDirection.FromBottom);
            SetAnimationDuration(300);
            SetAnimationEasing(AnimationEasing.EaseOut);
            SetAnimateOut(true);

            // Just set DataContext, navigation service is already injected into VM
            DataContext = new TransactionsPageViewModel(
                ServiceLocator.GetService<INavigationService>()
            );
        }
    }
}