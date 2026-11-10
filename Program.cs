using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RazerTouchpadHelper.Helpers;
using RazerTouchpadHelper.UI;

namespace RazerTouchpadHelper
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            using var mutex = new Mutex(true, "RazerTouchpadHelper_SingleInstance", out bool createdNew);

            bool startHidden = false;
            foreach (var a in args)
                if (a == "--tray" || a == "/tray" || a == "-tray")
                    startHidden = true;

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!createdNew)
            {
                ShowAlreadyRunningDialog();
                return;
            }

            Application.Run(new MainForm(startHidden));
        }

        private static void ShowAlreadyRunningDialog()
        {
            string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "razer-logo.png");
            Image logo = null;
            if (File.Exists(pngPath))
            {
                try { logo = Image.FromFile(pngPath); }
                catch { }
            }

            using var form = new Form
            {
                Text = "Razer Touchpad Helper",
                Size = new Size(470, 200),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                TopMost = true,
                BackColor = RazerColors.BgDark,
                ForeColor = RazerColors.TextPrimary
            };
            form.Icon = LogoHelper.CreateWindowIcon(logo, 32);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = form.BackColor,
                Padding = new Padding(18),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var pic = new PictureBox
            {
                Image = logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 14, 0)
            };
            root.Controls.Add(pic, 0, 0);

            var textLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            var heading = new Label
            {
                Text = "Touchpad helper is already running",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = RazerColors.TextPrimary,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 4),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var body = new Label
            {
                Text = "Look for the Razer icon in the notification area (system tray).",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = RazerColors.TextSecondary,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var buttonPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
            var button = new Button
            {
                Text = "OK",
                Width = 92,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = RazerColors.Green,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK,
                Margin = new Padding(0)
            };
            button.FlatAppearance.BorderSize = 0;
            buttonPanel.Controls.Add(button);

            textLayout.Controls.Add(heading, 0, 0);
            textLayout.Controls.Add(body, 0, 1);
            textLayout.Controls.Add(buttonPanel, 0, 2);

            root.Controls.Add(textLayout, 1, 0);
            form.Controls.Add(root);
            form.AcceptButton = button;
            form.ShowDialog();

            logo?.Dispose();
        }
    }
}