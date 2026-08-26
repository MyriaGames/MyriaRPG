using Myria.Lib.Core.Services;
using System.IO;
using System.Windows.Ink;

namespace Myria.Wpf.Services.Rune
{
    /// <summary>
    /// Matches ink strokes against the Myriac script letter templates.
    ///
    /// Both template masks and drawn masks are pre-processed to "fat-band outlines"
    /// (edge-extracted + dilated) before comparison, so Jaccard similarity works even
    /// though the SVG templates are solid fills and the player draws thin ink strokes.
    ///
    /// The threshold is intentionally lower than a naive pixel-comparison threshold
    /// because outline bands are sparser than filled silhouettes.
    /// </summary>
    public class RuneLetterRecognizer
    {
        // Outline-band Jaccard is sparser than fill Jaccard — empirically calibrate
        // this value while testing: start at 0.18 and raise if too many false positives.
        public const float RecognitionThreshold = 0.58f;

        // Primary: copied to output via .csproj. Fallback: solution root during development.
        private static readonly string DefaultSvgFolder = LocateSvgFolder();

        private static string LocateSvgFolder()
        {
            var copied = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MyriacScript");
            if (Directory.Exists(copied)) return copied;
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Myriac Script SVG"));
        }

        private readonly Dictionary<string, bool[]> _templates;

        public RuneLetterRecognizer(string? svgFolder = null)
        {
            var folder = svgFolder ?? DefaultSvgFolder;
            _templates = SvgLetterParser.LoadTemplates(folder);
        }

        /// <summary>Number of letter templates successfully loaded.</summary>
        public int TemplateCount => _templates.Count;

        // ── Letter recognition ────────────────────────────────────────────────────

        /// <summary>
        /// Compares the ink strokes against all letter templates.
        /// Returns the best matching letter key and confidence, or null if below threshold.
        /// Letter keys match SVG names: "A"–"Z", "a"–"z", "Ch", "ch", "Sch", "sch".
        /// </summary>
        public (string letter, float confidence)? RecognizeLetter(StrokeCollection strokes)
        {
            if (strokes.Count == 0 || _templates.Count == 0)
                return null;

            var inkMask = SvgLetterParser.RenderStrokesToMask(strokes);

            float bestScore = 0f;
            string? bestLetter = null;

            foreach (var (letter, template) in _templates)
            {
                float score = Jaccard(inkMask, template);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestLetter = letter;
                }
            }

            return bestScore >= RecognitionThreshold ? (bestLetter!, bestScore) : null;
        }

        /// <summary>
        /// Returns every template's Jaccard score against the given strokes, sorted descending.
        /// Useful for calibrating the threshold: call from a debug button, log to Output.
        /// </summary>
        public IReadOnlyList<(string letter, float score)> DebugScores(StrokeCollection strokes)
        {
            if (strokes.Count == 0 || _templates.Count == 0)
                return Array.Empty<(string, float)>();

            var inkMask = SvgLetterParser.RenderStrokesToMask(strokes);
            return _templates
                .Select(kv => (kv.Key, Jaccard(inkMask, kv.Value)))
                .OrderByDescending(t => t.Item2)
                .ToList();
        }

        // ── Word-to-rune lookup ───────────────────────────────────────────────────

        /// <summary>
        /// Looks up a spelled word (list of recognized letter keys joined as a string)
        /// against the rune word data.
        /// Returns the matching <see cref="Myria.Lib.Core.Models.RuneWord"/> or null.
        /// Comparison is case-insensitive.
        /// </summary>
        public static Myria.Lib.Core.Models.RuneWord? FindWordBySpelling(IEnumerable<string> letterKeys)
        {
            var spelled = string.Join("", letterKeys);
            return RuneWordService.AllWords.Values
                .FirstOrDefault(w => w.RunicScript.Equals(spelled, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Looks up a spelled word against base rune definitions.
        /// Returns the matching <see cref="Myria.Lib.Core.Models.BaseRuneData"/> or null.
        /// </summary>
        public static Myria.Lib.Core.Models.BaseRuneData? FindBaseRuneBySpelling(IEnumerable<string> letterKeys)
        {
            var spelled = string.Join("", letterKeys);
            // BaseRuneData.Name holds the Myriac word in Latin characters
            foreach (var word in RuneWordService.AllWords.Values)
            {
                if (!word.RunicScript.Equals(spelled, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if this word is the core word of any base rune
                return Myria.Lib.Core.Services.Builder.BaseRuneService.GetAll()
                    .FirstOrDefault(r => r.CoreWordId.Equals(word.Id, StringComparison.OrdinalIgnoreCase));
            }
            return null;
        }

        // ── Similarity metric ─────────────────────────────────────────────────────

        private static float Jaccard(bool[] a, bool[] b)
        {
            int intersection = 0, union = 0;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                if (a[i] && b[i]) intersection++;
                if (a[i] || b[i]) union++;
            }
            return union == 0 ? 0f : (float)intersection / union;
        }
    }
}
