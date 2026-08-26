namespace Myria.Wpf.Utils;

/// <summary>
/// Converts plain Latin text containing Myriac digraphs (Ch, Sch) into the
/// Private Use Area codepoints used by MyriacScript.ttf.
///
/// Mapping (matches make_myriac_font.py):
///   Capital Ch  -> U+E000    small ch  -> U+E001
///   Capital Sch -> U+E002    small sch -> U+E003
/// </summary>
public static class MyriacEncoder
{
    private static readonly string CapitalCh  = ((char)0xE000).ToString();
    private static readonly string SmallCh    = ((char)0xE001).ToString();
    private static readonly string CapitalSch = ((char)0xE002).ToString();
    private static readonly string SmallSch   = ((char)0xE003).ToString();

    public static string Encode(string text)
    {
        // Sch must be substituted before Ch to avoid partial matches
        return text
            .Replace("Sch", CapitalSch)
            .Replace("sch", SmallSch)
            .Replace("Ch",  CapitalCh)
            .Replace("ch",  SmallCh);
    }
}
