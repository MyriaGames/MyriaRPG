using Myria.Lib.Core.Services;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class MultiplayerSkillCombinationViewModel : SkillCombinationViewModel
    {
        protected override void Combine()
        {
            var ids = InputSlots.Where(s => s.IsSet).Select(s => s.SkillId!).ToList();
            if (ids.Count < 2) return;
            _ = CombineAsync(ids);
        }

        private async Task CombineAsync(List<string> ids)
        {
            bool ok = await GameHubService.CombineSkillsAsync(ids);
            if (!ok)
            {
                StatusText = Localization.T("pg.skill_combo.duplicate");
                return;
            }

            var character = UserAccountService.CurrentCharacter;
            var result = SkillCombinationService.TryCreateForCharacter(character, ids);
            if (result == null) return; // server confirmed but local character already had it — ignore

            CombinedSkills.Add(new CombinedSkillVm(result, character.Skills));
            foreach (var slot in InputSlots) slot.Clear();
            RaiseInputChanged();
            OnPropertyChanged(nameof(HeaderText));
            StatusText = Localization.T("pg.skill_combo.success", result.DisplayName);
        }
    }
}
