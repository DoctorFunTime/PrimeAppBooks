using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PrimeAppBooks.Conveters
{
    /// <summary>
    /// Converts a boolean balance status to appropriate visual indicators (color brush or text)
    /// for journal entry balance validation display
    /// </summary>
    public class BalanceIndicatorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean balance status to either a color brush or text indicator
        /// </summary>
        /// <param name="value">Boolean indicating if journal entry is balanced</param>
        /// <param name="targetType">Target type for conversion (Brush for background color, string for text)</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture info for conversion (not used)</param>
        /// <returns>SolidColorBrush (green/red) for Brush target, or status text for string target</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isBalanced)
            {
                if (targetType == typeof(Brush))
                {
                    // Return background color brush
                    return isBalanced ?
                        new SolidColorBrush(Color.FromRgb(39, 174, 96)) :    // Green (#27AE60)
                        new SolidColorBrush(Color.FromRgb(231, 76, 60));     // Red (#E74C3C)
                }
                else if (targetType == typeof(string))
                {
                    // Return text indicator
                    return isBalanced ? "BALANCED" : "OUT OF BALANCE";
                }
            }

            // Default fallback
            return targetType == typeof(Brush) ?
                new SolidColorBrush(Color.FromRgb(127, 140, 141)) : // Gray (#7F8C8D)
                "UNKNOWN";
        }

        /// <summary>
        /// ConvertBack is not implemented as this converter is for one-way binding only
        /// </summary>
        /// <param name="value">The value to convert back (not used)</param>
        /// <param name="targetType">The target type (not used)</param>
        /// <param name="parameter">Optional parameter (not used)</param>
        /// <param name="culture">Culture info (not used)</param>
        /// <returns>Throws NotImplementedException</returns>
        /// <exception cref="NotImplementedException">Always thrown as ConvertBack is not supported</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BalanceIndicatorConverter does not support ConvertBack");
        }
    }
}