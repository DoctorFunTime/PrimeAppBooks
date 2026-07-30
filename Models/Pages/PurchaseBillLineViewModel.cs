using CommunityToolkit.Mvvm.ComponentModel;
using PrimeAppBooks.Models;
using System.ComponentModel;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.ViewModels.Pages
{
    /// <summary>
    /// Line ViewModel for purchase invoices.
    /// Key difference from InvoiceLineViewModel: SelectedItem auto-fills from
    /// PurchaseCost (not SalePrice) since we are receiving goods, not selling them.
    /// </summary>
    public partial class PurchaseBillLineViewModel : ObservableObject
    {
        private int _lineNumber;
        private ChartOfAccount _selectedAccount;
        private InventoryItem _selectedItem;
        private string _description;
        private decimal? _quantity = 1;
        private decimal? _unitPrice;
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
                    OnPropertyChanged(nameof(IsValid));
            }
        }

        public InventoryItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    if (_selectedItem != null)
                    {
                        // Purchase side: auto-fill from PurchaseCost, not SalePrice
                        Description = _selectedItem.ItemName;
                        UnitPrice = _selectedItem.PurchaseCost;
                        Quantity = 1;

                        // Auto-select the item's Asset account (we are receiving inventory)
                        // Parent ViewModel handles this via PropertyChanged
                    }
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                    OnPropertyChanged(nameof(IsValid));
            }
        }

        public decimal? Quantity
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

        public decimal? UnitPrice
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

        public bool IsValid =>
            SelectedAccount != null &&
            !string.IsNullOrWhiteSpace(Description) &&
            (Quantity ?? 0) > 0 &&
            (UnitPrice ?? 0) >= 0;

        private void UpdateAmount()
        {
            Amount = (Quantity ?? 0) * (UnitPrice ?? 0);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }
    }
}
