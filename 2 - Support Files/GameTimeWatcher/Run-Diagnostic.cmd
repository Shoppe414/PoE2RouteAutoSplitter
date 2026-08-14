@echo off
setlocal
echo PoE2 GameTimeWatcher diagnostic launcher
echo This window will remain open after the watcher exits or fails.
echo.
powershell.exe -NoExit -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-Diagnostic.ps1" %*
