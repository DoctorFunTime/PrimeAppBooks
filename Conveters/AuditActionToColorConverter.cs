using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    public class AuditActionToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string severity)
            {
                return severity.ToLower() switch
                {
                    "critical" or "error" => "#E74C3C",    // Red
                    "warning" => "#F39C12",                // Orange
                    "info" => "#3498DB",                   // Blue
                    "success" => "#27AE60",                // Green
                    _ => "#95A5A6"                         // Gray
                };
            }
            return "#95A5A6";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}