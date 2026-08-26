using System.Windows.Media;

namespace Myria.Wpf.Model
{
    public enum ChatChannel { General, Room, Global, Party, Whisper }

    public class ChatMessageVm
    {
        public ChatChannel Channel { get; init; }
        public string Text { get; init; } = string.Empty;
        public string Sender { get; init; } = string.Empty;
        public Brush Foreground { get; init; } = Brushes.WhiteSmoke;

        private static readonly Brush RoomBrush    = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
        private static readonly Brush GlobalBrush  = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00));
        private static readonly Brush PartyBrush   = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
        private static readonly Brush WhisperBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0x93, 0xD8));

        public static Brush BrushFor(ChatChannel channel) => channel switch
        {
            ChatChannel.Global  => GlobalBrush,
            ChatChannel.Party   => PartyBrush,
            ChatChannel.Whisper => WhisperBrush,
            _                   => RoomBrush,
        };
    }
}
