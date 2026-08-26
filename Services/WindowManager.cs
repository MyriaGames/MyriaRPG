using Myria.Wpf.View.UserControls;
using Myria.Wpf.View.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Myria.Wpf.Services
{
    public static class WindowManager
    {
        private static int _zCounter = 10;

        public static int NextZIndex() => ++_zCounter;

        public static CharacterMenuWindow OpenWindow(Page page, string title,
                                            double relLeft = 0.2, double relTop = 0.15)
        {
            CharacterMenuWindow? win = null;
            win = new CharacterMenuWindow(
                setMarginAction: m => { if (win != null) win.Margin = m; },
                closeAction:     () => MainWindow.Instance.Canvas.Children.Remove(win!),
                relLeft: relLeft, relTop: relTop);

            win.NavigateTo(page, title);
            MainWindow.Instance.Canvas.Children.Add(win);
            return win;
        }
    }
}
