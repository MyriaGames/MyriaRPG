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
        {
            InitializeComponent();
            Action goBack = () => Myria.Wpf.Services.Navigation.Current.GoBack(NavigationFrameType.NpcWindow);
            DataContext = GameHubService.IsConnected
                ? new MultiplayerCraftPanelViewModel(npc, UserAccountService.CurrentCharacter, goBack)
                : new CraftPanelViewModel(npc, UserAccountService.CurrentCharacter, goBack);
        }
    }
}
