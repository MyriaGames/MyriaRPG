using Myria.Lib.Core.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Windows.Controls;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.Inventory
{
    public partial class MoneyBag : Page
    {
        public MoneyBag()
        {
            InitializeComponent();
            this.DataContext = new MoneyBagViewModel(UserAccoundService.CurrentCharacter);
        }
    }
}
