using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PrimeAppBooks.Conveters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "draft" => new SolidColorBrush(Color.FromArgb(0xFF, 0xF1, 0xFA, 0x8C)), // Yellow
                    "posted" => new SolidColorBrush(Color.FromArgb(0xFF, 0x50, 0xFA, 0x7B)), // Green
                    "void" => new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B)),   // Red
                    _ => new SolidColorBrush(Color.FromArgb(0xFF, 0x62, 0x72, 0xA4))         // Default (TextSecondary)
                };
            }
            return new SolidColorBrush(Color.FromArgb(0xFF, 0x62, 0x72, 0xA4));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}