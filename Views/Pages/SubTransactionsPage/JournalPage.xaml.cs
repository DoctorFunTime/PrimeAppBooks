using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using PrimeAppBooks.ViewModels.Pages.SubTransactionsPage;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrimeAppBooks.Views.Pages.SubTransactionsPage
{
    /// <summary>
    /// Interaction logic for JournalPage.xaml
    /// </summary>
    public partial class JournalPage : BaseAnimatedPage
    {
        public JournalPage(JournalPageViewModel viewModel)
        {
            InitializeComponent();

            // Set custom animation properties for journal page
            // Slide from right to feel like opening a drawer
            SetAnimationDirection(AnimationDirection.FromRight);
            SetAnimationDuration(300);
            SetAnimationEasing(AnimationEasing.EaseOut);
            SetAnimateOut(true);

            // Just set DataContext, navigation service is already injected into VM
            DataContext = viewModel;
        }
    }
}