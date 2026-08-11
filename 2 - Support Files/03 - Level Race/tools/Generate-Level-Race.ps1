param([int]$TargetLevel = 100, [int]$SplitEvery = 10, [string]$OutputDirectory = ".")
if ($TargetLevel -lt 2 -or $TargetLevel -gt 100) { throw "TargetLevel must be 2..100" }
if ($SplitEvery -lt 1) { throw "SplitEvery must be >= 1" }
$levels = @()
for ($n = $SplitEvery; $n -lt $TargetLevel; $n += $SplitEvery) { if ($n -ge 2) { $levels += $n } }
if ($levels -notcontains $TargetLevel) { $levels += $TargetLevel }
$config = Join-Path $OutputDirectory 'poe2_level_race.txt'
$levels | Set-Content -Encoding UTF8 $config
[xml]$xml = '<Run version="1.7.0"><GameIcon/><GameName>Path of Exile 2</GameName><CategoryName></CategoryName><Metadata><Run id=""/><Platform usesEmulator="False">PC</Platform><Region/><Variables/></Metadata><Offset>00:00:00</Offset><AttemptCount>0</AttemptCount><Segments/></Run>'
$xml.Run.CategoryName = "Level $TargetLevel"
foreach ($level in $levels) {
  $seg = $xml.CreateElement('Segment'); $name = $xml.CreateElement('Name'); $name.InnerText = "Level $level"; $seg.AppendChild($name) | Out-Null
  $seg.AppendChild($xml.CreateElement('Icon')) | Out-Null
  $st = $xml.CreateElement('SplitTimes'); $s = $xml.CreateElement('SplitTime'); $s.SetAttribute('name','Personal Best'); $st.AppendChild($s) | Out-Null; $seg.AppendChild($st) | Out-Null
  $seg.AppendChild($xml.CreateElement('BestSegmentTime')) | Out-Null; $seg.AppendChild($xml.CreateElement('SegmentHistory')) | Out-Null
  $xml.Run.Segments.AppendChild($seg) | Out-Null
}
$lss = Join-Path $OutputDirectory "Path of Exile 2 - Level $TargetLevel.lss"; $xml.Save($lss)
Write-Host "Created $config and $lss with $($levels.Count) milestones."
