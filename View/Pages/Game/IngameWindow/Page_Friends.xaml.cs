using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_Friends : Page
    {
        public Page_Friends()
        {
            InitializeComponent();
            var vm = new FriendsPageViewModel();
            DataContext = vm;
            Unloaded += (_, _) => vm.Cleanup();
        }

        private void MoreOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not FriendEntryVm entry) return;

            var menu = new ContextMenu
            {
                Items =
                {
                    new MenuItem { Header = Myria.Lib.Core.Systems.Localization.T("pg.game.player.invite_party"), Command = entry.InviteToPartyCommand },
                    new Separator(),
                    new MenuItem { Header = Myria.Lib.Core.Systems.Localization.T("pg.friends.remove"), Command = entry.RemoveCommand },
                }
            };
            menu.PlacementTarget = btn;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
}
