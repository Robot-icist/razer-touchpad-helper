using System.Text.Json.Serialization;

namespace RazerTouchpadHelper
{
    public sealed class AppSettings
    {
        public bool Enabled { get; set; } = true;
        public int RightZonePercent { get; set; } = 25;
        public int MaxTapMs { get; set; } = 700;
        public int MaxMove { get; set; } = 120;
        public bool TwoFingerRightClick { get; set; } = true;
        public bool ForceReleaseBeforeClick { get; set; } = true;
        public bool MirrorX { get; set; } = false;
        public bool TwoFingerScroll { get; set; } = true;
        public bool MultiFingerGestures { get; set; } = true;
        public bool InvertScroll { get; set; } = false;
        public bool FreezeCursorOnMultiFinger { get; set; } = true;
        public bool LongPressDrag { get; set; } = true;
        public int DragHoldMs { get; set; } = 300;
        public int ClickDelayMs { get; set; } = 15;
        public int ScrollSpeed { get; set; } = 15;
        public int SwipeThreshold { get; set; } = 450;

        // Legacy property kept for settings migration (no longer used)
        [JsonPropertyName("HorizontalScroll")]
        public bool HorizontalScroll { get; set; } = true;
    }
}