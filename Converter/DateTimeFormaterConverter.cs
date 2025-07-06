using System.Globalization;
using System.Windows.Data;

namespace quick_sql.Converter
{
    public class DateTimeFormaterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly dateOnly)
            {
                return dateOnly == DateOnly.MinValue ? "" :
                       $"{dateOnly:yyyy-MM-dd}";
            }
            else if (value is DateTime datetimeValue)
            {
                return datetimeValue == DateTime.MinValue ? "" :
                       $"{datetimeValue:yyyy-MM-dd hh:mm:ss}";
            }

            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
