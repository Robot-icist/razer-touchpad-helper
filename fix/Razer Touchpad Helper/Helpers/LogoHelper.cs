using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;

namespace RazerTouchpadHelper.Helpers
{
    public static class LogoHelper
    {
        public static (Image Logo, Image Wordmark) LoadRazerLogo(string baseDirectory)
        {
            string pngPath = Path.Combine(baseDirectory, "razer-logo.png");
            if (File.Exists(pngPath))
            {
                try
                {
                    using var src = Image.FromFile(pngPath);
                    var wordmark = new Bitmap(src);
                    int size = src.Height;
                    var square = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(square))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, new Rectangle(0, 0, size, size), new Rectangle(0, 0, size, size), GraphicsUnit.Pixel);
                    }
                    return (square, wordmark);
                }
                catch { }
            }

            var fallback = CreateBrightLogo(256, 256);
            return (fallback, fallback);
        }

        public static Icon CreateWindowIcon(Image logo, int size = 32)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                if (logo != null)
                    g.DrawImage(logo, 2, 2, size - 4, size - 4);
                else
                    DrawFallbackLogo(g, size, size, 0, 0, size, size);
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        public static Icon CreateTrayIcon(Image logo, bool active, float animProgress = 0f)
        {
            var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var brush = new SolidBrush(Color.FromArgb(20, 25, 22)))
                    g.FillEllipse(brush, 2, 2, 28, 28);
                using (var pen = new Pen(active ? RazerColors.Green : RazerColors.BorderBright, 1.5f))
                    g.DrawEllipse(pen, 2, 2, 28, 28);

                if (logo != null)
                    g.DrawImage(logo, 4, 4, 24, 24);
                else
                    DrawFallbackLogo(g, 32, 32, 4, 4, 24, 24);

                if (active)
                {
                    using var pen = new Pen(Color.FromArgb((int)(180 * (1 - animProgress)), RazerColors.Green), 2);
                    float inset = 4 + animProgress * 8;
                    g.DrawEllipse(pen, inset, inset, 32 - inset * 2, 32 - inset * 2);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        public static Image CreateMenuIcon(Image logo, int size = 16)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                if (logo != null)
                    g.DrawImage(logo, 0, 0, size, size);
                else
                {
                    using var brush = new SolidBrush(RazerColors.Green);
                    PointF[] pts = { new PointF(size / 2f, 2), new PointF(size * 0.375f, size * 0.375f), new PointF(size * 0.625f, size * 0.375f) };
                    g.FillPolygon(brush, pts);
                }
            }
            return bmp;
        }

        private static void DrawFallbackLogo(Graphics g, int w, int h, int x = 0, int y = 0, int? drawW = null, int? drawH = null)
        {
            float cx = x + (drawW ?? w) / 2f;
            float cy = y + (drawH ?? h) / 2f;
            float s = Math.Min(drawW ?? w, drawH ?? h) * 0.36f;

            using var brush = new SolidBrush(RazerColors.Green);
            PointF[] h1 = { new PointF(cx, cy - s * 1.25f), new PointF(cx - s * 0.55f, cy - s * 0.35f), new PointF(cx + s * 0.55f, cy - s * 0.35f) };
            PointF[] h2 = { new PointF(cx - s * 0.85f, cy + s * 0.55f), new PointF(cx - s * 1.35f, cy + s * 1.15f), new PointF(cx - s * 0.35f, cy + s * 1.15f) };
            PointF[] h3 = { new PointF(cx + s * 0.85f, cy + s * 0.55f), new PointF(cx + s * 0.35f, cy + s * 1.15f), new PointF(cx + s * 1.35f, cy + s * 1.15f) };
            g.FillPolygon(brush, h1); g.FillPolygon(brush, h2); g.FillPolygon(brush, h3);
            using var pen = new Pen(RazerColors.Green, Math.Max(2f, w / 40f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, cx, cy - s * 0.3f, cx, cy + s * 0.15f);
            g.DrawLine(pen, cx, cy + s * 0.15f, cx - s * 0.65f, cy + s * 0.75f);
            g.DrawLine(pen, cx, cy + s * 0.15f, cx + s * 0.65f, cy + s * 0.75f);
        }

        private static Image CreateBrightLogo(int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);
                float cx = w / 2f, cy = h / 2f, s = Math.Min(w, h) * 0.36f;

                for (int i = 6; i >= 1; i--)
                {
                    using var brush = new SolidBrush(Color.FromArgb(18, 0, 255, 65));
                    g.FillEllipse(brush, cx - s * 2.2f - i, cy - s * 2.2f - i, s * 4.4f + i * 2, s * 4.4f + i * 2);
                }

                using var brush2 = new SolidBrush(RazerColors.Green);
                PointF[] h1 = { new PointF(cx, cy - s * 1.25f), new PointF(cx - s * 0.55f, cy - s * 0.35f), new PointF(cx + s * 0.55f, cy - s * 0.35f) };
                PointF[] h2 = { new PointF(cx - s * 0.85f, cy + s * 0.55f), new PointF(cx - s * 1.35f, cy + s * 1.15f), new PointF(cx - s * 0.35f, cy + s * 1.15f) };
                PointF[] h3 = { new PointF(cx + s * 0.85f, cy + s * 0.55f), new PointF(cx + s * 0.35f, cy + s * 1.15f), new PointF(cx + s * 1.35f, cy + s * 1.15f) };
                g.FillPolygon(brush2, h1); g.FillPolygon(brush2, h2); g.FillPolygon(brush2, h3);
                using var pen = new Pen(RazerColors.Green, Math.Max(2f, w / 40f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, cx, cy - s * 0.3f, cx, cy + s * 0.15f);
                g.DrawLine(pen, cx, cy + s * 0.15f, cx - s * 0.65f, cy + s * 0.75f);
                g.DrawLine(pen, cx, cy + s * 0.15f, cx + s * 0.65f, cy + s * 0.75f);
            }
            return bmp;
        }
    }
}