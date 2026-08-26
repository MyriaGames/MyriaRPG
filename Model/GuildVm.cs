using Myria.Wpf.Services;
using Myria.Wpf.ViewModel;
using System.Windows.Input;
using System.Windows.Media;

namespace Myria.Wpf.Model
{
    public class GuildMemberVm : BaseViewModel
    {
        public int    CharacterId { get; init; }
        public string Name        { get; init; } = string.Empty;
        public string Rank        { get; init; } = string.Empty;
        public bool   IsRookie    { get; init; }

        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); OnPropertyChanged(nameof(OnlineDot)); OnPropertyChanged(nameof(OnlineBrush)); }
        }

        public string AvatarInitial => string.IsNullOrEmpty(Name) ? "?" : Name[0].ToString().ToUpper();

        public string OnlineDot  => IsOnline ? "●" : "○";
        public Brush  OnlineBrush => IsOnline
            ? new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0x6A))
            : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x66));

        public Brush RankBrush => Rank switch
        {
            "Leader"  => new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)),
            "Officer" => new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00)),
            "Ward"    => new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0x6A)),
            "Elite"   => new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
            "Rookie"  => new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            _         => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
        };

        public ICommand? KickCommand    { get; init; }
        public ICommand? PromoteCommand { get; init; }
        public ICommand? DemoteCommand  { get; init; }
    }

    public class GuildRoomRowVm
    {
        public string DisplayName     { get; }
        public string MinRankRequired { get; }
        public bool   IsBuilt         { get; }

        public GuildRoomRowVm(ServerApiService.GuildRoomInfo info)
        {
            DisplayName     = FormatRoomType(info.RoomType);
            MinRankRequired = info.MinRankRequired;
            IsBuilt         = info.IsBuilt;
        }

        private static string FormatRoomType(string t) => t switch
        {
            "GatheringPlace" => "Gathering Place",
            "OfficersHall"   => "Officers' Hall",
            "LeaderOffice"   => "Leader's Office",
            _                => t,
        };
    }

    public class GuildInviteVm : BaseViewModel
    {
        public int    InviteId      { get; init; }
        public int    GuildId       { get; init; }
        public string GuildName     { get; init; } = string.Empty;
        public string GuildTag      { get; init; } = string.Empty;
        public bool   IsRookieInvite { get; init; }
        public string TypeLabel     => IsRookieInvite ? "ROOKIE" : "MEMBER";

        public ICommand AcceptCommand  { get; init; } = null!;
        public ICommand DeclineCommand { get; init; } = null!;
    }
}
