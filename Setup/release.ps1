<#
Builds one alpha release end-to-end:
  publish WPF client (self-contained, single-file) -> sign exe -> build installer -> sign installer
  publish MyriaServer/MyriaAuthServer for win-x64 + linux-x64 (self-contained) -> zip each
  -> stage + commit (and optionally push) all of the above, plus the current Legal/*.md docs,
     to the MyriaRPG-releases repo.

Requires: New-SigningCert.ps1 already run once (cert present in Cert:\CurrentUser\My),
Inno Setup 6 installed, and a local clone of the public MyriaRPG-releases repo. Myria.Server.Realm
and Myria.Server.Auth are published straight out of this same Myria solution (no separate
sibling-repo clones needed - that split happened before this script was updated to match).
Signing uses the cert directly from the Windows cert store by thumbprint - no
.pfx password is ever needed or asked for here.

Usage:
  .\release.ps1 -ReleasesRepoPath "C:\path\to\MyriaRPG-releases"
  .\release.ps1 -ReleasesRepoPath "C:\path\to\MyriaRPG-releases" -Push
  .\release.ps1 -Push -SkipServer   # WPF client only, skip building/staging server zips
#>

param(
    [string]$ReleasesRepoPath = "$PSScriptRoot\..\..\MyriaRPG-releases",
    [string]$ServerProjectPath = "$PSScriptRoot\..\..\Myria.Server.Realm",
    [string]$AuthServerProjectPath = "$PSScriptRoot\..\..\Myria.Server.Auth",
    [string]$CertSubject = "CN=MyriaRPG Alpha, O=Rhyen",
    [switch]$Push,
    [switch]$SkipServer
)

$ErrorActionPreference = "Stop"

$RepoRoot     = Resolve-Path "$PSScriptRoot\.."
$SolutionRoot = Resolve-Path "$RepoRoot\.."
$CsprojPath   = Join-Path $RepoRoot "Myria.Wpf.csproj"
$LegalPath    = Join-Path $SolutionRoot "Legal"
$IsccPath    = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$TimestampUrl = "http://timestamp.digicert.com"

# ── 0. Preconditions ─────────────────────────────────────────────────────────
# Sign directly against the cert in the Windows cert store (by thumbprint) instead
# of exporting/re-entering a .pfx password - avoids signtool mangling special
# characters when a password is passed as a plaintext command-line argument.
$SigningCert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
               Where-Object { $_.Subject -eq $CertSubject } |
               Sort-Object NotBefore -Descending | Select-Object -First 1
if (-not $SigningCert) {
    Write-Host "No code-signing cert with subject '$CertSubject' found in Cert:\CurrentUser\My - run New-SigningCert.ps1 first." -ForegroundColor Red
    exit 1
}
$Thumbprint = $SigningCert.Thumbprint
Write-Host "Signing with cert thumbprint: $Thumbprint"

if (-not (Test-Path $IsccPath)) {
    Write-Host "Inno Setup compiler not found at $IsccPath - install Inno Setup 6." -ForegroundColor Red
    exit 1
}

if (-not $SkipServer -and -not (Test-Path $ServerProjectPath)) {
    Write-Host "Myria.Server.Realm project not found at $ServerProjectPath - re-run with -ServerProjectPath or -SkipServer." -ForegroundColor Red
    exit 1
}

if (-not $SkipServer -and -not (Test-Path $AuthServerProjectPath)) {
    Write-Host "Myria.Server.Auth project not found at $AuthServerProjectPath - re-run with -AuthServerProjectPath or -SkipServer." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $LegalPath)) {
    Write-Host "Legal docs folder not found at $LegalPath - expected Legal/*.md in the solution root." -ForegroundColor Red
    exit 1
}

function Find-SignTool {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Shared\NuGetPackages\microsoft.windows.sdk.buildtools\*\bin\*\x64\signtool.exe"
    )
    foreach ($pattern in $candidates) {
        $found = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue |
                 Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    return $null
}

$SignTool = Find-SignTool
if (-not $SignTool) {
    Write-Host "signtool.exe not found. Install the Windows SDK (or 'Windows SDK BuildTools' via NuGet)." -ForegroundColor Red
    exit 1
}

# ── 1. Read version from csproj ──────────────────────────────────────────────
$csprojContent = Get-Content $CsprojPath -Raw
if ($csprojContent -notmatch "<Version>([\d\.]+)</Version>") {
    Write-Host "Could not find <Version> in $CsprojPath" -ForegroundColor Red
    exit 1
}
$Version = $Matches[1]
Write-Host "Building MyriaRPG alpha $Version" -ForegroundColor Cyan

function Invoke-Sign {
    param([string]$FilePath)
    & $SignTool sign /sha1 $Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 "$FilePath"
    if ($LASTEXITCODE -ne 0) { throw "signtool failed on $FilePath" }
}

# ── 2. Publish WPF client (self-contained, single-file win-x64) ─────────────
Write-Host "`nPublishing WPF client..." -ForegroundColor Cyan
Push-Location $RepoRoot
try {
    dotnet publish Myria.Wpf.csproj -c Release -p:PublishProfile=FolderProfile
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
} finally {
    Pop-Location
}

$PublishDir = Join-Path $RepoRoot "bin\Release\net8.0-windows\publish"
$PublishedExe = Join-Path $PublishDir "MyriaRPG.exe"
if (-not (Test-Path $PublishedExe)) {
    Write-Host "Published exe not found at $PublishedExe" -ForegroundColor Red
    exit 1
}

# ── 3. Sign the published exe ────────────────────────────────────────────────
Write-Host "`nSigning MyriaRPG.exe..." -ForegroundColor Cyan
Invoke-Sign -FilePath $PublishedExe

# ── 4. Build the installer ───────────────────────────────────────────────────
Write-Host "`nBuilding installer..." -ForegroundColor Cyan
& $IsccPath "/DMyAppVersion=$Version" (Join-Path $PSScriptRoot "myriaRPG_Setupscript.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC.exe failed" }

$InstallerExe = Join-Path $PSScriptRoot "MyriaRPG_Setup.exe"
if (-not (Test-Path $InstallerExe)) {
    Write-Host "Installer not found at $InstallerExe" -ForegroundColor Red
    exit 1
}

# ── 5. Sign the installer ────────────────────────────────────────────────────
Write-Host "`nSigning installer..." -ForegroundColor Cyan
Invoke-Sign -FilePath $InstallerExe

# ── 5b. Publish MyriaServer for win-x64 + linux-x64 and zip each ────────────
$WinServerZip = $null
$LinuxServerZip = $null
if (-not $SkipServer) {
    Write-Host "`nPublishing MyriaServer (win-x64)..." -ForegroundColor Cyan
    $WinServerOut   = Join-Path $ServerProjectPath "bin\publish-win-x64"
    $LinuxServerOut = Join-Path $ServerProjectPath "bin\publish-linux-x64"
    Remove-Item $WinServerOut, $LinuxServerOut -Recurse -Force -ErrorAction SilentlyContinue

    Push-Location $ServerProjectPath
    try {
        dotnet publish Myria.Server.Realm.csproj -c Release -r win-x64 --self-contained true -o $WinServerOut
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish (win-x64 server) failed" }

        Write-Host "`nPublishing MyriaServer (linux-x64)..." -ForegroundColor Cyan
        dotnet publish Myria.Server.Realm.csproj -c Release -r linux-x64 --self-contained true -o $LinuxServerOut
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish (linux-x64 server) failed" }
    } finally {
        Pop-Location
    }

    # MyriaAuthServer is published into an "auth" subfolder of each server output so its
    # files (notably appsettings.json) don't collide with MyriaServer's own — linux-x64's
    # run-production.sh starts both together from this exact layout (./auth/MyriaAuthServer
    # alongside ./MyriaServer).
    Write-Host "`nPublishing MyriaAuthServer (win-x64)..." -ForegroundColor Cyan
    Push-Location $AuthServerProjectPath
    try {
        dotnet publish Myria.Server.Auth.csproj -c Release -r win-x64 --self-contained true -o (Join-Path $WinServerOut "auth")
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish (win-x64 auth server) failed" }

        Write-Host "`nPublishing MyriaAuthServer (linux-x64)..." -ForegroundColor Cyan
        dotnet publish Myria.Server.Auth.csproj -c Release -r linux-x64 --self-contained true -o (Join-Path $LinuxServerOut "auth")
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish (linux-x64 auth server) failed" }
    } finally {
        Pop-Location
    }

    $WinServerZip   = Join-Path $PSScriptRoot "MyriaServer_win-x64.zip"
    $LinuxServerZip = Join-Path $PSScriptRoot "MyriaServer_linux-x64.zip"
    if (Test-Path $WinServerZip)   { Remove-Item $WinServerZip -Force }
    if (Test-Path $LinuxServerZip) { Remove-Item $LinuxServerZip -Force }

    Write-Host "`nZipping server builds..." -ForegroundColor Cyan
    # Both Compress-Archive and ZipFile.CreateFromDirectory write entry names with backslash
    # path separators for nested directories (e.g. "auth\MyriaAuthServer") under Windows
    # PowerShell 5.1's .NET Framework - invalid per the zip spec, which requires forward
    # slashes. `unzip` on Linux warns about it and may extract "auth\MyriaAuthServer" as a
    # single literal filename instead of into an auth/ subfolder, breaking
    # update-production.sh's expected layout. Build entries manually instead, forcing '/'.
    function New-ZipWithForwardSlashes {
        param([string]$SourceDir, [string]$DestZip)
        Add-Type -AssemblyName System.IO.Compression
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::Open($DestZip, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            $resolvedSourceDir = (Resolve-Path -LiteralPath $SourceDir).Path.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
            Get-ChildItem -Path $resolvedSourceDir -Recurse -File | ForEach-Object {
                if ($_.FullName.Length -le $resolvedSourceDir.Length) {
                    throw "Zip entry path shorter than source root - SourceDir='$resolvedSourceDir' FullName='$($_.FullName)'"
                }
                # Avoid a literal backslash char in this string - use DirectorySeparatorChar/
                # char code 47 ('/') instead, since a bare '\' here has been observed to get
                # mangled by some shell layers this script gets invoked through.
                $relativePath = $_.FullName.Substring($resolvedSourceDir.Length + 1).Replace([IO.Path]::DirectorySeparatorChar, [char]47)
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                    $zip, $_.FullName, $relativePath, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        } finally {
            $zip.Dispose()
        }
    }
    New-ZipWithForwardSlashes -SourceDir $WinServerOut   -DestZip $WinServerZip
    New-ZipWithForwardSlashes -SourceDir $LinuxServerOut -DestZip $LinuxServerZip
    Write-Host "Server zips built:" -ForegroundColor Green
    Write-Host "  $WinServerZip"
    Write-Host "  $LinuxServerZip"
}

# ── 6. Stage into the releases repo ──────────────────────────────────────────
if (-not (Test-Path $ReleasesRepoPath)) {
    Write-Host "`nReleases repo not found at $ReleasesRepoPath - skipping publish step." -ForegroundColor Yellow
    Write-Host "Signed installer is ready at: $InstallerExe" -ForegroundColor Green
    if ($WinServerZip)   { Write-Host "Windows server zip ready at: $WinServerZip" -ForegroundColor Green }
    if ($LinuxServerZip) { Write-Host "Linux server zip ready at: $LinuxServerZip" -ForegroundColor Green }
    exit 0
}

$VersionedName = "MyriaRPG_Setup_$Version.exe"
Copy-Item $InstallerExe (Join-Path $ReleasesRepoPath $VersionedName) -Force

$VersionedWinServerZip   = "MyriaServer_win-x64_$Version.zip"
$VersionedLinuxServerZip = "MyriaServer_linux-x64_$Version.zip"
if ($WinServerZip)   { Copy-Item $WinServerZip   (Join-Path $ReleasesRepoPath $VersionedWinServerZip)   -Force }
if ($LinuxServerZip) { Copy-Item $LinuxServerZip (Join-Path $ReleasesRepoPath $VersionedLinuxServerZip) -Force }

$ManifestPath = Join-Path $ReleasesRepoPath "version.json"
$Manifest = @{
    version      = $Version
    installerUrl = "https://github.com/rllyben/MyriaRPG-releases/releases/download/v$Version/$VersionedName"
    notes        = "Alpha build $Version"
}
if ($WinServerZip)   { $Manifest.serverWinUrl   = "https://github.com/rllyben/MyriaRPG-releases/releases/download/v$Version/$VersionedWinServerZip" }
if ($LinuxServerZip) { $Manifest.serverLinuxUrl = "https://github.com/rllyben/MyriaRPG-releases/releases/download/v$Version/$VersionedLinuxServerZip" }
$Manifest | ConvertTo-Json | Set-Content -Path $ManifestPath -Encoding utf8

# ── 6a. Sync legal docs (Impressum, Datenschutzerklärung, Nutzungsbedingungen) ──────
# These live at Legal/*.md in the main Myria solution and describe what the published
# game actually does (data collected, self-service account deletion, etc.) - keep the
# public releases repo's copy in lockstep with every release rather than only updating
# it by hand on the rare occasion someone remembers to.
$ReleasesLegalPath = Join-Path $ReleasesRepoPath "Legal"
New-Item -ItemType Directory -Path $ReleasesLegalPath -Force | Out-Null
Copy-Item (Join-Path $LegalPath "*.md") $ReleasesLegalPath -Force

Write-Host "`nStaged release $Version in $ReleasesRepoPath" -ForegroundColor Green
Write-Host "NOTE: version.json's URLs assume you attach $VersionedName$(if ($WinServerZip) {", $VersionedWinServerZip"})$(if ($LinuxServerZip) {", $VersionedLinuxServerZip"}) to a GitHub Release tagged v$Version." -ForegroundColor Yellow

# ── 6b. Prune old releases - keep only the newest $MaxKeptReleases per artifact ──
# Only prunes the files tracked in this repo; it does NOT delete the corresponding
# GitHub Releases (tags + attached assets) - remove those separately with
# `gh release delete vX.Y.Z` if you want the Releases page itself cleaned up too.
$MaxKeptReleases = 10

function Get-PruneList {
    param([string]$Pattern, [string]$CaptureRegex)
    Get-ChildItem -Path $ReleasesRepoPath -Filter $Pattern |
        Where-Object { $_.Name -match $CaptureRegex } |
        ForEach-Object { [PSCustomObject]@{ File = $_; Version = [version]$Matches[1] } } |
        Sort-Object Version -Descending |
        Select-Object -Skip $MaxKeptReleases
}

$ToPrune = @()
$ToPrune += Get-PruneList -Pattern "MyriaRPG_Setup_*.exe"          -CaptureRegex '^MyriaRPG_Setup_(\d+\.\d+\.\d+)\.exe$'
$ToPrune += Get-PruneList -Pattern "MyriaServer_win-x64_*.zip"     -CaptureRegex '^MyriaServer_win-x64_(\d+\.\d+\.\d+)\.zip$'
$ToPrune += Get-PruneList -Pattern "MyriaServer_linux-x64_*.zip"   -CaptureRegex '^MyriaServer_linux-x64_(\d+\.\d+\.\d+)\.zip$'

if ($ToPrune.Count -gt 0) {
    Write-Host "`nPruning $($ToPrune.Count) release asset(s) older than the newest ${MaxKeptReleases} per platform:" -ForegroundColor Cyan
    foreach ($old in $ToPrune) { Write-Host "  - $($old.File.Name)" }
}

Push-Location $ReleasesRepoPath
try {
    git add $VersionedName version.json Legal
    if ($WinServerZip)   { git add $VersionedWinServerZip }
    if ($LinuxServerZip) { git add $VersionedLinuxServerZip }
    foreach ($old in $ToPrune) {
        git rm --quiet $old.File.Name
    }

    $CommitMessage = "Alpha release $Version"
    if ($ToPrune.Count -gt 0) { $CommitMessage += " (pruned $($ToPrune.Count) old asset[s])" }
    git commit -m $CommitMessage

    if ($Push) {
        git push -u origin HEAD
        $AttachList = $VersionedName
        if ($WinServerZip)   { $AttachList += ", $VersionedWinServerZip" }
        if ($LinuxServerZip) { $AttachList += ", $VersionedLinuxServerZip" }
        Write-Host "`nPushed. Now create a GitHub Release tagged 'v$Version' and attach $AttachList as assets." -ForegroundColor Green
        if ($ToPrune.Count -gt 0) {
            Write-Host "Remember: the pruned GitHub Releases themselves (tags + assets) still exist - delete with 'gh release delete vX.Y.Z' if wanted." -ForegroundColor Yellow
        }
    } else {
        Write-Host "`nCommitted locally. Run 'git push -u origin HEAD' in $ReleasesRepoPath when ready (or re-run with -Push)." -ForegroundColor Yellow
        Write-Host "Then create a GitHub Release tagged 'v$Version' and attach the staged assets." -ForegroundColor Yellow
    }
} finally {
    Pop-Location
}
