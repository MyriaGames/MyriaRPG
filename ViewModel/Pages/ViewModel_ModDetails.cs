using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Services.Mods;
using Myria.Wpf.Utils;
using System.Globalization;

namespace Myria.Wpf.ViewModel.Pages
{
    // ── Per-setting entry ─────────────────────────────────────────────────────

    public class ModSettingEntryVm : BaseViewModel
    {
        public string Key         { get; }
        public string Label       { get; }
        public string Description { get; }
        public string Type        { get; }
        public double Min         { get; }
        public double Max         { get; }
        public double Step        { get; }

        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

        public Visibility BoolVisibility        => Type == "bool"        ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SliderVisibility     => Type == "slider"      ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorVisibility      => Type == "colorSlider" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ColorPickerVisibility=> Type == "colorPicker" ? Visibility.Visible : Visibility.Collapsed;

        private string _current;
        public string CurrentValue
        {
            get => _current;
            set
            {
                if (!SetProperty(ref _current, value)) return;
                OnPropertyChanged(nameof(BoolValue));
                OnPropertyChanged(nameof(SliderValue));
                OnPropertyChanged(nameof(ColorPreview));
                // Keep the HSV picker in sync when hex is typed manually.
                if (Type == "colorPicker" && !_updatingHsv)
                    SyncHsvFromHex();
            }
        }

        // ── Typed accessors ────────────────────────────────────────────────────

        public bool BoolValue
        {
            get => string.Equals(_current, "true", StringComparison.OrdinalIgnoreCase);
            set => CurrentValue = value ? "true" : "false";
        }

        public double SliderValue
        {
            get => double.TryParse(_current, System.Globalization.NumberStyles.Any,
                                   System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : Min;
            set => CurrentValue = ((int)Math.Round(value)).ToString();
        }

        /// <summary>Live color preview brush — updates as the user types a valid hex code.</summary>
        public Brush ColorPreview
        {
            get
            {
                try   { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(_current)); }
                catch { return Brushes.Transparent; }
            }
        }

        // ── HSV color picker state ─────────────────────────────────────────────

        private bool   _updatingHsv;
        private double _hue        = 0;
        private double _saturation = 100;
        private double _brightness = 100;

        /// <summary>Hue 0–360.</summary>
        public double HueValue
        {
            get => _hue;
            set
            {
                if (!SetProperty(ref _hue, Math.Clamp(value, 0, 360))) return;
                if (!_updatingHsv) ApplyHsvToHex();
                NotifyGradients();
            }
        }

        /// <summary>Saturation 0–100.</summary>
        public double SaturationValue
        {
            get => _saturation;
            set
            {
                if (!SetProperty(ref _saturation, Math.Clamp(value, 0, 100))) return;
                if (!_updatingHsv) ApplyHsvToHex();
                NotifyGradients();
            }
        }

        /// <summary>Brightness 0–100.</summary>
        public double BrightnessValue
        {
            get => _brightness;
            set
            {
                if (!SetProperty(ref _brightness, Math.Clamp(value, 0, 100))) return;
                if (!_updatingHsv) ApplyHsvToHex();
                NotifyGradients();
            }
        }

        // ── Gradient brushes for slider tracks ────────────────────────────────

        /// <summary>Static full-spectrum hue gradient.</summary>
        public static Brush HueGradient { get; } = BuildHueGradient();

        /// <summary>White → fully-saturated current hue; updates when hue or brightness changes.</summary>
        public Brush SaturationGradient => new LinearGradientBrush(
            Colors.White,
            HsvToColor(_hue, 1, _brightness / 100.0),
            new Point(0, 0.5), new Point(1, 0.5));

        /// <summary>Black → fully-bright current hue+saturation; updates when hue or saturation changes.</summary>
        public Brush BrightnessGradient => new LinearGradientBrush(
            Colors.Black,
            HsvToColor(_hue, _saturation / 100.0, 1),
            new Point(0, 0.5), new Point(1, 0.5));

        // ── HSV ↔ hex helpers ─────────────────────────────────────────────────

