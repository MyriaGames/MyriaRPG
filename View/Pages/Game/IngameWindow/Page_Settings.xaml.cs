using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_IngameMenu : Page
    {
        public Page_IngameMenu()
        {
            InitializeComponent();
            this.DataContext = new ViewModel_IngameMenu();
        }
    }
}
