using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public class MultiplayerCraftPanelViewModel : CraftPanelViewModel
    {
        public MultiplayerCraftPanelViewModel(Npc npc, Character character, Action goBack)
            : base(npc, character, goBack) { }

        protected override async Task ExecuteCraft()
        {
            var result = await GameHubService.CraftAsync(_npc.Id, SelectedRecipe.Id, Quantity);
            if (result == null || !result.Success)
            {
                StatusMessage = result?.Reason switch
                {
                    "missing_ingredients" => Localization.T("npc.craft.notEnoughMaterials"),
                    "unknown_recipe"      => Localization.T("npc.craft.fail"),
                    _                     => Localization.T("npc.craft.fail")
                };
                return;
            }

            foreach (var ing in SelectedRecipe.Ingredients)
            {
                int remaining = ing.Amount * Quantity;
                foreach (var stack in _character.Inventory.Items.Where(i => i.Id == ing.Id).ToList())
                {
                    if (remaining <= 0) break;
                    int take = Math.Min(stack.StackSize, remaining);
                    stack.StackSize -= take;
                    remaining -= take;
                    if (stack.StackSize <= 0) _character.Inventory.RemoveItem(stack);
                }
            }

            if (result.ItemId != null && ItemFactory.TryCreateItem(result.ItemId, out var output))
            {
                output.StackSize = result.Amount;
                _character.Inventory.AddItem(output, _character);
            }
            if (!string.IsNullOrEmpty(result.JobId))
                JobManager.GrantSkillXp(_character, result.JobId, result.SkillXpGained);

            StatusMessage = Localization.T("npc.craft.success", Quantity, SelectedRecipe.Name);
            UpdateMaxCraftable();
            Quantity = 1;
        }
    }
}
