using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    // Landing page shown right after a successful login/registration (or immediately, if the
    // player is already logged in this session — see ViewModel_StartupMenuPage.MultiplayerAction).
    public class ViewModel_MultiplayerHubPage : BaseViewModel
    {
        private string _tblTitle;
        private string _tblWelcome;
        private string _tblPlay;
        private string _tblSettings;
        private string _tblAccount;
        private string _tblBack;
        private Type? _activeSubPage;

        [LocalizedKey("pg.hub.tbl.title")]
        public string TblTitle
        {
            get => _tblTitle;
            set { _tblTitle = value; OnPropertyChanged(nameof(TblTitle)); }
        }

        public string TblWelcome
        {
            get => _tblWelcome;
            set { _tblWelcome = value; OnPropertyChanged(nameof(TblWelcome)); }
        }

        [LocalizedKey("app.general.UI.start")]
        public string TblPlay
        {
            get => _tblPlay;
            set { _tblPlay = value; OnPropertyChanged(nameof(TblPlay)); }
        }

        [LocalizedKey("app.general.UI.settings")]
        public string TblSettings
        {
            get => _tblSettings;
            set { _tblSettings = value; OnPropertyChanged(nameof(TblSettings)); }
        }

        [LocalizedKey("pg.hub.btn.account")]
        public string TblAccount
        {
            get => _tblAccount;
            set { _tblAccount = value; OnPropertyChanged(nameof(TblAccount)); }
        }

        [LocalizedKey("app.general.UI.back")]
        public string TblBack
        {
            get => _tblBack;
            set { _tblBack = value; OnPropertyChanged(nameof(TblBack)); }
        }

        [LocalizedKey("pg.hub.btn.logout")]
        public string TblLogout
        {
            get => _tblLogout;
            set { _tblLogout = value; OnPropertyChanged(nameof(TblLogout)); }
        }
        private string _tblLogout;

        public ICommand Play     { get; }
        public ICommand Settings { get; }
        public ICommand Account  { get; }
        public ICommand Back     { get; }
        public ICommand Logout   { get; }

        public ViewModel_MultiplayerHubPage()
        {
            LocalizationAutoWire.Wire(this);
            TblWelcome = string.Format(
                Myria.Lib.Core.Systems.Localization.T("pg.hub.tbl.welcome"),
                ServerApiService.LastUsername ?? "?");

            Play     = new RelayCommand(PlayAction);
            Settings = new RelayCommand(SettingsAction);
            Account  = new RelayCommand(AccountAction);
            Back     = new RelayCommand(BackAction);
            Logout   = new RelayCommand(LogoutAction);
        }

        private void PlayAction()
        {
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_LobbySelection());
        }

        private void SettingsAction() => ToggleSubPage(typeof(Page_MainMenuSettings), () => new Page_MainMenuSettings());

        private void AccountAction() => ToggleSubPage(typeof(Page_Account), () => new Page_Account());

        private void ToggleSubPage(Type pageType, Func<Page> factory)
        {
            if (_activeSubPage == pageType)
            {
                _activeSubPage = null;
                Navigation.Current.ClearFrame(NavigationFrameType.MultiplayerHub);
            }
            else
            {
                _activeSubPage = pageType;
                Navigation.Current.Navigate(NavigationFrameType.MultiplayerHub, factory());
            }
        }

        private void BackAction() => ReturnToStartupMenu();

        // Lets a player switch accounts without restarting the game: drops the session token
        // (and whatever character list was cached under it - GetCharacterNamesAsync only runs
        // again once a new login picks a realm) and returns to the startup menu the same way
        // Back does, so the next Multiplayer click asks for credentials again instead of
        // skipping straight back in under the account that was just logged out of.
        private void LogoutAction()
        {
            ServerApiService.ClearToken();
            UserAccoundService.CurrentUser = null;
            ReturnToStartupMenu();
        }

        private void ReturnToStartupMenu()
        {
            // Page_MultiplayerHub replaces Page_StartupMenu outright in the Main frame (same as
            // Page_CharacterSelection does for singleplayer) - re-navigate to the cached Startup
            // view instead of clearing a sub-frame, mirroring ViewModel_CharacterSelectionPage.BackAction.
            Navigation.Current.Navigate(Nav.Startup);

            // Page_StartupMenu is cached and reused (see Navigation.Navigate(Nav)), so whatever
            // sub-view was open in its Startup frame before Multiplayer was clicked (Page_Login,
            // left mid-entry) is still sitting there - clear it so Back/Logout always land on a
            // clean menu.
            Navigation.Current.ClearFrame(NavigationFrameType.Startup);
        }
    }
}
