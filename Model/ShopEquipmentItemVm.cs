using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Systems.Enums;
using Myria.Lib.Core.Systems;
using Myria.Wpf.ViewModel.UserControls.IngameWindow;

namespace Myria.Wpf.Model
{
    public class ShopEquipmentItemVm : ShopItemVm
    {
        public string SlotType { get; set; }
        public List<string> AllowedClasses { get; set; } = new();
        public bool IsTool { get; set; }

        public static ShopEquipmentItemVm FromEquipment(EquipmentItem item)
        {
            return new ShopEquipmentItemVm
            {
                Id = item.Id,
                Name = Localization.T($"item.{item.Id}"),
                BuyPrice = item.BuyPrice,
                MaxStackSize = item.MaxStackSize,
                SlotType = item.SlotType,
                AllowedClasses = item.AllowedClasses,
                IsTool = item.IsTool,
                ItemKind = item.IsTool ? ShopItemKind.Tool : ShopItemKind.Equipment
            };
        }
    }
}
