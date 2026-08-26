using System.Windows.Media;

namespace Myria.Wpf.Model
{
    public class LobbyInfo
    {
        public string Id          { get; init; } = string.Empty;
        public string Name        { get; init; } = string.Empty;
        public string Url         { get; init; } = string.Empty;
        public int    CharacterCount { get; init; }
        public bool   IsOnline    { get; init; }

        public string PopulationLabel => IsOnline
            ? CharacterCount switch
            {
                0      => "Empty",
                <= 20  => "Low",
                <= 100 => "Medium",
                _      => "High"
            }
            : "Offline";

        public Brush PopulationBrush => IsOnline
            ? CharacterCount switch
            {
                0      => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
                <= 20  => new SolidColorBrush(Color.FromRgb(0x56, 0xC1, 0x64)),
                <= 100 => new SolidColorBrush(Color.FromRgb(0xE1, 0xC4, 0x81)),
                _      => new SolidColorBrush(Color.FromRgb(0xCF, 0x47, 0x58))
            }
            : new SolidColorBrush(Color.FromRgb(0xCF, 0x47, 0x58));
    }
}
