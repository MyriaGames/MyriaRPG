using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.Pages.Settings;
using Myria.Wpf.ViewModel.Pages;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages
{
    public partial class Page_MainMenuSettings : Page
    {
        public Page_MainMenuSettings()
        {
            InitializeComponent();
            var vm = new ViewModel_MainMenuSettings();
            vm.NavigateTo = page => ContentFrame.Navigate(page);
            DataContext = vm;
            ContentFrame.Navigate(new Page_SettingsVisuals());
        }
    }
}
