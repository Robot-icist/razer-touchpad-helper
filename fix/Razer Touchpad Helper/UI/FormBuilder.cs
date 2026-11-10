using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RazerTouchpadHelper.Helpers;
using RazerTouchpadHelper.UI.Controls;

namespace RazerTouchpadHelper.UI
{
    public static class FormBuilder
    {
        public static Panel CreateCard(string title, int? fixedHeight = null)
        {
            var card = new Panel
            {
                BackColor = RazerColors.BgCard,
                Padding = new Padding(16, 44, 16, 16),
                Margin = new Padding(0),
                Dock = DockStyle.Fill
            };

            if (fixedHeight.HasValue)
                card.Height = fixedHeight.Value;

            card.Paint += (s, e) =>
            {
                DrawingHelpers.DrawCardBorder(e.Graphics, card.ClientRectangle);
            };

            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = RazerColors.Green,
                    AutoSize = true,
                    Location = new Point(18, 10)
                };
                card.Controls.Add(titleLabel);
            }

            return card;
        }

        public static Panel CreateDivider()
        {
            return new Panel
            {
                Height = 1,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 12, 0, 12),
                BackColor = RazerColors.BorderColor
            };
        }

        public static Button CreateModernButton(string text, int width, Action onClick, bool primary = false)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(width, 38),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = primary ? RazerColors.GreenDim : Color.FromArgb(0, 60, 15);
            btn.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(0, 140, 35) : Color.FromArgb(0, 80, 20);

            if (primary)
            {
                btn.BackColor = RazerColors.Green;
                btn.ForeColor = Color.FromArgb(0x0A, 0x0E, 0x0C);
            }
            else
            {
                btn.BackColor = RazerColors.BgCard;
                btn.ForeColor = RazerColors.Green;
            }

            btn.Paint += (s, e) =>
            {
                using var path = DrawingHelpers.GetRoundedRect(btn.ClientRectangle, 8);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (!primary)
                {
                    using var pen = new Pen(RazerColors.BorderColor, 1);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            btn.Click += (s, e) => onClick?.Invoke();
            return btn;
        }

        public static void AddToggle(
            FlowLayoutPanel parent,
            string text,
            bool checkedState,
            Action<bool> onChange,
            bool primary,
            string tooltip)
        {
            var item = new Panel
            {
                Size = new Size(420, 62),
                Margin = new Padding(0, 6, 0, 6),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = RazerColors.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 6),
                MaximumSize = new Size(300, 0)
            };

            var desc = new Label
            {
                Text = tooltip,
                Font = new Font("Segoe UI", 9f),
                ForeColor = RazerColors.TextMuted,
                AutoSize = true,
                Location = new Point(0, 34),
                MaximumSize = new Size(300, 0)
            };

            var toggle = new ModernToggle
            {
                Checked = checkedState,
                Location = new Point(item.Width - 62, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            toggle.CheckedChanged += (s, e) => onChange(toggle.Checked);

            var tip = new ToolTip { InitialDelay = 300, AutoPopDelay = 5000, ReshowDelay = 100 };
            tip.SetToolTip(item, tooltip);
            tip.SetToolTip(lbl, tooltip);
            tip.SetToolTip(toggle, tooltip);

            item.Controls.AddRange(new Control[] { lbl, desc, toggle });
            parent.Controls.Add(item);
        }

        public static void AddNumeric(
            FlowLayoutPanel parent,
            string label,
            int min,
            int max,
            int value,
            Action<int> onChange,
            string tooltip)
        {
            var item = new Panel
            {
                Size = new Size(420, 64),
                Margin = new Padding(0, 6, 0, 6),
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = RazerColors.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 4),
                MaximumSize = new Size(290, 0)
            };

            var desc = new Label
            {
                Text = tooltip,
                Font = new Font("Segoe UI", 9f),
                ForeColor = RazerColors.TextMuted,
                AutoSize = true,
                Location = new Point(0, 30),
                MaximumSize = new Size(290, 0)
            };

            var num = new ModernNumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Width = 100,
                Location = new Point(item.Width - 112, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            num.ValueChanged += (s, e) => onChange((int)num.Value);

            var tip = new ToolTip { InitialDelay = 300, AutoPopDelay = 5000 };
            tip.SetToolTip(item, tooltip);
            tip.SetToolTip(lbl, tooltip);
            tip.SetToolTip(num, tooltip);

            item.Controls.AddRange(new Control[] { lbl, desc, num });
            parent.Controls.Add(item);
        }

        public static void AddSlider(
            FlowLayoutPanel parent,
            string label,
            int min,
            int max,
            int value,
            Action<int> onChange,
            string tooltip)
        {
            var item = new Panel
            {
                Size = new Size(560, 96),
                Margin = new Padding(0, 8, 0, 8),
                BackColor = RazerColors.BgCard
            };

            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = RazerColors.TextPrimary,
                AutoSize = true,
                Location = new Point(16, 12),
                MaximumSize = new Size(420, 0)
            };

            var desc = new Label
            {
                Text = tooltip,
                Font = new Font("Segoe UI", 9f),
                ForeColor = RazerColors.TextMuted,
                AutoSize = true,
                Location = new Point(16, 40),
                MaximumSize = new Size(420, 0)
            };

            var track = new TrackBar
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Clamp(value, min, max),
                TickStyle = TickStyle.None,
                AutoSize = false,
                Size = new Size(360, 34),
                Location = new Point(16, 62),
                BackColor = RazerColors.BgCard
            };

            var val = new Label
            {
                Text = track.Value.ToString(),
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = RazerColors.Green,
                AutoSize = true,
                Location = new Point(392, 66),
                Size = new Size(96, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = RazerColors.BgCard
            };

            track.ValueChanged += (s, e) =>
            {
                try { onChange(track.Value); } catch { }
                val.Text = track.Value.ToString();
            };

            var tip = new ToolTip { InitialDelay = 300 };
            tip.SetToolTip(item, tooltip);
            tip.SetToolTip(lbl, tooltip);
            tip.SetToolTip(track, tooltip);
            tip.SetToolTip(val, tooltip);

            item.Controls.AddRange(new Control[] { lbl, desc, track, val });
            parent.Controls.Add(item);
        }
    }
}