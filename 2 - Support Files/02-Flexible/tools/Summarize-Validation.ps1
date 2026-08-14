param(
    [Parameter(Mandatory=$true)]
    [string]$ValidationCsv,
    [string]$Zones = (Join-Path $PSScriptRoot '..\zones.csv')
)

$zones = Import-Csv $Zones
$expected = @($zones | Where-Object { $_.DefaultEnabled -eq 'true' })
$seen = Import-Csv $ValidationCsv
$seenIds = @{}
foreach ($row in $seen) { $seenIds[$row.AreaId] = $row }

$missing = @($expected | Where-Object { -not $seenIds.ContainsKey($_.AreaId) })
$unknown = @($seen | Where-Object { $_.Known -ne 'true' })

Write-Host "Default enabled areas:        $($expected.Count)"
Write-Host "Unique IDs observed:          $($seenIds.Count)"
Write-Host "Missing enabled IDs:          $($missing.Count)"
Write-Host "Unknown observed IDs:         $($unknown.Count)"
Write-Host

foreach ($group in @('Act 1','Act 2','Act 3','Act 4','Interludes')) {
    $gExpected = @($expected | Where-Object { $_.Subgroup -eq $group })
    $gSeen = @($gExpected | Where-Object { $seenIds.ContainsKey($_.AreaId) })
    Write-Host ("{0,-12} {1,2} / {2,2}" -f $group, $gSeen.Count, $gExpected.Count)
}
Write-Host

if ($missing.Count -gt 0) {
    Write-Host "MISSING:" -ForegroundColor Yellow
    $missing | ForEach-Object { Write-Host ("  {0,-38} {1,-28} [{2}]" -f $_.AreaId, $_.AreaName, $_.Subgroup) }
    Write-Host
}

if ($unknown.Count -gt 0) {
    Write-Host "UNKNOWN OBSERVED IDS:" -ForegroundColor Cyan
    $unknown | ForEach-Object { Write-Host ("  {0,-38} level={1}" -f $_.AreaId, $_.GeneratedLevel) }
}
