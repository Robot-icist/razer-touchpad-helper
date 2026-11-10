using System;
using System.Collections.Generic;
using RazerTouchpadHelper.Models;

namespace RazerTouchpadHelper.Models
{
    public sealed class GestureState
    {
        public bool IsActive { get; set; }
        public DateTime StartTime { get; set; }
        public int StartContactCount { get; set; }
        public int MaxContacts { get; set; }
        public double MaxDisplacement { get; set; }
        public int FirstContactX { get; set; }
        public int FirstContactY { get; set; }
        public double FirstCentroidX { get; set; }
        public double FirstCentroidY { get; set; }
        public double LastCentroidX { get; set; }
        public double LastCentroidY { get; set; }
        public int TwoContactReportCount { get; set; }
        public bool SawSecondContact { get; set; }
        public bool HasScrolled { get; set; }
        public readonly Dictionary<int, (int X, int Y)> ContactStartPositions = new Dictionary<int, (int X, int Y)>();

        public void Reset()
        {
            IsActive = false;
            StartTime = default;
            StartContactCount = 0;
            MaxContacts = 0;
            MaxDisplacement = 0;
            FirstContactX = 0;
            FirstContactY = 0;
            FirstCentroidX = 0;
            FirstCentroidY = 0;
            LastCentroidX = 0;
            LastCentroidY = 0;
            TwoContactReportCount = 0;
            SawSecondContact = false;
            HasScrolled = false;
            ContactStartPositions.Clear();
        }

        public double DurationMs => (DateTime.UtcNow - StartTime).TotalMilliseconds;

        public double DeltaX => LastCentroidX - FirstCentroidX;
        public double DeltaY => LastCentroidY - FirstCentroidY;
        public double AbsDeltaX => Math.Abs(DeltaX);
        public double AbsDeltaY => Math.Abs(DeltaY);
        public double MaxAbsDelta => Math.Max(AbsDeltaX, AbsDeltaY);
    }

    public sealed class TapState
    {
        public DateTime LastTapTime { get; set; } = DateTime.MinValue;
        public int LastTapX { get; set; }
        public int LastTapY { get; set; }
        public bool LastTapValid { get; set; }
        public DateTime LastOneFingerTapInjectedAt { get; set; } = DateTime.MinValue;

        public bool IsDoubleClickWindowElapsed =>
            LastTapValid && (DateTime.UtcNow - LastTapTime).TotalMilliseconds <= System.Windows.Forms.SystemInformation.DoubleClickTime;

        public void Reset()
        {
            LastTapTime = DateTime.MinValue;
            LastTapX = 0;
            LastTapY = 0;
            LastTapValid = false;
        }
    }

    public sealed class DragState
    {
        public bool IsArming { get; set; }
        public bool IsActive { get; set; }
        public System.Drawing.Point StartPosition { get; set; }
        public bool StartPositionValid { get; set; }

        public void Reset()
        {
            IsArming = false;
            IsActive = false;
            StartPosition = System.Drawing.Point.Empty;
            StartPositionValid = false;
        }
    }

    public sealed class ScrollState
    {
        public bool IsActive { get; set; }
        public double PreviousCentroidX { get; set; }
        public double PreviousCentroidY { get; set; }
        public double AccumulatorV { get; set; }
        public double AccumulatorH { get; set; }
        public double SmoothV { get; set; }
        public double SmoothH { get; set; }
        public int Axis { get; set; } // 0 = vertical, 1 = horizontal
        public int LastSign { get; set; }
        public int VerticalNotches { get; set; }
        public int HorizontalNotches { get; set; }
        public bool HasScrolled { get; set; }

        public bool FreezeActive { get; set; }
        public System.Drawing.Point FreezePosition { get; set; }

        public void Reset()
        {
            IsActive = false;
            PreviousCentroidX = 0;
            PreviousCentroidY = 0;
            AccumulatorV = 0;
            AccumulatorH = 0;
            SmoothV = 0;
            SmoothH = 0;
            Axis = 0;
            LastSign = 0;
            VerticalNotches = 0;
            HorizontalNotches = 0;
            FreezeActive = false;
            FreezePosition = System.Drawing.Point.Empty;
        }
    }
}