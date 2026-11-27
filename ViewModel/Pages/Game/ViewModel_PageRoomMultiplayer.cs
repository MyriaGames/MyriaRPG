using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Services;

namespace Myria.Wpf.ViewModel.Pages.Game
{
    public class ViewModel_PageRoomMultiplayer : ViewModel_PageRoom
    {
        protected override async Task ExecuteMove(Room targetRoom)
        {
            if (!MeetsRoomRequirement(targetRoom)) return;
            bool ok = await GameHubService.JoinRoomAsync(targetRoom.Id);
            if (!ok)   // requirements were met, so it's the dungeon run (or e.g. a party requirement)
                WriteLog(Localization.T(targetRoom.IsDungeonRoom ? "dungeon.locked" : "room.locked"));
            if (ok)
            {
                NoteDungeonExit(currentRoom, targetRoom);
                ApplyRoomMove(targetRoom);
                Myria.Lib.Core.Systems.GameEvents.FireRoomEntered(character, targetRoom);
            }
        }

        protected override Task ExecuteGather() => StartGatheringServerAsync();

        // v0.3 dungeons: the run lives on the server (per party) - ask it what's left here.
        private int _dungeonVersion;

        protected override void RefreshDungeonState()
        {
            int version = ++_dungeonVersion;
            if (!currentRoom.IsDungeonRoom) { DungeonStatus = string.Empty; return; }
            _ = RefreshDungeonStateAsync(version, currentRoom.Id);
        }

        private async Task RefreshDungeonStateAsync(int version, int roomId)
        {
            var state = await GameHubService.GetDungeonStateAsync();
            if (version != _dungeonVersion || state is null || state.RoomId != roomId) return;
            HasMonsters = !state.Cleared;
            DungeonStatus = DungeonStatusText(state.Remaining.Select(m => m.Name));
        }

        protected override void StartFight()
        {
            _ = StartFightServerAsync(soloOnly: true);
        }

        protected override void StartGroupFightLocal()
        {
            _ = StartFightServerAsync(soloOnly: false);
        }

        // ── PvP arena (v0.3) ─────────────────────────────────────────────────

        private static readonly HashSet<string> KnownPvpReasons = new()
        {
            "not_pvp_room", "target_not_here", "target_busy", "same_party", "already_in_combat", "player_dead",
        };

        protected override void OnRoomFlagsRefreshed()
        {
            IsPvpRoom = currentRoom.IsPvpRoom && GameHubService.IsConnected;
            if (IsPvpRoom) _ = RefreshPvpAsync();
            else { PvpTargets.Clear(); OnPropertyChanged(nameof(HasNoPvpTargets)); }
        }

        protected override async Task RefreshPvpAsync()
        {
            PvpStatsText = Localization.T("pvp.stats", character.PvpRating, character.PvpCurrency, character.PvpWins, character.PvpLosses);
            var targets = await GameHubService.GetPvpTargetsAsync();
            PvpTargets.Clear();
            foreach (var t in targets)
                PvpTargets.Add(new Model.PvpTargetVm(t, (name, party) => _ = ChallengeAsync(name, party)));
            OnPropertyChanged(nameof(HasNoPvpTargets));
        }

        private async Task ChallengeAsync(string targetName, bool partyFight)
        {
            var result = await GameHubService.StartPvpFightAsync(targetName, partyFight);
            if (result?.Success == true) return; // PvpStarted pulls every participant into the fight page

            string reason = result?.Reason is { } r && KnownPvpReasons.Contains(r) ? r : "failed";
            ViewModel_PageRoom.WriteLog(Localization.T("pvp.reason." + reason));
            await RefreshPvpAsync();
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
                case "room_cleared":
                    ViewModel_PageRoom.WriteLog(Localization.T("dungeon.room_cleared"));
                    RefreshDungeonState();
                    break;
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
