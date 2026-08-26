using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages
{
    /// <summary>
    /// Interaktionslogik für Page_StartupMenu.xaml
    /// </summary>
    public partial class Page_StartupMenu : Page
    {
        public Page_StartupMenu()
        {
            InitializeComponent();
            Myria.Wpf.Services.Navigation.Current.RegisterFrame(NavigationFrameType.Startup, Frame);
            this.DataContext = new ViewModel_StartupMenuPage();
        }

    }

}
