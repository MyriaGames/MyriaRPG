using Myria.Lib.Core.Entities.Monsters;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Models.Dto;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Events;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace Myria.Wpf.ViewModel.Pages.Game
{
    public class ViewModel_PageFight : BaseViewModel
    {
        public static StartGroupCombatResult?  PendingGroupCombat    { get; set; }
        public static GroupCombatEncounter?    PendingLocalEncounter { get; set; }

        public static ViewModel_PageFight? ActiveFight { get; protected set; }

        protected readonly CombatEncounter? _encounter;

        // Group mode
        protected bool                  _isGroupCombat;
        protected bool                  _isLocalGroupCombat;
        protected bool                  _isMyTurn;
        protected int                   _selectedMonsterIndex;
        protected string                _currentTurnCharacterName = "";
        protected GroupCombatEncounter? _groupEncounter;
        protected int                   _lastGroupLogIndex = 0;

        public bool IsGroupCombat
        {
            get => _isGroupCombat;
            protected set { _isGroupCombat = value; OnPropertyChanged(); }
        }

        public bool IsMyTurn
        {
            get => _isMyTurn;
            protected set { _isMyTurn = value; OnPropertyChanged(); }
        }

        public int SelectedMonsterIndex
        {
            get => _selectedMonsterIndex;
            set
            {
                _selectedMonsterIndex = value;
                OnPropertyChanged();
                if (_isGroupCombat && value >= 0 && value < GroupMonsters.Count)
                {
                    var m = GroupMonsters[value];
                    OnMonsterSelected(m);
                    OnPropertyChanged(nameof(ActiveMonsterName));
                    OnPropertyChanged(nameof(EnemyHp));
                    OnPropertyChanged(nameof(EnemyHpMax));
                    OnPropertyChanged(nameof(EnemyLevel));
                    OnPropertyChanged(nameof(OtherMonsters));
                    OnPropertyChanged(nameof(HasOtherMonsters));
                }
            }
        }

        // Called when SelectedMonsterIndex changes — override in multiplayer to sync server fields.
        protected virtual void OnMonsterSelected(GroupCombatantVm m) { }

        public string CurrentTurnCharacterName
        {
            get => _currentTurnCharacterName;
            protected set { _currentTurnCharacterName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<GroupCombatantVm> GroupCharacters  { get; } = new();
        public ObservableCollection<GroupCombatantVm> GroupMonsters { get; } = new();
        private string _title = string.Empty;
        private string _btnAttack = string.Empty;
        private string _btnRun = string.Empty;
        private string _skillsLabel = string.Empty;
        private string _tblGroupParty = string.Empty;
        private string _tblGroupTargets = string.Empty;

        [LocalizedKey("pg.fight.title")]
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.fight.btn.attack")]
        public string BtnAttack
        {
            get => _btnAttack;
            set { _btnAttack = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.fight.btn.run")]
        public string BtnRun
        {
            get => _btnRun;
            set { _btnRun = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.fight.skills")]
        public string SkillsLabel
        {
            get => _skillsLabel;
            set { _skillsLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.fight.group.party")]
        public string TblGroupParty
        {
            get => _tblGroupParty;
            set { _tblGroupParty = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.fight.group.targets")]
        public string TblGroupTargets
        {
            get => _tblGroupTargets;
            set { _tblGroupTargets = value; OnPropertyChanged(); }
        }

        protected GroupCombatantVm? SelectedGroupMonster =>
            _selectedMonsterIndex >= 0 && _selectedMonsterIndex < GroupMonsters.Count
                ? GroupMonsters[_selectedMonsterIndex]
                : null;

        public virtual string ActiveMonsterName => _isGroupCombat
            ? LocalizationText.LocalizeMonsterName(SelectedGroupMonster?.RawName ?? "")
            : LocalizationText.LocalizeMonsterName(_encounter?.Enemy.Name ?? "");
        public virtual int CharacterHp    => _encounter?.Character.CurrentHealth ?? 1;
        public virtual int CharacterHpMax => _encounter?.Character.MaxHealth     ?? 1;
        public virtual int CharacterMp    => _encounter?.Character.CurrentMana   ?? 0;
        public virtual int CharacterMpMax => _encounter?.Character.MaxMana       ?? 0;
        public virtual int EnemyHp    => _isGroupCombat ? (SelectedGroupMonster?.Hp    ?? 1) : (_encounter?.Enemy.CurrentHealth ?? 1);
        public virtual int EnemyHpMax => _isGroupCombat ? (SelectedGroupMonster?.MaxHp ?? 1) : (_encounter?.Enemy.MaxHealth     ?? 1);
        public virtual int EnemyLevel => _isGroupCombat ? (SelectedGroupMonster?.Level ?? 1) : (_encounter?.Enemy.Level ?? 1);
        public int EnemyMp    => 0;
        public int EnemyMpMax => 1;

        public string EnemyHpText  => $"{Localization.T("pg.fight.label.enemy_hp")}: {EnemyHp}";
        public string CharacterHpText => $"{Localization.T("pg.fight.label.your_hp")}: {CharacterHp}";
        public string CharacterMpText => $"{Localization.T("pg.fight.label.your_mp")}: {CharacterMp}";
        public string TurnText     => $"{Localization.T("pg.fight.label.turn")}: {CurrentTurnCharacterName}";

        public virtual bool CanAct => _isGroupCombat
            ? _isMyTurn
            : (_encounter != null &&
               _encounter.Phase != CombatPhase.EnemyTurn &&
               _encounter.Phase != CombatPhase.Recovery &&
               _encounter.Phase != CombatPhase.Finished);

        public ObservableCollection<LogLineVm>   LogLines { get; } = new();
        public ObservableCollection<FightSkillVm> Skills  { get; } = new();

        public ICommand AttackCommand        { get; protected set; }
        public ICommand RunCommand           { get; protected set; }
        public ICommand CastSkillCommand     { get; protected set; }
        public ICommand SelectMonsterCommand { get; protected set; }

        public IEnumerable<GroupCombatantVm> OtherMonsters =>
            GroupMonsters.Where((m, i) => i != _selectedMonsterIndex && m.IsAlive);
        public bool HasOtherMonsters => OtherMonsters.Any();

        protected Monster _monster;

        // ── Single-player constructor ─────────────────────────────────────────

        public ViewModel_PageFight()
        {
            _monster = MonsterService.GetMonsterById(1);
            var character = UserAccountService.CurrentCharacter;

            SetupCommands();

            if (PendingGroupCombat != null)
            {
                IsGroupCombat = true;
                _encounter    = null;
                var pend      = PendingGroupCombat;
                PendingGroupCombat = null;

                if (PendingLocalEncounter != null)
                {
                    _groupEncounter       = PendingLocalEncounter;
                    PendingLocalEncounter = null;
                    _isLocalGroupCombat   = true;
                }

                InitGroupCombat(pend);
                _lastGroupLogIndex = _groupEncounter?.Log.Count ?? 0;

                foreach (var (skill, source) in SkillSlotService.GetCombatSkills(character))
                    Skills.Add(new FightSkillVm(skill, DetermineTag(character, skill, source)));
            }
            else
            {
                if (character.CurrentRoom.HasMonsters)
                    _monster = MonsterService.PickMonsterForFight(
                        character.CurrentRoom.Monsters,
                        character.CurrentRoom.EncounterableMonsters);

                _encounter = new CombatEncounter(character, _monster);
                _encounter.MonsterKilled += OnMonsterKilled;
                character.HealthChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(CharacterHp));
                    OnPropertyChanged(nameof(CharacterHpMax));
                    OnPropertyChanged(nameof(CharacterHpText));
                };
                character.ManaChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(CharacterMp));
                    OnPropertyChanged(nameof(CharacterMpMax));
                    OnPropertyChanged(nameof(CharacterMpText));
                };

                foreach (var (skill, source) in SkillSlotService.GetCombatSkills(character))
                    Skills.Add(new FightSkillVm(skill, DetermineTag(character, skill, source)));

                FlushNewLogEntries();
                RaiseAll();
            }

            ActiveFight = this;
        }

        // ── Protected no-init constructor for multiplayer subclass ────────────

        protected ViewModel_PageFight(bool _)
        {
            _monster = MonsterService.GetMonsterById(1);
            SetupCommands();
            ActiveFight = this;
        }

        private void SetupCommands()
        {
            AttackCommand        = new RelayCommand(() => _ = AttackAsync(), CanActMethod);
            CastSkillCommand     = new RelayCommand<FightSkillVm>(vm => _ = CastSkillAsync(vm), _ => CanAct);
            RunCommand           = new RelayCommand(Run);
            SelectMonsterCommand = new RelayCommand<GroupCombatantVm>(vm =>
            {
                if (vm == null) return;
                int idx = GroupMonsters.IndexOf(vm);
                if (idx >= 0) SelectedMonsterIndex = idx;
            });
        }

        // ── Group combat helpers ─────────────────────────────────────────────

        protected virtual void InitGroupCombat(StartGroupCombatResult result)
        {
            GroupCharacters.Clear();
            foreach (var p in result.Characters)
                GroupCharacters.Add(new GroupCombatantVm(p.Name, p.Hp, p.MaxHp, p.IsAlive));

            GroupMonsters.Clear();
            foreach (var m in result.Monsters)
                GroupMonsters.Add(new GroupCombatantVm(m.Name, m.Hp, m.MaxHp, m.IsAlive, m.Level));

            CurrentTurnCharacterName = result.CurrentTurnCharacterName ?? "";
            var myName = UserAccountService.CurrentCharacter.Name;
            IsMyTurn = string.Equals(CurrentTurnCharacterName, myName, StringComparison.OrdinalIgnoreCase);

            _selectedMonsterIndex = GroupMonsters.Count > 0
                ? Math.Max(0, GroupMonsters.IndexOf(GroupMonsters.FirstOrDefault(m => m.IsAlive) ?? GroupMonsters[0]))
                : 0;

            LogLines.Add(LogLineVm.From("pg.fight.log.group_start",
                Localization.T("pg.fight.log.group_start", LocalizationText.LocalizeMonsterName(""))));
            RaiseAll();
        }

        protected virtual void OnGroupCombatUpdated(GroupCombatSnapshot snap)
        {
            foreach (var msg in snap.LogEntries)
                LogLines.Add(LogLineVm.From(msg.Key,
                    Localization.T(msg.Key, LocalizationText.LocalizeMonsterArgs(msg.Args.Cast<object>()))));

            for (int i = 0; i < snap.Characters.Count && i < GroupCharacters.Count; i++)
            {
                GroupCharacters[i].Hp      = snap.Characters[i].Hp;
                GroupCharacters[i].IsAlive = snap.Characters[i].IsAlive;
            }

            for (int i = 0; i < snap.Monsters.Count && i < GroupMonsters.Count; i++)
            {
                GroupMonsters[i].Hp      = snap.Monsters[i].Hp;
                GroupMonsters[i].IsAlive = snap.Monsters[i].IsAlive;
            }

            CurrentTurnCharacterName = snap.CurrentTurnCharacterName ?? "";
            var myName = UserAccountService.CurrentCharacter.Name;
            IsMyTurn = string.Equals(CurrentTurnCharacterName, myName, StringComparison.OrdinalIgnoreCase);

            RaiseAll();
        }

        protected virtual void OnGroupCombatFinished(GroupCombatSnapshot snap)
        {
            foreach (var msg in snap.LogEntries)
                LogLines.Add(LogLineVm.From(msg.Key,
                    Localization.T(msg.Key, LocalizationText.LocalizeMonsterArgs(msg.Args.Cast<object>()))));

            // The server only ever sends one of "GroupCombatUpdated" or "GroupCombatFinished"
            // per turn, never both — so the finishing turn's HP values only ever arrive here.
            for (int i = 0; i < snap.Characters.Count && i < GroupCharacters.Count; i++)
            {
                GroupCharacters[i].Hp      = snap.Characters[i].Hp;
                GroupCharacters[i].IsAlive = snap.Characters[i].IsAlive;
            }

            for (int i = 0; i < snap.Monsters.Count && i < GroupMonsters.Count; i++)
            {
                GroupMonsters[i].Hp      = snap.Monsters[i].Hp;
                GroupMonsters[i].IsAlive = snap.Monsters[i].IsAlive;
            }

            IsMyTurn = false;

            GameHubService.GroupCombatUpdated  -= OnGroupCombatUpdated;
            GameHubService.GroupCombatFinished -= OnGroupCombatFinished;

            if (snap.CharactersWon)
            {
                GameHubService.CharacterRespawned -= OnCharacterRespawned;
                SkillFactory.UpdateSkills(UserAccountService.CurrentCharacter);
                ViewModel_PageRoom.WriteLog(Localization.T("msg.fight.won"));
                Navigation.Current.SetFightState(false);
                Navigation.Current.Navigate(Nav.Room);
            }
        }

        protected static string DetermineTag(Character character, Skill skill, SlottedSkillSource source) =>
            source switch
            {
                SlottedSkillSource.Combined        => "Combined",
                SlottedSkillSource.CompositeFusion => "Fusion",
                _                                  => ""
            };

        protected bool CanActMethod() => CanAct;

        protected virtual async Task AttackAsync()
        {
            if (_isGroupCombat && _isLocalGroupCombat && _groupEncounter != null)
            {
                var lp = UserAccountService.CurrentCharacter;
                _groupEncounter.CharacterAttack(lp.Name, _selectedMonsterIndex);
                SyncFromGroupEncounter();
                FlushGroupLog();
                if (!_groupEncounter.IsFinished)
                {
                    CurrentTurnCharacterName = _groupEncounter.CurrentTurnCharacterName;
                    IsMyTurn = string.Equals(CurrentTurnCharacterName, lp.Name, StringComparison.OrdinalIgnoreCase);
                }
                RaiseAll();
                CheckLocalGroupFinished();
                return;
            }

            if (_encounter != null)
            {
                _encounter.CharacterAttack();
                FlushNewLogEntries();
                CheckLocalCombatFinished();
                RaiseAll();
            }
        }

        protected virtual async Task CastSkillAsync(FightSkillVm? vm)
        {
            if (vm == null) return;

            if (_isGroupCombat && _isLocalGroupCombat && _groupEncounter != null)
            {
                int targetIdx = vm.Skill.Target == SkillTarget.SingleAlly ? 0 : _selectedMonsterIndex;
                var lp = UserAccountService.CurrentCharacter;
                _groupEncounter.CharacterCastSkill(lp.Name, vm.Skill, targetIdx);
                SyncFromGroupEncounter();
                FlushGroupLog();
                if (!_groupEncounter.IsFinished)
                {
                    CurrentTurnCharacterName = _groupEncounter.CurrentTurnCharacterName;
                    IsMyTurn = string.Equals(CurrentTurnCharacterName, lp.Name, StringComparison.OrdinalIgnoreCase);
                }
                RaiseAll();
                CheckLocalGroupFinished();
                return;
            }

            if (_encounter != null)
            {
                _encounter.CharacterBeginCast(vm.Skill);
                FlushNewLogEntries();
                CheckLocalCombatFinished();
                RaiseAll();
            }
        }

        protected virtual void Run()
        {
            Navigation.Current.SetFightState(false);
            ViewModel_PageRoom.WriteLog(Localization.T("msg.fight.run.success"));
            Navigation.Current.Navigate(Nav.Room);
        }

        protected void OnCharacterRespawned(int roomId, string roomName)
        {
            GameHubService.CharacterRespawned -= OnCharacterRespawned;
            var character = UserAccountService.CurrentCharacter;
            character.CurrentRoomId = roomId;
            character.CurrentRoom   = RoomService.GetRoomById(roomId);
            Navigation.Current.SetFightState(false);
            ViewModel_PageRoom.Reload();
            Navigation.Current.Navigate(Nav.Room);
        }

        protected void CheckLocalCombatFinished()
        {
            if (_encounter == null || _encounter.Phase != CombatPhase.Finished) return;

            var character = UserAccountService.CurrentCharacter;

            if (!character.IsAlive)
            {
                character.ApplyDeathXpPenalty();

                int respawnRoomId = character.LastHealerRoomId ?? 1;
                var respawnRoom   = Myria.Lib.Core.Services.RoomService.AllRooms.FirstOrDefault(r => r.Id == respawnRoomId)
                                    ?? Myria.Lib.Core.Services.RoomService.AllRooms.FirstOrDefault();
                if (respawnRoom is not null)
                {
                    character.CurrentRoomId = respawnRoom.Id;
                    character.CurrentRoom   = respawnRoom;
                }
                character.Heal(int.MaxValue);
                character.RestoreMana(int.MaxValue);
                ViewModel_PageRoom.WriteLog(Localization.T("msg.fight.lost"));
            }

            Navigation.Current.SetFightState(false);
            ViewModel_PageRoom.Reload();
            Navigation.Current.Navigate(Nav.Room);
        }

        protected virtual void SyncFromGroupEncounter()
        {
            if (_groupEncounter == null) return;

            var chars = _groupEncounter.Characters;
            for (int i = 0; i < chars.Count && i < GroupCharacters.Count; i++)
            {
                GroupCharacters[i].Hp      = chars[i].CurrentHealth;
                GroupCharacters[i].IsAlive = chars[i].IsAlive;
            }

            for (int i = 0; i < _groupEncounter.Monsters.Count && i < GroupMonsters.Count; i++)
            {
                GroupMonsters[i].Hp      = _groupEncounter.Monsters[i].CurrentHealth;
                GroupMonsters[i].IsAlive = _groupEncounter.Monsters[i].IsAlive;
            }

            if (_selectedMonsterIndex < GroupMonsters.Count && !GroupMonsters[_selectedMonsterIndex].IsAlive)
            {
                int next = GroupMonsters.IndexOf(GroupMonsters.FirstOrDefault(m => m.IsAlive));
                if (next >= 0) SelectedMonsterIndex = next;
            }
        }

        protected virtual void CheckLocalGroupFinished()
        {
            if (_groupEncounter == null || !_groupEncounter.IsFinished) return;

            IsMyTurn = false;

            var character = UserAccountService.CurrentCharacter;
            if (_groupEncounter.CharactersWon)
            {
                SkillFactory.UpdateSkills(character);
                ViewModel_PageRoom.WriteLog(Localization.T("msg.fight.won"));
            }
            else
            {
                character.ApplyDeathXpPenalty();
                int respawnRoomId = character.LastHealerRoomId ?? 1;
                var respawnRoom   = Myria.Lib.Core.Services.RoomService.AllRooms.FirstOrDefault(r => r.Id == respawnRoomId)
                                    ?? Myria.Lib.Core.Services.RoomService.AllRooms.FirstOrDefault();
                if (respawnRoom is not null)
                {
                    character.CurrentRoomId = respawnRoom.Id;
                    character.CurrentRoom   = respawnRoom;
                }
                character.Heal(int.MaxValue);
                character.RestoreMana(int.MaxValue);
                ViewModel_PageRoom.WriteLog(Localization.T("msg.fight.lost"));
            }

            Navigation.Current.SetFightState(false);
            ViewModel_PageRoom.Reload();
            Navigation.Current.Navigate(Nav.Room);
        }

        protected void FlushGroupLog()
        {
            if (_groupEncounter == null) return;
            var log = _groupEncounter.Log;
            while (_lastGroupLogIndex < log.Count)
            {
                var entry = log[_lastGroupLogIndex++];
                LogLines.Add(LogLineVm.From(entry.Key,
                    Localization.T(entry.Key, LocalizationText.LocalizeMonsterArgs(entry.Args))));
            }
        }

        private int _lastLogIndex = 0;
        protected void FlushNewLogEntries()
        {
            if (_encounter == null) return;
            var log = _encounter.Log;
            while (_lastLogIndex < log.Count)
            {
                var entry = log[_lastLogIndex++];
                LogLines.Add(LogLineVm.From(entry.Key,
                    Localization.T(entry.Key, LocalizationText.LocalizeMonsterArgs(entry.Args))));
            }
        }

        private void OnMonsterKilled(object? sender, MonsterKilledEventArgs e)
        {
            ViewModel_PageRoom.WriteLog($"{LocalizationText.LocalizeMonsterName(_monster.Name)} {Localization.T("msg.fight.won")}");
        }

        protected void RaiseAll()
        {
            OnPropertyChanged(nameof(ActiveMonsterName));
            OnPropertyChanged(nameof(CharacterHp));
            OnPropertyChanged(nameof(CharacterHpMax));
            OnPropertyChanged(nameof(CharacterMp));
            OnPropertyChanged(nameof(CharacterMpMax));
            OnPropertyChanged(nameof(EnemyHp));
            OnPropertyChanged(nameof(EnemyHpMax));
            OnPropertyChanged(nameof(EnemyMp));
            OnPropertyChanged(nameof(EnemyMpMax));
            OnPropertyChanged(nameof(EnemyLevel));
            OnPropertyChanged(nameof(OtherMonsters));
            OnPropertyChanged(nameof(HasOtherMonsters));
            OnPropertyChanged(nameof(EnemyHpText));
            OnPropertyChanged(nameof(CharacterHpText));
            OnPropertyChanged(nameof(CharacterMpText));
            OnPropertyChanged(nameof(TurnText));
            OnPropertyChanged(nameof(CanAct));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            if (_encounter != null && EnemyHp < 1)
            {
                Navigation.Current.SetFightState(false);
                Navigation.Current.Navigate(Nav.Room);
            }
        }
    }}

    public sealed class LogLineVm
    {
        public string Text        { get; }
        public Brush  Color       { get; }
        public string Bullet      { get; }
        public Brush  BulletColor { get; }

        private LogLineVm(string text, Brush color, string bullet, Brush bulletColor)
        {
            Text = text; Color = color; Bullet = bullet; BulletColor = bulletColor;
        }

        public static LogLineVm From(string key, string text) =>
            new LogLineVm(text, s_colorMap.GetValueOrDefault(key, s_default), BulletFor(key), BulletColorFor(key));

        private static string BulletFor(string key)      => key == "pg.fight.log.critHit" ? "★" : "›";
        private static Brush  BulletColorFor(string key) => key == "pg.fight.log.critHit" ? s_crit : s_gold;

        private static readonly Brush s_default   = F(0xD4, 0xC4, 0xA8);
        private static readonly Brush s_playerHit = F(0xE8, 0xC4, 0x7A);
        private static readonly Brush s_crit      = F(0xFF, 0xE0, 0x66);
        private static readonly Brush s_muted     = F(0x9A, 0x8A, 0x68);
        private static readonly Brush s_enemyHit  = F(0xCF, 0x66, 0x79);
        private static readonly Brush s_heal      = F(0x6F, 0xCF, 0x97);
        private static readonly Brush s_skill     = F(0x65, 0xB5, 0xE8);
        private static readonly Brush s_victory   = F(0xD4, 0xAA, 0x3C);
        private static readonly Brush s_defeat    = F(0x8B, 0x22, 0x22);
        private static readonly Brush s_manaLow   = F(0x4A, 0x7A, 0x9B);
        private static readonly Brush s_gold      = F(0xC9, 0xA8, 0x4C);

        private static Brush F(byte r, byte g, byte b)
        {
            var b2 = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            b2.Freeze();
            return b2;
        }

        private static readonly Dictionary<string, Brush> s_colorMap = new()
        {
            ["pg.fight.log.critHit"]   = s_crit,
            ["pg.fight.log.hit"]       = s_playerHit,
            ["pg.fight.log.skillHit"]  = s_skill,
            ["pg.fight.log.heal"]      = s_heal,
            ["pg.fight.log.miss"]      = s_muted,
            ["pg.fight.log.nomana"]    = s_manaLow,
            ["pg.fight.log.enemyHit"]  = s_enemyHit,
            ["pg.fight.log.enemyMiss"] = s_muted,
            ["pg.fight.log.win"]       = s_victory,
            ["pg.fight.log.lose"]      = s_defeat,
        };
    }

    public class FightSkillVm
    {
        public Skill  Skill       { get; }
        public string Tag         { get; }
        public bool   HasTag      => !string.IsNullOrEmpty(Tag);
        public string ButtonLabel { get; }
        public string Name           => Skill.Name;
        public string Description    => Skill.Description;
        public int    ManaCost       => Skill.ManaCost;
        public string ManaCostText   => $"{Localization.T("pg.fight.label.mana")}: {Skill.ManaCost}";

        public FightSkillVm(Skill skill, string tag = "")
        {
            Skill = skill;
            Tag   = tag;
            string firstWord = skill.Name.Split(' ')[0];
            ButtonLabel = firstWord.Length <= 5 ? firstWord : firstWord[..5];
        }
    }
