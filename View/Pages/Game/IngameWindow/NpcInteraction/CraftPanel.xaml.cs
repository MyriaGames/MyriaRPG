using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.UserControls.IngameWindow;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.NpcInteraction
{
    public partial class CraftPanel : Page
    {
        public CraftPanel(Npc npc)
            : this(npc, () => Myria.Wpf.Services.Navigation.Current.GoBack(NavigationFrameType.NpcWindow), viaJobPage: false) { }

        /// <param name="viaJobPage">Opened from the Jobs page for an opted-in active job rather
        /// than at the NPC. Offline this makes no difference (local crafting has no proximity
        /// check); online it routes through GameHub.CraftFromJob.</param>
        public CraftPanel(Npc npc, Action goBack, bool viaJobPage)
        {
            InitializeComponent();
            DataContext = GameHubService.IsConnected
                ? new MultiplayerCraftPanelViewModel(npc, UserAccountService.CurrentCharacter, goBack, viaJobPage)
                : new CraftPanelViewModel(npc, UserAccountService.CurrentCharacter, goBack);
        }
    }
}
