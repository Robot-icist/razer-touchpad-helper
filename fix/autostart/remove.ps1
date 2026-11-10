# Remove the Razer Touchpad Helper autostart entry and stop any running copy.
# Reverses install.ps1 completely. No admin required.

$ErrorActionPreference = 'Continue'

$TaskName = 'Razer Touchpad Helper'

# --- Scheduled Task ---------------------------------------------------------
try {
    $existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    if ($existing) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Host "OK: Scheduled Task '$TaskName' removed." -ForegroundColor Green
    }
    else {
        Write-Host "Scheduled Task '$TaskName' not present."
    }
}
catch {
    Write-Host "Could not remove task: $($_.Exception.Message)" -ForegroundColor Yellow
}

# --- Startup-folder shortcut ------------------------------------------------
try {
    $startup = [Environment]::GetFolderPath('Startup')
    $lnkPath = Join-Path $startup 'Razer Touchpad Helper.lnk'
    if (Test-Path $lnkPath) {
        Remove-Item $lnkPath -Force
        Write-Host "OK: Startup shortcut removed." -ForegroundColor Green
    }
    else {
        Write-Host "Startup shortcut not present."
    }
}
catch {
    Write-Host "Could not remove shortcut: $($_.Exception.Message)" -ForegroundColor Yellow
}

# --- Stop a running copy ----------------------------------------------------
$proc = Get-Process "Razer Touchpad Helper" -ErrorAction SilentlyContinue
if ($proc) {
    $proc | Stop-Process -Force
    Write-Host "Stopped running helper (PID $($proc.Id -join ', '))." -ForegroundColor Green
}
else {
    Write-Host "Helper is not running."
}

Write-Host ""
Write-Host "Done. Nothing was changed in the registry or drivers."
