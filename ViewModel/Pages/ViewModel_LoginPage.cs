using MyriaLib.Models;
using MyriaLib.Services;
using MyriaLib.Systems;
using MyriaRPG.Model;
using MyriaRPG.Services;
using MyriaRPG.Utils;
using MyriaRPG.View.Pages;
using MyriaRPG.View.Windows;
using System.Windows.Input;

namespace MyriaRPG.ViewModel.Pages
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
        public ICommand Login { get; }
        public ICommand Cancel { get; }
        public string Username { get; set; }
        public string tblUserNameMsg 
        { 
            get { return _tblUserNameMsg; }
            set
            {
                _tblUserNameMsg = value;
                OnPropertyChanged(nameof(tblUserNameMsg));
            }

        }
        public string tblPasswordMsg
        {
            get { return _tblPasswordMsg; }
            set
            {
                _tblPasswordMsg = value;
                OnPropertyChanged(nameof(tblPasswordMsg));
            }

        }
        

        [LocalizedKey("app.accounting.UI.username")]
        public string tblUsername 
        {
            get { return _tblUsername; }
            set
            {
                _tblUsername = value;
                OnPropertyChanged(nameof(tblUsername));
            }

        }

        [LocalizedKey("app.accounting.UI.password")]
        public string tblPassword 
        {
            get { return _tblPassword; }
            set
            {
                _tblPassword = value;
                OnPropertyChanged(nameof(tblPassword));
            }

        }

        [LocalizedKey("app.accounting.UI.login")]
        public string Title 
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }

        }

        [LocalizedKey("app.accounting.UI.login")]
        public string btnLogin 
        {
            get { return _btnLogin; }
            set
            {
                _btnLogin = value;
                OnPropertyChanged(nameof(btnLogin));
            }

        }

        [LocalizedKey("app.general.UI.cancel")]
        public string btnCancel 
        {
            get { return _btnCancel; }
            set
            {
                _btnCancel = value;
                OnPropertyChanged(nameof(btnCancel));
            }

        }

        public ViewModel_LoginPage()
        {
            Login = new RelayCommand(LoginAction);
            Cancel = new RelayCommand(CancelAction);
            LocalizationAutoWire.Wire(this);
        }
        private async void LoginAction()
        {
            tblUserNameMsg = string.Empty;
            var result = await ServerApiService.LoginAsync(Username, Password);
            switch (result)
            {
                case AuthResult.Success:
                    var loginNames = await ServerApiService.GetCharacterNamesAsync();
                    UserAccoundService.CurrentUser = new UserAccount
                    {
                        Username = ServerApiService.LastUsername,
                        CharacterNames = loginNames
                    };
                    Navigation.NavigateMain(new Page_CharacterSelection());
                    break;
                case AuthResult.InvalidCredentials:
                    tblUserNameMsg = Localization.T("pg.login.user.nonexistent");
                    break;
                case AuthResult.ServerError:
                    new Window_InitError("Login", ServerApiService.LastError ?? "Unknown error").ShowDialog();
                    break;
            }
        }
        private void CancelAction()
        {
            Navigation.NavigateStartup(null);
        }

    }

}
