using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_Keybindings : Page
    {
        private KeybindingsViewModel _vm;

        public Page_Keybindings()
        {
            InitializeComponent();
            _vm = new KeybindingsViewModel();
            DataContext = _vm;
            Focusable = true;
            Loaded += (_, _) => Focus();
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var listening = _vm.Rows.FirstOrDefault(r => r.IsListening);
            if (listening == null) return;

            // Ignore modifier-only presses
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LeftCtrl  || e.Key == Key.RightCtrl  ||
                e.Key == Key.LeftAlt   || e.Key == Key.RightAlt   ||
                e.Key == Key.System)
                return;

            listening.ApplyKey(e.Key);
            e.Handled = true;
        }
    }
}
