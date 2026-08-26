using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_SkillSlots : Page
    {
        public Page_SkillSlots()
        {
            InitializeComponent();
            DataContext = GameHubService.IsConnected
                ? (SkillSlotViewModel)new MultiplayerSkillSlotViewModel()
                : new SkillSlotViewModel();
        }
    }
}
