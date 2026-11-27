using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Entities.Monsters;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;
using System.Linq;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Events;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages.Game;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using Myria.Wpf.ViewModel.UserControls;
using Myria.Wpf.View.Pages.Game.IngameWindow.NpcInteraction;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Dungeons;

namespace Myria.Wpf.ViewModel.Pages.Game
{
    public class ViewModel_PageRoom : BaseViewModel
    {
        private static ViewModel_PageRoom _instantce;

        protected Character character;
        protected Room currentRoom;

        private bool _hasNorth;
        private bool _hasSouth;
        private bool _hasWest;
        private bool _hasEast;
        private string _btnNorth = string.Empty;
        private string _btnSouth = string.Empty;
        private string _btnWest = string.Empty;
        private string _btnEast = string.Empty;
        private string _tblLog = string.Empty;
        private string _btnFight = string.Empty;
        private string _btnGroupFight = string.Empty;
        private string _btnGather = string.Empty;
        private string _btnNpcs = string.Empty;
        private string _btnTalk = string.Empty;
        private string _btnGuildHouse = string.Empty;
        private bool _hasGuildEntry;

        private string _roomName = string.Empty;
        private string _roomDescription = string.Empty;
        private string _imageSource = string.Empty;

        private System.Windows.Visibility btnFightVisibility;
        private System.Windows.Visibility btnGatherVisibility;
        private System.Windows.Visibility _hasNpcsVisibility;

        private bool _hasMonsters;
        private bool _canGather;
        private bool _hasNpcs;
        private Npc _selectedNpc;

        [LocalizedKey("game.log")]
        public string TblLog
        {
            get => _tblLog;
            set { _tblLog = value; OnPropertyChanged(); }
        }

        [LocalizedKey("game.exit.north")]
        public string BtnNorth
        {
            get => _btnNorth;
            set { _btnNorth = value; OnPropertyChanged(); }
        }
        [LocalizedKey("game.exit.south")]
        public string BtnSouth
        {
            get => _btnSouth;
            set { _btnSouth = value; OnPropertyChanged(); }
        }
        [LocalizedKey("game.exit.west")]
        public string BtnWest
        {
            get => _btnWest;
            set { _btnWest = value; OnPropertyChanged(); }
        }
        [LocalizedKey("game.exit.east")]
        public string BtnEast
        {
            get => _btnEast;
            set { _btnEast = value; OnPropertyChanged(); }
        }
        [LocalizedKey("app.general.UI.fight")]
        public string BtnFight
        {
            get => _btnFight;
            set { _btnFight = value;  OnPropertyChanged(); }
        }

