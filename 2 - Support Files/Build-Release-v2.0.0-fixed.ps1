param(
    [string]$Version = '2.0.0',
    [switch]$SkipInstaller,
    [switch]$SkipPortableZip
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must look like 2.0.0. Received: $Version"
}

$supportRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$releaseRoot = Split-Path -Parent $supportRoot
$userRoot = Join-Path $releaseRoot '1 - User Setup'
$setupUiRoot = Join-Path $supportRoot 'Setup UI [Configuration]'
$bossRoot = Join-Path $supportRoot 'BossWatcher [Boss Rush Detection]'
$installerRoot = Join-Path $supportRoot 'Installer [Windows]'
$artifactsRoot = Join-Path $releaseRoot 'artifacts'
$portableName = "PoE2RouteAutoSplitter-v$Version"
$portableRoot = Join-Path $artifactsRoot $portableName
$portableUserRoot = Join-Path $portableRoot '1 - User Setup'
$portableSupportRoot = Join-Path $portableRoot '2 - Support Files'
$portableTarget = Join-Path $portableUserRoot 'LiveSplit Target'
$portableZip = Join-Path $artifactsRoot "$portableName.zip"
$installerOutput = $artifactsRoot
$prereqRoot = Join-Path $artifactsRoot 'prereqs'
$checksumsPath = Join-Path $artifactsRoot 'SHA256SUMS.txt'

function Copy-DirectoryContents {
    param([Parameter(Mandatory)][string]$Source, [Parameter(Mandatory)][string]$Destination)
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $Destination -Recurse -Force
    }
}

function Resolve-Iscc {
    # 1. Prefer PATH when the installer/compiler has registered itself there.
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command -and $command.Source -and (Test-Path -LiteralPath $command.Source -PathType Leaf)) {
        return $command.Source
    }

    # 2. WinGet/Inno Setup can be installed either per-user or machine-wide.
    #    A per-user install lives under %LOCALAPPDATA%\Programs\Inno Setup 6.
    $candidatePaths = New-Object System.Collections.Generic.List[string]

    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    if (-not $localAppData) { $localAppData = $env:LOCALAPPDATA }
    if ($localAppData) {
        $candidatePaths.Add((Join-Path $localAppData 'Programs\Inno Setup 6\ISCC.exe'))
    }

    $programFilesX86 = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)
    if (-not $programFilesX86) { $programFilesX86 = ${env:ProgramFiles(x86)} }
    if ($programFilesX86) {
        $candidatePaths.Add((Join-Path $programFilesX86 'Inno Setup 6\ISCC.exe'))
    }

    $programFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    if (-not $programFiles) { $programFiles = $env:ProgramFiles }
    if ($programFiles) {
        $candidatePaths.Add((Join-Path $programFiles 'Inno Setup 6\ISCC.exe'))
    }

    # 3. Also honor the install location recorded by Inno Setup. This makes
    #    discovery work with non-default install directories.
    $registryKeys = @(
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1'
    )
    foreach ($registryKey in $registryKeys) {
        $entry = Get-ItemProperty -LiteralPath $registryKey -ErrorAction SilentlyContinue
        if ($entry -and $entry.InstallLocation) {
            $candidatePaths.Add((Join-Path ([string]$entry.InstallLocation) 'ISCC.exe'))
        }
    }

    foreach ($candidate in @($candidatePaths | Select-Object -Unique)) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            Write-Host "Using Inno Setup compiler: $candidate"
            return $candidate
        }
    }

    $searched = @($candidatePaths | Select-Object -Unique | ForEach-Object { "  - $_" }) -join [Environment]::NewLine
    throw "Inno Setup 6 was not found. Checked PATH and these locations:$([Environment]::NewLine)$searched$([Environment]::NewLine)Install Inno Setup 6, or run Build-Release.ps1 with -SkipInstaller."
}

Write-Host "Building PoE2 Route AutoSplitter v$Version release..."

# The OCR language model is intentionally generated/downloaded rather than stored in Git.
$trainedData = Join-Path $bossRoot 'tessdata\eng.traineddata'
if (-not (Test-Path -LiteralPath $trainedData)) {
    Write-Host 'OCR language data is missing; downloading it...'
    & (Join-Path $bossRoot 'Setup-OCR.ps1')
}
if (-not (Test-Path -LiteralPath $trainedData)) {
    throw "OCR setup did not create: $trainedData"
}

