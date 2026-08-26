using System.IO;
using System.Windows;
using Myria.Lib.Core.Systems.Mods;

namespace Myria.Wpf.Services
{
    public static class ThemeManager
    {
        private static readonly Uri LightUri = new("Assets/Light.xaml", UriKind.Relative);
        private static readonly Uri DarkUri = new("Assets/Dark.xaml", UriKind.Relative);
        private static string? _lightOverridePath;
        private static string? _darkOverridePath;
        private static LoadedMod? _lightOverrideMod;
        private static LoadedMod? _darkOverrideMod;
        private static Action<bool, ResourceDictionary>? _themeResourcePostProcessor;
        private static ResourceDictionary? _currentThemeDictionary;

        public static bool IsDark { get; private set; }
        public static ResourceDictionary? CurrentThemeDictionary => _currentThemeDictionary;

        public static void SetThemeResourceOverrides(
            string? lightOverridePath,
            string? darkOverridePath,
            LoadedMod? lightOverrideMod = null,
            LoadedMod? darkOverrideMod = null)
        {
            _lightOverridePath = lightOverridePath;
            _darkOverridePath = darkOverridePath;
            _lightOverrideMod = lightOverrideMod;
            _darkOverrideMod = darkOverrideMod;
        }

        public static void SetThemeResourcePostProcessor(Action<bool, ResourceDictionary>? postProcessor)
        {
            _themeResourcePostProcessor = postProcessor;
        }

        public static void Apply(bool dark)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var newDict = LoadThemeDictionary(dark);
            _themeResourcePostProcessor?.Invoke(dark, newDict);

            // replace the first theme dictionary, or add if missing
            if (dictionaries.Count > 0)
                dictionaries[0] = newDict;
            else
                dictionaries.Add(newDict);

            _currentThemeDictionary = newDict;
            IsDark = dark;
        }

        private static ResourceDictionary LoadThemeDictionary(bool dark)
        {
            var overrideMod = dark ? _darkOverrideMod : _lightOverrideMod;
            try
            {
                return ResourceDictionaryLoader.Load(
                    GetThemeUri(dark),
                    dark ? "dark theme" : "light theme");
            }
            catch (Exception ex) when (overrideMod != null)
            {
                ModLoader.UnloadModAfterError(
                    overrideMod,
                    dark ? "Loading dark theme override" : "Loading light theme override",
                    ex,
                    nameof(ThemeManager));

                if (dark)
                {
                    _darkOverridePath = null;
                    _darkOverrideMod = null;
                }
                else
                {
                    _lightOverridePath = null;
                    _lightOverrideMod = null;
                }

                return ResourceDictionaryLoader.Load(
                    dark ? DarkUri : LightUri,
                    dark ? "base dark theme" : "base light theme");
            }
        }

        private static Uri GetThemeUri(bool dark)
        {
            string? overridePath = dark ? _darkOverridePath : _lightOverridePath;
            return !string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath)
                ? new Uri(Path.GetFullPath(overridePath), UriKind.Absolute)
                : dark ? DarkUri : LightUri;
        }
    }
}
