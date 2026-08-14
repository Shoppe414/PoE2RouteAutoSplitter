$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$tess = Join-Path $root 'tessdata'
$out = Join-Path $tess 'eng.traineddata'
$url = 'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/eng.traineddata'

# Use .NET filesystem/network APIs here instead of Invoke-WebRequest -OutFile.
# PowerShell treats '[' and ']' in paths as wildcard characters in some cmdlets,
# and this project intentionally uses a folder named "BossWatcher".
[System.IO.Directory]::CreateDirectory($tess) | Out-Null

Write-Host "Downloading Tesseract English trained data..."

$webClient = New-Object System.Net.WebClient
try {
    # GitHub requires modern TLS. This is safe on supported Windows/.NET versions.
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    $webClient.DownloadFile($url, $out)
}
finally {
    $webClient.Dispose()
}

if (-not [System.IO.File]::Exists($out)) {
    throw "OCR download did not create the expected file: $out"
}

$fileInfo = New-Object System.IO.FileInfo($out)
if ($fileInfo.Length -le 0) {
    throw "OCR download created an empty file: $out"
}

Write-Host "Saved: $out"
Write-Host "OCR data setup complete."
Write-Host "Note: TesseractOCR also requires the Microsoft Visual C++ 2015-2022 x64 runtime."
