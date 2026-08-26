using System.Windows;
using System.Windows.Controls;
using Myria.Wpf.ViewModel;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
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
            SetMode(registration);
            pbx_UserPassword.PasswordChanged += PasswordToViewModel;
            pbx_ConfirmPassword.PasswordChanged += ConfirmPasswordToViewModel;
        }

        // Swaps the hosted view model in place (login <-> register) instead of navigating away,
        // so the "Register instead" / "Login instead" link stays inside the same Startup-frame
        // page. The PasswordBox event handlers below read _viewModel fresh on every keystroke,
        // so they don't need to be re-subscribed when the view model changes.
        private void SetMode(bool registration)
        {
            pbx_UserPassword.Password = string.Empty;
            pbx_ConfirmPassword.Password = string.Empty;

            if (registration)
            {
                var vm = new ViewModel_RegisterPage { RequestModeSwitch = () => SetMode(false) };
                _viewModel = vm;
            }
            else
            {
                var vm = new ViewModel_LoginPage { RequestModeSwitch = () => SetMode(true) };
                _viewModel = vm;
            }
            DataContext = _viewModel;
        }

        private void PasswordToViewModel(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = pbx_UserPassword.Password;
        }

        private void ConfirmPasswordToViewModel(object sender, RoutedEventArgs e)
        {
            if (_viewModel is ViewModel_RegisterPage vm)
                vm.ConfirmPassword = pbx_ConfirmPassword.Password;
        }
    }

}
