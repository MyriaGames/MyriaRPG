using Myria.Lib.Core.Models.Dto;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using System.Windows.Threading;

namespace Myria.Wpf.ViewModel.Pages.Game
{
    /// <summary>
    /// PvP fight (v0.3), on the same Page_Fight layout as group combat: the player's own side fills
    /// GroupCharacters (the party panel) and the opposing side fills GroupMonsters (the target
    /// panel), so targeting, HP bars and the action column all work unchanged. The server is fully
    /// authoritative - this only sends actions and renders PvpUpdated/PvpFinished snapshots.
    /// </summary>
    public class ViewModel_PageFightPvp : ViewModel_PageFight
    {
        /// <summary>Set by ViewModel_PageGame from the PvpStarted event right before navigating here.</summary>
        public static PvpSnapshot? Pending { get; set; }

        /// <summary>How often a waiting client asks the server to skip a stalled turn. The server
        /// only does so past PvpEncounter.TurnTimeout, so polling early is harmless.</summary>
        private static readonly TimeSpan StallPollInterval = TimeSpan.FromSeconds(5);

        private readonly string _me;
        private readonly int _mySide;          // 1 = SideA, 2 = SideB
        private readonly DispatcherTimer _stallTimer;
        private bool _waiting;                 // an action is in flight
        private bool _finished;
        private int _myHp = 1, _myHpMax = 1, _myMp, _myMpMax;

        public override int CharacterHp    => _myHp;
        public override int CharacterHpMax => _myHpMax;
        public override int CharacterMp    => _myMp;
        public override int CharacterMpMax => _myMpMax;
        public override bool CanAct => _isMyTurn && !_waiting && !_finished;

        public ViewModel_PageFightPvp() : base(false /* skip base init */)
        {
            var snap = Pending ?? throw new InvalidOperationException("No pending PvP fight.");
            Pending = null;

            var character = UserAccountService.CurrentCharacter;
            _me = character.Name;
            _mySide = snap.SideA.Any(c => IsMe(c.Name)) ? 1 : 2;
            IsGroupCombat = true;

            foreach (var (skill, _) in SkillSlotService.GetCombatSkills(character))
                Skills.Add(new FightSkillVm(skill, ""));

            foreach (var c in Mine(snap))   GroupCharacters.Add(new GroupCombatantVm(c.Name, c.Hp, c.MaxHp, c.IsAlive, c.Level));
            foreach (var c in Theirs(snap)) GroupMonsters.Add(new GroupCombatantVm(c.Name, c.Hp, c.MaxHp, c.IsAlive, c.Level));
            _selectedMonsterIndex = Math.Max(0, GroupMonsters.IndexOf(GroupMonsters.FirstOrDefault(m => m.IsAlive)!));

            GameHubService.PvpUpdated  += OnPvpUpdated;
            GameHubService.PvpFinished += OnPvpFinished;

            _stallTimer = new DispatcherTimer { Interval = StallPollInterval };
            _stallTimer.Tick += (_, _) =>
            {
                if (!_finished && !_isMyTurn) _ = GameHubService.PvpSkipStalledTurnAsync();
            };
            _stallTimer.Start();

            Apply(snap);
        }

        private bool IsMe(string name) => string.Equals(name, _me, StringComparison.OrdinalIgnoreCase);
        private List<GroupCombatantState> Mine(PvpSnapshot s)   => _mySide == 1 ? s.SideA : s.SideB;
        private List<GroupCombatantState> Theirs(PvpSnapshot s) => _mySide == 1 ? s.SideB : s.SideA;

        /// <summary>Renders a snapshot: new log lines, both sides' HP (fixed order, so by index),
        /// own HP/MP (also mirrored onto the real character for the HUD), and whose turn it is.</summary>
        private void Apply(PvpSnapshot snap)
        {
            foreach (var msg in snap.LogEntries)
                LogLines.Add(LogLineVm.From(msg.Key,
                    Localization.T(msg.Key, LocalizationText.LocalizeMonsterArgs(msg.Args.Cast<object>()))));

            Sync(GroupCharacters, Mine(snap));
            Sync(GroupMonsters, Theirs(snap));

            if (Mine(snap).FirstOrDefault(c => IsMe(c.Name)) is { } me)
            {
                (_myHp, _myHpMax, _myMp, _myMpMax) = (me.Hp, me.MaxHp, me.Mana, me.MaxMana);
                var character = UserAccountService.CurrentCharacter;
                character.SetHealth(me.Hp);
                character.SetMana(me.Mana);
            }

            if (_selectedMonsterIndex < GroupMonsters.Count && !GroupMonsters[_selectedMonsterIndex].IsAlive)
            {
                int next = GroupMonsters.IndexOf(GroupMonsters.FirstOrDefault(m => m.IsAlive)!);
                if (next >= 0) SelectedMonsterIndex = next;
            }

            CurrentTurnCharacterName = snap.CurrentTurnCharacterName ?? "";
            IsMyTurn = IsMe(CurrentTurnCharacterName);
            RaiseAll();
        }

