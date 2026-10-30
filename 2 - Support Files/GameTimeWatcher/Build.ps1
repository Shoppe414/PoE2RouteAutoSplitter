$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2GameTimeWatcher\PoE2GameTimeWatcher.csproj'
$publish = Join-Path $root 'publish'
$tempArtifacts = Join-Path $env:TEMP ('PoE2GTW-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))

try {
    Write-Host "Using short build-artifact path: $tempArtifacts"
    New-Item -ItemType Directory -Force -Path $tempArtifacts | Out-Null

    if (Test-Path -LiteralPath $publish) {
        Remove-Item -LiteralPath $publish -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $publish | Out-Null

    Write-Host 'Publishing self-contained PoE2 GameTimeWatcher v0.4.7...'
    & dotnet restore $project -r win-x64 --artifacts-path $tempArtifacts --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

    & dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore `
        --artifacts-path $tempArtifacts --disable-build-servers -o $publish `
        -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

    # Explicitly stage runtime data. Single-file publish behavior around linked
    # content has varied across SDK versions, so do not rely on implicit copies.
    Copy-Item -LiteralPath (Join-Path $root 'config.json') -Destination (Join-Path $publish 'config.json') -Force
    $publishTemplates = Join-Path $publish 'templates'
    New-Item -ItemType Directory -Force -Path $publishTemplates | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $root 'templates') -Filter '*.png' -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $publishTemplates $_.Name) -Force
    }


    $publishedExe = Join-Path $publish 'PoE2GameTimeWatcher.exe'
    $publishedConfig = Join-Path $publish 'config.json'
    $publishedStackTemplate = Join-Path $publish 'templates\pause-menu-stack.png'
    $publishedResumeTemplate = Join-Path $publish 'templates\pause-resume-game.png'
    $publishedPauseBannerTemplate = Join-Path $publish 'templates\pause-menu-tight.png'
    $publishedExitTemplate = Join-Path $publish 'templates\pause-exit-path-of-exile.png'
    $publishedMtxTemplate = Join-Path $publish 'templates\mtx-shop.png'
    $publishedControllerStackTemplate = Join-Path $publish 'templates\controller-pause-menu-stack.png'
    $publishedControllerResumeTemplate = Join-Path $publish 'templates\controller-pause-resume-game.png'
    $publishedControllerPauseBannerTemplate = Join-Path $publish 'templates\controller-pause-menu-tight.png'
    $publishedControllerExitTemplate = Join-Path $publish 'templates\controller-pause-exit-path-of-exile.png'
    foreach ($required in @($publishedExe, $publishedConfig, $publishedStackTemplate, $publishedResumeTemplate, $publishedPauseBannerTemplate, $publishedExitTemplate, $publishedMtxTemplate, $publishedControllerStackTemplate, $publishedControllerResumeTemplate, $publishedControllerPauseBannerTemplate, $publishedControllerExitTemplate)) {
        if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
            throw "Required GameTimeWatcher runtime file was not found after publish: $required"
        }
    }

    Write-Host ''
    Write-Host 'Build succeeded.'
    Write-Host "Executable: $publishedExe"
}
finally {
    if (Test-Path -LiteralPath $tempArtifacts) {
        Remove-Item -LiteralPath $tempArtifacts -Recurse -Force -ErrorAction SilentlyContinue
    }
}
