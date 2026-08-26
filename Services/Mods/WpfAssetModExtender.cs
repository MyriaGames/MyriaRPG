using System.IO;
using System.Windows;
using System.Windows.Media;
using Myria.Lib.Core.Systems.Mods;

namespace Myria.Wpf.Services.Mods
{
    public sealed class WpfAssetModExtender : ModLoaderExtender
    {
        private const string LightAsset = "Assets/Light.xaml";
        private const string DarkAsset = "Assets/Dark.xaml";
        private const string LayoutAsset = "Assets/Layout.xaml";
        private const string IconsAsset = "Assets/Icons.xaml";

        private static readonly Uri LayoutUri = new(LayoutAsset, UriKind.Relative);
        private static readonly Uri IconsUri = new(IconsAsset, UriKind.Relative);
        private static LoadedMod? _darkThemeMod;

        public override void AfterModsLoaded(ModLoadContext context)
        {
            WpfModVisualEffectRunner.StopAll();

            ApplyApplicationAssetDictionaries(context.ActiveMods, context.MultiplayerMode);

            var lightOverride = FindOverride(context, LightAsset);
            var darkOverride = FindOverride(context, DarkAsset);
            _darkThemeMod = darkOverride.Mod;

            ThemeManager.SetThemeResourceOverrides(
                lightOverride.Path,
                darkOverride.Path,
                lightOverride.Mod,
                darkOverride.Mod);
            ThemeManager.SetThemeResourcePostProcessor(ApplyThemeSettings);
            ThemeManager.Apply(ThemeManager.IsDark);

            WpfModVisualEffectRunner.Apply(context);
        }

        public static void EnsureApplicationAssetDictionaries()
        {
            ApplyApplicationAssetDictionaries(ModLoader.ActiveMods, ModLoader.MultiplayerMode);
        }

        private static void ApplyApplicationAssetDictionaries(IReadOnlyList<LoadedMod> activeMods, bool multiplayerMode)
        {
            ReplaceDictionary(LayoutAsset, FindOverride(activeMods, multiplayerMode, LayoutAsset), LayoutUri, 1);
            ReplaceDictionary(IconsAsset, FindOverride(activeMods, multiplayerMode, IconsAsset), IconsUri, 2);
        }

        private static ModAssetOverride FindOverride(ModLoadContext context, string relativePath)
            => FindOverride(context.ActiveMods, context.MultiplayerMode, relativePath);

        private static ModAssetOverride FindOverride(IReadOnlyList<LoadedMod> activeMods, bool multiplayerMode, string relativePath)
        {
            foreach (var mod in activeMods.Reverse())
            {
                if (multiplayerMode && !mod.IsVisualOnly)
                    continue;

                string candidate = Path.Combine(mod.Directory, relativePath);
                if (ModLoader.ActiveMods.Contains(mod) && File.Exists(candidate))
                    return new ModAssetOverride(Path.GetFullPath(candidate), mod);
            }

            return new ModAssetOverride(null, null);
        }

        private static void ApplyThemeSettings(bool dark, ResourceDictionary dictionary)
        {
            if (!dark || _darkThemeMod == null || !ModLoader.ActiveMods.Contains(_darkThemeMod))
                return;

            var values = _darkThemeMod.SettingValues;
            if (!TryGetColor(values, "accentColor", out var accent))
                return;

            double brightness = TryGetDouble(values, "accentBrightness", out var configuredBrightness)
                ? configuredBrightness
                : 100;

            WpfThemeEffectTargets.TryApply(
                WpfThemeEffectTargets.ThemeAccentPalette,
                dictionary,
                accent,
                brightness / 100d);
        }

        private static bool TryGetColor(Dictionary<string, string> values, string key, out Color color)
        {
            color = default;
            if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                return false;

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

        private static bool TryGetDouble(Dictionary<string, string> values, string key, out double result)
        {
            result = 0;
            return values.TryGetValue(key, out var value)
                && double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        private static void ReplaceDictionary(string assetPath, ModAssetOverride assetOverride, Uri fallbackUri, int fallbackIndex)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var source = !string.IsNullOrWhiteSpace(assetOverride.Path) && File.Exists(assetOverride.Path)
                ? new Uri(assetOverride.Path, UriKind.Absolute)
                : fallbackUri;

            var replacement = LoadDictionaryOrFallback(assetPath, assetOverride, source, fallbackUri);
            int index = FindDictionaryIndex(assetPath);

            if (index >= 0)
            {
                dictionaries[index] = replacement;
                return;
            }

            if (fallbackIndex >= 0 && fallbackIndex <= dictionaries.Count)
                dictionaries.Insert(fallbackIndex, replacement);
            else
                dictionaries.Add(replacement);
        }

        private static ResourceDictionary LoadDictionaryOrFallback(
            string assetPath,
            ModAssetOverride assetOverride,
            Uri source,
            Uri fallbackUri)
        {
            try
            {
                return ResourceDictionaryLoader.Load(source, assetPath);
            }
            catch (Exception ex) when (assetOverride.Mod != null)
            {
                ModLoader.UnloadModAfterError(
                    assetOverride.Mod,
                    $"Loading {assetPath}",
                    ex,
                    nameof(WpfAssetModExtender));

                return ResourceDictionaryLoader.Load(fallbackUri, $"base {assetPath}");
            }
        }

        private static int FindDictionaryIndex(string assetPath)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            for (int i = 0; i < dictionaries.Count; i++)
            {
                var source = dictionaries[i].Source;
                if (source != null && SourceMatches(source, assetPath))
                    return i;
            }

            return -1;
        }

        private static bool SourceMatches(Uri source, string assetPath)
        {
            string expected = Normalize(assetPath);
            string actual = Normalize(source.IsAbsoluteUri ? source.LocalPath : source.OriginalString);
            return actual.EndsWith(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string path)
            => path.Replace('\\', '/').TrimStart('/');

        private sealed record ModAssetOverride(string? Path, LoadedMod? Mod);

    }
}