        private static void Sync(IList<GroupCombatantVm> vms, List<GroupCombatantState> states)
        {
            for (int i = 0; i < states.Count && i < vms.Count; i++)
            {
                vms[i].Hp      = states[i].Hp;
                vms[i].IsAlive = states[i].IsAlive;
            }
        }

        private void OnPvpUpdated(PvpSnapshot snap)
        {
            if (_finished) return;
            Apply(snap);
        }

        private void OnPvpFinished(PvpSnapshot snap)
        {
            if (_finished) return;
            _finished = true;
            Apply(snap);

            // The server already restored this character's pre-fight HP/MP and pushed it (plus the
            // new PvP stats) via CharacterUpdated; only the summary line is left to show.
            if (snap.Results?.FirstOrDefault(r => IsMe(r.Name)) is { } mine)
                ViewModel_PageRoom.WriteLog(mine.Won
                    ? Localization.T("pvp.result.win", mine.NewRating, mine.RatingDelta, mine.CurrencyDelta)
                    : Localization.T("pvp.result.loss", mine.NewRating, mine.RatingDelta));

            Close();
        }

        /// <summary>A reconnect put this character back into the fight (a fresh PvpStarted): this
        /// page's view model stops listening; the new page takes over.</summary>
        public void Detach()
        {
            _finished = true;
            _stallTimer.Stop();
            GameHubService.PvpUpdated  -= OnPvpUpdated;
            GameHubService.PvpFinished -= OnPvpFinished;
        }

        private void Close()
        {
            _finished = true;
            _stallTimer.Stop();
            GameHubService.PvpUpdated  -= OnPvpUpdated;
            GameHubService.PvpFinished -= OnPvpFinished;
            Navigation.Current.SetFightState(false);
            ViewModel_PageRoom.Reload(); // refreshes the arena's challenge list too
            Navigation.Current.Navigate(Nav.Room);
        }

        // ── Actions ──────────────────────────────────────────────────────────

        protected override Task AttackAsync() =>
            SendAsync(() => GameHubService.PvpAttackAsync(_selectedMonsterIndex));

        protected override Task CastSkillAsync(FightSkillVm? vm)
        {
            if (vm == null) return Task.CompletedTask;
            // Enemy-targeted skills aim at the selected opponent; ally-targeted ones at the ally
            // picked in the HUD (yourself by default), as an index into your own side.
            int targetIdx = vm.Skill.Target == SkillTarget.SingleAlly ? AllyTargetIndex() : _selectedMonsterIndex;
            return SendAsync(() => GameHubService.PvpCastSkillAsync(vm.Skill.Id ?? vm.Skill.Name, targetIdx));
        }

        protected override Task UseItemAsync(FightItemVm? vm) =>
            vm == null ? Task.CompletedTask : SendAsync(() => GameHubService.PvpUseItemAsync(vm.ItemId));

        /// <summary>
        /// Sends one action. A successful turn is rendered from the PvpUpdated/PvpFinished broadcast
        /// every participant gets, so its direct return value is ignored. The server returns a
        /// snapshot *only to the caller* for a rejection with a reason (no mana, on cooldown) -
        /// recognisable because it's still this player's turn - and that one is shown here.
        /// </summary>
        private async Task SendAsync(Func<Task<PvpSnapshot?>> action)
        {
            if (!CanAct) return;
            _waiting = true;
            RaiseAll();
            var result = await action();
            _waiting = false;
            if (result is { Finished: false } && IsMe(result.CurrentTurnCharacterName) && result.LogEntries.Count > 0)
                Apply(result);
            else
                RaiseAll();
        }

        /// <summary>Fleeing forfeits (a loss). The server answers the fleer with its own PvpFinished
        /// carrying the result, which OnPvpFinished shows; Close() covers a lost connection.</summary>
        protected override void Run() => _ = FleeAsync();

        private async Task FleeAsync()
        {
            await GameHubService.PvpFleeAsync();
            if (!_finished) Close();
        }
    }
}