Write-Host 'Building self-contained Setup UI...'
& (Join-Path $setupUiRoot 'Build.ps1')
if ($LASTEXITCODE -ne 0) { throw "Setup UI build failed with exit code $LASTEXITCODE." }

Write-Host 'Building self-contained BossWatcher...'
& (Join-Path $bossRoot 'Build.ps1')
if ($LASTEXITCODE -ne 0) { throw "BossWatcher build failed with exit code $LASTEXITCODE." }

$setupExe = Join-Path $userRoot 'PoE2RouteSetup.exe'
$bossPublish = Join-Path $bossRoot 'publish'
$bossExe = Join-Path $bossPublish 'PoE2BossWatcher.exe'
if (-not (Test-Path -LiteralPath $setupExe)) { throw "Missing built Setup UI: $setupExe" }
if (-not (Test-Path -LiteralPath $bossExe)) { throw "Missing built BossWatcher: $bossExe" }

if (Test-Path -LiteralPath $artifactsRoot) { Remove-Item -LiteralPath $artifactsRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $portableUserRoot, $portableSupportRoot, $portableTarget | Out-Null

# User launcher.
Copy-Item -LiteralPath $setupExe -Destination (Join-Path $portableUserRoot 'PoE2RouteSetup.exe') -Force

# Runtime-only Setup UI data. Source/build files stay in Git and are not installed.
$runtimeUi = Join-Path $portableSupportRoot 'Setup UI [Configuration]'
New-Item -ItemType Directory -Force -Path $runtimeUi | Out-Null
$manifestSource = Join-Path $setupUiRoot 'ui-manifest.json'
$manifest = Get-Content -LiteralPath $manifestSource -Raw | ConvertFrom-Json
if ([string]$manifest.Version -ne $Version) {
    throw "Release version $Version does not match ui-manifest.json version $($manifest.Version). Update the source version before creating the release."
}
$manifest.Version = $Version
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $runtimeUi 'ui-manifest.json') -Encoding UTF8

# All route-mode data is small and remains available to the Setup UI at runtime.
Get-ChildItem -LiteralPath $supportRoot -Directory | Where-Object { $_.Name -match '^\d{2} - ' } | Sort-Object Name | ForEach-Object {
    Copy-DirectoryContents -Source $_.FullName -Destination (Join-Path $portableSupportRoot $_.Name)
}

# BossWatcher runtime plus the catalogs the Setup UI reads for custom-route selection.
$runtimeBoss = Join-Path $portableSupportRoot 'BossWatcher [Boss Rush Detection]'
New-Item -ItemType Directory -Force -Path $runtimeBoss | Out-Null
Copy-DirectoryContents -Source $bossPublish -Destination (Join-Path $runtimeBoss 'publish')
Copy-Item -LiteralPath (Join-Path $bossRoot 'bosses.txt') -Destination $runtimeBoss -Force
New-Item -ItemType Directory -Force -Path (Join-Path $runtimeBoss 'BossLists') | Out-Null
Copy-Item -LiteralPath (Join-Path $bossRoot 'BossLists\support-only.txt') -Destination (Join-Path $runtimeBoss 'BossLists\support-only.txt') -Force

@"
PoE2 Route AutoSplitter v$Version - Installed Runtime

Normal users should launch:
  1 - User Setup\PoE2RouteSetup.exe

The Setup UI deploys the selected LiveSplit .lss/.asl/runtime files into:
  1 - User Setup\LiveSplit Target

LiveSplit layouts (.lsl) are intentionally not generated. Configure your own
LiveSplit layout and point its Scriptable Auto Splitter component to the .asl
file generated inside LiveSplit Target.

BossWatcher is installed under 2 - Support Files and is started from the Setup UI.
The installed runtime is self-contained; users do not need the .NET SDK or PowerShell.
"@ | Set-Content -LiteralPath (Join-Path $portableSupportRoot 'README - Installed Runtime.txt') -Encoding UTF8

