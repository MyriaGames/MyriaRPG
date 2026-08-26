using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Myria.Wpf.View.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string checkValue = value.ToString();
            string targetValue = parameter.ToString();
            bool invert = false;

            if (targetValue.StartsWith("!"))
            {
                invert = true;
                targetValue = targetValue.Substring(1);
            }

            bool matches = checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);

            if (invert)
                return matches ? Visibility.Collapsed : Visibility.Visible;
            else
                return matches ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
