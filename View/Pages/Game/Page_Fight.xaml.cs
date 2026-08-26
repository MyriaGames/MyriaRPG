using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myria.Wpf.View.Pages.Game
{
    public partial class Page_Fight : Page
    {
        internal static Page_Fight? Current { get; private set; }

        private ViewModel_PageFight _vm;

        public Page_Fight()
        {
            InitializeComponent();
            _vm = GameHubService.IsConnected
                ? (ViewModel_PageFight)new ViewModel_PageFightMultiplayer()
                : new ViewModel_PageFight();
            DataContext = _vm;
            Current = this;

            _vm.LogLines.CollectionChanged += (_, _) =>
            {
                Dispatcher.BeginInvoke(() => LogScrollViewer.ScrollToEnd());
            };
        }

        internal void HandleKey(KeyEventArgs e)
        {
            var kb = Myria.Lib.Core.Models.Settings.Settings.Current.Keybindings;
            Key pressed = e.Key;

            // Attack key
            if (MatchKey(pressed, kb.FightAttack))
            {
                if (_vm.AttackCommand.CanExecute(null))
                    _vm.AttackCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Skill keys 1–10 map to indices 0–9
            string[] skillKeys =
            [
                kb.FightSkill1, kb.FightSkill2, kb.FightSkill3, kb.FightSkill4,
                kb.FightSkill5, kb.FightSkill6, kb.FightSkill7, kb.FightSkill8,
                kb.FightSkill9, kb.FightSkill10
            ];

            for (int i = 0; i < skillKeys.Length; i++)
            {
                if (MatchKey(pressed, skillKeys[i]) && i < _vm.Skills.Count)
                {
                    var skill = _vm.Skills[i];
                    if (_vm.CastSkillCommand.CanExecute(skill))
                        _vm.CastSkillCommand.Execute(skill);
                    e.Handled = true;
                    return;
                }
            }
        }

        private static bool MatchKey(Key pressed, string keyName)
            => Enum.TryParse<Key>(keyName, out var target) && pressed == target;
    }
}
