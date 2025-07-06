using System.Globalization;
using System.Windows.Data;

namespace quick_sql.Converter
{
    public class CountFormaterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long count = (long)value;
            if (count >= 1000000000000)
                return (count / 1000000000000D).ToString("0.#T");

            if (count >= 1000000000)
                return (count / 1000000000D).ToString("0.#B");

            if (count >= 1000000)
                return (count / 1000000D).ToString("0.#M");

            if (count >= 10000)
                return (count / 1000D).ToString("0.#K");

            return count.ToString("#,0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
