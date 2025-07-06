using System.Globalization;
using System.Windows.Data;

namespace quick_sql.Converter
{
    public class QueryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                stringValue = stringValue.TrimStart();
                return stringValue;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
