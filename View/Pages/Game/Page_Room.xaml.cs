using System.Windows.Controls;
using System.Windows.Input;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game;

namespace Myria.Wpf.View.Pages.Game
{
    public partial class Page_Room : Page
    {
        public Page_Room()
        {
            InitializeComponent();
            var vm = GameHubService.IsConnected
                ? (ViewModel_PageRoom)new ViewModel_PageRoomMultiplayer()
                : new ViewModel_PageRoom();
            DataContext = vm;

            vm.Log.CollectionChanged += (_, _) =>
            {
                Dispatcher.BeginInvoke(() => LogScrollViewer.ScrollToEnd());
            };
        }

        private void NpcListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ViewModel_PageRoom vm ||
                sender is not ListBoxItem item ||
                item.DataContext is not Npc npc)
            {
                return;
            }

            vm.SelectedNpc = npc;
            if (vm.TalkCommand.CanExecute(null))
                vm.TalkCommand.Execute(null);

            e.Handled = true;
        }
    }
}
