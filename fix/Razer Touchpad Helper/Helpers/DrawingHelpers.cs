using System.Drawing;
using System.Drawing.Drawing2D;

namespace RazerTouchpadHelper.Helpers
{
    public static class DrawingHelpers
    {
        public static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath GetRoundedRectF(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawCardBorder(Graphics g, Rectangle rect, int radius = 12, float penWidth = 1.5f)
        {
            using var pen = new Pen(RazerColors.BorderBright, penWidth);
            using var path = GetRoundedRect(rect, radius);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawPath(pen, path);
        }

        public static void FillRoundedRect(Graphics g, Rectangle rect, int radius, Color color)
        {
            using var brush = new SolidBrush(color);
            using var path = GetRoundedRect(rect, radius);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillPath(brush, path);
        }
    }
}