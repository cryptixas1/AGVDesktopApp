<#
 Simple MSIX packaging helper (requires Windows SDK tools: MakeAppx.exe and SignTool.exe)
 Usage:
 1. Build your app and copy publish output into `installer\msix\package_root` (create folder and put AGVDesktop.exe and dependencies there).
 2. Edit AppxManifest.xml (Identity/Publisher) and provide Assets (logos) inside package_root\Assets.
 3. Run this script from repository root in PowerShell (run as Admin if necessary):
    powershell -ExecutionPolicy Bypass -File .\installer\msix\pack.ps1

 This script creates an unsigned MSIX package `AGVDesktop.msix` in the msix folder.
 To sign the package, use SignTool.exe with a certificate, for example:
  signtool sign /fd SHA256 /a /f path\to\cert.pfx /p <password> AGVDesktop.msix
#>

param()

$root = Join-Path $PSScriptRoot 'package_root'
if (-not (Test-Path $root)) { Write-Host "Please create the package root at $root and copy publish output inside."; exit 1 }

$manifest = Join-Path $PSScriptRoot 'AppxManifest.xml'
$out = Join-Path $PSScriptRoot '..\AGVDesktop.msix' | Resolve-Path -Relative

Write-Host "Packing MSIX from $root using manifest $manifest -> $out"

# MakeAppx.exe must be available on PATH (part of Windows SDK). If not available, install Windows 10/11 SDK.
& MakeAppx.exe pack /d $root /p $out /m $manifest

if ($LASTEXITCODE -ne 0) { Write-Error "MakeAppx failed (exit $LASTEXITCODE)"; exit $LASTEXITCODE }

Write-Host "Unsigned MSIX created at: $out"
Write-Host "To sign: signtool sign /fd SHA256 /a /f path\to\cert.pfx /p <password> $out"
