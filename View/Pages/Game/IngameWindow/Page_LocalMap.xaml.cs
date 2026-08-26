using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_LocalMap : Page
    {
        public Page_LocalMap()
        {
            InitializeComponent();
        }

        private void BtnZoomIn_Click(object sender, System.Windows.RoutedEventArgs e)
            => MapControl.ZoomIn();

        private void BtnZoomOut_Click(object sender, System.Windows.RoutedEventArgs e)
            => MapControl.ZoomOut();
    }
}
