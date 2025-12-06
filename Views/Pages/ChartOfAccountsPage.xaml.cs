using PrimeAppBooks.Interfaces;
using PrimeAppBooks.Services;
using PrimeAppBooks.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Views.Pages
{
    /// <summary>
    /// Interaction logic for ChartOfAccountsPage.xaml
    /// </summary>
    public partial class ChartOfAccountsPage : BaseAnimatedPage
    {
        public ChartOfAccountsPage(ChartOfAccountsPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ChartOfAccountsPageViewModel viewModel && e.NewValue is ChartOfAccount selectedAccount)
            {
                viewModel.SelectedAccount = selectedAccount;
            }
        }

        private void TreeView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is TreeView && !e.Handled)
            {
                e.Handled = true;
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}