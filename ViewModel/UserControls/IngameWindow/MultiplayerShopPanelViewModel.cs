using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Items;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Models.Dto;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Model;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public class MultiplayerShopPanelViewModel : ShopPanelViewModel
    {
        public MultiplayerShopPanelViewModel(Npc npc, Character character, Action goBack)
            : base(npc, character, goBack) { }

        // ── Stock loading ─────────────────────────────────────────────────────

        protected override void LoadShopStock()
        {
            // Clear any stock the base may have started to build with local (possibly modded)
            // NPC data, then repopulate from the server's authoritative item list.
            Stock.Clear();
            SelectedStock = null;
            _ = LoadStockFromServerAsync();
        }

        private async Task LoadStockFromServerAsync()
        {
            var itemIds = await GameHubService.GetNpcShopItemsAsync(_npc.Id);
            if (itemIds == null || itemIds.Count == 0)
            {
                StatusMessage = Localization.T("npc.shop.noStock");
                return;
            }

            Stock.Clear();
            foreach (var itemId in itemIds)
            {
                if (!ItemFactory.TryCreateItem(itemId, out var item) || item == null) continue;

                if (item is EquipmentItem eq)
                {
                    bool allowed = eq.IsTool
                        || eq.AllowedClasses.Count == 0
                        || eq.AllowedClasses.Contains(_character.Class);
                    if (allowed)
                        Stock.Add(ShopEquipmentItemVm.FromEquipment(eq));
                }
                else
                {
                    bool allowed = item.AllowedClasses.Count == 0
                        || item.AllowedClasses.Contains(_character.Class);
                    if (allowed)
                        Stock.Add(ShopItemVm.FromItem(item));
                }
            }

            SelectedStock = FilteredStock.FirstOrDefault();
            if (SelectedStock == null)
                StatusMessage = Localization.T("npc.shop.noStock");
        }

        // ── Purchase ──────────────────────────────────────────────────────────

        protected override async Task ExecuteBuy(ShopItemVm item, int quantity)
        {
            var result = await GameHubService.BuyFromNpcShopAsync(_npc.Id, item.Id, quantity);

            if (result == null || !result.Success)
            {
                StatusMessage = result?.Reason switch
                {
                    "not_enough_money" => Localization.T("npc.shop.notEnoughMoney"),
                    "inventory_full"   => Localization.T("npc.shop.inventoryFull"),
                    "wrong_class"      => Localization.T("pg.inventory.wrong_class"),
                    _                  => Localization.T("npc.shop.buyFailed")
                };
                return;
            }

            if (ItemFactory.TryCreateItem(item.Id, out var purchased) && purchased != null)
            {
                purchased.StackSize = result.Quantity;
                _character.Inventory.AddItem(purchased, _character);
            }
            _character.Money.TrySpend(result.TotalCost);
            CharacterMoney = (int)_character.Money.Balance.BronzeTotal;
            StatusMessage = Localization.T("npc.shop.buySuccess", result.Quantity, item.Name);
            BuyQuantity = 1;
            OnPropertyChanged(nameof(CanBuy));
        }
    }
}
