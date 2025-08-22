using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Conveters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    namespace PrimeAppBooks.Converters
    {
        public class StatusToColorConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string status)
                {
                    return status switch
                    {
                        "POSTED" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#204ECDC4")),
                        "DRAFT" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20FF6B6B")),
                        "VOIDED" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#20666666")),
                        _ => new SolidColorBrush(Colors.Transparent)
                    };
                }
                return new SolidColorBrush(Colors.Transparent);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}