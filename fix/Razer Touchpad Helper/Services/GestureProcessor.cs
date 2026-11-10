using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RazerTouchpadHelper;
using RazerTouchpadHelper.Models;
using NativeMethods = RazerTouchpadHelper.NativeMethods;

namespace RazerTouchpadHelper.Services
{
    public sealed class GestureProcessor
    {
        private readonly InputInjectionService _inputInjection;
        private readonly Func<AppSettings> _getSettings;
        private readonly Action _onDragStarted;
        private readonly Action _onDragEnded;
        private readonly Action<string> _writeLog;

        private readonly GestureState _gestureState = new GestureState();
        private readonly TapState _tapState = new TapState();
        private readonly DragState _dragState = new DragState();
        private readonly ScrollProcessor _scrollProcessor;
        private readonly System.Windows.Forms.Timer _dragTimer = new System.Windows.Forms.Timer();

        private const int TapDragMaxDistance = 1200;
        private const int TapDragMoveThreshold = 200;
        private const int DragArmSlop = 100;
        private const int MinTwoFingerTapReports = 2;
        private const double MinGestureDurationMs = 30;

        public GestureProcessor(
            InputInjectionService inputInjection,
            Func<AppSettings> getSettings,
            Action onDragStarted,
            Action onDragEnded,
            ScrollProcessor scrollProcessor,
            Action<string> writeLog)
        {
            _inputInjection = inputInjection;
            _getSettings = getSettings;
            _onDragStarted = onDragStarted;
            _onDragEnded = onDragEnded;
            _scrollProcessor = scrollProcessor;
            _writeLog = writeLog;

            _dragTimer.Tick += OnDragTimerTick;
        }

        public string LastTapText { get; private set; } = "(idle)";
        public int GesturesFired { get; private set; }
        public int ReportsProcessed { get; private set; }
        public int CurrentContacts { get; private set; }
        public DateTime LastReportTime { get; private set; }

        public void ProcessContacts(List<Contact> contacts)
        {
            ReportsProcessed++;
            LastReportTime = DateTime.UtcNow;
            CurrentContacts = contacts.Count;

            if (contacts.Count > 0)
            {
                ProcessActiveGesture(contacts);
            }
            else
            {
                ProcessContactLoss();
            }
        }

        private void ProcessActiveGesture(List<Contact> contacts)
        {
            double cx = 0, cy = 0;
            foreach (var c in contacts) { cx += c.X; cy += c.Y; }
            cx /= contacts.Count; cy /= contacts.Count;

            if (!_gestureState.IsActive)
            {
                StartGesture(contacts, cx, cy);
            }
            else
            {
                ContinueGesture(contacts, cx, cy);
            }

            _gestureState.LastCentroidX = cx;
            _gestureState.LastCentroidY = cy;
        }

        private void StartGesture(List<Contact> contacts, double cx, double cy)
        {
            _gestureState.Reset();
            _gestureState.IsActive = true;
            _gestureState.StartTime = DateTime.UtcNow;
            _gestureState.StartContactCount = contacts.Count;
            _gestureState.MaxContacts = contacts.Count;
            _gestureState.FirstContactX = contacts[0].X;
            _gestureState.FirstContactY = contacts[0].Y;
            _gestureState.FirstCentroidX = cx;
            _gestureState.FirstCentroidY = cy;
            _gestureState.TwoContactReportCount = contacts.Count >= 2 ? 1 : 0;
            _gestureState.SawSecondContact = contacts.Count >= 2;

            // Initialize drag state
            _dragState.Reset();
            if (NativeMethods.GetCursorPos(out var sp))
            {
                _dragState.StartPosition = new System.Drawing.Point(sp.X, sp.Y);
                _dragState.StartPositionValid = true;
            }

            // Check for double-tap hold to arm drag
            var settings = _getSettings();
            bool doubleTapHold = _tapState.IsDoubleClickWindowElapsed
                && Math.Abs(contacts[0].X - _tapState.LastTapX) <= TapDragMaxDistance
                && Math.Abs(contacts[0].Y - _tapState.LastTapY) <= TapDragMaxDistance;

            if (contacts.Count == 1 && settings.Enabled && settings.LongPressDrag && doubleTapHold)
            {
                _dragState.IsArming = true;
                _dragTimer.Interval = Math.Max(50, settings.DragHoldMs);
                _dragTimer.Start();
            }

            _tapState.LastTapValid = false;
        }

