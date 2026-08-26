using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.Windows;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class ViewModel_IngameMenu : BaseViewModel
    {
        private string _btnSettings;
        private string _btnCharacterMenu;
        private string _btnMainMenu;
        private string _btnSaveQuit;
        private string _btnQuit;
        private string _windowTitle;

        [LocalizedKey("app.general.UI.settings")]
        public string BtnSettings
        {
            get { return _btnSettings; }
            set { _btnSettings = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.character.select.title")]
        public string BtnCharacterMenu
        {
            get { return _btnCharacterMenu; }
            set { _btnCharacterMenu = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.menu.main")]
        public string BtnMainMenu
        {
            get { return _btnMainMenu; }
            set { _btnMainMenu = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.quit.save")]
        public string BtnSaveQuit
        {
            get { return _btnSaveQuit; }
            set { _btnSaveQuit = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.quit")]
        public string BtnQuit
        {
            get { return _btnQuit; }
            set { _btnQuit = value; OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.menu")]
        public string WindowTitle
        {
            get { return _windowTitle; }
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        public ICommand Settings { get; }
        public ICommand CharacterMenu { get; }
        public ICommand MainMenu { get; }
        public ICommand SaveQuit { get; }
        public ICommand Quit { get; }

        public ViewModel_IngameMenu()
        {
            LocalizationAutoWire.Wire(this);
            Settings      = new RelayCommand(SettingsAction);
            CharacterMenu = new RelayCommand(CharacterMenuAction);
            MainMenu      = new RelayCommand(MainMenuAction);
            SaveQuit      = new RelayCommand(SaveQuitAction);
            Quit          = SaveQuit;
        }

        public void SettingsAction()
        {
            Navigation.Current.Navigate(new Myria.Wpf.View.Pages.Page_Settings());
        }

        public async void CharacterMenuAction()
        {
            await SaveAsync();
            Navigation.Current.SetGameState(false);
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection());
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Hidden;
            MainWindow.Instance.npcWindow.Visibility = Visibility.Hidden;
        }

        public async void MainMenuAction()
        {
            await SaveAsync();
            if (ServerApiService.Token is not null)
            {
                await GameHubService.DisconnectAsync();
                ServerApiService.ClearToken();
            }
            Navigation.Current.SetGameState(false);
            Navigation.Current.Navigate(Nav.Startup);
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Hidden;
            MainWindow.Instance.npcWindow.Visibility = Visibility.Hidden;
        }

        public async void SaveQuitAction()
        {
            await SaveAsync();
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
