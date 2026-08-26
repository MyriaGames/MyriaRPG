using Myria.Lib.Core.Models;
using Myria.Lib.Core.Services;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_StartupMenuPage : BaseViewModel
    {
        private string _btnSingle = string.Empty;
        private string _btnMultiplayer = string.Empty;
        private string _btnSettings = string.Empty;
        private string _btnQuit = string.Empty;
        private Type? _activeStartupPage;
        [LocalizedKey("pg.start.btn.single")]
        public string btnSingle
        {
            get => _btnSingle;
            private set { _btnSingle = value; OnPropertyChanged(nameof(btnSingle)); }
        }

        [LocalizedKey("pg.start.btn.multiplayer")]
        public string btnMultiplayer
        {
            get => _btnMultiplayer;
            private set { _btnMultiplayer = value; OnPropertyChanged(nameof(btnMultiplayer)); }
        }

        [LocalizedKey("app.general.UI.settings")]
        public string btnSettings
        {
            get => _btnSettings;
            private set { _btnSettings = value; OnPropertyChanged(nameof(btnSettings)); }
        }

        [LocalizedKey("app.general.UI.quit")]
        public string btnQuit
        {
            get => _btnQuit;
            private set { _btnQuit = value; OnPropertyChanged(nameof(btnQuit)); }
        }

        public ICommand SingleCharacter {  get; }
        public ICommand Multiplayer { get; }
        public ICommand Settings { get; }
        public ICommand Quit { get; }

        public ViewModel_StartupMenuPage()
        {
            LocalizationAutoWire.Wire(this);

            SingleCharacter = new RelayCommand(SingleCharacterAction);
            Multiplayer = new RelayCommand(MultiplayerAction);
            Settings = new RelayCommand(SettingsAction);
            Quit = new RelayCommand(() => Application.Current.Shutdown());
        }
        private void SingleCharacterAction()
        {

            string path = Path.Combine("Data/users", $"localUser.json");

            if (!File.Exists(path))
            {

                if (!Path.Exists(path))
                    Directory.CreateDirectory("Data/users");

                UserAccount account = new UserAccount();
                account.Username = "localUser";


                var jsons = JsonSerializer.Serialize(account, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, jsons);
            }

            var json = File.ReadAllText(path);
            UserAccountService.CurrentUser = JsonSerializer.Deserialize<UserAccount>(json);

            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection());
        }
        // A session-scoped login (ServerApiService.Token set) skips straight to the multiplayer
        // hub - a full-page replace, same as SingleCharacterAction above, since there's nothing
        // left to log in for. Otherwise Page_Login opens as a toggleable sub-view, same as Settings.
        private void MultiplayerAction()
        {
            if (ServerApiService.Token is not null)
            {
                Navigation.Current.Navigate(NavigationFrameType.Main, new Page_MultiplayerHub());
                return;
            }

            ToggleStartupSubPage(typeof(Page_Login), () => new Page_Login());
        }
        private void SettingsAction()
        {
            ToggleStartupSubPage(typeof(Page_MainMenuSettings), () => new Page_MainMenuSettings());
        }

        // Toggles a sub-view in and out of the Startup frame: same target while it's already
        // open closes it, any other target (or this one) replaces/opens it. The emptiness check
        // guards against a stale _activeStartupPage - a page hosted in that frame (Page_Login's
        // Cancel, Page_MultiplayerHub's Back) can clear the frame on its own, independently of
        // this view model, and without that check the next click here would wrongly think its
        // own page was still open and silently no-op instead of reopening it.
        private void ToggleStartupSubPage(Type pageType, Func<Page> factory)
        {
            if (_activeStartupPage == pageType && !Navigation.Current.IsFrameEmpty(NavigationFrameType.Startup))
            {
                _activeStartupPage = null;
                Navigation.Current.ClearFrame(NavigationFrameType.Startup);
            }
            else
            {
                _activeStartupPage = pageType;
                Navigation.Current.Navigate(NavigationFrameType.Startup, factory());
            }
        }

    }

}
