using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages.Settings
{
    /// <summary>
    /// Interaktionslogik für Page_SettingsGame.xaml
    /// </summary>
    public partial class Page_SettingsGame : Page
    {
        public Page_SettingsGame()
        {
            InitializeComponent();
            this.DataContext = new ViewModel_SettingsGame();
        }
    }
}
