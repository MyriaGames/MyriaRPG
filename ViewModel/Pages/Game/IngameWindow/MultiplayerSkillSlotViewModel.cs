using Myria.Lib.Core.Services;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class MultiplayerSkillSlotViewModel : SkillSlotViewModel
    {
        // All three actions below apply the change to the local character and refresh the UI
        // immediately (optimistically), then confirm with the server in the background - only
        // rolling back if the server actually rejects it. Waiting for the round-trip before
        // updating locally (the old behavior) meant that a slow or dropped response left the UI
        // silently stuck on the old state until something else (e.g. a reconnect) forced a fresh
        // reload from the server, even though the server had already applied the change.

        protected override void SlotSkill(SlottableSkillVm? vm)
        {
            if (vm == null) return;
            if (!SkillSlotService.TryAddSlot(_player, vm.Source, vm.SkillId)) return;
            Refresh();
            _ = ConfirmSlotAsync(vm);
        }

        private async Task ConfirmSlotAsync(SlottableSkillVm vm)
        {
            bool ok = await GameHubService.SlotSkillAsync(vm.Source.ToString(), vm.SkillId);
            if (!ok)
            {
                SkillSlotService.RemoveSlot(_player, vm.Source, vm.SkillId);
                Refresh();
            }
        }

        protected override void UnslotSkill(ActiveSlotVm? vm)
        {
            if (vm == null) return;
            if (!SkillSlotService.RemoveSlot(_player, vm.Source, vm.SkillId)) return;
            Refresh();
            _ = ConfirmUnslotAsync(vm);
        }

        private async Task ConfirmUnslotAsync(ActiveSlotVm vm)
        {
            bool ok = await GameHubService.UnslotSkillAsync(vm.Source.ToString(), vm.SkillId);
            if (!ok)
            {
                SkillSlotService.TryAddSlot(_player, vm.Source, vm.SkillId);
                Refresh();
            }
        }

        protected override void MoveUp(ActiveSlotVm? vm)
        {
            if (vm == null) return;
            int idx = _player.SkillSlots.FindIndex(s => s.Source == vm.Source && s.SkillId == vm.SkillId);
            ReorderAndConfirm(idx, idx - 1);
        }

        protected override void MoveDown(ActiveSlotVm? vm)
        {
            if (vm == null) return;
            int idx = _player.SkillSlots.FindIndex(s => s.Source == vm.Source && s.SkillId == vm.SkillId);
            ReorderAndConfirm(idx, idx + 1);
        }

        private void ReorderAndConfirm(int fromIndex, int toIndex)
        {
            SkillSlotService.ReorderSlots(_player, fromIndex, toIndex);
            Refresh();
            _ = ConfirmReorderAsync(fromIndex, toIndex);
        }

        private async Task ConfirmReorderAsync(int fromIndex, int toIndex)
        {
            bool ok = await GameHubService.ReorderSkillSlotAsync(fromIndex, toIndex);
            if (!ok)
            {
                // Server rejected the move (e.g. stale indices) - undo the optimistic reorder.
                SkillSlotService.ReorderSlots(_player, toIndex, fromIndex);
                Refresh();
            }
        }
    }
}
