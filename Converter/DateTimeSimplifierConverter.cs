using System.Globalization;
using System.Windows.Data;

namespace quick_sql.Converter
{
    public class DateTimeSimplifierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateOnly dateOnly)
            {
                return dateOnly == DateOnly.MinValue ? "" : SimplifyDate(dateOnly.ToDateTime(TimeOnly.MinValue));
            }
            else if (value is DateTime datetimeValue)
            {
                return datetimeValue == DateTime.MinValue ? "" : SimplifyDate(datetimeValue);
            }

            return value;
        }

        private static string SimplifyDate(DateTime? dateTime)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            if (!dateTime.HasValue || dateTime.Value == DateTime.MinValue)
                return string.Empty;

            DateTime dtValue = dateTime.Value;
            var ts = new TimeSpan(DateTime.Now.Ticks - dtValue.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);
            bool isPast = dateTime < DateTime.Now;

            if (delta < 1 * MINUTE)
                return ts.Seconds == 0 ? "just now" :
                    ts.Seconds == 1 ? "one second ago" :
                        isPast ?
                            string.Format("{0} seconds ago", ts.Seconds) :
                            string.Format("in {0} seconds", ts.Seconds * -1);

            if (delta < 2 * MINUTE)
                return isPast ? "one minute ago" : "in one minute";

            if (delta < 45 * MINUTE)
                return isPast ?
                    string.Format("{0} minutes ago", ts.Minutes) :
                    string.Format("in {0} minutes", ts.Minutes * -1);

            if (delta < 90 * MINUTE)
                return isPast ? "one hour ago" : "in one hour";

            if (delta < 24 * HOUR)
                return isPast ?
                    string.Format("{0} hours ago", ts.Hours) :
                    string.Format("in {0} hours", ts.Hours * -1);

            if (delta < 48 * HOUR)
                return isPast ? "yesterday" : "tomorrow";

            if (delta < 30 * DAY)
                return isPast ?
                    string.Format("{0} days ago", ts.Days) :
                    string.Format("in {0} days", ts.Days * -1);

            if (delta < 12 * MONTH)
            {
                int months = (int)Math.Floor((double)ts.Days / 30);
                if (months == 1 || months == -1)
                    return isPast ? "one month ago" : "in one month";
                else
                    return isPast ?
                        string.Format("{0} months ago", months) :
                        string.Format("in {0} months", months * -1);
            }
            else
            {
                int years = (int)Math.Floor((double)ts.Days / 365);
                if (years == 1 || years == -1)
                    return isPast ? "one year ago" : "in one year";
                else
                    return isPast ?
                        string.Format("{0} years ago", years) :
                        string.Format("in {0} years", years * -1);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }
}
