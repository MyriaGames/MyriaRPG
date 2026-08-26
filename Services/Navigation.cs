using System.Windows.Controls;
using System.Windows.Navigation;

namespace Myria.Wpf.Services
{
    public class Navigation
    {
        public static Navigation Current { get; } = new();

        private readonly Dictionary<NavigationFrameType, Frame> _frames = new();
        private readonly Dictionary<Nav, (NavigationFrameType FrameType, Func<Page> Factory)> _registrations = new();
        private readonly Dictionary<Nav, Page> _cache = new();

        public bool IsInGame { get; private set; }
        public void SetGameState(bool inGame) => IsInGame = inGame;

        public event Action<bool>? FightStateChanged;

        public bool IsInFight { get; private set; }
        public void SetFightState(bool isInFight)
        {
            IsInFight = isInFight;
            FightStateChanged?.Invoke(isInFight);
        }

        public void RegisterFrame(NavigationFrameType frameType, Frame frame)
        {
            // CharacterMenu and NpcWindow both support back-navigation (NPC sub-panel hierarchy)
            bool allowBackNav = frameType is NavigationFrameType.CharacterMenu
                                           or NavigationFrameType.NpcWindow;
            if (!allowBackNav)
                DisableJournalNavigation(frame);
            _frames[frameType] = frame;
        }

        public void RegisterView(Nav nav, NavigationFrameType targetFrame, Func<Page> factory)
            => _registrations[nav] = (targetFrame, factory);

        // Navigate to a registered, cached destination
        public bool Navigate(Nav nav)
        {
            if (!_registrations.TryGetValue(nav, out var reg)) return false;
            if (!_frames.TryGetValue(reg.FrameType, out var frame)) return false;
            if (!_cache.TryGetValue(nav, out var page))
            {
                page = reg.Factory();
                _cache[nav] = page;
            }
            try { frame.Navigate(page); return true; }
            catch (Exception ex)
            {
                ApplicationErrorService.ShowUnhandledError($"Navigating to {nav}", ex);
                return false;
            }
        }

        // Navigate a pre-created page to a specific frame (for dynamic/stateful content)
        public bool Navigate(NavigationFrameType frameType, Page page)
        {
            if (!_frames.TryGetValue(frameType, out var frame)) return false;
            try { frame.Navigate(page); return true; }
            catch (Exception ex)
            {
                ApplicationErrorService.ShowUnhandledError($"Navigating {frameType}", ex);
                return false;
            }
        }

        // Shorthand: navigate a pre-created page to the CharacterMenu frame
        public bool Navigate(Page page) => Navigate(NavigationFrameType.CharacterMenu, page);

        // Go back in the specified frame (defaults to CharacterMenu for backward compatibility)
        public bool GoBack(NavigationFrameType frameType = NavigationFrameType.CharacterMenu)
        {
            if (!_frames.TryGetValue(frameType, out var frame)) return false;
            if (!frame.CanGoBack) return false;
            frame.GoBack();
            return true;
        }

        public bool ClearFrame(NavigationFrameType frameType)
        {
            if (!_frames.TryGetValue(frameType, out var frame)) return false;
            frame.Content = null;
            return true;
        }

        // Lets a toggle button (open sub-view / close it on a second click) ask the frame itself
        // whether it's showing anything, instead of tracking "which page did I last open" in a
        // private field — a page's own Cancel/Back command can clear the frame independently of
        // that field, which would otherwise go stale and make the next toggle-open silently no-op.
        public bool IsFrameEmpty(NavigationFrameType frameType)
            => !_frames.TryGetValue(frameType, out var frame) || frame.Content is null;

        public void InvalidateCache(Nav nav) => _cache.Remove(nav);

        private static void DisableJournalNavigation(Frame frame)
        {
            frame.Navigating += (_, e) =>
            {
                if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
                    e.Cancel = true;
            };
        }
    }
}
