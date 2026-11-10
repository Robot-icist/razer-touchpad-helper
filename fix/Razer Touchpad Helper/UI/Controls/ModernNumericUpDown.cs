using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RazerTouchpadHelper.Helpers;

namespace RazerTouchpadHelper.UI.Controls
{
    public sealed class ModernNumericUpDown : NumericUpDown
    {
        public ModernNumericUpDown()
        {
            BorderStyle = BorderStyle.None;
            Font = new Font("Segoe UI", 11f);
            BackColor = RazerColors.BgInput;
            ForeColor = RazerColors.TextPrimary;
            TextAlign = HorizontalAlignment.Center;
            Controls[0].Visible = false; // Hide default up/down buttons
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(RazerColors.BorderColor, 1);
            using var path = DrawingHelpers.GetRoundedRect(ClientRectangle, 6);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            Invalidate();
        }
    }
}