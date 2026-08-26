using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.Pages.Settings;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_SettingsPage : BaseViewModel
    {
        private string _title = string.Empty;
        private string _btnLanguage = string.Empty;
        private string _btnVisuals = string.Empty;
        private string _btnBack = string.Empty;
        private string _btnKeybindings = string.Empty;
        private string _btnMods = string.Empty;

        [LocalizedKey("pg.settings.title")]
        public string Title 
        { 
            get => _title;
            private set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        [LocalizedKey("pg.settings.language")]
        public string btnLanguage
        {
            get => _btnLanguage;
            private set { _btnLanguage = value; OnPropertyChanged(nameof(btnLanguage)); }
        }

        [LocalizedKey("pg.settings.visuals")]
        public string btnVisuals 
        { 
            get => _btnVisuals;
            private set { _btnVisuals = value; OnPropertyChanged(nameof(btnVisuals)); }
        }
        [LocalizedKey("app.general.UI.back")]
        public string TblBack
        {
            get => _btnBack;
            set { _btnBack = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.keybindings")]
        public string BtnKeybindings
        {
            get => _btnKeybindings;
            set { _btnKeybindings = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.settings.mods")]
        public string BtnMods
        {
            get => _btnMods;
            set { _btnMods = value; OnPropertyChanged(); }
        }
        public ICommand Language { get; }
        public ICommand Visuals { get; }
        public ICommand KeybindingsCommand { get; }
        public ICommand ModsCommand { get; }
        public ICommand BackCommand { get; }
        public Visibility BackButtonVisibility => Navigation.Current.IsInGame ? Visibility.Visible : Visibility.Collapsed;

        public ViewModel_SettingsPage()
        {
            Language = new RelayCommand(LanguageAction);
            Visuals = new RelayCommand(VisualsAction);
            KeybindingsCommand = new RelayCommand(KeybindingsAction);
            ModsCommand = new RelayCommand(ModsAction);
            BackCommand = new RelayCommand(BackAction);
            LocalizationAutoWire.Wire(this);
        }
        private void VisualsAction()
        {
            Navigation.Current.Navigate(Nav.SettingsVisuals);
        }
        private void LanguageAction()
        {
            Navigation.Current.Navigate(Nav.SettingsLanguage);
        }
        public void KeybindingsAction()
        {
            Navigation.Current.Navigate(Nav.SettingsKeybindings);
        }

        public void ModsAction()
        {
            Navigation.Current.Navigate(Nav.SettingsMods);
        }

        private void BackAction()
        {
            Navigation.Current.Navigate(new Page_IngameMenu());
        }

    }

}
