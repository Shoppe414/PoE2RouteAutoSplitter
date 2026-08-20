param(
    [string]$Manifest = ""
)

$ErrorActionPreference = 'Stop'
$verificationRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageRoot = Split-Path -Parent $verificationRoot

if ([string]::IsNullOrWhiteSpace($Manifest)) {
    $runtime = Join-Path $verificationRoot 'RUNTIME-SHA256SUMS.txt'
    $source = Join-Path $verificationRoot 'SOURCE-PACKAGE-SHA256SUMS.txt'
    if (Test-Path -LiteralPath $runtime -PathType Leaf) { $Manifest = $runtime }
    elseif (Test-Path -LiteralPath $source -PathType Leaf) { $Manifest = $source }
    else { throw 'No SHA-256 manifest was found in 3 - verification files.' }
}

$Manifest = [IO.Path]::GetFullPath($Manifest)
$failures = 0
$checked = 0
foreach ($line in Get-Content -LiteralPath $Manifest) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith('#')) { continue }
    if ($line -notmatch '^([0-9a-fA-F]{64})\s{2}(.+)$') {
        Write-Warning "Skipping malformed manifest line: $line"
        continue
    }
    $expected = $Matches[1].ToLowerInvariant()
    $relative = $Matches[2]
    $path = Join-Path $packageRoot ($relative.Replace('/', [IO.Path]::DirectorySeparatorChar))
    $checked++
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Write-Host "MISSING  $relative" -ForegroundColor Red
        $failures++
        continue
    }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -eq $expected) { Write-Host "OK       $relative" }
    else {
        Write-Host "MISMATCH $relative" -ForegroundColor Red
        $failures++
    }
}
Write-Host "Checked $checked file(s); failures: $failures"
if ($failures -gt 0) { exit 1 }
