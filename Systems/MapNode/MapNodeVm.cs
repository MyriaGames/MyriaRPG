namespace Myria.Wpf.Systems.MapNode
{
    public enum NodeKind { World, City, Dungeon, Cave, Forest, Boss }

    public class MapNodeVm
    {
        public int    RoomId      { get; set; }
        public string Label       { get; set; } = "";
        /// <summary>Canvas left edge of the node rectangle.</summary>
        public double X           { get; set; }
        /// <summary>Canvas top edge of the node rectangle.</summary>
        public double Y           { get; set; }
        public double CenterX     { get; set; }
        public double CenterY     { get; set; }
        public double Width       { get; set; }
        public double Height      { get; set; }
        public bool   IsCurrent   { get; set; }
        public NodeKind Kind      { get; set; }
        /// <summary>True when this node represents a collapsed zone rather than a single room.</summary>
        public bool   IsGroupNode { get; set; }
        /// <summary>Zone ID for group nodes; empty for regular room nodes.</summary>
        public string ZoneId      { get; set; } = "";
        /// <summary>Comma-separated NPC names present in this room; empty if none.</summary>
        public string NpcTooltip  { get; set; } = "";
    }

    public class MapEdgeVm
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }
}
