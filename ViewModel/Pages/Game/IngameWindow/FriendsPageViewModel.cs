using Myria.Lib.Core.Services;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.Model;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public enum FriendsTab { Friends, Requests, Blocked }

    public class FriendEntryVm : BaseViewModel
    {
        public int    FriendshipId       { get; init; }
        public string Username           { get; init; } = string.Empty;
        public string CharacterClass     { get; init; } = string.Empty;
        public int    Level              { get; init; }
        public string Location           { get; init; } = string.Empty;
        public bool   IsOnline           { get; init; }
        public bool   IsInParty          { get; init; }

        public string OnlineDot   => IsOnline ? "●" : "○";
        public Brush  OnlineColor => IsOnline
            ? new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0x6A))
            : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x66));

        public string StatusText => IsInParty ? "IN PARTY" : IsOnline ? "ONLINE" : "OFFLINE";
        public Brush  StatusBackground => IsInParty
            ? new SolidColorBrush(Color.FromArgb(0xCC, 0x15, 0x50, 0x8C))
            : IsOnline
                ? new SolidColorBrush(Color.FromArgb(0xCC, 0x1B, 0x5E, 0x20))
                : new SolidColorBrush(Color.FromArgb(0xCC, 0x33, 0x33, 0x33));
        public Brush  StatusForeground => IsInParty
            ? new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7))
            : IsOnline
                ? new SolidColorBrush(Color.FromRgb(0xA5, 0xD6, 0xA7))
                : new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));

        public string LevelClassText =>
            Level > 0 && !string.IsNullOrEmpty(CharacterClass)
                ? $"Lv.{Level} · {CharacterClass}"
                : Level > 0
                    ? $"Lv.{Level}"
                    : string.Empty;

        public string AvatarInitial => string.IsNullOrEmpty(Username) ? "?" : Username[0].ToString().ToUpper();

        public ICommand WhisperCommand        { get; init; } = null!;
        public ICommand InviteToPartyCommand  { get; init; } = null!;
        public ICommand RemoveCommand         { get; init; } = null!;
    }

    public class FriendRequestEntryVm : BaseViewModel
    {
        public int    FriendshipId { get; init; }
        public string FromUsername { get; init; } = string.Empty;

        public string AvatarInitial => string.IsNullOrEmpty(FromUsername) ? "?" : FromUsername[0].ToString().ToUpper();

        public ICommand AcceptCommand  { get; init; } = null!;
        public ICommand DeclineCommand { get; init; } = null!;
    }

    public class BlockedEntryVm : BaseViewModel
    {
        public int    BlockId       { get; init; }
        public string Username      { get; init; } = string.Empty;

        public string AvatarInitial => string.IsNullOrEmpty(Username) ? "?" : Username[0].ToString().ToUpper();

        public ICommand UnblockCommand { get; init; } = null!;
    }

    public class FriendsPageViewModel : BaseViewModel
    {
        private string _tblWhisper = string.Empty;
        private string _tblMoreOptions = string.Empty;
        private string _tblAddFriend = string.Empty;
        private string _tblRequestsHeader = string.Empty;
        private string _tblRequestsEmpty = string.Empty;
        private string _tblAccept = string.Empty;
        private string _tblDecline = string.Empty;
        private string _tblBlockedHeader = string.Empty;
        private string _tblBlockedDesc = string.Empty;
        private string _tblBlockedEmpty = string.Empty;
        private string _tblBlockedLabel = string.Empty;
        private string _tblUnblock = string.Empty;
        // ── Localized labels ─────────────────────────────────────────────────
        [LocalizedKey("pg.friends.whisper")]
        public string TblWhisper
        {
            get => _tblWhisper;
            set { _tblWhisper = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.more_options")]
        public string TblMoreOptions
        {
            get => _tblMoreOptions;
            set { _tblMoreOptions = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.add")]
        public string TblAddFriend
        {
            get => _tblAddFriend;
            set { _tblAddFriend = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.requests.header")]
        public string TblRequestsHeader
        {
            get => _tblRequestsHeader;
            set { _tblRequestsHeader = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.requests.empty")]
        public string TblRequestsEmpty
        {
            get => _tblRequestsEmpty;
            set { _tblRequestsEmpty = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.accept")]
        public string TblAccept
        {
            get => _tblAccept;
            set { _tblAccept = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.decline")]
        public string TblDecline
        {
            get => _tblDecline;
            set { _tblDecline = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.blocked.header")]
        public string TblBlockedHeader
        {
            get => _tblBlockedHeader;
            set { _tblBlockedHeader = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.blocked.desc")]
        public string TblBlockedDesc
        {
            get => _tblBlockedDesc;
            set { _tblBlockedDesc = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.blocked.empty")]
        public string TblBlockedEmpty
        {
            get => _tblBlockedEmpty;
            set { _tblBlockedEmpty = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.blocked.label")]
        public string TblBlockedLabel
        {
            get => _tblBlockedLabel;
            set { _tblBlockedLabel = value; OnPropertyChanged(); }
        }
        [LocalizedKey("pg.friends.unblock")]
        public string TblUnblock
        {
            get => _tblUnblock;
            set { _tblUnblock = value; OnPropertyChanged(); }
        }

        private readonly ObservableCollection<FriendEntryVm> _allFriends = new();

        public ObservableCollection<FriendEntryVm>        FilteredFriends { get; } = new();
        public ObservableCollection<FriendRequestEntryVm> Requests        { get; } = new();
        public ObservableCollection<BlockedEntryVm>       BlockedCharacters  { get; } = new();

        // ── Tab selection ─────────────────────────────────────────────────────

        private FriendsTab _selectedTab = FriendsTab.Friends;
        public FriendsTab SelectedTab
        {
            get => _selectedTab;
            set
            {
                _selectedTab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFriendsTab));
                OnPropertyChanged(nameof(IsRequestsTab));
                OnPropertyChanged(nameof(IsBlockedTab));
            }
        }

        public bool IsFriendsTab  => SelectedTab == FriendsTab.Friends;
        public bool IsRequestsTab => SelectedTab == FriendsTab.Requests;
        public bool IsBlockedTab  => SelectedTab == FriendsTab.Blocked;

        // ── Search / sort ─────────────────────────────────────────────────────

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        private bool _sortOnlineFirst = true;
        public bool SortOnlineFirst
        {
            get => _sortOnlineFirst;
            set { _sortOnlineFirst = value; OnPropertyChanged(); OnPropertyChanged(nameof(SortLabel)); ApplyFilter(); }
        }

        public string SortLabel => SortOnlineFirst ? "ONLINE FIRST" : "ALPHABETICAL";

        // ── Counts ────────────────────────────────────────────────────────────

        public int  FriendCount  => _allFriends.Count;
        public int  RequestCount => Requests.Count;
        public bool HasRequests  => Requests.Count > 0;
        public bool HasBlocked   => BlockedCharacters.Count > 0;

        // ── Commands ──────────────────────────────────────────────────────────

        public ICommand SelectFriendsTabCommand  { get; }
        public ICommand SelectRequestsTabCommand { get; }
        public ICommand SelectBlockedTabCommand  { get; }
        public ICommand ToggleSortCommand        { get; }
        public ICommand AddFriendCommand         { get; }
        public ICommand RefreshCommand           { get; }

        public FriendsPageViewModel()
        {
            SelectFriendsTabCommand  = new RelayCommand(() => SelectedTab = FriendsTab.Friends);
            SelectRequestsTabCommand = new RelayCommand(() => SelectedTab = FriendsTab.Requests);
            SelectBlockedTabCommand  = new RelayCommand(() => SelectedTab = FriendsTab.Blocked);
            ToggleSortCommand        = new RelayCommand(() => SortOnlineFirst = !SortOnlineFirst);
            AddFriendCommand         = new RelayCommand(OpenAddFriendWindow);
            RefreshCommand           = new RelayCommand(Reload);
            Reload();
        }

        public void Cleanup() => GameHubService.HubConnected -= Reload;

        private async void Reload()
        {
            _allFriends.Clear();
            Requests.Clear();
            BlockedCharacters.Clear();

            var friends  = await ServerApiService.GetFriendsAsync();
            var requests = await ServerApiService.GetFriendRequestsAsync();
            var blocks   = await ServerApiService.GetBlocksAsync();

            foreach (var f in friends)
            {
                _allFriends.Add(new FriendEntryVm
                {
                    FriendshipId         = f.FriendshipId,
                    Username             = f.CharacterName,
                    IsOnline             = f.IsOnline,
                    Level                = f.Level,
                    CharacterClass       = f.ClassName,
                    IsInParty            = f.InParty,
                    WhisperCommand       = new RelayCommand(() => ViewModel_PageGame.StartWhisper(f.CharacterName)),
                    InviteToPartyCommand = new RelayCommand(() => _ = GameHubService.InviteToPartyAsync(f.CharacterName)),
                    RemoveCommand        = new RelayCommand(() => RemoveFriend(f.FriendshipId)),
                });
            }

            foreach (var r in requests)
            {
                Requests.Add(new FriendRequestEntryVm
                {
                    FriendshipId   = r.FriendshipId,
                    FromUsername   = r.FromCharacterName,
                    AcceptCommand  = new RelayCommand(() => AcceptRequest(r.FriendshipId)),
                    DeclineCommand = new RelayCommand(() => DeclineRequest(r.FriendshipId)),
                });
            }

            foreach (var b in blocks)
            {
                BlockedCharacters.Add(new BlockedEntryVm
                {
                    BlockId         = b.BlockId,
                    Username        = b.Username,
                    UnblockCommand  = new RelayCommand(() => Unblock(b.BlockId)),
                });
            }

            OnPropertyChanged(nameof(FriendCount));
            OnPropertyChanged(nameof(RequestCount));
            OnPropertyChanged(nameof(HasRequests));
            OnPropertyChanged(nameof(HasBlocked));
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            FilteredFriends.Clear();

            var query = string.IsNullOrWhiteSpace(SearchText)
                ? _allFriends
                : _allFriends.Where(f => f.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            var sorted = SortOnlineFirst
                ? query.OrderByDescending(f => f.IsOnline).ThenBy(f => f.Username)
                : query.OrderBy(f => f.Username);

            foreach (var f in sorted)
                FilteredFriends.Add(f);
        }

        private void OpenAddFriendWindow()
        {
            var aw = View.Windows.MainWindow.Instance.addFriendWindow;
            aw.NavigateTo(new View.Pages.Game.IngameWindow.Page_AddFriend());
            aw.Visibility = System.Windows.Visibility.Visible;
            System.Windows.Controls.Panel.SetZIndex(aw, Services.WindowManager.NextZIndex());
        }

        private async void AcceptRequest(int id)
        {
            if (await ServerApiService.AcceptFriendRequestAsync(id))
                Reload();
        }

        private async void DeclineRequest(int id)
        {
            if (await ServerApiService.RemoveFriendAsync(id))
                Reload();
        }

        private async void RemoveFriend(int id)
        {
            if (await ServerApiService.RemoveFriendAsync(id))
                Reload();
        }

        private async void Unblock(int blockId)
        {
            if (await ServerApiService.UnblockAsync(blockId))
                Reload();
        }
    }
}
