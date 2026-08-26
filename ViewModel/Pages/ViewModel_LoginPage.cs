using Myria.Lib.Core.Models;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_LoginPage : BaseViewModel
    {
        private string _tblUserNameMsg;
        private string _tblPasswordMsg;
        private string _tblUsername;
        private string _tblPassword;
        private string _title;
        private string _btnLogin;
        private string _btnCancel;
        private bool   _isBusy;
        private readonly RelayCommand _loginCommand;
        private readonly RelayCommand _cancelCommand;
        private readonly RelayCommand _toggleModeCommand;
        public ICommand Login      => _loginCommand;
        public ICommand Cancel     => _cancelCommand;
        public ICommand ToggleMode => _toggleModeCommand;
        public string Username { get; set; }

        public bool IsRegisterMode => false;

        /// <summary>Set by Page_Login so the "Register instead" link can swap the page's
        /// hosted view model without leaving/re-navigating the Startup frame.</summary>
        public Action? RequestModeSwitch { get; set; }

        [LocalizedKey("pg.login.btn.switchToRegister")]
        public string tblSwitchMode
        {
            get => _tblSwitchMode;
            set { _tblSwitchMode = value; OnPropertyChanged(nameof(tblSwitchMode)); }
        }
        private string _tblSwitchMode;

        // Guards against double-clicking Login/Cancel while the login request is in flight -
        // without this, a slow/laggy server response let a double-click fire a second concurrent
        // LoginAsync call.
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
                _loginCommand.RaiseCanExecuteChanged();
                _cancelCommand.RaiseCanExecuteChanged();
                _toggleModeCommand.RaiseCanExecuteChanged();
            }
        }
        public string tblUserNameMsg 
        { 
            get => _tblUserNameMsg;
            set { _tblUserNameMsg = value; OnPropertyChanged(nameof(tblUserNameMsg)); }
        }
        public string tblPasswordMsg
        {
            get => _tblPasswordMsg;
            set { _tblPasswordMsg = value; OnPropertyChanged(nameof(tblPasswordMsg)); }
        }
        

        [LocalizedKey("app.accounting.UI.username")]
        public string tblUsername 
        {
            get => _tblUsername;
            set { _tblUsername = value; OnPropertyChanged(nameof(tblUsername)); }
        }

        [LocalizedKey("app.accounting.UI.password")]
        public string tblPassword 
        {
            get => _tblPassword;
            set { _tblPassword = value; OnPropertyChanged(nameof(tblPassword)); }
        }

        [LocalizedKey("app.accounting.UI.login")]
        public string Title 
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        [LocalizedKey("app.accounting.UI.login")]
        public string btnLogin 
        {
            get => _btnLogin;
            set { _btnLogin = value; OnPropertyChanged(nameof(btnLogin)); }
        }

        [LocalizedKey("app.general.UI.cancel")]
        public string btnCancel 
        {
            get => _btnCancel;
            set { _btnCancel = value; OnPropertyChanged(nameof(btnCancel)); }
        }

        public ViewModel_LoginPage()
        {
            _loginCommand      = new RelayCommand(LoginAction, () => !IsBusy);
            _cancelCommand     = new RelayCommand(CancelAction, () => !IsBusy);
            _toggleModeCommand = new RelayCommand(() => RequestModeSwitch?.Invoke(), () => !IsBusy);
            LocalizationAutoWire.Wire(this);
        }
        private async void LoginAction()
        {
            tblUserNameMsg = string.Empty;
            IsBusy = true;
            try
            {
                var result = await ServerApiService.LoginAsync(Username, Password);
                switch (result)
                {
                    case AuthResult.Success:
                        // Character names live per-realm; BaseUrl isn't set to the right realm yet at
                        // this point (that happens on lobby Join), so don't fetch them here - fetching
                        // now would hit whatever BaseUrl still defaults to and silently cache an empty
                        // list. ViewModel_LobbySelectionPage.JoinAction fetches the real list instead.
                        UserAccountService.CurrentUser = new UserAccount
                        {
                            Username = ServerApiService.LastUsername,
                            CharacterNames = []
                        };
                        // Full-page replace, same as Page_CharacterSelection does for singleplayer -
                        // Page_MultiplayerHub's own Back button returns to Page_StartupMenu.
                        Navigation.Current.Navigate(NavigationFrameType.Main, new Page_MultiplayerHub());
                        break;
                    case AuthResult.InvalidCredentials:
                        tblUserNameMsg = Localization.T("pg.login.user.nonexistent");
                        break;
                    case AuthResult.ServerError:
                        new Window_AppError("Login", ServerApiService.LastError ?? "Unknown error").ShowDialog();
                        break;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
        private void CancelAction()
        {
            Navigation.Current.ClearFrame(NavigationFrameType.Startup);
        }

    }

}
