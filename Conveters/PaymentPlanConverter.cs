using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PrimeAppBooks.Conveters
{
    public class BooleanToPaymentPlanBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasPlan && hasPlan)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71")); // Green
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34495E")); // Dark Grey/Blue
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToPaymentPlanTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasPlan && hasPlan)
            {
                return "ACTIVE PLAN";
            }
            return "NO PLAN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
