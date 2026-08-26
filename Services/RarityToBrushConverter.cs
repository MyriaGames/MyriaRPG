using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Myria.Wpf.Services
{
    public class RarityToBrushConverter : IValueConverter
    {
        public Brush Common { get; set; } = new SolidColorBrush(Color.FromRgb(128, 128, 128));
        public Brush Uncommon { get; set; } = new SolidColorBrush(Color.FromRgb(60, 179, 113));
        public Brush Rare { get; set; } = new SolidColorBrush(Color.FromRgb(30, 144, 255));
        public Brush Epic { get; set; } = new SolidColorBrush(Colors.Magenta);
        public Brush Unique { get; set; } = new SolidColorBrush(Color.FromRgb(255, 245, 157));
        public Brush Legendary { get; set; } = new SolidColorBrush(Color.FromRgb(218, 165, 32));
        public Brush Godly { get; set; } = new SolidColorBrush(Color.FromRgb(255, 182, 193));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var name = value?.ToString() ?? "Common";
            return name switch
            {
                "Uncommon" => Uncommon,
                "Rare" => Rare,
                "Epic" => Epic,
                "Unique" => Unique,
                "Legendary" => Legendary,
                "Godly" => Godly,
                _ => Common
            };

        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
    }

}
