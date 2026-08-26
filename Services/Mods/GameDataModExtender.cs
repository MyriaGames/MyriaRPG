using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems.Mods;
using Myria.Wpf.ViewModel.Pages.Game;
using System.Text.Json.Nodes;
using System.Windows;

namespace Myria.Wpf.Services.Mods
{
    /// <summary>
    /// Reloads all gameplay data (items, monsters, NPCs, rooms, skills, etc.) whenever the
    /// active mod list changes — including when multiplayer mode is toggled on or off.
    /// This makes mod activation/deactivation hot-pluggable: no restart required.
    /// </summary>
    public sealed class GameDataModExtender : ModLoaderExtender
    {
        public override void AfterModsLoaded(ModLoadContext context)
        {
            // Any mod that is marked enabled in its manifest but failed validation
            // (i.e. has at least one non-optional warning) is automatically disabled so
            // that (a) the user's mod list reflects reality and (b) subsequent startups
            // don't attempt to activate mods that are known to be broken.
            AutoDisableBlockedMods();

            // Reload all data definitions. skipGameState=true preserves the running
            // day/time cycle and game-status file — only the data tables are refreshed.
            GameService.InitializeGame(null, skipGameState: true);

            // Reconnect any active character to the newly-created room/skill objects.
            Application.Current?.Dispatcher.BeginInvoke(ReconnectActiveSession);
        }

        private static void AutoDisableBlockedMods()
        {
            foreach (var mod in ModLoader.AllMods)
            {
                if (!mod.IsEnabled) continue;

                // A "blocking" warning is any warning that is NOT flagged as optional.
                bool hasBlocker = mod.Warnings.Any(w =>
                    !w.StartsWith("[Optional]", StringComparison.OrdinalIgnoreCase));

                if (!hasBlocker) continue;

                // Update in-memory state so the current session reflects the disabled status.
                mod.IsEnabled         = false;
                mod.Manifest.Enabled  = false;

                // Persist the change so the mod starts as disabled on the next launch.
                try { ModManifestStore.WriteProperty(mod, "enabled", JsonValue.Create(false)); }
                catch { /* best-effort — if the file is locked or read-only, skip silently */ }
            }
        }

        private static void ReconnectActiveSession()
        {
            var character = Myria.Lib.Core.Services.UserAccoundService.CurrentCharacter;
            if (character == null) return;

            // Room objects are recreated on each reload — update the character's reference.
            character.CurrentRoom = RoomService.AllRooms
                .FirstOrDefault(r => r.Id == character.CurrentRoomId)
                ?? character.CurrentRoom;

            // Reconnect learned skills to new skill definitions.
            SkillFactory.UpdateSkills(character);

            // Refresh the room page display so item/NPC changes appear immediately.
            ViewModel_PageRoom.Reload();
        }
    }
}
