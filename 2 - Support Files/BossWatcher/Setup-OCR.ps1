param(
    [string[]]$Language = @('all'),
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$tess = Join-Path $root 'tessdata'
New-Item -ItemType Directory -Force -Path $tess | Out-Null

# PoE2 authoritative game-client languages supported by this development build.
# App code -> Tesseract tessdata_fast model.
$models = @(
    [pscustomobject]@{ Code='en';    Tess='eng'; Name='English' },
    [pscustomobject]@{ Code='fr';    Tess='fra'; Name='Français' },
    [pscustomobject]@{ Code='de';    Tess='deu'; Name='Deutsch' },
    [pscustomobject]@{ Code='es-ES'; Tess='spa'; Name='Español (España)' },
    [pscustomobject]@{ Code='ja';    Tess='jpn'; Name='日本語' },
    [pscustomobject]@{ Code='ko';    Tess='kor'; Name='한국어' },
    [pscustomobject]@{ Code='pt-BR'; Tess='por'; Name='Português (Brasil)' },
    [pscustomobject]@{ Code='ru';    Tess='rus'; Name='Русский' },
    [pscustomobject]@{ Code='th';    Tess='tha'; Name='ไทย' }
)

$requested = @($Language | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$installAll = $requested.Count -eq 0 -or ($requested | Where-Object { $_ -ieq 'all' }).Count -gt 0
$selected = if ($installAll) {
    $models
} else {
    $found = New-Object System.Collections.Generic.List[object]
    foreach ($code in $requested) {
        $match = $models | Where-Object { $_.Code -ieq $code -or $_.Tess -ieq $code } | Select-Object -First 1
        if (-not $match) {
            throw "Unsupported OCR language '$code'. Supported PoE2 language codes: $($models.Code -join ', ')"
        }
        $found.Add($match)
    }
    @($found | Sort-Object Code -Unique)
}

foreach ($model in $selected) {
    $out = Join-Path $tess ($model.Tess + '.traineddata')
    if ((Test-Path -LiteralPath $out -PathType Leaf) -and -not $Force) {
        Write-Host "OCR data already present: $($model.Name) [$($model.Tess)]"
        continue
    }

    $url = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/$($model.Tess).traineddata"
    Write-Host "Downloading $($model.Name) OCR data [$($model.Tess)]..."
    Invoke-WebRequest -Uri $url -OutFile $out
    if (-not (Test-Path -LiteralPath $out -PathType Leaf) -or (Get-Item -LiteralPath $out).Length -le 0) {
        throw "OCR download failed or produced an empty file: $out"
    }
}

Write-Host ''
Write-Host "OCR setup complete. Installed models: $(@($selected).Tess -join ', ')"
