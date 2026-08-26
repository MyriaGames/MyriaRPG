using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.UserControls;

namespace Myria.Wpf.View.UserControls
{
    public partial class CharacterMenuWindow : UserControl
    {
        // Primary window — registers with Navigation service
        public CharacterMenuWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel_CharacterMenuWindow();
            Navigation.Current.RegisterFrame(NavigationFrameType.CharacterMenu, Frame);
            PreviewMouseDown += (_, _) => BringToFront();
        }

        // Secondary window — owns its frame, does not register with Navigation
        internal CharacterMenuWindow(Action<Thickness> setMarginAction, Action closeAction,
                                  double relLeft = 0.2, double relTop = 0.15)
        {
            InitializeComponent();
            DataContext = new ViewModel_CharacterMenuWindow(setMarginAction, closeAction, relLeft, relTop);
            PreviewMouseDown += (_, _) => BringToFront();
        }

        public void NavigateTo(Page page, string title)
        {
            Frame.Navigate(page);
            ((ViewModel_CharacterMenuWindow)DataContext).SetTitleAndSection(title, string.Empty);
        }

        private void BringToFront()
        {
            Panel.SetZIndex(this, WindowManager.NextZIndex());
        }
    }
}
