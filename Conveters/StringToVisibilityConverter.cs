using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                bool isEmpty = string.IsNullOrWhiteSpace(text);
                bool invert = parameter?.ToString() == "Invert";

                bool isVisible = !isEmpty;
                if (invert) isVisible = !isVisible;

                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
