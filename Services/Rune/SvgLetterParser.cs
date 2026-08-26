using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Myria.Wpf.Services.Rune
{
    /// <summary>
    /// Parses Myriac script SVG files into edge-dilated pixel masks for letter recognition.
    ///
    /// Pipeline per template:
    ///   1. Parse all M…Z subpaths (handles multi-component letters such as Capital G).
    ///   2. Render filled silhouette to a MaskSize×MaskSize bitmap.
    ///   3. Extract boundary pixels of the silhouette (thin 1-pixel contour).
    ///   4. Dilate the contour by TemplateDilation pixels.
    ///
    /// Pipeline for a drawn ink sample:
    ///   1. Render strokes with a medium-weight pen, normalised to the bounding box.
    ///   2. Dilate the result by DrawingDilation pixels.
    ///
    /// Both processed masks are "fat-band outlines" of similar width, making Jaccard
    /// similarity meaningful even though the template was a solid fill and the drawing
    /// consists of thin ink strokes.
    /// </summary>
    public static class SvgLetterParser
    {
        private const int MaskSize         = 96;
        private const int TemplateDilation = 5;   // pixels
        private const int DrawingDilation  = 4;   // pixels

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads all SVG files from <paramref name="folder"/> and returns a map from
        /// letter key ("A"…"Z", "a"…"z", "Ch", "ch", "Sch", "sch") to the processed mask.
        /// </summary>
        public static Dictionary<string, bool[]> LoadTemplates(string folder)
        {
            var templates = new Dictionary<string, bool[]>(StringComparer.Ordinal);

            if (!Directory.Exists(folder))
                return templates;

            foreach (var file in Directory.GetFiles(folder, "*.svg"))
            {
                var key = FileNameToKey(Path.GetFileNameWithoutExtension(file));
                if (key is null) continue;

                try
                {
                    var svg      = File.ReadAllText(file);
                    var pathD    = ExtractPathD(svg);
                    if (pathD is null) continue;

                    var subpaths = ParseAllSubpaths(pathD);
                    if (subpaths.Count == 0) continue;

                    var filled   = RenderSubpathsToMask(subpaths, MaskSize);
                    var edges    = ExtractEdges(filled, MaskSize);
                    var dilated  = Dilate(edges, MaskSize, TemplateDilation);

                    templates[key] = dilated;
                }
                catch
                {
                    // Skip malformed SVGs silently
                }
            }

            return templates;
        }

        /// <summary>
        /// Renders ink strokes to a normalised MaskSize×MaskSize processed mask ready
        /// for Jaccard comparison against the template masks.
        /// </summary>
        public static bool[] RenderStrokesToMask(StrokeCollection strokes)
        {
            if (strokes.Count == 0)
                return new bool[MaskSize * MaskSize];

            var bounds = Rect.Empty;
            foreach (var stroke in strokes)
                bounds.Union(stroke.GetBounds());

            if (bounds.IsEmpty || bounds.Width < 1 || bounds.Height < 1)
                return new bool[MaskSize * MaskSize];

            double margin    = MaskSize * 0.08;
            double innerSize = MaskSize - margin * 2;
            double scale     = Math.Min(innerSize / bounds.Width, innerSize / bounds.Height);
            double offsetX   = margin + (innerSize - bounds.Width  * scale) / 2 - bounds.Left * scale;
            double offsetY   = margin + (innerSize - bounds.Height * scale) / 2 - bounds.Top  * scale;

            // Stroke width: ~9 px at 96×96 after normalisation, comparable to the dilated template edges
            double strokeWidth = Math.Max(bounds.Width, bounds.Height) * 0.10;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, MaskSize, MaskSize));
                dc.PushTransform(new TranslateTransform(offsetX, offsetY));
                dc.PushTransform(new ScaleTransform(scale, scale));

                var pen = new Pen(Brushes.Black, strokeWidth)
                {
                    LineJoin    = PenLineJoin.Round,
                    StartLineCap = PenLineCap.Round,
                    EndLineCap   = PenLineCap.Round
                };

                foreach (var stroke in strokes)
                    dc.DrawGeometry(Brushes.Black, pen, stroke.GetGeometry());

                dc.Pop();
                dc.Pop();
            }

            var rtb  = new RenderTargetBitmap(MaskSize, MaskSize, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            var raw = ExtractMask(rtb);
            return Dilate(raw, MaskSize, DrawingDilation);
        }

        // ── Private: SVG parsing ──────────────────────────────────────────────────

        private static string? FileNameToKey(string baseName)
        {
            if (baseName.StartsWith("Capital ", StringComparison.OrdinalIgnoreCase))
            {
                var letter = baseName["Capital ".Length..];
                return letter.Length > 0
                    ? char.ToUpper(letter[0]) + letter[1..].ToLower()
                    : null;
            }
            if (baseName.StartsWith("small ", StringComparison.OrdinalIgnoreCase))
                return baseName["small ".Length..].ToLower();
            return null;
        }

        private static string? ExtractPathD(string svgContent)
        {
            var start = svgContent.IndexOf(" d=\"", StringComparison.Ordinal);
            if (start < 0) return null;
            start += 4;
            var end = svgContent.IndexOf('"', start);
            return end < 0 ? null : svgContent[start..end];
        }

        /// <summary>
        /// Splits an SVG path data string into one List entry per M…Z subpath.
        /// Correctly handles multi-component glyphs like Capital G that contain
        /// two or more disconnected regions.
        /// </summary>
        private static List<Point[]> ParseAllSubpaths(string d)
        {
            var result  = new List<Point[]>();
            var current = new List<Point>();

            var tokens = d.Split(new[] { ' ', '\t', '\n', '\r' },
                                 StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                switch (token)
                {
                    case "M":
                        // Start of a new subpath — flush any points collected so far
                        if (current.Count >= 3)
                            result.Add(current.ToArray());
                        current = new List<Point>();
                        break;

                    case "Z":
                        // End of subpath — close and flush
                        if (current.Count >= 3)
                            result.Add(current.ToArray());
                        current = new List<Point>();
                        break;

                    case "L":
                        // Line-to command — just a separator, ignore
                        break;

                    default:
                        if (token.Contains(','))
                        {
                            var parts = token.Split(',');
                            if (parts.Length == 2
                                && double.TryParse(parts[0], NumberStyles.Float,
                                                   CultureInfo.InvariantCulture, out double x)
                                && double.TryParse(parts[1], NumberStyles.Float,
                                                   CultureInfo.InvariantCulture, out double y))
                            {
                                current.Add(new Point(x, y));
                            }
                        }
                        break;
                }
            }

            // Final flush (in case there was no trailing Z)
            if (current.Count >= 3)
                result.Add(current.ToArray());

            return result;
        }

        // ── Private: rendering ────────────────────────────────────────────────────

        private static bool[] RenderSubpathsToMask(List<Point[]> subpaths, int size)
        {
            // Compute bounding box across ALL subpaths combined
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (var sp in subpaths)
            foreach (var pt in sp)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            double w = maxX - minX, h = maxY - minY;
            if (w < 1 || h < 1) return new bool[size * size];

            double margin    = size * 0.08;
            double innerSize = size - margin * 2;
            double scale     = Math.Min(innerSize / w, innerSize / h);
            double offsetX   = margin + (innerSize - w * scale) / 2 - minX * scale;
            double offsetY   = margin + (innerSize - h * scale) / 2 - minY * scale;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                foreach (var sp in subpaths)
                {
                    var transformed = sp
                        .Select(p => new Point(p.X * scale + offsetX, p.Y * scale + offsetY))
                        .ToArray();
                    ctx.BeginFigure(transformed[0], isFilled: true, isClosed: true);
                    ctx.PolyLineTo(transformed[1..], isStroked: false, isSmoothJoin: false);
                }
            }
            geometry.Freeze();

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, size, size));
                dc.DrawGeometry(Brushes.Black, null, geometry);
            }

            var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            return ExtractMask(rtb);
        }

        // ── Private: image processing ─────────────────────────────────────────────

        private static bool[] ExtractMask(RenderTargetBitmap rtb)
        {
            int size   = rtb.PixelWidth;
            var pixels = new byte[size * rtb.PixelHeight * 4];
            rtb.CopyPixels(pixels, size * 4, 0);

            var mask = new bool[size * rtb.PixelHeight];
            for (int i = 0; i < mask.Length; i++)
            {
                int b = pixels[i * 4];
                int g = pixels[i * 4 + 1];
                int r = pixels[i * 4 + 2];
                mask[i] = (r + g + b) / 3 < 128;
            }
            return mask;
        }

        /// <summary>
        /// Returns the 4-connected boundary pixels of all filled regions.
        /// A pixel is an edge pixel if it is set AND at least one 4-connected
        /// neighbour is either outside the image or unset.
        /// </summary>
        private static bool[] ExtractEdges(bool[] mask, int size)
        {
            var edges = new bool[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (!mask[y * size + x]) continue;

                bool isEdge =
                    y == 0        || !mask[(y - 1) * size + x] ||
                    y == size - 1 || !mask[(y + 1) * size + x] ||
                    x == 0        || !mask[y * size + (x - 1)] ||
                    x == size - 1 || !mask[y * size + (x + 1)];

                edges[y * size + x] = isEdge;
            }
            return edges;
        }

        /// <summary>
        /// Expands every set pixel by <paramref name="radius"/> pixels in all directions
        /// (square structuring element for speed; circular approximation via distance check
        /// would be marginal improvement given the small radii used).
        /// </summary>
        private static bool[] Dilate(bool[] mask, int size, int radius)
        {
            var result = new bool[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (!mask[y * size + x]) continue;

                int yMin = Math.Max(0, y - radius);
                int yMax = Math.Min(size - 1, y + radius);
                int xMin = Math.Max(0, x - radius);
                int xMax = Math.Min(size - 1, x + radius);

                for (int ny = yMin; ny <= yMax; ny++)
                for (int nx = xMin; nx <= xMax; nx++)
                    result[ny * size + nx] = true;
            }
            return result;
        }
    }
}
