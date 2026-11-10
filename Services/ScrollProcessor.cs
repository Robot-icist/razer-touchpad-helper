using System;
using System.Drawing;
using RazerTouchpadHelper;
using RazerTouchpadHelper.Models;
using NativeMethods = RazerTouchpadHelper.NativeMethods;

namespace RazerTouchpadHelper.Services
{
    public sealed class ScrollProcessor
    {
        private readonly InputInjectionService _inputInjection;
        private readonly Func<AppSettings> _getSettings;

        private ScrollState _state = new ScrollState();

        // Constants matching the original logic exactly
        private const double WheelDelta = 120.0;
        private const double WheelUnitsPerDeviceUnitBase = 120.0 / 180.0;
        private const int MaxWheelUnitsPerReport = 360;
        private const int ScrollStartThreshold = 3; // Original: Math.Abs(dx) > 3 || Math.Abs(dy) > 3

        public ScrollProcessor(
            InputInjectionService inputInjection,
            Func<AppSettings> getSettings)
        {
            _inputInjection = inputInjection;
            _getSettings = getSettings;
        }

        public void Update(double centroidX, double centroidY, bool freezeCursorEnabled)
        {
            var settings = _getSettings();

            if (!_state.IsActive)
            {
                StartScroll(centroidX, centroidY, freezeCursorEnabled);
                return;
            }

            // Update freeze cursor position if enabled (original logic)
            if (_state.FreezeActive && NativeMethods.GetCursorPos(out var cp))
            {
                if (cp.X != _state.FreezePosition.X || cp.Y != _state.FreezePosition.Y)
                    NativeMethods.SetCursorPos(_state.FreezePosition.X, _state.FreezePosition.Y);
            }

            double dx = centroidX - _state.PreviousCentroidX;
            double dy = centroidY - _state.PreviousCentroidY;
            _state.PreviousCentroidX = centroidX;
            _state.PreviousCentroidY = centroidY;

            // Original: if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3) _gestureScrolled = true;
            if (Math.Abs(dx) > ScrollStartThreshold || Math.Abs(dy) > ScrollStartThreshold)
                _state.HasScrolled = true;

            if (_state.HasScrolled)
            {
                double units = Math.Max(1, settings.ScrollSpeed) * WheelUnitsPerDeviceUnitBase;
                double sign = settings.InvertScroll ? 1.0 : -1.0;

                // Original axis locking logic: determine dominant axis on first scroll, then lock to it
                if (_state.LastSign == 0)
                    _state.Axis = Math.Abs(dy) >= Math.Abs(dx) ? 0 : 1; // 0 = vertical, 1 = horizontal

                double axisDelta = _state.Axis == 0 ? dy : dx;
                bool horiz = _state.Axis == 1;

                if (Math.Sign(axisDelta) != 0)
                    _state.LastSign = Math.Sign(axisDelta);

                double raw = axisDelta * sign * units;
                raw = Math.Clamp(raw, -MaxWheelUnitsPerReport, MaxWheelUnitsPerReport);
                int wheel = (int)Math.Truncate(raw);

                if (wheel == 0 && raw != 0)
                    wheel = raw > 0 ? 1 : -1;

                if (wheel != 0)
                {
                    _inputInjection.InjectWheelDelta(wheel, horiz);
                    if (horiz)
                        _state.HorizontalNotches += wheel;
                    else
                        _state.VerticalNotches += wheel;
                }
            }
        }

        private void StartScroll(double centroidX, double centroidY, bool freezeCursorEnabled)
        {
            _state.Reset();
            _state.IsActive = true;
            _state.PreviousCentroidX = centroidX;
            _state.PreviousCentroidY = centroidY;
            _state.Axis = 0;
            _state.LastSign = 0;
            _state.HasScrolled = false;

            if (freezeCursorEnabled && NativeMethods.GetCursorPos(out var fp))
            {
                _state.FreezeActive = true;
                _state.FreezePosition = new System.Drawing.Point(fp.X, fp.Y);
                var rc = new NativeMethods.RECT
                {
                    left = fp.X,
                    top = fp.Y,
                    right = fp.X + 1,
                    bottom = fp.Y + 1
                };
                NativeMethods.ClipCursor(ref rc);
            }
        }

        public void Stop()
        {
            if (_state.FreezeActive)
            {
                NativeMethods.ClipCursor(IntPtr.Zero);
                if (_state.FreezePosition != Point.Empty)
                    NativeMethods.SetCursorPos(_state.FreezePosition.X, _state.FreezePosition.Y);
            }
            _state.Reset();
        }

        public bool IsActive => _state.IsActive;
        public int VerticalNotches => _state.VerticalNotches;
        public int HorizontalNotches => _state.HorizontalNotches;
    }
}