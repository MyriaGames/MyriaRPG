using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_Runes : Page
    {
        public Page_Runes()
        {
            InitializeComponent();
            DataContext = new RunePageViewModel();
        }
    }
}
