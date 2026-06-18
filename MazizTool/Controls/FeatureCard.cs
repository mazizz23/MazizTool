using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MazizTool.Controls
{
    public class FeatureCard : Control
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string Category { get; set; } = "";
        public Color IconBg { get; set; } = Theme.Accent;

        private float _hoverT;
        private float _targetHover;
        private Timer _animTimer;

        public FeatureCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            BackColor = Theme.Surface;
            Size = new Size(280, 140);
            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += Animate;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _targetHover = 1f;
            if (!_animTimer.Enabled) _animTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _targetHover = 0f;
            if (!_animTimer.Enabled) _animTimer.Start();
        }

        private void Animate(object sender, EventArgs e)
        {
            if (Math.Abs(_hoverT - _targetHover) > 0.003f)
            {
                _hoverT += (_targetHover - _hoverT) * 0.18f;
                Invalidate();
            }
            else
            {
                _hoverT = _targetHover;
                _animTimer.Stop();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.Clear(Theme.Surface);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            if (_hoverT > 0.01f)
            {
                GraphicsExt.DrawShadow(g, rect, 12, (int)(10 * _hoverT), 80);
                var bg = Anim.LerpColor(Theme.Surface, Theme.SurfaceLight, _hoverT * 0.7f);
                using (var path = GraphicsExt.RoundedRect(rect, 12))
                using (var brush = new SolidBrush(bg))
                    g.FillPath(brush, path);
            }
            else
            {
                using (var path = GraphicsExt.RoundedRect(rect, 12))
                using (var brush = new SolidBrush(Theme.Surface))
                    g.FillPath(brush, path);
            }

            var borderColor = Anim.LerpColor(Theme.Border, Theme.Accent, _hoverT * 0.6f);
            using (var path2 = GraphicsExt.RoundedRect(rect, 12))
            using (var pen = new Pen(borderColor, 1))
                g.DrawPath(pen, path2);

            var iconSize = 44;
            var iconRect = new Rectangle(18, 18, iconSize, iconSize);
            using (var iconPath = GraphicsExt.RoundedRect(iconRect, 10))
            using (var iconBrush = new LinearGradientBrush(iconRect, IconBg, GraphicsExt.Darken(IconBg, 30), 90f))
                g.FillPath(iconBrush, iconPath);
            TextRenderer.DrawText(g, Icon, new Font("Segoe UI", 16f), iconRect,
                Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            TextRenderer.DrawText(g, Title, new Font("Segoe UI", 12f, FontStyle.Bold),
                new Rectangle(18, 68, Width - 36, 24), Theme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(g, Subtitle, new Font("Segoe UI", 9f),
                new Rectangle(18, 92, Width - 36, 20), Theme.TextSecondary,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            var catColor = Anim.LerpColor(Theme.TextMuted, Theme.Accent, _hoverT);
            var catBg = Anim.LerpColor(Theme.SurfaceLight, Theme.AccentDarker, _hoverT * 0.5f);
            var catSize = TextRenderer.MeasureText(Category, new Font("Segoe UI", 7.5f, FontStyle.Bold));
            var catRect = new Rectangle(18, Height - 28, catSize.Width + 16, 18);
            using (var catPath = GraphicsExt.RoundedRect(catRect, 4))
            using (var catBrush = new SolidBrush(catBg))
                g.FillPath(catBrush, catPath);
            TextRenderer.DrawText(g, Category, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                catRect, catColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            if (_hoverT > 0.3f)
            {
                int arrowAlpha = (int)(200 * _hoverT);
                TextRenderer.DrawText(g, "→", new Font("Segoe UI", 14f, FontStyle.Bold),
                    new Rectangle(Width - 32, 18, 20, 24),
                    Color.FromArgb(arrowAlpha, Theme.Accent),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _animTimer?.Stop(); _animTimer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