        private void ContinueGesture(List<Contact> contacts, double cx, double cy)
        {
            if (contacts.Count > _gestureState.MaxContacts)
                _gestureState.MaxContacts = contacts.Count;

            if (contacts.Count >= 2)
            {
                _gestureState.TwoContactReportCount++;
                if (!_gestureState.SawSecondContact)
                    _gestureState.SawSecondContact = true;
            }

            // Cancel drag arming if contact count changes
            if (_dragState.IsArming && contacts.Count != 1)
            {
                _dragState.IsArming = false;
                _dragTimer.Stop();
            }

            // Update contact start positions for displacement tracking
            foreach (var c in contacts)
            {
                if (!_gestureState.ContactStartPositions.TryGetValue(c.Id, out var start))
                    _gestureState.ContactStartPositions[c.Id] = (c.X, c.Y);
                else
                {
                    double dx = c.X - start.X;
                    double dy = c.Y - start.Y;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d > _gestureState.MaxDisplacement)
                        _gestureState.MaxDisplacement = d;
                }
            }

            var settings = _getSettings();

            // Process two-finger scroll
            if (contacts.Count == 2 && settings.Enabled && settings.TwoFingerScroll)
            {
                _scrollProcessor.Update(cx, cy, settings.FreezeCursorOnMultiFinger);
            }
            else
            {
                _scrollProcessor.Stop();
            }
        }

        private void ProcessContactLoss()
        {
            if (_gestureState.IsActive)
            {
                EndGesture();
            }
            _scrollProcessor.Stop();
        }

        public void EndGesture()
        {
            if (!_gestureState.IsActive) return;

            bool wasScroll = _gestureState.HasScrolled || _scrollProcessor.IsActive;
            bool wasFreeze = _scrollProcessor.IsActive; // ScrollProcessor manages freeze
            _scrollProcessor.Stop();

            bool wasDrag = _dragState.IsActive;
            if (_dragState.IsActive)
            {
                _dragState.IsActive = false;
                _inputInjection.InjectMouseButton(NativeMethods.MOUSEEVENTF_LEFTUP);
            }

            double durationMs = _gestureState.DurationMs;

            bool wasTap = false;

            if (!wasDrag && !wasScroll && IsTapGesture(durationMs))
            {
                ProcessTapGesture();
                wasTap = true;
            }
            else if (!wasScroll && IsTwoFingerTap(durationMs))
            {
                ProcessTwoFingerTap(durationMs);
                wasTap = true;
            }
            else if (!wasScroll && _gestureState.StartContactCount == 2 && _gestureState.TwoContactReportCount < 3)
            {
                LastTapText = "Ignored unstable tap";
            }
            else if (!wasScroll && IsMultiFingerGesture())
            {
                ProcessMultiFingerGesture();
            }
            else if (wasScroll)
            {
                LastTapText = $"Scroll: V={_scrollProcessor.VerticalNotches} H={_scrollProcessor.HorizontalNotches}";
            }

            // Reset LastTapValid for non-tap gestures to prevent stale double-tap detection
            if (!wasTap)
            {
                _tapState.LastTapValid = false;
            }

            if (wasDrag)
            {
                _onDragEnded?.Invoke();
                _tapState.LastTapValid = false;
                LastTapText = "Drag";
            }

            _gestureState.Reset();
            _dragState.Reset();
        }

        private bool IsTapGesture(double durationMs)
        {
            return (_gestureState.StartContactCount == 1 || _gestureState.TwoContactReportCount < MinTwoFingerTapReports)
                && _gestureState.MaxDisplacement <= _getSettings().MaxMove
                && durationMs <= _getSettings().MaxTapMs;
        }

        private bool IsTwoFingerTap(double durationMs)
        {
            var settings = _getSettings();
            return settings.Enabled && settings.TwoFingerRightClick
                && _gestureState.StartContactCount >= 2
                && _gestureState.TwoContactReportCount >= MinTwoFingerTapReports
                && durationMs >= MinGestureDurationMs
                && _gestureState.MaxDisplacement <= Math.Max(settings.MaxMove, 400)
                && durationMs <= settings.MaxTapMs;
        }

        private bool IsMultiFingerGesture()
        {
            var settings = _getSettings();
            return settings.MultiFingerGestures && settings.Enabled
                && (_gestureState.MaxContacts >= 3);
        }

        private void ProcessTapGesture()
        {
            var settings = _getSettings();
            bool right = IsRightZone(_gestureState.FirstContactX);

            _writeLog($"Tap: FirstContactX={_gestureState.FirstContactX}, RightZone={right}, Threshold={TouchConstants.MaxX * (100 - settings.RightZonePercent) / 100}");

            _inputInjection.InjectClick(right, settings.ForceReleaseBeforeClick, settings.ClickDelayMs);

            _tapState.LastTapTime = DateTime.UtcNow;
            _tapState.LastTapX = _gestureState.FirstContactX;
            _tapState.LastTapY = _gestureState.FirstContactY;
            _tapState.LastTapValid = true;
            _tapState.LastOneFingerTapInjectedAt = DateTime.UtcNow;

            LastTapText = right ? "Right click" : "Left click";
        }

