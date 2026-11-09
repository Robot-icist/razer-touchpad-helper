# Remove the TouchClick helper autostart entry and stop any running copy.
# Reverses install.ps1 completely. No admin required.

$ErrorActionPreference = 'Continue'

$TaskName = 'TouchClick'

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
    $lnkPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'TouchClick.lnk'
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
$proc = Get-Process TouchClick -ErrorAction SilentlyContinue
if ($proc) {
    $proc | Stop-Process -Force
    Write-Host "Stopped running helper (PID $($proc.Id -join ', '))." -ForegroundColor Green
}
else {
    Write-Host "Helper is not running."
}

# --- Restore pointer speed (if we changed it) ------------------------------
$speedFile = Join-Path $env:USERPROFILE 'razer-touchpad-fix\logs\touchclick-original-touchpad-cursor-speed.txt'
if (Test-Path $speedFile) {
    try {
        $orig = [int](Get-Content $speedFile -Raw).Trim()
        $regPath = "Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad"
        if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
        Set-ItemProperty -Path $regPath -Name CursorSpeed -Value $orig -Type DWord
        Write-Host "OK: Touchpad cursor speed restored to $orig." -ForegroundColor Green
    }
    catch {
        Write-Host "Could not restore touchpad cursor speed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}
else {
    Write-Host "No saved pointer-speed value found (nothing to restore)."
}

Write-Host ""
Write-Host "Done. Nothing was changed in the registry or drivers."
