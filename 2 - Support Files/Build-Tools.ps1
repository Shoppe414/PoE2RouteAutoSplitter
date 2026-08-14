$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseRoot = Split-Path -Parent $root

$bossRoot = Join-Path $root 'BossWatcher'
$trainedData = Join-Path $bossRoot 'tessdata\eng.traineddata'
if (-not (Test-Path -LiteralPath $trainedData)) {
    Write-Host 'OCR language data is missing; downloading it...'
    & (Join-Path $bossRoot 'Setup-OCR.ps1')
}

Write-Host 'Building setup UI...'
& (Join-Path $root 'Setup UI [Configuration]\Build.ps1')
if ($LASTEXITCODE -ne 0) { throw "Setup UI build failed with exit code $LASTEXITCODE." }

Write-Host ''
Write-Host 'Building BossWatcher...'
& (Join-Path $bossRoot 'Build.ps1')
if ($LASTEXITCODE -ne 0) { throw "BossWatcher build failed with exit code $LASTEXITCODE." }

Write-Host ''
Write-Host 'Building GameTimeWatcher optional manual-pause helper...'
& (Join-Path $root 'GameTimeWatcher\Build.ps1')
if ($LASTEXITCODE -ne 0) { throw "GameTimeWatcher build failed with exit code $LASTEXITCODE." }

Write-Host ''
Write-Host 'User tools built successfully.'
Write-Host (Join-Path $releaseRoot '1 - User Setup\PoE2RouteSetup.exe')
Write-Host 'For a distributable installer and portable ZIP, run Build-Release.ps1 instead.'
