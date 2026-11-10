using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using RazerTouchpadHelper.Helpers;
using RazerTouchpadHelper.Models;
using RazerTouchpadHelper.Services;
using RazerTouchpadHelper.UI.Controls;
using NativeMethods = RazerTouchpadHelper.NativeMethods;

namespace RazerTouchpadHelper.UI
{
    public sealed class MainForm : Form
    {
        // Services
        private readonly LoggingService _logging;
        private readonly SettingsService _settingsService;
        private readonly InputInjectionService _inputInjection;
        private readonly RawInputService _rawInput;
        private readonly ScrollProcessor _scrollProcessor;
        private readonly GestureProcessor _gestureProcessor;
        private TrayService _trayService;

        // UI Controls
        private TouchVisualizer _touchVisualizer;
        private TextBox _logBox;
        private Label _lblContacts;
        private Label _lblGesture;
        private StatusPill _statusPill;
        private readonly System.Windows.Forms.Timer _uiTimer = new System.Windows.Forms.Timer { Interval = 33 };
        private readonly Image _razerLogo;
        private readonly Image _razerWordmark;

        // State
        private bool _exiting;
        private readonly bool _startHidden;

        public MainForm(bool startHidden)
        {
            _startHidden = startHidden;

            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "razer-touchpad-helper", "logs");

            // Initialize services
            _logging = new LoggingService(logDir);
            _settingsService = new SettingsService(
                Path.Combine(logDir, "razer-touchpad-helper-settings.json"),
                OnSettingsChanged);

            _inputInjection = new InputInjectionService();
            _scrollProcessor = new ScrollProcessor(
                _inputInjection,
                () => _settingsService.Settings);

            _gestureProcessor = new GestureProcessor(
                _inputInjection,
                () => _settingsService.Settings,
                OnDragStarted,
                OnDragEnded,
                _scrollProcessor,
                WriteLog);

            // Load logo
            (_razerLogo, _razerWordmark) = LogoHelper.LoadRazerLogo(AppDomain.CurrentDomain.BaseDirectory);

            // Build UI (creates handle)
            BuildUi();

            // Initialize raw input AFTER handle is created
            _rawInput = new RawInputService(Handle, OnContactsUpdate);

            // Initialize tray
            _trayService = new TrayService(
                _razerLogo,
                ShowWindow,
                enabled => _settingsService.Update(s => s.Enabled = enabled),
                scroll => _settingsService.Update(s => s.TwoFingerScroll = scroll),
                gestures => _settingsService.Update(s => s.MultiFingerGestures = gestures),
                ExitApplication);

            // Start timers
            _uiTimer.Tick += (s, e) => RefreshUi();
            _uiTimer.Start();

            // Initial log
            WriteLog("=== Razer Touchpad Helper v3 ===");
            WriteLog($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}  StartHidden={startHidden}");
            WriteLog($"Log:  {_logging.LogPath}");
            WriteLog("Physical button bit: IGNORED. All input from touch only.");
            WriteLog("1-finger tap = click (right zone = right click)");
            WriteLog("2-finger = scroll | 2-finger tap = right click");
            WriteLog("3-finger: up=Task View, down=Show Desktop, left/right=Switch App, tap=Search");
            WriteLog("4-finger: up=Task View, down=Show Desktop, left/right=Switch Desktop, tap=Notifications");
            WriteLog("Raw Input: 0x0D:0x05 ONLY (no mouse conflict)");
            WriteLog("Low-level mouse hook: DISABLED (zero global latency)");
        }

