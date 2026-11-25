using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Myria.Wpf.Services
{
    public enum UpdateStatus { Checking, Downloading, LaunchingInstaller, UpToDate, Failed }

    public record UpdateProgress(UpdateStatus Status, double? PercentComplete = null);

    /// <summary>
    /// Checks the Myria release server for a newer alpha build and silently reinstalls if found.
    /// Mirrors ServerApiService's static-HttpClient, swallow-all-exceptions pattern - a failed or
    /// slow check must never block or crash startup. Every step is logged to Data/Misc/update.log
    /// so a silent failure (e.g. an AV false-positive deleting the downloaded installer) is at
    /// least diagnosable after the fact, since nothing about a failed check is shown in-app.
    ///
    /// Releases are published by Setup/release.ps1 to the game's own server (not GitHub):
    ///   {ReleasesBaseUrl}/latest.json             newest release: version, files, SHA-256s
    ///   {ReleasesBaseUrl}/{version}/{file name}   the files themselves
    /// The installer runs silently, so before launching it this checks BOTH the SHA-256 from the
    /// manifest and that it carries a valid Authenticode signature from the release signing
    /// certificate (AuthenticodeVerifier) - a compromised download server alone can't push code.
    /// </summary>
    public static class UpdateService
    {
        public const string ReleasesBaseUrl = "https://myria.myria-games.duckdns.org/downloads";

        /// <summary>Thumbprint of the "CN=MyriaRPG Alpha, O=Rhyen" code-signing certificate that
        /// Setup/release.ps1 signs the installer with (Setup/New-SigningCert.ps1). If that
        /// certificate is ever replaced, ship one release that pins the new thumbprint while still
        /// signed with the old one, then switch.</summary>
        public const string TrustedSignerThumbprint = "602147A12B2E604A306C769A28A041B3322AE89B";

        private static readonly HttpClient _http = CreateHttpClient();
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MyriaRPG-UpdateChecker");
            return client;
        }

        /// <summary>Returns true if an installer was launched and the caller should stop its own
        /// startup immediately (Shutdown() has already been requested internally).</summary>
        public static async Task<bool> CheckForUpdatesAsync(IProgress<UpdateProgress>? progress = null)
        {
            string? installerPath = null;
            try
            {
                Log("Checking for updates...");
                progress?.Report(new UpdateProgress(UpdateStatus.Checking));

                var manifest = await _http.GetFromJsonAsync<ReleaseManifest>($"{ReleasesBaseUrl}/latest.json", _jsonOpts);
                if (!TryGetInstaller(manifest, out var latest, out var installer, out var problem))
                {
                    Log($"Release manifest unusable ({problem}) - aborting.");
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }

                var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
                Log($"Current version: {current}, latest available: {latest}");
                if (!IsNewer(latest, current))
                {
                    Log("Already up to date.");
                    progress?.Report(new UpdateProgress(UpdateStatus.UpToDate));
                    return false;
                }

                var installerUrl = $"{ReleasesBaseUrl}/{Uri.EscapeDataString(manifest!.Version!)}/{Uri.EscapeDataString(installer.Name!)}";
                // Suffix with the current process id so two overlapping checks (e.g. a stray
                // second app instance launched before the first fully exits - there's no
                // single-instance guard) never race on the exact same temp file.
                installerPath = Path.Combine(Path.GetTempPath(),
                    $"MyriaRPG_Setup_{manifest.Version}_{Environment.ProcessId}.exe");
                Log($"Downloading {installerUrl} -> {installerPath}");
                progress?.Report(new UpdateProgress(UpdateStatus.Downloading, 0));

                string actualSha256;
                using (var response = await _http.GetAsync(installerUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength;

                    await using var httpStream = await response.Content.ReadAsStreamAsync();
                    await using var file = File.Create(installerPath);
                    using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

                    var buffer = new byte[81920];
                    long bytesRead = 0;
                    int read;
                    while ((read = await httpStream.ReadAsync(buffer)) > 0)
                    {
                        await file.WriteAsync(buffer.AsMemory(0, read));
                        sha.AppendData(buffer, 0, read);
                        bytesRead += read;
                        double? percent = totalBytes is > 0 ? bytesRead * 100.0 / totalBytes.Value : null;
                        progress?.Report(new UpdateProgress(UpdateStatus.Downloading, percent));
                    }
                    actualSha256 = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
                }

                if (!string.Equals(actualSha256, installer.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"Checksum mismatch: expected {installer.Sha256}, got {actualSha256} - aborting.");
                    TryDelete(installerPath);
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }

                if (!File.Exists(installerPath))
                {
                    // Downloaded successfully but vanished before we could run it - almost
                    // certainly antivirus quarantining/deleting the file.
                    Log("Downloaded installer no longer exists on disk (likely quarantined by antivirus) - aborting.");
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }

                if (!AuthenticodeVerifier.IsSignedBy(installerPath, TrustedSignerThumbprint, out var signatureInfo))
                {
                    Log($"Installer signature rejected: {signatureInfo} - aborting and deleting it.");
                    TryDelete(installerPath);
                    progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                    return false;
                }
                Log($"Checksum OK, {signatureInfo}.");

                Log("Launching installer and shutting down.");
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
                if (installerPath is not null) TryDelete(installerPath);
                progress?.Report(new UpdateProgress(UpdateStatus.Failed));
                return false;
            }
        }

        // Installer names come from the server, so only accept the release script's own pattern
        // (no path separators, no "..") before using one in a URL or temp-file name.
        private static readonly Regex InstallerNamePattern =
            new(@"^MyriaRPG_Setup[A-Za-z0-9._-]*\.exe$", RegexOptions.CultureInvariant);
        private static readonly Regex Sha256Pattern = new(@"^[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant);

        /// <summary>Validates a release manifest and picks out the client installer entry.</summary>
        public static bool TryGetInstaller(ReleaseManifest? manifest, out Version version,
                                           out ReleaseFile installer, out string problem)
        {
            version = new Version(0, 0);
            installer = new ReleaseFile();
            if (manifest is null) { problem = "empty manifest"; return false; }
            if (string.IsNullOrWhiteSpace(manifest.Version) || !Version.TryParse(manifest.Version, out var parsed)
                || !Regex.IsMatch(manifest.Version, @"^\d+(\.\d+){1,3}$"))
            {
                problem = $"invalid version '{manifest.Version}'";
                return false;
            }
            var file = manifest.Files?.FirstOrDefault(f => f.Kind == "client-installer");
            if (file is null) { problem = "no client-installer entry"; return false; }
            if (file.Name is null || !InstallerNamePattern.IsMatch(file.Name)) { problem = $"unexpected installer name '{file.Name}'"; return false; }
            if (file.Sha256 is null || !Sha256Pattern.IsMatch(file.Sha256)) { problem = "missing or malformed SHA-256"; return false; }
            version = parsed;
            installer = file;
            problem = "";
            return true;
        }

        /// <summary>Compares release versions; missing components count as 0, so the manifest's
        /// "0.2.22" is newer than assembly version 0.2.21.0 and equal to 0.2.22.0.</summary>
        public static bool IsNewer(Version latest, Version current)
        {
            static Version Norm(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
            return Norm(latest) > Norm(current);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
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
    }

    /// <summary>latest.json / releases.json entry written by Setup/release.ps1.</summary>
    public sealed class ReleaseManifest
    {
        public string? Version { get; set; }
        public string? Date { get; set; }
        public Dictionary<string, string>? Notes { get; set; }
        public List<ReleaseFile>? Files { get; set; }
    }

    public sealed class ReleaseFile
    {
        public string? Kind { get; set; }
        public string? Name { get; set; }
        public string? Sha256 { get; set; }
        public long Size { get; set; }
    }
}
