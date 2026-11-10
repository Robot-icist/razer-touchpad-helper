using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RazerTouchpadHelper.Helpers;

namespace RazerTouchpadHelper.UI.Controls
{
    public sealed class ModernToggle : Control
    {
        private bool _checked;
        private float _animPos = 0f;
        private readonly System.Windows.Forms.Timer _animTimer = new System.Windows.Forms.Timer { Interval = 10 };

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    _animTimer.Start();
                    Invalidate();
                }
            }
        }

        public event EventHandler CheckedChanged;

        public ModernToggle()
        {
            Size = new Size(52, 28);
            DoubleBuffered = true;
            _animTimer.Tick += (s, e) =>
            {
                float target = _checked ? 1f : 0f;
                _animPos += (target - _animPos) * 0.35f;
                if (Math.Abs(_animPos - target) < 0.02f)
                {
                    _animPos = target;
                    _animTimer.Stop();
                }
                Invalidate();
            };
            Click += (s, e) => { Checked = !_checked; CheckedChanged?.Invoke(this, EventArgs.Empty); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            int h = rect.Height;
            int trackW = rect.Width;
            int thumb = h - 4;

            Color trackColor = _checked ? RazerColors.Green : Color.FromArgb(60, 70, 60);
            using (var brush = new SolidBrush(trackColor))
            using (var path = DrawingHelpers.GetRoundedRectF(new RectangleF(0, 0, trackW, h), h / 2f))
                g.FillPath(brush, path);

            float thumbX = 2 + _animPos * (trackW - thumb - 4);
            using (var brush = new SolidBrush(Color.White))
            using (var path = DrawingHelpers.GetRoundedRectF(new RectangleF(thumbX, 2, thumb, thumb), thumb / 2f))
                g.FillPath(brush, path);

            if (_checked)
            {
                using var pen = new Pen(Color.FromArgb(100, RazerColors.Green), 2);
                g.DrawEllipse(pen, thumbX - 3, -1, thumb + 6, thumb + 6);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _animTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}