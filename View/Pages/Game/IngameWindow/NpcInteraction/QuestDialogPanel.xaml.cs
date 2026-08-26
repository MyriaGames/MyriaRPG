using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.NpcInteraction
{
    public partial class QuestDialogPanel : Page
    {
        public QuestDialogPanel(Quest quest, Npc npc, bool isReturn)
        {
            InitializeComponent();
            Action goBack = () => Myria.Wpf.Services.Navigation.Current.GoBack(NavigationFrameType.NpcWindow);
            DataContext = GameHubService.IsConnected
                ? new MultiplayerQuestDialogPanelViewModel(quest, UserAccoundService.CurrentCharacter, npc, isReturn, goBack)
                : new QuestDialogPanelViewModel(quest, UserAccoundService.CurrentCharacter, npc, isReturn, goBack);
        }
    }
}
