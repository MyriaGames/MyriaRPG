using Myria.Lib.Core.Services;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.Inventory
{
    public partial class Inventory : Page
    {
        private InventoryGridViewModel _viewModel;

        public Inventory()
        {
            InitializeComponent();
            var ch = UserAccoundService.CurrentCharacter;
            _viewModel = GameHubService.IsConnected
                ? (InventoryGridViewModel)new MultiplayerInventoryGridViewModel(ch)
                : new InventoryGridViewModel(ch);
            this.DataContext = _viewModel;
        }

        private void InventorySlot_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border && border.DataContext is InventoryItemViewModel itemViewModel)
            {
                ItemTooltipPopup.PlacementTarget = border;
                bool flipLeft = itemViewModel.GridColumn >= 4;
                bool flipUp   = itemViewModel.GridRow >= 4;
                ItemTooltipPopup.CustomPopupPlacementCallback = (popupSize, targetSize, _) =>
                {
                    double x = flipLeft ? -popupSize.Width : targetSize.Width;
                    double y = flipUp   ? targetSize.Height - popupSize.Height : 0;
                    return new[] { new CustomPopupPlacement(new Point(x, y), PopupPrimaryAxis.None) };
                };
                _viewModel.ShowTooltipCommand.Execute(itemViewModel);
            }
        }

        private void InventorySlot_MouseLeave(object sender, MouseEventArgs e)
        {
            _viewModel.HideTooltipCommand.Execute(null);
        }

        private void InventorySlot_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(InventoryItemViewModel)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void InventorySlot_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(InventoryItemViewModel)) is InventoryItemViewModel draggedItem
                && sender is Border targetBorder)
            {
                if (targetBorder.DataContext is InventoryItemViewModel targetItem)
                    _viewModel.HandleItemDrop(draggedItem, targetItem.Index);
                else
                    _viewModel.RefreshInventory();

                e.Handled = true;
            }
        }

        private void InventorySlot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is InventoryItemViewModel itemViewModel
                && itemViewModel.Item != null)
            {
                DataObject data = new DataObject(typeof(InventoryItemViewModel), itemViewModel);
                DragDrop.DoDragDrop(border, data, DragDropEffects.Move);
                e.Handled = true;
            }
        }
    }
}
