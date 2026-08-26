using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Services;

namespace Myria.Wpf.Services
{
    internal static class GuildPropertyCache
    {
        public static ServerApiService.GuildPropertyInfo? Current { get; private set; }

        public static async Task RefreshAsync()
        {
            Current = await ServerApiService.GetGuildPropertyAsync();
            RegisterRooms();
        }

        public static void Set(ServerApiService.GuildPropertyInfo? info)
        {
            Current = info;
            RegisterRooms();
        }

        public static bool IsEntryRoom(int roomId)
        {
            if (Current is null) return false;
            if (Current.Type == "House" && Current.CityRoomId == roomId) return true;
            if (Current.Type == "Base"  && Current.BaseAnchorRoomId == roomId) return true;
            return false;
        }

        public static int? GetFirstBuiltRoomId()
        {
            return Current?.Rooms.FirstOrDefault(r => r.IsBuilt)?.RoomId;
        }

        /// <summary>
        /// Creates <see cref="Room"/> objects for every built guild room and injects them
        /// into <see cref="RoomService.AllRooms"/> so normal navigation can find them.
        /// Rooms are chained linearly (north/south) with the last "south" exit leading
        /// back to the property entry room (city room for a house, anchor room for a base).
        /// </summary>
        public static void RegisterRooms()
        {
            if (Current is null) return;

            int entryRoomId = Current.Type == "House"
                ? (Current.CityRoomId ?? 0)
                : (Current.BaseAnchorRoomId ?? 0);

            var entryRoom = RoomService.GetRoomById(entryRoomId);

            var builtInfos = Current.Rooms
                .Where(r => r.IsBuilt)
                .OrderBy(r => r.RoomId)
                .ToList();

            // Create any rooms that aren't registered yet
            foreach (var info in builtInfos)
            {
                if (RoomService.GetRoomById(info.RoomId) != null) continue;

                var room = new Room(info.Name, info.Description)
                {
                    Id        = info.RoomId,
                    IsLiteral = true,
                };
                RoomService.AllRooms.Add(room);
            }

            // Resolve Room objects and wire exits (linear chain, south → entry at the bottom)
            var guildRooms = builtInfos
                .Select(r => RoomService.GetRoomById(r.RoomId))
                .Where(r => r != null)
                .ToList();

            for (int i = 0; i < guildRooms.Count; i++)
            {
                var room = guildRooms[i]!;
                room.Exits.Clear();
                room.ExitIds.Clear();

                // South: back toward entry (previous room, or the entry itself for room 0)
                if (i == 0)
                {
                    if (entryRoom != null)
                    {
                        room.Exits["south"]   = entryRoom;
                        room.ExitIds["south"] = entryRoomId;
                    }
                }
                else
                {
                    room.Exits["south"]   = guildRooms[i - 1]!;
                    room.ExitIds["south"] = builtInfos[i - 1].RoomId;
                }

                // North: deeper into the guild (next room in the chain)
                if (i < guildRooms.Count - 1)
                {
                    room.Exits["north"]   = guildRooms[i + 1]!;
                    room.ExitIds["north"] = builtInfos[i + 1].RoomId;
                }
            }
        }
    }
}
