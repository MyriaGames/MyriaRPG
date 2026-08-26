<#
Run this once before installing MyriaRPG (Alpha): right-click -> "Run with PowerShell" and
accept the UAC prompt. It trusts the self-signed certificate MyriaRPG's installer and exe are
signed with, so Windows stops showing an "Unknown Publisher" warning on every launch.

This does NOT remove Windows SmartScreen's "Windows protected your PC" screen the first time
you run the installer - that's reputation-based and only goes away with a paid certificate.
Just click "More info" -> "Run anyway" once; that's expected for an alpha test build.
#>

param(
    [string]$CerPath = "$PSScriptRoot\MyriaRPGSigning.cer"
)

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "This needs to run as Administrator. Restarting elevated..." -ForegroundColor Yellow
    Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -CerPath `"$CerPath`""
    exit
}

if (-not (Test-Path $CerPath)) {
    Write-Host "Certificate not found at $CerPath" -ForegroundColor Red
    Write-Host "Make sure MyriaRPGSigning.cer is in the same folder as this script." -ForegroundColor Red
    exit 1
}

Import-Certificate -FilePath $CerPath -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
Import-Certificate -FilePath $CerPath -CertStoreLocation Cert:\LocalMachine\TrustedPublisher | Out-Null

Write-Host ""
Write-Host "Done - MyriaRPG's certificate is now trusted." -ForegroundColor Green
Write-Host "You can now install and run MyriaRPG without the 'Unknown Publisher' prompt." -ForegroundColor Green
Write-Host "Windows SmartScreen may still show 'Windows protected your PC' once - click 'More info' then 'Run anyway'." -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to close..."
$null = $Host.UI.RawUI.ReadKey("NoRepeat,IncludeKeyOnly")
