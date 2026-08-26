namespace Myria.Wpf.Model
{
    public enum RuneDrawResultType
    {
        /// <summary>Drawn word matched a rune and the word is in the player's lexica.</summary>
        KnownRune,
        /// <summary>Drawn word matched a rune but the word is NOT in the player's lexica.</summary>
        UnknownRune,
        /// <summary>Drawn word did not match any known Myriac word above the confidence threshold.</summary>
        NoMatch,
    }

    public class RuneDrawResult
    {
        public RuneDrawResultType Type { get; init; }

        /// <summary>Set for KnownRune and UnknownRune — the matched base rune ID.</summary>
        public string? BaseRuneId { get; init; }

        /// <summary>Set only for KnownRune — the display name of the matched rune.</summary>
        public string? RuneName { get; init; }

        /// <summary>The spelled word as the player drew it (joined letter sequence).</summary>
        public string SpelledWord { get; init; } = "";
    }
}
