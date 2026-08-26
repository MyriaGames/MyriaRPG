using Myria.Lib.Core.Models.Settings;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.Pages.Game;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_SettingsVisuals : BaseViewModel
    {
        private string darkMode = string.Empty;
        private string darkMidnightMode = string.Empty;
        private string fullScreen = string.Empty;
        private string fullScreenCheck = string.Empty;
        private string _tblLanguage = string.Empty;
        private string _tblVisualsTitle = string.Empty;
        private string _tblVisualsDescription = string.Empty;
        private string _tblGameLanguage = string.Empty;
        private string _tblAppearance = string.Empty;
        private string _tblLightMode = string.Empty;
        private string _tblDisplay = string.Empty;
        private string _tblWindowed = string.Empty;
        private LanguageOption _selectedLanguage = null!;
        private bool _canWriteSettings;

        [LocalizedKey("pg.settings.visuals.title")]
        public string TblVisualsTitle
        {
            get => _tblVisualsTitle;
            set { _tblVisualsTitle = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.visuals.description")]
        public string TblVisualsDescription
        {
            get => _tblVisualsDescription;
            set { _tblVisualsDescription = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.visuals.game_language")]
        public string TblGameLanguage
        {
            get => _tblGameLanguage;
            set { _tblGameLanguage = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.visuals.appearance")]
        public string TblAppearance
        {
            get => _tblAppearance;
            set { _tblAppearance = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.visuals.lightmode")]
        public string TblLightMode
        {
            get => _tblLightMode;
            set { _tblLightMode = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.visuals.display")]
        public string TblDisplay
        {
            get => _tblDisplay;
            set { _tblDisplay = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.visuals.windowed")]
        public string TblWindowed
        {
            get => _tblWindowed;
            set { _tblWindowed = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.language")]
        public string TblLanguage
        {
            get => _tblLanguage;
            set { _tblLanguage = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.visuals.darkmode")]
        public string DarkMode
        {
            get => darkMode;
            set { darkMode = value; OnPropertyChanged(nameof(DarkMode)); }
        }

        [LocalizedKey("pg.settings.visuals.darkmodecheck")]
        public string DarkMidnightMode
        {
            get => darkMidnightMode;
            set { darkMidnightMode = value; OnPropertyChanged(nameof(DarkMidnightMode)); }
        }

        [LocalizedKey("pg.settings.visuals.fullscreen")]
        public string FullScreen
        {
            get => fullScreen;
            set { fullScreen = value; OnPropertyChanged(nameof(FullScreen)); }
        }

        [LocalizedKey("pg.settings.visuals.fullscreencheck")]
        public string FullScreenCheck
        {
            get => fullScreenCheck;
            set { fullScreenCheck = value; OnPropertyChanged(nameof(FullScreenCheck)); }
        }

        private bool _darkModeSetter = Settings.Current.VisualSettings.DarkMode;
        public bool DarkModeSetter
        {
            get => _darkModeSetter;
            set
            {
                if (_darkModeSetter == value)
                    return;

                _darkModeSetter = value;
                if (_canWriteSettings)
                    SetDarkmode();

                OnPropertyChanged();
                OnPropertyChanged(nameof(LightModeSetter));
            }
        }

        public bool LightModeSetter
        {
            get { return !_darkModeSetter; }
            set { if (value) DarkModeSetter = false; }
        }

        private bool _fullScreenSetter = Settings.Current.VisualSettings.FullScreen;
        public bool FullScreenSetter
        {
            get => _fullScreenSetter;
            set
            {
                if (_fullScreenSetter == value)
                    return;

                _fullScreenSetter = value;
                if (_canWriteSettings)
                    SetFullScreen();

                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowedModeSetter));
            }
        }

        public bool WindowedModeSetter
        {
            get { return !FullScreenSetter; }
            set { if (value) FullScreenSetter = false; }
        }

        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;
                _selectedLanguage = value;

                if (_canWriteSettings)
                {
                    if (value.IsBuiltIn)
                    {
                        Settings.Current.LanguageSettings.Local        = value.Id == "de" ? GameLanguage.De : GameLanguage.En;
                        Settings.Current.LanguageSettings.ModLanguageId = null;
                    }
                    else
                    {
                        Settings.Current.LanguageSettings.ModLanguageId = value.Id;
                    }
                    SettingsService.Save();
                    Localization.Load(value.Id);
                    ViewModel_PageRoom.RefreshLocalisation();
                }

                OnPropertyChanged();
            }
        }

        public List<LanguageOption> Languages { get; } =
            Enum.GetValues<GameLanguage>()
                .Select(LanguageOption.ForBuiltIn)
                .Concat(ModLoader.GetModLanguages().Select(ml => LanguageOption.ForMod(ml.Def)))
                .ToList();

        public ViewModel_SettingsVisuals()
        {
            LocalizationAutoWire.Wire(this);
        }

        public void EnableSettingsWrites()
        {
            var currentId = Settings.Current.LanguageSettings.EffectiveLanguageId;
            _selectedLanguage = Languages.FirstOrDefault(l =>
                    l.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase))
                ?? Languages.First();
            _darkModeSetter = Settings.Current.VisualSettings.DarkMode;
            _fullScreenSetter = Settings.Current.VisualSettings.FullScreen;

            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(DarkModeSetter));
            OnPropertyChanged(nameof(LightModeSetter));
            OnPropertyChanged(nameof(FullScreenSetter));
            OnPropertyChanged(nameof(WindowedModeSetter));

            _canWriteSettings = true;
        }

        private void SetDarkmode()
        {
            Settings.Current.VisualSettings.DarkMode = DarkModeSetter;
            SettingsService.Save();
            ThemeManager.Apply(Settings.Current.VisualSettings.DarkMode);
        }

        private void SetFullScreen()
        {
            Settings.Current.VisualSettings.FullScreen = FullScreenSetter;
            SettingsService.Save();
            MainWindow.Instance.ApplyWindowMode(FullScreenSetter);
        }
    }
}
