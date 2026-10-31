$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2CaptureHost\PoE2CaptureHost.csproj'
$publish = Join-Path $root 'publish'
$tempArtifacts = Join-Path $env:TEMP ('PoE2CH-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))

try {
    New-Item -ItemType Directory -Force -Path $tempArtifacts | Out-Null
    if (Test-Path -LiteralPath $publish) { Remove-Item -LiteralPath $publish -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $publish | Out-Null

    Write-Host 'Publishing self-contained PoE2 CaptureHost (Windows Graphics Capture)...'
    & dotnet restore $project -r win-x64 --artifacts-path $tempArtifacts --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

    & dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore `
        --artifacts-path $tempArtifacts --disable-build-servers -o $publish `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

    $exe = Join-Path $publish 'PoE2CaptureHost.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
        throw "Published PoE2CaptureHost.exe was not found at: $exe"
    }

    Write-Host ''
    Write-Host "CaptureHost published to: $publish"
}
finally {
    if (Test-Path -LiteralPath $tempArtifacts) {
        Remove-Item -LiteralPath $tempArtifacts -Recurse -Force -ErrorAction SilentlyContinue
    }
}