        [LocalizedKey("app.general.UI.group_fight")]
        public string BtnGroupFight
        {
            get => _btnGroupFight;
            set { _btnGroupFight = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.room.UI.gather")]
        public string BtnGather
        {
            get => _btnGather;
            set { _btnGather = value; OnPropertyChanged(); }
        }
        [LocalizedKey("app.general.UI.npcs")]
        public string BtnNpcs
        {
            get => _btnNpcs;
            set { _btnNpcs = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.room.UI.talk")]
        public string BtnTalk
        {
            get => _btnTalk;
            set { _btnTalk = value; OnPropertyChanged(); }
        }
        [LocalizedKey("game.exit.guild_house")]
        public string BtnGuildHouse
        {
            get => _btnGuildHouse;
            set { _btnGuildHouse = value; OnPropertyChanged(); }
        }

        public bool HasGuildEntry
        {
            get => _hasGuildEntry;
            set { _hasGuildEntry = value; OnPropertyChanged(); }
        }

        public string RoomName 
        { 
            get => _roomName;
            private set { _roomName = value; OnPropertyChanged(); }
        }
        public string RoomDescription 
        { 
            get => _roomDescription;
            private set { _roomDescription = value; OnPropertyChanged(); }
        }
        public string ImageSource
        {
            get => _imageSource;
            set { _imageSource = value; OnPropertyChanged(); }
        }


        // Exit flags
        public bool HasNorth { get => _hasNorth; set { _hasNorth = value; OnPropertyChanged(); } }
        public bool HasEast { get => _hasEast; set { _hasEast = value; OnPropertyChanged(); } }
        public bool HasSouth { get => _hasSouth; set { _hasSouth = value; OnPropertyChanged(); } }
        public bool HasWest { get => _hasWest; set { _hasWest = value; OnPropertyChanged(); } }

        // v0.3 dungeons: what's left in this room of the run (or that it's cleared); empty elsewhere.
        private string _dungeonStatus = string.Empty;
        public string DungeonStatus
        {
            get => _dungeonStatus;
            protected set
            {
                _dungeonStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DungeonStatusVisibility));
            }
        }
        public System.Windows.Visibility DungeonStatusVisibility =>
            string.IsNullOrEmpty(_dungeonStatus) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

        public System.Windows.Visibility BtnFightVisibility { get => btnFightVisibility; set { btnFightVisibility = value; OnPropertyChanged(); } }
        public System.Windows.Visibility BtnGatherVisibility { get => btnGatherVisibility; set { btnGatherVisibility = value; OnPropertyChanged(); } }
        public System.Windows.Visibility HasNpcsVisibility { get => _hasNpcsVisibility; set { _hasNpcsVisibility = value; OnPropertyChanged(); } }
        public bool HasMonsters {
            get => _hasMonsters;
            set 
            { 
                _hasMonsters = value;
                if (value)
                    BtnFightVisibility = System.Windows.Visibility.Visible;
                else
                    BtnFightVisibility = System.Windows.Visibility.Hidden;

                OnPropertyChanged(); 
            }
        }
        public bool CanGather
        {
            get => _canGather;
            set 
            {
                _canGather = value;
                if (value)
                    BtnGatherVisibility = System.Windows.Visibility.Visible;
                else
                    BtnGatherVisibility = System.Windows.Visibility.Hidden;

                OnPropertyChanged();
            }
        }
        public bool HasNpcs
        {
            get => _hasNpcs;
            set
            {
                _hasNpcs = value;
                if (value)
                    HasNpcsVisibility = System.Windows.Visibility.Visible;
                else
                    HasNpcsVisibility = System.Windows.Visibility.Collapsed;

                OnPropertyChanged();
            }
        }

        public Npc SelectedNpc
        {
            get => _selectedNpc;
            set
            {
                _selectedNpc = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedNpcName));
                OnPropertyChanged(nameof(SelectedNpcDescription));
            }
        }
        public string SelectedNpcName => _selectedNpc?.ToString() ?? string.Empty;
        public string SelectedNpcDescription => _selectedNpc != null ? Localization.T(_selectedNpc.DescriptionKey) : string.Empty;

        public ObservableCollection<string> Log { get; set; }
        public ObservableCollection<Npc> Npcs { get; set; }

        // Commands
        public ICommand MoveCommand { get; }
        public ICommand StartFightCommand { get; }
        public ICommand StartGroupFightLocalCommand { get; }
        public ICommand OpenNpcsCommand { get; }
        public ICommand TalkCommand { get; }
        public ICommand StartGatheringCommand { get; }

        // ── PvP arena panel (v0.3) - multiplayer only, filled by ViewModel_PageRoomMultiplayer ──
        public ObservableCollection<PvpTargetVm> PvpTargets { get; } = new();
        private bool _isPvpRoom;
        public bool IsPvpRoom { get => _isPvpRoom; protected set { _isPvpRoom = value; OnPropertyChanged(); } }
        public bool HasNoPvpTargets => PvpTargets.Count == 0;
        private string _pvpStatsText = "";
        public string PvpStatsText { get => _pvpStatsText; protected set { _pvpStatsText = value; OnPropertyChanged(); } }
        public string TblPvpTitle     => Localization.T("pvp.title");
        public string TblPvpChallenge => Localization.T("pvp.challenge_header");
        public string TblPvpNone      => Localization.T("pvp.none");
        public string TblPvpRefresh   => Localization.T("pvp.refresh");
        public ICommand RefreshPvpTargetsCommand { get; }

        /// <summary>Called at the end of every RefreshRoomFlags (room entered/reloaded). Offline
        /// there's no PvP, so the base just keeps the arena panel hidden.</summary>
        protected virtual void OnRoomFlagsRefreshed() => IsPvpRoom = false;
        protected virtual Task RefreshPvpAsync() => Task.CompletedTask;
        public ICommand EnterGuildCommand { get; }

