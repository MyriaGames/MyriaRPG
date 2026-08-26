using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    public partial class Page_LobbySelection : Page
    {
        public Page_LobbySelection()
        {
            InitializeComponent();
            DataContext = new ViewModel_LobbySelectionPage();
        }
    }
}
