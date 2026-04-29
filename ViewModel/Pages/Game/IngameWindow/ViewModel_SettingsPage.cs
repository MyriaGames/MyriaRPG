using MyriaLib.Services;
using MyriaRPG.Model;
using MyriaRPG.Services;
using MyriaRPG.Utils;
using MyriaRPG.View.Pages;
using MyriaRPG.View.Pages.Game.IngameWindow;
using MyriaRPG.View.Windows;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace MyriaRPG.ViewModel.Pages.Game.IngameWindow
{
    public class ViewModel_SettingsPage : BaseViewModel
    {
        private string _btnSettings;
        private string _btnCharacterMenue;
        private string _btnMainMenue;
        private string _btnSaveQuit;
        private string _btnQuit;
        private string _windowTitle;
        [LocalizedKey("app.general.UI.settings")]
        public string BtnSettings
        {
            get { return _btnSettings; }
            set 
            { 
                _btnSettings = value; 
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.select.title")]
        public string BtnCharacterMenue
        {
            get { return _btnCharacterMenue; }
            set 
            {
                _btnCharacterMenue = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("app.general.UI.menue.main")]
        public string BtnMainMenue
        {
            get { return _btnMainMenue; }
            set 
            {
                _btnMainMenue = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("app.general.UI.quit.save")]
        public string BtnSaveQuit
        {
            get { return _btnSaveQuit; }
            set
            {
                _btnSaveQuit = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("app.general.UI.quit")]
        public string BtnQuit
        {
            get { return _btnQuit; }
            set 
            {
                _btnQuit = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("app.general.UI.menue")]
        public string WindowTitle
        {
            get { return _windowTitle; }
            set
            {
                _windowTitle = value; 
                OnPropertyChanged();
            }

        }

        public ICommand Settings { get; }
        public ICommand CharacterMenue { get; }
        public ICommand MainMenue { get; }
        public ICommand SaveQuit { get; }
        public ICommand Quit { get; }
        public ViewModel_SettingsPage()
        {
            LocalizationAutoWire.Wire(this);
            Settings = new RelayCommand(SettingsAction);
            CharacterMenue = new RelayCommand(CharacterMenueAction);
            MainMenue = new RelayCommand(MainMenueAction);
            SaveQuit = new RelayCommand(SaveQuitAction);
            Quit = new RelayCommand(QuitAction);
        }
        public void SettingsAction()
        {
            Navigation.NavigateIngameMenue(new MyriaRPG.View.Pages.Page_Settings());
        }
        public async void CharacterMenueAction()
        {
            await SaveAsync();
            Navigation.NavigateMain(new Page_CharacterSelection());
            MainWindow.Instance.gameWindow.Visibility = Visibility.Hidden;
        }
        public async void MainMenueAction()
        {
            await SaveAsync();
            Navigation.NavigateMain(new Page_StartupMenue());
            MainWindow.Instance.gameWindow.Visibility = Visibility.Hidden;
        }
        public async void SaveQuitAction()
        {
            await SaveAsync();
            Application.Current.Shutdown();
        }
        public void QuitAction()
        {
            Application.Current.Shutdown();
        }

        private static async Task SaveAsync()
        {
            var user = UserAccoundService.CurrentUser;
            var player = UserAccoundService.CurrentCharacter;
            if (ServerApiService.Token is not null)
                await ServerApiService.SaveCharacterAsync(player);
            else
                CharacterService.SaveCharacter(user, player);
        }

    }

}
