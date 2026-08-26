using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game
{
    public class ViewModel_PageRoomMultiplayer : ViewModel_PageRoom
    {
        protected override async Task ExecuteMove(Room targetRoom)
        {
            bool ok = await GameHubService.JoinRoomAsync(targetRoom.Id);
            if (ok)
            {
                ApplyRoomMove(targetRoom);
                Myria.Lib.Core.Systems.GameEvents.FireRoomEntered(character, targetRoom);
            }
        }

        protected override Task ExecuteGather() => StartGatheringServerAsync();

        protected override void StartFight()
        {
            _ = StartFightServerAsync(soloOnly: true);
        }

        protected override void StartGroupFightLocal()
        {
            _ = StartFightServerAsync(soloOnly: false);
        }

        private async Task StartFightServerAsync(bool soloOnly)
        {
            var result = await GameHubService.StartGroupCombatAsync(character.CurrentRoom.Id, soloOnly);
            if (result != null && result.Success)
                return; // GroupCombatStarted hub event handles navigation for all party members

            // Do NOT fall back to base.StartGroupFightLocal() here — that starts a fake,
            // client-only solo encounter completely disconnected from the real party/server
            // state. That silent fallback masked every server-side failure (including a party
            // member's own attempt to join an already-active party fight) behind what looked
            // like a working fight, which is why fixing the server side alone didn't help.
            switch (result?.Reason)
            {
                case "no_monsters":
                case "already_in_combat":
                    break; // the server already sent a ChatMessage explaining why
                case "player_dead":
                    ViewModel_PageRoom.WriteLog(Localization.T("pg.room.group_fight.player_dead"));
                    break;
                default:
                    ViewModel_PageRoom.WriteLog(Localization.T("pg.room.group_fight.failed"));
                    break;
            }
        }
    }
}
