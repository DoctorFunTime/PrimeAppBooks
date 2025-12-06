using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    public class BalanceToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal balance)
            {
                if (balance == 0) return "#7F8C8D"; // Gray for zero
                return balance > 0 ? "#27AE60" : "#E74C3C"; // Green for positive, Red for negative
            }
            return "#2C3E50"; // Default dark color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}