# Install Razer Touchpad Helper to start automatically at logon.
# Runs as the current user, no admin required. Fully reversible with remove.ps1.
#
# What it does (in order):
#   1. Creates a per-user Scheduled Task "Razer Touchpad Helper" that runs the
#      helper with --tray (starts hidden, lives in the notification area).
#   2. If the task cannot be created, falls back to a Startup-folder shortcut.

$ErrorActionPreference = 'Stop'

$TaskName = 'Razer Touchpad Helper'
$Exe = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Razer Touchpad Helper\bin\Release\net8.0-windows\Razer Touchpad Helper.exe'))

if (-not (Test-Path $Exe)) {
    Write-Host "ERROR: helper not found at:" -ForegroundColor Red
    Write-Host "  $Exe" -ForegroundColor Red
    Write-Host "Build it first:  dotnet build -c Release" -ForegroundColor Yellow
    Write-Host "  (in fix\Razer Touchpad Helper)" -ForegroundColor Yellow
    exit 1
}

Write-Host "Helper: $Exe"

# --- 1. Try a Scheduled Task at logon ---------------------------------------
$taskCreated = $false
try {
    $action = New-ScheduledTaskAction -Execute $Exe -Argument '--tray'
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
    $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -StartWhenAvailable `
        -ExecutionTimeLimit ([TimeSpan]::Zero) `
        -MultipleInstances IgnoreNew

    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger `
        -Principal $principal -Settings $settings -Description 'Razer Touchpad Helper - tap, scroll and gesture helper (touch only)' -Force | Out-Null

    $taskCreated = $true
    Write-Host "OK: Scheduled Task '$TaskName' created (starts at logon, runs as $env:USERNAME)." -ForegroundColor Green
}
catch {
    Write-Host "Scheduled Task creation failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

# --- 2. Fallback: Startup-folder shortcut ------------------------------------
if (-not $taskCreated) {
    try {
        $startup = [Environment]::GetFolderPath('Startup')
        $lnkPath = Join-Path $startup 'Razer Touchpad Helper.lnk'
        $ws = New-Object -ComObject WScript.Shell
        $lnk = $ws.CreateShortcut($lnkPath)
        $lnk.TargetPath = $Exe
        $lnk.Arguments = '--tray'
        $lnk.WorkingDirectory = Split-Path $Exe -Parent
        $lnk.Description = 'Razer Touchpad Helper - tap, scroll and gesture helper'
        $lnk.Save()
        Write-Host "OK: Startup shortcut created at:" -ForegroundColor Green
        Write-Host "  $lnkPath"
    }
    catch {
        Write-Host "ERROR: could not create autostart entry: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "To start it right now without logging off:"
Write-Host "  Start-Process `"$Exe`" -ArgumentList '--tray'"
Write-Host ""
Write-Host "To undo everything later, run:  .\remove.ps1"
