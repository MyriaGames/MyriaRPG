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
    public class ViewModel_RegisterPage : BaseViewModel
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

        [LocalizedKey("pg.register.tbl.title")]
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }

        }

        [LocalizedKey("app.accounting.UI.register")]
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

        public ViewModel_RegisterPage()
        {
            Login = new RelayCommand(RegisterAction);
            Cancel = new RelayCommand(CancelAction);
            LocalizationAutoWire.Wire(this);
        }
        private async void RegisterAction()
        {
            tblUserNameMsg = string.Empty;
            var result = await ServerApiService.RegisterAsync(Username, Password);
            switch (result)
            {
                case AuthResult.Success:
                    var regNames = await ServerApiService.GetCharacterNamesAsync();
                    UserAccoundService.CurrentUser = new UserAccount
                    {
                        Username = ServerApiService.LastUsername,
                        CharacterNames = regNames
                    };
                    Navigation.NavigateMain(new Page_CharacterSelection());
                    break;
                case AuthResult.Conflict:
                    tblUserNameMsg = Localization.T("pg.register.user.alreadyexists");
                    break;
                case AuthResult.ServerError:
                    new Window_InitError("Register", ServerApiService.LastError ?? "Unknown error").ShowDialog();
                    break;
            }
        }
        private void CancelAction()
        {
            Navigation.NavigateStartup(null);
        }

    }

}
