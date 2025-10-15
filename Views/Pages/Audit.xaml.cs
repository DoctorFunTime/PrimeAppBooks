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
        public Audit(AuditPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}