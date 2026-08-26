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
    public class ViewModel_RegisterPage : BaseViewModel
    {
        private string _tblUserNameMsg;
        private string _tblPasswordMsg;
        private string _tblPasswordConfirmMsg;
        private string _tblUsername;
        private string _tblPassword;
        private string _tblPasswordConfirm;
        private string _tblSwitchMode;
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

        /// <summary>Bound to the second PasswordBox (the confirmation field) — kept separate
        /// from the inherited <see cref="BaseViewModel.Password"/> so a typo can be caught by
        /// comparing the two rather than trusting a single entry.</summary>
        public string ConfirmPassword { get; set; }

        public bool IsRegisterMode => true;

        /// <summary>Set by Page_Login so the "Login instead" link can swap the page's hosted
        /// view model without leaving/re-navigating the Startup frame.</summary>
        public Action? RequestModeSwitch { get; set; }

        // Guards against double-clicking Register/Cancel while the request is in flight.
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
        public string tblPasswordConfirmMsg
        {
            get => _tblPasswordConfirmMsg;
            set { _tblPasswordConfirmMsg = value; OnPropertyChanged(nameof(tblPasswordConfirmMsg)); }
        }

        [LocalizedKey("app.accounting.UI.passwordConfirm")]
        public string tblPasswordConfirm
        {
            get => _tblPasswordConfirm;
            set { _tblPasswordConfirm = value; OnPropertyChanged(nameof(tblPasswordConfirm)); }
        }

        [LocalizedKey("pg.login.btn.switchToLogin")]
        public string tblSwitchMode
        {
            get => _tblSwitchMode;
            set { _tblSwitchMode = value; OnPropertyChanged(nameof(tblSwitchMode)); }
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

        [LocalizedKey("pg.register.tbl.title")]
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        [LocalizedKey("app.accounting.UI.register")]
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

        public ViewModel_RegisterPage()
        {
            _loginCommand      = new RelayCommand(RegisterAction, () => !IsBusy);
            _cancelCommand     = new RelayCommand(CancelAction, () => !IsBusy);
            _toggleModeCommand = new RelayCommand(() => RequestModeSwitch?.Invoke(), () => !IsBusy);
            LocalizationAutoWire.Wire(this);
        }
        private async void RegisterAction()
        {
            tblUserNameMsg = string.Empty;
            tblPasswordMsg = string.Empty;
            tblPasswordConfirmMsg = string.Empty;

            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 3)
            {
                tblUserNameMsg = Localization.T("pg.register.username.tooshort");
                return;
            }
            if (Username.Length > 50)
            {
                tblUserNameMsg = Localization.T("pg.register.username.toolong");
                return;
            }
            if (string.IsNullOrEmpty(Password) || Password.Length < 8)
            {
                tblPasswordMsg = Localization.T("pg.register.password.tooshort");
                return;
            }
            // Catches typos in the password before they get baked into the account — without
            // this, a mistyped password only surfaces the next time the player tries to log in.
            if (Password != ConfirmPassword)
            {
                tblPasswordConfirmMsg = Localization.T("pg.register.password.mismatch");
                return;
            }

            IsBusy = true;
            try
            {
                var result = await ServerApiService.RegisterAsync(Username, Password);
                switch (result)
                {
                    case AuthResult.Success:
                        // Same reasoning as ViewModel_LoginPage.LoginAction: BaseUrl isn't set to the
                        // right realm until lobby Join, so don't fetch character names here.
                        UserAccountService.CurrentUser = new UserAccount
                        {
                            Username = ServerApiService.LastUsername,
                            CharacterNames = []
                        };
                        // See ViewModel_LoginPage.LoginAction's remark on this being a full-page replace.
                        Navigation.Current.Navigate(NavigationFrameType.Main, new Page_MultiplayerHub());
                        break;
                    case AuthResult.Conflict:
                        tblUserNameMsg = Localization.T("pg.register.user.alreadyexists");
                        break;
                    case AuthResult.ValidationError:
                        tblPasswordMsg = ServerApiService.LastError ?? Localization.T("pg.register.password.tooshort");
                        break;
                    case AuthResult.ServerError:
                        new Window_AppError("Register", ServerApiService.LastError ?? "Unknown error").ShowDialog();
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