        public ViewModel_PageRoom()
        {
            // ViewModel_PageGame's own constructor forces the next room navigation to build a
            // fresh ViewModel_PageRoom on every character switch/rejoin (see its
            // Navigation.Current.InvalidateCache(Nav.Room) comment) - without this, every past
            // instance's 9 static subscriptions below stayed alive forever, each independently
            // reacting to every future server broadcast, and OnHubConnected re-firing a full
            // EstablishServerSessionAsync (LoadCharacter/SetCharacterName/JoinRoom) per leaked
            // instance on every future reconnect. Same bug class, same fix shape as
            // ViewModel_PageGame's own leak (item 48) and CharacterPageViewModel's (item 57) -
            // reuses the _instantce field this class already had for its "last instance wins"
            // static callbacks, rather than adding a second one.
            _instantce?.Unsubscribe();

            character = UserAccountService.CurrentCharacter;

            Npcs = new ObservableCollection<Npc>();
            Log = new ObservableCollection<string>();

            MoveCommand = new RelayCommand<string>(Move);
            StartFightCommand          = new RelayCommand(StartFight);
            StartGroupFightLocalCommand = new RelayCommand(StartGroupFightLocal);
            TalkCommand = new RelayCommand(TalkNpc);
            StartGatheringCommand = new RelayCommand(StartGathering);
            RefreshPvpTargetsCommand = new RelayCommand(() => _ = RefreshPvpAsync());
            EnterGuildCommand = new RelayCommand(EnterGuild);

            currentRoom = RoomService.GetRoomById(character.CurrentRoom.Id);
            RoomName = ResolveName(currentRoom);
            RoomDescription = ResolveDescription(currentRoom);
            ImageSource = "/Data/images/rooms/" + currentRoom.Name + ".jpg";

            HasNpcs = currentRoom.Npcs != null && currentRoom.Npcs.Count > 0;
            Npcs.Clear();
            foreach (Npc npc in currentRoom.NpcRefs)
            {
                Npcs.Add(npc);
            }

            RefreshRoomFlags();
            _instantce = this;
            _ = GuildPropertyCache.RefreshAsync().ContinueWith(_ =>
                System.Windows.Application.Current.Dispatcher.Invoke(RefreshGuildEntry));

            character.SkillLearned += OnCharacterSkillLearned;
            character.Inventory.ItemReceived += OnItemReceived;
            character.Inventory.ItemRemoved += OnItemRemoved;
            character.Inventory.ItemSold += OnItemSold;
            character.XpGained += OnXpGained;
            character.LeveledUp += OnLeveledUp;
            GameLog.EntryAdded += OnGameLogEntry;

            GameHubService.HubConnected += OnHubConnected;
            GameHubService.CharacterEntered += OnCharacterEntered;
            GameHubService.CharacterLeft += OnCharacterLeft;
            GameHubService.RoomCharactersReceived += OnRoomCharacters;
            GameHubService.CharacterGathering += OnCharacterGathering;
            GameHubService.CharacterCrafting += OnCharacterCrafting;
            GameHubService.CharacterUpgrading += OnCharacterUpgrading;
            GameHubService.CharacterInCombat += OnCharacterInCombat;
            GameHubService.CharacterCombatEnded += OnCharacterCombatEnded;
            // Register character name, join room, and load the character into the server session
            // immediately if already connected; OnHubConnected will handle it if the connection is
            // still being established. LoadCharacterOnServerAsync must run here too - not just from
            // OnHubConnected - because ConnectAsync() fires the HubConnected event as its very last
            // step before returning; if the caller already awaited ConnectAsync() before navigating
            // here (as ViewModel_PageGame now does), that event has already fired and this
            // subscription registered too late to ever catch it, leaving no session character on
            // the server at all.
            _ = EstablishServerSessionAsync();
        }

        /// <summary>
        /// Removes this instance's 9 static-event subscriptions (GameLog + GameHubService).
        /// Called on the outgoing "_instantce" right before a new ViewModel_PageRoom takes over -
        /// same shape as ViewModel_PageGame.Unsubscribe and CharacterPageViewModel.Unsubscribe.
        /// </summary>
        private void Unsubscribe()
        {
            GameLog.EntryAdded -= OnGameLogEntry;

            GameHubService.HubConnected -= OnHubConnected;
            GameHubService.CharacterEntered -= OnCharacterEntered;
            GameHubService.CharacterLeft -= OnCharacterLeft;
            GameHubService.RoomCharactersReceived -= OnRoomCharacters;
            GameHubService.CharacterGathering -= OnCharacterGathering;
            GameHubService.CharacterCrafting -= OnCharacterCrafting;
            GameHubService.CharacterUpgrading -= OnCharacterUpgrading;
            GameHubService.CharacterInCombat -= OnCharacterInCombat;
            GameHubService.CharacterCombatEnded -= OnCharacterCombatEnded;
        }

