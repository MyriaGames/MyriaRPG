using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages.Settings
{
    public partial class Page_SettingsMods : Page
    {
        public Page_SettingsMods(bool allowToggle = true)
        {
            InitializeComponent();
            DataContext = new ViewModel_SettingsMods(allowToggle);
        }
    }
}
