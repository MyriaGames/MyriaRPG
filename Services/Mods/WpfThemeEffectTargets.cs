using System.Windows;
using System.Windows.Media;

namespace Myria.Wpf.Services.Mods
{
    internal static class WpfThemeEffectTargets
    {
        public const string ThemeAccentPalette = "themeAccentPalette";

        public static bool TryApply(string target, ResourceDictionary dictionary, Color accent, double brightness)
        {
            if (!string.Equals(target, ThemeAccentPalette, StringComparison.OrdinalIgnoreCase))
                return false;

            ApplyThemeAccentPalette(dictionary, accent, brightness);
            return true;
        }

        public static void ApplyThemeAccentPalette(ResourceDictionary dictionary, Color accent, double brightness)
        {
            var palette = CreateAccentPalette(accent, brightness);

            SetColor(dictionary, "Color.Background", palette.Background);
            SetColor(dictionary, "Color.Accent", palette.Accent);
            SetColor(dictionary, "Color.Border", palette.Border);
            SetColor(dictionary, "Color.Surface", palette.Surface);
            SetColor(dictionary, "Color.TileBackground", palette.TileBackground);
            SetColor(dictionary, "Color.TitleBackground", palette.TitleBackground);
            SetColor(dictionary, "Color.SecondaryText", palette.SecondaryText);
            SetColor(dictionary, "Color.Rarity.Common", palette.Common);
            SetColor(dictionary, "Color.Rarity.Rare", palette.Rare);
            SetColor(dictionary, "Color.Rarity.Epic", palette.Epic);
            SetColor(dictionary, "Color.Rarity.Mythic", palette.Mythic);
            SetColor(dictionary, "Color.Equipment.Accessory", palette.Rare);
            SetColor(dictionary, "Color.MapBackground", palette.MapBackground);
            SetColor(dictionary, "Color.Map.Node.Dungeon", palette.MapNode);
            SetColor(dictionary, "Color.Map.Node.Boss", palette.Boss);
            SetColor(dictionary, "Color.Map.Node.Cave", palette.Cave);
            SetColor(dictionary, "Color.Map.Node.World", palette.World);
            SetColor(dictionary, "Color.Map.Border.Normal", palette.Border);
            SetColor(dictionary, "Color.Map.Border.Current", palette.Accent);
            SetColor(dictionary, "Color.Map.Marker", palette.Accent);

            SetBrush(dictionary, "Brush.Background", palette.Background);
            SetBrush(dictionary, "Brush.Accent", palette.Accent);
            SetBrush(dictionary, "Brush.Border", palette.Border);
            SetBrush(dictionary, "Brush.Surface", palette.Surface);
            SetBrush(dictionary, "Brush.TileBackground", palette.TileBackground);
            SetBrush(dictionary, "Brush.TitleBackground", palette.TitleBackground);
            SetBrush(dictionary, "Brush.SecondaryText", palette.SecondaryText);
            SetBrush(dictionary, "Brush.Rarity.Common", palette.Common);
            SetBrush(dictionary, "Brush.Rarity.Rare", palette.Rare);
            SetBrush(dictionary, "Brush.Rarity.Epic", palette.Epic);
            SetBrush(dictionary, "Brush.Rarity.Mythic", palette.Mythic);
            SetBrush(dictionary, "Brush.Equipment.Accessory", palette.Rare);
            SetBrush(dictionary, "Brush.MapBackground", palette.MapBackground);
            SetBrush(dictionary, "Brush.Panel", palette.Panel);
            SetBrush(dictionary, "Brush.PanelDark", palette.PanelDark);
            SetBrush(dictionary, "Brush.TextOnGold", palette.Panel);
            SetBrush(dictionary, "Brush.BlueActive", palette.Active);
            SetBrush(dictionary, "Brush.NavHover", palette.NavHover);

            SetGradient(dictionary, "Brush.MainPanelGradient", palette.MainPanelA, palette.MainPanelB);
            SetGradient(dictionary, "Brush.PortraitGradient", palette.PortraitA, palette.PortraitB, palette.PortraitC);
            SetGradient(dictionary, "Brush.HudBodyGradient", palette.HudA, palette.HudB, palette.HudC);
            SetGradient(dictionary, "Brush.HudCrestGradient", palette.CrestA, palette.CrestB);
            SetGradient(dictionary, "Brush.WindowPanelGradient", palette.HudA, palette.WindowB, palette.HudC);
            SetGradient(dictionary, "Brush.WindowBackgroundGradient", palette.Background, palette.WindowMid, palette.WindowEnd);
        }

