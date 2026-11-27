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

            // The server's CharacterUpdated push (sent before this call returns) already brought the
            // inventory and job XP up to date - applying the result again here did it twice.

            StatusMessage = Localization.T("npc.craft.success", Quantity, SelectedRecipe.Name);
            UpdateMaxCraftable();
            Quantity = 1;
        }
    }
}
