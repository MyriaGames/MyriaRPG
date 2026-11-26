using Myria.Wpf.Services;
using Xunit;

namespace Myria.Wpf.Tests;

/// <summary>
/// NormalizeAddress turns whatever a player types into the server-address box into a usable URL.
/// Its security-relevant rule is the scheme default: anything that isn't loopback must come out
/// as https, because the very next thing sent to that address is a login request carrying the
/// player's password. A regression that flipped the default to http would still "work" in every
/// manual test against localhost and silently leak credentials for every remote server.
///
/// This suite is deliberately identical to Myria.Console.Tests.NormalizeAddressTests: the two
/// clients each carry their own independent copy of this method, and the source comments on both
/// say they mirror each other. Keeping one suite per copy is what actually enforces that — change
/// one implementation without the other and exactly one of the two suites goes red.
/// </summary>
public class NormalizeAddressTests
{
    [Theory]
    [InlineData("localhost:5001", "http://localhost:5001")]
    [InlineData("127.0.0.1:5001", "http://127.0.0.1:5001")]
    [InlineData("localhost", "http://localhost")]
    [InlineData("127.0.0.1", "http://127.0.0.1")]
    public void UsesPlainHttpForLoopbackAddresses(string input, string expected)
    {
        Assert.Equal(expected, ServerApiService.NormalizeAddress(input));
    }

    /// <summary>
    /// KNOWN BUG, pinned as current behaviour rather than as the intent.
    ///
    /// The implementation lists "::1" as a loopback host, but that branch can never be reached:
    /// the host is extracted with <c>address.Split('/')[0].Split(':')[0]</c>, and an IPv6 literal
    /// is nothing but colons — "::1" splits to an empty string, which matches no loopback name. So
    /// the IPv6 loopback is classified as remote and gets https, and a player pointing either
    /// client at a local dev server over "::1" fails to connect (the dev server serves plain HTTP).
    ///
    /// Low impact — "localhost" and "127.0.0.1" both work, and those are what the UI suggests — but
    /// the "::1" entry in the source reads as though it is handled when it is not. Present in both
    /// clients identically. When it is fixed, move these cases up into the loopback theory above
    /// in this file and in Myria.Console.Tests.
    /// </summary>
    [Theory]
    [InlineData("::1:5001", "https://::1:5001")]
    [InlineData("::1", "https://::1")]
    public void TreatsTheIpv6LoopbackAsRemote_KnownBug(string input, string expected)
    {
        Assert.Equal(expected, ServerApiService.NormalizeAddress(input));
    }

    [Theory]
    [InlineData("myria.duckdns.org:5000", "https://myria.duckdns.org:5000")]
    [InlineData("example.com", "https://example.com")]
    [InlineData("192.168.1.10:5001", "https://192.168.1.10:5001")]
    [InlineData("myria.example.com/realm-one", "https://myria.example.com/realm-one")]
    public void DefaultsToHttpsForEverythingElse(string input, string expected)
    {
        Assert.Equal(expected, ServerApiService.NormalizeAddress(input));
    }

    /// <summary>
    /// "localhost.evil.com" merely starts with the loopback name — it is a remote host and must
    /// get https. This is the case a naive StartsWith check would get wrong.
    /// </summary>
    [Theory]
    [InlineData("localhost.evil.com:5001", "https://localhost.evil.com:5001")]
    [InlineData("127.0.0.1.evil.com", "https://127.0.0.1.evil.com")]
    [InlineData("notlocalhost", "https://notlocalhost")]
    public void OnlyTreatsExactLoopbackNamesAsLocal(string input, string expected)
    {
        Assert.Equal(expected, ServerApiService.NormalizeAddress(input));
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("HTTP://example.com")]
    [InlineData("HtTpS://example.com")]
    public void LeavesAnExplicitSchemeAlone(string input)
    {
        Assert.Equal(input, ServerApiService.NormalizeAddress(input));
    }

    [Theory]
    [InlineData("  localhost:5001  ", "http://localhost:5001")]
    [InlineData("  example.com  ", "https://example.com")]
    [InlineData(" http://example.com ", "http://example.com")]
    public void TrimsSurroundingWhitespaceBeforeDeciding(string input, string expected)
    {
        Assert.Equal(expected, ServerApiService.NormalizeAddress(input));
    }

    /// <summary>
    /// Servers can live under a path behind a reverse proxy (e.g. "https://host/auth"). Every
    /// endpoint is appended as "/api/...", so a trailing slash must be dropped or requests would
    /// go to "//api/..." and miss both the server route and the proxy's path prefix.
    /// </summary>
    [Theory]
    [InlineData("myria.example.com/auth/", "https://myria.example.com/auth")]
    [InlineData("https://myria.example.com/realm-one/", "https://myria.example.com/realm-one")]
    [InlineData("example.com//", "https://example.com")]
    [InlineData("localhost:5050/", "http://localhost:5050")]
    [InlineData("myria.example.com/auth", "https://myria.example.com/auth")]
    public void DropsTrailingSlashesSoPathsJoinCleanly(string input, string expected)
    {
        Assert.Equal(expected, ServerApiService.NormalizeAddress(input));
    }

    /// <summary>
    /// An empty box means "use the built-in default" (see App.xaml.cs), so it must come back empty
    /// rather than as a bare scheme like "https://".
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LeavesABlankAddressBlank(string input)
    {
        Assert.Equal(string.Empty, ServerApiService.NormalizeAddress(input));
    }
}
