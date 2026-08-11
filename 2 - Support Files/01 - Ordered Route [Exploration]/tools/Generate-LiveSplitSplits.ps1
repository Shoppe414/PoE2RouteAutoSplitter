param(
    [string]$RouteFile = ".\poe2_route.txt",
    [string]$ZonesCsv = ".\zones.csv",
    [string]$OutputFile = ".\Path of Exile 2 - Generated Route.lss",
    [string]$CategoryName = "Campaign Route"
)

$ErrorActionPreference = 'Stop'

$zones = Import-Csv $ZonesCsv
$names = @{}
foreach ($zone in $zones) { $names[$zone.AreaId] = $zone.AreaName }

$route = New-Object System.Collections.Generic.List[string]
$lineNumber = 0
foreach ($raw in Get-Content $RouteFile) {
    $lineNumber++
    $line = $raw.Trim()
    if (-not $line -or $line.StartsWith('#') -or $line.StartsWith(';')) { continue }
    $hash = $line.IndexOf('#')
    if ($hash -ge 0) { $line = $line.Substring(0, $hash).Trim() }
    if (-not $line) { continue }
    if (-not $names.ContainsKey($line)) { throw "Unknown area ID '$line' on route line $lineNumber" }
    $route.Add($line)
}

if ($route.Count -eq 0) { throw 'Route contains no entries.' }

$doc = New-Object System.Xml.XmlDocument
$decl = $doc.CreateXmlDeclaration('1.0', 'UTF-8', $null)
$doc.AppendChild($decl) | Out-Null
$run = $doc.CreateElement('Run'); $run.SetAttribute('version','1.7.0'); $doc.AppendChild($run) | Out-Null

function Add-TextElement([System.Xml.XmlElement]$parent, [string]$name, [string]$value) {
    $e = $doc.CreateElement($name); $e.InnerText = $value; $parent.AppendChild($e) | Out-Null; return $e
}

$run.AppendChild($doc.CreateElement('GameIcon')) | Out-Null
Add-TextElement $run 'GameName' 'Path of Exile 2' | Out-Null
Add-TextElement $run 'CategoryName' $CategoryName | Out-Null
$metadata = $doc.CreateElement('Metadata'); $run.AppendChild($metadata) | Out-Null
$runMeta = $doc.CreateElement('Run'); $runMeta.SetAttribute('id',''); $metadata.AppendChild($runMeta) | Out-Null
$platform = Add-TextElement $metadata 'Platform' 'PC'; $platform.SetAttribute('usesEmulator','False')
$metadata.AppendChild($doc.CreateElement('Region')) | Out-Null
$metadata.AppendChild($doc.CreateElement('Variables')) | Out-Null
Add-TextElement $run 'Offset' '00:00:00' | Out-Null
Add-TextElement $run 'AttemptCount' '0' | Out-Null
$segments = $doc.CreateElement('Segments'); $run.AppendChild($segments) | Out-Null

foreach ($id in $route) {
    $segment = $doc.CreateElement('Segment'); $segments.AppendChild($segment) | Out-Null
    Add-TextElement $segment 'Name' $names[$id] | Out-Null
    $segment.AppendChild($doc.CreateElement('Icon')) | Out-Null
    $splitTimes = $doc.CreateElement('SplitTimes'); $segment.AppendChild($splitTimes) | Out-Null
    $splitTime = $doc.CreateElement('SplitTime'); $splitTime.SetAttribute('name','Personal Best'); $splitTimes.AppendChild($splitTime) | Out-Null
    $segment.AppendChild($doc.CreateElement('BestSegmentTime')) | Out-Null
    $segment.AppendChild($doc.CreateElement('SegmentHistory')) | Out-Null
}
$run.AppendChild($doc.CreateElement('AutoSplitterSettings')) | Out-Null

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = New-Object System.Text.UTF8Encoding($false)
$fullOutput = [System.IO.Path]::GetFullPath($OutputFile)
$parent = [System.IO.Path]::GetDirectoryName($fullOutput)
if ($parent -and -not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
$xmlWriter = [System.Xml.XmlWriter]::Create($fullOutput, $settings)
$doc.Save($xmlWriter)
$xmlWriter.Close()

Write-Host "Generated $($route.Count) LiveSplit segments: $fullOutput"
