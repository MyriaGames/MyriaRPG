using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    public partial class Page_Account : Page
    {
        private readonly ViewModel_AccountPage _viewModel;

        public Page_Account()
        {
            InitializeComponent();
            _viewModel = new ViewModel_AccountPage();
            DataContext = _viewModel;

            pbx_CurrentPasswordForUsername.PasswordChanged += (_, _) =>
                _viewModel.CurrentPasswordForUsername = pbx_CurrentPasswordForUsername.Password;
            pbx_OldPassword.PasswordChanged += (_, _) =>
                _viewModel.OldPassword = pbx_OldPassword.Password;
            pbx_NewPassword.PasswordChanged += (_, _) =>
                _viewModel.NewPassword = pbx_NewPassword.Password;
            pbx_ConfirmNewPassword.PasswordChanged += (_, _) =>
                _viewModel.ConfirmNewPassword = pbx_ConfirmNewPassword.Password;
            pbx_DeletePassword.PasswordChanged += (_, _) =>
                _viewModel.DeletePassword = pbx_DeletePassword.Password;
        }
    }
}
