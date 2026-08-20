param(
    [Parameter(Mandatory=$true)][string]$StateFile,
    [string]$SettingsFile = "",
    [switch]$DevConsole
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\PoE2GameTimeWatcher\PoE2GameTimeWatcher.csproj'
$argsList = @('--state-file', $StateFile)
if ($SettingsFile) { $argsList += @('--settings', [System.IO.Path]::GetFullPath($SettingsFile)) }
if ($DevConsole) { $argsList += '--dev-console' }
dotnet run --project $project -- @argsList
