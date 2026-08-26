using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Regestries;
using Myria.Lib.Core.Systems;
using Myria.Wpf.Systems.MapNode;
using Myria.Wpf.Utils;
using System.Windows.Input;

namespace Myria.Wpf.ViewModel.Pages.Game.IngameWindow
{
    public class ViewModel_PageLocalMap : BaseViewModel
    {
        // Layout constants
        private const double STEP_X   = 160;
        private const double STEP_Y   = 100;
        private const double NODE_W   = 120;
        private const double NODE_H   = 40;
        private const double GROUP_W  = 150;
        private const double GROUP_H  = 50;
        private const double PADDING  = 30;

        // A zone with â‰¥ this many rooms is shown collapsed on the world map
        private const int GROUP_THRESHOLD = 4;

        private Room _currentRoom = null!;
        private IReadOnlyList<ZoneInfo> _bigZones = [];

        // Tracks the view currently on screen and the views drilled through to reach it, so
        // "Back" steps to the previous map instead of always jumping to the player's own area.
        private ViewState _currentView = new(IsWorld: true, BfsStart: null, Zone: null);
        private readonly Stack<ViewState> _history = new();

        private sealed record ViewState(bool IsWorld, Room? BfsStart, ZoneInfo? Zone);

        public string MapTitle { get; private set; } = "";
        public double CanvasWidth  { get; private set; }
        public double CanvasHeight { get; private set; }

        public IReadOnlyList<MapNodeVm> Nodes { get; private set; } = [];
        public IReadOnlyList<MapEdgeVm> Edges { get; private set; } = [];

        public ICommand GroupNodeClickCommand { get; }
        public ICommand BackMapCommand { get; }
        public ICommand WorldMapCommand { get; }

        public ViewModel_PageLocalMap(Room currentRoom)
        {
            GroupNodeClickCommand = new RelayCommand<MapNodeVm>(OnGroupNodeClicked);
            BackMapCommand = new RelayCommand(BackToCurrentRoomMap);
            WorldMapCommand = new RelayCommand(OpenWorldMap);
            _bigZones = GetBigZones();
            Build(currentRoom);
        }

        // ---------- Entry point ------------------------------------------------------------------------------------------------------------------------------

        private void Build(Room currentRoom)
        {
            _currentRoom = currentRoom;
            var currentZone = _bigZones.FirstOrDefault(z => z.RoomIds.Contains(currentRoom.Id));

            if (currentZone != null)
                ShowGroupView(currentRoom, currentZone, pushHistory: false);
            else
                ShowWorldView(pushHistory: false);
        }

        private void ShowGroupView(Room bfsStart, ZoneInfo zone, bool pushHistory)
        {
            if (pushHistory) _history.Push(_currentView);
            BuildGroupView(bfsStart, zone, _bigZones);
            _currentView = new ViewState(false, bfsStart, zone);
            NotifyMapChanged();
        }

        private void ShowWorldView(bool pushHistory)
        {
            if (pushHistory) _history.Push(_currentView);
            BuildWorldView(_currentRoom, _bigZones);
            _currentView = new ViewState(true, null, null);
            NotifyMapChanged();
        }

        private void NotifyMapChanged()
        {
            OnPropertyChanged(nameof(MapTitle));
            OnPropertyChanged(nameof(Nodes));
            OnPropertyChanged(nameof(Edges));
            OnPropertyChanged(nameof(CanvasWidth));
            OnPropertyChanged(nameof(CanvasHeight));
        }

        private void OnGroupNodeClicked(MapNodeVm node)
        {
            var zone = _bigZones.FirstOrDefault(z => z.Id == node.ZoneId);
            if (zone == null) return;

            Room bfsStart;
            if (zone.RoomIds.Contains(_currentRoom.Id))
                bfsStart = _currentRoom;
            else
            {
                bfsStart = RoomService.GetRoomById(zone.AnchorRoomId);
                if (bfsStart == null) return;
            }

            ShowGroupView(bfsStart, zone, pushHistory: true);
        }

        // ----- Zone list -------------------------------------------------------------------------------------------------------------------------------------

        private void BackToCurrentRoomMap()
        {
            if (_history.Count > 0)
            {
                var previous = _history.Pop();
                if (previous.IsWorld)
                    ShowWorldView(pushHistory: false);
                else
                    ShowGroupView(previous.BfsStart!, previous.Zone!, pushHistory: false);
                return;
            }

            Build(_currentRoom);
        }

        private void OpenWorldMap()
        {
            ShowWorldView(pushHistory: true);
        }

