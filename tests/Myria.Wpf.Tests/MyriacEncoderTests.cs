using Myria.Wpf.Utils;
using Xunit;

namespace Myria.Wpf.Tests;

/// <summary>
/// MyriacEncoder rewrites Latin text into the Private Use Area codepoints that MyriacScript.ttf
/// draws as single Myriac glyphs (used for in-world NPC script — see Page_GeneralNpcInteraction).
///
/// The whole method is four chained Replace calls, and their ORDER is the entire correctness
/// argument: "sch" contains "ch", so if "ch" were substituted first, "sch" would become
/// "s" + &lt;ch-glyph&gt; and the three-letter glyph could never match. The source says as much in a
/// comment; these tests are what stop a later tidy-up from reordering the lines and breaking a
/// script no reviewer can read by eye.
/// </summary>
public class MyriacEncoderTests
{
    // Spelled out as codepoints rather than pasted literals: these live in the Private Use Area,
    // so a literal renders as a placeholder box in most editors and is liable to be stripped by any
    // tool that rewrites the file's encoding (it was, the first time this file was written). The
    // values match make_myriac_font.py.
    private static readonly string CapitalCh = ((char)0xE000).ToString();
    private static readonly string SmallCh = ((char)0xE001).ToString();
    private static readonly string CapitalSch = ((char)0xE002).ToString();
    private static readonly string SmallSch = ((char)0xE003).ToString();

    [Fact]
    public void Encode_MapsTheChDigraphs()
    {
        Assert.Equal(CapitalCh, MyriacEncoder.Encode("Ch"));
        Assert.Equal(SmallCh, MyriacEncoder.Encode("ch"));
    }

    [Fact]
    public void Encode_MapsTheSchTrigraphs()
    {
        Assert.Equal(CapitalSch, MyriacEncoder.Encode("Sch"));
        Assert.Equal(SmallSch, MyriacEncoder.Encode("sch"));
    }

    /// <summary>
    /// The ordering guard. "sch" must come out as the single sch glyph — not as "s" followed by the
    /// ch glyph, which is what substituting "ch" first would produce.
    /// </summary>
    [Fact]
    public void Encode_PrefersTheSchGlyphOverSplittingIntoSPlusCh()
    {
        Assert.Equal(SmallSch, MyriacEncoder.Encode("sch"));
        Assert.DoesNotContain(SmallCh, MyriacEncoder.Encode("sch"));
        Assert.DoesNotContain("s", MyriacEncoder.Encode("sch"));

        Assert.Equal(CapitalSch, MyriacEncoder.Encode("Sch"));
        Assert.DoesNotContain(CapitalCh, MyriacEncoder.Encode("Sch"));
    }

    [Fact]
    public void Encode_HandlesBothDigraphsInOneWord()
    {
        // "Schach" -> Sch + a + ch
        Assert.Equal(CapitalSch + "a" + SmallCh, MyriacEncoder.Encode("Schach"));
    }

    /// <summary>Case is preserved as a distinct glyph — capital and small forms must not collapse.</summary>
    [Fact]
    public void Encode_KeepsCapitalAndSmallFormsDistinct()
    {
        Assert.NotEqual(MyriacEncoder.Encode("Ch"), MyriacEncoder.Encode("ch"));
        Assert.NotEqual(MyriacEncoder.Encode("Sch"), MyriacEncoder.Encode("sch"));
    }

    [Fact]
    public void Encode_LeavesTextWithNoDigraphsUnchanged()
    {
        Assert.Equal("Myria", MyriacEncoder.Encode("Myria"));
        Assert.Equal("", MyriacEncoder.Encode(""));
    }

    [Fact]
    public void Encode_LeavesSurroundingCharactersIntact()
    {
        Assert.Equal("Wel" + SmallCh + "e Wahl?", MyriacEncoder.Encode("Welche Wahl?"));
    }

    [Fact]
    public void Encode_ReplacesEveryOccurrence()
    {
        Assert.Equal(SmallCh + SmallCh + SmallCh, MyriacEncoder.Encode("chchch"));
    }

    /// <summary>
    /// KNOWN LIMITATION, pinned as current behaviour.
    ///
    /// A mixed-case "SCH" or "CH" matches nothing — only the exact "Sch"/"sch"/"Ch"/"ch" forms are
    /// substituted, so all-caps text passes through as plain Latin and renders in the fallback font
    /// rather than the Myriac script. Fine while the source strings are normal prose; worth knowing
    /// before anyone adds an upper-cased NPC name or a shouted line.
    /// </summary>
    [Fact]
    public void Encode_DoesNotMatchAllCapsForms_KnownLimitation()
    {
        Assert.Equal("SCH", MyriacEncoder.Encode("SCH"));
        Assert.Equal("CH", MyriacEncoder.Encode("CH"));
    }

    /// <summary>
    /// Running the encoder on already-encoded output must be a no-op — the PUA codepoints contain
    /// no Latin "ch" for a second pass to find. Guards against double-encoding if a value ever
    /// flows through the encoder twice.
    /// </summary>
    [Fact]
    public void Encode_IsANoOpOnAlreadyEncodedText()
    {
        var once = MyriacEncoder.Encode("Schach");

        Assert.Equal(once, MyriacEncoder.Encode(once));
    }
}
