using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    public class AccountTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string accountType)
            {
                return accountType.ToLower() switch
                {
                    "asset" or "assets" => "#3498DB",      // Blue
                    "liability" or "liabilities" => "#E74C3C", // Red
                    "equity" => "#2ECC71",                 // Green
                    "revenue" => "#F39C12",                // Orange
                    "expense" or "expenses" => "#9B59B6",  // Purple
                    _ => "#95A5A6"                         // Gray (default)
                };
            }
            return "#95A5A6";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}