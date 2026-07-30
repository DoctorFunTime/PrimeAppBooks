using CommunityToolkit.Mvvm.ComponentModel;
using System;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Models.Windows
{
    /// <summary>
    /// Observable staging row displayed in the Expense Import DataGrid.
    /// </summary>
    public partial class CashbookExpenseRow : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected = true;

        [ObservableProperty]
        private long _cbId;

        [ObservableProperty]
        private DateTime _date;

        [ObservableProperty]
        private string _docNumber;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private string _tag;

        [ObservableProperty]
        private decimal _amount;

        [ObservableProperty]
        private string _currencyCode;

        // The debit account chosen by the user (SelectedItem binding)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DebitAccountId))]
        private ChartOfAccount _selectedDebitAccount;

        // The credit account (SelectedItem binding)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CreditAccountId))]
        private ChartOfAccount _selectedCreditAccount;

        // Convenience accessors used by the journalling code
        public int? DebitAccountId => SelectedDebitAccount?.AccountId;
        public int? CreditAccountId => SelectedCreditAccount?.AccountId;

        /// <summary>
        /// Unique fingerprint embedded in Journal Reference for duplicate-detection.
        /// Anchored on CbId (the cashbook table's own primary key), which is the only
        /// field guaranteed unique per row — DocNumber/Date/Amount/Tag can all
        /// legitimately repeat (e.g. two identical uniform items on one receipt).
        /// </summary>
        public string Fingerprint => $"CBID{CbId}";
    }
}