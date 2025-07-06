using System.Globalization;
using System.Windows.Data;

namespace quick_sql.Converter
{
    public class SizeFormaterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] sizes = { "MB", "GB", "TB", "PB", "EB" };
            decimal sizeMb = (decimal)value;
            int order = 0;

            while (sizeMb >= 1000 && order < sizes.Length - 1)
            {
                order++;
                sizeMb /= 1000;
            }

            return string.Format("{0:0.##} {1}", sizeMb, sizes[order]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
