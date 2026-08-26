using Myria.Lib.Core.Services;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.NpcInteraction
{
    public partial class ClassPanel : Page
    {
        public ClassPanel()
        {
            InitializeComponent();
            Action goBack = () => Myria.Wpf.Services.Navigation.Current.GoBack(NavigationFrameType.NpcWindow);
            DataContext = GameHubService.IsConnected
                ? new MultiplayerClassPanelViewModel(UserAccoundService.CurrentCharacter, goBack)
                : new ClassPanelViewModel(UserAccoundService.CurrentCharacter, goBack);
        }
    }
}
