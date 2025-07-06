using System.Globalization;
using System.Windows.Data;

namespace quick_sql.Converter
{
    public class NumberFormaterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long longValue)
            {
                return longValue.ToString("#,0");
            }
            else if (value is int intValue)
            {
                return intValue.ToString("#,0");
            }
            else if (value is double doubleValue)
            {
                return doubleValue.ToString("#,0.00");
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
