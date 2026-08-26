using System.Globalization;
using System.Windows.Media;
using System.Windows.Threading;
using Myria.Lib.Core.Systems.Mods;

namespace Myria.Wpf.Services.Mods
{
    internal static class WpfModVisualEffectRunner
    {
        private static readonly List<DispatcherTimer> _timers = new();

        public static void Apply(ModLoadContext context)
        {
            StopAll();

            if (!ThemeManager.IsDark || ThemeManager.CurrentThemeDictionary == null)
                return;

            foreach (var mod in context.ActiveMods)
            {
                foreach (var effect in mod.VisualEffects)
                {
                    if (!IsEffectEnabled(mod, effect))
                        continue;

                    if (IsHueCycle(effect))
                        StartHueCycle(mod, effect);
                }
            }
        }

        public static void StopAll()
        {
            foreach (var timer in _timers)
                timer.Stop();

            _timers.Clear();
        }

        private static bool IsEffectEnabled(LoadedMod mod, ModVisualEffectDefinition effect)
        {
            if (string.IsNullOrWhiteSpace(effect.EnabledSetting))
                return true;

            return mod.SettingValues.TryGetValue(effect.EnabledSetting, out var value)
                && bool.TryParse(value, out var enabled)
                && enabled;
        }

        private static bool IsHueCycle(ModVisualEffectDefinition effect)
            => string.Equals(effect.Target, WpfThemeEffectTargets.ThemeAccentPalette, StringComparison.OrdinalIgnoreCase)
               && string.Equals(effect.Effect, "hueCycle", StringComparison.OrdinalIgnoreCase);

        private static void StartHueCycle(LoadedMod mod, ModVisualEffectDefinition effect)
        {
            if (!TryGetColor(mod, effect.SourceSetting, out var baseColor))
                return;

            var startedAt = DateTime.UtcNow;
            var baseHue = GetHue(baseColor);
            var speed = effect.Speed <= 0 ? 0.2 : effect.Speed;

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };

            timer.Tick += (_, _) =>
            {
                if (!ThemeManager.IsDark || ThemeManager.CurrentThemeDictionary == null)
                    return;

                double elapsedSeconds = (DateTime.UtcNow - startedAt).TotalSeconds;
                double hue = baseHue + elapsedSeconds * speed * 360d;
                double brightness = TryGetDouble(mod, effect.BrightnessSetting, out var configuredBrightness)
                    ? configuredBrightness / 100d
                    : 1d;

                WpfThemeEffectTargets.TryApply(
                    effect.Target,
                    ThemeManager.CurrentThemeDictionary,
                    FromHue(hue),
                    brightness);
            };

            _timers.Add(timer);
            timer.Start();
        }

        private static bool TryGetColor(LoadedMod mod, string settingKey, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(settingKey)
                || !mod.SettingValues.TryGetValue(settingKey, out var value)
                || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                color = (Color)ColorConverter.ConvertFromString(value.Trim());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetDouble(LoadedMod mod, string settingKey, out double result)
        {
            result = 0;
            return !string.IsNullOrWhiteSpace(settingKey)
                && mod.SettingValues.TryGetValue(settingKey, out var value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static double GetHue(Color color)
        {
            double r = color.R / 255d;
            double g = color.G / 255d;
            double b = color.B / 255d;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            if (delta == 0) return 0;

            double hue = max == r
                ? 60 * (((g - b) / delta) % 6)
                : max == g
                    ? 60 * (((b - r) / delta) + 2)
                    : 60 * (((r - g) / delta) + 4);

            return hue < 0 ? hue + 360 : hue;
        }

        private static Color FromHue(double hue)
        {
            hue = ((hue % 360) + 360) % 360;
            double c = 1;
            double x = c * (1 - Math.Abs((hue / 60d % 2) - 1));

            (double r, double g, double b) = hue switch
            {
                < 60 => (c, x, 0d),
                < 120 => (x, c, 0d),
                < 180 => (0d, c, x),
                < 240 => (0d, x, c),
                < 300 => (x, 0d, c),
                _ => (c, 0d, x)
            };

            return Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));
        }

        private static byte ToByte(double value)
            => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
    }
}
