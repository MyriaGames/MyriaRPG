using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Myria.Wpf.Services;
using Myria.Wpf.View.Converters;
using Xunit;

namespace Myria.Wpf.Tests;

/// <summary>
/// These converters are bound from XAML (Page_PlayerShop, Page_QuestList, Assets/Layout), which
/// means nothing type-checks their inputs — WPF hands them whatever the binding produced, including
/// null and wrong types when a binding path fails to resolve. Their fallback behaviour is therefore
/// load-bearing: a converter that throws on unexpected input surfaces as a silent binding failure
/// or a blank control, with the real cause buried in the WPF trace output.
///
/// So each converter is checked for both its intended mapping and its behaviour on the junk WPF
/// can legitimately pass it.
/// </summary>
public class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    // ---- StringToVisibilityConverter ---------------------------------------------------------
    // Shows an element when the bound value matches the ConverterParameter; a "!" prefix inverts.

    private static object Visible(object? value, object? parameter) =>
        new StringToVisibilityConverter().Convert(value!, typeof(Visibility), parameter!, Culture);

    [Fact]
    public void StringToVisibility_ShowsOnAMatch()
    {
        Assert.Equal(Visibility.Visible, Visible("Shop", "Shop"));
    }

    [Fact]
    public void StringToVisibility_CollapsesOnAMismatch()
    {
        Assert.Equal(Visibility.Collapsed, Visible("Shop", "Craft"));
    }

    /// <summary>
    /// Comparison is case-insensitive, so a XAML parameter's casing doesn't have to match the
    /// value's exactly — worth pinning because tightening it would silently hide UI.
    /// </summary>
    [Fact]
    public void StringToVisibility_IgnoresCase()
    {
        Assert.Equal(Visibility.Visible, Visible("shop", "SHOP"));
    }

    [Fact]
    public void StringToVisibility_InvertsWithABangPrefix()
    {
        Assert.Equal(Visibility.Collapsed, Visible("Shop", "!Shop"));
        Assert.Equal(Visibility.Visible, Visible("Craft", "!Shop"));
    }

    /// <summary>
    /// A null value or a missing ConverterParameter collapses the element. That's the safe default
    /// — showing an element because a binding failed is worse than hiding one.
    /// </summary>
    [Fact]
    public void StringToVisibility_CollapsesWhenValueOrParameterIsMissing()
    {
        Assert.Equal(Visibility.Collapsed, Visible(null, "Shop"));
        Assert.Equal(Visibility.Collapsed, Visible("Shop", null));
        Assert.Equal(Visibility.Collapsed, Visible(null, null));
    }

    /// <summary>
    /// KNOWN EDGE CASE, pinned as current behaviour.
    ///
    /// A bare "!" parameter strips to an empty target, which no real value equals, so the inverted
    /// branch always reports Visible. Harmless today (nothing binds "!" alone) but it means "!" is
    /// silently a no-op rather than an error if someone ever writes it by mistake.
    /// </summary>
    [Fact]
    public void StringToVisibility_TreatsABareBangAsAlwaysVisible()
    {
        Assert.Equal(Visibility.Visible, Visible("anything", "!"));
    }

    // ---- RarityToBrushConverter ---------------------------------------------------------------
    // Colours item names by rarity. Its brushes are settable from XAML, so the defaults only apply
    // when a style doesn't override them.

    private static object Brush(object? value) =>
        new RarityToBrushConverter().Convert(value!, typeof(Brush), null!, Culture);

    [Theory]
    [InlineData("Uncommon")]
    [InlineData("Rare")]
    [InlineData("Epic")]
    [InlineData("Unique")]
    [InlineData("Legendary")]
    [InlineData("Godly")]
    public void RarityToBrush_GivesEachRarityItsOwnBrush(string rarity)
    {
        var converter = new RarityToBrushConverter();

        var brush = converter.Convert(rarity, typeof(Brush), null!, Culture);

        Assert.NotSame(converter.Common, brush);
    }

    /// <summary>
    /// Every rarity must map to a distinct brush — two rarities sharing a colour makes the item
    /// list misleading, and is exactly what a copy-paste slip in the switch would produce.
    /// </summary>
    [Fact]
    public void RarityToBrush_MapsEveryRarityToADistinctBrush()
    {
        var converter = new RarityToBrushConverter();
        string[] rarities = ["Common", "Uncommon", "Rare", "Epic", "Unique", "Legendary", "Godly"];

        var brushes = rarities
            .Select(r => converter.Convert(r, typeof(Brush), null!, Culture))
            .ToList();

        Assert.Equal(rarities.Length, brushes.Distinct().Count());
    }

    /// <summary>
    /// An unknown or missing rarity falls back to Common rather than throwing or returning null —
    /// a null Brush would leave the text invisible against the background.
    /// </summary>
    [Fact]
    public void RarityToBrush_FallsBackToCommonForUnknownOrMissingRarity()
    {
        var converter = new RarityToBrushConverter();

        Assert.Same(converter.Common, converter.Convert(null!, typeof(Brush), null!, Culture));
        Assert.Same(converter.Common, converter.Convert("NotARarity", typeof(Brush), null!, Culture));
        Assert.Same(converter.Common, converter.Convert("Common", typeof(Brush), null!, Culture));
    }

    /// <summary>
    /// Matching is case-sensitive: "rare" does NOT resolve to the Rare brush. Pinned because the
    /// rarity strings in Data/common are capitalised and any lowercase source would silently show
    /// as Common rather than failing loudly.
    /// </summary>
    [Fact]
    public void RarityToBrush_IsCaseSensitive()
    {
        var converter = new RarityToBrushConverter();

        Assert.Same(converter.Common, converter.Convert("rare", typeof(Brush), null!, Culture));
        Assert.Same(converter.Rare, converter.Convert("Rare", typeof(Brush), null!, Culture));
    }

    // ---- NotFriendToVisibilityConverter -------------------------------------------------------
    // Hides "Send Friend Request" for someone already on the friends list. The match path needs a
    // real ViewModel_PageGame (which can't be constructed outside a running app), so only the
    // fallback is covered here — that's the branch that decides what happens when a binding breaks.

    /// <summary>
    /// On any unexpected input the menu item stays Visible. That's the deliberate safe default: a
    /// redundant "add friend" option is a much smaller problem than the option vanishing for
    /// everyone because a binding stopped resolving.
    /// </summary>
    [Fact]
    public void NotFriend_StaysVisibleWhenTheBindingDoesNotResolve()
    {
        var converter = new NotFriendToVisibilityConverter();

        Assert.Equal(Visibility.Visible, converter.Convert([], typeof(Visibility), null!, Culture));
        Assert.Equal(Visibility.Visible, converter.Convert(["only-one"], typeof(Visibility), null!, Culture));
        Assert.Equal(Visibility.Visible, converter.Convert([null!, "Bob"], typeof(Visibility), null!, Culture));
        Assert.Equal(Visibility.Visible, converter.Convert(["not-a-vm", 42], typeof(Visibility), null!, Culture));
    }

    // ---- ConvertBack ---------------------------------------------------------------------------

    /// <summary>
    /// All of these are one-way display converters. ConvertBack must keep throwing: silently
    /// returning something would let a TwoWay binding write nonsense back into a view model, which
    /// is far harder to diagnose than an immediate exception.
    /// </summary>
    [Fact]
    public void ConvertBack_IsRefusedOnEveryDisplayConverter()
    {
        Assert.ThrowsAny<Exception>(() =>
            new StringToVisibilityConverter().ConvertBack(Visibility.Visible, typeof(string), null!, Culture));
        Assert.ThrowsAny<Exception>(() =>
            new RarityToBrushConverter().ConvertBack(null!, typeof(string), null!, Culture));
        Assert.ThrowsAny<Exception>(() =>
            new NotFriendToVisibilityConverter().ConvertBack(
                Visibility.Visible, [typeof(object), typeof(string)], null!, Culture));
    }
}
