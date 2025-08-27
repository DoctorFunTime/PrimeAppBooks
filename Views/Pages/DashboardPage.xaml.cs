using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
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

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page, IAnimatedPage
    {
        public string AnimationStyle => "SlideFromBottom";

        public DashboardPage()
        {
            InitializeComponent();

            // Just set DataContext, navigation service is already injected into VM
            DataContext = new DashboardPageViewModel(
                ServiceLocator.GetService<INavigationService>()
            );
        }
    }
}