        private static AccentPalette CreateAccentPalette(Color accent, double brightness)
        {
            brightness = Math.Clamp(brightness, 0.25d, 2d);

            Color Tone(double amount, byte alpha = 0xFF)
            {
                double factor = amount * brightness;
                return Color.FromArgb(
                    alpha,
                    ClampChannel(accent.R * factor),
                    ClampChannel(accent.G * factor),
                    ClampChannel(accent.B * factor));
            }

            return new AccentPalette(
                Accent: Tone(1.00),
                Border: Tone(0.34),
                Surface: Tone(0.18),
                TileBackground: Tone(0.22),
                TitleBackground: Tone(0.14),
                SecondaryText: Tone(0.58),
                Common: Tone(0.66),
                Rare: Tone(0.88),
                Epic: Tone(1.12),
                Mythic: Tone(1.45),
                MapBackground: Tone(0.14),
                MapNode: Tone(0.46),
                Boss: Tone(0.72),
                Cave: Tone(0.40),
                World: Tone(0.20),
                Panel: Tone(0.13, 0xFF),
                PanelDark: Tone(0.09, 0xFF),
                Active: Tone(0.32, 0xFF),
                NavHover: Tone(0.20, 0xFF),
                MainPanelA: Tone(0.16, 0xFF),
                MainPanelB: Tone(0.09, 0xFF),
                PortraitA: Tone(0.20, 0xFF),
                PortraitB: Tone(0.10, 0xFF),
                PortraitC: Color.FromRgb(6, 0, 0),
                HudA: Tone(0.15, 0xFF),
                HudB: Tone(0.08, 0xFF),
                HudC: Tone(0.04, 0xFF),
                CrestA: Tone(0.28, 0xFF),
                CrestB: Tone(0.10, 0xFF),
                WindowB: Tone(0.08, 0xFF),
                Background: Tone(0.06, 0xFF),
                WindowMid: Tone(0.12, 0xFF),
                WindowEnd: Tone(0.04, 0xFF));
        }

        private static byte ClampChannel(double value)
            => (byte)Math.Clamp((int)Math.Round(value), 0, 255);

        private static void SetColor(ResourceDictionary dictionary, string key, Color color)
        {
            if (dictionary.Contains(key))
                dictionary[key] = color;
        }

        private static void SetBrush(ResourceDictionary dictionary, string key, Color color)
        {
            if (dictionary.Contains(key))
                dictionary[key] = new SolidColorBrush(color);
        }

        private static void SetGradient(ResourceDictionary dictionary, string key, params Color[] colors)
        {
            if (!dictionary.Contains(key) || colors.Length == 0)
                return;

            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            for (int i = 0; i < colors.Length; i++)
            {
                double offset = colors.Length == 1 ? 0 : (double)i / (colors.Length - 1);
                brush.GradientStops.Add(new GradientStop(colors[i], offset));
            }

            dictionary[key] = brush;
        }

        private sealed record AccentPalette(
            Color Accent,
            Color Border,
            Color Surface,
            Color TileBackground,
            Color TitleBackground,
            Color SecondaryText,
            Color Common,
            Color Rare,
            Color Epic,
            Color Mythic,
            Color MapBackground,
            Color MapNode,
            Color Boss,
            Color Cave,
            Color World,
            Color Panel,
            Color PanelDark,
            Color Active,
            Color NavHover,
            Color MainPanelA,
            Color MainPanelB,
            Color PortraitA,
            Color PortraitB,
            Color PortraitC,
            Color HudA,
            Color HudB,
            Color HudC,
            Color CrestA,
            Color CrestB,
            Color WindowB,
            Color Background,
            Color WindowMid,
            Color WindowEnd);
    }
}
