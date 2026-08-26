using System.Windows;
using System.Windows.Controls;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.UserControls;

namespace Myria.Wpf.View.UserControls
{
    public partial class SkillDetailWindow : UserControl
    {
        public SkillDetailWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel_SkillDetailWindow();
            Navigation.Current.RegisterFrame(NavigationFrameType.SkillDetailWindow, Frame);
            PreviewMouseDown += (_, _) => Panel.SetZIndex(this, WindowManager.NextZIndex());
        }

        public void NavigateTo(Page page, string title)
        {
            Frame.Navigate(page);
            Panel.SetZIndex(this, WindowManager.NextZIndex());
            ((ViewModel_SkillDetailWindow)DataContext).SetTitle(title);
        }
    }
}
