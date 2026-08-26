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
        public ICommand EnterGuildCommand { get; }

        public ViewModel_PageRoom()
        {
            character = UserAccountService.CurrentCharacter;

            Npcs = new ObservableCollection<Npc>();
            Log = new ObservableCollection<string>();

            MoveCommand = new RelayCommand<string>(Move);
            StartFightCommand          = new RelayCommand(StartFight);
            StartGroupFightLocalCommand = new RelayCommand(StartGroupFightLocal);
            TalkCommand = new RelayCommand(TalkNpc);
            StartGatheringCommand = new RelayCommand(StartGathering);
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
            _ = GameHubService.SetCharacterNameAsync(character.Name);
            _ = GameHubService.JoinRoomAsync(currentRoom.Id);
            _ = GameHubService.LoadCharacterOnServerAsync(character.Name);
        }
        private void OnHubConnected()
        {
            _ = GameHubService.SetCharacterNameAsync(character.Name);
            _ = GameHubService.JoinRoomAsync(currentRoom.Id);
            _ = GameHubService.LoadCharacterOnServerAsync(character.Name);
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

        protected async Task StartGatheringServerAsync()
        {
            var result = await GameHubService.GatherAsync(currentRoom.Id);
            if (result == null || !result.Success)
            {
                string key = result?.Reason switch
                {
                    "depleted" => "log.gather.depleted",
                    "no_tool"  => "log.gather.no_tool_generic",
                    _          => "log.gather.fail"
                };
                WriteLog(Localization.T(key));
                if (result?.Reason == "depleted")
                {
                    currentRoom.GathersRemaining = 0;
                    RefreshRoomFlags();
                }
                return;
            }

            if (result.ItemId != null && ItemFactory.TryCreateItem(result.ItemId, out var item))
            {
                item.StackSize = result.Amount;
                character.Inventory.AddItem(item, character, "gather");
                WriteLog(Localization.T("log.gather.success", result.Amount, Localization.T(item.Name)));
            }

            if (!string.IsNullOrEmpty(result.JobId))
                JobManager.GrantSkillXp(character, result.JobId, result.SkillXpGained);

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
            HasMonsters = currentRoom.HasMonsters && !currentRoom.IsCleared
                         && (currentRoom.Monsters.Count > 0 || currentRoom.CurrentMonsters.Count > 0);
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
            if (currentRoom.CurrentMonsters.Count > 0)
            {
                // Dungeon room: use spawned instances directly
                monsters = currentRoom.CurrentMonsters.Take(maxMonsters).ToList();
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

            ViewModel_PageFight.PendingLocalEncounter = new GroupCombatEncounter(new[] { character }, monsters);

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

        protected virtual async Task ExecuteMove(Room targetRoom)
        {
            if (!RoomService.CanEnterRoom(targetRoom, character)) return;
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
