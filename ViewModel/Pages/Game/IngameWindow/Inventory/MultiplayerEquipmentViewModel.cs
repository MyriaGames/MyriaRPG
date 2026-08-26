using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Systems.Enums;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow.Inventory
{
    public class MultiplayerEquipmentViewModel : EquipmentViewModel
    {
        public MultiplayerEquipmentViewModel(Character character) : base(character) { }

        protected override void ExecuteEquip(EquipmentItem equipment)
        {
            _ = EquipAsync(equipment);
        }

        private async Task EquipAsync(EquipmentItem equipment)
        {
            var result = await GameHubService.EquipItemAsync(equipment.Id);
            if (result.Success) base.ExecuteEquip(equipment);
            else ShowNotification(result.Reason ?? "Could not equip item.");
        }

        protected override void ExecuteUnequip(EquipmentItem item, string slotType)
        {
            _ = UnequipAsync(item, slotType);
        }

        private async Task UnequipAsync(EquipmentItem item, string slotType)
        {
            bool ok = await GameHubService.UnequipItemAsync(slotType);
            if (ok) base.ExecuteUnequip(item, slotType);
        }
    }
}
