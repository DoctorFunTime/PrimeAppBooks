using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PrimeAppBooks.Conveters
{
    public class ReportTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string reportType)
            {
                return reportType.ToLower() switch
                {
                    "balance sheet" => "⚖️",
                    "income statement" => "📈",
                    "cash flow" => "💸",
                    "trial balance" => "📋",
                    "general ledger" => "📒",
                    "aging" => "⏰",
                    "budget" => "💰",
                    "tax" => "🧾",
                    _ => "📄"
                };
            }
            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}