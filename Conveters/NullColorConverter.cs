using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PrimeAppBooks.Conveters
{
    public class NullColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string paramStr)
            {
                var parts = paramStr.Split('|');
                var nullColor = parts.Length > 0 ? parts[0] : "Red";
                var notNullColor = parts.Length > 1 ? parts[1] : "Black";

                var colorString = value == null ? nullColor : notNullColor;
                
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
