using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Model;
using Myria.Wpf.Services.Mods;
using Myria.Wpf.Utils;
using GameLocalization = Myria.Lib.Core.Systems.Localization;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_SettingsModDetails : BaseViewModel
    {
        private const string ModsDirectory = "Data/Mods";

        private readonly LoadedMod _mod;

        private string _tblBack = string.Empty;
        private string _tblSettings = string.Empty;
        private string _tblNoSettings = string.Empty;
        private string _tblFolder = string.Empty;
        private string _tblId = string.Empty;

        public ObservableCollection<ModSettingRowVm> Settings { get; } = new();

        [LocalizedKey("pg.mods.details.back")]
        public string TblBack
        {
            get => _tblBack;
            set { _tblBack = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.details.settings")]
        public string TblSettings
        {
            get => _tblSettings;
            set { _tblSettings = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.details.no_settings")]
        public string TblNoSettings
        {
            get => _tblNoSettings;
            set { _tblNoSettings = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.details.folder")]
        public string TblFolder
        {
            get => _tblFolder;
            set { _tblFolder = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.mods.details.id")]
        public string TblId
        {
            get => _tblId;
            set { _tblId = value; OnPropertyChanged(); }
        }

        public string Name { get; }
        public string VersionText { get; }
        public string AuthorText { get; }
        public string Description { get; }
        public string FolderName { get; }
        public string Id { get; }
        public string TypeLabel => GameLocalization.T(_mod.IsVisualOnly ? "pg.mods.type.visual" : "pg.mods.type.gameplay");
        public string ActiveLabel => GameLocalization.T(_mod.IsEnabled ? "pg.mods.active.on" : "pg.mods.active.off");

        public Visibility EmptyVisibility => Settings.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SettingsVisibility => Settings.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        public ICommand BackCommand { get; }
        public Action? GoBack { get; set; }

        public ViewModel_SettingsModDetails(LoadedMod mod)
        {
            _mod = mod;
            LocalizationAutoWire.Wire(this);

            Name = string.IsNullOrWhiteSpace(mod.Manifest.Name) ? mod.Manifest.Id : mod.Manifest.Name;
            VersionText = string.IsNullOrWhiteSpace(mod.Manifest.Version) ? "" : $"v{mod.Manifest.Version}";
            AuthorText = mod.Manifest.Author;
            Description = mod.Manifest.Description;
            FolderName = System.IO.Path.GetFileName(mod.Directory.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
            Id = mod.Manifest.Id;

            BackCommand = new RelayCommand(() => GoBack?.Invoke());
            BuildSettings();
        }

        protected override void OnLanguageChanged(object? sender, EventArgs e)
        {
            base.OnLanguageChanged(sender, e);
            OnPropertyChanged(nameof(TypeLabel));
            OnPropertyChanged(nameof(ActiveLabel));
        }

        private void BuildSettings()
        {
            Settings.Clear();

            foreach (var definition in _mod.Settings.Where(s => !string.IsNullOrWhiteSpace(s.Key)))
            {
                string value = _mod.SettingValues.TryGetValue(definition.Key, out var storedValue)
                    ? storedValue
                    : definition.DefaultValue;

                Settings.Add(new ModSettingRowVm(_mod, definition, value, SaveSetting));
            }

            OnPropertyChanged(nameof(EmptyVisibility));
            OnPropertyChanged(nameof(SettingsVisibility));
        }

        private static void SaveSetting(LoadedMod mod, string key, string value)
        {
            ModManifestStore.WriteSettingValue(mod, key, value);
            ModLoader.Load(ModsDirectory);
        }
    }

    public class ModSettingRowVm : BaseViewModel
    {
        private readonly LoadedMod _mod;
        private readonly Action<LoadedMod, string, string> _valueChanged;
        private string _value;

        public string Key { get; }
        public string Label { get; }
        public string Description { get; }
        public string Type { get; }
        public double Min { get; }
        public double Max { get; }
        public double Step { get; }

        public string Value
        {
            get => _value;
            set
            {
                if (!SetProperty(ref _value, value)) return;
                OnPropertyChanged(nameof(BooleanValue));
                OnPropertyChanged(nameof(ColorHueValue));
                OnPropertyChanged(nameof(ColorPreviewBrush));
                _valueChanged(_mod, Key, value);
            }
        }

        public bool BooleanValue
        {
            get => bool.TryParse(Value, out var result) && result;
            set => Value = value.ToString().ToLowerInvariant();
        }

        public double NumericValue
        {
            get => double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : Min;
            set => Value = value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public double ColorHueValue
        {
            get => HexToHue(Value);
            set => Value = HueToHex(value);
        }

        public Brush ColorPreviewBrush
        {
            get
            {
                try
                {
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(Value));
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
        }

        public Visibility TextVisibility => Type is "text" or "" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility NumberVisibility => Type == "number" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorVisibility => Type == "color" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SliderVisibility => Type == "slider" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorSliderVisibility => Type is "colorslider" or "hue" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BooleanVisibility => Type is "bool" or "boolean" ? Visibility.Visible : Visibility.Collapsed;

        public ModSettingRowVm(
            LoadedMod mod,
            ModSettingDefinition definition,
            string value,
            Action<LoadedMod, string, string> valueChanged)
        {
            _mod = mod;
            Key = definition.Key;
            Label = string.IsNullOrWhiteSpace(definition.Label) ? definition.Key : definition.Label;
            Description = definition.Description;
            Type = definition.Type.Trim().ToLowerInvariant();
            Min = definition.Min;
            Max = definition.Max <= definition.Min ? definition.Min + 100 : definition.Max;
            Step = definition.Step <= 0 ? 1 : definition.Step;
            _value = value;
            _valueChanged = valueChanged;
        }

        private static string HueToHex(double hue)
        {
            hue = ((hue % 360) + 360) % 360;
            double c = 1;
            double x = c * (1 - Math.Abs((hue / 60d % 2) - 1));
            double m = 0;

            (double r, double g, double b) = hue switch
            {
                < 60 => (c, x, 0d),
                < 120 => (x, c, 0d),
                < 180 => (0d, c, x),
                < 240 => (0d, x, c),
                < 300 => (x, 0d, c),
                _ => (c, 0d, x)
            };

            return $"#{ToByte(r + m):X2}{ToByte(g + m):X2}{ToByte(b + m):X2}";
        }

        private static double HexToHue(string value)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(value);
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
            catch
            {
                return 0;
            }
        }

        private static byte ToByte(double value)
            => (byte)Math.Clamp((int)Math.Round(value * 255), 0, 255);
    }
}
