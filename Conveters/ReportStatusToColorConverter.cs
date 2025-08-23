using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    public class ReportStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "completed" or "success" => "#27AE60",    // Green
                    "processing" => "#F39C12",               // Orange
                    "failed" or "error" => "#E74C3C",        // Red
                    "scheduled" => "#3498DB",                // Blue
                    _ => "#95A5A6"                          // Gray
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