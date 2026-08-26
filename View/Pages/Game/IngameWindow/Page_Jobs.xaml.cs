using Myria.Lib.Core.Services;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_Jobs : Page
    {
        public Page_Jobs()
        {
            InitializeComponent();
            var ch = UserAccountService.CurrentCharacter;
            DataContext = GameHubService.IsConnected
                ? (JobsPageViewModel)new MultiplayerJobsPageViewModel(ch)
                : new JobsPageViewModel(ch);
        }
    }
}
