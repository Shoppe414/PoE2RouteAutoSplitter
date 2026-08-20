param(
    [string]$StateFile = "",
    [string]$SettingsFile = "",
    [ValidateRange(100,5000)][int]$SampleMs = 500
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$supportRoot = Split-Path -Parent $root
$packageRoot = Split-Path -Parent $supportRoot

$exeCandidates = @(
    (Join-Path $root 'publish\PoE2GameTimeWatcher.exe'),
    (Join-Path $root 'PoE2GameTimeWatcher.exe')
)
$exe = $exeCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $exe) {
    throw "PoE2GameTimeWatcher.exe was not found. Run Build.ps1 in this folder first."
}

$existing = Get-Process -Name 'PoE2GameTimeWatcher' -ErrorAction SilentlyContinue | Where-Object { -not $_.HasExited }
if ($existing) {
    throw "PoE2GameTimeWatcher is already running (PID $($existing[0].Id)). Close it before starting a diagnostic run."
}

if ([string]::IsNullOrWhiteSpace($StateFile)) {
    $StateFile = Join-Path $packageRoot '1 - User Setup\LiveSplit Target\poe2_manual_pause_state.txt'
}
$StateFile = [System.IO.Path]::GetFullPath($StateFile)
$stateDir = Split-Path -Parent $StateFile
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null

if ([string]::IsNullOrWhiteSpace($SettingsFile)) {
    $SettingsFile = Join-Path $stateDir 'poe2_run_settings.json'
}
$SettingsFile = [System.IO.Path]::GetFullPath($SettingsFile)
$settingsHash = if (Test-Path -LiteralPath $SettingsFile -PathType Leaf) { (Get-FileHash -LiteralPath $SettingsFile -Algorithm SHA256).Hash } else { '<missing>' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$diagDir = Join-Path $packageRoot "4-README's_and_Diagnostics\Diagnostics"
$imageDir = Join-Path $diagDir 'images'
New-Item -ItemType Directory -Force -Path $diagDir, $imageDir | Out-Null
$prefix = "$stamp-"

$stdoutPath = Join-Path $diagDir ($prefix + 'watcher-stdout.log')
$stderrPath = Join-Path $diagDir ($prefix + 'watcher-stderr.log')
$samplesPath = Join-Path $diagDir ($prefix + 'process-samples.csv')
$metaPath = Join-Path $diagDir ($prefix + 'diagnostic-summary.txt')
$eventsPath = Join-Path $diagDir ($prefix + 'windows-application-events.txt')

$startTime = Get-Date
$exeItem = Get-Item -LiteralPath $exe
$exeHash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
$exeDir = Split-Path -Parent $exe
$configCandidates = @(
    (Join-Path $exeDir 'config.json'),
    (Join-Path $root 'config.json')
)
$configPath = $configCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $configPath) { $configPath = Join-Path $exeDir 'config.json' }
$configHash = if (Test-Path -LiteralPath $configPath) { (Get-FileHash -LiteralPath $configPath -Algorithm SHA256).Hash } else { '<missing>' }

@(
    "PoE2 GameTimeWatcher external crash diagnostic",
    "Started: $($startTime.ToString('o'))",
    "Diagnostic directory: $diagDir",
    "Diagnostic images: $imageDir",
    "Executable: $exe",
    "Executable SHA256: $exeHash",
    "File version: $($exeItem.VersionInfo.FileVersion)",
    "Product version: $($exeItem.VersionInfo.ProductVersion)",
    "Config: $configPath",
    "Config SHA256: $configHash",
    "State file: $StateFile",
    "Run settings: $SettingsFile",
    "Run settings SHA256: $settingsHash",
    "PowerShell: $($PSVersionTable.PSVersion)",
    "OS: $([System.Environment]::OSVersion.VersionString)",
    "64-bit OS: $([System.Environment]::Is64BitOperatingSystem)",
    "64-bit PowerShell process: $([System.Environment]::Is64BitProcess)",
    "Sample interval ms: $SampleMs",
    "Launcher revision: v3.0.0 centralized diagnostics",
    ""
) | Set-Content -LiteralPath $metaPath -Encoding UTF8

'Timestamp,Pid,WorkingSetBytes,PrivateBytes,VirtualBytes,HandleCount,ThreadCount,CpuMs,Responding' |
    Set-Content -LiteralPath $samplesPath -Encoding ASCII

# Start the watcher through System.Diagnostics.Process instead of Start-Process.
# PowerShell 5.1 Start-Process can mis-handle executable paths containing literal
# square brackets (for example: GameTimeWatcher) because
# its path binding can interpret them as wildcard syntax. By the time we reach
# this point the executable has already been located, hashed, and version-read
# using literal-path APIs, so use the .NET process API with that exact path.
#
# stdout/stderr intentionally inherit this diagnostic console. This keeps any
# startup exception visible to the user and avoids another path-sensitive
# redirection layer. GameTimeWatcher's --diagnostic-dir and --wait-on-error
# provide persistent diagnostics as well.
$argLine = '--state-file "' + $StateFile.Replace('"','\"') + '" --settings "' + $SettingsFile.Replace('"','\"') + '" --dev-console --wait-on-error --diagnostic-dir "' + $diagDir.Replace('"','\"') + '" --diagnostic-image-dir "' + $imageDir.Replace('"','\"') + '"'

