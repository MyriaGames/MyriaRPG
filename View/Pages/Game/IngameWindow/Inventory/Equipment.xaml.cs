using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Services;
using Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Myria.Wpf.View.Pages.Game.IngameWindow.Inventory
{
    public partial class Equipment : Page
    {
        private EquipmentViewModel _viewModel;

        public Equipment()
        {
            InitializeComponent();
            var ch = UserAccountService.CurrentCharacter;
            _viewModel = GameHubService.IsConnected
                ? (EquipmentViewModel)new MultiplayerEquipmentViewModel(ch)
                : new EquipmentViewModel(ch);
            this.DataContext = _viewModel;
        }

        private void EquipmentSlot_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(InventoryItemViewModel)))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void EquipmentSlot_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(InventoryItemViewModel)) is not InventoryItemViewModel itemViewModel) return;
            if (sender is not ContentControl control) return;

            // EquipmentType is now a string-constant set, not an enum — null is the natural
            // "no match" sentinel instead of the old (EquipmentType)(-1) int cast.
            string? slotType = control.Name switch
            {
                "WeaponSlot"    => EquipmentType.Weapon,
                "ArmorSlot"     => EquipmentType.Armor,
                "AccessorySlot" => EquipmentType.Accessory,
                _               => null
            };

            if (slotType == null) return;

            _viewModel.HandleEquipmentDrop(itemViewModel, slotType);
            e.Handled = true;
        }

        private void EquipmentSlot_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is ContentControl control && control.DataContext is EquipmentSlotViewModel slotViewModel
                && slotViewModel.Item != null)
            {
                ItemTooltipPopup.PlacementTarget = control;
                bool flipLeft = control.Name == "AccessorySlot";
                ItemTooltipPopup.CustomPopupPlacementCallback = (popupSize, targetSize, _) =>
                {
                    double x = flipLeft ? -popupSize.Width : targetSize.Width;
                    return new[] { new CustomPopupPlacement(new Point(x, 0), PopupPrimaryAxis.None) };
                };
                var tempItem = new InventoryItemViewModel(slotViewModel.Item, 0);
                _viewModel.ShowTooltipCommand.Execute(tempItem);
            }
        }

        private void EquipmentSlot_MouseLeave(object sender, MouseEventArgs e)
        {
            _viewModel.HideTooltipCommand.Execute(null);
        }

        private void EquipmentSlot_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ContentControl control) return;
            if (control.DataContext is not EquipmentSlotViewModel slotViewModel || slotViewModel.Item == null) return;

            var menu = new ContextMenu();
            var unequipItem = new MenuItem { Header = Myria.Lib.Core.Systems.Localization.T("pg.inventory.slot.unequip") };
            unequipItem.Click += (s, _) => _viewModel.UnequipItemCommand.Execute(slotViewModel);
            menu.Items.Add(unequipItem);

            menu.PlacementTarget = control;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private void EquipmentSlot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ContentControl control && control.DataContext is EquipmentSlotViewModel slotViewModel
                && slotViewModel.Item != null)
            {
                var tempItem = new InventoryItemViewModel(slotViewModel.Item, 0);
                DataObject data = new DataObject(typeof(InventoryItemViewModel), tempItem);
                DragDrop.DoDragDrop(control, data, DragDropEffects.Move);
                e.Handled = true;
            }
        }
    }
}
