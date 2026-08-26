using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    /// <summary>
    /// Interaktionslogik für Page_QuestList.xaml
    /// </summary>
    public partial class Page_QuestList : Page
    {
        public Page_QuestList()
        {
            InitializeComponent();
            this.DataContext = new QuestListPageViewModel();
        }
    }
}
