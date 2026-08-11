param(
    [Parameter(Mandatory=$true)]
    [string]$ClientLog,
    [string]$Route = (Join-Path $PSScriptRoot '..\poe2_route.txt'),
    [string]$Zones = (Join-Path $PSScriptRoot '..\zones.csv')
)

$areaRegex = '^[^ ]+ [^ ]+ (\d+).*Generating level (\d+) area "([^"]+)"'
$enteredRegex = '^[^ ]+ [^ ]+ (\d+).*: You have entered (.+)\.$'

$routeIds = Get-Content $Route |
    ForEach-Object { ($_ -split '#', 2)[0].Trim() } |
    Where-Object { $_ -ne '' -and -not $_.StartsWith(';') }

$zoneRows = Import-Csv $Zones
$names = @{}
$idsByName = @{}
foreach ($z in $zoneRows) {
    $names[$z.AreaId] = $z.AreaName
    if (-not $idsByName.ContainsKey($z.AreaName)) { $idsByName[$z.AreaName] = $z.AreaId }
}

$bad = @($routeIds | Where-Object { -not $names.ContainsKey($_) })
if ($bad.Count -gt 0) {
    Write-Error "Route contains unknown IDs: $($bad -join ', ')"
    exit 1
}

$index = 0
$lastArea = $null
$unknown = New-Object 'System.Collections.Generic.HashSet[string]'

Write-Host "Route entries: $($routeIds.Count)"
Write-Host "Reading: $ClientLog"
Write-Host

Get-Content $ClientLog | ForEach-Object {
    $area = $null
    $level = $null
    $source = $null

    if ($_ -match $areaRegex) {
        $level = [int]$Matches[2]
        $area = $Matches[3]
        $source = 'GeneratingLevel'
    }
    elseif ($_ -match $enteredRegex) {
        $enteredName = $Matches[2].Trim()
        if ($idsByName.ContainsKey($enteredName)) {
            $area = $idsByName[$enteredName]
            $source = 'EnteredName'
        }
    }

    if ($null -eq $area) { return }
    if ($area -eq $lastArea) { return }
    $lastArea = $area

    $name = if ($names.ContainsKey($area)) { $names[$area] } else { 'UNKNOWN AREA' }
    $expected = if ($index -lt $routeIds.Count) { $routeIds[$index] } else { '<complete>' }
    $expectedName = if ($names.ContainsKey($expected)) { $names[$expected] } else { $expected }
    $levelText = if ($null -eq $level) { 'n/a' } else { $level }

    if (-not $names.ContainsKey($area)) { [void]$unknown.Add($area) }

    if ($area -eq $expected) {
        $verb = if ($index -eq $routeIds.Count - 1 -and $area -eq 'G_Endgame_Town') { 'FINISH' } else { 'SPLIT ' }
        Write-Host ("{0} {1,-34} {2,-28} level={3} source={4} expected={5}" -f $verb, $area, $name, $levelText, $source, $expectedName)
        $index++
    } else {
        Write-Host ("IGNORE {0,-34} {1,-28} level={2} source={3} expected={4}" -f $area, $name, $levelText, $source, $expectedName)
    }
}

Write-Host
Write-Host "Matched $index / $($routeIds.Count) route entries."
if ($unknown.Count -gt 0) {
    Write-Host "Unknown area IDs observed:" -ForegroundColor Yellow
    $unknown | Sort-Object | ForEach-Object { Write-Host "  $_" }
}