try {
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $exe
    $psi.Arguments = $argLine
    $psi.WorkingDirectory = $exeDir
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $false
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError = $false

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    if (-not $process.Start()) {
        throw "System.Diagnostics.Process.Start returned false for: $exe"
    }

    "Watcher PID: $($process.Id)" | Add-Content -LiteralPath $metaPath -Encoding UTF8
    "Launch API: System.Diagnostics.ProcessStartInfo (literal executable path)" | Add-Content -LiteralPath $metaPath -Encoding UTF8
    "Console output: inherited by this PowerShell window" | Add-Content -LiteralPath $metaPath -Encoding UTF8
    Write-Host "GameTimeWatcher diagnostic started (PID $($process.Id))."
    Write-Host "Diagnostics: $diagDir"
    Write-Host "Leave this window open for the test run. If the watcher crashes, this script will capture its exit code and Windows crash events."

    while (-not $process.HasExited) {
        try {
            $process.Refresh()
            $timestamp = (Get-Date).ToString('o')
            $working = $process.WorkingSet64
            $private = $process.PrivateMemorySize64
            $virtual = $process.VirtualMemorySize64
            $handles = $process.HandleCount
            $threads = $process.Threads.Count
            $cpu = [Math]::Round($process.TotalProcessorTime.TotalMilliseconds, 3)
            $responding = $process.Responding
            "$timestamp,$($process.Id),$working,$private,$virtual,$handles,$threads,$cpu,$responding" |
                Add-Content -LiteralPath $samplesPath -Encoding ASCII
        }
        catch {
            # The process may have exited between HasExited and Refresh. The exit loop below is authoritative.
        }
        Start-Sleep -Milliseconds $SampleMs
    }

    $process.WaitForExit()
    $endTime = Get-Date
    $duration = $endTime - $startTime

    @(
        "Ended: $($endTime.ToString('o'))",
        "Elapsed: $duration",
        "Exit code: $($process.ExitCode)",
        ""
    ) | Add-Content -LiteralPath $metaPath -Encoding UTF8

    # Preserve the state/output logs at the exact end of the diagnostic session.
    if (Test-Path -LiteralPath $StateFile) {
        Copy-Item -LiteralPath $StateFile -Destination (Join-Path $diagDir ($prefix + 'final-state.txt')) -Force -ErrorAction SilentlyContinue
    }
    $watcherLog = Join-Path $diagDir 'poe2_gametimewatcher.log'
    if (Test-Path -LiteralPath $watcherLog) {
        Copy-Item -LiteralPath $watcherLog -Destination (Join-Path $diagDir ($prefix + 'poe2_gametimewatcher.log')) -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $configPath) {
        Copy-Item -LiteralPath $configPath -Destination (Join-Path $diagDir ($prefix + 'config.json')) -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $SettingsFile) {
        Copy-Item -LiteralPath $SettingsFile -Destination (Join-Path $diagDir ($prefix + 'poe2_run_settings.json')) -Force -ErrorAction SilentlyContinue
    }

    # Native CLR/application failures can terminate the process without a managed exception.
    # Query the Windows Application log after exit for the matching executable name.
    try {
        $eventStart = $startTime.AddMinutes(-1)
        $eventEnd = $endTime.AddMinutes(1)
        $events = Get-WinEvent -FilterHashtable @{ LogName='Application'; StartTime=$eventStart; EndTime=$eventEnd } -ErrorAction Stop |
            Where-Object {
                ($_.ProviderName -in @('.NET Runtime','Application Error','Windows Error Reporting')) -and
                ($_.Message -match 'PoE2GameTimeWatcher')
            } |
            Select-Object TimeCreated, ProviderName, Id, LevelDisplayName, Message

        if ($events) {
            $events | Format-List | Out-String -Width 400 | Set-Content -LiteralPath $eventsPath -Encoding UTF8
        }
        else {
            'No matching .NET Runtime / Application Error / Windows Error Reporting events were found.' |
                Set-Content -LiteralPath $eventsPath -Encoding UTF8
        }
    }
    catch {
        ("Could not query the Windows Application event log: " + $_.Exception.Message) |
            Set-Content -LiteralPath $eventsPath -Encoding UTF8
    }

    if ($process.ExitCode -eq 0) {
        Write-Host "GameTimeWatcher exited normally. Diagnostics saved to: $diagDir"
    }
    else {
        Write-Warning "GameTimeWatcher exited with code $($process.ExitCode). Diagnostics saved to: $diagDir"
    }
}
catch {
    $details = "Diagnostic launcher failure: " + $_.Exception.ToString()
    $details | Add-Content -LiteralPath $metaPath -Encoding UTF8
    $details | Set-Content -LiteralPath (Join-Path $diagDir ($prefix + 'launcher-error.txt')) -Encoding UTF8
    Write-Host ''
    Write-Error $details
    throw
}
