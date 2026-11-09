using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace TouchClick
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            using var mutex = new Mutex(true, "TouchClickHelper_SingleInstance", out bool createdNew);

            bool startHidden = false;
            foreach (var a in args)
                if (a == "--tray" || a == "/tray" || a == "-tray") startHidden = true;

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

    private static Image LoadRazerLogoImage()
    {
        string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "razer-logo.png");
        if (File.Exists(pngPath))
        {
            try { return new Bitmap(pngPath); }
            catch { }
        }
        return null;
    }

    private static Icon CreateRazerAppIcon()
    {
        string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "razer-logo.png");
        if (!File.Exists(pngPath)) return null;
        try
        {
            using var src = Image.FromFile(pngPath);
            int size = Math.Min(src.Width, src.Height);
            var square = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(square))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, new Rectangle(0, 0, size, size), new Rectangle(0, 0, size, size), GraphicsUnit.Pixel);
            }
            return Icon.FromHandle(square.GetHicon());
        }
        catch { return null; }
    }

    private static void ShowAlreadyRunningDialog()
    {
        using var logo = LoadRazerLogoImage();
        using var form = new Form
        {
            Text = "Razer Touchpad Helper",
            Size = new Size(470, 200),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true,
            BackColor = Color.FromArgb(0x12, 0x14, 0x18),
            ForeColor = Color.FromArgb(0xE6, 0xF0, 0xE6)
        };
        form.Icon = CreateRazerAppIcon();

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
            ForeColor = Color.FromArgb(0xE6, 0xF0, 0xE6),
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var body = new Label
        {
            Text = "Look for the Razer icon in the notification area (system tray).",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(0xB8, 0xD4, 0xB8),
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
            BackColor = Color.FromArgb(0x00, 0x99, 0x26),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            DialogResult = DialogResult.OK,
            Margin = new Padding(0)
        };
        buttonPanel.Controls.Add(button);

        textLayout.Controls.Add(heading, 0, 0);
        textLayout.Controls.Add(body, 0, 1);
        textLayout.Controls.Add(buttonPanel, 0, 2);

        root.Controls.Add(textLayout, 1, 0);
        form.Controls.Add(root);
        form.AcceptButton = button;
        form.ShowDialog();
    }

    }

    internal sealed class AppSettings
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
        public int ScrollSpeed { get; set; } = 15;
        public int SwipeThreshold { get; set; } = 450;
    }

    internal sealed class MainForm : Form
    {
        // ---- Touchpad geometry ----
        private const int TouchMaxX = 4613;
        private const int TouchMaxY = 2918;

        // ---- Raw Input ----
        private const uint WM_INPUT = 0x00FF;
        private const uint RID_INPUT = 0x10000003;
        private const int RIM_TYPEHID = 2;
        private const uint RIDEV_INPUTSINK = 0x00000100;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWHID hid;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint numDevices, uint size);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint command, IntPtr data, ref uint size, uint headerSize);

        // ---- SendInput ----
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x1000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const ushort VK_SHIFT = 0x10;
        private const ushort VK_CONTROL = 0x11;
        private const ushort VK_MENU = 0x12;
        private const ushort VK_TAB = 0x09;
        private const ushort VK_LEFT = 0x25;
        private const ushort VK_RIGHT = 0x27;
        private const ushort VK_A = 0x41;
        private const ushort VK_D = 0x44;
        private const ushort VK_S = 0x53;
        private const ushort VK_LWIN = 0x5B;

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT { public uint type; public INPUTUNION u; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

         [StructLayout(LayoutKind.Sequential)]
         private struct RECT { public int left, top, right, bottom; }
         [DllImport("user32.dll")]
         private static extern bool ClipCursor(ref RECT lpRect);
         [DllImport("user32.dll")]
         private static extern bool ClipCursor(IntPtr lpRect);

        // ---- Pointer device enumeration ----
        private const int POINTER_DEVICE_PRODUCT_STRING_MAX = 520;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct POINTER_DEVICE_INFO
        {
            public uint displayOrientation;
            public IntPtr device;
            public int pointerDeviceType;
            public IntPtr monitor;
            public uint startingCursorId;
            public ushort maxActiveContacts;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = POINTER_DEVICE_PRODUCT_STRING_MAX)]
            public string productString;
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetPointerDevices(ref uint deviceCount, [Out] POINTER_DEVICE_INFO[] pointerDevices);

        // ---- UI ----
        private readonly string _logPath;
        private readonly string _settingsPath;
        private readonly StreamWriter _file;
        private readonly object _sync = new object();
        private readonly Queue<string> _pending = new Queue<string>();

        private AppSettings _settings;
        private int _reports, _curContacts, _injected, _wheelEvents, _gesturesFired;
        private bool? _btn1; private int _btnFlips;
        private string _lastContactText = "(none)";
        private string _lastTapText = "(idle)";
        private DateTime _lastReportAt = DateTime.UtcNow;
        private int _emptyReports;
        private const int ContactLossDebounce = 4;

        // Gesture tracking
        private bool _gestureActive;
        private DateTime _gestureStart;
        private int _gestureMaxContacts;
        private double _gestureMaxDisp;
        private int _gestureFirstX, _gestureFirstY;
        private double _gestureFirstCx, _gestureFirstCy;
        private double _gestureLastCx, _gestureLastCy;
        private readonly Dictionary<int, (int X, int Y)> _gestureStarts = new Dictionary<int, (int X, int Y)>();

        // Two-finger scroll
        private bool _twoFingerScrollActive, _gestureScrolled;
        private double _scrollPrevCx, _scrollPrevCy;
        private double _scrollAccumV, _scrollAccumH, _scrollSmoothV, _scrollSmoothH;
        private int _scrollAxis, _scrollLastSign, _scrollVNotches, _scrollHNotches;
        private const double WheelDelta = 120.0;
        private const double WheelUnitsPerDeviceUnitBase = 120.0 / 180.0;
        private const double ScrollSmoothing = 1.0;
        private const int MaxWheelUnitsPerReport = 360;

        // Cursor freeze during 2-finger scroll
        private bool _scrollFreezeActive;
        private Point _scrollFreezePos;

        // Tap / drag state
        private Point _touchStartPos; private bool _touchStartValid;
        private bool _dragArming, _dragActive;
        private const int DragArmSlop = 100;

        // Tap / double-click
        private DateTime _lastTapAt = DateTime.MinValue;
        private int _lastTapX, _lastTapY;
        private bool _lastTapValid;
        private bool _doubleTapPrimed;
        private bool _quickTapPrimed;
        private DateTime _lastOneFingerTapInjectedAt = DateTime.MinValue;
        private int _gestureStartContactCount;
        private int _gestureTwoContactReports;
        private bool _gestureSawSecondContact;
        private int _lastClickScreenX, _lastClickScreenY;
        private bool _lastClickScreenValid, _tapDragCandidate, _dblInject;
        private const int TapDragMaxDistance = 1200;
        private const int TapDragMoveThreshold = 200;

        // Test-area
        // Test click buttons were removed per user preference.

        // UI Controls
        private TouchVisualizer _touchVisualizer;
        private TextBox _logBox;
        private Label _lblContacts, _lblGesture, _lblReports, _lblStatus;
        private Panel _statusPill;
        private NotifyIcon _trayIcon;
        private ToolStripMenuItem _miEnabled, _miScroll, _miGestures;
        private readonly System.Windows.Forms.Timer _uiTimer = new System.Windows.Forms.Timer { Interval = 33 };
        private readonly System.Windows.Forms.Timer _dragTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _trayAnimTimer = new System.Windows.Forms.Timer { Interval = 50 };
        private int _trayAnimFrame = 0;
        private bool _trayAnimActive = false;
        private Icon _trayIconBase, _trayIconActive;
        private Image _razerLogoImage;
        private Image _razerWordmarkImage;
        private bool _exiting, _startHidden;

        // Colors - Razer 2026 Theme
        private static readonly Color RazerGreen = Color.FromArgb(0x00, 0xFF, 0x41);
        private static readonly Color RazerGreenDim = Color.FromArgb(0x00, 0xB4, 0x2D);
        private static readonly Color RazerGreenGlow = Color.FromArgb(60, 0x00, 0xFF, 0x41);
        private static readonly Color BgDark = Color.FromArgb(0x0C, 0x0E, 0x10);
        private static readonly Color BgCard = Color.FromArgb(0x12, 0x14, 0x18);
        private static readonly Color BgCardHover = Color.FromArgb(0x18, 0x1C, 0x20);
        private static readonly Color BorderColor = Color.FromArgb(0x00, 0x99, 0x26);
        private static readonly Color BorderBright = Color.FromArgb(0x00, 0x80, 0x20);
        private static readonly Color TextPrimary = Color.FromArgb(0xE6, 0xF0, 0xE6);
        private static readonly Color TextSecondary = Color.FromArgb(0xB8, 0xD4, 0xB8);
        private static readonly Color TextMuted = Color.FromArgb(0x8A, 0xA6, 0x8A);
        private static readonly Color TextDisabled = Color.FromArgb(0x3A, 0x4A, 0x3A);
        private static readonly Color BgInput = Color.FromArgb(0x08, 0x0A, 0x0C);
        private static readonly Color AccentGreen = Color.FromArgb(0x00, 0xE6, 0x3A);

        public MainForm(bool startHidden)
        {
            _startHidden = startHidden;

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "razer-touchpad-fix", "logs");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, $"touchclick-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            _settingsPath = Path.Combine(dir, "touchclick-settings.json");
            _file = new StreamWriter(_logPath, append: false, Encoding.UTF8) { AutoFlush = true };
            _settings = LoadSettings();
            _settings.MaxMove = Math.Clamp(_settings.MaxMove, 20, 500);
            _settings.SwipeThreshold = Math.Clamp(_settings.SwipeThreshold, 100, 1200);
            _settings.DragHoldMs = Math.Clamp(_settings.DragHoldMs, 100, 2000);
            // Pointer-speed Windows-touchpad setting removed per user request.

            LoadRazerLogo();
            BuildUi();
            SetupTray();
            RegisterRawInput();

            WriteLog("=== Razer Touchpad Helper v3 ===");
            WriteLog($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}  StartHidden={startHidden}");
            WriteLog($"Log:  {_logPath}");
            WriteLog("Physical button bit: IGNORED. All input from touch only.");
            WriteLog("1-finger tap = click (right zone = right click)");
            WriteLog("2-finger = scroll | 2-finger tap = right click");
            WriteLog("3-finger: up=Task View, down=Show Desktop, left/right=Switch App, tap=Search");
            WriteLog("4-finger: up=Task View, down=Show Desktop, left/right=Switch Desktop, tap=Notifications");
            WriteLog("Raw Input: 0x0D:0x05 ONLY (no mouse conflict)");
            WriteLog("Low-level mouse hook: DISABLED (zero global latency)");
        }

        // =====================================================================
        //  RAZER LOGO LOADING
        // =====================================================================
        private void LoadRazerLogo()
        {
            string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "razer-logo.png");
            if (File.Exists(pngPath))
            {
                try
                {
                    var src = Image.FromFile(pngPath);
                    _razerWordmarkImage = new Bitmap(src);
                    int size = src.Height;
                    var square = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                    using (var g = Graphics.FromImage(square))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, new Rectangle(0, 0, size, size), new Rectangle(0, 0, size, size), GraphicsUnit.Pixel);
                    }
                    _razerLogoImage = square;
                    src.Dispose();
                    return;
                }
                catch { }
            }

            _razerLogoImage = CreateBrightLogo(256, 256);
            _razerWordmarkImage = _razerLogoImage;
        }

        private Image RenderSvgToBitmap(string svgPath, int width, int height)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(svgPath);
                var svg = doc.DocumentElement;
                string viewBox = svg.GetAttribute("viewBox");
                if (string.IsNullOrEmpty(viewBox)) viewBox = $"0 0 {width} {height}";

                var parts = viewBox.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                float vbX = float.Parse(parts[0]), vbY = float.Parse(parts[1]);
                float vbW = float.Parse(parts[2]), vbH = float.Parse(parts[3]);

                var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                bmp.SetResolution(96, 96);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Transparent);

                    // Scale to fit
                    float scale = Math.Min(width / vbW, height / vbH) * 0.95f;
                    float tx = (width - vbW * scale) / 2f;
                    float ty = (height - vbH * scale) / 2f;
                    g.TranslateTransform(tx, ty);
                    g.ScaleTransform(scale, scale);
                    g.TranslateTransform(-vbX, -vbY);

                    // Render paths
                    RenderSvgNode(g, svg, RazerGreen);
                }
                return bmp;
            }
            catch { return null; }
        }

        private void RenderSvgNode(Graphics g, XmlNode node, Color defaultColor)
        {
            if (node.Name == "path")
            {
                string d = node.Attributes?["d"]?.Value;
                if (!string.IsNullOrEmpty(d))
                {
                    using (var path = ParseSvgPath(d))
                    using (var brush = new SolidBrush(RazerGreen))
                        g.FillPath(brush, path);
                }
            }
            foreach (XmlNode child in node.ChildNodes)
                RenderSvgNode(g, child, defaultColor);
        }

        private GraphicsPath ParseSvgPath(string d)
        {
            var path = new GraphicsPath();
            var tokens = TokenizeSvgPath(d);
            float cx = 0, cy = 0, startX = 0, startY = 0;
            char lastCmd = ' ';

            for (int i = 0; i < tokens.Count;)
            {
                string token = tokens[i++];
                char cmd = char.ToUpper(token[0]);
                bool relative = char.IsLower(token[0]);

                // Handle implicit commands
                if (!"MMLLHHVVCCSSTTQQAaZz".Contains(token[0]))
                {
                    i--; cmd = lastCmd; relative = char.IsLower(lastCmd.ToString()[0]);
                }

                Action<float, float> map = (x, y) => { cx = relative ? cx + x : x; cy = relative ? cy + y : y; };

                switch (cmd)
                {
                    case 'M':
                        map(float.Parse(tokens[i++]), float.Parse(tokens[i++]));
                        path.StartFigure();
                        startX = cx; startY = cy;
                        lastCmd = 'L'; break;
                    case 'L':
                        map(float.Parse(tokens[i++]), float.Parse(tokens[i++]));
                        path.AddLine(path.GetLastPoint(), new PointF(cx, cy));
                        lastCmd = 'L'; break;
                    case 'H':
                        cx = relative ? cx + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        path.AddLine(path.GetLastPoint(), new PointF(cx, cy));
                        lastCmd = 'L'; break;
                    case 'V':
                        cy = relative ? cy + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        path.AddLine(path.GetLastPoint(), new PointF(cx, cy));
                        lastCmd = 'L'; break;
                    case 'C':
                        var c1x = relative ? cx + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        var c1y = relative ? cy + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        var c2x = relative ? cx + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        var c2y = relative ? cy + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        map(float.Parse(tokens[i++]), float.Parse(tokens[i++]));
                        path.AddBezier(path.GetLastPoint(), new PointF(c1x, c1y), new PointF(c2x, c2y), new PointF(cx, cy));
                        lastCmd = 'C'; break;
                    case 'S':
                        var sc2x = relative ? cx + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        var sc2y = relative ? cy + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        map(float.Parse(tokens[i++]), float.Parse(tokens[i++]));
                        PointF lastPt = path.GetLastPoint();
                        // Simplified: treat as C with reflected control point
                        path.AddBezier(lastPt, lastPt, new PointF(sc2x, sc2y), new PointF(cx, cy));
                        lastCmd = 'C'; break;
                    case 'Q':
                        var qcx = relative ? cx + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        var qcy = relative ? cy + float.Parse(tokens[i++]) : float.Parse(tokens[i++]);
                        map(float.Parse(tokens[i++]), float.Parse(tokens[i++]));
                        path.AddBezier(path.GetLastPoint(), new PointF(qcx, qcy), new PointF(qcx, qcy), new PointF(cx, cy));
                        lastCmd = 'Q'; break;
                    case 'T':
                        map(float.Parse(tokens[i++]), float.Parse(tokens[i++]));
                        lastCmd = 'Q'; break;
                    case 'A': // Arc - skip for simplicity
                        i += 7; lastCmd = 'A'; break;
                    case 'Z':
                        path.CloseFigure(); cx = startX; cy = startY; lastCmd = 'M'; break;
                }
            }
            return path;
        }

        private List<string> TokenizeSvgPath(string d)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();
            bool inNumber = false;
            for (int i = 0; i < d.Length; i++)
            {
                char c = d[i];
                if (c == 'M' || c == 'm' || c == 'L' || c == 'l' || c == 'H' || c == 'h' || c == 'V' || c == 'v' ||
                    c == 'C' || c == 'c' || c == 'S' || c == 's' || c == 'Q' || c == 'q' || c == 'T' || c == 't' ||
                    c == 'A' || c == 'a' || c == 'Z' || c == 'z')
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                    tokens.Add(c.ToString());
                    inNumber = false;
                }
                else if (c == '-' || c == '+' || c == '.' || char.IsDigit(c) || c == 'e' || c == 'E')
                {
                    sb.Append(c); inNumber = true;
                }
                else if (c == ',' || c == ' ' || c == '\t' || c == '\n' || c == '\r')
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); inNumber = false; }
                }
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }

        private Image CreateBrightLogo(int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);
                float cx = w / 2f, cy = h / 2f, s = Math.Min(w, h) * 0.36f;

                // Glow
                for (int i = 6; i >= 1; i--)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(18, 0, 255, 65)))
                        g.FillEllipse(brush, cx - s * 2.2f - i, cy - s * 2.2f - i, s * 4.4f + i * 2, s * 4.4f + i * 2);
                }

                using (var brush = new SolidBrush(RazerGreen))
                {
                    // three Razer-style heads
                    PointF[] h1 = { new PointF(cx, cy - s * 1.25f), new PointF(cx - s * 0.55f, cy - s * 0.35f), new PointF(cx + s * 0.55f, cy - s * 0.35f) };
                    PointF[] h2 = { new PointF(cx - s * 0.85f, cy + s * 0.55f), new PointF(cx - s * 1.35f, cy + s * 1.15f), new PointF(cx - s * 0.35f, cy + s * 1.15f) };
                    PointF[] h3 = { new PointF(cx + s * 0.85f, cy + s * 0.55f), new PointF(cx + s * 0.35f, cy + s * 1.15f), new PointF(cx + s * 1.35f, cy + s * 1.15f) };
                    g.FillPolygon(brush, h1); g.FillPolygon(brush, h2); g.FillPolygon(brush, h3);
                    using (var pen = new Pen(RazerGreen, Math.Max(2f, w / 40f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        g.DrawLine(pen, cx, cy - s * 0.3f, cx, cy + s * 0.15f);
                        g.DrawLine(pen, cx, cy + s * 0.15f, cx - s * 0.65f, cy + s * 0.75f);
                        g.DrawLine(pen, cx, cy + s * 0.15f, cx + s * 0.65f, cy + s * 0.75f);
                    }
                }
            }
            return bmp;
        }

        private bool HasVisiblePixels(Image img)
        {
            try
            {
                using (var bmp = new Bitmap(img))
                {
                    for (int x = 0; x < bmp.Width; x += 4)
                        for (int y = 0; y < bmp.Height; y += 4)
                        {
                            var c = bmp.GetPixel(x, y);
                            if (c.A > 40 && (c.R + c.G + c.B) > 40) return true;
                        }
                }
            }
            catch { }
            return false;
        }

        private Image CreateFallbackLogo(int w, int h)
        {
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                float cx = w / 2f, cy = h / 2f, s = Math.Min(w, h) * 0.35f;
                using (var brush = new SolidBrush(RazerGreen))
                {
                    PointF[] h1 = { new PointF(cx, cy - s * 1.2f), new PointF(cx - s * 0.5f, cy - s * 0.4f), new PointF(cx + s * 0.5f, cy - s * 0.4f) };
                    PointF[] h2 = { new PointF(cx - s * 0.8f, cy + s * 0.6f), new PointF(cx - s * 1.3f, cy + s * 1.1f), new PointF(cx - s * 0.3f, cy + s * 1.1f) };
                    PointF[] h3 = { new PointF(cx + s * 0.8f, cy + s * 0.6f), new PointF(cx + s * 0.3f, cy + s * 1.1f), new PointF(cx + s * 1.3f, cy + s * 1.1f) };
                    g.FillPolygon(brush, h1); g.FillPolygon(brush, h2); g.FillPolygon(brush, h3);
                    using (var pen = new Pen(RazerGreen, 2.5f))
                    {
                        g.DrawLine(pen, cx, cy - s * 0.4f, cx, cy + s * 0.2f);
                        g.DrawLine(pen, cx, cy + s * 0.2f, cx - s * 0.6f, cy + s * 0.8f);
                        g.DrawLine(pen, cx, cy + s * 0.2f, cx + s * 0.6f, cy + s * 0.8f);
                    }
                }
            }
            return bmp;
        }

        // =====================================================================
        //  UI — Production-Ready 2026 Razer Style
        // =====================================================================
        private void BuildUi()
        {
            Text = "Razer Touchpad Helper";
            ClientSize = new Size(1080, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDark;
            ForeColor = TextPrimary;
            MinimumSize = new Size(980, 640);
            ShowInTaskbar = !_startHidden;
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;
            ResizeRedraw = true;
            Icon = CreateWindowIcon();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = BgDark,
                Padding = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            Controls.Add(root);

            // ===== HEADER =====
            var headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Padding = new Padding(16, 12, 16, 12) };
            root.Controls.Add(headerPanel, 0, 0);

            var logoBox = new PictureBox
            {
                Size = new Size(230, 60),
                Image = _razerWordmarkImage ?? _razerLogoImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Left,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 14, 0)
            };
            headerPanel.Controls.Add(logoBox);

            var titleStack = new Panel { Width = 560, Dock = DockStyle.Left, BackColor = Color.Transparent, Padding = new Padding(0) };

            var titleLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            var title = new Label
            {
                Text = "Touchpad Helper",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Margin = new Padding(0)
            };

            var subtitle = new Label
            {
                Text = "Razer Blade 16  -  Tap, scroll & multi-finger gestures",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = TextSecondary,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0)
            };

            titleLayout.Controls.Add(title, 0, 0);
            titleLayout.Controls.Add(subtitle, 0, 1);
            titleStack.Controls.Add(titleLayout);
            headerPanel.Controls.Add(titleStack);

            _statusPill = new Panel { Width = 230, Dock = DockStyle.Right, BackColor = Color.Transparent, Margin = new Padding(12, 6, 0, 6) };
            _statusPill.Paint += (s, e) => DrawStatusPill(e.Graphics, _statusPill.ClientRectangle);
            headerPanel.Controls.Add(_statusPill);

            // ===== CONTENT =====
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BgDark,
                Padding = new Padding(16, 10, 16, 10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Controls.Add(content, 0, 1);

            // LEFT: visualizer + stats
            var leftPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = new Padding(0, 0, 8, 0)
            };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            content.Controls.Add(leftPanel, 0, 0);

            var leftCard = CreateCard("Live Touch Visualization", (int?)null);
            leftCard.Dock = DockStyle.Fill;
            leftPanel.Controls.Add(leftCard, 0, 0);

            _touchVisualizer = new TouchVisualizer { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            leftCard.Controls.Add(_touchVisualizer);

            var statsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(14, 8, 14, 8) };
            leftPanel.Controls.Add(statsPanel, 0, 1);

            _lblContacts = new Label { Text = "Contacts: 0", Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = RazerGreen, AutoSize = true, Margin = new Padding(0, 0, 0, 6) };
            statsPanel.Controls.Add(_lblContacts);

            _lblGesture = new Label { Text = "Last gesture: IDLE", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = TextSecondary, AutoSize = true };
            statsPanel.Controls.Add(_lblGesture);

            // RIGHT: settings + log
            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = new Padding(8, 0, 0, 0)
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 410));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            content.Controls.Add(rightPanel, 1, 0);

            var settingsCard = CreateCard("Settings", (int?)null);
            settingsCard.Dock = DockStyle.Fill;
            rightPanel.Controls.Add(settingsCard, 0, 0);

            var settingsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(16, 38, 16, 14)
            };
            settingsCard.Controls.Add(settingsFlow);

            AddSlider(settingsFlow, "Scroll speed", 1, 30, _settings.ScrollSpeed, v => { _settings.ScrollSpeed = v; AutoSave(); }, "Sensitivity multiplier for two-finger scrolling");
            settingsFlow.Controls.Add(CreateDivider());

            AddToggle(settingsFlow, "Enabled", _settings.Enabled, v => { _settings.Enabled = v; AutoSave(); }, true, "Master on/off for all touchpad processing");
            AddToggle(settingsFlow, "Two-finger scroll", _settings.TwoFingerScroll, v => { _settings.TwoFingerScroll = v; AutoSave(); }, false, "Scroll with two fingers on the trackpad");
            AddToggle(settingsFlow, "Multi-finger gestures", _settings.MultiFingerGestures, v => { _settings.MultiFingerGestures = v; AutoSave(); }, false, "3/4-finger swipes and taps for system actions");
            AddToggle(settingsFlow, "Two-finger tap = right click", _settings.TwoFingerRightClick, v => { _settings.TwoFingerRightClick = v; AutoSave(); }, false, "Tap with two fingers simultaneously for right-click");
            AddToggle(settingsFlow, "Release stuck button before click", _settings.ForceReleaseBeforeClick, v => { _settings.ForceReleaseBeforeClick = v; AutoSave(); }, false, "Ensure no mouse button is stuck down before injecting a click");
            AddToggle(settingsFlow, "Mirror X (swap left/right zones)", _settings.MirrorX, v => { _settings.MirrorX = v; AutoSave(); }, false, "Swap the left/right click zones horizontally");
            AddToggle(settingsFlow, "Invert scroll direction", _settings.InvertScroll, v => { _settings.InvertScroll = v; AutoSave(); }, false, "Natural scrolling (content moves with fingers)");
            AddToggle(settingsFlow, "Long-press drag", _settings.LongPressDrag, v => { _settings.LongPressDrag = v; if (!v) StopDragArming(); AutoSave(); }, false, "Hold one finger to start drag (tap-hold-move)");
            AddToggle(settingsFlow, "Freeze cursor during 2F scroll", _settings.FreezeCursorOnMultiFinger, v => { _settings.FreezeCursorOnMultiFinger = v; AutoSave(); }, false, "Lock cursor position while scrolling with two fingers");

            settingsFlow.Controls.Add(CreateDivider());

            AddNumeric(settingsFlow, "Swipe threshold", 100, 1200, _settings.SwipeThreshold, v => { _settings.SwipeThreshold = v; AutoSave(); }, "Minimum swipe distance (device units) to trigger a 3/4-finger gesture");
            AddNumeric(settingsFlow, "Max tap movement", 20, 500, _settings.MaxMove, v => { _settings.MaxMove = v; AutoSave(); }, "Maximum finger movement (device units) that still counts as a tap");
            AddNumeric(settingsFlow, "Drag hold (ms)", 100, 2000, _settings.DragHoldMs, v => { _settings.DragHoldMs = v; AutoSave(); }, "Hold time before long-press drag activates");
            AddNumeric(settingsFlow, "Right-click zone (%)", 5, 60, _settings.RightZonePercent, v => { _settings.RightZonePercent = v; AutoSave(); }, "Width of right-click zone from right edge");
            AddNumeric(settingsFlow, "Max tap time (ms)", 80, 1500, _settings.MaxTapMs, v => { _settings.MaxTapMs = v; AutoSave(); }, "Maximum duration for a tap to register as a click");

            var settingsNote = new Label { Text = "Settings are applied instantly and saved automatically.", Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), ForeColor = TextMuted, AutoSize = true, Margin = new Padding(2, 10, 0, 0) };
            settingsFlow.Controls.Add(settingsNote);

            // LOG
            var logCard = CreateCard("Activity Log", (int?)null);
            logCard.Dock = DockStyle.Fill;
            rightPanel.Controls.Add(logCard, 0, 1);

            _logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10f),
                BackColor = BgInput,
                ForeColor = RazerGreen,
                BorderStyle = BorderStyle.None,
                HideSelection = false,
                Margin = new Padding(0, 0, 0, 0)
            };
            logCard.Controls.Add(_logBox);

            var logToolbar = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 44, BackColor = Color.Transparent, Padding = new Padding(12, 6, 12, 6) };
            logCard.Controls.Add(logToolbar);
            var btnOpenLogs = CreateModernButton("Open Log Folder", 150, () =>
            {
                try { System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(_logPath)); } catch { }
            }, false);
            var btnClear = CreateModernButton("Clear", 90, () => _logBox.Clear(), false);
            logToolbar.Controls.Add(btnClear);
            logToolbar.Controls.Add(btnOpenLogs);

            // ===== BOTTOM BAR =====
            var bottomBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BgCard,
                Padding = new Padding(16, 10, 16, 10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            bottomBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Controls.Add(bottomBar, 0, 2);

            var leftBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent, WrapContents = false, AutoSize = true };
            bottomBar.Controls.Add(leftBtns, 0, 0);

            // Test left/right click buttons removed; hidden self-test controls are not needed.

            var rightBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            bottomBar.Controls.Add(rightBtns, 1, 0);
            var btnSave = CreateModernButton("Save Settings", 150, () => { SaveSettings(); WriteLog("Settings saved"); }, true);
            btnSave.Dock = DockStyle.Right;
            rightBtns.Controls.Add(btnSave);

            Resize += (s, e) => { Invalidate(true); _touchVisualizer?.Invalidate(); _statusPill?.Invalidate(); };
            FormClosing += OnFormClosing;
            _uiTimer.Tick += (s, e) => RefreshUi();
            _uiTimer.Start();
            _dragTimer.Tick += (s, e) => OnDragHoldTimer();
            _trayAnimTimer.Tick += (s, e) => UpdateTrayAnimation();

            if (_statusPill != null)
                _statusPill.Invalidate();
        }

        private Icon CreateWindowIcon()
        {
            var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                if (_razerLogoImage != null)
                    g.DrawImage(_razerLogoImage, 2, 2, 28, 28);
                else
                {
                    using (var brush = new SolidBrush(RazerGreen))
                    {
                        PointF[] h1 = { new PointF(16, 4), new PointF(12, 10), new PointF(20, 10) };
                        PointF[] h2 = { new PointF(8, 18), new PointF(4, 24), new PointF(12, 24) };
                        PointF[] h3 = { new PointF(24, 18), new PointF(20, 24), new PointF(28, 24) };
                        g.FillPolygon(brush, h1); g.FillPolygon(brush, h2); g.FillPolygon(brush, h3);
                    }
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        // =====================================================================
        //  UI HELPERS
        // =====================================================================
        private Panel CreateCard(string title, int? fixedHeight)
        {
            var card = new Panel
            {
                BackColor = BgCard,
                Padding = new Padding(16, 44, 16, 16),
                Margin = new Padding(0, 0, 0, 0),
                Dock = DockStyle.Fill
            };
            if (fixedHeight.HasValue) card.Height = fixedHeight.Value;

            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderBright, 1.5f))
                using (var path = GetRoundedRect(card.ClientRectangle, 12))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };

            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = RazerGreen,
                    AutoSize = true,
                    Location = new Point(18, 10)
                };
                card.Controls.Add(titleLabel);
            }
            return card;
        }

        private Label CreateStatLabel(string initialValue, string caption, Color valueColor)
        {
            var container = new Label { AutoSize = false, Size = new Size(420, 54), BackColor = Color.Transparent, Text = initialValue, Tag = caption };
            container.Paint += (s, e) =>
            {
                e.Graphics.Clear(BgCard);
                using (var brush = new SolidBrush(valueColor))
                using (var fontVal = new Font("Segoe UI", 24f, FontStyle.Bold))
                using (var fontLbl = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var brush2 = new SolidBrush(TextMuted))
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    e.Graphics.DrawString(container.Text, fontVal, brush, 0, -2);
                    e.Graphics.DrawString(caption, fontLbl, brush2, 0, 32);
                }
            };
            return container;
        }

        private void AddToggle(FlowLayoutPanel parent, string text, bool checkedState, Action<bool> onChange, bool primary, string tooltip)
        {
            var item = new Panel { Size = new Size(420, 62), Margin = new Padding(0, 6, 0, 6), BackColor = Color.Transparent, Cursor = Cursors.Hand };
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(0, 6),
                MaximumSize = new Size(300, 0)
            };
            var desc = new Label
            {
                Text = tooltip,
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(0, 34),
                MaximumSize = new Size(300, 0)
            };
            var toggle = new ModernToggle { Checked = checkedState, Location = new Point(item.Width - 62, 16), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            toggle.CheckedChanged += (s, e) => onChange(toggle.Checked);

            var tip = new ToolTip { InitialDelay = 300, AutoPopDelay = 5000, ReshowDelay = 100 };
            tip.SetToolTip(item, tooltip);
            tip.SetToolTip(lbl, tooltip);
            tip.SetToolTip(toggle, tooltip);

            item.Controls.AddRange(new Control[] { lbl, desc, toggle });
            parent.Controls.Add(item);
        }

        private void AddNumeric(FlowLayoutPanel parent, string label, int min, int max, int value, Action<int> onChange, string tooltip)
        {
            var item = new Panel { Size = new Size(420, 64), Margin = new Padding(0, 6, 0, 6), BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(0, 4),
                MaximumSize = new Size(290, 0)
            };
            var desc = new Label
            {
                Text = tooltip,
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(0, 30),
                MaximumSize = new Size(290, 0)
            };
            var num = new ModernNumericUpDown
            {
                Minimum = min, Maximum = max, Value = value,
                Width = 100, Location = new Point(item.Width - 112, 22),
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

        private void AddSlider(FlowLayoutPanel parent, string label, int min, int max, int value, Action<int> onChange, string tooltip)
        {
            var item = new Panel { Size = new Size(560, 96), Margin = new Padding(0, 8, 0, 8), BackColor = BgCard };
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(16, 12),
                MaximumSize = new Size(420, 0)
            };
            var desc = new Label
            {
                Text = tooltip,
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
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
                BackColor = BgCard
            };
            var val = new Label
            {
                Text = track.Value.ToString(),
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = RazerGreen,
                AutoSize = true,
                Location = new Point(392, 66),
                Size = new Size(96, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = BgCard
            };
            track.ValueChanged += (s, e) =>
            {
                try { onChange(track.Value); } catch { }
                val.Text = track.Value.ToString();
                AutoSave();
            };
            var tip = new ToolTip { InitialDelay = 300 };
            tip.SetToolTip(item, tooltip);
            tip.SetToolTip(lbl, tooltip);
            tip.SetToolTip(track, tooltip);
            tip.SetToolTip(val, tooltip);
            item.Controls.AddRange(new Control[] { lbl, desc, track, val });
            parent.Controls.Add(item);
        }

        private Panel CreateDivider()
        {
            return new Panel { Height = 1, Dock = DockStyle.Top, Margin = new Padding(0, 12, 0, 12), BackColor = BorderColor };
        }

        private Button CreateModernButton(string text, int width, Action onClick, bool primary)
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
            btn.FlatAppearance.MouseOverBackColor = primary ? RazerGreenDim : Color.FromArgb(0, 60, 15);
            btn.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(0, 140, 35) : Color.FromArgb(0, 80, 20);

            if (primary)
            {
                btn.BackColor = RazerGreen;
                btn.ForeColor = Color.FromArgb(0x0A, 0x0E, 0x0C);
            }
            else
            {
                btn.BackColor = BgCard;
                btn.ForeColor = RazerGreen;
            }

            btn.Paint += (s, e) =>
            {
                using (var path = GetRoundedRect(btn.ClientRectangle, 8))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    if (!primary)
                    {
                        using (var pen = new Pen(BorderColor, 1))
                            e.Graphics.DrawPath(pen, path);
                    }
                }
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
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

        private void DrawStatusPill(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool active = _settings.Enabled && _curContacts > 0;
            Color bg = active ? Color.FromArgb(0, 60, 15) : Color.FromArgb(30, 35, 40);
            Color dotColor = active ? RazerGreen : Color.FromArgb(80, 90, 80);
            string text = active ? "ACTIVE" : "IDLE";

            using (var path = GetRoundedRect(rect, rect.Height / 2))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            // Status dot with pulse
            int dotSize = 10;
            int dotX = rect.X + 16;
            int dotY = rect.Y + (rect.Height - dotSize) / 2;
            using (var brush = new SolidBrush(dotColor))
                g.FillEllipse(brush, dotX, dotY, dotSize, dotSize);

            if (active && _trayAnimActive)
            {
                using (var pen = new Pen(Color.FromArgb(80, RazerGreen), 2))
                    g.DrawEllipse(pen, dotX - 4, dotY - 4, dotSize + 8, dotSize + 8);
            }

            using (var font = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var brush = new SolidBrush(active ? RazerGreen : TextMuted))
            {
                var txtSize = g.MeasureString(text, font);
                g.DrawString(text, font, brush, rect.X + 32, rect.Y + (rect.Height - txtSize.Height) / 2);
            }
        }

        // =====================================================================
        //  TOUCH VISUALIZER
        // =====================================================================
        private class TouchVisualizer : Panel
        {
            private readonly List<TouchPoint> _points = new List<TouchPoint>();
            private readonly object _lock = new object();
            private readonly MainForm _parent;

            public TouchVisualizer(MainForm parent = null) { _parent = parent; DoubleBuffered = true; }

            public void UpdateContacts(List<(int Id, int X, int Y)> contacts)
            {
                lock (_lock)
                {
                    _points.Clear();
                    foreach (var c in contacts)
                    {
                        float px = (float)c.X / TouchMaxX;
                        float py = (float)c.Y / TouchMaxY;
                        _points.Add(new TouchPoint { Id = c.Id, X = Math.Clamp(px, 0, 1), Y = Math.Clamp(py, 0, 1), Life = 1f });
                    }
                    Invalidate();
                }
            }

            public void Clear() { lock (_lock) { _points.Clear(); Invalidate(); } }

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
                using (var brush = new SolidBrush(Color.FromArgb(0x08, 0x0A, 0x0C)))
                using (var path = GetRoundedRectF(padRect, cornerRadius))
                    g.FillPath(brush, path);

                // Border
                using (var pen = new Pen(BorderColor, 1.5f))
                using (var path = GetRoundedRectF(padRect, cornerRadius))
                    g.DrawPath(pen, path);

                // Right zone indicator
                float rightZoneX = padRect.X + padRect.Width * 0.75f;
                using (var brush = new SolidBrush(Color.FromArgb(15, RazerGreen)))
                    g.FillRectangle(brush, rightZoneX, padRect.Y, padRect.Right - rightZoneX, padRect.Height);
                using (var pen = new Pen(Color.FromArgb(40, RazerGreen), 1))
                    g.DrawLine(pen, rightZoneX, padRect.Y, rightZoneX, padRect.Bottom);

                // Zone labels
                using (var font = new Font("Segoe UI", 7f, FontStyle.Bold))
                using (var brush = new SolidBrush(TextMuted))
                {
                    g.DrawString("LEFT CLICK", font, brush, padRect.X + 12, padRect.Y + 8);
                    g.DrawString("RIGHT CLICK", font, brush, rightZoneX + 12, padRect.Y + 8);
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
                        using (var pen = new Pen(Color.FromArgb(alpha / 3, RazerGreen), 2))
                            g.DrawEllipse(pen, x - size / 2 - 4, y - size / 2 - 4, size + 8, size + 8);

                        // Contact circle
                        using (var brush = new SolidBrush(Color.FromArgb(alpha, RazerGreen)))
                            g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);

                        // Inner white dot
                        using (var brush = new SolidBrush(Color.FromArgb(alpha, Color.White)))
                            g.FillEllipse(brush, x - 5, y - 5, 10, 10);

                        // Contact ID
                        using (var font = new Font("Segoe UI", 7f, FontStyle.Bold))
                        using (var brush = new SolidBrush(Color.FromArgb(alpha, TextPrimary)))
                            g.DrawString($"#{p.Id}", font, brush, x + size / 2 + 4, y - 8);
                    }
                }
            }

            private struct TouchPoint { public int Id; public float X, Y, Life; }
        }

        // =====================================================================
        //  MODERN TOGGLE
        // =====================================================================
        private class ModernToggle : Control
        {
            private bool _checked;
            private float _animPos = 0f;
            private readonly System.Windows.Forms.Timer _animTimer = new System.Windows.Forms.Timer { Interval = 10 };

            public bool Checked { get => _checked; set { _checked = value; _animTimer.Start(); Invalidate(); } }
            public event EventHandler CheckedChanged;

            public ModernToggle() { Size = new Size(52, 28); DoubleBuffered = true; _animTimer.Tick += (s, e) => { float t = _checked ? 1f : 0f; _animPos += (t - _animPos) * 0.35f; if (Math.Abs(_animPos - t) < 0.02f) { _animPos = t; _animTimer.Stop(); } Invalidate(); }; Click += (s, e) => { Checked = !_checked; CheckedChanged?.Invoke(this, EventArgs.Empty); }; }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = ClientRectangle; int h = rect.Height; int trackW = rect.Width; int thumb = h - 4;

                Color trackC = _checked ? RazerGreen : Color.FromArgb(60, 70, 60);
                using (var brush = new SolidBrush(trackC))
                using (var path = GetRoundedRectF(new RectangleF(0, 0, trackW, h), h / 2f))
                    g.FillPath(brush, path);

                float thumbX = 2 + _animPos * (trackW - thumb - 4);
                using (var brush = new SolidBrush(Color.White))
                using (var path = GetRoundedRectF(new RectangleF(thumbX, 2, thumb, thumb), thumb / 2f))
                    g.FillPath(brush, path);

                if (_checked)
                {
                    using (var pen = new Pen(Color.FromArgb(100, RazerGreen), 2))
                        g.DrawEllipse(pen, thumbX - 3, -1, thumb + 6, thumb + 6);
                }
            }
        }

        // Shared helper for rounded rectangles (float version)
        private static GraphicsPath GetRoundedRectF(RectangleF rect, float radius)
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

        // =====================================================================
        //  MODERN NUMERIC UPDOWN
        // =====================================================================
        private class ModernNumericUpDown : NumericUpDown
        {
            public ModernNumericUpDown()
            {
                BorderStyle = BorderStyle.None;
                Font = new Font("Segoe UI", 11f);
                BackColor = BgInput;
                ForeColor = TextPrimary;
                TextAlign = HorizontalAlignment.Center;
                Controls[0].Visible = false; // Hide default up/down buttons
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (var pen = new Pen(BorderColor, 1))
                using (var path = GetRoundedRect(ClientRectangle, 6))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
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

        // =====================================================================
        //  TRAY ICON WITH RAZER LOGO & ANIMATION
        // =====================================================================
        private void SetupTray()
        {
            _trayIconBase = CreateTrayIcon(false);
            _trayIconActive = CreateTrayIcon(true);

            _trayIcon = new NotifyIcon
            {
                Icon = _trayIconBase,
                Text = "Razer Touchpad Helper",
                Visible = true
            };

            var menu = new ContextMenuStrip
            {
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Renderer = new ModernToolStripRenderer(),
                ShowImageMargin = true,
                ImageScalingSize = new Size(16, 16)
            };

            var open = new ToolStripMenuItem("Open Settings") { Font = new Font("Segoe UI", 9f), Image = CreateRazerMenuIcon() };
            open.Click += (s, e) => ShowWindow();

            _miEnabled = new ToolStripMenuItem("Enabled") { CheckOnClick = true, Checked = _settings.Enabled, Font = new Font("Segoe UI", 9f) };
            _miEnabled.CheckedChanged += (s, e) => { _settings.Enabled = _miEnabled.Checked; AutoSave(); };

            _miScroll = new ToolStripMenuItem("Two-finger Scroll") { CheckOnClick = true, Checked = _settings.TwoFingerScroll, Font = new Font("Segoe UI", 9f) };
            _miScroll.CheckedChanged += (s, e) => { _settings.TwoFingerScroll = _miScroll.Checked; AutoSave(); };

            _miGestures = new ToolStripMenuItem("Multi-finger Gestures") { CheckOnClick = true, Checked = _settings.MultiFingerGestures, Font = new Font("Segoe UI", 9f) };
            _miGestures.CheckedChanged += (s, e) => { _settings.MultiFingerGestures = _miGestures.Checked; AutoSave(); };

            var exit = new ToolStripMenuItem("Exit") { Font = new Font("Segoe UI", 9f), Image = CreateRazerMenuIcon() };
            exit.Click += (s, e) => { _exiting = true; Close(); };

            menu.Items.Add(open);
            menu.Items.Add(new ToolStripSeparator { Margin = new Padding(6, 0, 6, 0) });
            menu.Items.Add(_miEnabled);
            menu.Items.Add(_miScroll);
            menu.Items.Add(_miGestures);
            menu.Items.Add(new ToolStripSeparator { Margin = new Padding(6, 0, 6, 0) });
            menu.Items.Add(exit);

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private Icon CreateTrayIcon(bool active)
        {
            var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Background circle (always dark so the green logo stays visible)
                using (var brush = new SolidBrush(Color.FromArgb(20, 25, 22)))
                    g.FillEllipse(brush, 2, 2, 28, 28);
                using (var pen = new Pen(active ? RazerGreen : BorderBright, 1.5f))
                    g.DrawEllipse(pen, 2, 2, 28, 28);

                // Razer logo
                if (_razerLogoImage != null)
                {
                    g.DrawImage(_razerLogoImage, 4, 4, 24, 24);
                }

                // Animation ring
                if (active)
                {
                    float progress = _trayAnimFrame / 20f;
                    using (var pen = new Pen(Color.FromArgb((int)(180 * (1 - progress)), RazerGreen), 2))
                    {
                        float inset = 4 + progress * 8;
                        g.DrawEllipse(pen, inset, inset, 32 - inset * 2, 32 - inset * 2);
                    }
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        private Image CreateRazerMenuIcon()
        {
            var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                if (_razerLogoImage != null)
                    g.DrawImage(_razerLogoImage, 0, 0, 16, 16);
                else
                {
                    using (var brush = new SolidBrush(RazerGreen))
                    {
                        PointF[] pts = { new PointF(8, 2), new PointF(6, 6), new PointF(10, 6) };
                        g.FillPolygon(brush, pts);
                    }
                }
            }
            return bmp;
        }

        private class ModernToolStripRenderer : ToolStripProfessionalRenderer
        {
            public ModernToolStripRenderer() : base(new ModernColorTable()) { }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Selected ? RazerGreen : TextPrimary;
                base.OnRenderItemText(e);
            }
        }

        private class ModernColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder => BorderColor;
            public override Color ToolStripDropDownBackground => BgCard;
            public override Color MenuItemSelected => BgCardHover;
            public override Color MenuItemSelectedGradientBegin => BgCardHover;
            public override Color MenuItemSelectedGradientEnd => BgCardHover;
            public override Color SeparatorDark => BorderColor;
            public override Color SeparatorLight => BorderColor;
        }

        private void ShowWindow()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        private void StartTrayAnimation()
        {
            if (!_trayAnimActive) { _trayAnimActive = true; _trayAnimFrame = 0; _trayAnimTimer.Start(); }
        }

        private void StopTrayAnimation()
        {
            _trayAnimActive = false; _trayAnimTimer.Stop(); _trayIcon.Icon = _trayIconBase;
        }

        private void UpdateTrayAnimation()
        {
            if (!_trayAnimActive) return;
            _trayAnimFrame++;
            if (_trayAnimFrame > 20)
            {
                _trayAnimFrame = 0;
                if (_curContacts == 0) { StopTrayAnimation(); return; }
            }
            _trayIcon.Icon = CreateTrayIcon(true);
        }

        // =====================================================================
        //  RAW INPUT REGISTRATION
        // =====================================================================
        private void RegisterRawInput()
        {
            var devices = new[]
            {
                new RAWINPUTDEVICE { usUsagePage = 0x0D, usUsage = 0x05, dwFlags = RIDEV_INPUTSINK, hwndTarget = Handle }
            };
            if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
                WriteLog($"Raw Input registration FAILED: {Marshal.GetLastWin32Error()}");
            else
                WriteLog("Raw Input registered for touchpad digitizer (0x0D:0x05) ONLY");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                uint size = 0;
                GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>());
                var buffer = new byte[size];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    GetRawInputData(m.LParam, RID_INPUT, handle.AddrOfPinnedObject(), ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>());
                    var rawInput = Marshal.PtrToStructure<RAWINPUT>(handle.AddrOfPinnedObject());
                    if (rawInput.header.dwType == RIM_TYPEHID)
                    {
                        int dataOffset = Marshal.SizeOf<RAWINPUTHEADER>() + Marshal.SizeOf<RAWHID>();
                        var hidData = new byte[rawInput.hid.dwSizeHid];
                        Array.Copy(buffer, dataOffset, hidData, 0, hidData.Length);
                        DecodeDigitizer(hidData);
                    }
                }
                finally { handle.Free(); }
            }
            base.WndProc(ref m);
        }

        // =====================================================================
        //  GESTURE PROCESSING
        // =====================================================================
        private void DecodeDigitizer(byte[] b)
        {
            _reports++; _lastReportAt = DateTime.UtcNow;
            if (b.Length < 4) return;

            bool btn1 = (b[1] & 1) != 0;
            if (_btn1.HasValue && _btn1.Value != btn1)
            { _btnFlips++; WriteLog($"Physical button bit CHANGED -> {btn1}  (flips={_btnFlips})"); }
            _btn1 = btn1;

            var contacts = new List<(int Id, int X, int Y)>();
            for (int k = 0; k < 5; k++)
            {
                int o = 4 + k * 5;
                if (o + 4 >= b.Length) break;
                int tip = (b[o] >> 1) & 1;
                int cid = (b[o] >> 4) & 0x0F;
                int x = b[o + 1] | (b[o + 2] << 8);
                int y = b[o + 3] | (b[o + 4] << 8);
                if (tip == 1) contacts.Add((cid, x, y));
            }

            int n = contacts.Count;
            _curContacts = n;
            _emptyReports = 0;

            // Update visualizer on UI thread
            if (_touchVisualizer != null && _touchVisualizer.IsHandleCreated)
                _touchVisualizer.BeginInvoke(new Action(() => _touchVisualizer.UpdateContacts(contacts)));

            if (n > 0)
            {
                double cx = 0, cy = 0;
                foreach (var c in contacts) { cx += c.X; cy += c.Y; }
                cx /= n; cy /= n;

                if (!_gestureActive)
                {
                    _gestureActive = true; _gestureStart = DateTime.UtcNow;
                    _gestureMaxContacts = 0; _gestureMaxDisp = 0;
                    _gestureStartContactCount = n;
                    _gestureTwoContactReports = n >= 2 ? 1 : 0;
                    _gestureSawSecondContact = n >= 2;
                    _gestureFirstX = contacts[0].X; _gestureFirstY = contacts[0].Y;
                    _gestureFirstCx = cx; _gestureFirstCy = cy;
                    _gestureLastCx = cx; _gestureLastCy = cy;
                    _gestureScrolled = false; _twoFingerScrollActive = false;
                    _scrollLastSign = 0; _scrollAxis = 0;
                    _gestureStarts.Clear();
                    _touchStartValid = GetCursorPos(out POINT sp);
                    if (_touchStartValid) _touchStartPos = new Point(sp.X, sp.Y);

                    // Start long-press drag arming on a one-finger hold.
                    _dragArming = false; _dragTimer.Stop();
                    bool doubleTapHold = _lastTapValid
                        && (DateTime.UtcNow - _lastTapAt).TotalMilliseconds <= SystemInformation.DoubleClickTime
                        && Math.Abs(contacts[0].X - _lastTapX) <= TapDragMaxDistance
                        && Math.Abs(contacts[0].Y - _lastTapY) <= TapDragMaxDistance;
                    if (n == 1 && _settings.Enabled && _settings.LongPressDrag && doubleTapHold)
                    {
                        _dragArming = true;
                        _dragTimer.Interval = Math.Max(50, _settings.DragHoldMs);
                        _dragTimer.Start();
                    }

                    _doubleTapPrimed = _lastTapValid
                        && (DateTime.UtcNow - _lastTapAt).TotalMilliseconds <= SystemInformation.DoubleClickTime
                        && Math.Abs(contacts[0].X - _lastTapX) <= TapDragMaxDistance
                        && Math.Abs(contacts[0].Y - _lastTapY) <= TapDragMaxDistance;

                    // Any tap that starts shortly after a previous tap is part of a rapid
                    // tap burst, so don't convert it to a right click even if it lands
                    // in the right-click zone. Windows handles multiple click timing.
                    _quickTapPrimed = _lastTapValid
                        && (DateTime.UtcNow - _lastTapAt).TotalMilliseconds <= 750;

                    _lastTapValid = false;
                }

                if (n > _gestureMaxContacts) _gestureMaxContacts = n;
                if (n >= 2)
                {
                    _gestureTwoContactReports++;
                    if (!_gestureSawSecondContact) { _gestureSawSecondContact = true; }
                }
                if (_dragArming && n != 1) { _dragArming = false; _dragTimer.Stop(); }

                foreach (var c in contacts)
                {
                    if (!_gestureStarts.TryGetValue(c.Id, out var s)) _gestureStarts[c.Id] = (c.X, c.Y);
                    else { double dx = c.X - s.X, dy = c.Y - s.Y, d = Math.Sqrt(dx * dx + dy * dy); if (d > _gestureMaxDisp) _gestureMaxDisp = d; }
                }

                // Live two-finger scrolling
                if (n == 2 && _settings.Enabled && _settings.TwoFingerScroll)
                {
                    if (!_twoFingerScrollActive)
                    {
                        _dragArming = false;
                        _dragTimer.Stop();
                        _twoFingerScrollActive = true;
                        _scrollPrevCx = cx; _scrollPrevCy = cy;
                        _scrollAccumV = 0; _scrollAccumH = 0;
                        _scrollSmoothV = 0; _scrollSmoothH = 0;
                        _scrollVNotches = 0; _scrollHNotches = 0;
                        _scrollLastSign = 0; _scrollAxis = 0;
                        if (_settings.FreezeCursorOnMultiFinger && GetCursorPos(out POINT fp))
                        {
                            _scrollFreezePos = new Point(fp.X, fp.Y);
                            _scrollFreezeActive = true;
                            var rc = new RECT { left = fp.X, top = fp.Y, right = fp.X + 1, bottom = fp.Y + 1 };
                            ClipCursor(ref rc);
                        }
                        else
                        {
                            _scrollFreezeActive = false;
                        }
                    }
                    else
                    {
                        if (_scrollFreezeActive && GetCursorPos(out POINT cp))
                            if (cp.X != _scrollFreezePos.X || cp.Y != _scrollFreezePos.Y)
                                SetCursorPos(_scrollFreezePos.X, _scrollFreezePos.Y);

                        double dx = cx - _scrollPrevCx, dy = cy - _scrollPrevCy;
                        _scrollPrevCx = cx; _scrollPrevCy = cy;

                        if (Math.Abs(dx) > 3 || Math.Abs(dy) > 3) _gestureScrolled = true;

                        if (_gestureScrolled)
                        {
                            double units = Math.Max(1, _settings.ScrollSpeed) * WheelUnitsPerDeviceUnitBase;
                            double sign = _settings.InvertScroll ? 1.0 : -1.0;

                            if (_scrollLastSign == 0)
                                _scrollAxis = Math.Abs(dy) >= Math.Abs(dx) ? 0 : 1;

                            double axisDelta = _scrollAxis == 0 ? dy : dx;
                            bool horiz = _scrollAxis == 1;
                            if (Math.Sign(axisDelta) != 0) _scrollLastSign = Math.Sign(axisDelta);

                            double raw = axisDelta * sign * units;
                            raw = Math.Clamp(raw, -MaxWheelUnitsPerReport, MaxWheelUnitsPerReport);
                            int wheel = (int)Math.Truncate(raw);
                            if (wheel == 0 && Math.Abs(raw) > 0) wheel = raw > 0 ? 1 : -1;
                            if (wheel != 0) { InjectWheelDelta(wheel, horiz); if (horiz) _scrollHNotches += wheel; else _scrollVNotches += wheel; }
                        }
                    }
                }
                else { if (_scrollFreezeActive) ClipCursor(IntPtr.Zero); _twoFingerScrollActive = false; _scrollFreezeActive = false; }

                _gestureLastCx = cx; _gestureLastCy = cy;
            }
            else
            {
                _emptyReports++;
                if (_gestureActive) EndGesture();
                _twoFingerScrollActive = false;
            }
        }

        private void EndGesture()
        {
            if (!_gestureActive) return;
            _gestureActive = false;
            bool wasScroll = _gestureScrolled;
            bool wasFreeze = _scrollFreezeActive;
            _twoFingerScrollActive = false; _scrollFreezeActive = false;
            if (wasFreeze) { ClipCursor(IntPtr.Zero); SetCursorPos(_scrollFreezePos.X, _scrollFreezePos.Y); }

            _dragArming = false;
            _dragTimer.Stop();
            bool wasDrag = _dragActive;
            if (_dragActive) { _dragActive = false; InjectMouseButton(MOUSEEVENTF_LEFTUP); }
            if (wasDrag) { _lastTapValid = false; _lastTapText = "Drag"; }

            double _gestureDurationMs = (DateTime.UtcNow - _gestureStart).TotalMilliseconds;
            if (!wasDrag && !wasScroll && (_gestureStartContactCount == 1 || _gestureTwoContactReports < 2)
                && _gestureMaxDisp <= _settings.MaxMove && _gestureDurationMs <= _settings.MaxTapMs)
            {
                // Right click is only for a clearly isolated one-finger tap. Any tap that
                // is part of a rapid repeated-tap burst stays a left click.
                bool rapidBurst = _quickTapPrimed
                    || (_lastOneFingerTapInjectedAt != DateTime.MinValue
                        && (DateTime.UtcNow - _lastOneFingerTapInjectedAt).TotalMilliseconds <= 750);
                bool isSecondClickOfDoubleClick = rapidBurst;
                bool right = IsRightZone(_gestureFirstX) && !rapidBurst;
                InjectClick(right);
                _lastTapAt = DateTime.UtcNow; _lastTapX = _gestureFirstX; _lastTapY = _gestureFirstY; _lastTapValid = true;
                _lastOneFingerTapInjectedAt = DateTime.UtcNow;
                _doubleTapPrimed = false;
                _quickTapPrimed = false;
                _lastTapText = right ? "Right click" : "Left click";
                if (isSecondClickOfDoubleClick) _lastTapText = "Left click (double)";
                WriteLog($"1F tap: x={_gestureFirstX} rightZone={right} secondClick={isSecondClickOfDoubleClick} disp={_gestureMaxDisp:F0} startCnt={_gestureStartContactCount} twoReports={_gestureTwoContactReports}");
            }
            else if (!wasScroll && _settings.Enabled && _settings.TwoFingerRightClick
                && _gestureStartContactCount >= 2 && _gestureTwoContactReports >= 2 && _gestureDurationMs >= 30
                && _gestureMaxDisp <= Math.Max(_settings.MaxMove, 400)
                && _gestureDurationMs <= _settings.MaxTapMs)
            {
                InjectClick(true);
                _lastTapText = "Right click (2F)";
                WriteLog($"2F tap: dur={_gestureDurationMs:F0}ms startCnt={_gestureStartContactCount} twoReports={_gestureTwoContactReports} disp={_gestureMaxDisp:F0} -> RIGHT");
            }
            else if (!wasScroll && _gestureStartContactCount == 2 && _gestureTwoContactReports < 3)
            {
                // Ignore brief/unstable contact that only looked like a 2-finger tap.
                _lastTapText = "Ignored unstable tap";
                WriteLog($"IGNORED tap: startCnt=2 twoReports={_gestureTwoContactReports} dur={_gestureDurationMs:F0}ms disp={_gestureMaxDisp:F0}");
            }
            else if (!wasScroll && _settings.MultiFingerGestures && _settings.Enabled)
            {
                ProcessMultiFingerGesture();
            }
            else if (wasScroll) _lastTapText = $"Scroll: V={_scrollVNotches} H={_scrollHNotches}";


            _gestureStarts.Clear();
        }

        private void ProcessMultiFingerGesture()
        {
            if (_gestureMaxContacts == 3)
            {
                double dx = _gestureLastCx - _gestureFirstCx, dy = _gestureLastCy - _gestureFirstCy;
                double adx = Math.Abs(dx), ady = Math.Abs(dy);
                if (Math.Max(adx, ady) >= _settings.SwipeThreshold)
                {
                    if (ady > adx)
                        SendHotkey(dy < 0 ? (VK_LWIN, VK_TAB) : (VK_LWIN, VK_D), dy < 0 ? "3F swipe up: Task View" : "3F swipe down: Show Desktop");
                    else
                        SendHotkey(dx < 0 ? (VK_LWIN, VK_LEFT) : (VK_LWIN, VK_RIGHT), dx < 0 ? "3F swipe left: Prev App" : "3F swipe right: Next App");
                    _gesturesFired++;
                }
                else if (_gestureMaxDisp <= _settings.MaxMove)
                {
                    SendHotkey((VK_LWIN, VK_S), "3F tap: Search"); _gesturesFired++;
                }
            }
            else if (_gestureMaxContacts >= 4)
            {
                double dx = _gestureLastCx - _gestureFirstCx, dy = _gestureLastCy - _gestureFirstCy;
                double adx = Math.Abs(dx), ady = Math.Abs(dy);
                if (Math.Max(adx, ady) >= _settings.SwipeThreshold)
                {
                    if (ady > adx)
                        SendHotkey(dy < 0 ? (VK_LWIN, VK_TAB) : (VK_LWIN, VK_D), dy < 0 ? "4F swipe up: Task View" : "4F swipe down: Show Desktop");
                    else
                        SendHotkey(dx < 0 ? (VK_CONTROL, VK_LWIN, VK_LEFT) : (VK_CONTROL, VK_LWIN, VK_RIGHT), dx < 0 ? "4F swipe left: Prev Desktop" : "4F swipe right: Next Desktop");
                    _gesturesFired++;
                }
                else if (_gestureMaxDisp <= _settings.MaxMove)
                {
                    SendHotkey((VK_LWIN, VK_A), "4F tap: Notifications"); _gesturesFired++;
                }
            }
        }

        private bool IsRightZone(int x) => _settings.MirrorX ? x < TouchMaxX * (100 - _settings.RightZonePercent) / 100 : x > TouchMaxX * (100 - _settings.RightZonePercent) / 100;

        // =====================================================================
        //  INPUT INJECTION
        // =====================================================================
        private void InjectClick(bool right)
        {
            if (!_settings.Enabled) return;
            if (_settings.ForceReleaseBeforeClick)
            { InjectMouseButton(MOUSEEVENTF_LEFTUP); InjectMouseButton(MOUSEEVENTF_RIGHTUP); }

            uint down = right ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_LEFTDOWN;
            uint up = right ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_LEFTUP;
            var inputs = new[] { MouseInput(down), MouseInput(up) };
            uint sent = SendInput(2, inputs, Marshal.SizeOf<INPUT>());
            if (sent == 2) { _injected++; _lastClickScreenValid = GetCursorPos(out POINT p); if (_lastClickScreenValid) { _lastClickScreenX = p.X; _lastClickScreenY = p.Y; } }
            else WriteLog($"SendInput click FAILED: {sent}/2");
        }

        private void InjectMouseButton(uint flags)
        {
            var arr = new[] { MouseInput(flags) };
            SendInput(1, arr, Marshal.SizeOf<INPUT>());
        }

        private void InjectWheelDelta(int delta, bool horizontal)
        {
            if (delta == 0) return;
            var mi = new MOUSEINPUT { mouseData = (uint)delta, dwFlags = horizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL };
            var input = new INPUT { type = INPUT_MOUSE, u = new INPUTUNION { mi = mi } };
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
            _wheelEvents++;
        }

        private INPUT MouseInput(uint flags) => new INPUT { type = INPUT_MOUSE, u = new INPUTUNION { mi = new MOUSEINPUT { dwFlags = flags } } };

        private void SendHotkey(params ushort[] keys)
        {
            string desc = "";
            var inputs = new List<INPUT>();
            foreach (var k in keys) { inputs.Add(new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = k } } }); desc += $"VK_{k:X} "; }
            for (int i = keys.Length - 1; i >= 0; i--)
                inputs.Add(new INPUT { type = INPUT_KEYBOARD, u = new INPUTUNION { ki = new KEYBDINPUT { wVk = keys[i], dwFlags = KEYEVENTF_KEYUP } } });
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        }

        private void SendHotkey((ushort, ushort) k, string desc) => SendHotkey(k.Item1, k.Item2);
        private void SendHotkey((ushort, ushort, ushort) k, string desc) => SendHotkey(k.Item1, k.Item2, k.Item3);

        // =====================================================================
        //  DRAG HOLD TIMER
        // =====================================================================
        private void OnDragHoldTimer()
        {
            _dragTimer.Stop();
            if (!_dragArming || !_gestureActive || _curContacts != 1) return;
            if (GetCursorPos(out POINT cp) && Math.Abs(cp.X - _touchStartPos.X) <= DragArmSlop && Math.Abs(cp.Y - _touchStartPos.Y) <= DragArmSlop)
            {
                // After a double-tap hold, inject the held button so dragging can begin.
                _dragArming = false;
                _dragActive = true;
                InjectMouseButton(MOUSEEVENTF_LEFTDOWN);
                WriteLog("Drag started (double-tap hold)");
            }
            else
            {
                _dragArming = false;
                _dragActive = false;
            }
        }

        private void StopDragArming() { _dragTimer.Stop(); _dragArming = false; }

        // =====================================================================
        //  UI REFRESH
        // =====================================================================
        private void RefreshUi()
        {
            // Watchdog
            if (_gestureActive && (DateTime.UtcNow - _lastReportAt).TotalMilliseconds > 250)
            { if (_dragActive) { _dragActive = false; InjectMouseButton(MOUSEEVENTF_LEFTUP); } EndGesture(); }
            if (_dragActive && !_gestureActive) { _dragActive = false; InjectMouseButton(MOUSEEVENTF_LEFTUP); }

            // Tray animation
            if (_curContacts > 0 && !_trayAnimActive) StartTrayAnimation();
            else if (_curContacts == 0 && _trayAnimActive) StopTrayAnimation();

            if (!Visible) { lock (_sync) _pending.Clear(); return; }

            // Flush log
            while (true) { string line; lock (_sync) { if (_pending.Count == 0) break; line = _pending.Dequeue(); } _logBox.AppendText(line + Environment.NewLine); }
            if (_logBox.TextLength > 250000) _logBox.Text = _logBox.Text.Substring(_logBox.TextLength - 150000);

            // Update stats
            _lblContacts.Text = $"Contacts: {_curContacts}";
            _lblGesture.Text = $"Last gesture: {_lastTapText}";
            _statusPill?.Invalidate();
        }

        // =====================================================================
        //  SETTINGS PERSISTENCE
        // =====================================================================
        private AppSettings LoadSettings()
        {
            try { if (File.Exists(_settingsPath)) return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new AppSettings(); } catch { }
            return new AppSettings();
        }

        private void SaveSettings()
        {
            try { File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true })); } catch { }
        }

        private void AutoSave() => SaveSettings();

        private void ReadUiInto() { } // Settings auto-saved via inline handlers

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_exiting)
            {
                e.Cancel = true; Hide();
                _trayIcon.ShowBalloonTip(1500, "Still Running", "Touchpad helper minimized to tray. Right-click tray icon to exit.", ToolTipIcon.None);
            }
            else
            {
                if (_scrollFreezeActive) { ClipCursor(IntPtr.Zero); _scrollFreezeActive = false; }
                try { _trayIcon.Visible = false; _trayIcon.Dispose(); } catch { }
                SaveSettings();
                try { _file.Flush(); _file.Dispose(); } catch { }
            }
        }

        private void WriteLog(string line)
        {
            string full = $"{DateTime.Now:HH:mm:ss.fff} {line}";
            lock (_sync) _pending.Enqueue(full);
            try { _file.WriteLine(full); } catch { }
        }

        private string PointerSummary()
        {
            uint count = 0;
            return GetPointerDevices(ref count, null) ? (count == 0 ? "0 (fallback)" : count.ToString()) : "query failed";
        }
    }
}