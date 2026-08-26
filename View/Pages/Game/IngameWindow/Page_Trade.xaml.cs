using Myria.Wpf.ViewModel.Pages.Game.IngameWindow;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myria.Wpf.View.Pages.Game.IngameWindow
{
    public partial class Page_Trade : Page
    {
        public Page_Trade()
        {
            InitializeComponent();
        }

        private void InvTile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.DataContext is InventoryItemViewModel item && item.Item != null)
            {
                DragDrop.DoDragDrop(b, new DataObject(typeof(InventoryItemViewModel), item), DragDropEffects.Copy);
                e.Handled = true;
            }
        }

        private void MyOffer_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(InventoryItemViewModel))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void MyOffer_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(InventoryItemViewModel)) is InventoryItemViewModel item
                && DataContext is TradeViewModel vm)
            {
                vm.AddItemByDrop(item);
                e.Handled = true;
            }
        }
    }
}
