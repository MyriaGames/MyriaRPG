using System.Windows;
using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.UserControls;

namespace Myria.Wpf.View.UserControls
{
    public partial class AddFriendWindow : UserControl
    {
        public AddFriendWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel_AddFriendWindow();
            Navigation.Current.RegisterFrame(NavigationFrameType.AddFriendWindow, Frame);
            PreviewMouseDown += (_, _) => Panel.SetZIndex(this, WindowManager.NextZIndex());
        }

        public void NavigateTo(Page page)
        {
            Frame.Navigate(page);
        }
    }
}
