using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.UserControls.IngameWindow
{
    public class MultiplayerUpgradePanelViewModel : UpgradePanelViewModel
    {
        public MultiplayerUpgradePanelViewModel(Npc npc, Character character, Action goBack)
            : base(npc, character, goBack) { }

        protected override async Task ExecuteUpgrade()
        {
            if (SelectedEquipment == null) return;
            string currentId = SelectedEquipment.Id;
            var item = SelectedEquipment.Item;

            var result = item is not null
                ? await GameHubService.UpgradeAsync(_npc.Id, item)
                : await GameHubService.UpgradeAsync(_npc.Id, currentId);
            if (result == null || !result.Success)
            {
                StatusMessage = Localization.T("npc.upgrade.fail");
                return;
            }

            // The server's CharacterUpdated push (sent before this call returns) already brought the
            // inventory and job XP up to date - applying the result again here did it twice.

            StatusMessage = Localization.T("npc.action.upgrade.ok", ResolveItemName(result.ItemId ?? ""), result.UpgradeLevel);
            LoadUpgradable(currentId);
        }
    }
}
