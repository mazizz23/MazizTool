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
        public Color DisabledColor { get; set; } = Theme.SurfaceElevated;
        public int Radius { get; set; } = 8;
        public bool Elevated { get; set; } = true;
        public bool Filled { get; set; } = true;
        public bool Outline { get; set; } = false;
        public int ElevationDepth { get; set; } = 6;

        private float _hoverT = 0f;
        private float _pressT = 0f;
        private float _elevationT = 0f;
        private AnimTimer _hoverAnim;
        private AnimTimer _pressAnim;
        private List<Ripple> _ripples = new List<Ripple>();
        private Timer _rippleTimer;

        public MaterialButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Color.Black;
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            Cursor = Cursors.Hand;

            _hoverAnim = new AnimTimer();
            _pressAnim = new AnimTimer();
            _rippleTimer = new Timer { Interval = 16 };
            _rippleTimer.Tick += (s, e) => UpdateRipples();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            AnimateHover(1f, 180);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            AnimateHover(0f, 200);
            if (_pressT > 0) AnimatePress(0f, 150);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            AnimatePress(1f, 100);
            if (e.Button == MouseButtons.Left)
                AddRipple(e.Location);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            AnimatePress(0f, 200);
        }

        private void AnimateHover(float target, int ms)
        {
            float start = _hoverT;
            _hoverAnim.Start(ms, t =>
            {
                _hoverT = Anim.Lerp(start, target, Anim.EaseOut(t));
                Invalidate();
            });
        }

        private void AnimatePress(float target, int ms)
        {
            float start = _pressT;
            _pressAnim.Start(ms, t =>
            {
                _pressT = Anim.Lerp(start, target, Anim.EaseOut(t));
                _elevationT = _pressT;
                Invalidate();
            });
        }

        private void AddRipple(Point location)
        {
            _ripples.Add(new Ripple { Origin = location, Age = 0, Life = 600 });
            if (!_rippleTimer.Enabled) _rippleTimer.Start();
        }

        private void UpdateRipples()
        {
            bool any = false;
            foreach (var r in _ripples)
            {
                r.Age += 16;
                if (r.Age < r.Life) any = true;
            }
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
            int elevationOffset = (int)(_elevationT * ElevationDepth * 0.3f);
            var drawRect = new Rectangle(rect.X, rect.Y - elevationOffset / 2, rect.Width, rect.Height);

            if (Elevated && _hoverT > 0.01f)
            {
                int depth = (int)(ElevationDepth * _hoverT);
                GraphicsExt.DrawShadow(g, drawRect, Radius, depth);
            }

            Color bgColor;
            if (Outline)
            {
                bgColor = Anim.LerpColor(Theme.Surface, Theme.SurfaceElevated, _hoverT);
            }
            else
            {
                bgColor = Anim.LerpColor(AccentColor, HoverColor, _hoverT);
                bgColor = Anim.LerpColor(bgColor, Theme.AccentDark, _pressT * 0.6f);
            }

            using (var path = GraphicsExt.RoundedRect(drawRect, Radius))
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillPath(brush, path);

                if (Outline)
                {
                    var penColor = Anim.LerpColor(AccentColor, HoverColor, _hoverT);
                    using (var pen = new Pen(penColor, 1.5f))
                        g.DrawPath(pen, path);
                }
            }

            foreach (var ripple in _ripples)
            {
                float t = (float)ripple.Age / ripple.Life;
                int maxRadius = Math.Max(Width, Height);
                int radius = (int)(maxRadius * Anim.EaseOut(t));
                int alpha = (int)(180 * (1f - t));
                using (var brush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                {
                    var bounds = new Rectangle(ripple.Origin.X - radius, ripple.Origin.Y - radius, radius * 2, radius * 2);
                    using (var path = GraphicsExt.RoundedRect(drawRect, Radius))
                    {
                        g.SetClip(path);
                        g.FillEllipse(brush, bounds);
                        g.ResetClip();
                    }
                }
            }

            var textColor = Outline
                ? Anim.LerpColor(AccentColor, HoverColor, _hoverT)
                : CalcTextColor(AccentColor);
            TextRenderer.DrawText(g, Text, Font, drawRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private Color CalcTextColor(Color bg)
        {
            double brightness = (bg.R * 0.299 + bg.G * 0.587 + bg.B * 0.114) / 255.0;
            return brightness > 0.55 ? Color.FromArgb(8, 18, 14) : Color.White;
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
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
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
        private AnimTimer _focusAnim;
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
            Inner.GotFocus += (s, e) => AnimateFocus(1f);
            Inner.LostFocus += (s, e) => AnimateFocus(0f);
            Controls.Add(Inner);

            _focusAnim = new AnimTimer();
        }

        private void AnimateFocus(float target)
        {
            float start = _focusT;
            _focusAnim.Start(200, t =>
            {
                _focusT = Anim.Lerp(start, target, Anim.EaseOut(t));
                Invalidate();
            });
        }

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
    }
}
