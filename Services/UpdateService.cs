using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Myria.Wpf.Services
{
    public enum UpdateStatus { Checking, Downloading, LaunchingInstaller, UpToDate, Failed }

    public record UpdateProgress(UpdateStatus Status, double? PercentComplete = null);

    /// <summary>
    /// Checks a public "releases" repo for a newer alpha build and silently reinstalls if found.
    /// Mirrors ServerApiService's static-HttpClient, swallow-all-exceptions pattern - a failed
    /// or slow check must never block or crash startup. Every step is logged to Data/Misc/update.log
    /// so a silent failure (e.g. an AV false-positive deleting the downloaded installer) is at
    /// least diagnosable after the fact, since nothing about a failed check is ever shown in-app.
    /// </summary>
    public static class UpdateService
    {
        private const string ManifestUrl =
            "https://raw.githubusercontent.com/rllyben/MyriaRPG-releases/main/version.json";

        private static readonly HttpClient _http = new();
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

        /// <summary>Returns true if an installer was launched and the caller should stop its own
        /// startup immediately (Shutdown() has already been requested internally).</summary>
        public static async Task<bool> CheckForUpdatesAsync(IProgress<UpdateProgress>? progress = null)
        {
            try
            {
                Log("Checking for updates...");
                progress?.Report(new UpdateProgress(UpdateStatus.Checking));

                var manifest = await _http.GetFromJsonAsync<VersionManifest>(ManifestUrl, _jsonOpts);
                if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(manifest.InstallerUrl))
                {
                    Log("Manifest missing or incomplete - aborting.");
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }

                if (!Version.TryParse(manifest.Version, out var latest))
                {
                    Log($"Could not parse manifest version '{manifest.Version}' - aborting.");
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

                // Suffix with the current process id so two overlapping checks (e.g. a stray
                // second app instance launched before the first fully exits - there's no
                // single-instance guard) never race on the exact same temp file. Downloading
                // to a temp name and only publishing it under its real name once complete also
                // means a half-written file is never mistaken for a finished download.
                var installerPath = Path.Combine(Path.GetTempPath(),
                    $"MyriaRPG_Setup_{manifest.Version}_{Environment.ProcessId}.exe");
                Log($"Downloading {manifest.InstallerUrl} -> {installerPath}");
                progress?.Report(new UpdateProgress(UpdateStatus.Downloading, 0));

                using (var response = await _http.GetAsync(manifest.InstallerUrl, HttpCompletionOption.ResponseHeadersRead))
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

        private sealed class VersionManifest
        {
            public string? Version { get; set; }
            public string? InstallerUrl { get; set; }
            public string? Notes { get; set; }
        }
    }
}
