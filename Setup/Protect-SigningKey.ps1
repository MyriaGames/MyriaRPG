<#
Re-imports the Authenticode signing key so that it can no longer be exported and every use asks you
first (security review 2026-11-26, A04 F2). Before this, any program running as you could copy the
key or sign an installer silently - and every player's updater trusts that signature.

What it does:
  1. Opens the .pfx backup with its password and checks it is the same certificate the client pins.
  2. Removes the current, exportable copy from Cert:\CurrentUser\My.
  3. Imports the .pfx again: NOT exportable, with strong protection. Windows asks which level:
     choose "High" (a password at every signature) or at least "Medium" (a confirmation).
  4. Signs a throw-away test file to prove it still works (that's the first prompt).

Afterwards the .pfx is the only exportable copy - move it off this PC (encrypted USB stick or your
password manager) and delete it here. release.ps1 needs no change; it will just prompt twice per
release (client exe + installer).

Usage:  .\Protect-SigningKey.ps1 [-PfxPath <path>]
#>
param(
    [string]$PfxPath = "D:\Myria Games\Myria.RPG\Myria.Wpf\Setup\certs\MyriaRPGSigning.pfx",
    [string]$CertSubject = "CN=MyriaRPG Alpha, O=Rhyen",
    # The thumbprint the client pins (Services/AuthenticodeVerifier.cs) - the key must stay the same.
    [string]$PinnedThumbprint = "602147A12B2E604A306C769A28A041B3322AE89B"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Security
$Flags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]

if (-not (Test-Path $PfxPath)) { throw "No .pfx at $PfxPath - pass -PfxPath." }

# ── 1. Check the backup before touching anything ─────────────────────────────
$pfxPassword = Read-Host "Password of $PfxPath" -AsSecureString
$probe = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
    $PfxPath, $pfxPassword, $Flags::EphemeralKeySet)
if ($probe.Thumbprint -ne $PinnedThumbprint) {
    throw "The .pfx holds $($probe.Thumbprint), not the pinned $PinnedThumbprint - stopping (nothing changed)."
}
if (-not $probe.HasPrivateKey) { throw "The .pfx has no private key - stopping (nothing changed)." }
Write-Host "Backup OK: $($probe.Subject), $($probe.Thumbprint), valid until $($probe.NotAfter)" -ForegroundColor Green
$probe.Dispose()

$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("My", "CurrentUser")
$store.Open("ReadWrite")
try {
    # ── 2. Remove the exportable copy ────────────────────────────────────────
    $existing = @($store.Certificates | Where-Object Thumbprint -eq $PinnedThumbprint)
    foreach ($c in $existing) {
        $store.Remove($c)
        Write-Host "Removed the old (exportable) copy from Cert:\CurrentUser\My."
    }

    # ── 3. Import non-exportable with strong protection ──────────────────────
    Write-Host "Importing... Windows now asks for the protection level: choose High (password) or Medium." -ForegroundColor Yellow
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
        $PfxPath, $pfxPassword, ($Flags::UserKeySet -bor $Flags::PersistKeySet -bor $Flags::UserProtected))
    $store.Add($cert)
} finally {
    $store.Close()
}

$installed = Get-ChildItem Cert:\CurrentUser\My | Where-Object Thumbprint -eq $PinnedThumbprint
$rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($installed)
$policy = if ($rsa -is [System.Security.Cryptography.RSACng]) { "$($rsa.Key.ExportPolicy) / $($rsa.Key.UIPolicy.ProtectionLevel)" } else { "(legacy CSP key)" }
Write-Host "Installed: export policy / protection = $policy"

# ── 4. Prove signing still works ─────────────────────────────────────────────
$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
if ($signtool) {
    $test = Join-Path $env:TEMP "myria-signtest-$PID.exe"
    Copy-Item "$env:WINDIR\System32\whoami.exe" $test
    try {
        Write-Host "Test signature (expect the protection prompt now)..." -ForegroundColor Yellow
        & $signtool.FullName sign /sha1 $PinnedThumbprint /fd SHA256 $test | Out-Host
        $sig = Get-AuthenticodeSignature $test
        if ($sig.SignerCertificate.Thumbprint -eq $PinnedThumbprint) {
            Write-Host "Signing works with the protected key." -ForegroundColor Green
        } else {
            Write-Host "Test signature failed - the .pfx is unchanged, re-run this script." -ForegroundColor Red
        }
    } finally {
        Remove-Item $test -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "signtool.exe not found - skipped the test signature." -ForegroundColor Yellow
}

Write-Host "`nNext: move $PfxPath to an encrypted USB stick / your password manager, then delete it here." -ForegroundColor Cyan
