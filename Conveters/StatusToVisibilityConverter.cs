using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                if (parameter?.ToString() == "Delete")
                {
                    // Show delete button for both DRAFT and POSTED
                    return (status == "DRAFT" || status == "POSTED") ? Visibility.Visible : Visibility.Collapsed;
                }

                // Show other buttons (like POST) only for DRAFT status
                return status == "DRAFT" ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
