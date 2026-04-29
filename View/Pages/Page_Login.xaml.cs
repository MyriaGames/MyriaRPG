using System.Windows;
using System.Windows.Controls;
using MyriaRPG.ViewModel;
using MyriaRPG.ViewModel.Pages;

namespace MyriaRPG.View.Pages
{
    /// <summary>
    /// Interaktionslogik für Page_Login.xaml
    /// </summary>
    public partial class Page_Login : Page
    {
        private BaseViewModel _viewModel;
        public Page_Login(bool registration = false)
        {
            InitializeComponent();
            if (registration)
            {
                _viewModel = new ViewModel_RegisterPage();
            }
            else
            {
                _viewModel = new ViewModel_LoginPage();
            }
            this.DataContext = _viewModel;
            pbx_UserPassword.PasswordChanged += PasswordToViewModel;
        }

        private void PasswordToViewModel(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = pbx_UserPassword.Password;
        }

    }

}
