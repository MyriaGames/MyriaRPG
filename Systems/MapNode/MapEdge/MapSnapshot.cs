namespace Myria.Wpf.Systems.MapNode.MapEdge
{
    // Local map snapshot (for one room/area)
    public enum PoiKind { Smith, Healer, Vendor, QuestGiver, Trainer, Inn, Crafting, Portal, Entrance, Other }

    public sealed record LocalPoi(string Id, PoiKind Kind, string Label, double X, double Y);
    // X/Y are 0..1 normalized positions on the local map canvas

    public sealed record LocalExit(string TargetRoomId, string Label, double X, double Y);

    public sealed record LocalMapSnapshot(
        string RoomId,
        string RoomName,
        IReadOnlyList<LocalPoi> Pois,
        IReadOnlyList<LocalExit> Exits);
}