        private void ProcessTwoFingerTap(double durationMs)
        {
            _inputInjection.InjectClick(true, _getSettings().ForceReleaseBeforeClick, _getSettings().ClickDelayMs);
            LastTapText = "Right click (2F)";
        }

        private void ProcessMultiFingerGesture()
        {
            if (_gestureState.MaxContacts == 3)
            {
                ProcessThreeFingerGesture();
            }
            else if (_gestureState.MaxContacts >= 4)
            {
                ProcessFourFingerGesture();
            }
        }

        private void ProcessThreeFingerGesture()
        {
            double dx = _gestureState.DeltaX;
            double dy = _gestureState.DeltaY;
            double adx = Math.Abs(dx), ady = Math.Abs(dy);

            if (Math.Max(adx, ady) >= _getSettings().SwipeThreshold)
            {
                if (ady > adx)
                {
                    _inputInjection.SendHotkey(NativeMethods.VK_LWIN, dy < 0 ? NativeMethods.VK_TAB : NativeMethods.VK_D);
                    LastTapText = dy < 0 ? "3F swipe up: Task View" : "3F swipe down: Show Desktop";
                }
                else
                {
                    _inputInjection.SendHotkey(NativeMethods.VK_LWIN, dx < 0 ? NativeMethods.VK_LEFT : NativeMethods.VK_RIGHT);
                    LastTapText = dx < 0 ? "3F swipe left: Prev App" : "3F swipe right: Next App";
                }
                GesturesFired++;
            }
            else if (_gestureState.MaxDisplacement <= _getSettings().MaxMove)
            {
                _inputInjection.SendHotkey(NativeMethods.VK_LWIN, NativeMethods.VK_S);
                LastTapText = "3F tap: Search";
                GesturesFired++;
            }
        }

        private void ProcessFourFingerGesture()
        {
            double dx = _gestureState.DeltaX;
            double dy = _gestureState.DeltaY;
            double adx = Math.Abs(dx), ady = Math.Abs(dy);

            if (Math.Max(adx, ady) >= _getSettings().SwipeThreshold)
            {
                if (ady > adx)
                {
                    _inputInjection.SendHotkey(NativeMethods.VK_LWIN, dy < 0 ? NativeMethods.VK_TAB : NativeMethods.VK_D);
                    LastTapText = dy < 0 ? "4F swipe up: Task View" : "4F swipe down: Show Desktop";
                }
                else
                {
                    _inputInjection.SendHotkey(NativeMethods.VK_CONTROL, NativeMethods.VK_LWIN, dx < 0 ? NativeMethods.VK_LEFT : NativeMethods.VK_RIGHT);
                    LastTapText = dx < 0 ? "4F swipe left: Prev Desktop" : "4F swipe right: Next Desktop";
                }
                GesturesFired++;
            }
            else if (_gestureState.MaxDisplacement <= _getSettings().MaxMove)
            {
                _inputInjection.SendHotkey(NativeMethods.VK_LWIN, NativeMethods.VK_A);
                LastTapText = "4F tap: Notifications";
                GesturesFired++;
            }
        }

        private bool IsRightZone(int x)
        {
            var settings = _getSettings();
            int threshold = TouchConstants.MaxX * (100 - settings.RightZonePercent) / 100;
            return settings.MirrorX ? x < threshold : x > threshold;
        }

        private void OnDragTimerTick(object sender, EventArgs e)
        {
            _dragTimer.Stop();
            if (!_dragState.IsArming || !_gestureState.IsActive || CurrentContacts != 1) return;

            if (NativeMethods.GetCursorPos(out var cp)
                && Math.Abs(cp.X - _dragState.StartPosition.X) <= DragArmSlop
                && Math.Abs(cp.Y - _dragState.StartPosition.Y) <= DragArmSlop)
            {
                _dragState.IsArming = false;
                _dragState.IsActive = true;
                _inputInjection.InjectMouseButton(NativeMethods.MOUSEEVENTF_LEFTDOWN);
                _onDragStarted?.Invoke();
            }
            else
            {
                _dragState.IsArming = false;
            }
        }

        public void WatchdogCheck()
        {
            if (_gestureState.IsActive && (DateTime.UtcNow - LastReportTime).TotalMilliseconds > 250)
            {
                if (_dragState.IsActive)
                {
                    _dragState.IsActive = false;
                    _inputInjection.InjectMouseButton(NativeMethods.MOUSEEVENTF_LEFTUP);
                }
                EndGesture();
            }

            if (_dragState.IsActive && !_gestureState.IsActive)
            {
                _dragState.IsActive = false;
                _inputInjection.InjectMouseButton(NativeMethods.MOUSEEVENTF_LEFTUP);
            }
        }

        public void StopDragArming()
        {
            _dragState.IsArming = false;
            _dragTimer.Stop();
        }
    }
}