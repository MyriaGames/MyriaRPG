using System.Windows.Controls;
using System.Windows.Navigation;
using MyriaRPG.View.Pages;

namespace MyriaRPG.Services
{
    public class Navigation
    {
        private static Frame MainNavigationFrame = new();
        private static Frame PageNavigationFrame = new();
        private static Frame SettingsNavigationFrame = new();
        private static Frame IngameNavigationFrame = new();
        private static Frame GameNavigationFrame = new();
        private static Frame IngameMenueNavigationFrame = new();
        private static Frame IngameSettingsNavigationFrame = new();
        private static Page GameRoomPage = new();
        private static Page GameCombatPage = new();

        public static bool IsInFight { get; private set; }
        public static void SetFightState(bool isInFight) => IsInFight = isInFight;

        private static void DisableJournalNavigation(Frame frame)
        {
            frame.Navigating += (s, e) =>
            {
                if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
                    e.Cancel = true;
            };
        }

        public static bool SetNavigationFrame(Frame navFrame, NavigationFrameType frameType)
        {
            DisableJournalNavigation(navFrame);
            switch (frameType)
            {
                case NavigationFrameType.Main: MainNavigationFrame = navFrame; MainNavigationFrame.Navigate(new Page_Loading()); return true;
                case NavigationFrameType.Startup: PageNavigationFrame = navFrame; return true;
                case NavigationFrameType.Settings: SettingsNavigationFrame = navFrame; return true;
                case NavigationFrameType.IngameWindow: IngameNavigationFrame = navFrame; return true;
                case NavigationFrameType.Game: GameNavigationFrame = navFrame; return true;
                case NavigationFrameType.IngameMenu: IngameMenueNavigationFrame = navFrame; return true;
                case NavigationFrameType.IngameSettings: IngameSettingsNavigationFrame = navFrame; return true;
                default: return false;
            }

        }
        public static bool RegisterGamePage(Page page, GamePageType pageType)
        {
            switch (pageType)
            {
                case GamePageType.Room: GameRoomPage = page; return true;
                case GamePageType.Combat: GameCombatPage = page; return true;
                default: return false;
            }

        }

        public static bool NavigateMain(Page page)
        {
            try
            {
                MainNavigationFrame.Navigate(page);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool NavigateStartup(Page? page)
        {
            try
            {
                if (page is null)
                    PageNavigationFrame.Content = null;
                else
                    PageNavigationFrame.Navigate(page);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool NavigateSettings(Page page)
        {
            try 
            {
                SettingsNavigationFrame.Navigate(page);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool NavigateIngameWindow(Page page)
        {
            try
            {
                IngameNavigationFrame.Navigate(page);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool NavigateGamePage(Page page)
        {
            try
            {
                GameNavigationFrame.Navigate(page);
            }
            catch
            { 
                return false; 
            }
            return true;
        }
        public static bool NavigateIngameMenue(Page page)
        {
            try
            {
                IngameMenueNavigationFrame.Navigate(page);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool NavigateIngameSettings(Page page)
        {
            try
            {
                IngameSettingsNavigationFrame.Navigate(page);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool NavigateGamePageToRegister(GamePageType id)
        {
            try
            {
                switch (id)
                {
                    case GamePageType.Room: GameNavigationFrame.Navigate(GameRoomPage); return true;
                    case GamePageType.Combat: GameNavigationFrame.Navigate(GameCombatPage); return true;
                    default: return false;
                }

            }
            catch 
            { 
                return false; 
            }

        }

    }

}
