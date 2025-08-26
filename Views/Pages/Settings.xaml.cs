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
    public partial class Settings : Page, Interfaces.IAnimatedPage
    {
        public string AnimationStyle => "SlideFromBottom";

        public Settings()
        {
            InitializeComponent();

            // Set initial selection after InitializeComponent()
            SettingsTabControl.SelectedIndex = 0;
        }

        private void CategoryButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag != null)
            {
                if (int.TryParse(radioButton.Tag.ToString(), out int tabIndex))
                {
                    SettingsTabControl.SelectedIndex = tabIndex;
                }
            }
        }
    }
}