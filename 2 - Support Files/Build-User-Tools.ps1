$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseRoot = Split-Path -Parent $root

Write-Host 'Building setup UI...'
& (Join-Path $root 'Setup UI [Configuration]\Build.ps1')
if ($LASTEXITCODE -ne 0) { throw "Setup UI build failed with exit code $LASTEXITCODE." }

Write-Host ''
Write-Host 'Building BossWatcher...'
$bossBuild = Join-Path $root 'BossWatcher [Boss Rush Detection]\Build.ps1'
try {
    & $bossBuild
} catch {
    Write-Host ''
    Write-Warning $_.Exception.Message
    Write-Host 'If tessdata is missing, run BossWatcher [Boss Rush Detection]\Setup-OCR.ps1, then run this script again.'
    throw
}

Write-Host ''
Write-Host 'User tools built successfully.'
Write-Host (Join-Path $releaseRoot '1 - User Setup\PoE2RouteSetup.exe')
