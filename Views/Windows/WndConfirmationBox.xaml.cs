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

namespace PrimeAppBooks.Views.Windows
{
    /// <summary>
    /// Interaction logic for WndConfirmationBox.xaml
    /// </summary>
    public partial class WndConfirmationBox : Window
    {
        public bool Result { get; private set; } = false;

        public WndConfirmationBox(string message, string title, string iconType)
        {
            InitializeComponent();
            DataContext = new WndConfirmationBoxViewModel(message, title, iconType, this);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        public void SetResult(bool result)
        {
            Result = result;
        }
    }
}
