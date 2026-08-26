using System.Windows.Controls;
using System.Windows.Threading;
using Myria.Wpf.ViewModel.Pages;

namespace Myria.Wpf.View.Pages.Settings
{
    /// <summary>
    /// Interaktionslogik fuer Page_SettingsVisuals.xaml
    /// </summary>
    public partial class Page_SettingsVisuals : Page
    {
        public Page_SettingsVisuals()
        {
            InitializeComponent();
            var viewModel = new ViewModel_SettingsVisuals();
            DataContext = viewModel;
            Loaded += (_, _) => Dispatcher.BeginInvoke(
                viewModel.EnableSettingsWrites,
                DispatcherPriority.ContextIdle);
        }
    }
}
