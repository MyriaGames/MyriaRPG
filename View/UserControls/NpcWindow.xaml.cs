using System.Windows;
using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.UserControls;

namespace Myria.Wpf.View.UserControls
{
    public partial class NpcWindow : UserControl
    {
        public NpcWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel_NpcWindow();
            Navigation.Current.RegisterFrame(NavigationFrameType.NpcWindow, Frame);
            PreviewMouseDown += (_, _) => Panel.SetZIndex(this, WindowManager.NextZIndex());
        }

        public void NavigateTo(Page page, string title)
        {
            Frame.Navigate(page);
            ((ViewModel_NpcWindow)DataContext).SetTitle(title);
            Visibility = Visibility.Visible;
            Panel.SetZIndex(this, WindowManager.NextZIndex());
        }
    }
}
