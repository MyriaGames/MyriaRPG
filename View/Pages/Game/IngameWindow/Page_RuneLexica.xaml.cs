using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using System.Windows;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_RuneLexica : Page
    {
        public Page_RuneLexica()
        {
            InitializeComponent();
            DataContext = new RuneLexicaViewModel();
        }

        private RuneLexicaViewModel Vm => (RuneLexicaViewModel)DataContext;

        private void SaveLabel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is LexicaEntryVm entry)
                Vm.SaveLabel(entry);
        }
    }
}
