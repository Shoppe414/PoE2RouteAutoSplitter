$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2RouteSetup\PoE2RouteSetup.csproj'
$properNounRefresh = Join-Path $root 'Refresh-ProperNouns.ps1'
Write-Host 'Refreshing authoritative PoE2 boss/campaign/map area display names...'
& $properNounRefresh
Push-Location $root
try { dotnet run -c Release --project $project } finally { Pop-Location }
