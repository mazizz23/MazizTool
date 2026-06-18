using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MazizTool;

namespace MazizTool.Controls
{
    public class MaterialButton : Control
    {
        public Color AccentColor { get; set; } = Theme.Accent;
        public Color HoverColor { get; set; } = Theme.AccentHover;
        public int Radius { get; set; } = 8;
        public bool Elevated { get; set; } = true;
        public bool Filled { get; set; } = true;
        public bool Outline { get; set; } = false;
        public int ElevationDepth { get; set; } = 6;
        public int BorderThickness { get; set; } = 2;

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
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.Opaque, true);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Cursor = Cursors.Hand;

            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += AnimateTick;
            _rippleTimer = new Timer { Interval = 16 };
            _rippleTimer.Tick += RippleTick;
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

        private void StartAnim()
        {
            if (!_animTimer.Enabled) _animTimer.Start();
        }

        private void AnimateTick(object sender, EventArgs e)
        {
            bool changed = false;
            if (Math.Abs(_hoverT - _targetHover) > 0.003f) { _hoverT += (_targetHover - _hoverT) * 0.25f; changed = true; }
            if (Math.Abs(_pressT - _targetPress) > 0.003f) { _pressT += (_targetPress - _pressT) * 0.3f; changed = true; }
            if (changed) Invalidate();
            else { _hoverT = _targetHover; _pressT = _targetPress; _animTimer.Stop(); Invalidate(); }
        }

        private void AddRipple(Point location)
        {
            _ripples.Add(new Ripple { Origin = location, Age = 0, Life = 550 });
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

            if (Elevated && _hoverT > 0.01f)
            {
                int depth = (int)(ElevationDepth * _hoverT);
                GraphicsExt.DrawShadow(g, rect, Radius, depth);
            }

            Color bgColor;
            if (Outline)
            {
                bgColor = Anim.LerpColor(Theme.Surface, Theme.SurfaceLight, _hoverT);
                bgColor = Anim.LerpColor(bgColor, Theme.AccentDark, _pressT * 0.3f);
            }
            else
            {
                bgColor = Anim.LerpColor(AccentColor, HoverColor, _hoverT);
                bgColor = Anim.LerpColor(bgColor, Theme.AccentDark, _pressT * 0.5f);
            }

            using (var path = GraphicsExt.RoundedRect(rect, Radius))
            using (var brush = new SolidBrush(bgColor))
                g.FillPath(brush, path);

            if (Outline)
            {
                var penColor = Anim.LerpColor(AccentColor, HoverColor, _hoverT);
                var penWidth = BorderThickness + _pressT * 0.5f;
                var penRect = new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                using (var penPath = GraphicsExt.RoundedRect(penRect, Radius))
                using (var pen = new Pen(penColor, penWidth))
                {
                    pen.Alignment = PenAlignment.Center;
                    g.DrawPath(pen, penPath);
                }
            }

            if (_ripples.Count > 0)
            {
                using (var clipPath = GraphicsExt.RoundedRect(rect, Radius))
                {
                    g.SetClip(clipPath);
                    foreach (var ripple in _ripples)
                    {
                        float t = (float)ripple.Age / ripple.Life;
                        int maxRadius = (int)Math.Sqrt(Width * Width + Height * Height);
                        int radius = (int)(maxRadius * Anim.EaseOut(t));
                        int alpha = (int)(120 * (1f - t));
                        using (var brush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                            g.FillEllipse(brush, ripple.Origin.X - radius, ripple.Origin.Y - radius, radius * 2, radius * 2);
                    }
                    g.ResetClip();
                }
            }

            var textColor = Outline
                ? Anim.LerpColor(AccentColor, HoverColor, _hoverT)
                : CalcTextColor(bgColor);
            var textRect = new Rectangle(0, 0, Width, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private Color CalcTextColor(Color bg)
        {
            double brightness = (bg.R * 0.299 + bg.G * 0.587 + bg.B * 0.114) / 255.0;
            return brightness > 0.55 ? Color.FromArgb(8, 18, 14) : Color.White;
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

        private class Ripple
        {
            public Point Origin;
            public int Age;
            public int Life;
        }
    }

    public class MaterialCard : Panel
    {
        public int Radius { get; set; } = 12;
        public int Elevation { get; set; } = 4;
        public Color CardColor { get; set; } = Theme.Surface;

        public MaterialCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            GraphicsExt.DrawShadow(g, rect, Radius, Elevation);
            using (var path = GraphicsExt.RoundedRect(rect, Radius))
            using (var brush = new SolidBrush(CardColor))
                g.FillPath(brush, path);
        }
    }

    public class MaterialTextBox : UserControl
    {
        public TextBox Inner { get; private set; }
        public Color LineColor { get; set; } = Theme.Border;
        public Color FocusColor { get; set; } = Theme.Accent;
        public string Placeholder { get => Inner.PlaceholderText; set => Inner.PlaceholderText = value; }

        private float _focusT = 0f;
        private float _targetFocus = 0f;
        private Timer _focusTimer;
        public new string Text { get => Inner.Text; set => Inner.Text = value; }

        public MaterialTextBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.InputBg;
            Height = 36;

            Inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Cascadia Code", 9f),
                Location = new Point(10, 8),
                Width = Width - 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Inner.GotFocus += (s, e) => { _targetFocus = 1f; StartFocus(); };
            Inner.LostFocus += (s, e) => { _targetFocus = 0f; StartFocus(); };
            Controls.Add(Inner);

            _focusTimer = new Timer { Interval = 16 };
            _focusTimer.Tick += (s, e) =>
            {
                if (Math.Abs(_focusT - _targetFocus) > 0.003f) { _focusT += (_targetFocus - _focusT) * 0.25f; Invalidate(); }
                else { _focusT = _targetFocus; _focusTimer.Stop(); Invalidate(); }
            };
        }

        private void StartFocus() { if (!_focusTimer.Enabled) _focusTimer.Start(); }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Inner != null) Inner.Width = Width - 20;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(Theme.InputBg))
                g.FillRectangle(brush, 0, 0, Width, Height);

            var lineColor = Anim.LerpColor(LineColor, FocusColor, _focusT);
            int lineY = Height - 2;
            int lineW = (int)(Width * (0.4f + 0.6f * _focusT));
            int lineX = (Width - lineW) / 2;
            using (var pen = new Pen(lineColor, 2))
                g.DrawLine(pen, lineX, lineY, lineX + lineW, lineY);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _focusTimer?.Stop(); _focusTimer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
