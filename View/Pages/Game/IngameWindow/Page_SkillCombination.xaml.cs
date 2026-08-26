using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_SkillCombination : Page
    {
        public Page_SkillCombination()
        {
            InitializeComponent();
            DataContext = GameHubService.IsConnected
                ? (SkillCombinationViewModel)new MultiplayerSkillCombinationViewModel()
                : new SkillCombinationViewModel();
        }
    }
}
