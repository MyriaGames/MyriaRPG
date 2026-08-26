using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.NpcInteraction
{
    public partial class JobMasterPanel : Page
    {
        public JobMasterPanel(Npc npc)
        {
            InitializeComponent();
            Action goBack = () => Myria.Wpf.Services.Navigation.Current.GoBack(NavigationFrameType.NpcWindow);
            DataContext = GameHubService.IsConnected
                ? new MultiplayerJobMasterPanelViewModel(npc, UserAccountService.CurrentCharacter, goBack)
                : new JobMasterPanelViewModel(npc, UserAccountService.CurrentCharacter, goBack);
        }
    }
}
