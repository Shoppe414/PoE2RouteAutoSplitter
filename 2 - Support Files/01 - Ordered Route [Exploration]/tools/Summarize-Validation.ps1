param(
    [Parameter(Mandatory=$true)]
    [string]$ValidationCsv,
    [string]$Zones = (Join-Path $PSScriptRoot '..\zones.csv')
)

$expected = Import-Csv $Zones
$seen = Import-Csv $ValidationCsv
$seenIds = @{}
foreach ($row in $seen) { $seenIds[$row.AreaId] = $row }

$missing = @($expected | Where-Object { -not $seenIds.ContainsKey($_.AreaId) })
$unknown = @($seen | Where-Object { $_.Known -ne 'true' })

Write-Host "Known area/checklist entries: $($expected.Count)"
Write-Host "Unique IDs observed:          $($seenIds.Count)"
Write-Host "Missing checklist IDs:        $($missing.Count)"
Write-Host "Unknown observed IDs:         $($unknown.Count)"
Write-Host

if ($missing.Count -gt 0) {
    Write-Host "MISSING:" -ForegroundColor Yellow
    $missing | ForEach-Object { Write-Host ("  {0,-38} {1}" -f $_.AreaId, $_.AreaName) }
    Write-Host
}

if ($unknown.Count -gt 0) {
    Write-Host "UNKNOWN OBSERVED IDS:" -ForegroundColor Cyan
    $unknown | ForEach-Object { Write-Host ("  {0,-38} level={1}" -f $_.AreaId, $_.GeneratedLevel) }
}
