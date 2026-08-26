using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myria.Wpf.View.Pages.Game
{
    public partial class Page_Game : Page
    {
        internal static Page_Game? Current { get; private set; }

        private ViewModel_PageGame _vm;

        public Page_Game()
        {
            InitializeComponent();
            Myria.Wpf.Services.Navigation.Current.RegisterFrame(NavigationFrameType.Game, frm_Navigation);
            _vm = new ViewModel_PageGame();
            DataContext = _vm;
            Current = this;
            Myria.Wpf.Services.Navigation.Current.SetGameState(true);
        }

        internal void HandleKey(KeyEventArgs e)
        {
            var kb = Myria.Lib.Core.Models.Settings.Settings.Current.Keybindings;
            Key pressed = e.Key;

            if (!Myria.Wpf.Services.Navigation.Current.IsInFight)
            {
                // Room movement
                if (MatchKey(pressed, kb.MoveNorth)) { ExecuteRoomCommand("North"); e.Handled = true; return; }
                if (MatchKey(pressed, kb.MoveSouth)) { ExecuteRoomCommand("South"); e.Handled = true; return; }
                if (MatchKey(pressed, kb.MoveWest))  { ExecuteRoomCommand("West");  e.Handled = true; return; }
                if (MatchKey(pressed, kb.MoveEast))  { ExecuteRoomCommand("East");  e.Handled = true; return; }

                // Room actions
                if (MatchKey(pressed, kb.StartFight))  { ExecuteRoomVoidCommand(vm => vm.StartFightCommand);     e.Handled = true; return; }
                if (MatchKey(pressed, kb.StartGather)) { ExecuteRoomVoidCommand(vm => vm.StartGatheringCommand); e.Handled = true; return; }

                // Ingame nav bar
                if (MatchKey(pressed, kb.OpenInventory)) { _vm.OpenInventoryCommand.Execute(null); e.Handled = true; return; }
                if (MatchKey(pressed, kb.OpenCharacter)) { _vm.OpenCharacterCommand.Execute(null); e.Handled = true; return; }
                if (MatchKey(pressed, kb.OpenSkills))    { _vm.OpenSkillsCommand.Execute(null);    e.Handled = true; return; }
                if (MatchKey(pressed, kb.OpenQuests))    { _vm.OpenQuestsCommand.Execute(null);    e.Handled = true; return; }
                if (MatchKey(pressed, kb.OpenMap))       { _vm.MapCommand.Execute(null);           e.Handled = true; return; }
            }

            // Settings is reachable even during a fight
            if (MatchKey(pressed, kb.OpenSettings)) { _vm.SettingsCommand.Execute(null); e.Handled = true; }
        }

        private static bool MatchKey(Key pressed, string keyName)
            => Enum.TryParse<Key>(keyName, out var target) && pressed == target;

        private void ExecuteRoomCommand(string direction)
        {
            var roomVm = (frm_Navigation.Content as System.Windows.FrameworkElement)?.DataContext as ViewModel_PageRoom;
            roomVm?.MoveCommand.Execute(direction);
        }

        private void ExecuteRoomVoidCommand(Func<ViewModel_PageRoom, ICommand> selector)
        {
            var roomVm = (frm_Navigation.Content as System.Windows.FrameworkElement)?.DataContext as ViewModel_PageRoom;
            if (roomVm != null) selector(roomVm).Execute(null);
        }
    }
}
