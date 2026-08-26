using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.View.Pages.Settings;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    /// <summary>
    /// Interaktionslogik fuer Page_Settings.xaml
    /// </summary>
    public partial class Page_Settings : Page
    {
        public Page_Settings()
        {
            InitializeComponent();
            this.DataContext = new ViewModel_SettingsPage();
            Myria.Wpf.Services.Navigation.Current.RegisterFrame(NavigationFrameType.Settings, frm_NavigationFrame);
            Myria.Wpf.Services.Navigation.Current.Navigate(Nav.SettingsVisuals);
        }
    }
}
