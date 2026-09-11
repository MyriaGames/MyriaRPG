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

            // Same reasoning as the singleplayer base's Combine(): a bare server rejection here
            // used to always get reported as "already exists" even when the real reason was that
            // this skill set doesn't form any combination at all. Checking that client-side first
            // (resolve-only, matches what the base class and Console's own preview already do)
            // means the server round-trip is only ever asked about combinations that could
            // legitimately succeed, so a server "no" left over after that check is honestly a
            // duplicate (or another server-side rejection - GameHub.CombineSkills only returns a
            // bare bool too, so that's still not fully distinguishable, but far less likely once
            // input count and recipe validity are already confirmed client-side).
            if (SkillCombinationService.Combine(ids) == null)
            {
                StatusText = Localization.T("pg.skill_combo.no_recipe");
                return;
            }

            _ = CombineAsync(ids);
        }

        private async Task CombineAsync(List<string> ids)
        {
            // null means the server was never actually reached (not connected, call threw/timed
            // out) - not "the server said no". The two used to be shown identically as "this
            // combination already exists", which was the main source of "unrelated errors get
            // shown as already exists": a dropped connection right before this call (see the
            // GameHub.LoadCharacter session-establishment race fixed for skill slots) produced
            // exactly the same message as a genuine duplicate, with nothing about the real cause.
            bool? ok = await GameHubService.CombineSkillsAsync(ids);
            if (ok is null)
            {
                StatusText = Localization.T("pg.skill_combo.not_connected");
                return;
            }
            if (!ok.Value)
            {
                StatusText = Localization.T("pg.skill_combo.duplicate");
                return;
            }

            var character = UserAccountService.CurrentCharacter;
            var result = SkillCombinationService.TryCreateForCharacter(character, ids);
            if (result == null)
            {
                // The server just confirmed this combination now exists, but mirroring it into
                // this client's own local CombinedSkills list hit the same duplicate check the
                // server just passed - meaning the local list must already have it (a prior
                // desync that's now self-corrected, or a leftover retry). There's nothing new to
                // add to the displayed list, but still confirm success and clear the inputs
                // instead of silently doing nothing, which otherwise looks exactly like "the
                // combination didn't happen" even though the server just said it did.
                foreach (var slot in InputSlots) slot.Clear();
                RaiseInputChanged();
                StatusText = Localization.T("pg.skill_combo.success", string.Join(" + ", ids));
                return;
            }

            CombinedSkills.Add(new CombinedSkillVm(result, character.Skills));
            foreach (var slot in InputSlots) slot.Clear();
            RaiseInputChanged();
            OnPropertyChanged(nameof(HeaderText));
            StatusText = Localization.T("pg.skill_combo.success", result.DisplayName);
        }
    }
}
