param(
  [Parameter(Mandatory=$true)][string]$RouteFile,
  [string]$OutputLss = "Custom-Mixed.lss"
)
$objectives = @()
Get-Content $RouteFile | ForEach-Object {
  $line = ($_ -split '#',2)[0].Trim()
  if ($line -and -not $line.StartsWith('@')) {
    if ($line -notmatch '^(area|boss|bossocc|bossall|bossany|bossnth|bossslot|level)\|.+$') { throw "Invalid objective: $line" }
    $objectives += $line
  }
}
if ($objectives.Count -eq 0) { throw 'No objectives found.' }
[xml]$xml = '<Run version="1.7.0"><GameIcon/><GameName>Path of Exile 2</GameName><CategoryName>Mixed - Custom Exploration + Boss Rush</CategoryName><Metadata><Run id=""/><Platform usesEmulator="False">PC</Platform><Region/><Variables/></Metadata><Offset>00:00:00</Offset><AttemptCount>0</AttemptCount><Segments/></Run>'
for ($i=1; $i -le $objectives.Count; $i++) {
  $seg=$xml.CreateElement('Segment'); $name=$xml.CreateElement('Name'); $name.InnerText=('Objective {0:D3}' -f $i); $seg.AppendChild($name)|Out-Null
  $seg.AppendChild($xml.CreateElement('Icon'))|Out-Null; $st=$xml.CreateElement('SplitTimes'); $s=$xml.CreateElement('SplitTime'); $s.SetAttribute('name','Personal Best'); $st.AppendChild($s)|Out-Null; $seg.AppendChild($st)|Out-Null
  $seg.AppendChild($xml.CreateElement('BestSegmentTime'))|Out-Null; $seg.AppendChild($xml.CreateElement('SegmentHistory'))|Out-Null; $xml.Run.Segments.AppendChild($seg)|Out-Null
}
$xml.Save($OutputLss)
Write-Host "Created $OutputLss with $($objectives.Count) mixed objective slots."
