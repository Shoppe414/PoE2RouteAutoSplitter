param(
    [switch]$DevConsole,
    [string]$EventFile = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2BossWatcher\PoE2BossWatcher.csproj'
$runArgs = @()
if ($EventFile) {
    $runArgs += '--event-file'
    $runArgs += [System.IO.Path]::GetFullPath($EventFile)
}
if ($DevConsole) { $runArgs += '--dev-console' }

Push-Location $root
try {
    if ($runArgs.Count -gt 0) { dotnet run -c Release --project $project -- @runArgs }
    else { dotnet run -c Release --project $project }
} finally { Pop-Location }
