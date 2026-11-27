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
        private readonly bool _viaJobPage;

        /// <param name="viaJobPage">Crafting from the Jobs page (GameHub.CraftFromJob, active
        /// job's NPC resolved by the server) instead of at the NPC itself (GameHub.Craft).</param>
        public MultiplayerCraftPanelViewModel(Npc npc, Character character, Action goBack, bool viaJobPage = false)
            : base(npc, character, goBack)
        {
            _viaJobPage = viaJobPage;
        }

        protected override async Task ExecuteCraft()
        {
            var result = _viaJobPage
                ? await GameHubService.CraftFromJobAsync(SelectedRecipe.Id, Quantity)
                : await GameHubService.CraftAsync(_npc.Id, SelectedRecipe.Id, Quantity);
            if (result == null || !result.Success)
            {
                StatusMessage = result?.Reason switch
                {
                    "missing_ingredients"      => Localization.T("npc.craft.notEnoughMaterials"),
                    "inventory_full"           => Localization.T("npc.craft.inventoryFull"),
                    "job_crafting_not_allowed" => Localization.T("pg.jobs.craft.not_allowed"),
                    _                          => Localization.T("npc.craft.fail")
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
