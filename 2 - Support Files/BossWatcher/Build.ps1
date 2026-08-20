$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2BossWatcher\PoE2BossWatcher.csproj'
$publish = Join-Path $root 'publish'
$requiredTessCodes = @('eng','fra','deu','spa','jpn','kor','por','rus','tha')
$missingTess = @($requiredTessCodes | Where-Object { -not (Test-Path -LiteralPath (Join-Path $root ("tessdata\$_.traineddata")) -PathType Leaf) })
$tempArtifacts = Join-Path $env:TEMP ('PoE2BW-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))

if ($missingTess.Count -gt 0) {
    throw "Missing OCR tessdata models: $($missingTess -join ', '). Run .\Setup-OCR.ps1 first, then run .\Build.ps1 again."
}

try {
    # Keep all SDK-generated bin/obj/runtimeconfig files under a short temp path.
    # This avoids Windows MAX_PATH failures when the project was extracted into
    # a deeply nested directory. The final publish directory itself is short
    # enough, so only the intermediate artifacts need relocation.
    Write-Host "Using short build-artifact path: $tempArtifacts"
    New-Item -ItemType Directory -Force -Path $tempArtifacts | Out-Null

    if (Test-Path -LiteralPath $publish) {
        Remove-Item -LiteralPath $publish -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $publish | Out-Null

    Write-Host 'Restoring packages...'
    & dotnet restore $project -r win-x64 --artifacts-path $tempArtifacts --disable-build-servers
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Publishing self-contained PoE2BossWatcher...'
    & dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore `
        --artifacts-path $tempArtifacts --disable-build-servers `
        -p:GenerateRuntimeConfigurationFiles=true `
        -p:DebugType=None -p:DebugSymbols=false -o $publish
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    $publishedExe = Join-Path $publish 'PoE2BossWatcher.exe'
    $publishedRuntimeConfig = Join-Path $publish 'PoE2BossWatcher.runtimeconfig.json'
    if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
        throw "Published PoE2BossWatcher.exe was not found at: $publishedExe"
    }
    if (-not (Test-Path -LiteralPath $publishedRuntimeConfig -PathType Leaf)) {
        throw "Published PoE2BossWatcher.runtimeconfig.json was not found at: $publishedRuntimeConfig"
    }

    Copy-Item -LiteralPath (Join-Path $root 'config.json') -Destination $publish -Force
    Copy-Item -LiteralPath (Join-Path $root 'bosses.txt') -Destination $publish -Force
    Copy-Item -LiteralPath (Join-Path $root 'map-bosses.json') -Destination $publish -Force
    Copy-Item -LiteralPath (Join-Path $root 'boss-localizations.json') -Destination $publish -Force
    New-Item -ItemType Directory -Force -Path (Join-Path $publish 'tessdata') | Out-Null
    foreach ($tessCode in $requiredTessCodes) {
        Copy-Item -LiteralPath (Join-Path $root ("tessdata\$tessCode.traineddata")) -Destination (Join-Path $publish ("tessdata\$tessCode.traineddata")) -Force
    }

    Write-Host ''
    Write-Host 'Build succeeded.'
    Write-Host "Self-contained BossWatcher published to: $publish"
}
finally {
    if (Test-Path -LiteralPath $tempArtifacts) {
        Remove-Item -LiteralPath $tempArtifacts -Recurse -Force -ErrorAction SilentlyContinue
    }
}
