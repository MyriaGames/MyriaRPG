using Myria.Lib.Core.Services;
using Myria.Wpf.Pages;
using Myria.Wpf.Services;
using Myria.Wpf.Utils;
using Myria.Wpf.Model;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Pages.Game.IngameWindow;
using Myria.Wpf.View.Pages.Settings;
using Myria.Wpf.View.Windows;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.UserControls
{
    public class ViewModel_CharacterMenuWindow : BaseViewModel
    {
        private static double _savedRelLeft = 0.1;
        private static double _savedRelTop  = 0.1;

        private readonly bool _persistPosition;
        private readonly Action<Thickness> _setMarginAction;
        private readonly Action _closeAction;
        private double _relLeft;
        private double _relTop;

        private string _title = "Window";
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        // ── Localized navigation labels ──────────────────────────────────────
        private string _navOverview    = string.Empty;
        private string _navSkills      = string.Empty;
        private string _navJobs        = string.Empty;
        private string _navQuests      = string.Empty;
        private string _navFriends     = string.Empty;
        private string _navRequests    = string.Empty;
        private string _navBlocked     = string.Empty;
        private string _navInventory   = string.Empty;
        private string _navCharacter   = string.Empty;
        private string _navVisuals     = string.Empty;
        private string _navKeybindings = string.Empty;
        private string _navMods        = string.Empty;
        private string _navCharSelect  = string.Empty;
        private string _navMainMenu    = string.Empty;
        private string _navSave        = string.Empty;
        private string _navSaveQuit    = string.Empty;
        private string _navMap         = string.Empty;
        private string _navSettings    = string.Empty;
        private string _sidebarQuote   = string.Empty;
        private string _navGuild       = "GUILD";

        [LocalizedKey("pg.gamewindow.nav.overview")]
        public string NavOverview    { get => _navOverview;    set { _navOverview    = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.skills")]
        public string NavSkills      { get => _navSkills;      set { _navSkills      = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.gamewindow.nav.jobs")]
        public string NavJobs        { get => _navJobs;        set { _navJobs        = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.quests")]
        public string NavQuests      { get => _navQuests;      set { _navQuests      = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.gamewindow.nav.friends")]
        public string NavFriends     { get => _navFriends;     set { _navFriends     = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.gamewindow.nav.requests")]
        public string NavRequests    { get => _navRequests;    set { _navRequests    = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.gamewindow.nav.blocked")]
        public string NavBlocked     { get => _navBlocked;     set { _navBlocked     = value; OnPropertyChanged(); } }
        public string NavGuild       { get => _navGuild;       set { _navGuild       = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.inventory")]
        public string NavInventory   { get => _navInventory;   set { _navInventory   = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.character")]
        public string NavCharacter   { get => _navCharacter;   set { _navCharacter   = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.settings.visuals")]
        public string NavVisuals     { get => _navVisuals;     set { _navVisuals     = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.settings.keybindings")]
        public string NavKeybindings { get => _navKeybindings; set { _navKeybindings = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.settings.mods")]
        public string NavMods        { get => _navMods;        set { _navMods        = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.character.select.title")]
        public string NavCharSelect  { get => _navCharSelect;  set { _navCharSelect  = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.menu.main")]
        public string NavMainMenu    { get => _navMainMenu;    set { _navMainMenu    = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.save")]
        public string NavSave        { get => _navSave;        set { _navSave        = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.quit.save")]
        public string NavSaveQuit    { get => _navSaveQuit;    set { _navSaveQuit    = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.map")]
        public string NavMap         { get => _navMap;         set { _navMap         = value; OnPropertyChanged(); } }
        [LocalizedKey("app.general.UI.settings")]
        public string NavSettings    { get => _navSettings;    set { _navSettings    = value; OnPropertyChanged(); } }
        [LocalizedKey("pg.gamewindow.quote")]
        public string SidebarQuote   { get => _sidebarQuote;   set { _sidebarQuote   = value; OnPropertyChanged(); } }

        public bool IsChatAvailable => ServerApiService.Token is not null;

        private string _activeTopSection = string.Empty;
        public string ActiveTopSection
        {
            get => _activeTopSection;
            private set
            {
                _activeTopSection = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SideColumnWidth));
                OnPropertyChanged(nameof(SideGutterWidth));
                OnPropertyChanged(nameof(SideNavigationVisibility));
                OnPropertyChanged(nameof(CharacterSideVisibility));
                OnPropertyChanged(nameof(QuestsSideVisibility));
                OnPropertyChanged(nameof(SocialSideVisibility));
                OnPropertyChanged(nameof(InventorySideVisibility));
                OnPropertyChanged(nameof(SettingsSideVisibility));
            }
        }

        private string _activeSideSection = string.Empty;
        public string ActiveSideSection
        {
            get => _activeSideSection;
            private set { _activeSideSection = value; OnPropertyChanged(); }
        }

        private double _left;
        private double _top;
        private double _width = 1120;
        private double _height = 720;
        private int _zIndex;

        public double Left   { get => _left;   set { _left   = value; OnPropertyChanged(); } }
        public double Top    { get => _top;    set { _top    = value; OnPropertyChanged(); } }
        public double Width  { get => _width;  set { _width  = value; OnPropertyChanged(); } }
        public double Height { get => _height; set { _height = value; OnPropertyChanged(); } }
        public int ZIndex    { get => _zIndex; set { _zIndex = value; OnPropertyChanged(); } }

        private static bool IsSideCollapsed(string section) => section is "Map";
        public GridLength SideColumnWidth => IsSideCollapsed(ActiveTopSection) ? new GridLength(0) : new GridLength(220);
        public GridLength SideGutterWidth => IsSideCollapsed(ActiveTopSection) ? new GridLength(0) : new GridLength(14);
        public Visibility SideNavigationVisibility => IsSideCollapsed(ActiveTopSection) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility CharacterSideVisibility => ActiveTopSection == "Character" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility QuestsSideVisibility => ActiveTopSection == "Quests" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SocialSideVisibility => ActiveTopSection == "Social" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility InventorySideVisibility => ActiveTopSection == "Inventory" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SettingsSideVisibility => ActiveTopSection == "Settings" ? Visibility.Visible : Visibility.Collapsed;

        public ICommand CloseCommand { get; }
        public ICommand FocusCommand { get; }
        public ICommand DragDeltaCommand { get; }
        public ICommand ResizeDeltaCommand { get; }
        public ICommand OpenOverviewCommand { get; }
        public ICommand OpenCharacterCommand { get; }
        public ICommand OpenQuestsCommand { get; }
        public ICommand OpenSkillsCommand { get; }
        public ICommand OpenJobsCommand { get; }
        public ICommand OpenInventoryCommand { get; }
        public ICommand OpenFriendsCommand { get; }
        public ICommand SelectFriendsTabCommand  { get; }
        public ICommand SelectRequestsTabCommand { get; }
        public ICommand SelectBlockedTabCommand  { get; }
        public ICommand OpenGuildCommand  { get; }
        public ICommand OpenMapCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenVisualsCommand { get; }
        public ICommand OpenKeybindingsCommand { get; }
        public ICommand OpenModsCommand { get; }
        public ICommand OpenCharacterSelectionCommand { get; }
        public ICommand OpenMainMenuCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAndQuitCommand { get; }

        public ViewModel_CharacterMenuWindow()
            : this(
                setMarginAction: m => MainWindow.Instance.playerMenuWindow.Margin = m,
                closeAction:     () => MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Hidden,
                relLeft: _savedRelLeft,
                relTop: _savedRelTop,
                persistPosition: true)
        { }

        public ViewModel_CharacterMenuWindow(Action<Thickness> setMarginAction, Action closeAction,
                                    double relLeft = 0.2, double relTop = 0.15,
                                    bool persistPosition = false)
        {
            _setMarginAction = setMarginAction;
            _closeAction = closeAction;
            _relLeft = relLeft;
            _relTop = relTop;
            _persistPosition = persistPosition;

            // This ViewModel is a long-lived singleton created at app startup, before login —
            // IsChatAvailable would otherwise never refresh once the player actually logs in.
            ServerApiService.AuthStateChanged += () => OnPropertyChanged(nameof(IsChatAvailable));

            CloseCommand = new RelayCommand(Close);
            FocusCommand = new RelayCommand(BringToFront);
            DragDeltaCommand = new RelayCommand<DragDeltaArgs>(OnDragDelta);
            ResizeDeltaCommand = new RelayCommand<ResizeDeltaArgs>(OnResizeDelta);

            OpenOverviewCommand = new RelayCommand(OpenOverview);
            OpenCharacterCommand = new RelayCommand(OpenContextCharacter);
            OpenQuestsCommand = new RelayCommand(OpenQuests);
            OpenSkillsCommand = new RelayCommand(OpenSkills);
            OpenJobsCommand = new RelayCommand(OpenJobs);
            OpenInventoryCommand = new RelayCommand(OpenInventory);
            OpenFriendsCommand       = new RelayCommand(OpenFriends);
            SelectFriendsTabCommand  = new RelayCommand(() => OpenFriendsTab(FriendsTab.Friends));
            SelectRequestsTabCommand = new RelayCommand(() => OpenFriendsTab(FriendsTab.Requests));
            SelectBlockedTabCommand  = new RelayCommand(() => OpenFriendsTab(FriendsTab.Blocked));
            OpenGuildCommand         = new RelayCommand(OpenGuild);
            OpenMapCommand = new RelayCommand(OpenMap);
            OpenSettingsCommand = new RelayCommand(OpenVisuals);
            OpenVisualsCommand = new RelayCommand(OpenVisuals);
            OpenKeybindingsCommand = new RelayCommand(OpenKeybindings);
            OpenModsCommand = new RelayCommand(OpenMods);
            OpenCharacterSelectionCommand = new RelayCommand(async () => await OpenCharacterSelectionAsync());
            OpenMainMenuCommand = new RelayCommand(async () => await OpenMainMenuAsync());
            SaveCommand = new RelayCommand(async () => await SaveAsync());
            SaveAndQuitCommand = new RelayCommand(async () => await SaveAndQuitAsync());

            RegisterIngameFactories();

            var host = MainWindow.Instance.WindowGrid;
            if (host.ActualWidth > 0)
                ApplyRelativePosition(host.ActualWidth, host.ActualHeight);
            else
                host.Loaded += (_, _) => ApplyRelativePosition(host.ActualWidth, host.ActualHeight);

            host.SizeChanged += OnHostSizeChanged;
        }

        private static void RegisterIngameFactories()
        {
            var nav = Navigation.Current;
            nav.RegisterView(Nav.Character,          NavigationFrameType.CharacterMenu, () => new Page_Character());
            nav.RegisterView(Nav.Skills,             NavigationFrameType.CharacterMenu, () => new Page_Skills());
            nav.RegisterView(Nav.Job,                NavigationFrameType.CharacterMenu, () => new Page_Jobs());
            nav.RegisterView(Nav.Quest,              NavigationFrameType.CharacterMenu, () => new Page_QuestList());
            nav.RegisterView(Nav.Friends,            NavigationFrameType.CharacterMenu, () => new Page_Friends());
            nav.RegisterView(Nav.Inventory,          NavigationFrameType.CharacterMenu, () => new InventoryPage(UserAccoundService.CurrentCharacter));
            nav.RegisterView(Nav.SkillCombination,   NavigationFrameType.CharacterMenu, () => new Page_SkillCombination());
            nav.RegisterView(Nav.SkillSlot,          NavigationFrameType.CharacterMenu, () => new Page_SkillSlots());
            nav.RegisterView(Nav.Runes,              NavigationFrameType.CharacterMenu, () => new Page_Runes());
            nav.RegisterView(Nav.RuneDrawing,        NavigationFrameType.CharacterMenu, () => new Page_RuneDrawing());
            nav.RegisterView(Nav.RuneLexica,         NavigationFrameType.CharacterMenu, () => new Page_RuneLexica());
            nav.RegisterView(Nav.SettingsVisuals,    NavigationFrameType.CharacterMenu, () => new Page_SettingsVisuals());
            nav.RegisterView(Nav.SettingsKeybindings,NavigationFrameType.CharacterMenu, () => new Page_Keybindings());
            nav.RegisterView(Nav.SettingsMods,       NavigationFrameType.CharacterMenu, () => new Page_SettingsMods(allowToggle: false));
            nav.RegisterView(Nav.Settings,           NavigationFrameType.CharacterMenu, () => new Page_Settings());
            nav.RegisterView(Nav.Guild,              NavigationFrameType.CharacterMenu, () => new Page_Guild());
        }

        private void BringToFront() => ZIndex = WindowManager.NextZIndex();

        private void Close()
        {
            if (_persistPosition) SaveRelativePosition();
            _closeAction();
        }

        private void OnHostSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyRelativePosition(e.NewSize.Width, e.NewSize.Height);
        }

        private void ApplyRelativePosition(double hostW, double hostH)
        {
            if (hostW <= 0 || hostH <= 0) return;
            Left = _relLeft * hostW;
            Top = _relTop * hostH;
            ClampAndSync();
        }

        private void SaveRelativePosition()
        {
            var host = MainWindow.Instance.WindowGrid;
            if (host.ActualWidth > 0) _savedRelLeft = Left / host.ActualWidth;
            if (host.ActualHeight > 0) _savedRelTop = Top / host.ActualHeight;
        }

        private void ClampAndSync()
        {
            var host = MainWindow.Instance.WindowGrid;
            if (Left < -40) Left = -40;
            if (Top < 0) Top = 0;
            if (Left > host.ActualWidth - 20) Left = host.ActualWidth - 20;
            if (Top > host.ActualHeight - 20) Top = host.ActualHeight - 20;
            _setMarginAction(new Thickness(Left, Top, 0, 0));
        }

        private void OnDragDelta(DragDeltaArgs a)
        {
            Left += a.HorizontalChange;
            Top += a.VerticalChange;
            ClampAndSync();
            if (_persistPosition) SaveRelativePosition();
        }

        private void OnResizeDelta(ResizeDeltaArgs a)
        {
            Width = Math.Max(Width + a.HorizontalChange, 200);
            Height = Math.Max(Height + a.VerticalChange, 120);
        }

        public void SetTitleAndSection(string title, string activeSection)
        {
            SetTitleAndSection(title, ResolveTopSection(activeSection), activeSection);
        }

        public void SetTitleAndSection(string title, string activeTopSection, string activeSideSection)
        {
            Title = title;
            ActiveTopSection = activeTopSection;
            ActiveSideSection = activeSideSection;
        }

        private void Navigate(Page page, string title, string activeTopSection, string activeSideSection)
        {
            SetTitleAndSection(title, activeTopSection, activeSideSection);
            Navigation.Current.Navigate(page);
        }

        private static string ResolveTopSection(string section) => section switch
        {
            "Overview" or "Character" or "Skills" or "Jobs" => "Character",
            "Quests" => "Quests",
            "Social" or "Friends" or "Requests" or "Blocked" or "Guild" => "Social",
            "Inventory" => "Inventory",
            "Map" => "Map",
            "Settings" or "Visuals" or "Keybindings" or "Mods" => "Settings",
            _ => string.Empty
        };

        private void OpenOverview()
        {
            var page = new Page_Character();
            var title = (page.DataContext as CharacterPageViewModel)?.WindowTitle ?? "Character";
            Navigate(page, title, "Character", "Overview");
        }

        private void OpenContextCharacter()
        {
            var top = ActiveTopSection is "Social" or "Inventory" ? ActiveTopSection : "Character";
            var side = top == "Character" ? "Overview" : "Character";
            var page = new Page_Character();
            var title = (page.DataContext as CharacterPageViewModel)?.WindowTitle ?? "Character";
            Navigate(page, title, top, side);
        }

        private void OpenQuests()
        {
            Navigate(new Page_QuestList(), "Quests", "Quests", "Quests");
        }

        private void OpenSkills()
        {
            if (UserAccoundService.CurrentCharacter.Class == Myria.Lib.Core.Systems.Enums.CharacterClass.RunicMage)
            {
                var runePage = new Page_Runes();
                var runeTitle = (runePage.DataContext as RunePageViewModel)?.WindowTitle ?? "Runes";
                Navigate(runePage, runeTitle, "Character", "Skills");
                return;
            }

            var page = new Page_Skills();
            var title = (page.DataContext as SkillPageViewModel)?.WindowTitle ?? "Skills";
            Navigate(page, title, "Character", "Skills");
        }

        private void OpenJobs()
        {
            var top = ActiveTopSection == "Quests" ? "Quests" : "Character";
            Navigate(new Page_Jobs(), "Jobs", top, "Jobs");
        }

        private void OpenInventory()
        {
            if (UserAccoundService.CurrentCharacter is null)
                return;

            Navigate(new InventoryPage(UserAccoundService.CurrentCharacter), "Inventory", "Inventory", "Inventory");
        }

        private void OpenFriends() => OpenFriendsTab(FriendsTab.Friends);

        private void OpenGuild()
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            Navigate(new Page_Guild(), "Guild", "Social", "Guild");
        }

        private void OpenFriendsTab(FriendsTab tab)
        {
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Visible;
            var page = new Page_Friends();
            if (page.DataContext is FriendsPageViewModel vm)
                vm.SelectedTab = tab;
            var section = tab switch
            {
                FriendsTab.Requests => "Requests",
                FriendsTab.Blocked  => "Blocked",
                _                   => "Friends",
            };
            Navigate(page, "Friends", "Social", section);
        }

        private void OpenMap()
        {
            var character = UserAccoundService.CurrentCharacter;
            if (character is null)
                return;

            var currentRoom = character.CurrentRoom ?? RoomService.GetRoomById(character.CurrentRoomId);
            if (currentRoom is null)
                return;

            var room = RoomService.GetRoomById(currentRoom.Id) ?? currentRoom;
            var vm = new ViewModel_PageLocalMap(room);
            Navigate(new Page_LocalMap { DataContext = vm }, vm.MapTitle, "Map", "Map");
        }

        private void OpenVisuals()
        {
            Navigate(new Page_SettingsVisuals(), "Settings", "Settings", "Visuals");
        }

        private void OpenKeybindings()
        {
            Navigate(new Page_Keybindings(), "Keybindings", "Settings", "Keybindings");
        }

        private void OpenMods()
        {
            Navigate(new Page_SettingsMods(allowToggle: false), "Mods", "Settings", "Mods");
        }

        private async Task OpenCharacterSelectionAsync()
        {
            await SaveAsync();
            Navigation.Current.SetGameState(false);
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_CharacterSelection());
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Hidden;
            MainWindow.Instance.npcWindow.Visibility = Visibility.Hidden;
        }

        private async Task OpenMainMenuAsync()
        {
            await SaveAsync();
            if (ServerApiService.Token is not null)
            {
                await GameHubService.DisconnectAsync();
                ServerApiService.ClearToken();
            }
            Navigation.Current.SetGameState(false);
            Navigation.Current.Navigate(Nav.Startup);
            MainWindow.Instance.playerMenuWindow.Visibility = Visibility.Hidden;
            MainWindow.Instance.npcWindow.Visibility = Visibility.Hidden;
        }

        private static async Task SaveAsync()
        {
            var player = UserAccoundService.CurrentCharacter;
            if (player is null)
                return;

            if (ServerApiService.Token is not null)
            {
                await ServerApiService.SaveCharacterAsync(player);
                return;
            }

            var user = UserAccoundService.CurrentUser;
            if (user is not null)
                CharacterService.SaveCharacter(user, player);
        }

        private static async Task SaveAndQuitAsync()
        {
            await SaveAsync();
            Application.Current.Shutdown();
        }
    }

    public record DragDeltaArgs(double HorizontalChange, double VerticalChange, double? HostWidth = null, double? HostHeight = null);
    public record ResizeDeltaArgs(double HorizontalChange, double VerticalChange);
}
