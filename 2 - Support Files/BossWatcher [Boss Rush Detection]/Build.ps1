$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2BossWatcher\PoE2BossWatcher.csproj'
$publish = Join-Path $root 'publish'
$trainedData = Join-Path $root 'tessdata\eng.traineddata'

if (-not (Test-Path -LiteralPath $trainedData)) {
    throw "Missing tessdata\eng.traineddata. Run .\Setup-OCR.ps1 first, then run .\Build.ps1 again."
}

Write-Host 'Restoring packages...'
dotnet restore $project
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

Write-Host 'Publishing self-contained PoE2BossWatcher...'
if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
dotnet publish $project -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $root 'config.json') -Destination $publish -Force
Copy-Item -LiteralPath (Join-Path $root 'bosses.txt') -Destination $publish -Force
New-Item -ItemType Directory -Force -Path (Join-Path $publish 'tessdata') | Out-Null
Copy-Item -LiteralPath $trainedData -Destination (Join-Path $publish 'tessdata\eng.traineddata') -Force

Write-Host ''
Write-Host 'Build succeeded.'
Write-Host "Self-contained BossWatcher published to: $publish"
