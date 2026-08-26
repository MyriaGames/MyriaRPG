using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_AddFriend : Page
    {
        public Page_AddFriend()
        {
            InitializeComponent();
            DataContext = new AddFriendViewModel();
        }
    }
}
