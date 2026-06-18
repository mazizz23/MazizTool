using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MazizTool;

namespace MazizTool.Controls
{
    public class ToastNotif : Form
    {
        private Timer _lifeTimer;
        private Timer _fadeTimer;
        private float _opacity = 0f;
        private int _targetOpacity = 0;
        private int _msLeft;

        public Color AccentColor { get; set; } = Theme.Accent;
        public int DurationMs { get; set; } = 2500;

        public static void Show(Form owner, string message, Color? color = null, int duration = 2500)
        {
            if (owner == null || !owner.IsHandleCreated) return;
            owner.BeginInvoke(new Action(() =>
            {
                var toast = new ToastNotif
                {
                    AccentColor = color ?? Theme.Accent,
                    DurationMs = duration
                };
                toast.SetText(message);
                toast.Show(owner);
            }));
        }

        public ToastNotif()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(20, 40, 32);
            Opacity = 0;
            Size = new Size(320, 56);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
        }

        public void SetText(string message)
        {
            using (var g = CreateGraphics())
            {
                var size = g.MeasureString(message, new Font("Segoe UI", 10f));
                int w = Math.Max(280, (int)size.Width + 80);
                Size = new Size(w, 56);
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
            using (var brush = new SolidBrush(Color.FromArgb(20, 40, 32)))
                g.FillPath(brush, path);

            using (var pen = new Pen(AccentColor, 2))
                g.DrawLine(pen, 0, 4, 0, Height - 4);

            var iconRect = new Rectangle(16, 14, 28, 28);
            using (var path = GraphicsExt.RoundedRect(iconRect, 6))
            using (var brush = new SolidBrush(AccentColor))
                g.FillPath(brush, path);
            TextRenderer.DrawText(g, "✓", new Font("Segoe UI", 12f, FontStyle.Bold), iconRect, Color.FromArgb(8, 18, 14),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var textRect = new Rectangle(54, 0, Width - 60, Height);
            TextRenderer.DrawText(g, _msg, new Font("Segoe UI", 10f), textRect, Theme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        public new void Show(Form owner)
        {
            var x = owner.Right - Width - 24;
            var y = owner.Bottom - Height - 24;
            Location = new Point(x, y);
            base.Show(owner);

            _targetOpacity = 1;
            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += (s, e) =>
            {
                if (_targetOpacity == 1)
                {
                    _opacity += 0.12f;
                    if (_opacity >= 1f) { _opacity = 1f; _fadeTimer.Stop(); StartLife(); }
                }
                else
                {
                    _opacity -= 0.10f;
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
                if (_msLeft <= 0)
                {
                    _lifeTimer.Stop();
                    _targetOpacity = 0;
                    _fadeTimer.Start();
                }
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

    public class NavButton : Control
    {
        public Color IconColor { get; set; } = Theme.TextSecondary;
        public Color ActiveColor { get; set; } = Theme.Accent;
        public bool IsActive { get; set; } = false;
        public string Icon { get; set; } = "";
        public string Label { get; set; } = "";

        private float _hoverT = 0f;
        private float _activeT = 0f;
        private AnimTimer _anim;

        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            _anim = new AnimTimer();
        }

        public void SetActive(bool active)
        {
            float start = _activeT;
            float target = active ? 1f : 0f;
            _anim.Start(220, t =>
            {
                _activeT = Anim.Lerp(start, target, Anim.EaseOut(t));
                Invalidate();
            });
            IsActive = active;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            float start = _hoverT;
            _anim.Start(180, t => { _hoverT = Anim.Lerp(start, 1f, Anim.EaseOut(t)); Invalidate(); });
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            float start = _hoverT;
            _anim.Start(180, t => { _hoverT = Anim.Lerp(start, 0f, Anim.EaseOut(t)); Invalidate(); });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width, Height);
            var bg = Anim.LerpColor(Theme.Surface, Theme.SurfaceLight, _hoverT);
            bg = Anim.LerpColor(bg, Theme.AccentDark, _activeT * 0.4f);
            using (var brush = new SolidBrush(bg))
            using (var path = GraphicsExt.RoundedRect(rect, 6))
                g.FillPath(brush, path);

            if (_activeT > 0.01f)
            {
                int barH = (int)(Height * 0.55f * _activeT);
                int barY = (Height - barH) / 2;
                var barRect = new Rectangle(0, barY, 3, barH);
                using (var brush = new SolidBrush(ActiveColor))
                    g.FillRectangle(brush, barRect);
            }

            var iconColor = Anim.LerpColor(IconColor, ActiveColor, _activeT);
            iconColor = Anim.LerpColor(iconColor, ActiveColor, _hoverT * 0.4f);
            var iconRect = new Rectangle(10, 0, 28, Height);
            TextRenderer.DrawText(g, Icon, new Font("Segoe UI", 11f), iconRect, iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var labelColor = Anim.LerpColor(Theme.TextSecondary, Theme.TextPrimary, Math.Max(_hoverT, _activeT));
            var labelRect = new Rectangle(42, 0, Width - 48, Height);
            TextRenderer.DrawText(g, Label, Font, labelRect, labelColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
