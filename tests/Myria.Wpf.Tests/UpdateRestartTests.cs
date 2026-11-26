using System.IO;
using Myria.Wpf.Services;
using Xunit;

namespace Myria.Wpf.Tests;

/// <summary>
/// The installer restarts the game after an auto-update; the game must not loop on an update that
/// didn't install, and Windows' Restart Manager must not start the old version early.
/// </summary>
public class UpdateRestartTests
{
    private static readonly Version Latest = new(0, 2, 25);
    private static readonly DateTime Now = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    private static string Pending(string version, DateTime launchedUtc) => $"{version}|{launchedUtc:O}";

    [Fact]
    public void NoPendingUpdate_Updates() =>
        Assert.True(UpdateService.ShouldAttemptUpdate(null, Latest, Now, out _));

    [Fact]
    public void SameUpdateLaunchedMomentsAgo_StillOld_DoesNotLoop()
    {
        Assert.False(UpdateService.ShouldAttemptUpdate(Pending("0.2.25", Now.AddMinutes(-1)), Latest, Now, out var reason));
        Assert.Contains("didn't install", reason);
    }

    [Fact]
    public void SameUpdate_AfterTheCooldown_IsRetried() =>
        Assert.True(UpdateService.ShouldAttemptUpdate(Pending("0.2.25", Now - UpdateService.RetryCooldown), Latest, Now, out _));

    [Fact]
    public void NewerReleaseThanThePendingOne_IsInstalledRightAway() =>
        Assert.True(UpdateService.ShouldAttemptUpdate(Pending("0.2.24", Now.AddMinutes(-1)), Latest, Now, out _));

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("0.2.25")]
    [InlineData("0.2.25|not-a-date")]
    public void UnreadablePendingFile_DoesNotBlockUpdates(string pending) =>
        Assert.True(UpdateService.ShouldAttemptUpdate(pending, Latest, Now, out _));

    [Fact]
    public void ClockMovedBackwards_DoesNotBlockUpdates() =>
        Assert.True(UpdateService.ShouldAttemptUpdate(Pending("0.2.25", Now.AddHours(2)), Latest, Now, out _));

    [Fact]
    public void Installer_IsSilent_AndRestartManagerDoesNotRelaunchTheOldVersion()
    {
        Assert.Contains("/VERYSILENT", UpdateService.InstallerArguments);
        Assert.Contains("/NORESTARTAPPLICATIONS", UpdateService.InstallerArguments);
        Assert.DoesNotContain("/RESTARTAPPLICATIONS ", UpdateService.InstallerArguments + " ");
    }

    [Fact]
    public void InstallerScript_StartsTheGameAfterASilentInstall()
    {
        var iss = File.ReadAllText(FindRepoFile(Path.Combine("Setup", "myriaRPG_Setupscript.iss")));
        Assert.Matches(@"(?m)^Filename: ""\{app\}\\{#MyAppExeName\}"";.*Check: WizardSilent", iss);
    }

    private static string FindRepoFile(string relative)
    {
        // The tests copy sits next to the Myria.Wpf worktree: walk up until Myria.Wpf/<relative> exists.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Myria.Wpf", relative);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException(relative);
    }
}
