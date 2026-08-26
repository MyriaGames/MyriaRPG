using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.UserControls;
using System.Windows;
using System.Windows.Controls;

namespace Myria.Wpf.View.UserControls
{
    public partial class TradeWindow : UserControl
    {
        public TradeWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel_TradeWindow();
            Navigation.Current.RegisterFrame(NavigationFrameType.TradeWindow, Frame);
            PreviewMouseDown += (_, _) => Panel.SetZIndex(this, WindowManager.NextZIndex());
        }

        public void NavigateTo(Page page, string title)
        {
            if (DataContext is ViewModel_TradeWindow vm)
                vm.Title = title;
            Frame.Navigate(page);
        }
    }
}
