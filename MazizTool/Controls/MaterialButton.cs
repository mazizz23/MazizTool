using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MazizTool.Controls
{
    public class MaterialButton : Control
    {
        public Color AccentColor { get; set; } = Theme.Accent;
        public int Radius { get; set; } = 8;
        public int ElevationDepth { get; set; } = 4;

        private float _hoverT = 0f;
        private float _pressT = 0f;
        private float _targetHover = 0f;
        private float _targetPress = 0f;
        private List<Ripple> _ripples = new List<Ripple>();
        private Timer _animTimer;
        private Timer _rippleTimer;

        public MaterialButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Opaque, true);
            BackColor = Theme.Background;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Cursor = Cursors.Hand;

            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += AnimateTick;
            _rippleTimer = new Timer { Interval = 16 };
            _rippleTimer.Tick += RippleTick;
            Theme.ThemeChanged += () => { AccentColor = Theme.Accent; Invalidate(); };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _targetHover = 1f;
            StartAnim();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _targetHover = 0f;
            if (_targetPress != 0) { _targetPress = 0f; StartAnim(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _targetPress = 1f;
            StartAnim();
            if (e.Button == MouseButtons.Left) AddRipple(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _targetPress = 0f;
            StartAnim();
        }

        private void StartAnim() { if (!_animTimer.Enabled) _animTimer.Start(); }

        private void AnimateTick(object sender, EventArgs e)
        {
            bool changed = false;
            if (Math.Abs(_hoverT - _targetHover) > 0.003f) { _hoverT += (_targetHover - _hoverT) * 0.2f; changed = true; }
            if (Math.Abs(_pressT - _targetPress) > 0.003f) { _pressT += (_targetPress - _pressT) * 0.25f; changed = true; }
            if (changed) Invalidate();
            else { _hoverT = _targetHover; _pressT = _targetPress; _animTimer.Stop(); Invalidate(); }
        }

        private void AddRipple(Point location)
        {
            _ripples.Add(new Ripple { Origin = location, Age = 0, Life = 500 });
            if (!_rippleTimer.Enabled) _rippleTimer.Start();
        }

        private void RippleTick(object sender, EventArgs e)
        {
            bool any = false;
            foreach (var r in _ripples) { r.Age += 16; if (r.Age < r.Life) any = true; }
            _ripples.RemoveAll(r => r.Age >= r.Life);
            Invalidate();
            if (!any) _rippleTimer.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            var bgColor = LerpColor(AccentColor, Lighten(AccentColor, 30), _hoverT);
            bgColor = LerpColor(bgColor, Darken(AccentColor, 20), _pressT * 0.5f);

            using (var path = GraphicsExt.RoundedRect(rect, Radius))
            using (var brush = new SolidBrush(bgColor))
                g.FillPath(brush, path);

            if (_ripples.Count > 0)
            {
                using (var clipPath = GraphicsExt.RoundedRect(rect, Radius))
                {
                    g.SetClip(clipPath);
                    foreach (var ripple in _ripples)
                    {
                        float t = (float)ripple.Age / ripple.Life;
                        int maxR = (int)Math.Sqrt(Width * Width + Height * Height);
                        int radius = (int)(maxR * (1f - (1f - t) * (1f - t)));
                        int alpha = (int)(100 * (1f - t));
                        using (var brush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                            g.FillEllipse(brush, ripple.Origin.X - radius, ripple.Origin.Y - radius, radius * 2, radius * 2);
                    }
                    g.ResetClip();
                }
            }

            var textColor = CalcTextColor(bgColor);
            TextRenderer.DrawText(g, Text, Font, rect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private Color LerpColor(Color a, Color b, float t)
        {
            if (t <= 0) return a;
            if (t >= 1) return b;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private Color Lighten(Color c, int amount) => Color.FromArgb(
            Math.Min(255, c.R + amount), Math.Min(255, c.G + amount), Math.Min(255, c.B + amount));

        private Color Darken(Color c, int amount) => Color.FromArgb(
            Math.Max(0, c.R - amount), Math.Max(0, c.G - amount), Math.Max(0, c.B - amount));

        private Color CalcTextColor(Color bg)
        {
            double brightness = (bg.R * 0.299 + bg.G * 0.587 + bg.B * 0.114) / 255.0;
            return brightness > 0.55 ? Color.FromArgb(10, 14, 12) : Color.White;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer?.Stop(); _animTimer?.Dispose();
                _rippleTimer?.Stop(); _rippleTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private class Ripple { public Point Origin; public int Age; public int Life; }
    }

    public class MaterialCard : Panel
    {
        public int Radius { get; set; } = 10;
        public int Elevation { get; set; } = 3;
        public Color CardColor { get; set; } = Theme.Surface;

        public MaterialCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
            Theme.ThemeChanged += () => Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = GraphicsExt.RoundedRect(rect, Radius))
            using (var brush = new SolidBrush(CardColor))
                g.FillPath(brush, path);
        }
    }
}
