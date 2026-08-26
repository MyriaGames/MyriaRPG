using Myria.Wpf.Model;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.ViewModel.Pages.Game;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public enum GuildTab { Roster, Rookies, Property }

    public class GuildPageViewModel : BaseViewModel
    {
        // ── State ─────────────────────────────────────────────────────────────

        private bool _isInGuild;
        public bool IsInGuild
        {
            get => _isInGuild;
            private set { _isInGuild = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoGuild)); }
        }
        public bool HasNoGuild => !IsInGuild;

        private int    _guildId;
        private string _guildName  = string.Empty;
        private string _guildTag   = string.Empty;
        private string _guildDesc  = string.Empty;
        private string _myRank     = string.Empty;
        private int    _memberCount;
        private int    _rookieCount;
        private long   _treasury;

        public string GuildName    { get => _guildName;   set { _guildName   = value; OnPropertyChanged(); } }
        public string GuildTag     { get => _guildTag;    set { _guildTag    = value; OnPropertyChanged(); } }
        public string GuildDesc    { get => _guildDesc;   set { _guildDesc   = value; OnPropertyChanged(); } }
        public string MyRank       { get => _myRank;      set { _myRank      = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanManage)); OnPropertyChanged(nameof(IsLeader)); } }
        public int    MemberCount  { get => _memberCount; set { _memberCount = value; OnPropertyChanged(); } }
        public int    RookieCount  { get => _rookieCount; set { _rookieCount = value; OnPropertyChanged(); } }
        public long   Treasury     { get => _treasury;    set { _treasury    = value; OnPropertyChanged(); } }

        // ── Permissions ───────────────────────────────────────────────────────

        public bool CanManage => MyRank is "Officer" or "Leader";
        public bool IsLeader  => MyRank == "Leader";

        // ── Tab ───────────────────────────────────────────────────────────────

        private GuildTab _tab = GuildTab.Roster;
        public GuildTab SelectedTab
        {
            get => _tab;
            set
            {
                _tab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRosterTab));
                OnPropertyChanged(nameof(IsRookiesTab));
                OnPropertyChanged(nameof(IsPropertyTab));
                OnPropertyChanged(nameof(ShowPropertyAcquire));
                OnPropertyChanged(nameof(ShowPropertyDetail));
                if (_tab == GuildTab.Property) ReloadProperty();
            }
        }
        public bool IsRosterTab   => SelectedTab == GuildTab.Roster;
        public bool IsRookiesTab  => SelectedTab == GuildTab.Rookies;
        public bool IsPropertyTab => SelectedTab == GuildTab.Property;

        // ── Collections ───────────────────────────────────────────────────────

        public ObservableCollection<GuildMemberVm>  Members        { get; } = new();
        public ObservableCollection<GuildMemberVm>  Rookies        { get; } = new();
        public ObservableCollection<GuildInviteVm>  PendingInvites { get; } = new();
        public ObservableCollection<GuildRoomRowVm> PropertyRooms  { get; } = new();

        // ── Property state ────────────────────────────────────────────────────

        private bool   _hasProperty;
        private string _propertyType       = string.Empty;
        private string _propertyAcquiredAt = string.Empty;
        private string _buyHouseIdInput    = string.Empty;
        private string _buyHouseError      = string.Empty;
        private string _establishBaseError = string.Empty;

        public bool HasProperty
        {
            get => _hasProperty;
            private set
            {
                _hasProperty = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowPropertyAcquire));
                OnPropertyChanged(nameof(ShowPropertyDetail));
            }
        }

        public string PropertyType       { get => _propertyType;       private set { _propertyType       = value; OnPropertyChanged(); } }
        public string PropertyAcquiredAt { get => _propertyAcquiredAt; private set { _propertyAcquiredAt = value; OnPropertyChanged(); } }
        public string BuyHouseIdInput    { get => _buyHouseIdInput;    set { _buyHouseIdInput    = value; OnPropertyChanged(); } }
        public string BuyHouseError      { get => _buyHouseError;      set { _buyHouseError      = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBuyHouseError)); } }
        public bool   HasBuyHouseError   => !string.IsNullOrEmpty(_buyHouseError);
        public string EstablishBaseError { get => _establishBaseError; set { _establishBaseError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEstablishBaseError)); } }
        public bool   HasEstablishBaseError => !string.IsNullOrEmpty(_establishBaseError);

        public bool ShowPropertyAcquire => IsPropertyTab && !_hasProperty;
        public bool ShowPropertyDetail  => IsPropertyTab && _hasProperty;

        public string CurrentRoomLabel =>
            $"Room #{Myria.Lib.Core.Services.UserAccountService.CurrentCharacter?.CurrentRoomId ?? 0}";

        // ── Invite panel ──────────────────────────────────────────────────────

        private bool   _showInvite;
        private string _inviteName    = string.Empty;
        private bool   _inviteAsRookie;
        private string _inviteError   = string.Empty;

        public bool ShowInvitePanel
        {
            get => _showInvite;
            set { _showInvite = value; OnPropertyChanged(); }
        }
        public string InviteName
        {
            get => _inviteName;
            set { _inviteName = value; OnPropertyChanged(); }
        }
        public bool InviteAsRookie
        {
            get => _inviteAsRookie;
            set { _inviteAsRookie = value; OnPropertyChanged(); OnPropertyChanged(nameof(InviteAsMember)); OnPropertyChanged(nameof(ShowRookieLevelNote)); }
        }
        public bool InviteAsMember
        {
            get => !_inviteAsRookie;
            set { InviteAsRookie = !value; }
        }
        public bool ShowRookieLevelNote => _inviteAsRookie;

        public string InviteError
        {
            get => _inviteError;
            set { _inviteError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasInviteError)); }
        }
        public bool HasInviteError => !string.IsNullOrEmpty(_inviteError);

        // ── Create panel ──────────────────────────────────────────────────────

        private bool _showCreate;
        public bool ShowCreatePanel
        {
            get => _showCreate;
            set { _showCreate = value; OnPropertyChanged(); }
        }

        private string _createName = string.Empty;
        private string _createTag  = string.Empty;
        private string _createDesc = string.Empty;
        private string _createError = string.Empty;

        public string CreateName  { get => _createName;  set { _createName  = value; OnPropertyChanged(); } }
        public string CreateTag   { get => _createTag;   set { _createTag   = value; OnPropertyChanged(); } }
        public string CreateDesc  { get => _createDesc;  set { _createDesc  = value; OnPropertyChanged(); } }
        public string CreateError { get => _createError; set { _createError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasCreateError)); } }
        public bool HasCreateError => !string.IsNullOrEmpty(_createError);

        // ── Status ────────────────────────────────────────────────────────────

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatus)); }
        }
        public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

        // ── Commands ──────────────────────────────────────────────────────────

        public ICommand RefreshCommand           { get; }
        public ICommand SelectRosterTabCommand   { get; }
        public ICommand SelectRookiesTabCommand  { get; }
        public ICommand SelectPropertyTabCommand { get; }
        public ICommand LeaveGuildCommand        { get; }
        public ICommand DisbandGuildCommand      { get; }
        public ICommand OpenInviteCommand        { get; }
        public ICommand CancelInviteCommand      { get; }
        public ICommand SendInviteCommand        { get; }
        public ICommand OpenCreateCommand        { get; }
        public ICommand CancelCreateCommand      { get; }
        public ICommand SubmitCreateCommand      { get; }
        public ICommand BuyHouseCommand          { get; }
        public ICommand EstablishBaseCommand     { get; }

        // ── Construction ──────────────────────────────────────────────────────

        public GuildPageViewModel()
        {
            RefreshCommand           = new RelayCommand(Reload);
            SelectRosterTabCommand   = new RelayCommand(() => SelectedTab = GuildTab.Roster);
            SelectRookiesTabCommand  = new RelayCommand(() => SelectedTab = GuildTab.Rookies);
            SelectPropertyTabCommand = new RelayCommand(() => SelectedTab = GuildTab.Property);
            LeaveGuildCommand        = new RelayCommand(LeaveGuild);
            DisbandGuildCommand      = new RelayCommand(DisbandGuild);
            OpenInviteCommand        = new RelayCommand(() => { ShowInvitePanel = true; InviteName = ""; InviteAsRookie = false; InviteError = ""; });
            CancelInviteCommand      = new RelayCommand(() => ShowInvitePanel = false);
            SendInviteCommand        = new RelayCommand(SendInvite);
            OpenCreateCommand        = new RelayCommand(() => { ShowCreatePanel = true; CreateError = ""; });
            CancelCreateCommand      = new RelayCommand(() => ShowCreatePanel = false);
            SubmitCreateCommand      = new RelayCommand(SubmitCreate);
            BuyHouseCommand          = new RelayCommand(BuyHouse);
            EstablishBaseCommand     = new RelayCommand(EstablishBase);

            GameHubService.GuildMemberOnline   += OnMemberOnline;
            GameHubService.GuildMemberOffline  += OnMemberOffline;
            GameHubService.GuildMemberJoined   += OnMemberJoined;
            GameHubService.GuildMemberLeft     += OnMemberLeft;
            GameHubService.GuildRankChanged    += OnRankChanged;
            GameHubService.GuildLeaderChanged  += OnLeaderChanged;
            GameHubService.GuildRookieAdded    += OnRookieAdded;
            GameHubService.GuildDisbanded      += OnGuildDisbanded;
            GameHubService.GuildLeft           += OnGuildLeft;
            GameHubService.GuildKicked         += OnGuildKicked;
            GameHubService.GuildInviteReceived += OnInviteReceived;
            GameHubService.GuildError          += OnGuildError;
            GameHubService.HubConnected        += Reload;

            Reload();
        }

        public void Cleanup()
        {
            GameHubService.GuildMemberOnline   -= OnMemberOnline;
            GameHubService.GuildMemberOffline  -= OnMemberOffline;
            GameHubService.GuildMemberJoined   -= OnMemberJoined;
            GameHubService.GuildMemberLeft     -= OnMemberLeft;
            GameHubService.GuildRankChanged    -= OnRankChanged;
            GameHubService.GuildLeaderChanged  -= OnLeaderChanged;
            GameHubService.GuildRookieAdded    += OnRookieAdded;
            GameHubService.GuildDisbanded      -= OnGuildDisbanded;
            GameHubService.GuildLeft           -= OnGuildLeft;
            GameHubService.GuildKicked         -= OnGuildKicked;
            GameHubService.GuildInviteReceived -= OnInviteReceived;
            GameHubService.GuildError          -= OnGuildError;
            GameHubService.HubConnected        -= Reload;
        }

        // ── Load ──────────────────────────────────────────────────────────────

        private async void Reload()
        {
            StatusMessage = string.Empty;
            var guild = await ServerApiService.GetMyGuildAsync();

            if (guild is not null)
            {
                LoadGuild(guild);
            }
            else
            {
                IsInGuild = false;
                Members.Clear();
                Rookies.Clear();
                var invites = await ServerApiService.GetGuildInvitesAsync();
                LoadInvites(invites);
            }
        }

        private void LoadGuild(ServerApiService.GuildInfo g)
        {
            _guildId    = g.Id;
            GuildName   = g.Name;
            GuildTag    = g.Tag;
            GuildDesc   = g.Description ?? string.Empty;
            Treasury    = g.TreasuryBalance;
            MemberCount = g.Members.Length;
            RookieCount = g.Rookies.Length;

            var me = Myria.Lib.Core.Services.UserAccountService.CurrentCharacter?.Name ?? string.Empty;
            var myEntry = g.Members.FirstOrDefault(m =>
                string.Equals(m.CharacterName, me, StringComparison.OrdinalIgnoreCase));
            MyRank = myEntry?.Rank ?? string.Empty;

            Members.Clear();
            foreach (var m in g.Members.OrderBy(m => m.CharacterName))
            {
                Members.Add(BuildMemberVm(m.CharacterName, m.Rank, isRookie: false));
            }

            Rookies.Clear();
            foreach (var r in g.Rookies.OrderBy(r => r.CharacterName))
            {
                Rookies.Add(BuildMemberVm(r.CharacterName, "Rookie", isRookie: true));
            }

            IsInGuild = true;
        }

        private void LoadInvites(List<ServerApiService.GuildInviteInfo> invites)
        {
            PendingInvites.Clear();
            foreach (var inv in invites)
            {
                var captured = inv;
                PendingInvites.Add(new GuildInviteVm
                {
                    InviteId       = captured.InviteId,
                    GuildId        = captured.GuildId,
                    GuildName      = captured.GuildName,
                    GuildTag       = captured.GuildTag,
                    IsRookieInvite = captured.IsRookieInvite,
                    AcceptCommand  = new RelayCommand(() => AcceptInvite(captured.InviteId)),
                    DeclineCommand = new RelayCommand(() => DeclineInvite(captured.InviteId)),
                });
            }
        }

        private GuildMemberVm BuildMemberVm(string name, string rank, bool isRookie)
        {
            var captured = name;
            return new GuildMemberVm
            {
                Name           = captured,
                Rank           = rank,
                IsRookie       = isRookie,
                KickCommand    = CanManage ? new RelayCommand(() => Kick(captured))                                               : null,
                PromoteCommand = CanManage ? new RelayCommand(() => { if (isRookie) PromoteRookie(captured); else Promote(captured); }) : null,
                DemoteCommand  = IsLeader  ? new RelayCommand(() => Demote(captured))                                             : null,
            };
        }

        // ── Hub event handlers ────────────────────────────────────────────────

        private void OnMemberOnline(string name, string rank)
        {
            var vm = Members.FirstOrDefault(m => m.Name == name)
                  ?? Rookies.FirstOrDefault(m => m.Name == name);
            if (vm is not null) vm.IsOnline = true;
        }

        private void OnMemberOffline(string name)
        {
            var vm = Members.FirstOrDefault(m => m.Name == name)
                  ?? Rookies.FirstOrDefault(m => m.Name == name);
            if (vm is not null) vm.IsOnline = false;
        }

        private void OnMemberJoined(string name, string rank) => Reload();
        private void OnMemberLeft(string name, bool kicked) => Reload();
        private void OnRankChanged(string name, string newRank) => Reload();
        private void OnLeaderChanged(string oldLeader, string newLeader) => Reload();
        private void OnRookieAdded(string name) => Reload();
        private void OnGuildDisbanded() { IsInGuild = false; Members.Clear(); Rookies.Clear(); StatusMessage = "Your guild was disbanded."; }
        private void OnGuildLeft() { IsInGuild = false; Members.Clear(); Rookies.Clear(); Reload(); }
        private void OnGuildKicked() { IsInGuild = false; Members.Clear(); Rookies.Clear(); StatusMessage = "You were removed from the guild."; Reload(); }
        private void OnInviteReceived(int inviteId, int guildId, string? guildName, string? guildTag, bool isRookie) => Reload();
        private void OnGuildError(string msg) => StatusMessage = msg;

        // ── Actions ───────────────────────────────────────────────────────────

        private async void AcceptInvite(int inviteId)
        {
            await GameHubService.GuildAcceptInviteAsync(inviteId);
        }

        private async void DeclineInvite(int inviteId)
        {
            await GameHubService.GuildDeclineInviteAsync(inviteId);
            var inv = PendingInvites.FirstOrDefault(i => i.InviteId == inviteId);
            if (inv is not null) PendingInvites.Remove(inv);
        }

        private async void Kick(string targetName)
        {
            await GameHubService.GuildKickAsync(targetName);
        }

        private async void Promote(string targetName)
        {
            await GameHubService.GuildPromoteAsync(targetName);
        }

        private async void PromoteRookie(string targetName)
        {
            await GameHubService.GuildPromoteRookieAsync(targetName);
        }

        private async void Demote(string targetName)
        {
            await GameHubService.GuildDemoteAsync(targetName);
        }

        private async void ReloadProperty()
        {
            var prop = await ServerApiService.GetGuildPropertyAsync();
            GuildPropertyCache.Set(prop);
            if (prop is not null)
            {
                PropertyType       = prop.Type == "House" ? "Guild House" : "Guild Base";
                PropertyAcquiredAt = prop.AcquiredAt.ToString("d MMM yyyy");
                PropertyRooms.Clear();
                foreach (var r in prop.Rooms)
                    PropertyRooms.Add(new GuildRoomRowVm(r));
                HasProperty = true;
            }
            else
            {
                HasProperty = false;
                PropertyRooms.Clear();
            }
            ViewModel_PageRoom.RefreshGuildEntryStatic();
        }

        private async void BuyHouse()
        {
            BuyHouseError = string.Empty;
            if (!int.TryParse(BuyHouseIdInput.Trim(), out int cityRoomId))
            {
                BuyHouseError = "Enter a valid numeric city room ID.";
                return;
            }
            var (success, error) = await ServerApiService.PurchaseGuildHouseAsync(cityRoomId);
            if (!success) { BuyHouseError = error; return; }
            BuyHouseIdInput = string.Empty;
            ReloadProperty();
            StatusMessage = "Guild house purchased!";
        }

        private async void EstablishBase()
        {
            EstablishBaseError = string.Empty;
            var roomId = Myria.Lib.Core.Services.UserAccountService.CurrentCharacter?.CurrentRoomId ?? 0;
            if (roomId == 0) { EstablishBaseError = "You are not in a valid room."; return; }
            var (success, error) = await ServerApiService.EstablishGuildBaseAsync(roomId);
            if (!success) { EstablishBaseError = error; return; }
            ReloadProperty();
            StatusMessage = "Guild base established!";
        }

        private async void SendInvite()
        {
            InviteError = string.Empty;
            if (string.IsNullOrWhiteSpace(InviteName)) { InviteError = "Enter a player name."; return; }
            await GameHubService.GuildSendInviteAsync(InviteName.Trim(), InviteAsRookie);
            // Hub event handler / GuildError will surface any server-side error.
            // On success, close panel so the officer can see the result in the status area.
            ShowInvitePanel = false;
            InviteName = string.Empty;
        }

        private async void LeaveGuild()
        {
            if (IsLeader) { StatusMessage = "Transfer leadership or disband the guild first."; return; }
            await GameHubService.GuildLeaveAsync();
        }

        private async void DisbandGuild()
        {
            await GameHubService.GuildDisbandAsync();
        }

        private async void SubmitCreate()
        {
            CreateError = string.Empty;
            if (string.IsNullOrWhiteSpace(CreateName)) { CreateError = "Name is required."; return; }
            var tag = CreateTag.Trim().ToUpperInvariant();
            if (tag.Length is 0 or > 5) { CreateError = "Tag must be 1–5 letters."; return; }

            var (success, error) = await ServerApiService.CreateGuildAsync(CreateName.Trim(), tag, CreateDesc.Trim());
            if (success)
            {
                ShowCreatePanel = false;
                Reload();
            }
            else
            {
                CreateError = error;
            }
        }
    }
}
