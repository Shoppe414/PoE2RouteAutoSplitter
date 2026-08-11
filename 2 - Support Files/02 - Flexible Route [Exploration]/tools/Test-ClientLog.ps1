param(
    [Parameter(Mandatory=$true)]
    [string]$ClientLog,
    [string]$Zones = (Join-Path $PSScriptRoot '..\zones.csv')
)

$areaRegex = '^[^ ]+ [^ ]+ (\d+).*Generating level (\d+) area "([^"]+)"'
$enteredRegex = '^[^ ]+ [^ ]+ (\d+).*: You have entered (.+)\.$'

$zoneRows = Import-Csv $Zones
$names = @{}
$idsByName = @{}
$enabled = @{}
$subgroup = @{}
foreach ($z in $zoneRows) {
    $names[$z.AreaId] = $z.AreaName
    $subgroup[$z.AreaId] = $z.Subgroup
    if (-not $idsByName.ContainsKey($z.AreaName)) { $idsByName[$z.AreaName] = $z.AreaId }
    if ($z.DefaultEnabled -eq 'true') { $enabled[$z.AreaId] = $true }
}

$completed = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$lastArea = $null
$splitCount = 0

Write-Host "Mode: unordered first-visit"
Write-Host "Default enabled areas: $($enabled.Count)"
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
    $group = if ($subgroup.ContainsKey($area)) { $subgroup[$area] } else { '<none>' }
    $levelText = if ($null -eq $level) { 'n/a' } else { $level }

    if ($area -eq 'G1_1') {
        Write-Host ("START  {0,-34} {1,-28} level={2} source={3}" -f $area, $name, $levelText, $source)
        return
    }

    if ($area -eq 'G_Endgame_Town') {
        Write-Host ("FINISH {0,-34} {1,-28} completed={2}/{3}" -f $area, $name, $completed.Count, $enabled.Count)
        return
    }

    if (-not $enabled.ContainsKey($area)) {
        Write-Host ("IGNORE {0,-34} {1,-28} reason=disabled/reference" -f $area, $name)
        return
    }

    if ($completed.Contains($area)) {
        Write-Host ("IGNORE {0,-34} {1,-28} reason=revisit" -f $area, $name)
        return
    }

    [void]$completed.Add($area)
    $splitCount++
    Write-Host ("SPLIT  {0,-34} {1,-28} subgroup={2} slot={3}" -f $area, $name, $group, $splitCount)
}

Write-Host
Write-Host "Unique enabled areas completed: $($completed.Count) / $($enabled.Count)"
