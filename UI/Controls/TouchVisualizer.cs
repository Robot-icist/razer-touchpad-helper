using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RazerTouchpadHelper;
using RazerTouchpadHelper.Helpers;
using RazerTouchpadHelper.Models;

namespace RazerTouchpadHelper.UI.Controls
{
    public sealed class TouchVisualizer : Panel
    {
        private readonly List<TouchPoint> _points = new List<TouchPoint>();
        private readonly object _lock = new object();
        private readonly Func<AppSettings> _getSettings;

        public TouchVisualizer(Func<AppSettings> getSettings = null)
        {
            _getSettings = getSettings;
            DoubleBuffered = true;
        }

        public void UpdateContacts(List<Contact> contacts)
        {
            lock (_lock)
            {
                _points.Clear();
                foreach (var c in contacts)
                {
                    float px = (float)c.X / TouchConstants.MaxX;
                    float py = (float)c.Y / TouchConstants.MaxY;
                    _points.Add(new TouchPoint
                    {
                        Id = c.Id,
                        X = Math.Clamp(px, 0, 1),
                        Y = Math.Clamp(py, 0, 1),
                        Life = 1f
                    });
                }
                Invalidate();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _points.Clear();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Pad outline with rounded corners
            var padRect = new RectangleF(24, 24, Width - 48, Height - 48);
            float cornerRadius = 16f;

            // Background
            using (var brush = new SolidBrush(RazerColors.BgInput))
            using (var path = DrawingHelpers.GetRoundedRectF(padRect, cornerRadius))
                g.FillPath(brush, path);

            // Border
            using (var pen = new Pen(RazerColors.BorderColor, 1.5f))
            using (var path = DrawingHelpers.GetRoundedRectF(padRect, cornerRadius))
                g.DrawPath(pen, path);

            // Right zone indicator (uses settings for percentage and mirroring)
            var settings = _getSettings?.Invoke() ?? new AppSettings();
            float zonePercent = Math.Clamp(settings.RightZonePercent, 5, 60) / 100f;
            bool mirrorX = settings.MirrorX;

            float rightZoneX = mirrorX
                ? padRect.X + padRect.Width * zonePercent
                : padRect.X + padRect.Width * (1f - zonePercent);

            float zoneLeft = mirrorX ? padRect.X : rightZoneX;
            float zoneRight = mirrorX ? rightZoneX : padRect.Right;

            using (var brush = new SolidBrush(Color.FromArgb(15, RazerColors.Green)))
                g.FillRectangle(brush, zoneLeft, padRect.Y, zoneRight - zoneLeft, padRect.Height);
            using (var pen = new Pen(Color.FromArgb(40, RazerColors.Green), 1))
                g.DrawLine(pen, rightZoneX, padRect.Y, rightZoneX, padRect.Bottom);

            // Zone labels
            using (var font = new Font("Segoe UI", 7f, FontStyle.Bold))
            using (var brush = new SolidBrush(RazerColors.TextMuted))
            {
                string leftLabel = mirrorX ? "RIGHT CLICK" : "LEFT CLICK";
                string rightLabel = mirrorX ? "LEFT CLICK" : "RIGHT CLICK";
                float leftLabelX = mirrorX ? padRect.X + 12 : padRect.X + 12;
                float rightLabelX = mirrorX ? rightZoneX + 12 : rightZoneX + 12;
                g.DrawString(leftLabel, font, brush, leftLabelX, padRect.Y + 8);
                g.DrawString(rightLabel, font, brush, rightLabelX, padRect.Y + 8);
            }

            // Grid lines (subtle)
            using (var pen = new Pen(Color.FromArgb(0x00, 0x20, 0x08), 1))
            {
                for (int i = 1; i < 4; i++)
                {
                    float y = padRect.Y + padRect.Height * i / 4f;
                    g.DrawLine(pen, padRect.X, y, padRect.Right, y);
                    float x = padRect.X + padRect.Width * i / 4f;
                    g.DrawLine(pen, x, padRect.Y, x, padRect.Bottom);
                }
            }

            lock (_lock)
            {
                foreach (var p in _points)
                {
                    float x = padRect.X + p.X * padRect.Width;
                    float y = padRect.Y + p.Y * padRect.Height;
                    float size = Math.Clamp(28f * p.Life, 16f, 40f);
                    int alpha = (int)(255 * p.Life);

                    // Outer glow ring
                    using (var pen = new Pen(Color.FromArgb(alpha / 3, RazerColors.Green), 2))
                        g.DrawEllipse(pen, x - size / 2 - 4, y - size / 2 - 4, size + 8, size + 8);

                    // Contact circle
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, RazerColors.Green)))
                        g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);

                    // Inner white dot
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, Color.White)))
                        g.FillEllipse(brush, x - 5, y - 5, 10, 10);

                    // Contact ID
                    using (var font = new Font("Segoe UI", 7f, FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, RazerColors.TextPrimary)))
                        g.DrawString($"#{p.Id}", font, brush, x + size / 2 + 4, y - 8);
                }
            }
        }
    }
}