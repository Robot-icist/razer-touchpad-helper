using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RazerTouchpadHelper.Helpers;

namespace RazerTouchpadHelper.UI.Controls
{
    public sealed class StatusPill : Panel
    {
        private bool _active;
        private bool _animActive;

        public StatusPill()
        {
            Width = 230;
            Height = 36;
            BackColor = Color.Transparent;
            DoubleBuffered = true;
        }

        public bool Active
        {
            get => _active;
            set
            {
                if (_active != value)
                {
                    _active = value;
                    Invalidate();
                }
            }
        }

        public bool AnimationActive
        {
            get => _animActive;
            set => _animActive = value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color bg = _active ? Color.FromArgb(0, 60, 15) : Color.FromArgb(30, 35, 40);
            Color dotColor = _active ? RazerColors.Green : Color.FromArgb(80, 90, 80);
            string text = _active ? "ACTIVE" : "IDLE";

            // Fixed pill height of 30px, centered in the rect
            int pillHeight = 30;
            int pillWidth = ClientRectangle.Width;
            int pillX = ClientRectangle.X;
            int pillY = ClientRectangle.Y + (ClientRectangle.Height - pillHeight) / 2;
            var pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);

            using (var path = DrawingHelpers.GetRoundedRect(pillRect, pillHeight / 2))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            // Status dot with pulse
            int dotSize = 10;
            int dotX = pillRect.X + 16;
            int dotY = pillRect.Y + (pillRect.Height - dotSize) / 2;
            using (var brush = new SolidBrush(dotColor))
                g.FillEllipse(brush, dotX, dotY, dotSize, dotSize);

            if (_active && _animActive)
            {
                using var pen = new Pen(Color.FromArgb(80, RazerColors.Green), 2);
                g.DrawEllipse(pen, dotX - 4, dotY - 4, dotSize + 8, dotSize + 8);
            }

            using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var brush = new SolidBrush(_active ? RazerColors.Green : RazerColors.TextMuted))
            {
                var txtSize = g.MeasureString(text, font);
                g.DrawString(text, font, brush, pillRect.X + 32, pillRect.Y + (pillRect.Height - txtSize.Height) / 2);
            }
        }
    }
}