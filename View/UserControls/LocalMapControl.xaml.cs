using Myria.Wpf.Systems.MapNode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Myria.Wpf.View.UserControls
{
    public partial class LocalMapControl : UserControl
    {
        // ── Constants ──────────────────────────────────────────────────────────
        private const double SCALE_STEP = 0.15;
        private const double SCALE_MIN  = 0.20;
        private const double SCALE_MAX  = 3.00;

        // ── State ──────────────────────────────────────────────────────────────
        private double      _scale    = 1.0;
        private Point       _dragOrigin;
        private double      _dragTx, _dragTy;
        private bool        _dragging;
        private MapNodeVm?  _groupClickCandidate;

        public LocalMapControl()
        {
            InitializeComponent();
            SizeChanged += (_, __) => CenterOnCurrentNode();
        }

        // ── Dependency Properties ──────────────────────────────────────────────

        public IReadOnlyList<MapNodeVm> Nodes
        {
            get => (IReadOnlyList<MapNodeVm>)GetValue(NodesProperty);
            set => SetValue(NodesProperty, value);
        }
        public static readonly DependencyProperty NodesProperty =
            DependencyProperty.Register(nameof(Nodes), typeof(IReadOnlyList<MapNodeVm>),
                typeof(LocalMapControl), new PropertyMetadata(null, (d, _) => ((LocalMapControl)d).Redraw()));

        public IReadOnlyList<MapEdgeVm> Edges
        {
            get => (IReadOnlyList<MapEdgeVm>)GetValue(EdgesProperty);
            set => SetValue(EdgesProperty, value);
        }
        public static readonly DependencyProperty EdgesProperty =
            DependencyProperty.Register(nameof(Edges), typeof(IReadOnlyList<MapEdgeVm>),
                typeof(LocalMapControl), new PropertyMetadata(null, (d, _) => ((LocalMapControl)d).Redraw()));

        public ICommand GroupNodeClickedCommand
        {
            get => (ICommand)GetValue(GroupNodeClickedCommandProperty);
            set => SetValue(GroupNodeClickedCommandProperty, value);
        }
        public static readonly DependencyProperty GroupNodeClickedCommandProperty =
            DependencyProperty.Register(nameof(GroupNodeClickedCommand), typeof(ICommand),
                typeof(LocalMapControl), new PropertyMetadata(null));

        // ── Colors ─────────────────────────────────────────────────────────────
        private static readonly Color _colorCity    = Color.FromRgb(100, 75, 10);
        private static readonly Color _colorDungeon = Color.FromRgb(80,  20, 20);
        private static readonly Color _colorBoss    = Color.FromRgb(120, 10, 10);
        private static readonly Color _colorCave    = Color.FromRgb(55,  55, 65);
        private static readonly Color _colorForest  = Color.FromRgb(25,  65, 30);
        private static readonly Color _colorWorld   = Color.FromRgb(35,  40, 55);

        // Fallback values; overridden at runtime by theme resources (Color.Map.Border.* etc.)
        private static readonly Color _fallbackBorderNormal  = Color.FromRgb(122, 102,  72);
        private static readonly Color _fallbackBorderCurrent = Color.FromRgb( 58,  95, 138);
        private static readonly Color _fallbackBorderGroup   = Color.FromRgb(180, 145,  60);
        private static readonly Color _fallbackEdge          = Color.FromRgb(130, 108,  68);
        private static readonly Color _fallbackLabelNormal   = Color.FromRgb(218, 212, 192);
        private static readonly Color _fallbackLabelGroup    = Color.FromRgb(237, 210, 130);
        private static readonly Color _fallbackMarker        = Color.FromRgb( 58,  95, 138);

        // ── Public zoom API (called from Page buttons) ─────────────────────────
        public void ZoomIn()  => ApplyZoom(_scale + SCALE_STEP);
        public void ZoomOut() => ApplyZoom(_scale - SCALE_STEP);

        // ── Zoom logic ─────────────────────────────────────────────────────────
        /// <summary>
        /// Scales the map around <paramref name="pivot"/> (viewport coords).
        /// When pivot is null the viewport center is used.
        /// </summary>
        private void ApplyZoom(double newScale, Point? pivot = null)
        {
            newScale = Math.Clamp(newScale, SCALE_MIN, SCALE_MAX);
            double cx     = pivot?.X ?? Viewport.ActualWidth  / 2;
            double cy     = pivot?.Y ?? Viewport.ActualHeight / 2;
            double factor = newScale / _scale;

            TranslateXform.X = cx - (cx - TranslateXform.X) * factor;
            TranslateXform.Y = cy - (cy - TranslateXform.Y) * factor;
            ScaleXform.ScaleX = ScaleXform.ScaleY = newScale;
            _scale = newScale;
        }

        // ── Center the current-room node in the viewport ───────────────────────
        private void CenterOnCurrentNode()
        {
            if (Nodes == null) return;
            var current = Nodes.FirstOrDefault(n => n.IsCurrent);
            if (current == null || Viewport.ActualWidth <= 0) return;

            TranslateXform.X = Viewport.ActualWidth  / 2 - current.CenterX * _scale;
            TranslateXform.Y = Viewport.ActualHeight / 2 - current.CenterY * _scale;
        }

        // ── Drawing ────────────────────────────────────────────────────────────
        private void Redraw()
        {
            Layer.Children.Clear();

            // Reset zoom/pan for each new map
            ScaleXform.ScaleX = ScaleXform.ScaleY = 1.0;
            TranslateXform.X  = TranslateXform.Y  = 0;
            _scale = 1.0;

            var nodes = Nodes;
            var edges = Edges;
            if (nodes == null || nodes.Count == 0) return;

            // Size the canvas so hit-testing works across the full map area
            Layer.Width  = nodes.Max(n => n.X + n.Width)  + 30;
            Layer.Height = nodes.Max(n => n.Y + n.Height) + 30;

            // Resolve structural colors from theme resources
            var borderNormal  = GetMapColor("Color.Map.Border.Normal",  _fallbackBorderNormal);
            var borderCurrent = GetMapColor("Color.Map.Border.Current", _fallbackBorderCurrent);
            var borderGroup   = GetMapColor("Color.Map.Border.Group",   _fallbackBorderGroup);
            var edgeColor     = GetMapColor("Color.Map.Edge",           _fallbackEdge);
            var labelNormal   = GetMapColor("Color.Map.Label.Normal",   _fallbackLabelNormal);
            var labelGroup    = GetMapColor("Color.Map.Label.Group",    _fallbackLabelGroup);
            var markerColor   = GetMapColor("Color.Map.Marker",         _fallbackMarker);

            // 1) Edges — behind nodes
            var edgePen = new SolidColorBrush(edgeColor);
            foreach (var edge in edges ?? [])
            {
                Layer.Children.Add(new Line
                {
                    X1 = edge.X1, Y1 = edge.Y1,
                    X2 = edge.X2, Y2 = edge.Y2,
                    Stroke          = edgePen,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 5, 3 }
                });
            }

            // 2) Nodes
            foreach (var node in nodes)
            {
                var fillColor = node.Kind switch
                {
                    NodeKind.City    => GetMapColor("Color.Map.Node.City",    _colorCity),
                    NodeKind.Dungeon => GetMapColor("Color.Map.Node.Dungeon", _colorDungeon),
                    NodeKind.Boss    => GetMapColor("Color.Map.Node.Boss",    _colorBoss),
                    NodeKind.Cave    => GetMapColor("Color.Map.Node.Cave",    _colorCave),
                    NodeKind.Forest  => GetMapColor("Color.Map.Node.Forest",  _colorForest),
                    _                => GetMapColor("Color.Map.Node.World",   _colorWorld)
                };

                Border rect;
                if (node.IsGroupNode)
                {
                    // Group nodes get a dashed double border to show they contain multiple rooms
                    rect = new Border
                    {
                        Width           = node.Width,
                        Height          = node.Height,
                        Background      = new SolidColorBrush(fillColor),
                        BorderBrush     = new SolidColorBrush(borderGroup),
                        BorderThickness = new Thickness(2),
                        CornerRadius    = new CornerRadius(8),
                        Cursor          = Cursors.Hand,
                        Tag             = node
                    };
                    rect.MouseLeftButtonDown += GroupNode_MouseDown;
                    rect.MouseLeftButtonUp   += GroupNode_MouseUp;
                    // Outer glow ring drawn as a slightly larger unfilled rect behind
                    var glow = new Border
                    {
                        Width           = node.Width + 6,
                        Height          = node.Height + 6,
                        Background      = Brushes.Transparent,
                        BorderBrush     = new SolidColorBrush(Color.FromArgb(80, borderGroup.R, borderGroup.G, borderGroup.B)),
                        BorderThickness = new Thickness(2),
                        CornerRadius    = new CornerRadius(10)
                    };
                    Canvas.SetLeft(glow, node.X - 3);
                    Canvas.SetTop(glow,  node.Y - 3);
                    Layer.Children.Add(glow);
                }
                else
                {
                    rect = new Border
                    {
                        Width           = node.Width,
                        Height          = node.Height,
                        Background      = new SolidColorBrush(fillColor),
                        BorderBrush     = new SolidColorBrush(node.IsCurrent ? borderCurrent : borderNormal),
                        BorderThickness = new Thickness(node.IsCurrent ? 2.5 : 1.5),
                        CornerRadius    = new CornerRadius(6)
                    };
                }

                if (!node.IsGroupNode && !string.IsNullOrEmpty(node.NpcTooltip))
                    rect.ToolTip = node.NpcTooltip;

                Canvas.SetLeft(rect, node.X);
                Canvas.SetTop(rect,  node.Y);
                Layer.Children.Add(rect);

                var lbl = new TextBlock
                {
                    Text          = node.Label,
                    Foreground    = new SolidColorBrush(GetLabelColor(fillColor, node.IsGroupNode, labelNormal, labelGroup)),
                    FontSize      = node.IsGroupNode ? 12 : 11,
                    FontWeight    = node.IsGroupNode || node.IsCurrent ? FontWeights.Bold : FontWeights.Normal,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping  = TextWrapping.NoWrap,
                    Width         = node.Width - 8,
                    TextTrimming  = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(lbl, node.X + 4);
                Canvas.SetTop(lbl,  node.Y + (node.Height - (node.IsGroupNode ? 18 : 16)) / 2);
                Layer.Children.Add(lbl);

                // ▶ marker left of current room node
                if (node.IsCurrent)
                {
                    var marker = new TextBlock
                    {
                        Text       = ">",
                        Foreground = new SolidColorBrush(markerColor),
                        FontSize   = 11,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(marker, node.X - 16);
                    Canvas.SetTop(marker,  node.Y + (node.Height - 14) / 2);
                    Layer.Children.Add(marker);
                }
            }

            // Center on current node once layout is complete
            Dispatcher.InvokeAsync(CenterOnCurrentNode, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // ── Group node click ───────────────────────────────────────────────────
        private void GroupNode_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _groupClickCandidate = (MapNodeVm)((FrameworkElement)sender).Tag;
            e.Handled = true; // prevent viewport pan from starting
        }

        private void GroupNode_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_groupClickCandidate != null &&
                GroupNodeClickedCommand?.CanExecute(_groupClickCandidate) == true)
            {
                GroupNodeClickedCommand.Execute(_groupClickCandidate);
            }
            _groupClickCandidate = null;
            e.Handled = true;
        }

        private static Color GetLabelColor(Color background, bool isGroupNode, Color normal, Color group)
        {
            double luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255.0;
            return luminance > 0.55 ? Color.FromRgb(16, 24, 32) : isGroupNode ? group : normal;
        }

        private Color GetMapColor(string key, Color fallback)
        {
            var res = TryFindResource(key);
            return res is Color c ? c : fallback;
        }

        // ── Mouse: Pan ─────────────────────────────────────────────────────────
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragging   = true;
            _dragOrigin = e.GetPosition(Viewport);
            _dragTx     = TranslateXform.X;
            _dragTy     = TranslateXform.Y;
            Viewport.CaptureMouse();
            Viewport.Cursor = Cursors.SizeAll;
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var pos = e.GetPosition(Viewport);
            TranslateXform.X = _dragTx + (pos.X - _dragOrigin.X);
            TranslateXform.Y = _dragTy + (pos.Y - _dragOrigin.Y);
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;
            Viewport.ReleaseMouseCapture();
            Viewport.Cursor = Cursors.Hand;
        }

        private void Viewport_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;
            Viewport.ReleaseMouseCapture();
            Viewport.Cursor = Cursors.Hand;
        }

        // ── Mouse: Zoom (wheel) ─────────────────────────────────────────────────
        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double delta = e.Delta > 0 ? SCALE_STEP : -SCALE_STEP;
            ApplyZoom(_scale + delta, e.GetPosition(Viewport));
            e.Handled = true; // prevent page scroll
        }
    }
}
