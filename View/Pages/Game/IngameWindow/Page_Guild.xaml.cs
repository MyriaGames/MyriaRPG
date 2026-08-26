using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_Guild : Page
    {
        public Page_Guild()
        {
            InitializeComponent();
            var vm = new GuildPageViewModel();
            DataContext = vm;
            Unloaded += (_, _) => vm.Cleanup();
        }
    }
}
