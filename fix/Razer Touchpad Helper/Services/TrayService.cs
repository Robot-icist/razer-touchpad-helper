using System;
using System.Drawing;
using System.Windows.Forms;
using RazerTouchpadHelper.Helpers;

namespace RazerTouchpadHelper.Services
{
    public sealed class TrayService : IDisposable
    {
        private readonly NotifyIcon _trayIcon;
        private readonly Image _logo;
        private readonly Image _menuIcon;
        private readonly System.Windows.Forms.Timer _animTimer;
        private readonly Action _onOpenSettings;
        private readonly Action<bool> _onEnabledChanged;
        private readonly Action<bool> _onScrollChanged;
        private readonly Action<bool> _onGesturesChanged;
        private readonly Action _onExit;

        private Icon _baseIcon;
        private Icon _activeIcon;
        private bool _animActive;
        private int _animFrame;

        public TrayService(
            Image logo,
            Action onOpenSettings,
            Action<bool> onEnabledChanged,
            Action<bool> onScrollChanged,
            Action<bool> onGesturesChanged,
            Action onExit)
        {
            _logo = logo;
            _onOpenSettings = onOpenSettings;
            _onEnabledChanged = onEnabledChanged;
            _onScrollChanged = onScrollChanged;
            _onGesturesChanged = onGesturesChanged;
            _onExit = onExit;

            _menuIcon = LogoHelper.CreateMenuIcon(logo);
            _baseIcon = LogoHelper.CreateTrayIcon(logo, false);
            _activeIcon = LogoHelper.CreateTrayIcon(logo, true);

            _trayIcon = new NotifyIcon
            {
                Icon = _baseIcon,
                Text = "Razer Touchpad Helper",
                Visible = true
            };

            _animTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _animTimer.Tick += OnAnimTick;

            BuildContextMenu();
            _trayIcon.DoubleClick += (s, e) => _onOpenSettings?.Invoke();
        }

        private void BuildContextMenu()
        {
            var menu = new ContextMenuStrip
            {
                BackColor = RazerColors.BgCard,
                ForeColor = RazerColors.TextPrimary,
                Renderer = new ModernToolStripRenderer(),
                ShowImageMargin = true,
                ImageScalingSize = new Size(16, 16)
            };

            var open = new ToolStripMenuItem("Open Settings") { Font = new Font("Segoe UI", 9f), Image = _menuIcon };
            open.Click += (s, e) => _onOpenSettings?.Invoke();

            var miEnabled = new ToolStripMenuItem("Enabled") { CheckOnClick = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            miEnabled.CheckedChanged += (s, e) => _onEnabledChanged?.Invoke(miEnabled.Checked);

            var miScroll = new ToolStripMenuItem("Two-finger Scroll") { CheckOnClick = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            miScroll.CheckedChanged += (s, e) => _onScrollChanged?.Invoke(miScroll.Checked);

            var miGestures = new ToolStripMenuItem("Multi-finger Gestures") { CheckOnClick = true, Checked = true, Font = new Font("Segoe UI", 9f) };
            miGestures.CheckedChanged += (s, e) => _onGesturesChanged?.Invoke(miGestures.Checked);

            var exit = new ToolStripMenuItem("Exit") { Font = new Font("Segoe UI", 9f), Image = _menuIcon };
            exit.Click += (s, e) => _onExit?.Invoke();

            menu.Items.Add(open);
            menu.Items.Add(new ToolStripSeparator { Margin = new Padding(6, 0, 6, 0) });
            menu.Items.Add(miEnabled);
            menu.Items.Add(miScroll);
            menu.Items.Add(miGestures);
            menu.Items.Add(new ToolStripSeparator { Margin = new Padding(6, 0, 6, 0) });
            menu.Items.Add(exit);

            _trayIcon.ContextMenuStrip = menu;
        }

        public void UpdateState(bool enabled, bool scrollEnabled, bool gesturesEnabled)
        {
            if (_trayIcon.ContextMenuStrip?.Items != null)
            {
                var items = _trayIcon.ContextMenuStrip.Items;
                if (items.Count > 2 && items[2] is ToolStripMenuItem miEnabled)
                    miEnabled.Checked = enabled;
                if (items.Count > 3 && items[3] is ToolStripMenuItem miScroll)
                    miScroll.Checked = scrollEnabled;
                if (items.Count > 4 && items[4] is ToolStripMenuItem miGestures)
                    miGestures.Checked = gesturesEnabled;
            }
        }

        public void SetContactsActive(bool active)
        {
            if (active && !_animActive)
            {
                _animActive = true;
                _animFrame = 0;
                _animTimer.Start();
            }
            else if (!active && _animActive)
            {
                _animActive = false;
                _animTimer.Stop();
                _trayIcon.Icon = _baseIcon;
            }
        }

        private void OnAnimTick(object sender, EventArgs e)
        {
            if (!_animActive) return;

            _animFrame++;
            if (_animFrame > 20)
            {
                _animFrame = 0;
            }

            float progress = _animFrame / 20f;
            _trayIcon.Icon = LogoHelper.CreateTrayIcon(_logo, true, progress);
        }

        public void Dispose()
        {
            _animTimer.Stop();
            _animTimer.Dispose();
            try { _trayIcon.Visible = false; } catch { }
            try { _trayIcon.Dispose(); } catch { }
            try { _baseIcon?.Dispose(); } catch { }
            try { _activeIcon?.Dispose(); } catch { }
        }

        private class ModernToolStripRenderer : ToolStripProfessionalRenderer
        {
            public ModernToolStripRenderer() : base(new ModernColorTable()) { }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Selected ? RazerColors.Green : RazerColors.TextPrimary;
                base.OnRenderItemText(e);
            }
        }

        private class ModernColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder => RazerColors.BorderColor;
            public override Color ToolStripDropDownBackground => RazerColors.BgCard;
            public override Color MenuItemSelected => RazerColors.BgCardHover;
            public override Color MenuItemSelectedGradientBegin => RazerColors.BgCardHover;
            public override Color MenuItemSelectedGradientEnd => RazerColors.BgCardHover;
            public override Color SeparatorDark => RazerColors.BorderColor;
            public override Color SeparatorLight => RazerColors.BorderColor;
        }
    }
}