using System.Windows;
using System.Windows.Input;
using Myria.Lib.Core.Models.Settings;
using Myria.Wpf.Services;
using Myria.Wpf.View.Pages;
using Myria.Wpf.View.Pages.Game;
using Myria.Wpf.View.UserControls;

namespace Myria.Wpf.View.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CharacterMenuWindow    playerMenuWindow;
        public NpcWindow           npcWindow;
        public SkillDetailWindow   skillDetailWindow;
        public AddFriendWindow     addFriendWindow;
        public TradeProposalWindow tradeProposalWindow;
        public TradeWindow         tradeWindow;

        public static MainWindow Instance { get; private set; }

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();

            playerMenuWindow    = new CharacterMenuWindow();
            npcWindow           = new NpcWindow();
            skillDetailWindow   = new SkillDetailWindow();
            addFriendWindow     = new AddFriendWindow();
            tradeProposalWindow = new TradeProposalWindow();
            tradeWindow         = new TradeWindow();

            Navigation.Current.RegisterFrame(NavigationFrameType.Main, Frame);
            Navigation.Current.Navigate(NavigationFrameType.Main, new Page_Loading());

            Canvas.Children.Add(playerMenuWindow);
            Canvas.Children.Add(npcWindow);
            Canvas.Children.Add(skillDetailWindow);
            Canvas.Children.Add(addFriendWindow);
            Canvas.Children.Add(tradeProposalWindow);
            Canvas.Children.Add(tradeWindow);

            playerMenuWindow.Visibility    = Visibility.Hidden;
            npcWindow.Visibility           = Visibility.Hidden;
            skillDetailWindow.Visibility   = Visibility.Hidden;
            addFriendWindow.Visibility     = Visibility.Hidden;
            tradeProposalWindow.Visibility = Visibility.Hidden;
            tradeWindow.Visibility         = Visibility.Hidden;

            ApplyWindowMode(Settings.Current.VisualSettings.FullScreen);

            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!Navigation.Current.IsInGame)
                return;

            // Never steal keys from a focused text input
            if (e.OriginalSource is System.Windows.Controls.TextBox
                or System.Windows.Controls.RichTextBox
                or System.Windows.Controls.PasswordBox)
                return;

            // Block game shortcuts when any ingame overlay is open
            if (playerMenuWindow.Visibility  == Visibility.Visible ||
                npcWindow.Visibility         == Visibility.Visible ||
                skillDetailWindow.Visibility == Visibility.Visible ||
                addFriendWindow.Visibility   == Visibility.Visible)
                return;

            if (Navigation.Current.IsInFight)
                Page_Fight.Current?.HandleKey(e);
            else
                Page_Game.Current?.HandleKey(e);
        }

        public void ApplyWindowMode(bool fullScreen)
        {
            if (fullScreen)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = WindowState.Normal;
            }
        }
    }
}
