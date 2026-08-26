using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Regestries;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems.Events;
using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Pages.Game;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.Pages.Settings;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using Myria.Wpf.ViewModel.UserControls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using Myria.Wpf.Pages;

namespace Myria.Wpf.ViewModel.Pages.Game
{
    public class ViewModel_PageGame : BaseViewModel
    {
        private string _btn_Inventory = string.Empty;
        private string _btn_Character = string.Empty;
        private string _btn_Skills = string.Empty;
        private string _btn_Quests = string.Empty;
        private string _btn_Map = string.Empty;
        private string _btn_Settings = string.Empty;
        private string _btnFriends = string.Empty;
        private string _tblPartyKick = string.Empty;
        private string _tblPartyPromote = string.Empty;
        private string _tblPartyKickLabel = string.Empty;
        private string _tblWhisperLabel = string.Empty;
        private string _tblLeaveParty = string.Empty;
        private string _tblGroupFight = string.Empty;
        private string _tblAcceptInvite = string.Empty;
        private string _tblDeclineInvite = string.Empty;
        private string _tblAlsoHere = string.Empty;
        private string _tblShopsHere = string.Empty;
        private string _tblOpenShopHere = string.Empty;
        private string _tblChatSend = string.Empty;
        private string _tblProposeTradeLabel = string.Empty;
        private string _tblVisitShopLabel = string.Empty;
        private string _tblInvitePartyLabel = string.Empty;
        private string _tblFriendRequestLabel = string.Empty;
        private string _tblBlockCharacterLabel = string.Empty;
        private string _tblChatAll = string.Empty;
        private string _tblChatRoom = string.Empty;
        private string _tblChatGlobal = string.Empty;
        private string _tblChatParty = string.Empty;
        private string _tblChatWhisper = string.Empty;
        [LocalizedKey("app.general.UI.settings")]
        public string BtnSettings
        {
            get { return _btn_Settings; }
            set
            {
                _btn_Settings = value;
                OnPropertyChanged();
            }
        }
        [LocalizedKey("pg.inventory.title")]
        public string BtnInventory
        {
            get { return _btn_Inventory; }
            set
            {
                _btn_Inventory = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.character.info.title")]
        public string BtnCharacter
        {
            get { return _btn_Character; }
            set
            {
                _btn_Character = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.skills.title")]
        public string BtnSkills
        {
            get { return _btn_Skills; }
            set
            {
                _btn_Skills = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("pg.quests.title")]
        public string BtnQuests
        {
            get { return _btn_Quests; }
            set
            {
                _btn_Quests = value;
                OnPropertyChanged();
            }

        }
        [LocalizedKey("game.map.title")]
        public string BtnMap
        {
            get { return _btn_Map; }
            set
            {
                _btn_Map = value;
                OnPropertyChanged();
            }

        }

        [LocalizedKey("pg.gamewindow.nav.friends")]
        public string BtnFriends
        {
            get => _btnFriends;
            set { _btnFriends = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.party.kick")]
        public string TblPartyKick
        {
            get => _tblPartyKick;
            set { _tblPartyKick = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.party.promote")]
        public string TblPartyPromote
        {
            get => _tblPartyPromote;
            set {  _tblPartyPromote = value;  OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.party.kick")]
        public string TblPartyKickLabel
        {
            get => _tblPartyKickLabel;
            set { _tblPartyKickLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.friends.whisper")]
        public string TblWhisperLabel
        {
            get => _tblWhisperLabel;
            set { _tblWhisperLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.party.leave")]
        public string TblLeaveParty
        {
            get => _tblLeaveParty;
            set { _tblLeaveParty = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.party.group_fight")]
        public string TblGroupFight
        {
            get => _tblGroupFight;
            set { _tblGroupFight = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.party.accept")]
        public string TblAcceptInvite
        {
            get => _tblAcceptInvite;
            set { _tblAcceptInvite = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.party.decline")]
        public string TblDeclineInvite
        {
            get => _tblDeclineInvite;
            set { _tblDeclineInvite = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.room.also_here")]
        public string TblAlsoHere
        {
            get => _tblAlsoHere;
            set { _tblAlsoHere = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.room.shops_here")]
        public string TblShopsHere
        {
            get => _tblShopsHere;
            set { _tblShopsHere = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.room.open_shop_here")]
        public string TblOpenShopHere
        {
            get => _tblOpenShopHere;
            set { _tblOpenShopHere = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.chat.send")]
        public string TblChatSend
        {
            get => _tblChatSend;
            set { _tblChatSend = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.character.propose_trade")]
        public string TblProposeTradeLabel
        {
            get => _tblProposeTradeLabel;
            set { _tblProposeTradeLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.character.visit_shop")]
        public string TblVisitShopLabel
        {
            get => _tblVisitShopLabel;
            set { _tblVisitShopLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.character.invite_party")]
        public string TblInvitePartyLabel
        {
            get => _tblInvitePartyLabel;
            set { _tblInvitePartyLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.character.friend_request")]
        public string TblFriendRequestLabel
        {
            get => _tblFriendRequestLabel;
            set { _tblFriendRequestLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.character.block")]
        public string TblBlockCharacterLabel
        {
            get => _tblBlockCharacterLabel;
            set {  _tblBlockCharacterLabel = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.chat.all")]
        public string TblChatAll
        {
            get => _tblChatAll;
            set { _tblChatAll = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.chat.room")]
        public string TblChatRoom
        {
            get => _tblChatRoom;
            set { _tblChatRoom = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.chat.global")]
        public string TblChatGlobal
        {
            get => _tblChatGlobal;
            set { _tblChatGlobal = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.chat.party")]
        public string TblChatParty
        {
            get => _tblChatParty;
            set { _tblChatParty = value; OnPropertyChanged(); }
        }

        [LocalizedKey("pg.game.chat.whisper")]
        public string TblChatWhisper
        {
            get => _tblChatWhisper;
            set { _tblChatWhisper = value; OnPropertyChanged(); }
        }

        // Character header
        public CharacterHeaderVm Char { get; } = new();

        private bool _hasReturnableQuest;
        public bool HasReturnableQuest
        {
            get => _hasReturnableQuest;
            set { _hasReturnableQuest = value; OnPropertyChanged(); }
        }

        // ── Chat ────────────────────────────────────────────────────────────
        private static readonly Brush _activeChannelBg   = new SolidColorBrush(Color.FromRgb(0x0F, 0x31, 0x4D));
        private static readonly Brush _inactiveChannelBg = new SolidColorBrush(Color.FromRgb(0x08, 0x0D, 0x10));

        private readonly ObservableCollection<ChatMessageVm> _allMessages = new();
        public ICollectionView ChatMessages { get; }

        private string _chatInput = string.Empty;
        public string ChatInput
        {
            get => _chatInput;
            set { _chatInput = value; OnPropertyChanged(); }
        }

        private string _whisperTarget = string.Empty;
        public string WhisperTarget
        {
            get => _whisperTarget;
            set { _whisperTarget = value; OnPropertyChanged(); }
        }

        private bool _isChatOpen;
        public bool IsChatOpen
        {
            get => _isChatOpen;
            set { _isChatOpen = value; OnPropertyChanged(); }
        }

        private ChatChannel _selectedChannel = ChatChannel.General;
        public ChatChannel SelectedChannel
        {
            get => _selectedChannel;
            set
            {
                _selectedChannel = value;
                OnPropertyChanged();
                ChatMessages.Filter = value == ChatChannel.General
                    ? null
                    : o => ((ChatMessageVm)o).Channel == value;
                OnPropertyChanged(nameof(BgGeneral));
                OnPropertyChanged(nameof(BgRoom));
                OnPropertyChanged(nameof(BgGlobal));
                OnPropertyChanged(nameof(BgParty));
                OnPropertyChanged(nameof(BgWhisper));
                OnPropertyChanged(nameof(IsWhisperMode));
            }
        }

        public bool IsWhisperMode => _selectedChannel == ChatChannel.Whisper;

        public Brush BgGeneral => _selectedChannel == ChatChannel.General ? _activeChannelBg : _inactiveChannelBg;
        public Brush BgRoom    => _selectedChannel == ChatChannel.Room    ? _activeChannelBg : _inactiveChannelBg;
        public Brush BgGlobal  => _selectedChannel == ChatChannel.Global  ? _activeChannelBg : _inactiveChannelBg;
        public Brush BgParty   => _selectedChannel == ChatChannel.Party   ? _activeChannelBg : _inactiveChannelBg;
        public Brush BgWhisper => _selectedChannel == ChatChannel.Whisper ? _activeChannelBg : _inactiveChannelBg;

        public bool IsChatAvailable => ServerApiService.Token is not null;

        // ── Room presence ────────────────────────────────────────────────────
        public ObservableCollection<RoomCharacterVm> RoomCharacters { get; } = new();
        public bool HasRoomCharacters => RoomCharacters.Count > 0;

        // Shops open in the current room - kept separate from RoomCharacters because a shop kept
        // open by a Merchant's Seal is visible here even when its owner isn't physically present
        // (isn't in RoomCharacters at all). Populated fresh on every room join (RoomShopsReceived)
        // and kept in sync afterwards via ShopOpened/ShopClosed.
        public ObservableCollection<string> RoomShopOwners { get; } = new();
        public bool HasRoomShops => RoomShopOwners.Count > 0;

        // Player shops can only be opened in a city room - gates the "Open Shop" button in the
        // shops panel. Purely a client-side UX guard; GameHub.OpenShop enforces the same rule
        // server-side regardless.
        private bool _isCurrentRoomCity;
        public bool IsCurrentRoomCity
        {
            get => _isCurrentRoomCity;
            set { _isCurrentRoomCity = value; OnPropertyChanged(); }
        }

        // ── Friends (cached for the player-interaction context menus) ───────
        private readonly HashSet<string> _friendNames = new(StringComparer.OrdinalIgnoreCase);

        public bool IsFriend(string? name) => name != null && _friendNames.Contains(name);

        private async void RefreshFriendNames()
        {
            var friends = await ServerApiService.GetFriendsAsync();
            _friendNames.Clear();
            foreach (var f in friends)
                _friendNames.Add(f.CharacterName);
        }

        // ── Party ────────────────────────────────────────────────────────────
        public ObservableCollection<PartyMemberVm> PartyMembers { get; } = new();

        private bool _isInParty;
        public bool IsInParty
        {
            get => _isInParty;
            set { _isInParty = value; OnPropertyChanged(); }
        }

        private bool _isPartyLeader;
        public bool IsPartyLeader
        {
            get => _isPartyLeader;
            set { _isPartyLeader = value; OnPropertyChanged(); }
        }

        private bool _hasPendingInvite;
        public bool HasPendingInvite
        {
            get => _hasPendingInvite;
            set { _hasPendingInvite = value; OnPropertyChanged(); }
        }

        private string _inviteFrom = string.Empty;
        public string InviteFrom
        {
            get => _inviteFrom;
            set { _inviteFrom = value; OnPropertyChanged(); OnPropertyChanged(nameof(PartyInviteText)); }
        }

        public string PartyInviteText => string.Format(Myria.Lib.Core.Systems.Localization.T("pg.game.party.invite_from"), InviteFrom);

        private string _pendingPartyId = string.Empty;

        // Static hook used by the Friends page to open a whisper conversation
        private static ViewModel_PageGame? _current;
        public static void StartWhisper(string username)
        {
            if (_current is null) return;
            _current.IsChatOpen      = true;
            _current.SelectedChannel = ChatChannel.Whisper;
            _current.WhisperTarget   = username;
        }

        // Commands
        public ICommand MapCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand OpenInventoryCommand { get; }
        public ICommand OpenCharacterCommand { get; }
        public ICommand OpenSkillsCommand { get; }
        public ICommand OpenQuestsCommand { get; }
        public ICommand ToggleChatCommand { get; }
        public ICommand SendChatCommand { get; }
        public ICommand OpenFriendsCommand { get; }
        public ICommand SetChannelGeneralCommand { get; }
        public ICommand SetChannelRoomCommand    { get; }
        public ICommand SetChannelGlobalCommand  { get; }
        public ICommand SetChannelPartyCommand   { get; }
        public ICommand SetChannelWhisperCommand { get; }
        public ICommand AcceptPartyInviteCommand    { get; }
        public ICommand DeclinePartyInviteCommand  { get; }
        public ICommand LeavePartyCommand          { get; }
        public ICommand KickFromPartyCommand       { get; }
        public ICommand TransferPartyLeaderCommand { get; }
        public ICommand WhisperMemberCommand       { get; }
        public ICommand SendFriendRequestCommand    { get; }
        public ICommand InviteToPartyFromRoomCommand { get; }
        public ICommand BlockCharacterCommand          { get; }
        public ICommand ProposeTradeCommand         { get; }
        public ICommand VisitCharacterShopCommand      { get; }
        public ICommand OpenMyShopCommand           { get; }
        public ICommand StartGroupFightCommand      { get; }


        public bool IsInFight { get; private set; }
        public bool IsNotInFight => !IsInFight;

        private ViewModel_PageFight? _currentFight;
        public ViewModel_PageFight? CurrentFight
        {
            get => _currentFight;
            private set
            {
                if (_currentFight != null)
                    _currentFight.PropertyChanged -= OnFightPropertyChanged;
                _currentFight = value;
                if (_currentFight != null)
                    _currentFight.PropertyChanged += OnFightPropertyChanged;
                OnPropertyChanged();
            }
        }

        private void OnFightPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel_PageFight.CurrentTurnCharacterName))
                SyncPartyTurnIndicators();
        }

        private void SyncPartyTurnIndicators()
        {
            var turn = CurrentFight?.CurrentTurnCharacterName ?? string.Empty;
            foreach (var m in PartyMembers)
                m.IsCurrentTurn = string.Equals(m.Username, turn, StringComparison.OrdinalIgnoreCase);
        }

        private void OnFightStateChanged(bool isInFight)
        {
            IsInFight = isInFight;
            OnPropertyChanged(nameof(IsInFight));
            OnPropertyChanged(nameof(IsNotInFight));
            CurrentFight = isInFight ? ViewModel_PageFight.ActiveFight : null;
            if (!isInFight)
                foreach (var m in PartyMembers) m.IsCurrentTurn = false;
        }

        public ViewModel_PageGame()
        {
            Navigation.Current.FightStateChanged += OnFightStateChanged;
            ChatMessages = CollectionViewSource.GetDefaultView(_allMessages);

            MapCommand = new RelayCommand(OpenMap);
            SettingsCommand = new RelayCommand(OpenSettings);
            OpenInventoryCommand = new RelayCommand(OpenInventory);
            OpenCharacterCommand = new RelayCommand(OpenCharacter);
            OpenSkillsCommand = new RelayCommand(OpenSkills);
            OpenQuestsCommand = new RelayCommand(OpenQuests);
            ToggleChatCommand = new RelayCommand(() => IsChatOpen = !IsChatOpen);
            SendChatCommand = new RelayCommand(SendChat);
            OpenFriendsCommand = new RelayCommand(OpenFriends);
            SetChannelGeneralCommand = new RelayCommand(() => SelectedChannel = ChatChannel.General);
            SetChannelRoomCommand    = new RelayCommand(() => SelectedChannel = ChatChannel.Room);
            SetChannelGlobalCommand  = new RelayCommand(() => SelectedChannel = ChatChannel.Global);
            SetChannelPartyCommand   = new RelayCommand(() => SelectedChannel = ChatChannel.Party);
            SetChannelWhisperCommand = new RelayCommand(() => SelectedChannel = ChatChannel.Whisper);
            AcceptPartyInviteCommand    = new RelayCommand(AcceptPartyInvite);
            DeclinePartyInviteCommand  = new RelayCommand(DeclinePartyInvite);
            LeavePartyCommand          = new RelayCommand(() => _ = GameHubService.LeavePartyAsync());
            KickFromPartyCommand         = new RelayCommand<string>(name => _ = GameHubService.KickFromPartyAsync(name));
            TransferPartyLeaderCommand   = new RelayCommand<string>(name => _ = GameHubService.TransferPartyLeaderAsync(name));
            WhisperMemberCommand         = new RelayCommand<string>(name => StartWhisper(name));
            SendFriendRequestCommand     = new RelayCommand<string>(name => _ = ServerApiService.SendFriendRequestAsync(name));
            InviteToPartyFromRoomCommand = new RelayCommand<string>(name => _ = GameHubService.InviteToPartyAsync(name));
            BlockCharacterCommand           = new RelayCommand<string>(name => _ = ServerApiService.BlockCharacterAsync(name));
            ProposeTradeCommand          = new RelayCommand<string>(name => _ = GameHubService.ProposeTradeAsync(name));
            VisitCharacterShopCommand       = new RelayCommand<string>(name => _ = OpenCharacterShopAsync(name));
            OpenMyShopCommand            = new RelayCommand(OpenMyShop);
            StartGroupFightCommand       = new RelayCommand(() => _ = StartGroupFightAsync());

            _current = this;

            if (UserAccountService.CurrentCharacter.CurrentRoom == null)
                UserAccountService.CurrentCharacter.CurrentRoom = RoomService.GetRoomById(UserAccountService.CurrentCharacter.CurrentRoomId);
            Navigation.Current.InvalidateCache(Nav.Room);

            var character = UserAccountService.CurrentCharacter;
            character.XpGained       += (s, e) => RefreshQuestBadge();
            character.Inventory.ItemReceived += (s, e) => RefreshQuestBadge();
            RefreshQuestBadge();

            RoomCharacters.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRoomCharacters));
            RoomShopOwners.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasRoomShops));

            if (IsChatAvailable)
            {
                IsCurrentRoomCity = character.CurrentRoom is not null && CityRegistry.GetCityByRoom(character.CurrentRoom) is not null;
                GameEvents.RoomEntered += OnRoomEnteredForShopGate;

                GameHubService.ForceLoggedOut          += OnForceLoggedOut;
                GameHubService.ChatMessageReceived     += OnChatMessage;
                GameHubService.PartyInviteReceived     += OnPartyInvite;
                GameHubService.PartyUpdated            += OnPartyUpdated;
                GameHubService.PartyDisbanded          += OnPartyDisbanded;
                GameHubService.KickedFromParty         += OnKickedFromParty;
                GameHubService.PartyMemberStatsUpdated += OnPartyMemberStats;
                GameHubService.RoomCharactersReceived     += OnRoomCharacters;
                GameHubService.CharacterEntered           += OnCharacterEntered;
                GameHubService.CharacterLeft              += OnCharacterLeft;
                GameHubService.TradeProposed           += OnTradeProposed;
                GameHubService.TradeStarted            += OnTradeStarted;
                GameHubService.TradeCancelled          += OnTradeCancelledWhilePending;
                GameHubService.RoomShopsReceived       += OnRoomShops;
                GameHubService.ShopOpened              += OnShopOpened;
                GameHubService.ShopClosed              += OnShopClosed;
                GameHubService.GroupCombatStarted      += OnGroupCombatStarted;

                RefreshFriendNames();

                character.HealthChanged += (_, _) => PushPartyStats();
                character.ManaChanged   += (_, _) => PushPartyStats();

                // Room navigation must wait for the hub connection attempt to finish (success or
                // failure) before happening. Page_Room picks its online/offline ViewModel once,
                // at construction, from GameHubService.IsConnected - navigating immediately (the
                // old behavior) meant the connection hadn't even started yet, so Page_Room always
                // locked in the offline ViewModel and every fight silently ran client-side only,
                // never reaching the server, regardless of the connection succeeding moments later.
                _ = ConnectThenNavigateToRoomAsync();
            }
            else
            {
                Navigation.Current.Navigate(Nav.Room);
            }
        }

        private async Task ConnectThenNavigateToRoomAsync()
        {
            try { await GameHubService.ConnectAsync(); }
            catch { /* fall back to the offline room ViewModel if the connection attempt fails */ }
            Navigation.Current.Navigate(Nav.Room);
        }

        private async void OnForceLoggedOut()
        {
            await GameHubService.DisconnectAsync();
            ServerApiService.ClearToken();
            Navigation.Current.SetGameState(false);
            Navigation.Current.Navigate(Nav.Startup);
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Hidden;
            MainWindow.Instance.npcWindow.Visibility = Visibility.Hidden;
            MessageBox.Show(
                Myria.Lib.Core.Systems.Localization.T("pg.game.force_logout_message"),
                Myria.Lib.Core.Systems.Localization.T("pg.game.force_logout_title"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnChatMessage(string sender, string message, string channelStr)
        {
            var ch = channelStr.ToLowerInvariant() switch
            {
                "global"  => ChatChannel.Global,
                "party"   => ChatChannel.Party,
                "whisper" => ChatChannel.Whisper,
                _         => ChatChannel.Room,
            };
            _allMessages.Add(new ChatMessageVm
            {
                Channel    = ch,
                Sender     = sender,
                Text       = $"[{sender}] {message}",
                Foreground = ChatMessageVm.BrushFor(ch),
            });
            if (_allMessages.Count > 200)
                _allMessages.RemoveAt(0);
        }

        private void SendChat()
        {
            var text = ChatInput?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            ChatInput = string.Empty;

            var channel = _selectedChannel switch
            {
                ChatChannel.Global  => "global",
                ChatChannel.Party   => "party",
                ChatChannel.Whisper => "whisper",
                _                   => "room",
            };
            var target = _selectedChannel == ChatChannel.Whisper ? WhisperTarget?.Trim() : null;
            _ = GameHubService.SendMessageAsync(text, channel, target);
        }

        private void RefreshQuestBadge()
        {
            HasReturnableQuest = UserAccountService.CurrentCharacter.ActiveQuests
                .Any(q => q.Status == QuestStatus.Completed);
        }

        // ── Party handlers ──────────────────────────────────────────────────

        private void OnPartyInvite(string from, string partyId)
        {
            _pendingPartyId  = partyId;
            InviteFrom       = from;
            HasPendingInvite = true;
        }

        private void OnPartyUpdated(List<string> members, string? leader)
        {
            var self = UserAccountService.CurrentCharacter.Name;

            // Remove members no longer in the party.
            var toRemove = PartyMembers.Where(vm => !members.Contains(vm.Username)).ToList();
            foreach (var r in toRemove) PartyMembers.Remove(r);

            // Add new members; update leader flag on existing.
            foreach (var m in members)
            {
                if (m == self) continue;
                var existing = PartyMembers.FirstOrDefault(vm => vm.Username == m);
                if (existing is null)
                    PartyMembers.Add(new PartyMemberVm { Username = m, IsLeader = m == leader });
                else
                    existing.IsLeader = m == leader;
            }

            // Sync leader flag and leader-action visibility for all cards.
            IsPartyLeader = self == leader;
            foreach (var vm in PartyMembers)
            {
                vm.IsLeader         = vm.Username == leader;
                vm.ShowLeaderActions = IsPartyLeader && !vm.IsLeader;
            }

            IsInParty        = PartyMembers.Count > 0;
            HasPendingInvite = false;

            // Push our own stats so newly joined members can see them.
            PushPartyStats();
        }

        private void OnPartyDisbanded()
        {
            PartyMembers.Clear();
            IsInParty     = false;
            IsPartyLeader = false;
        }

        private void OnKickedFromParty()
        {
            PartyMembers.Clear();
            IsInParty     = false;
            IsPartyLeader = false;
        }

        private void OnPartyMemberStats(string username, int hp, int maxHp, int mana, int maxMana, int level)
        {
            var vm = PartyMembers.FirstOrDefault(m => m.Username == username);
            if (vm is null) return;
            vm.Hp     = hp;
            vm.MaxHp  = maxHp;
            vm.Mana   = mana;
            vm.MaxMana = maxMana;
            vm.Level  = level;
        }

        private void AcceptPartyInvite()
        {
            _ = GameHubService.AcceptPartyInviteAsync(_pendingPartyId);
            HasPendingInvite = false;
        }

        private void DeclinePartyInvite()
        {
            _ = GameHubService.DeclinePartyInviteAsync(_pendingPartyId, InviteFrom);
            HasPendingInvite = false;
        }

        // ── Trade handlers ───────────────────────────────────────────────────

        private void OnTradeProposed(string fromName, string tradeId)
        {
            MainWindow.Instance.tradeProposalWindow.Show(fromName, tradeId);
        }

        private void OnTradeCancelledWhilePending(string _)
        {
            // Close the proposal window if the proposer withdrew before we responded
            if (MainWindow.Instance.tradeProposalWindow.Visibility == Visibility.Visible)
                MainWindow.Instance.tradeProposalWindow.ForceClose();
        }

        private void OnTradeStarted(string partnerName)
        {
            var vm   = new ViewModel.Pages.Game.IngameWindow.TradeViewModel(partnerName);
            var page = new View.Pages.Game.IngameWindow.Page_Trade { DataContext = vm };
            MainWindow.Instance.tradeWindow.NavigateTo(page, $"Trade — {partnerName}");
            MainWindow.Instance.tradeWindow.Visibility = Visibility.Visible;
        }

        // ── Group combat ─────────────────────────────────────────────────────

        private async Task StartGroupFightAsync()
        {
            if (!GameHubService.IsConnected) return;
            var character = UserAccountService.CurrentCharacter;
            var result = await GameHubService.StartGroupCombatAsync(character.CurrentRoom.Id);
            if (result == null || !result.Success) return;
            // GroupCombatStarted event fires on all party members (including caller); nav happens there.
        }

        private void OnGroupCombatStarted(Myria.Lib.Core.Models.Dto.StartGroupCombatResult result)
        {
            ViewModel_PageFight.PendingGroupCombat = result;
            Navigation.Current.Navigate(NavigationFrameType.Game, new Page_Fight());
            Navigation.Current.SetFightState(true);
        }

        // ── Shop handlers ────────────────────────────────────────────────────

        // Room-scoped: RoomShopsReceived/ShopOpened/ShopClosed all describe "shops visible in
        // whatever room I'm currently in" (the server only ever broadcasts to that room's
        // group), so these don't need to filter by current room on this end.
        private void OnRoomShops(List<string> ownerNames)
        {
            RoomShopOwners.Clear();
            foreach (var name in ownerNames) RoomShopOwners.Add(name);
        }

        private void OnShopOpened(string ownerName)
        {
            if (!RoomShopOwners.Contains(ownerName))
                RoomShopOwners.Add(ownerName);
        }

        private void OnShopClosed(string ownerName) => RoomShopOwners.Remove(ownerName);

        private void OnRoomEnteredForShopGate(Myria.Lib.Core.Entities.Characters.Character c, Myria.Lib.Core.Entities.Maps.Room r) =>
            IsCurrentRoomCity = CityRegistry.GetCityByRoom(r) is not null;

        private async Task OpenCharacterShopAsync(string ownerName)
        {
            var vm   = await CharacterShopViewModel.OpenBuyerView(ownerName);
            var page = new Page_CharacterShop { DataContext = vm };
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection($"Shop — {ownerName}", string.Empty);
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            Navigation.Current.Navigate(page);
        }

        private async void OpenMyShop()
        {
            // "Open Shop Here" is the only entry point into the owner's shop page - actually
            // open the shop on the server here so the page never shows a not-yet-created shop
            // (deposits would otherwise fail with "no_shop" until the page's own toggle was
            // clicked separately).
            await GameHubService.OpenShopAsync();
            var vm   = await CharacterShopViewModel.OpenOwnerView();
            var page = new Page_CharacterShop { DataContext = vm };
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection("My Shop", string.Empty);
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            Navigation.Current.Navigate(page);
        }

        // ── Room presence handlers ───────────────────────────────────────────

        private void OnRoomCharacters(List<string> players)
        {
            RoomCharacters.Clear();
            var self = UserAccountService.CurrentCharacter.Name;
            foreach (var name in players.Where(n => n != self))
                RoomCharacters.Add(new RoomCharacterVm { Name = name });
            RefreshFriendNames();
        }

        private void OnCharacterEntered(string name)
        {
            if (name == UserAccountService.CurrentCharacter.Name) return;
            if (!RoomCharacters.Any(p => p.Name == name))
                RoomCharacters.Add(new RoomCharacterVm { Name = name });
        }

        private void OnCharacterLeft(string name)
        {
            var vm = RoomCharacters.FirstOrDefault(p => p.Name == name);
            if (vm is not null) RoomCharacters.Remove(vm);
        }

        private void PushPartyStats()
        {
            if (!IsInParty) return;
            _ = GameHubService.UpdatePartyStatsAsync();
        }

        // ── Page navigation ─────────────────────────────────────────────────

        private void OpenMap()
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            var room = RoomService.GetRoomById(UserAccountService.CurrentCharacter.CurrentRoom.Id);
            var vm   = new ViewModel_PageLocalMap(room);
            var page = new Page_LocalMap { DataContext = vm };
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection(vm.MapTitle, "Map", "Map");
            Navigation.Current.Navigate(page);
        }
        private void OpenInventory()
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            InventoryPage inv = new InventoryPage(UserAccountService.CurrentCharacter);
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection("Inventory", "Inventory", "Inventory");
            Navigation.Current.Navigate(inv);
        }
        private void OpenCharacter()
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            Page_Character character = new Page_Character();
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection(
                ((character.DataContext) as CharacterPageViewModel)?.WindowTitle ?? "Character",
                "Character",
                "Overview");
            Navigation.Current.Navigate(character);
        }
        private void OpenSkills()
        {
            var page = new Page_Skills()
            {
                DataContext = new SkillPageViewModel()
            };
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection(
                (page.DataContext as SkillPageViewModel)?.WindowTitle ?? "Skills",
                "Character",
                "Skills");
            Navigation.Current.Navigate(page);
        }
        private void OpenQuests()
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            var page = new Page_QuestList();
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection("Quests", "Quests", "Quests");
            Navigation.Current.Navigate(page);
        }
        private void OpenSettings()
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            var page = new Page_SettingsVisuals();
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection("Settings", "Settings", "Visuals");
            Navigation.Current.Navigate(page);
        }
        private void OpenFriends()
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            var page = new View.Pages.Game.IngameWindow.Page_Friends();
            (MainWindow.Instance.playerMenuWindow.DataContext as ViewModel_CharacterMenuWindow)?.SetTitleAndSection("Friends", "Social", "Social");
            Navigation.Current.Navigate(page);
        }
    }


    public class CharacterHeaderVm : BaseViewModel
    {
        private static CharacterHeaderVm instance;
        private string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = UserAccountService.CurrentCharacter.Name;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NameAndLevel));
            }

        }

        private int _level;
        public int Level
        {
            get { return _level; }
            set
            {
                _level = UserAccountService.CurrentCharacter.Level;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NameAndLevel));
            }

        }

        private long _xp;
        public long CurrentXp
        {
            get { return _xp; }
            set
            {
                _xp = UserAccountService.CurrentCharacter.Experience;
                OnPropertyChanged();
                XpPercent++;
            }

        }

        private long _xpToNext = 1;
        public long XpToNext
        {
            get { return _xpToNext; }
            set
            {
                _xpToNext = UserAccountService.CurrentCharacter.ExpForNextLvl;
                OnPropertyChanged();
                XpPercent++;
            }

        }

        private int _hp;
        public int Hp
        {
            get { return _hp; }
            set
            {
                _hp = UserAccountService.CurrentCharacter.CurrentHealth; OnPropertyChanged();
                HpDisplay = "";
            }

        }

        private int _hpMax = 1;
        public int MaxHp
        {
            get { return _hpMax; }
            set
            {
                _hpMax = UserAccountService.CurrentCharacter.MaxHealth;
                OnPropertyChanged();
                HpDisplay = "";
            }

        }

        private int _mp;
        public int Mana
        {
            get { return _mp; }
            set
            {
                _mp = UserAccountService.CurrentCharacter.CurrentMana;
                OnPropertyChanged();
                ManaDisplay = "";
            }

        }

        private int _mpMax = 1;
        public int MaxMana
        {
            get { return _mpMax; }
            set
            {
                _mpMax = UserAccountService.CurrentCharacter.MaxMana;
                OnPropertyChanged();
                ManaDisplay = "";
            }

        }
        public string NameAndLevel => string.IsNullOrWhiteSpace(Name) ? string.Empty : $"{Name} • Lv {Level}";
        private int xpPercent;
        public int XpPercent
        {
            get => xpPercent;
            set
            {
                xpPercent = (int)Math.Round(100.0 * CurrentXp / Math.Max(1, XpToNext));
                OnPropertyChanged();
            }

        }
        private string hpDisplay;
        public string HpDisplay
        {
            get => hpDisplay;
            set
            {
                hpDisplay = $"{Hp}/{MaxHp}";
                OnPropertyChanged();
            }

        }
        private string manaDisplay;
        public string ManaDisplay
        {
            get => manaDisplay;
            set
            {
                manaDisplay = $"{Mana}/{MaxMana}";
                OnPropertyChanged();
            }

        }

        public CharacterHeaderVm()
        {
            Character character = UserAccountService.CurrentCharacter;
            Set(character.Name, character.Level, character.Experience, character.ExpForNextLvl, character.CurrentHealth, character.MaxHealth, character.CurrentMana, character.MaxMana);

            XpPercent = (int)Math.Round(100.0 * CurrentXp / Math.Max(1, XpToNext));
            HpDisplay = $"{Hp}/{MaxHp}";
            ManaDisplay = $"{Mana}/{MaxMana}";
            instance = this;
            character.XpGained += OnXpUpdateEvent;
            character.LeveledUp += OnXpUpdateEvent;
            character.HealthChanged += (s, e) => { Refresh(); };
            character.ManaChanged += (s, e) => { Refresh(); };
        }
        private void OnXpUpdateEvent(object? sender, EventArgs e)
        {
            Refresh();
        }
        private void Refresh()
        {
            instance.Hp++;
            instance.MaxHp++;
            instance.Mana++;
            instance.MaxMana++;
            instance.CurrentXp++;
            instance.XpToNext++;
            instance.XpPercent++;
            instance.Level++;
        }
        public void Set(string name, int level, long currentXp, long xpToNext, int hp, int maxHp, int mana, int maxMana)
        {
            Name = name; Level = level; CurrentXp = currentXp; XpToNext = xpToNext;
            Hp = hp; MaxHp = maxHp; Mana = mana; MaxMana = maxMana;
        }

    }

}
