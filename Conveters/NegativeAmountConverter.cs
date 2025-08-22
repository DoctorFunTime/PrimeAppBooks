using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    /// <summary>
    /// Converter that returns true if the amount is negative, false otherwise.
    /// Used for styling negative amounts differently in the DataGrid.
    /// </summary>
    public class NegativeAmountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Handle null values
            if (value == null)
                return false;

            // Try to convert to decimal first (most common for financial amounts)
            if (value is decimal decimalAmount)
            {
                return decimalAmount < 0;
            }

            // Handle double values
            if (value is double doubleAmount)
            {
                return doubleAmount < 0;
            }

            // Handle float values
            if (value is float floatAmount)
            {
                return floatAmount < 0;
            }

            // Handle integer values
            if (value is int intAmount)
            {
                return intAmount < 0;
            }

            // Handle long values
            if (value is long longAmount)
            {
                return longAmount < 0;
            }

            // Try to parse string values
            if (value is string stringAmount)
            {
                // Remove currency symbols and whitespace for parsing
                string cleanAmount = stringAmount.Replace("$", "")
                                                 .Replace("€", "")
                                                 .Replace("£", "")
                                                 .Replace("¥", "")
                                                 .Replace(",", "")
                                                 .Trim();

                if (decimal.TryParse(cleanAmount, NumberStyles.Currency, culture, out decimal parsedDecimal))
                {
                    return parsedDecimal < 0;
                }

                if (double.TryParse(cleanAmount, NumberStyles.Currency, culture, out double parsedDouble))
                {
                    return parsedDouble < 0;
                }
            }

            // If we can't determine the sign, assume positive
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter is one-way only
            throw new NotSupportedException("NegativeAmountConverter is a one-way converter.");
        }
    }
}