        private void ApplyHsvToHex()
        {
            var c = HsvToColor(_hue, _saturation / 100.0, _brightness / 100.0);
            CurrentValue = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        private void SyncHsvFromHex()
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(_current);
                _updatingHsv = true;
                (var h, var s, var v) = ColorToHsv(c);
                HueValue        = h;
                SaturationValue = s * 100;
                BrightnessValue = v * 100;
                _updatingHsv = false;
            }
            catch { _updatingHsv = false; }
        }

        private void NotifyGradients()
        {
            OnPropertyChanged(nameof(SaturationGradient));
            OnPropertyChanged(nameof(BrightnessGradient));
        }

        private static Brush BuildHueGradient()
        {
            var gb = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
            for (int i = 0; i <= 12; i++)
                gb.GradientStops.Add(new GradientStop(HsvToColor(i * 30.0, 1, 1), i / 12.0));
            gb.Freeze();
            return gb;
        }

        private static Color HsvToColor(double h, double s, double v)
        {
            if (s <= 0) { byte w = (byte)(v * 255); return Color.FromRgb(w, w, w); }
            h = ((h % 360) + 360) % 360 / 60.0;
            int   i = (int)Math.Floor(h);
            double f = h - i;
            double p = v * (1 - s), q = v * (1 - s * f), t = v * (1 - s * (1 - f));
            var (r, g, b) = i switch
            {
                0 => (v, t, p), 1 => (q, v, p), 2 => (p, v, t),
                3 => (p, q, v), 4 => (t, p, v), _ => (v, p, q)
            };
            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private static (double h, double s, double v) ColorToHsv(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            double delta = max - min, v = max, s = max == 0 ? 0 : delta / max, h = 0;
            if (delta != 0)
            {
                h = max == r ? 60 * (((g - b) / delta % 6 + 6) % 6) :
                    max == g ? 60 * ((b - r) / delta + 2) :
                               60 * ((r - g) / delta + 4);
            }
            return (h, s, v);
        }

        public ModSettingEntryVm(ModSettingDefinition def, string currentValue)
        {
            Key         = def.Key;
            Label       = def.Label;
            Description = def.Description;
            Type        = def.Type;
            Min         = def.Min;
            Max         = def.Max;
            Step        = def.Step > 0 ? def.Step : 1;
            _current    = currentValue;
            if (Type == "colorPicker") SyncHsvFromHex();
        }
    }

    // ── Details panel ─────────────────────────────────────────────────────────

    public class ModDetailsVm : BaseViewModel
    {
        private readonly LoadedMod _mod;

        public string ModName        { get; }
        public string ModId          { get; }
        public string ModVersion     { get; }
        public string ModAuthor      { get; }
        public string ModDescription { get; }
        public bool   HasPlugin      { get; }
        public bool   HasSettings    => Settings.Count > 0;

        public ObservableCollection<ModSettingEntryVm> Settings { get; } = new();

        public ICommand SaveCommand  { get; }
        public ICommand ResetCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? CloseRequested;

        public ModDetailsVm(LoadedMod mod, Action closeCallback, Action reloadCallback)
        {
            _mod            = mod;
            _reloadCallback = reloadCallback;
            ModName         = string.IsNullOrWhiteSpace(mod.Manifest.Name) ? mod.Manifest.Id : mod.Manifest.Name;
            ModId           = mod.Manifest.Id;
            ModVersion      = mod.Manifest.Version;
            ModAuthor       = mod.Manifest.Author;
            ModDescription  = mod.Manifest.Description;
            HasPlugin       = mod.HasPlugin;

            foreach (var def in mod.Settings)
            {
                mod.SettingValues.TryGetValue(def.Key, out var current);
                Settings.Add(new ModSettingEntryVm(def, current ?? def.DefaultValue));
            }

            SaveCommand  = new RelayCommand(Save);
            ResetCommand = new RelayCommand(Reset);
            CloseCommand = new RelayCommand(closeCallback);
        }

        private readonly Action _reloadCallback;

        private void Save()
        {
            foreach (var entry in Settings)
                ModManifestStore.WriteSettingValue(_mod, entry.Key, entry.CurrentValue);

            _reloadCallback();
        }

        private void Reset()
        {
            foreach (var entry in Settings)
            {
                var def = _mod.Settings.FirstOrDefault(d => d.Key == entry.Key);
                if (def != null) entry.CurrentValue = def.DefaultValue;
            }
        }
    }
}
