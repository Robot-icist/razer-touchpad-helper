# Razer Blade 16 touchpad fix (touch only)

Reversible, per-user Windows userspace helper that restores usable touchpad
input (taps, click, double-click, drag-and-drop, scroll, gestures) when the
precision touchpad stack is not delivering contacts. Nothing is changed in
drivers or registry. Everything is confined to `%USERPROFILE%\razer-touchpad-fix`
and `C:\projects\razer-touchpad-fix`; the task removes with one script.

## How it works

- Listens to the pad's Raw Input `0x54` vendor report. The physical button bit
  is always `1`, so it is ignored; clicks come from touch only.
- 1 finger: tap = LMB click (right 25% of the surface = RMB click).
- 2 fingers: tap = RMB; move = scroll (high-resolution, paced wheel deltas).
- 3/4 fingers: swipe = Task View / Show desktop / app switch / desktop switch;
  tap = search / notifications.
- A low-level mouse hook freezes the cursor while 2+ fingers are down.
- Long-press (=600 ms) starts a drag (left button held). Double-tap then
  move also starts a drag, so double-click / click-drag both work.
- Right-click zone is the rightmost `RightZonePercent` of the surface.

## Run / build

```powershell
cd C:\projects\razer-touchpad-fix\fix\TouchClick
dotnet build -c Release
# Start (hidden)
Start-Process .\bin\Release\net8.0-windows\TouchClick.exe -ArgumentList '--tray'
```

Open the tray icon (notification area) -> "Open settings". Settings are
**saved immediately** on every change to `%USERPROFILE%\razer-touchpad-fix\logs\touchclick-settings.json`
and reloaded on next start, including after reboot.

## Autostart / rollback

```powershell
cd C:\projects\razer-touchpad-fix\fix\autostart
.\install.ps1   # Scheduled Task at logon (tray, start hidden)
.\remove.ps1    # removes task + shortcut, stops the helper
```

No admin required; no system/driver/registry changes.

## Diagnostic tools

- `test\HidProbe\` captures the raw HID report (`hidprobe.exe rawinput`) to
  confirm contact reports/btn state. Logs go to
  `%USERPROFILE%\razer-touchpad-fix\logs`.

## Notes

- Logs/settings live in `%USERPROFILE%\razer-touchpad-fix\logs`.
- Source lives in `C:\projects\razer-touchpad-fix`.
- To exit for real: right-click tray icon -> Exit (window Close = hide).