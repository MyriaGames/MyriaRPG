using Myria.Lib.Core.Entities.Characters;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Windows.Controls;

namespace Myria.Wpf.Pages
{
    public partial class InventoryPage : Page
    {
        public InventoryPage(Character character)
        {
            InitializeComponent();
            this.DataContext = new InventoryPageViewModel();
        }
    }
}
