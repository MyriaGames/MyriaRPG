using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    public partial class Page_MultiplayerHub : Page
    {
        public Page_MultiplayerHub()
        {
            InitializeComponent();
            Navigation.Current.RegisterFrame(NavigationFrameType.MultiplayerHub, Frame);
            DataContext = new ViewModel_MultiplayerHubPage();
        }
    }
}
