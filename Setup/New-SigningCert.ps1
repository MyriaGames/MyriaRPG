<#
One-time setup: creates a free self-signed code-signing certificate for MyriaRPG alpha builds.

Run this once on the release machine. The exported .pfx (private key) stays local only and
must NEVER be committed. The .cer (public key) is safe to commit/ship and is what
TrustMyriaRPG.ps1 installs on testers' machines.

Re-run with -Force to wipe an existing cert with the same subject and start over
(e.g. if you mistyped the .pfx password last time).
#>

param(
    [string]$CertDir = "$PSScriptRoot\certs",
    [string]$Subject = "CN=MyriaRPG Alpha, O=Rhyen",
    [switch]$Force
)

New-Item -ItemType Directory -Force -Path $CertDir | Out-Null

$existing = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -eq $Subject }
if ($existing) {
    if (-not $Force) {
        Write-Host "A code-signing cert with subject '$Subject' already exists (thumbprint $($existing.Thumbprint))." -ForegroundColor Yellow
        Write-Host "Re-run with -Force to delete it and generate a fresh one." -ForegroundColor Yellow
        exit 1
    }
    Write-Host "Removing existing cert (thumbprint $($existing.Thumbprint))..." -ForegroundColor Yellow
    Remove-Item "Cert:\CurrentUser\My\$($existing.Thumbprint)" -Force
}

$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject $Subject `
    -CertStoreLocation Cert:\CurrentUser\My `
    -KeyUsage DigitalSignature `
    -FriendlyName "MyriaRPG Alpha Code Signing" `
    -NotAfter (Get-Date).AddYears(10)

Write-Host "Created certificate: $($cert.Thumbprint)"

function Read-ConfirmedPassword {
    param([string]$Prompt)

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        $first = Read-Host -AsSecureString -Prompt $Prompt
        $second = Read-Host -AsSecureString -Prompt "Confirm password"

        $firstPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($first))
        $secondPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($second))

        if ($firstPlain -eq $secondPlain) {
            return $first
        }
        Write-Host "Passwords didn't match - try again ($attempt/5)." -ForegroundColor Red
    }

    Write-Host "Too many mismatched attempts. Aborting." -ForegroundColor Red
    exit 1
}

$pfxPassword = Read-ConfirmedPassword -Prompt "Enter a password to protect the exported .pfx (release.ps1 will ask for this again when signing)"
$pfxPath = Join-Path $CertDir "MyriaRPGSigning.pfx"
$cerPath = Join-Path $CertDir "MyriaRPGSigning.cer"

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pfxPassword | Out-Null
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  Private key (local only, NEVER commit): $pfxPath"
Write-Host "  Public cert (ship with installer, safe to commit): $cerPath"
Write-Host ""
Write-Host "Next: run release.ps1 to publish + sign + package a build."
