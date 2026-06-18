using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MazizTool;

namespace MazizTool.Controls
{
    public class NavButton : Control
    {
        public Color IconColor { get; set; } = Theme.TextSecondary;
        public Color ActiveColor { get; set; } = Theme.Accent;
        public bool IsActive { get; set; }
        public string Icon { get; set; } = "";
        public string Label { get; set; } = "";

        private float _hoverT;
        private float _activeT;
        private float _targetHover;
        private float _targetActive;
        private Timer _animTimer;

        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.Transparent;
            Height = 34;
            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += Animate;
            _animTimer.Start();
        }

        public void SetActive(bool active)
        {
            _targetActive = active ? 1f : 0f;
            IsActive = active;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _targetHover = 1f;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _targetHover = 0f;
        }

        private void Animate(object sender, EventArgs e)
        {
            bool changed = false;
            if (Math.Abs(_hoverT - _targetHover) > 0.003f) { _hoverT += (_targetHover - _hoverT) * 0.3f; changed = true; }
            if (Math.Abs(_activeT - _targetActive) > 0.003f) { _activeT += (_targetActive - _activeT) * 0.25f; changed = true; }
            if (changed) Invalidate();
            if (!changed && Math.Abs(_hoverT - _targetHover) < 0.001f && Math.Abs(_activeT - _targetActive) < 0.001f)
            {
                _hoverT = _targetHover;
                _activeT = _targetActive;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(4, 2, Width - 8, Height - 4);
            int r = 10;

            var bgColor = Color.FromArgb(
                (int)(Theme.Surface.R + (Theme.SurfaceLight.R - Theme.Surface.R) * _hoverT),
                (int)(Theme.Surface.G + (Theme.SurfaceLight.G - Theme.Surface.G) * _hoverT),
                (int)(Theme.Surface.B + (Theme.SurfaceLight.B - Theme.Surface.B) * _hoverT));

            if (_activeT > 0.01f)
            {
                int alpha = (int)(60 * _activeT);
                bgColor = Color.FromArgb(
                    (int)(bgColor.R + (Theme.AccentDarker.R - bgColor.R) * _activeT * 0.5f),
                    (int)(bgColor.G + (Theme.AccentDarker.G - bgColor.G) * _activeT * 0.5f),
                    (int)(bgColor.B + (Theme.AccentDarker.B - bgColor.B) * _activeT * 0.5f));
            }

            using (var path = GraphicsExt.RoundedRect(rect, r))
            using (var brush = new SolidBrush(bgColor))
                g.FillPath(brush, path);

            if (_activeT > 0.01f)
            {
                int alpha = (int)(40 * _activeT);
                using (var glowPen = new Pen(Color.FromArgb(alpha, Theme.Accent), 2))
                using (var glowPath = GraphicsExt.RoundedRect(Rectangle.Inflate(rect, 1, 1), r + 1))
                    g.DrawPath(glowPen, glowPath);
            }

            if (_activeT > 0.01f)
            {
                int barW = 3;
                int barH = (int)((Height - 12) * _activeT);
                int barY = (Height - barH) / 2;
                var barRect = new Rectangle(8, barY, barW, barH);
                using (var path = GraphicsExt.RoundedRect(barRect, barW / 2))
                using (var brush = new SolidBrush(Theme.Accent))
                    g.FillPath(brush, path);
            }

            var iconColor = Color.FromArgb(
                (int)(IconColor.R + (ActiveColor.R - IconColor.R) * _activeT),
                (int)(IconColor.G + (ActiveColor.G - IconColor.G) * _activeT),
                (int)(IconColor.B + (ActiveColor.B - IconColor.B) * _activeT));
            if (_hoverT > 0 && _activeT < 0.5f)
                iconColor = Color.FromArgb(
                    (int)(iconColor.R + (ActiveColor.R - iconColor.R) * _hoverT * 0.5f),
                    (int)(iconColor.G + (ActiveColor.G - iconColor.G) * _hoverT * 0.5f),
                    (int)(iconColor.B + (ActiveColor.B - iconColor.B) * _hoverT * 0.5f));

            int iconX = _activeT > 0.01f ? 24 : 18;
            var iconRect = new Rectangle(iconX, 0, 28, Height);
            TextRenderer.DrawText(g, Icon, new Font("Segoe UI", 12f), iconRect, iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var textColor = Color.FromArgb(
                (int)(Theme.TextSecondary.R + (Theme.TextPrimary.R - Theme.TextSecondary.R) * Math.Max(_hoverT, _activeT)),
                (int)(Theme.TextSecondary.G + (Theme.TextPrimary.G - Theme.TextSecondary.G) * Math.Max(_hoverT, _activeT)),
                (int)(Theme.TextSecondary.B + (Theme.TextPrimary.B - Theme.TextSecondary.B) * Math.Max(_hoverT, _activeT)));
            var textRect = new Rectangle(52, 0, Width - 58, Height);
            TextRenderer.DrawText(g, Label, Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    public class ToastNotif : Form
    {
        private Timer _lifeTimer;
        private Timer _fadeTimer;
        private float _opacity;
        private int _targetOpacity;
        private int _msLeft;

        public Color AccentColor { get; set; } = Theme.Accent;
        public int DurationMs { get; set; } = 2500;

        public static void Show(Form owner, string message, Color? color = null, int duration = 2500)
        {
            if (owner == null || !owner.IsHandleCreated || owner.IsDisposed) return;
            owner.BeginInvoke(new Action(() =>
            {
                try
                {
                    var toast = new ToastNotif { AccentColor = color ?? Theme.Accent, DurationMs = duration };
                    toast.SetText(message);
                    toast.ShowTo(owner);
                }
                catch { }
            }));
        }

        public ToastNotif()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.SurfaceElevated;
            Opacity = 0;
            Size = new Size(320, 48);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        public void SetText(string message)
        {
            using (var g = CreateGraphics())
            {
                var size = g.MeasureString(message, new Font("Segoe UI", 9.5f));
                int w = Math.Max(280, (int)size.Width + 64);
                if (w > 600) w = 600;
                Size = new Size(w, 48);
            }
            _msg = message;
            Invalidate();
        }

        private string _msg = "";

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GraphicsExt.RoundedRect(rect, 10))
            using (var brush = new SolidBrush(Theme.SurfaceElevated))
                g.FillPath(brush, path);

            var lineRect = new Rectangle(0, 0, 4, Height);
            using (var linePath = GraphicsExt.RoundedRect(lineRect, 2))
            using (var brush = new LinearGradientBrush(new Point(0, 0), new Point(0, Height),
                Color.FromArgb(200, AccentColor), Color.FromArgb(50, AccentColor)))
                g.FillPath(brush, linePath);

            var textRect = new Rectangle(18, 0, Width - 28, Height);
            TextRenderer.DrawText(g, _msg, new Font("Segoe UI", 9.5f), textRect, Theme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        public void ShowTo(Form owner)
        {
            var x = owner.Left + (owner.Width - Width) / 2;
            var y = owner.Bottom - Height - 32;
            Location = new Point(x, y);
            owner.AddOwnedForm(this);
            base.Show();

            _targetOpacity = 1;
            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += (s, e) =>
            {
                if (_targetOpacity == 1)
                {
                    _opacity += 0.15f;
                    if (_opacity >= 1f) { _opacity = 1f; _fadeTimer.Stop(); StartLife(); }
                }
                else
                {
                    _opacity -= 0.12f;
                    if (_opacity <= 0f) { _opacity = 0f; _fadeTimer.Stop(); Close(); }
                }
                Opacity = _opacity;
            };
            _fadeTimer.Start();
        }

        private void StartLife()
        {
            _msLeft = DurationMs;
            _lifeTimer = new Timer { Interval = 100 };
            _lifeTimer.Tick += (s, e) =>
            {
                _msLeft -= 100;
                if (_msLeft <= 0) { _lifeTimer.Stop(); _targetOpacity = 0; _fadeTimer.Start(); }
            };
            _lifeTimer.Start();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000008;
                return cp;
            }
        }
    }
}
