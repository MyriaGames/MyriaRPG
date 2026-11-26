using Myria.Wpf.Services;
using Xunit;

namespace Myria.Wpf.Tests;

/// <summary>
/// Security review 2026-11-26, A08 F3: the installer's own (signed) version must be the announced
/// one and newer than the running client - no silent downgrade by relabelling an older installer.
/// </summary>
public class UpdateIntegrityTests
{
    private static readonly Version Running = new(0, 2, 24, 0);

    [Theory]
    [InlineData("0.2.25", "0.2.25                                            ")]   // Inno pads ProductVersion
    [InlineData("0.2.25", "0.2.25.0")]
    [InlineData("0.3", "0.3.0")]
    public void MatchingNewerInstaller_IsAccepted(string manifest, string installer) =>
        Assert.True(UpdateService.IsAcceptableInstallerVersion(manifest, installer, Running, out _));

    [Fact]
    public void OldInstaller_RelabelledAsNewer_IsRejected()
    {
        Assert.False(UpdateService.IsAcceptableInstallerVersion("9.9.9", "0.2.22", Running, out var problem));
        Assert.Contains("was announced", problem);
    }

    [Fact]
    public void InstallerNotNewerThanTheClient_IsRejected()
    {
        Assert.False(UpdateService.IsAcceptableInstallerVersion("0.2.24", "0.2.24", Running, out var problem));
        Assert.Contains("not newer", problem);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("v0.2.25")]
    public void InstallerWithoutUsableVersion_IsRejected(string? installer) =>
        Assert.False(UpdateService.IsAcceptableInstallerVersion("0.2.25", installer, Running, out _));
}