        private void OnHubConnected()
        {
            _ = EstablishServerSessionAsync();
        }

        // These three used to fire unawaited in parallel. LoadCharacterOnServerAsync is what
        // actually attaches a live Character to this connection server-side (session.TryAdd in
        // GameHub.LoadCharacter) - every other hub action (slotting a skill, attacking, etc.)
        // looks that session up and silently no-ops/fails if it isn't there yet. Racing a fast
        // player action against this call (e.g. opening the skill page and slotting something
        // within the first second of joining) meant the action could reach the server before the
        // session existed, fail, and get reverted client-side - looking like the assignment
        // "didn't take" even though the client-side click was never actually at fault. Awaiting
        // it first closes that window; SetCharacterName/JoinRoom stay fire-and-forget relative to
        // each other since neither depends on the other, only on the session existing.
        private async Task EstablishServerSessionAsync()
        {
            // Nothing else ever retries this if it fails outright (a transient hiccup right after
            // connecting, DB replication lag right after a fresh character save, etc.) - previously
            // that meant the whole rest of the session was stuck with no server-side Character at
            // all, only fixable by a full relog (a fresh connection gets a fresh attempt). A few
            // short retries here covers the transient case without needing that.
            bool loaded = false;
            for (int attempt = 0; attempt < 3 && !loaded; attempt++)
            {
                if (attempt > 0) await Task.Delay(400);
                loaded = await GameHubService.LoadCharacterOnServerAsync(character.Name);
            }

            _ = GameHubService.SetCharacterNameAsync(character.Name);
            _ = GameHubService.JoinRoomAsync(currentRoom.Id);
        }

        private void OnCharacterEntered(string username) =>
            WriteLog($"? {username} entered the room.");

        private void OnCharacterLeft(string username) =>
            WriteLog($"? {username} left the room.");

        private void OnRoomCharacters(List<string> characters)
        {
            if (characters.Count == 0) return;
            WriteLog($"Also here: {string.Join(", ", characters)}");
        }

        private void OnCharacterGathering(string username) =>
            WriteLog($"{username} is gathering nearby.");

        private void OnCharacterCrafting(string username) =>
            WriteLog($"{username} is crafting nearby.");

        private void OnCharacterUpgrading(string username) =>
            WriteLog($"{username} is upgrading gear nearby.");

        private void OnCharacterInCombat(string username) =>
            WriteLog($"{username} has engaged in combat nearby.");

        private void OnCharacterCombatEnded(string username) =>
            WriteLog($"{username} has finished fighting.");

