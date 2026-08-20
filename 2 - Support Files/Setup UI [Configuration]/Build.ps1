$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$supportRoot = Split-Path -Parent $root
$releaseRoot = Split-Path -Parent $supportRoot
$userRoot = Join-Path $releaseRoot '1 - User Setup'
$target = Join-Path $userRoot 'LiveSplit Target'
$project = Join-Path $root 'src\PoE2RouteSetup\PoE2RouteSetup.csproj'
$publish = Join-Path $root 'publish'
$publishedExe = Join-Path $publish 'PoE2RouteSetup.exe'
$userExe = Join-Path $userRoot 'PoE2RouteSetup.exe'
$tempArtifacts = Join-Path $env:TEMP ('PoE2UI-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))

$properNounRefresh = Join-Path $root 'Refresh-ProperNouns.ps1'

Write-Host 'Refreshing authoritative PoE2 boss/campaign/map area display names...'
& $properNounRefresh

try {
    Write-Host "Using short build-artifact path: $tempArtifacts"
    New-Item -ItemType Directory -Force -Path $tempArtifacts | Out-Null

    Write-Host 'Publishing self-contained PoE2 Route AutoSplitter Setup UI...'
    if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $publish | Out-Null
    New-Item -ItemType Directory -Force -Path $userRoot | Out-Null
    New-Item -ItemType Directory -Force -Path $target | Out-Null

    & dotnet restore $project -r win-x64 --artifacts-path $tempArtifacts --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

    & dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore `
        --artifacts-path $tempArtifacts --disable-build-servers `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None -p:DebugSymbols=false -o $publish
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

    if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
        throw "Published PoE2RouteSetup.exe was not found at: $publishedExe"
    }

    Copy-Item -LiteralPath $publishedExe -Destination $userExe -Force

    Write-Host ''
    Write-Host 'Build succeeded.'
    Write-Host "Self-contained user launcher: $userExe"
    Write-Host "LiveSplit target: $target"
}
finally {
    if (Test-Path -LiteralPath $tempArtifacts) {
        Remove-Item -LiteralPath $tempArtifacts -Recurse -Force -ErrorAction SilentlyContinue
    }
}
