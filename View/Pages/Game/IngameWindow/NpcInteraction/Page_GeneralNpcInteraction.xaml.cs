using Myria.Lib.Core.Entities.NPCs;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.NpcInteraction;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.NpcInteraction
{
    public partial class Page_GeneralNpcInteraction : Page
    {
        public Page_GeneralNpcInteraction(Npc npc)
        {
            InitializeComponent();
            this.DataContext = new GeneralNpcInteractionViewModel(npc);
            NpcPanelFrame.Navigate(new DialogPanel(npc));
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            return value.ToString().Equals(parameter.ToString(), StringComparison.InvariantCultureIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
