using CommunityToolkit.Mvvm.ComponentModel;
using PrimeAppBooks.Models;
using System.ComponentModel;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    public partial class InvoiceLineViewModel : ObservableObject
    {
        private int _lineNumber;
        private ChartOfAccount _selectedAccount;
        private string _description;
        private decimal _quantity = 1;
        private decimal _unitPrice;
        private decimal _amount;

        public int LineNumber
        {
            get => _lineNumber;
            set => SetProperty(ref _lineNumber, value);
        }

        public ChartOfAccount SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value))
                {
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    UpdateAmount();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    UpdateAmount();
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public bool IsValid => SelectedAccount != null && Quantity > 0 && UnitPrice >= 0;

        private void UpdateAmount()
        {
            Amount = Quantity * UnitPrice;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }
    }
}