        private record ZoneInfo(string Id, string DisplayName, List<int> RoomIds, NodeKind Kind,
                                int AnchorRoomId = 0, bool IsDungeon = false,
                                bool ExpandWorldMapWidth = false, bool ExpandWorldMapHeight = false);

        private record ZoneLayout(int MinX, int MinY, int MaxX, int MaxY,
                                  bool ExpandWidth, bool ExpandHeight,
                                  Dictionary<int, (int x, int y)> RoomOffsets);

        private static IReadOnlyList<ZoneInfo> GetBigZones()
        {
            var zones = new List<ZoneInfo>();

            foreach (var c in CityRegistry.GetAll().Where(c => c.RoomIds.Count >= GROUP_THRESHOLD))
            {
                var expand = GetWorldMapExpansion(c);
                zones.Add(new ZoneInfo(c.Id, Localization.T(c.Name), c.RoomIds, NodeKind.City,
                                       c.AnchorRoomId, ExpandWorldMapWidth: expand.Width,
                                       ExpandWorldMapHeight: expand.Height));
            }

            foreach (var c in CaveRegistry.GetAll().Where(c => c.RoomIds.Count >= GROUP_THRESHOLD))
            {
                var expand = GetWorldMapExpansion(c);
                zones.Add(new ZoneInfo(c.Id, Localization.T(c.Name), c.RoomIds, NodeKind.Cave,
                                       c.AnchorRoomId, ExpandWorldMapWidth: expand.Width,
                                       ExpandWorldMapHeight: expand.Height));
            }

            foreach (var f in ForestRegistry.GetAll().Where(f => f.RoomIds.Count >= GROUP_THRESHOLD))
            {
                var expand = GetWorldMapExpansion(f);
                zones.Add(new ZoneInfo(f.Id, Localization.T(f.Name), f.RoomIds, NodeKind.Forest,
                                       f.AnchorRoomId, ExpandWorldMapWidth: expand.Width,
                                       ExpandWorldMapHeight: expand.Height));
            }

            foreach (var d in DungeonRegistry.GetAll())
            {
                var expand = GetWorldMapExpansion(d);
                zones.Add(new ZoneInfo(d.Id, Localization.T(d.Name), d.RoomIds, NodeKind.Dungeon,
                                       d.AnchorRoomId, IsDungeon: true, ExpandWorldMapWidth: expand.Width,
                                       ExpandWorldMapHeight: expand.Height));
            }

            return zones;
        }

        // ----- Group view ---- show only rooms inside the zone the player is currently in ----

