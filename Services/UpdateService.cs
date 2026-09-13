using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace Myria.Wpf.Services
{
    public enum UpdateStatus
    {
        Checking, Downloading, LaunchingInstaller, UpToDate, Failed,
        /// <summary>A compat-tier-differing update was found and is awaiting the user's decision
        /// via the callback passed to <see cref="UpdateService.CheckForUpdatesAsync"/> — see
        /// <see cref="UpdateProgress.PendingVersion"/>/<see cref="UpdateProgress.PendingNotes"/>.</summary>
        PendingConfirmation,
        /// <summary>The user declined a compat-tier-differing update. Distinct from
        /// <see cref="Failed"/> since nothing went wrong — this was a deliberate choice.</summary>
        Declined
    }

    public record UpdateProgress(
        UpdateStatus Status,
        double? PercentComplete = null,
        string? PendingVersion = null,
        string? PendingNotes = null);

    /// <summary>
    /// Checks this repo's own GitHub Releases for a newer alpha build and reinstalls if found.
    /// Mirrors ServerApiService's static-HttpClient, swallow-all-exceptions pattern - a failed or
    /// slow check must never block or crash startup. Every step is logged to Data/Misc/update.log
    /// so a silent failure (e.g. an AV false-positive deleting the downloaded installer) is at
    /// least diagnosable after the fact, since nothing about a failed check is ever shown in-app.
    ///
    /// Previously this checked a hand-maintained version.json in a separate rllyben/MyriaRPG-releases
    /// repo. Now that releases are published directly on this repo (MyriaGames/MyriaRPG), the
    /// GitHub Releases API is the source of truth instead - no separate manifest file to keep in
    /// sync. The release's tag name (e.g. "v0.2.15" or "0.2.15") is the version, and the installer
    /// is whichever release asset looks like "MyriaRPG_Setup*.exe" (the exact filename varies by
    /// version - see Setup/release.ps1).
    ///
    /// v0.3+ versioning scheme: a version is Major.Minor.CompatBreak.Patch (mapped onto .NET's
    /// Major/Minor/Build/Revision). An update whose Major.Minor.CompatBreak (the "mod-compat
    /// version") matches the currently-installed one is Patch-only and safe for mods/other clients
    /// to ignore - that case installs silently, exactly like before this scheme existed. An update
    /// that changes the mod-compat version might break mods, so it's held for the caller to confirm
    /// via <paramref name="confirmCompatBreakUpdate"/> before anything is downloaded.
    /// </summary>
    public static class UpdateService
    {
        private const string LatestReleaseApiUrl =
            "https://api.github.com/repos/MyriaGames/MyriaRPG/releases/latest";

        private static readonly HttpClient _http = CreateHttpClient();
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            // The GitHub API rejects requests with no User-Agent (unlike raw.githubusercontent.com,
            // which the old version.json-based check used and didn't need one for).
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyriaRPG-UpdateChecker");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        /// <summary>Returns true if an installer was launched and the caller should stop its own
        /// startup immediately (Shutdown() has already been requested internally).</summary>
        /// <param name="confirmCompatBreakUpdate">Called only when the available update changes
        /// the mod-compat version (Major.Minor.CompatBreak) — awaited before anything is
        /// downloaded. Receives the new version string and the release's notes (may be null/empty)
        /// and should return true to proceed, false to skip this update for now. Same-compat-tier
        /// (Patch-only) updates never call this - they install silently as before.</param>
        public static async Task<bool> CheckForUpdatesAsync(
            IProgress<UpdateProgress>? progress = null,
            Func<string, string?, Task<bool>>? confirmCompatBreakUpdate = null)
        {
            try
            {
                Log("Checking for updates...");
                progress?.Report(new UpdateProgress(UpdateStatus.Checking));

                var release = await _http.GetFromJsonAsync<GitHubRelease>(LatestReleaseApiUrl, _jsonOpts);
                var installerAsset = release?.Assets?.FirstOrDefault(a =>
                    a.Name is not null &&
                    a.Name.StartsWith("MyriaRPG_Setup", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

                if (release is null || string.IsNullOrWhiteSpace(release.TagName) || installerAsset?.BrowserDownloadUrl is null)
                {
                    Log("Latest release missing, has no tag, or has no matching installer asset - aborting.");
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }

                var installerUrl = installerAsset.BrowserDownloadUrl;
                var versionText = release.TagName.TrimStart('v', 'V');
                if (!Version.TryParse(versionText, out var latest))
                {
                    Log($"Could not parse release tag '{release.TagName}' as a version - aborting.");
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }

                var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
                Log($"Current version: {current}, latest available: {latest}");
                if (latest <= current)
                {
                    Log("Already up to date.");
                    progress?.Report(new UpdateProgress(UpdateStatus.UpToDate));
                    return false;
                }

                bool sameCompatTier = latest.Major == current.Major
                    && latest.Minor == current.Minor
                    && latest.Build == current.Build;
                if (!sameCompatTier)
                {
                    Log($"Update changes the mod-compat version ({current.Major}.{current.Minor}.{current.Build} -> " +
                        $"{latest.Major}.{latest.Minor}.{latest.Build}) - asking before installing.");
                    progress?.Report(new UpdateProgress(UpdateStatus.PendingConfirmation,
                        PendingVersion: versionText, PendingNotes: release.Body));

                    bool confirmed = confirmCompatBreakUpdate is null
                        || await confirmCompatBreakUpdate(versionText, release.Body);
                    if (!confirmed)
                    {
                        Log("User declined the compat-breaking update.");
                        progress?.Report(new UpdateProgress(UpdateStatus.Declined));
                        return false;
                    }
                    Log("User confirmed the compat-breaking update - proceeding.");
                }

                // Suffix with the current process id so two overlapping checks (e.g. a stray
                // second app instance launched before the first fully exits - there's no
                // single-instance guard) never race on the exact same temp file. Downloading
                // to a temp name and only publishing it under its real name once complete also
                // means a half-written file is never mistaken for a finished download.
                var installerPath = Path.Combine(Path.GetTempPath(),
                    $"MyriaRPG_Setup_{versionText}_{Environment.ProcessId}.exe");
                Log($"Downloading {installerUrl} -> {installerPath}");
                progress?.Report(new UpdateProgress(UpdateStatus.Downloading, 0));

                using (var response = await _http.GetAsync(installerUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength;

                    await using var httpStream = await response.Content.ReadAsStreamAsync();
                    await using var file = File.Create(installerPath);

                    var buffer = new byte[81920];
                    long bytesRead = 0;
                    int read;
                    while ((read = await httpStream.ReadAsync(buffer)) > 0)
                    {
                        await file.WriteAsync(buffer.AsMemory(0, read));
                        bytesRead += read;
                        double? percent = totalBytes is > 0 ? bytesRead * 100.0 / totalBytes.Value : null;
                        progress?.Report(new UpdateProgress(UpdateStatus.Downloading, percent));
                    }
                }

                if (!File.Exists(installerPath))
                {
                    // Downloaded successfully but vanished before we could run it - almost
                    // certainly antivirus quarantining/deleting the file (known issue with
                    // unsigned self-contained single-file .NET apps).
                    Log("Downloaded installer no longer exists on disk (likely quarantined by antivirus) - aborting.");
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }

                Log("Download complete, launching installer and shutting down.");
                progress?.Report(new UpdateProgress(UpdateStatus.LaunchingInstaller));
                Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                    UseShellExecute = true
                });

                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
                return true;
            }
            catch (Exception ex)
            {
                // Never let a failed/offline update check disrupt the running game.
                Log($"Update check failed: {ex}");
                progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                return false;
            }
        }

        private static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory("Data/Misc");
                File.AppendAllText("Data/Misc/update.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Logging must never itself throw.
            }
        }

        // Minimal subset of GitHub's "Get the latest release" API response
        // (https://docs.github.com/en/rest/releases/releases#get-the-latest-release) - only the
        // fields this class actually reads.
        private sealed class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }
            public string? Body { get; set; }
            public List<GitHubReleaseAsset>? Assets { get; set; }
        }

        private sealed class GitHubReleaseAsset
        {
            public string? Name { get; set; }
            [JsonPropertyName("browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }
        }
    }
}
