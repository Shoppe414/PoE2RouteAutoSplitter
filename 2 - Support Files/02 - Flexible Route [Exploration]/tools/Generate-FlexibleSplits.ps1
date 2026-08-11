param(
    [string]$Output = (Join-Path $PSScriptRoot '..\Path of Exile 2 - Campaign 100% Flexible.generated.lss'),
    [int]$AreaSlots = 97
)

$doc = New-Object System.Xml.XmlDocument
$decl = $doc.CreateXmlDeclaration('1.0', 'utf-8', $null)
[void]$doc.AppendChild($decl)
$run = $doc.CreateElement('Run')
$run.SetAttribute('version', '1.7.0')
[void]$doc.AppendChild($run)

function AddTextNode($parent, $name, $text) {
    $n = $doc.CreateElement($name)
    if ($null -ne $text) { $n.InnerText = $text }
    [void]$parent.AppendChild($n)
    return $n
}

[void](AddTextNode $run 'GameIcon' $null)
[void](AddTextNode $run 'GameName' 'Path of Exile 2')
[void](AddTextNode $run 'CategoryName' 'Campaign 100% Flexible')
$metadata = AddTextNode $run 'Metadata' $null
$r = $doc.CreateElement('Run'); $r.SetAttribute('id',''); [void]$metadata.AppendChild($r)
$p = $doc.CreateElement('Platform'); $p.SetAttribute('usesEmulator','False'); $p.InnerText='PC'; [void]$metadata.AppendChild($p)
[void](AddTextNode $metadata 'Region' $null)
[void](AddTextNode $metadata 'Variables' $null)
[void](AddTextNode $run 'Offset' '00:00:00')
[void](AddTextNode $run 'AttemptCount' '0')
$segments = AddTextNode $run 'Segments' $null

function AddSegment($name) {
    $segment = $doc.CreateElement('Segment')
    [void]$segments.AppendChild($segment)
    [void](AddTextNode $segment 'Name' $name)
    [void](AddTextNode $segment 'Icon' $null)
    $times = AddTextNode $segment 'SplitTimes' $null
    $splitTime = $doc.CreateElement('SplitTime'); $splitTime.SetAttribute('name','Personal Best'); [void]$times.AppendChild($splitTime)
    [void](AddTextNode $segment 'BestSegmentTime' $null)
    [void](AddTextNode $segment 'SegmentHistory' $null)
}

for ($i=1; $i -le $AreaSlots; $i++) {
    AddSegment ("Area Split {0:D3}" -f $i)
}
AddSegment 'The Ziggurat Refuge'

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.Encoding = New-Object System.Text.UTF8Encoding($false)
$writer = [System.Xml.XmlWriter]::Create($Output, $settings)
$doc.Save($writer)
$writer.Close()
Write-Host "Wrote $Output with $AreaSlots flexible area slots + Ziggurat Refuge."
