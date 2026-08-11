$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$tess = Join-Path $root 'tessdata'
New-Item -ItemType Directory -Force -Path $tess | Out-Null
$out = Join-Path $tess 'eng.traineddata'
$url = 'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/eng.traineddata'
Write-Host "Downloading Tesseract English trained data..."
Invoke-WebRequest -Uri $url -OutFile $out
Write-Host "Saved: $out"
Write-Host "OCR data setup complete."
Write-Host "Note: TesseractOCR also requires the Microsoft Visual C++ 2015-2022 x64 runtime."