        private void BuildGroupView(Room bfsRoom, ZoneInfo zone, IReadOnlyList<ZoneInfo> allZones)
        {
            MapTitle = zone.DisplayName;

            var zoneRoomIds = new HashSet<int>(zone.RoomIds);
            var positions = BuildZoneBfs(bfsRoom, zoneRoomIds);
            if (positions.Count == 0) return;

            int minX = positions.Values.Min(p => p.x);
            int minY = positions.Values.Min(p => p.y);
            int maxX = positions.Values.Max(p => p.x);
            int maxY = positions.Values.Max(p => p.y);

            // Build regular zone nodes
            var nodeMap = new Dictionary<int, MapNodeVm>();
            foreach (var (room, (gx, gy)) in positions)
            {
                double cx = (gx - minX) * STEP_X + STEP_X / 2 + PADDING;
                double cy = (gy - minY) * STEP_Y + STEP_Y / 2 + PADDING;

                var kind = room.IsBossRoom ? NodeKind.Boss : zone.Kind;

                nodeMap[room.Id] = new MapNodeVm
                {
                    RoomId     = room.Id,
                    Label      = Localization.T(room.Name),
                    X          = cx - NODE_W / 2,
                    Y          = cy - NODE_H / 2,
                    CenterX    = cx,
                    CenterY    = cy,
                    Width      = NODE_W,
                    Height     = NODE_H,
                    IsCurrent  = room.Id == _currentRoom.Id,
                    Kind       = kind,
                    NpcTooltip = room.NpcRefs.Count > 0
                        ? string.Join(", ", room.NpcRefs.Where(n => n != null).Select(n => Localization.T(n.NameKey)))
                        : ""
                };
            }

            // Build edges between zone rooms
            var edges = new List<MapEdgeVm>();
            var seen  = new HashSet<(int, int)>();
            foreach (var (room, _) in positions)
            {
                if (!nodeMap.TryGetValue(room.Id, out var fromNode)) continue;
                foreach (var (_, targetId) in room.ExitIds)
                {
                    if (!nodeMap.TryGetValue(targetId, out var toNode)) continue;
                    var key = (Math.Min(room.Id, targetId), Math.Max(room.Id, targetId));
                    if (!seen.Add(key)) continue;
                    edges.Add(new MapEdgeVm
                    {
                        X1 = fromNode.CenterX, Y1 = fromNode.CenterY,
                        X2 = toNode.CenterX,   Y2 = toNode.CenterY
                    });
                }
            }

            // Add adjacent exits outside this zone. Grouped targets are shown as group nodes;
            // ungrouped targets are shown as standalone room nodes.
            var adjacentZoneSeen = new HashSet<string>();
            var adjacentRoomSeen = new HashSet<int>();
            foreach (var (room, _) in positions)
            {
                if (!nodeMap.TryGetValue(room.Id, out var fromNode)) continue;
                foreach (var (dir, targetId) in room.ExitIds)
                {
                    if (zoneRoomIds.Contains(targetId)) continue;
                    var adjZone = allZones.FirstOrDefault(z => z.RoomIds.Contains(targetId));

                    int dx = dir.ToLower() switch { "east" => 1, "west" => -1, _ => 0 };
                    int dy = dir.ToLower() switch { "south" => 1, "north" => -1, _ => 0 };

                    double cx = fromNode.CenterX + dx * STEP_X;
                    double cy = fromNode.CenterY + dy * STEP_Y;

                    MapNodeVm exitNode;
                    int nodeKey;

                    if (adjZone != null)
                    {
                        if (!adjacentZoneSeen.Add(adjZone.Id)) continue;
                        exitNode = new MapNodeVm
                        {
                            RoomId      = -1,
                            Label       = adjZone.DisplayName,
                            X           = cx - GROUP_W / 2,
                            Y           = cy - GROUP_H / 2,
                            CenterX     = cx,
                            CenterY     = cy,
                            Width       = GROUP_W,
                            Height      = GROUP_H,
                            IsCurrent   = false,
                            Kind        = adjZone.Kind,
                            IsGroupNode = true,
                            ZoneId      = adjZone.Id
                        };

                        nodeKey = -(adjZone.GetHashCode() & 0x7FFFFFFF) - 1;
                    }
                    else
                    {
                        if (!adjacentRoomSeen.Add(targetId)) continue;
                        var targetRoom = RoomService.GetRoomById(targetId);
                        if (targetRoom == null) continue;

                        exitNode = new MapNodeVm
                        {
                            RoomId     = targetRoom.Id,
                            Label      = Localization.T(targetRoom.Name),
                            X          = cx - NODE_W / 2,
                            Y          = cy - NODE_H / 2,
                            CenterX    = cx,
                            CenterY    = cy,
                            Width      = NODE_W,
                            Height     = NODE_H,
                            IsCurrent  = targetRoom.Id == _currentRoom.Id,
                            Kind       = GetRoomKind(targetRoom),
                            NpcTooltip = targetRoom.NpcRefs.Count > 0
                                ? string.Join(", ", targetRoom.NpcRefs.Where(n => n != null).Select(n => Localization.T(n.NameKey)))
                                : ""
                        };

                        nodeKey = targetRoom.Id;
                    }

                    nodeMap[nodeKey] = exitNode;

                    edges.Add(new MapEdgeVm
                    {
                        X1 = fromNode.CenterX, Y1 = fromNode.CenterY,
                        X2 = cx,               Y2 = cy
                    });
                }
            }
            // Canvas bounds from all nodes (including group nodes that may extend beyond grid)
            double maxRight  = nodeMap.Values.Max(n => n.X + n.Width);
            double maxBottom = nodeMap.Values.Max(n => n.Y + n.Height);
            CanvasWidth  = maxRight  + PADDING;
            CanvasHeight = maxBottom + PADDING;

            Nodes = nodeMap.Values.ToList();
            Edges = edges;
        }

        // --------- World view ------ show everything; collapse big zones to single nodes -----------------