        private void BuildUi()
        {
            Text = "Razer Touchpad Helper";
            ClientSize = new Size(1080, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = RazerColors.BgDark;
            ForeColor = RazerColors.TextPrimary;
            MinimumSize = new Size(980, 640);
            ShowInTaskbar = !_startHidden;
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;
            ResizeRedraw = true;
            Icon = LogoHelper.CreateWindowIcon(_razerLogo);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = RazerColors.BgDark,
                Padding = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            Controls.Add(root);

            // ===== HEADER =====
            var headerPanel = new Panel { Dock = DockStyle.Fill, BackColor = RazerColors.BgCard, Padding = new Padding(16, 12, 16, 12) };
            root.Controls.Add(headerPanel, 0, 0);

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            headerPanel.Controls.Add(headerLayout);

            // Logo - top left
            var logoBox = new PictureBox
            {
                Size = new Size(70, 70),
                Image = _razerLogo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 16, 0)
            };
            headerLayout.Controls.Add(logoBox, 0, 0);

            // Title + subtitle - center left
            var titleStack = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 0) };

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
                Text = "Razer Touchpad Helper",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = RazerColors.TextPrimary,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Margin = new Padding(0)
            };

            var subtitle = new Label
            {
                Text = "Razer Blade 16  -  Tap, scroll & multi-finger gestures",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = RazerColors.TextSecondary,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Margin = new Padding(0)
            };

            titleLayout.Controls.Add(title, 0, 0);
            titleLayout.Controls.Add(subtitle, 0, 1);
            titleStack.Controls.Add(titleLayout);
            headerLayout.Controls.Add(titleStack, 1, 0);

            // Status pill - right (fixed size, centered vertically)
            var statusPillContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            _statusPill = new StatusPill { Anchor = AnchorStyles.None };
            statusPillContainer.Resize += (s, e) =>
            {
                _statusPill.Location = new Point(
                    statusPillContainer.Width - _statusPill.Width,
                    (statusPillContainer.Height - _statusPill.Height) / 2);
            };
            statusPillContainer.Controls.Add(_statusPill);
            headerLayout.Controls.Add(statusPillContainer, 2, 0);

            // ===== CONTENT =====
            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = RazerColors.BgDark,
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

            var leftCard = FormBuilder.CreateCard("Live Touch Visualization");
            leftCard.Dock = DockStyle.Fill;
            leftPanel.Controls.Add(leftCard, 0, 0);

            _touchVisualizer = new TouchVisualizer(() => _settingsService.Settings) { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            leftCard.Controls.Add(_touchVisualizer);

            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(14, 8, 14, 8)
            };
            leftPanel.Controls.Add(statsPanel, 0, 1);

            _lblContacts = new Label
            {
                Text = "Contacts: 0",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = RazerColors.Green,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            statsPanel.Controls.Add(_lblContacts);

            _lblGesture = new Label
            {
                Text = "Last gesture: IDLE",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = RazerColors.TextSecondary,
                AutoSize = true
            };
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

            var settingsCard = FormBuilder.CreateCard("Settings");
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

            // Build settings UI
            BuildSettingsUi(settingsFlow);

            // LOG
            var logCard = FormBuilder.CreateCard("Activity Log");
            logCard.Dock = DockStyle.Fill;
            rightPanel.Controls.Add(logCard, 0, 1);

            _logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10f),
                BackColor = RazerColors.BgInput,
                ForeColor = RazerColors.Green,
                BorderStyle = BorderStyle.None,
                HideSelection = false,
                Margin = new Padding(0)
            };
            logCard.Controls.Add(_logBox);

            var logToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 6, 12, 6)
            };
            logCard.Controls.Add(logToolbar);

            var btnClear = FormBuilder.CreateModernButton("Clear", 90, () => _logBox.Clear(), false);
            var btnOpenLogs = FormBuilder.CreateModernButton("Open Log Folder", 150, () =>
            {
                try { System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(_logging.LogPath)); } catch { }
            }, false);
            logToolbar.Controls.Add(btnClear);
            logToolbar.Controls.Add(btnOpenLogs);

            // ===== BOTTOM BAR =====
            var bottomBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = RazerColors.BgCard,
                Padding = new Padding(16, 10, 16, 10),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            bottomBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Controls.Add(bottomBar, 0, 2);

            var rightBtns = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            bottomBar.Controls.Add(rightBtns, 1, 0);
            var btnSave = FormBuilder.CreateModernButton("Save Settings", 150, () => { _settingsService.Save(); WriteLog("Settings saved"); }, true);
            btnSave.Dock = DockStyle.Right;
            rightBtns.Controls.Add(btnSave);

            Resize += (s, e) => { Invalidate(true); _touchVisualizer?.Invalidate(); _statusPill?.Invalidate(); };
            FormClosing += OnFormClosing;
        }

        private void BuildSettingsUi(FlowLayoutPanel settingsFlow)
        {
            var s = _settingsService.Settings;

            FormBuilder.AddSlider(settingsFlow, "Scroll speed", 1, 30, s.ScrollSpeed,
                v => _settingsService.Update(x => x.ScrollSpeed = v),
                "Sensitivity multiplier for two-finger scrolling");
            settingsFlow.Controls.Add(FormBuilder.CreateDivider());

            FormBuilder.AddToggle(settingsFlow, "Enabled", s.Enabled,
                v => _settingsService.Update(x => x.Enabled = v),
                true, "Master on/off for all touchpad processing");
            FormBuilder.AddToggle(settingsFlow, "Two-finger scroll", s.TwoFingerScroll,
                v => _settingsService.Update(x => x.TwoFingerScroll = v),
                false, "Scroll with two fingers on the trackpad");
            FormBuilder.AddToggle(settingsFlow, "Multi-finger gestures", s.MultiFingerGestures,
                v => _settingsService.Update(x => x.MultiFingerGestures = v),
                false, "3/4-finger swipes and taps for system actions");
            FormBuilder.AddToggle(settingsFlow, "Two-finger tap = right click", s.TwoFingerRightClick,
                v => _settingsService.Update(x => x.TwoFingerRightClick = v),
                false, "Tap with two fingers simultaneously for right-click");
            FormBuilder.AddToggle(settingsFlow, "Release stuck button before click", s.ForceReleaseBeforeClick,
                v => _settingsService.Update(x => x.ForceReleaseBeforeClick = v),
                false, "Ensure no mouse button is stuck down before injecting a click");
            FormBuilder.AddToggle(settingsFlow, "Mirror X (swap left/right zones)", s.MirrorX,
                v => _settingsService.Update(x => x.MirrorX = v),
                false, "Swap the left/right click zones horizontally");
            FormBuilder.AddToggle(settingsFlow, "Invert scroll direction", s.InvertScroll,
                v => _settingsService.Update(x => x.InvertScroll = v),
                false, "Natural scrolling (content moves with fingers)");
            FormBuilder.AddToggle(settingsFlow, "Long-press drag", s.LongPressDrag,
                v => _settingsService.Update(x => { x.LongPressDrag = v; if (!v) _gestureProcessor.StopDragArming(); }),
                false, "Hold one finger to start drag (tap-hold-move)");
            FormBuilder.AddToggle(settingsFlow, "Freeze cursor during 2F scroll", s.FreezeCursorOnMultiFinger,
                v => _settingsService.Update(x => x.FreezeCursorOnMultiFinger = v),
                false, "Lock cursor position while scrolling with two fingers");

            settingsFlow.Controls.Add(FormBuilder.CreateDivider());

            FormBuilder.AddNumeric(settingsFlow, "Swipe threshold", 100, 1200, s.SwipeThreshold,
                v => _settingsService.Update(x => x.SwipeThreshold = v),
                "Minimum swipe distance (device units) to trigger a 3/4-finger gesture");
            FormBuilder.AddNumeric(settingsFlow, "Max tap movement", 20, 500, s.MaxMove,
                v => _settingsService.Update(x => x.MaxMove = v),
                "Maximum finger movement (device units) that still counts as a tap");
            FormBuilder.AddNumeric(settingsFlow, "Drag hold (ms)", 100, 2000, s.DragHoldMs,
                v => _settingsService.Update(x => x.DragHoldMs = v),
                "Hold time before long-press drag activates");
            FormBuilder.AddNumeric(settingsFlow, "Click delay (ms)", 0, 100, s.ClickDelayMs,
                v => _settingsService.Update(x => x.ClickDelayMs = v),
                "Delay between mouse down/up (helps Electron/elevated apps)");
            FormBuilder.AddNumeric(settingsFlow, "Right-click zone (%)", 5, 60, s.RightZonePercent,
                v => _settingsService.Update(x => x.RightZonePercent = v),
                "Width of right-click zone from right edge");
            FormBuilder.AddNumeric(settingsFlow, "Max tap time (ms)", 80, 1500, s.MaxTapMs,
                v => _settingsService.Update(x => x.MaxTapMs = v),
                "Maximum duration for a tap to register as a click");

            var settingsNote = new Label
            {
                Text = "Settings are applied instantly and saved automatically.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = RazerColors.TextMuted,
                AutoSize = true,
                Margin = new Padding(2, 10, 0, 0)
            };
            settingsFlow.Controls.Add(settingsNote);
        }

        private void OnSettingsChanged(AppSettings settings)
        {
            // Update tray menu checkboxes
            _trayService?.UpdateState(settings.Enabled, settings.TwoFingerScroll, settings.MultiFingerGestures);
            // Invalidate visualizer to reflect RightZonePercent/MirrorX changes
            _touchVisualizer?.Invalidate();
        }

        private void OnContactsUpdate(List<Contact> contacts)
        {
            // Update visualizer on UI thread
            if (_touchVisualizer.IsHandleCreated)
                _touchVisualizer.BeginInvoke(new Action(() => _touchVisualizer.UpdateContacts(contacts)));

            _gestureProcessor.ProcessContacts(contacts);
        }

        private void OnDragStarted()
        {
            WriteLog("Drag started (double-tap hold)");
        }

        private void OnDragEnded()
        {
            // Drag ended, handled in gesture processor
        }

        private void RefreshUi()
        {
            // Watchdog
            _gestureProcessor.WatchdogCheck();

            // Tray animation
            _trayService.SetContactsActive(_gestureProcessor.CurrentContacts > 0);

            if (!Visible)
                return;

            // Flush log
            foreach (var line in _logging.DrainPending())
                _logBox.AppendText(line + Environment.NewLine);

            if (_logBox.TextLength > 250000)
                _logBox.Text = _logBox.Text.Substring(_logBox.TextLength - 150000);

            // Update stats
            _lblContacts.Text = $"Contacts: {_gestureProcessor.CurrentContacts}";
            _lblGesture.Text = $"Last gesture: {_gestureProcessor.LastTapText}";

            // Update status pill only when state changes to prevent flickering
            bool active = _settingsService.Settings.Enabled && _gestureProcessor.CurrentContacts > 0;
            _statusPill.Active = active;
            _statusPill.AnimationActive = _gestureProcessor.CurrentContacts > 0;
        }

        private void WriteLog(string line) => _logging.Write(line);

        private void ShowWindow()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _exiting = true;
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_exiting)
            {
                e.Cancel = true;
                Hide();
                // TrayService handles its own NotifyIcon
            }
            else
            {
                _scrollProcessor.Stop();
                _settingsService.Save();
                _logging.Dispose();
                _trayService?.Dispose();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_INPUT)
            {
                _rawInput.ProcessMessage(m.LParam);
            }
            base.WndProc(ref m);
        }
    }
}