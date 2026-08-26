using System;
using System.Globalization;
using System.Windows.Data;

namespace Myria.Wpf.View.Converters
{
    public class TruncateAtWordConverter : IValueConverter
    {
        private const int BreakAt = 100;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string text || text.Length <= BreakAt)
                return value ?? string.Empty;

            int cut = text.LastIndexOf(' ', BreakAt);
            if (cut <= 0) cut = BreakAt;
            return text[..cut] + "\n" + text[cut..].TrimStart();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
