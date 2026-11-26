using Myria.Wpf.Services;
using Xunit;

namespace Myria.Wpf.Tests;

/// <summary>Security review 2026-11-26, A04 F1: the official server never falls back to trust-on-first-use.</summary>
public class CertificateTrustTests
{
    [Fact]
    public void OfficialServer_IsCaOnly() =>
        Assert.Contains("myria.myria-games.duckdns.org", ServerApiService.CaOnlyHosts);

    [Fact]
    public void UpdateServer_IsTheSameCaOnlyHost() =>
        Assert.Contains(new Uri(UpdateService.ReleasesBaseUrl).Host, ServerApiService.CaOnlyHosts);
}
