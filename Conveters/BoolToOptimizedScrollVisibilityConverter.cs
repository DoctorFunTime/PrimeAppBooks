using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    // Converts bool to ScrollBarVisibility (Auto vs Disabled)
    public class BoolToOptimizedScrollVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue && boolValue ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ScrollBarVisibility visibility && visibility == ScrollBarVisibility.Disabled;
        }
    }
}