        private void OnXpGained(object? sender, XpGainedEventArgs e)
        {
            WriteLog(Localization.T("log.xp.gained", e.Amount));
        }
        private void OnLeveledUp(object? sender, LevelUpEventArgs e)
        {
            WriteLog(Localization.T("log.level.up", e.NewLevel));
        }
        private void OnItemReceived(object? sender, ItemReceivedEventArgs e)
        {
            if (_gatherInFlight) return;   // reported as "You gathered ..." instead
            WriteLog(Localization.T("log.item.received", e.Amount, Localization.T(e.Item.Name)));
        }
        private void OnItemRemoved(object? sender, ItemReceivedEventArgs e)
        {
            WriteLog(Localization.T("log.item.removed", e.Amount, Localization.T(e.Item.Name)));
        }
        private void OnItemSold(object? sender, ItemReceivedEventArgs e)
        {
            WriteLog(Localization.T("log.item.sold", e.Amount, Localization.T(e.Item.Name)));
        }
        private void OnCharacterSkillLearned(object? sender, SkillLearnedEventArgs e)
        {
            WriteLog(Localization.T("log.skill.learned", Localization.T(e.Skill.Name)));
        }
        private void OnGameLogEntry(GameLogEntry entry)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                string prefix = entry.IsError ? "[!] " : string.Empty;
                WriteLog(prefix + entry.Message);
            });
        }
        public static void WriteLog(string msg)
        {
            if (_instantce == null) return;
            _instantce.Log.Add(msg);
            if (_instantce.Log.Count > 100)
                _instantce.Log.Remove(_instantce.Log[0]);
        }

        public static void Reload()
        {
            if (_instantce == null) return;
            _instantce.ReloadFromCurrentRoom();
        }

        private static string ResolveName(Room room) =>
            room.IsLiteral ? room.Name : Localization.T(room.Name);

        private static string ResolveDescription(Room room) =>
            room.IsLiteral ? room.Description : Localization.T(room.Description);

        public static void RefreshGuildEntryStatic()
        {
            if (_instantce == null) return;
            _instantce.RefreshGuildEntry();
        }

        private void ReloadFromCurrentRoom()
        {
            currentRoom = RoomService.GetRoomById(character.CurrentRoom.Id);
            RoomName        = ResolveName(currentRoom);
            RoomDescription = ResolveDescription(currentRoom);
            ImageSource     = "/Data/images/rooms/" + currentRoom.Name + ".jpg";

            Npcs.Clear();
            foreach (Npc npc in currentRoom.NpcRefs)
                Npcs.Add(npc);

            RefreshRoomFlags();
        }
        private void TalkNpc()
        {
            if (SelectedNpc == null) return;
            var npcWin  = MainWindow.Instance.npcWindow;
            var npcName = Localization.T(SelectedNpc.NameKey);
            var page    = new Page_GeneralNpcInteraction(SelectedNpc);
            npcWin.NavigateTo(page, npcName);
        }
        public static void RefreshLocalisation()
        {
            if (_instantce == null || _instantce.currentRoom == null)
                return;
            _instantce.RoomName = ResolveName(_instantce.currentRoom);
        }
        public void StartGathering() => _ = ExecuteGather();

        protected virtual Task ExecuteGather()
        {
            StartGatheringLocal();
            return Task.CompletedTask;
        }

        // While a gather request is in flight, the server's inventory push (which arrives before the
        // answer) adds the item - its generic "received" line would duplicate "You gathered ...".
        private bool _gatherInFlight;

        protected async Task StartGatheringServerAsync()
        {
            Myria.Lib.Core.Models.Dto.GatherActionResult? result;
            _gatherInFlight = true;
            try { result = await GameHubService.GatherAsync(currentRoom.Id); }
            finally { _gatherInFlight = false; }
            if (result == null || !result.Success)
            {
                string key = result?.Reason switch
                {
                    "depleted"      => "log.gather.depleted",
                    "no_tool"       => "log.gather.no_tool_generic",
                    "skill_too_low" => "log.gather.skill_too_low",
                    _               => "log.gather.fail"
                };
                WriteLog(Localization.T(key));
                if (result?.Reason == "depleted")
                {
                    currentRoom.GathersRemaining = 0;
                    RefreshRoomFlags();
                }
                return;
            }

            // The server's inventory push already added the item (adding it here too doubled it until
            // the next sync, and logged it three times) - only report it.
            if (result.ItemId != null && ItemFactory.GetItemName(result.ItemId) is { } itemName)
                WriteLog(Localization.T("log.gather.success", result.Amount, Localization.T(itemName)));

            // (Job XP came with the same push - granting it here too showed it doubled.)
            currentRoom.GathersRemaining = result.RemainingGathers;
            RefreshRoomFlags();
        }

        private void StartGatheringLocal()
        {
            // Delegates to GatherService.Gather — the same method the multiplayer server now
            // calls (GameHub.Gather) — instead of this ViewModel's own independent copy of the
            // same tool-check/amount/XP logic, which used to be hand-duplicated on both sides.
            var outcome = GatherService.Gather(character, currentRoom);

            if (outcome.Result != GatherResult.Success)
            {
                string key = outcome.Result switch
                {
                    GatherResult.Depleted => "log.gather.depleted",
                    GatherResult.NoTool   => "log.gather.no_tool_generic",
                    GatherResult.SkillTooLow => "log.gather.skill_too_low",
                    _                     => "log.gather.fail"
                };
                WriteLog(Localization.T(key));
                RefreshRoomFlags();
                return;
            }

            string itemName = ItemFactory.GetItemName(outcome.ItemId!) ?? outcome.ItemId!;
            WriteLog(Localization.T("log.gather.success", outcome.Amount, Localization.T(itemName)));
            RefreshRoomFlags();
        }
        protected void RefreshRoomFlags()
        {
            GetDirections();
            HasMonsters = currentRoom.HasMonsters && currentRoom.Monsters.Count > 0;
            RefreshDungeonState();
            bool hasSpots = currentRoom.GatheringSpots != null && currentRoom.GatheringSpots.Count > 0;
            if (ServerApiService.Token != null && GameHubService.IsConnected)
                CanGather = currentRoom.GathersRemaining > 0 && hasSpots;
            else
            {
                int gatherBonus = JobManager.GetGatherKnowledgeBonus(character);
                CanGather = (currentRoom.GathersRemaining + gatherBonus) > 0 && hasSpots;
            }
            HasNpcs = currentRoom.Npcs != null && currentRoom.Npcs.Count > 0;
            RefreshGuildEntry();
            OnRoomFlagsRefreshed();
        }

        /// <summary>Called whenever the page is shown again (e.g. back from a fight) - the cached page
        /// would otherwise keep showing the room as it was before.</summary>
        public void OnPageShown() => RefreshRoomFlags();

        /// <summary>Dungeon rooms: Fight only while the run still has monsters here, plus a status line.
        /// Offline from the local run; multiplayer asks the server (its run is per party).</summary>
        protected virtual void RefreshDungeonState()
        {
            LocalDungeonRuns.NoteLocation(currentRoom);
            if (LocalDungeonRuns.For(currentRoom) is not { } run) { DungeonStatus = string.Empty; return; }
            var remaining = run.Remaining(currentRoom);
            HasMonsters = remaining.Count > 0;
            DungeonStatus = DungeonStatusText(remaining.Select(m => m.Name));
        }

        /// <summary>"Still here: 2× Grotto Crab, Drowned Maka Maka" - or that the room is cleared.</summary>
        protected static string DungeonStatusText(IEnumerable<string> monsterNames)
        {
            var groups = monsterNames.GroupBy(n => n).Select(g => (g.Count() > 1 ? $"{g.Count()}× " : "") + Localization.T(g.Key)).ToList();
            return groups.Count == 0
                ? Localization.T("dungeon.room_cleared")
                : Localization.T("dungeon.remaining", string.Join(", ", groups));
        }

        /// <summary>Log line when a move takes the player out of a dungeon (its run resets).</summary>
        protected static void NoteDungeonExit(Room from, Room to)
        {
            var left = DungeonRules.DungeonIdOf(from);
            if (left is not null && DungeonRules.DungeonIdOf(to) != left)
                WriteLog(Localization.T("dungeon.left"));
        }

        private void RefreshGuildEntry()
        {
            HasGuildEntry = GuildPropertyCache.IsEntryRoom(currentRoom.Id)
                            && GuildPropertyCache.GetFirstBuiltRoomId().HasValue;
        }

        private void EnterGuild()
        {
            var roomId = GuildPropertyCache.GetFirstBuiltRoomId();
            if (roomId == null) return;
            var room = RoomService.GetRoomById(roomId.Value);
            if (room == null) return;
            _ = ExecuteMove(room);
        }
        private void GetDirections()
        {
            HasNorth = false; HasEast = false; HasSouth = false; HasWest = false;
            foreach (string direction in currentRoom.ExitIds.Keys)
            {
                switch (direction)
                {
                    case "north": HasNorth = true; break;
                    case "east": HasEast = true; break;
                    case "south": HasSouth = true; break;
                    case "west": HasWest = true; break;
                }

            }

        }
        // "Fight" and "Group Fight" both run on GroupCombatEncounter now - the only difference is
        // how many monsters get pulled in (1 vs up to 3). Multiplayer overrides both to go through
        // the server instead (StartFightServerAsync / StartGroupFightServerAsync).
        protected virtual void StartFight() => StartLocalFight(maxMonsters: 1);

        protected virtual void StartGroupFightLocal() => StartLocalFight(maxMonsters: 3);

        private void StartLocalFight(int maxMonsters)
        {
            var character = UserAccountService.CurrentCharacter;

            List<Monster> monsters;
            GroupCombatEncounter? dungeonFight = null;
            var run = LocalDungeonRuns.For(currentRoom);
            if (run is not null)
            {
                // Dungeon room: the next pack of the local run's monsters for this room.
                monsters = run.NextPack(currentRoom, maxMonsters, () => dungeonFight is null || !dungeonFight.IsFinished);
                if (monsters.Count == 0)
                {
                    WriteLog(Localization.T("dungeon.room_cleared"));
                    RefreshRoomFlags();
                    return;
                }
            }
            else if (currentRoom.Monsters.Count > 0)
            {
                monsters = maxMonsters == 1
                    // Single monster: plain weighted pick, no variant slot.
                    ? new List<Monster> { MonsterService.PickMonsterForFight(currentRoom.Monsters, currentRoom.EncounterableMonsters).Clone() }
                    // Overworld group: primary + clones + one re-rolled variant
                    : MonsterService.PickGroupWithOneVariant(
                        currentRoom.Monsters, currentRoom.EncounterableMonsters, maxMonsters);
            }
            else
            {
                monsters = new List<Monster>
                    { MonsterService.GetMonsterById(1).Clone() };
            }

            var encounter = new GroupCombatEncounter(new[] { character }, monsters);
            if (run is not null)
            {
                dungeonFight = encounter;
                int roomId = currentRoom.Id;
                encounter.MonsterKilled += (_, e) => { if (e.Monster is not null) run.RecordKill(roomId, e.Monster); };
            }
            ViewModel_PageFight.PendingLocalEncounter = encounter;

            ViewModel_PageFight.PendingGroupCombat = new Myria.Lib.Core.Models.Dto.StartGroupCombatResult(
                Success: true,
                Reason: null,
                Characters: new List<Myria.Lib.Core.Models.Dto.GroupCombatantState>
                {
                    new(character.Name, character.CurrentHealth, character.MaxHealth, true)
                },
                Monsters: monsters.Select(m =>
                    new Myria.Lib.Core.Models.Dto.GroupCombatantState(m.Name, m.CurrentHealth, m.MaxHealth, true, m.Level)).ToList(),
                CurrentTurnCharacterName: character.Name);

            Navigation.Current.Navigate(NavigationFrameType.Game, new Page_Fight());
            Navigation.Current.SetFightState(true);
        }
        private void Move(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            if (!currentRoom.Exits.ContainsKey(dir.ToLower())) return;
            _ = ExecuteMove(currentRoom.Exits[dir.ToLower()]);
        }

        /// <summary>The room's own requirements (level, quest), checked before moving - with the
        /// reason in the log. (Offline a refused move used to do nothing at all, and online a level-
        /// gated dungeon entrance showed "clear this room first".)</summary>
        protected bool MeetsRoomRequirement(Room targetRoom)
        {
            switch (targetRoom.RequirementType)
            {
                case RoomRequirementType.Level when character.Level < targetRoom.AccessLevel:
                    WriteLog(Localization.T("room.locked.level", targetRoom.AccessLevel));
                    return false;
                case RoomRequirementType.Quest when !RoomService.CanEnterRoom(targetRoom, character):
                    WriteLog(Localization.T("room.locked.quest"));
                    return false;
            }
            return true;
        }

        protected virtual async Task ExecuteMove(Room targetRoom)
        {
            if (!MeetsRoomRequirement(targetRoom)) return;
            if (!RoomService.CanEnterRoom(targetRoom, character)) return;
            if (!LocalDungeonRuns.CanMove(currentRoom, targetRoom))
            {
                WriteLog(Localization.T("dungeon.locked"));
                return;
            }
            NoteDungeonExit(currentRoom, targetRoom);
            ApplyRoomMove(targetRoom);
            _ = GameHubService.JoinRoomAsync(currentRoom.Id);
            GameEvents.FireRoomEntered(character, targetRoom);
        }

        protected void ApplyRoomMove(Room targetRoom)
        {
            MainWindow.Instance.npcWindow.Visibility = System.Windows.Visibility.Hidden;
            currentRoom = targetRoom;
            character.CurrentRoom   = currentRoom;
            character.CurrentRoomId = currentRoom.Id;
            RoomName = ResolveName(currentRoom);
            RoomDescription = ResolveDescription(currentRoom);
            ImageSource = "/Data/images/rooms/" + currentRoom.Name + ".jpg";
            if (currentRoom.Npcs != null && currentRoom.Npcs.Count > 0)
            {
                HasNpcs = currentRoom.Npcs.Count > 0;
                Npcs.Clear();
                foreach (Npc npc in currentRoom.NpcRefs)
                    Npcs.Add(npc);
            }
            GetDirections();
            RefreshRoomFlags();
        }

    }

}
