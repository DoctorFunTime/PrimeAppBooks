using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeAppBooks.Converters
{
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return 0.0;

            try
            {
                double value = System.Convert.ToDouble(values[0]);
                double maximum = System.Convert.ToDouble(values[1]);
                double width = System.Convert.ToDouble(values[2]);

                if (maximum > 0)
                    return (value / maximum) * width;

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}