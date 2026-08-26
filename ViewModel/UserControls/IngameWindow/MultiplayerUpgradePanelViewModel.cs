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

            var result = await GameHubService.UpgradeAsync(_npc.Id, currentId);
            if (result == null || !result.Success)
            {
                StatusMessage = Localization.T("npc.upgrade.fail");
                return;
            }

            string matId  = SelectedEquipment.UpgradeMaterialId;
            int remaining = SelectedEquipment.UpgradeMaterialCount;
            foreach (var stack in _character.Inventory.Items.Where(i => i.Id == matId).ToList())
            {
                if (remaining <= 0) break;
                int take = Math.Min(stack.StackSize, remaining);
                stack.StackSize -= take;
                remaining -= take;
                if (stack.StackSize <= 0) _character.Inventory.RemoveItem(stack);
            }

            var eq = SelectedEquipment.Item;
            if (eq != null)
            {
                string jobId  = _npc.MasterJobId ?? "blacksmith";
                float quality = (float)JobXpService.GetSkillMultiplierFromXp(JobManager.GetOrAdd(_character, jobId).SkillXp);
                if (quality > eq.CraftQuality) eq.CraftQuality = quality;
                eq.TryUpgrade(_character);
            }

            if (!string.IsNullOrEmpty(result.JobId))
                JobManager.GrantSkillXp(_character, result.JobId, result.SkillXpGained);

            StatusMessage = Localization.T("npc.action.upgrade.ok", ResolveItemName(result.ItemId ?? ""), result.UpgradeLevel);
            LoadUpgradable(currentId);
        }
    }
}
