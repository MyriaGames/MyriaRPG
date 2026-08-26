using System.Windows.Controls;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    /// <summary>
    /// Interaktionslogik für Page_Skills.xaml
    /// </summary>
    public partial class Page_Skills : Page
    {
        public Page_Skills()
        {
            InitializeComponent();
            this.DataContext = new SkillPageViewModel();
        }
    }
}
