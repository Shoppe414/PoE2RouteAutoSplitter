$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2RouteSetup\PoE2RouteSetup.csproj'
Push-Location $root
try { dotnet run -c Release --project $project } finally { Pop-Location }