        private void BuildWorldView(Room currentRoom, IReadOnlyList<ZoneInfo> bigZones)
        {
            MapTitle = Localization.T("game.map.world.title");

            var layoutRoot = RoomService.GetRoomById(1) ?? currentRoom;
            var positions = MapBuilder.BuildRoomMap(layoutRoot);
            if (positions.Count == 0) return;

            // Build room-to-zone lookup (non-dungeon zones; dungeon rooms are excluded by MapBuilder)
            var nonDungeonZones = bigZones.Where(z => !z.IsDungeon).ToList();
            var roomToZone = new Dictionary<int, ZoneInfo>();
            foreach (var zone in nonDungeonZones)
                foreach (var id in zone.RoomIds)
                    roomToZone[id] = zone;

            var zoneLayouts = BuildWorldZoneLayouts(bigZones);

            // Use a fixed world root so the same world always draws the same way, regardless of
            // the player's current room. The second pass never enters collapsed zones, keeping
            // non-zone roads anchored by their own exits instead of by zone interiors.
            var rPos = new Dictionary<int, (int x, int y)>();
            {
                var rOccupied = new HashSet<(int, int)> { (0, 0) };
                var rQueue    = new Queue<(Room room, int x, int y)>();
                var rVisited  = new HashSet<int>();

                rPos[layoutRoot.Id] = (0, 0);
                rQueue.Enqueue((layoutRoot, 0, 0));

                while (rQueue.Count > 0)
                {
                    var (room, x, y) = rQueue.Dequeue();
                    if (!rVisited.Add(room.Id)) continue;

                    foreach (var (dir, targetId) in room.ExitIds.OrderBy(e => e.Key.ToLower() switch { "north" => 0, "east" => 1, "south" => 2, "west" => 3, _ => 4 }))
                    {
                        if (roomToZone.ContainsKey(targetId)) continue; // never enter zone rooms

                        var target = RoomService.GetRoomById(targetId);
                        if (target == null || target.IsDungeonRoom || rPos.ContainsKey(targetId)) continue;

                        int dx = dir.ToLower() switch { "east" => 1, "west" => -1, _ => 0 };
                        int dy = dir.ToLower() switch { "south" => 1, "north" => -1, _ => 0 };

                        int extend = 1;
                        (int cx, int cy) cand = (x + dx, y + dy);
                        while (rOccupied.Contains(cand)) { extend++; cand = (x + dx * extend, y + dy * extend); }

                        rOccupied.Add(cand);
                        rPos[targetId] = cand;
                        rQueue.Enqueue((target, cand.cx, cand.cy));
                    }
                }

                // Apply corrected positions: match by room.Id so reference inequality is not an issue
                foreach (var (room, _) in positions.ToList())
                {
                    if (rPos.TryGetValue(room.Id, out var corrected))
                        positions[room] = corrected;
                }
            }

            // Anchor each zone.
            // For zones with AnchorRoomId: compute from the gateway non-zone room's
            // (now-corrected) position + direction to anchor room, so the zone node
            // always appears directly adjacent to the correct non-zone neighbour.
            // Fallback / zones without AnchorRoomId: BFS-first.
            var zoneGridPos = new Dictionary<string, (double gx, double gy)>();

            foreach (var zone in nonDungeonZones.Where(z => z.AnchorRoomId != 0))
            {
                bool found = false;
                foreach (var (roomId, pos) in rPos)
                {
                    if (found) break;
                    if (zone.RoomIds.Contains(roomId)) continue;
                    var room = RoomService.GetRoomById(roomId);
                    if (room == null) continue;
                    foreach (var (dir, targetId) in room.ExitIds)
                    {
                        if (targetId != zone.AnchorRoomId) continue;
                        int dx = dir.ToLower() switch { "east" => 1, "west" => -1, _ => 0 };
                        int dy = dir.ToLower() switch { "south" => 1, "north" => -1, _ => 0 };
                        zoneGridPos[zone.Id] = (pos.x + dx, pos.y + dy);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    // Anchor room not reachable from a non-zone gateway â€” use anchor room's BFS pos
                    found = TryAnchorFromCollapsedZone(zone, nonDungeonZones, zoneGridPos);
                    if (found) continue;

                    foreach (var (room, pos) in positions)
                    {
                        if (room.Id == zone.AnchorRoomId) { zoneGridPos[zone.Id] = (pos.x, pos.y); break; }
                    }
                }
            }

            // BFS-first for zones without AnchorRoomId (or as final fallback)
            foreach (var (room, pos) in positions)
            {
                if (!roomToZone.TryGetValue(room.Id, out var zone)) continue;
                if (!zoneGridPos.ContainsKey(zone.Id))
                    zoneGridPos[zone.Id] = (pos.x, pos.y);
            }

            // Cascade-correct correctable bridge chains between two different big zones
            var cascadedPaths = new HashSet<(string, int)>();
            foreach (var zone in nonDungeonZones)
            {
                if (!zoneGridPos.TryGetValue(zone.Id, out _)) continue;
                var zoneRoomSet = new HashSet<int>(zone.RoomIds);

                foreach (var (room, _) in positions)
                {
                    if (!zoneRoomSet.Contains(room.Id)) continue;
                    foreach (var (dir, targetId) in room.ExitIds)
                    {
                        if (zoneRoomSet.Contains(targetId)) continue;       // within zone
                        if (roomToZone.ContainsKey(targetId)) continue;     // another zone room
                        if (!cascadedPaths.Add((zone.Id, targetId))) continue;

                        if (IsBridge(targetId, room.Id, zone, roomToZone))
                            CascadeCorrect(zone, dir, targetId, room.Id, positions, zoneGridPos, roomToZone);
                    }
                }
            }

            // Position dungeon group nodes relative to their connecting non-dungeon room
            var dungeonZones = bigZones.Where(z => z.IsDungeon).ToList();
            foreach (var dungeon in dungeonZones)
            {
                var dungeonRoomSet = new HashSet<int>(dungeon.RoomIds);
                bool found = false;
                foreach (var (room, pos) in positions)
                {
                    if (found) break;
                    foreach (var (dir, targetId) in room.ExitIds)
                    {
                        if (!dungeonRoomSet.Contains(targetId)) continue;

                        // Effective position: zone anchor if room is in a zone, else own position
                        double effectiveGx, effectiveGy;
                        if (roomToZone.TryGetValue(room.Id, out var connectingZone) &&
                            zoneGridPos.TryGetValue(connectingZone.Id, out var zPos))
                            (effectiveGx, effectiveGy) = zPos;
                        else
                            (effectiveGx, effectiveGy) = (pos.x, pos.y);

                        int dx = dir.ToLower() switch { "east" => 1, "west" => -1, _ => 0 };
                        int dy = dir.ToLower() switch { "south" => 1, "north" => -1, _ => 0 };

                        zoneGridPos[dungeon.Id] = (effectiveGx + dx, effectiveGy + dy);
                        found = true;
                        break;
                    }
                }
            }

            // Gather all effective grid coords (non-zone rooms + zone centroids)
            var allGx = new List<double>();
            var allGy = new List<double>();

            foreach (var (room, (gx, gy)) in positions)
            {
                if (roomToZone.ContainsKey(room.Id)) continue;
                allGx.Add(gx); allGy.Add(gy);
            }
            foreach (var (zoneId, (gx, gy)) in zoneGridPos)
            {
                if (zoneLayouts.TryGetValue(zoneId, out var layout))
                {
                    allGx.Add(gx + (layout.ExpandWidth ? layout.MinX : 0));
                    allGx.Add(gx + (layout.ExpandWidth ? layout.MaxX : 0));
                    allGy.Add(gy + (layout.ExpandHeight ? layout.MinY : 0));
                    allGy.Add(gy + (layout.ExpandHeight ? layout.MaxY : 0));
                }
                else
                {
                    allGx.Add(gx); allGy.Add(gy);
                }
            }

            if (allGx.Count == 0) return;

            double minGx = allGx.Min(), maxGx = allGx.Max();
            double minGy = allGy.Min(), maxGy = allGy.Max();

            CanvasWidth  = (maxGx - minGx + 1) * STEP_X + PADDING * 2;
            CanvasHeight = (maxGy - minGy + 1) * STEP_Y + PADDING * 2;

            (double x, double y) GridToCanvas(double gx, double gy) =>
                ((gx - minGx) * STEP_X + STEP_X / 2 + PADDING,
                 (gy - minGy) * STEP_Y + STEP_Y / 2 + PADDING);

            // String-keyed node map: room ID string or "zone_<zoneId>"
            var nodeMap = new Dictionary<string, MapNodeVm>();

            // Non-zone rooms
            foreach (var (room, (gx, gy)) in positions)
            {
                if (roomToZone.ContainsKey(room.Id)) continue;

                double cx = (gx - minGx) * STEP_X + STEP_X / 2 + PADDING;
                double cy = (gy - minGy) * STEP_Y + STEP_Y / 2 + PADDING;

                var kind = GetRoomKind(room);

                nodeMap[room.Id.ToString()] = new MapNodeVm
                {
                    RoomId     = room.Id,
                    Label      = Localization.T(room.Name),
                    X          = cx - NODE_W / 2,
                    Y          = cy - NODE_H / 2,
                    CenterX    = cx,
                    CenterY    = cy,
                    Width      = NODE_W,
                    Height     = NODE_H,
                    IsCurrent  = room.Id == currentRoom.Id,
                    Kind       = kind,
                    NpcTooltip = room.NpcRefs.Count > 0
                        ? string.Join(", ", room.NpcRefs.Where(n => n != null).Select(n => Localization.T(n.NameKey)))
                        : ""
                };
            }

            // All zone group nodes (non-dungeon and dungeon)
            foreach (var (zoneId, (gx, gy)) in zoneGridPos)
            {
                var zone = bigZones.First(z => z.Id == zoneId);
                double x, y, cx, cy, width, height;
                if (zoneLayouts.TryGetValue(zoneId, out var layout))
                {
                    int minX = layout.ExpandWidth ? layout.MinX : 0;
                    int maxX = layout.ExpandWidth ? layout.MaxX : 0;
                    int minY = layout.ExpandHeight ? layout.MinY : 0;
                    int maxY = layout.ExpandHeight ? layout.MaxY : 0;

                    var topLeft = GridToCanvas(gx + minX, gy + minY);
                    width  = (maxX - minX) * STEP_X + GROUP_W;
                    height = (maxY - minY) * STEP_Y + GROUP_H;
                    x = topLeft.x - GROUP_W / 2;
                    y = topLeft.y - GROUP_H / 2;
                    cx = x + width / 2;
                    cy = y + height / 2;
                }
                else
                {
                    (cx, cy) = GridToCanvas(gx, gy);
                    width = GROUP_W;
                    height = GROUP_H;
                    x = cx - GROUP_W / 2;
                    y = cy - GROUP_H / 2;
                }

                nodeMap["zone_" + zoneId] = new MapNodeVm
                {
                    RoomId      = -1,
                    Label       = zone.DisplayName,
                    X           = x,
                    Y           = y,
                    CenterX     = cx,
                    CenterY     = cy,
                    Width       = width,
                    Height      = height,
                    IsCurrent   = false,
                    Kind        = zone.Kind,
                    IsGroupNode = true,
                    ZoneId      = zoneId
                };
            }

            // Edges: replace zone-room endpoints with the zone node key
            // Also handle dungeon zone edges (from connecting non-dungeon room)
            var edges = new List<MapEdgeVm>();
            var seenEdges = new HashSet<(string, string)>();

            (double x, double y) EdgePoint(string key, MapNodeVm node, int zoneRoomId)
            {
                const string zonePrefix = "zone_";
                if (key.StartsWith(zonePrefix, StringComparison.Ordinal))
                {
                    var zoneId = key[zonePrefix.Length..];
                    if (zoneLayouts.TryGetValue(zoneId, out var layout) &&
                        layout.RoomOffsets.TryGetValue(zoneRoomId, out var offset) &&
                        zoneGridPos.TryGetValue(zoneId, out var zonePos))
                    {
                        int xOffset = layout.ExpandWidth ? offset.x : 0;
                        int yOffset = layout.ExpandHeight ? offset.y : 0;
                        return GridToCanvas(zonePos.gx + xOffset, zonePos.gy + yOffset);
                    }
                }

                return (node.CenterX, node.CenterY);
            }

            // Edges from MapBuilder BFS (non-dungeon rooms)
            foreach (var (room, _) in positions)
            {
                string fromKey = roomToZone.TryGetValue(room.Id, out var fz)
                    ? "zone_" + fz.Id
                    : room.Id.ToString();

                if (!nodeMap.ContainsKey(fromKey)) continue;

                foreach (var (_, targetId) in room.ExitIds)
                {
                    string toKey = roomToZone.TryGetValue(targetId, out var tz)
                        ? "zone_" + tz.Id
                        : targetId.ToString();

                    if (!nodeMap.ContainsKey(toKey)) continue;
                    if (fromKey == toKey) continue;

                    var edgeKey = string.Compare(fromKey, toKey, StringComparison.Ordinal) < 0
                        ? (fromKey, toKey) : (toKey, fromKey);
                    if (!seenEdges.Add(edgeKey)) continue;

                    var fromNode = nodeMap[fromKey];
                    var toNode   = nodeMap[toKey];
                    var fromPoint = EdgePoint(fromKey, fromNode, room.Id);
                    var toPoint = EdgePoint(toKey, toNode, targetId);
                    if (Math.Abs(fromPoint.x - toPoint.x) > 1.0 &&
                        Math.Abs(fromPoint.y - toPoint.y) > 1.0) continue;
                    edges.Add(new MapEdgeVm
                    {
                        X1 = fromPoint.x, Y1 = fromPoint.y,
                        X2 = toPoint.x,   Y2 = toPoint.y
                    });
                }

                // Dungeon edges: exits from non-dungeon rooms into dungeon rooms
                foreach (var (_, targetId) in room.ExitIds)
                {
                    var dungeonZone = dungeonZones.FirstOrDefault(d => d.RoomIds.Contains(targetId));
                    if (dungeonZone == null) continue;

                    string dungeonKey = "zone_" + dungeonZone.Id;
                    if (!nodeMap.ContainsKey(dungeonKey)) continue;

                    string roomKey = roomToZone.TryGetValue(room.Id, out var rz)
                        ? "zone_" + rz.Id
                        : room.Id.ToString();
                    if (!nodeMap.ContainsKey(roomKey)) continue;
                    if (roomKey == dungeonKey) continue;

                    var edgeKey = string.Compare(roomKey, dungeonKey, StringComparison.Ordinal) < 0
                        ? (roomKey, dungeonKey) : (dungeonKey, roomKey);
                    if (!seenEdges.Add(edgeKey)) continue;

                    var fromNode = nodeMap[roomKey];
                    var toNode   = nodeMap[dungeonKey];
                    var fromPoint = EdgePoint(roomKey, fromNode, room.Id);
                    var toPoint = EdgePoint(dungeonKey, toNode, targetId);
                    if (Math.Abs(fromPoint.x - toPoint.x) > 1.0 &&
                        Math.Abs(fromPoint.y - toPoint.y) > 1.0) continue;
                    edges.Add(new MapEdgeVm
                    {
                        X1 = fromPoint.x, Y1 = fromPoint.y,
                        X2 = toPoint.x,   Y2 = toPoint.y
                    });
                }
            }

            Nodes = nodeMap.Values.ToList();
            Edges = edges;
        }

        // ---------- Helpers -------------------------------------------------------------------------------------------------------------------------

        private static NodeKind GetRoomKind(Room room)
        {
            if (room.IsBossRoom)    return NodeKind.Boss;
            if (room.IsDungeonRoom) return NodeKind.Dungeon;
            if (room.IsCaveRoom)    return NodeKind.Cave;
            if (room.IsCity)        return NodeKind.City;
            if (ForestRegistry.GetForestByRoom(room) != null) return NodeKind.Forest;
            return NodeKind.World;
        }

        private static (bool Width, bool Height) GetWorldMapExpansion(MapArea area) =>
            (area.ExpandOnWorldMap || area.ExpandWorldMapWidth,
             area.ExpandOnWorldMap || area.ExpandWorldMapHeight);

        private static Dictionary<string, ZoneLayout> BuildWorldZoneLayouts(IReadOnlyList<ZoneInfo> zones)
        {
            var layouts = new Dictionary<string, ZoneLayout>();

            foreach (var zone in zones.Where(z => z.ExpandWorldMapWidth || z.ExpandWorldMapHeight))
            {
                var startRoom = RoomService.GetRoomById(zone.AnchorRoomId);
                if (startRoom == null)
                {
                    foreach (var roomId in zone.RoomIds)
                    {
                        startRoom = RoomService.GetRoomById(roomId);
                        if (startRoom != null) break;
                    }
                }
                if (startRoom == null) continue;

                var positions = BuildZoneBfs(startRoom, new HashSet<int>(zone.RoomIds));
                if (positions.Count == 0) continue;

                var offsets = positions.ToDictionary(p => p.Key.Id, p => p.Value);
                layouts[zone.Id] = new ZoneLayout(
                    positions.Values.Min(p => p.x),
                    positions.Values.Min(p => p.y),
                    positions.Values.Max(p => p.x),
                    positions.Values.Max(p => p.y),
                    zone.ExpandWorldMapWidth,
                    zone.ExpandWorldMapHeight,
                    offsets);
            }

            return layouts;
        }

        private static bool TryAnchorFromCollapsedZone(
            ZoneInfo zone,
            IReadOnlyList<ZoneInfo> zones,
            Dictionary<string, (double gx, double gy)> zoneGridPos)
        {
            foreach (var sourceZone in zones)
            {
                if (sourceZone.Id == zone.Id) continue;
                if (!zoneGridPos.TryGetValue(sourceZone.Id, out var sourcePos)) continue;

                foreach (var sourceRoomId in sourceZone.RoomIds)
                {
                    var sourceRoom = RoomService.GetRoomById(sourceRoomId);
                    if (sourceRoom == null) continue;

                    foreach (var (dir, targetId) in sourceRoom.ExitIds)
                    {
                        if (targetId != zone.AnchorRoomId) continue;

                        int dx = dir.ToLower() switch { "east" => 1, "west" => -1, _ => 0 };
                        int dy = dir.ToLower() switch { "south" => 1, "north" => -1, _ => 0 };
                        zoneGridPos[zone.Id] = (sourcePos.gx + dx, sourcePos.gy + dy);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the non-zone room chain starting at <paramref name="startId"/> (coming from
        /// <paramref name="fromId"/>) leads to exactly one other big zone without any junction.
        /// </summary>
        private static bool IsBridge(int startId, int fromId, ZoneInfo originZone,
                                      Dictionary<int, ZoneInfo> roomToZone)
        {
            var visited = new HashSet<int> { fromId };
            int current = startId;

            while (true)
            {
                if (roomToZone.TryGetValue(current, out var zone))
                {
                    // Hit a zone: bridge only if it's a different zone
                    return zone.Id != originZone.Id;
                }

                var room = RoomService.GetRoomById(current);
                if (room == null) return false;

                visited.Add(current);

                var outward = room.ExitIds
                    .Where(e => !visited.Contains(e.Value))
                    .ToList();

                if (outward.Count == 0) return false; // dead end â€” not a bridge
                if (outward.Count > 1) return false;  // junction â€” not correctable

                current = outward[0].Value;
            }
        }

        /// <summary>
        /// Overrides grid positions of rooms in a bridge corridor so they sit directly in line
        /// with <paramref name="originZone"/>'s anchor, then updates the terminating zone's anchor.
        /// </summary>
        private static void CascadeCorrect(ZoneInfo originZone, string direction, int startId, int fromId,
                                           Dictionary<Room, (int x, int y)> positions,
                                           Dictionary<string, (double gx, double gy)> zoneGridPos,
                                           Dictionary<int, ZoneInfo> roomToZone)
        {
            int dx = direction.ToLower() switch { "east" => 1, "west" => -1, _ => 0 };
            int dy = direction.ToLower() switch { "south" => 1, "north" => -1, _ => 0 };

            var (anchorGx, anchorGy) = zoneGridPos[originZone.Id];
            var visited = new HashSet<int> { fromId };
            int current = startId;
            int step = 1;

            while (true)
            {
                if (roomToZone.TryGetValue(current, out var endZone))
                {
                    // Update the terminating zone's anchor to the cascaded position
                    zoneGridPos[endZone.Id] = (anchorGx + dx * step, anchorGy + dy * step);
                    break;
                }

                var room = RoomService.GetRoomById(current);
                if (room == null) break;

                visited.Add(current);

                if (positions.ContainsKey(room))
                    positions[room] = ((int)(anchorGx + dx * step), (int)(anchorGy + dy * step));

                step++;

                var next = room.ExitIds
                    .Where(e => !visited.Contains(e.Value))
                    .ToList();

                if (next.Count != 1) break;
                current = next[0].Value;
            }
        }

        /// <summary>BFS restricted to a specific set of room IDs (for group view).</summary>
        private static Dictionary<Room, (int x, int y)> BuildZoneBfs(Room startRoom, HashSet<int> zoneRoomIds)
        {
            var positions = new Dictionary<Room, (int x, int y)>();
            var occupied  = new HashSet<(int, int)> { (0, 0) };
            var queue     = new Queue<(Room room, int x, int y)>();
            var visited   = new HashSet<int>();

            queue.Enqueue((startRoom, 0, 0));
            positions[startRoom] = (0, 0);

            while (queue.Count > 0)
            {
                var (room, x, y) = queue.Dequeue();
                if (!visited.Add(room.Id)) continue;

                foreach (var (dir, targetId) in room.ExitIds.OrderBy(e => e.Key.ToLower() switch { "north" => 0, "east" => 1, "south" => 2, "west" => 3, _ => 4 }))
                {
                    if (!zoneRoomIds.Contains(targetId)) continue;
                    var target = RoomService.GetRoomById(targetId);
                    if (target == null || positions.ContainsKey(target)) continue;

                    int ddx = dir.ToLower() switch { "east" => 1, "west" => -1, _ => 0 };
                    int ddy = dir.ToLower() switch { "north" => -1, "south" => 1, _ => 0 };

                    int extend = 1;
                    (int cx, int cy) candidate = (x + ddx * extend, y + ddy * extend);
                    while (occupied.Contains(candidate))
                    {
                        extend++;
                        candidate = (x + ddx * extend, y + ddy * extend);
                    }

                    occupied.Add(candidate);
                    positions[target] = candidate;
                    queue.Enqueue((target, candidate.cx, candidate.cy));
                }
            }

            return positions;
        }
    }
}
