using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Items;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory
{
    public class MultiplayerInventoryGridViewModel : InventoryGridViewModel
    {
        public MultiplayerInventoryGridViewModel(Character character) : base(character) { }

        protected override void ExecuteEquip(EquipmentItem equipment)
        {
            _ = EquipAsync(equipment);
        }

        private async Task EquipAsync(EquipmentItem equipment)
        {
            var result = await GameHubService.EquipItemAsync(equipment);
            // Success: the server's CharacterUpdated push (sent before this call returns) has
            // already applied the result - doing it again locally applied it twice.
            if (result.Success) RefreshInventory();
            else ShowNotification(result.Reason ?? "Could not equip item.");
        }

        protected override void ExecuteUse(ConsumableItem consumable)
        {
            _ = UseAsync(consumable);
        }

        private async Task UseAsync(ConsumableItem consumable)
        {
            bool ok = await GameHubService.UseItemAsync(consumable.Id);
            // Success: the server's CharacterUpdated push (sent before this call returns) has
            // already applied the result - doing it again locally applied it twice.
            if (ok) RefreshInventory();
        }

        protected override void SellAmount(InventoryItemViewModel itemViewModel, int amount)
        {
            if (itemViewModel?.Item == null || amount <= 0) return;
            _ = SellAsync(itemViewModel, amount);
        }

        private async Task SellAsync(InventoryItemViewModel itemViewModel, int amount)
        {
            var result = await GameHubService.SellItemToNpcAsync(itemViewModel.Item.Id, amount);
            // Success: the server's CharacterUpdated push (sent before this call returns) has
            // already applied the result - doing it again locally applied it twice.
            if (result != null && result.Success)
                RefreshInventory();
            else
                ShowNotification(result?.Reason ?? "Could not sell item.");
        }
    }
}
