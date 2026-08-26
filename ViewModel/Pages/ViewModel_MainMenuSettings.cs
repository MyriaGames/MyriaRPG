using Myria.Wpf.Model;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.Pages.Settings;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_MainMenuSettings : BaseViewModel
    {
        private string _tblVisuals;
        private string _tblKeybindings;
        private string _tblMods;
        private string _tblGame;

        [LocalizedKey("pg.settings.visuals")]
        public string TblVisuals
        {
            get => _tblVisuals;
            set { _tblVisuals = value; OnPropertyChanged(nameof(TblVisuals)); }
        }

        [LocalizedKey("pg.settings.keybindings")]
        public string TblKeybindings
        {
            get => _tblKeybindings;
            set { _tblKeybindings = value; OnPropertyChanged(nameof(TblKeybindings)); }
        }

        [LocalizedKey("pg.settings.mods")]
        public string TblMods
        {
            get => _tblMods;
            set { _tblMods = value; OnPropertyChanged(nameof(TblMods)); }
        }

        [LocalizedKey("pg.settings.game")]
        public string TblGame
        {
            get => _tblGame;
            set { _tblGame = value; OnPropertyChanged(nameof(TblGame)); }
        }

        private string _tblVersionLabel;

        [LocalizedKey("pg.settings.version")]
        public string TblVersionLabel
        {
            get => _tblVersionLabel;
            set { _tblVersionLabel = value; OnPropertyChanged(nameof(TblVersionLabel)); }
        }

        public string VersionText { get; } = FormatVersion();

        public Action<Page>? NavigateTo { get; set; }

        public ICommand ShowVisuals { get; }
        public ICommand ShowKeybindings { get; }
        public ICommand ShowMods { get; }
        public ICommand ShowGame { get; }

        public ViewModel_MainMenuSettings()
        {
            LocalizationAutoWire.Wire(this);
            ShowVisuals     = new RelayCommand(() => NavigateTo?.Invoke(new Page_SettingsVisuals()));
            ShowKeybindings = new RelayCommand(() => NavigateTo?.Invoke(new Page_Keybindings()));
            ShowMods        = new RelayCommand(() => NavigateTo?.Invoke(new Page_SettingsMods()));
            ShowGame        = new RelayCommand(() => NavigateTo?.Invoke(new Page_SettingsGame()));
        }

        // Trims the trailing ".0" revision segment (always 0 for our builds - AssemblyVersion is
        // "Major.Minor.Build.0") so this matches the version testers actually see in the
        // installer filename / GitHub release tag, e.g. "0.2.4" rather than "0.2.4.0".
        private static string FormatVersion()
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (v is null) return "?";
            return v.Revision <= 0 ? $"{v.Major}.{v.Minor}.{v.Build}" : v.ToString();
        }
    }
}
