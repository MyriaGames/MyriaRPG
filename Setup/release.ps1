<#
Builds one alpha release end-to-end:
  publish WPF client (self-contained, single-file) -> sign exe -> build installer -> sign installer
  publish MyriaServer/MyriaAuthServer for win-x64 + linux-x64 (self-contained) -> zip each
  -> (optionally) publish a GitHub Release directly on each project's own repo via the gh CLI:
     the signed installer to MyriaGames/MyriaRPG, the server zips to MyriaGames/MyriaServer.

Requires: New-SigningCert.ps1 already run once (cert present in Cert:\CurrentUser\My),
Inno Setup 6 installed, and the GitHub CLI (`gh`, authenticated) if you pass -Publish. Myria.Server.Realm
and Myria.Server.Auth are published straight out of this same Myria solution (no separate
sibling-repo clones needed - that split happened before this script was updated to match).
Signing uses the cert directly from the Windows cert store by thumbprint - no
.pfx password is ever needed or asked for here.

Previously this staged everything into a local clone of a separate rllyben/MyriaRPG-releases repo
and committed/pushed there, with a hand-maintained version.json manifest describing the latest
build. Both this script and the runtime updaters (UpdateService.cs client-side,
update-production.sh server-side) have since moved to publishing/reading GitHub Releases directly
on each project's own repo - no separate releases repo or manifest file to keep in sync anymore.

Usage:
  .\release.ps1                          # build + sign everything, don't publish anywhere
  .\release.ps1 -Publish                 # build, sign, and create the GitHub Releases too
  .\release.ps1 -Publish -SkipServer     # WPF client only, skip building/releasing server zips
#>

param(
    [string]$ServerProjectPath = "$PSScriptRoot\..\..\Myria.Server.Realm",
    [string]$AuthServerProjectPath = "$PSScriptRoot\..\..\Myria.Server.Auth",
    [string]$CertSubject = "CN=MyriaRPG Alpha, O=Rhyen",
    [string]$WpfRepo = "MyriaGames/MyriaRPG",
    [string]$ServerRepo = "MyriaGames/MyriaServer",
    [switch]$Publish,
    [switch]$SkipServer
)

$ErrorActionPreference = "Stop"

$RepoRoot     = Resolve-Path "$PSScriptRoot\.."
$CsprojPath   = Join-Path $RepoRoot "Myria.Wpf.csproj"
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

# Newest version wins if more than one is installed (e.g. mid-upgrade from 6 to 7) - the .iss
# script itself has no version-specific syntax, so whichever ISCC compiles it is fine. Installer
# location varies: the Inno Setup installer defaults to Program Files (x86) system-wide, but its
# "install for me only" option (and newer per-user-by-default versions) puts it under
# LocalAppData\Programs instead - check both rather than assuming either one.
function Find-Iscc {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    )
    foreach ($path in $candidates) {
        if (Test-Path $path) { return $path }
    }
    return $null
}

$IsccPath = Find-Iscc
if (-not $IsccPath) {
    Write-Host "Inno Setup compiler (ISCC.exe) not found - install Inno Setup 6 or 7." -ForegroundColor Red
    exit 1
}
Write-Host "Using Inno Setup compiler: $IsccPath"

if (-not $SkipServer -and -not (Test-Path $ServerProjectPath)) {
    Write-Host "Myria.Server.Realm project not found at $ServerProjectPath - re-run with -ServerProjectPath or -SkipServer." -ForegroundColor Red
    exit 1
}

if (-not $SkipServer -and -not (Test-Path $AuthServerProjectPath)) {
    Write-Host "Myria.Server.Auth project not found at $AuthServerProjectPath - re-run with -AuthServerProjectPath or -SkipServer." -ForegroundColor Red
    exit 1
}

if ($Publish -and -not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "-Publish was passed but the GitHub CLI ('gh') isn't on PATH - install it or omit -Publish to just build+sign locally." -ForegroundColor Red
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
$Tag = "v$Version"
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

# UpdateService.cs matches release assets by prefix ("MyriaRPG_Setup") + ".exe" suffix - the
# exact filename doesn't need the version baked in, but versioning it avoids ambiguity/overwrite
# surprises if multiple builds ever sit in the same folder before being uploaded.
$VersionedInstallerName = "MyriaRPG_Setup_$Version.exe"
$VersionedInstallerPath = Join-Path $PSScriptRoot $VersionedInstallerName
Copy-Item $InstallerExe $VersionedInstallerPath -Force

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

    $WinServerZip   = Join-Path $PSScriptRoot "MyriaServer_win-x64_$Version.zip"
    $LinuxServerZip = Join-Path $PSScriptRoot "MyriaServer_linux-x64_$Version.zip"
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

Write-Host "`nBuild complete." -ForegroundColor Green
Write-Host "  Signed installer: $VersionedInstallerPath"
if ($WinServerZip)   { Write-Host "  Windows server zip: $WinServerZip" }
if ($LinuxServerZip) { Write-Host "  Linux server zip:   $LinuxServerZip" }

# ── 6. Publish GitHub Releases directly on each project's own repo ──────────
if (-not $Publish) {
    Write-Host "`nNot publishing (pass -Publish to create the GitHub Releases)." -ForegroundColor Yellow
    Write-Host "Would tag '$Tag' on $WpfRepo (installer) and $ServerRepo (server zips)." -ForegroundColor Yellow
    exit 0
}

function Publish-GitHubRelease {
    param([string]$Repo, [string]$Tag, [string]$Title, [string[]]$Assets)
    # $ErrorActionPreference = "Stop" (script-wide) plus PowerShell 7.3+'s
    # $PSNativeCommandUseErrorActionPreference promotes ANY non-zero exit from a native exe to a
    # terminating exception - including this "does the release exist yet" probe, whose whole point
    # is to fail on a fresh tag. Scope the preference down to "Continue" just for this call so a
    # not-found here is just a $LASTEXITCODE check, not a thrown error.
    $releaseExists = $false
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & gh release view $Tag --repo $Repo *> $null
        $releaseExists = ($LASTEXITCODE -eq 0)
    } finally {
        $ErrorActionPreference = $prevEap
    }
    if ($releaseExists) {
        Write-Host "Release $Tag already exists on $Repo - uploading/overwriting assets only." -ForegroundColor Yellow
        & gh release upload $Tag --repo $Repo --clobber @Assets
        if ($LASTEXITCODE -ne 0) { throw "gh release upload failed for $Repo" }
    } else {
        & gh release create $Tag --repo $Repo --title $Title --notes "Alpha build $Version" @Assets
        if ($LASTEXITCODE -ne 0) { throw "gh release create failed for $Repo" }
    }
}

Write-Host "`nPublishing GitHub Release $Tag on $WpfRepo..." -ForegroundColor Cyan
Publish-GitHubRelease -Repo $WpfRepo -Tag $Tag -Title "Alpha $Version" -Assets @($VersionedInstallerPath)

if ($WinServerZip -or $LinuxServerZip) {
    $ServerAssets = @()
    if ($WinServerZip)   { $ServerAssets += $WinServerZip }
    if ($LinuxServerZip) { $ServerAssets += $LinuxServerZip }
    Write-Host "`nPublishing GitHub Release $Tag on $ServerRepo..." -ForegroundColor Cyan
    Publish-GitHubRelease -Repo $ServerRepo -Tag $Tag -Title "Alpha $Version" -Assets $ServerAssets
}

Write-Host "`nDone. Released $Tag on $WpfRepo$(if ($WinServerZip -or $LinuxServerZip) { " and $ServerRepo" })." -ForegroundColor Green
