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

Write-Host 'Publishing self-contained PoE2 Route AutoSplitter Setup UI...'
if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
New-Item -ItemType Directory -Force -Path $publish | Out-Null
New-Item -ItemType Directory -Force -Path $userRoot | Out-Null
New-Item -ItemType Directory -Force -Path $target | Out-Null

dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Published PoE2RouteSetup.exe was not found at: $publishedExe"
}

Copy-Item -LiteralPath $publishedExe -Destination $userExe -Force

Write-Host ''
Write-Host 'Build succeeded.'
Write-Host "Self-contained user launcher: $userExe"
Write-Host "LiveSplit target: $target"