Write-Host 'Validating assembled runtime...'
$runtimeManifestPath = Join-Path $runtimeUi 'ui-manifest.json'
$runtimeManifest = Get-Content -LiteralPath $runtimeManifestPath -Raw | ConvertFrom-Json
if ([string]$runtimeManifest.Version -ne $Version) { throw 'Staged manifest version mismatch.' }
if (@($runtimeManifest.Presets).Count -eq 0) { throw 'Staged manifest contains no premade setups.' }
foreach ($preset in @($runtimeManifest.Presets)) {
    foreach ($relative in @([string]$preset.LssSource, [string]$preset.AslSource)) {
        $required = Join-Path $portableSupportRoot ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Staged runtime is missing manifest file: $relative" }
    }
    foreach ($runtimeFile in @($preset.RuntimeFiles)) {
        if ($null -eq $runtimeFile) { continue }
        $relative = [string]$runtimeFile.Source
        $required = Join-Path $portableSupportRoot ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Staged runtime is missing manifest runtime file: $relative" }
    }
}
foreach ($relative in @([string]$runtimeManifest.AreaCatalog, [string]$runtimeManifest.BossCatalog, [string]$runtimeManifest.BossSupportOnlyList, [string]$runtimeManifest.CustomAslSource)) {
    $required = Join-Path $portableSupportRoot ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Staged runtime is missing catalog/custom file: $relative" }
}
if (Get-ChildItem -LiteralPath $portableRoot -Recurse -File -Filter '*.lsl') {
    throw 'Staged runtime unexpectedly contains a LiveSplit .lsl layout.'
}
if (-not (Test-Path -LiteralPath (Join-Path $portableUserRoot 'PoE2RouteSetup.exe') -PathType Leaf)) { throw 'Staged Setup UI executable is missing.' }
if (-not (Test-Path -LiteralPath (Join-Path $runtimeBoss 'publish\PoE2BossWatcher.exe') -PathType Leaf)) { throw 'Staged BossWatcher executable is missing.' }
Write-Host 'Assembled runtime validation passed.'

if (-not $SkipPortableZip) {
    Write-Host 'Creating portable ZIP...'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $portableZip) { Remove-Item -LiteralPath $portableZip -Force }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($portableRoot, $portableZip, [System.IO.Compression.CompressionLevel]::Optimal, $true)
}

$installerPath = $null
if (-not $SkipInstaller) {
    $iscc = Resolve-Iscc
    New-Item -ItemType Directory -Force -Path $installerOutput, $prereqRoot | Out-Null

    $vcRedist = Join-Path $prereqRoot 'vc_redist.x64.exe'
    if (-not (Test-Path -LiteralPath $vcRedist)) {
        Write-Host 'Downloading Microsoft Visual C++ 2015-2022 x64 Redistributable...'
        Invoke-WebRequest -Uri 'https://aka.ms/vs/17/release/vc_redist.x64.exe' -OutFile $vcRedist
    }

    $iss = Join-Path $installerRoot 'PoE2RouteAutoSplitter.iss'
    Write-Host 'Compiling Windows installer with Inno Setup...'
    & $iscc "/DMyAppVersion=$Version" "/DStageRoot=$portableRoot" "/DInstallerOutputDir=$installerOutput" "/DVcRedistPath=$vcRedist" $iss
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compiler failed with exit code $LASTEXITCODE." }

    $installerPath = Join-Path $installerOutput "$portableName-Setup.exe"
    if (-not (Test-Path -LiteralPath $installerPath)) { throw "Installer output was not found: $installerPath" }

    # Keep the locally generated installer where the developer originally requested it,
    # but .gitignore prevents this large binary from being committed accidentally.
    Copy-Item -LiteralPath $installerPath -Destination (Join-Path $userRoot "$portableName-Setup.exe") -Force
}

$checksumFiles = @()
if (Test-Path -LiteralPath $portableZip) { $checksumFiles += $portableZip }
if ($installerPath -and (Test-Path -LiteralPath $installerPath)) { $checksumFiles += $installerPath }
$checksumLines = foreach ($file in $checksumFiles) {
    $hash = Get-FileHash -LiteralPath $file -Algorithm SHA256
    "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), (Split-Path -Leaf $file)
}
$checksumLines | Set-Content -LiteralPath $checksumsPath -Encoding ASCII

Write-Host ''
Write-Host 'Release build completed.'
Write-Host "Portable runtime: $portableRoot"
if (Test-Path -LiteralPath $portableZip) { Write-Host "Portable ZIP:     $portableZip" }
if ($installerPath) { Write-Host "Installer:        $installerPath" }
Write-Host "Checksums:        $checksumsPath"
