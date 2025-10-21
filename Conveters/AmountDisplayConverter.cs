using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Conveters
{
    public class AmountDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JournalLine line)
            {
                // Get the account name from the ChartOfAccount navigation property
                string accountName = line.ChartOfAccount?.AccountName ?? $"Account {line.AccountId}";
                
                // Display the non-zero amount (either debit or credit) with account info
                if (line.DebitAmount > 0)
                {
                    return $"Dr: {line.DebitAmount:C} ({accountName})";
                }
                else if (line.CreditAmount > 0)
                {
                    return $"Cr: {line.CreditAmount:C} ({accountName})";
                }
                else
                {
                    return "$0.00";
                }
            }
            
            return "$0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
