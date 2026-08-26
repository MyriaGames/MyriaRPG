using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Windows;
using System.Windows;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages
{
    public class ViewModel_AccountPage : BaseViewModel
    {
        // ── Change username ─────────────────────────────────────────────────────
        private string _newUsername = string.Empty;
        private string _tblUsernameMsg = string.Empty;
        private bool   _isUsernameBusy;

        public string NewUsername
        {
            get => _newUsername;
            set { _newUsername = value; OnPropertyChanged(nameof(NewUsername)); }
        }

        /// <summary>Bound to the "current password" PasswordBox in the username section.</summary>
        public string CurrentPasswordForUsername { get; set; } = string.Empty;

        public string tblUsernameMsg
        {
            get => _tblUsernameMsg;
            set { _tblUsernameMsg = value; OnPropertyChanged(nameof(tblUsernameMsg)); }
        }

        public bool IsUsernameBusy
        {
            get => _isUsernameBusy;
            set { _isUsernameBusy = value; OnPropertyChanged(nameof(IsUsernameBusy)); _changeUsernameCommand.RaiseCanExecuteChanged(); }
        }

        private readonly RelayCommand _changeUsernameCommand;
        public ICommand ChangeUsername => _changeUsernameCommand;

        // ── Change password ─────────────────────────────────────────────────────
        private string _newPassword = string.Empty;
        private string _confirmNewPassword = string.Empty;
        private string _tblPasswordMsg = string.Empty;
        private bool   _isPasswordBusy;

        /// <summary>Bound to the "old password" PasswordBox in the password section.</summary>
        public string OldPassword { get; set; } = string.Empty;

        public string NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(nameof(NewPassword)); }
        }

        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set { _confirmNewPassword = value; OnPropertyChanged(nameof(ConfirmNewPassword)); }
        }

        public string tblPasswordMsg
        {
            get => _tblPasswordMsg;
            set { _tblPasswordMsg = value; OnPropertyChanged(nameof(tblPasswordMsg)); }
        }

        public bool IsPasswordBusy
        {
            get => _isPasswordBusy;
            set { _isPasswordBusy = value; OnPropertyChanged(nameof(IsPasswordBusy)); _changePasswordCommand.RaiseCanExecuteChanged(); }
        }

        private readonly RelayCommand _changePasswordCommand;
        public ICommand ChangePassword => _changePasswordCommand;

        // ── Delete account ──────────────────────────────────────────────────────
        private string _tblDeleteMsg = string.Empty;
        private bool   _isDeleteBusy;

        /// <summary>Bound to the password PasswordBox in the delete section.</summary>
        public string DeletePassword { get; set; } = string.Empty;

        public string tblDeleteMsg
        {
            get => _tblDeleteMsg;
            set { _tblDeleteMsg = value; OnPropertyChanged(nameof(tblDeleteMsg)); }
        }

        public bool IsDeleteBusy
        {
            get => _isDeleteBusy;
            set { _isDeleteBusy = value; OnPropertyChanged(nameof(IsDeleteBusy)); _deleteAccountCommand.RaiseCanExecuteChanged(); }
        }

        private readonly RelayCommand _deleteAccountCommand;
        public ICommand DeleteAccount => _deleteAccountCommand;

        // ── Labels ───────────────────────────────────────────────────────────────

        private string _tblTitle, _tblUsernameSection, _tblPasswordSection, _tblDeleteSection,
            _tblNewUsername, _tblCurrentPassword, _tblOldPassword, _tblNewPassword, _tblConfirmNewPassword,
            _tblChangeUsernameBtn, _tblChangePasswordBtn, _tblDeleteAccountBtn, _tblDeleteWarning,
            _tblDeleteConfirmTitle, _tblDeleteConfirmMessage;

        [LocalizedKey("pg.account.tbl.title")]
        public string TblTitle { get => _tblTitle; set { _tblTitle = value; OnPropertyChanged(nameof(TblTitle)); } }

        [LocalizedKey("pg.account.section.username")]
        public string TblUsernameSection { get => _tblUsernameSection; set { _tblUsernameSection = value; OnPropertyChanged(nameof(TblUsernameSection)); } }

        [LocalizedKey("pg.account.section.password")]
        public string TblPasswordSection { get => _tblPasswordSection; set { _tblPasswordSection = value; OnPropertyChanged(nameof(TblPasswordSection)); } }

        [LocalizedKey("pg.account.section.delete")]
        public string TblDeleteSection { get => _tblDeleteSection; set { _tblDeleteSection = value; OnPropertyChanged(nameof(TblDeleteSection)); } }

        [LocalizedKey("pg.account.lbl.newUsername")]
        public string TblNewUsername { get => _tblNewUsername; set { _tblNewUsername = value; OnPropertyChanged(nameof(TblNewUsername)); } }

        [LocalizedKey("pg.account.lbl.currentPassword")]
        public string TblCurrentPassword { get => _tblCurrentPassword; set { _tblCurrentPassword = value; OnPropertyChanged(nameof(TblCurrentPassword)); } }

        [LocalizedKey("pg.account.lbl.oldPassword")]
        public string TblOldPassword { get => _tblOldPassword; set { _tblOldPassword = value; OnPropertyChanged(nameof(TblOldPassword)); } }

        [LocalizedKey("pg.account.lbl.newPassword")]
        public string TblNewPassword { get => _tblNewPassword; set { _tblNewPassword = value; OnPropertyChanged(nameof(TblNewPassword)); } }

        [LocalizedKey("app.accounting.UI.passwordConfirm")]
        public string TblConfirmNewPassword { get => _tblConfirmNewPassword; set { _tblConfirmNewPassword = value; OnPropertyChanged(nameof(TblConfirmNewPassword)); } }

        [LocalizedKey("pg.account.btn.changeUsername")]
        public string TblChangeUsernameBtn { get => _tblChangeUsernameBtn; set { _tblChangeUsernameBtn = value; OnPropertyChanged(nameof(TblChangeUsernameBtn)); } }

        [LocalizedKey("pg.account.btn.changePassword")]
        public string TblChangePasswordBtn { get => _tblChangePasswordBtn; set { _tblChangePasswordBtn = value; OnPropertyChanged(nameof(TblChangePasswordBtn)); } }

        [LocalizedKey("pg.account.btn.deleteAccount")]
        public string TblDeleteAccountBtn { get => _tblDeleteAccountBtn; set { _tblDeleteAccountBtn = value; OnPropertyChanged(nameof(TblDeleteAccountBtn)); } }

        [LocalizedKey("pg.account.delete.warning")]
        public string TblDeleteWarning { get => _tblDeleteWarning; set { _tblDeleteWarning = value; OnPropertyChanged(nameof(TblDeleteWarning)); } }

        [LocalizedKey("pg.account.delete.confirm.title")]
        public string TblDeleteConfirmTitle { get => _tblDeleteConfirmTitle; set { _tblDeleteConfirmTitle = value; OnPropertyChanged(nameof(TblDeleteConfirmTitle)); } }

        [LocalizedKey("pg.account.delete.confirm.message")]
        public string TblDeleteConfirmMessage { get => _tblDeleteConfirmMessage; set { _tblDeleteConfirmMessage = value; OnPropertyChanged(nameof(TblDeleteConfirmMessage)); } }

        public ViewModel_AccountPage()
        {
            LocalizationAutoWire.Wire(this);

            _changeUsernameCommand = new RelayCommand(ChangeUsernameAction, () => !IsUsernameBusy);
            _changePasswordCommand = new RelayCommand(ChangePasswordAction, () => !IsPasswordBusy);
            _deleteAccountCommand  = new RelayCommand(DeleteAccountAction,  () => !IsDeleteBusy);
        }

        private async void ChangeUsernameAction()
        {
            tblUsernameMsg = string.Empty;

            if (string.IsNullOrWhiteSpace(NewUsername) || NewUsername.Length < 3)
            {
                tblUsernameMsg = Myria.Lib.Core.Systems.Localization.T("pg.register.username.tooshort");
                return;
            }
            if (NewUsername.Length > 50)
            {
                tblUsernameMsg = Myria.Lib.Core.Systems.Localization.T("pg.register.username.toolong");
                return;
            }
            if (string.IsNullOrEmpty(CurrentPasswordForUsername))
            {
                tblUsernameMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.error.currentPasswordRequired");
                return;
            }

            IsUsernameBusy = true;
            try
            {
                var result = await ServerApiService.ChangeUsernameAsync(CurrentPasswordForUsername, NewUsername);
                switch (result)
                {
                    case AccountUpdateResult.Success:
                        tblUsernameMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.success.usernameChanged");
                        NewUsername = string.Empty;
                        CurrentPasswordForUsername = string.Empty;
                        break;
                    case AccountUpdateResult.InvalidCredentials:
                        tblUsernameMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.error.wrongPassword");
                        break;
                    case AccountUpdateResult.Conflict:
                        tblUsernameMsg = Myria.Lib.Core.Systems.Localization.T("pg.register.user.alreadyexists");
                        break;
                    case AccountUpdateResult.RealmUnreachable:
                        tblUsernameMsg = ServerApiService.LastError ?? Myria.Lib.Core.Systems.Localization.T("pg.account.error.realmUnreachable");
                        break;
                    default:
                        new Window_AppError("Account", ServerApiService.LastError ?? "Unknown error").ShowDialog();
                        break;
                }
            }
            finally
            {
                IsUsernameBusy = false;
            }
        }

        private async void ChangePasswordAction()
        {
            tblPasswordMsg = string.Empty;

            if (string.IsNullOrEmpty(OldPassword))
            {
                tblPasswordMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.error.currentPasswordRequired");
                return;
            }
            if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 8)
            {
                tblPasswordMsg = Myria.Lib.Core.Systems.Localization.T("pg.register.password.tooshort");
                return;
            }
            if (NewPassword != ConfirmNewPassword)
            {
                tblPasswordMsg = Myria.Lib.Core.Systems.Localization.T("pg.register.password.mismatch");
                return;
            }

            IsPasswordBusy = true;
            try
            {
                var result = await ServerApiService.ChangePasswordAsync(OldPassword, NewPassword);
                switch (result)
                {
                    case AccountUpdateResult.Success:
                        tblPasswordMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.success.passwordChanged");
                        OldPassword = string.Empty;
                        NewPassword = string.Empty;
                        ConfirmNewPassword = string.Empty;
                        break;
                    case AccountUpdateResult.InvalidCredentials:
                        tblPasswordMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.error.wrongPassword");
                        break;
                    default:
                        new Window_AppError("Account", ServerApiService.LastError ?? "Unknown error").ShowDialog();
                        break;
                }
            }
            finally
            {
                IsPasswordBusy = false;
            }
        }

        private async void DeleteAccountAction()
        {
            tblDeleteMsg = string.Empty;

            if (string.IsNullOrEmpty(DeletePassword))
            {
                tblDeleteMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.error.currentPasswordRequired");
                return;
            }

            var confirm = MessageBox.Show(
                TblDeleteConfirmMessage,
                TblDeleteConfirmTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            IsDeleteBusy = true;
            try
            {
                var result = await ServerApiService.DeleteAccountAsync(DeletePassword);
                switch (result)
                {
                    case AccountUpdateResult.Success:
                        // ServerApiService.DeleteAccountAsync already cleared the token/session -
                        // back out all the way to the startup menu, there's nothing left to show here.
                        Navigation.Current.ClearFrame(NavigationFrameType.MultiplayerHub);
                        Navigation.Current.ClearFrame(NavigationFrameType.Startup);
                        break;
                    case AccountUpdateResult.InvalidCredentials:
                        tblDeleteMsg = Myria.Lib.Core.Systems.Localization.T("pg.account.error.wrongPassword");
                        break;
                    case AccountUpdateResult.RealmUnreachable:
                        tblDeleteMsg = ServerApiService.LastError ?? Myria.Lib.Core.Systems.Localization.T("pg.account.error.realmUnreachable");
                        break;
                    default:
                        new Window_AppError("Account", ServerApiService.LastError ?? "Unknown error").ShowDialog();
                        break;
                }
            }
            finally
            {
                IsDeleteBusy = false;
            }
        }
    }
}
