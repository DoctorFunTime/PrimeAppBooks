using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Conveters
{
    public class ZeroAmountVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JournalLine line)
            {
                // Show the line only if it has a non-zero amount (either debit or credit)
                return (line.DebitAmount > 0 || line.CreditAmount > 0) ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
