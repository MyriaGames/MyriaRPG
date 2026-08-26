using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages.Settings
{
    /// <summary>
    /// Interaktionslogik für Page_SettingsLanguage.xaml
    /// </summary>
    public partial class Page_SettingsLanguage : Page
    {
        public Page_SettingsLanguage()
        {
            InitializeComponent();
            this.DataContext = new ViewModel_SettingsLanguage();
        }

    }

}
