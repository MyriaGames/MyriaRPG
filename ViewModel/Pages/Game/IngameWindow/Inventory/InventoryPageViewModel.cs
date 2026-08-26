using Myria.Wpf.Model;
using Myria.Wpf.ViewModel;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory
{
    /// <summary>
    /// Page-level ViewModel for InventoryPage.xaml.
    /// Owns only the page title; the sub-pages (Inventory, Equipment, MoneyBag)
    /// each manage their own dedicated ViewModels.
    /// </summary>
    public class InventoryPageViewModel : BaseViewModel
    {
        private string _inventoryTitle;

        [LocalizedKey("app.general.UI.inventory")]
        public string InventoryTitle
        {
            get => _inventoryTitle;
            set => SetProperty(ref _inventoryTitle, value);
        }
    }
}
