using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MazizTool.Controls
{
    public class IconButton : Control
    {
        public bool IsActive { get; set; }
        public string Icon { get; set; } = "";
        public string Tooltip { get; set; } = "";

        private float _hoverT;
        private float _activeT;
        private float _targetHover;
        private float _targetActive;
        private Timer _animTimer;
        private ToolTip _tooltip;

        public IconButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.Opaque, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.Transparent;
            Size = new Size(48, 48);
            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += Animate;
            _tooltip = new ToolTip { InitialDelay = 200, ShowAlways = true };
            Theme.ThemeChanged += () => Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!string.IsNullOrEmpty(Tooltip)) _tooltip.SetToolTip(this, Tooltip);
        }

        public void SetActive(bool active)
        {
            _targetActive = active ? 1f : 0f;
            IsActive = active;
            StartAnim();
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
            StartAnim();
        }

        private void StartAnim() { if (!_animTimer.Enabled) _animTimer.Start(); }

        private void Animate(object sender, EventArgs e)
        {
            bool changed = false;
            if (Math.Abs(_hoverT - _targetHover) > 0.003f) { _hoverT += (_targetHover - _hoverT) * 0.2f; changed = true; }
            if (Math.Abs(_activeT - _targetActive) > 0.003f) { _activeT += (_targetActive - _activeT) * 0.15f; changed = true; }
            if (changed) Invalidate();
            else { _hoverT = _targetHover; _activeT = _targetActive; _animTimer.Stop(); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int size = Math.Min(Width, Height) - 8;
            int x = (Width - size) / 2;
            int y = (Height - size) / 2;
            var rect = new Rectangle(x, y, size, size);

            if (_activeT > 0.01f)
            {
                using (var path = GraphicsExt.RoundedRect(rect, 12))
                {
                    var bg = Anim.LerpColor(Theme.Surface, Theme.AccentDarker, _activeT * 0.5f);
                    using (var brush = new SolidBrush(bg))
                        g.FillPath(brush, path);
                }
            }
            else if (_hoverT > 0.01f)
            {
                using (var path = GraphicsExt.RoundedRect(rect, 12))
                {
                    var bg = Anim.LerpColor(Theme.Surface, Theme.SurfaceLight, _hoverT);
                    using (var brush = new SolidBrush(bg))
                        g.FillPath(brush, path);
                }
            }

            if (_activeT > 0.01f)
            {
                int barH = (int)(size * 0.3f * _activeT);
                int barY = (Height - barH) / 2;
                using (var barBrush = new SolidBrush(Theme.Accent))
                    g.FillRectangle(barBrush, 0, barY, 3, barH);
            }

            var iconColor = Anim.LerpColor(Theme.TextSecondary, Theme.Accent, _activeT);
            iconColor = Anim.LerpColor(iconColor, Theme.TextPrimary, _hoverT * 0.4f);
            TextRenderer.DrawText(g, Icon, new Font("Segoe UI", 15f), ClientRectangle, iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _animTimer?.Stop(); _animTimer?.Dispose(); _tooltip?.Dispose(); }
            base.Dispose(disposing);
        }
    }

    public class ModuleCard : Control
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Color IconColor { get; set; } = Theme.Accent;
        public int Category { get; set; }

        private float _hoverT;
        private float _targetHover;
        private Timer _animTimer;

        public ModuleCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor | ControlStyles.Opaque, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.Transparent;
            Size = new Size(220, 110);
            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += Animate;
            Theme.ThemeChanged += () => Invalidate();
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
            StartAnim();
        }

        private void StartAnim() { if (!_animTimer.Enabled) _animTimer.Start(); }

        private void Animate(object sender, EventArgs e)
        {
            if (Math.Abs(_hoverT - _targetHover) > 0.003f) { _hoverT += (_targetHover - _hoverT) * 0.18f; Invalidate(); }
            else { _hoverT = _targetHover; _animTimer.Stop(); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int lift = (int)(_hoverT * 2);
            var rect = new Rectangle(0, -lift, Width - 1, Height - 1 + lift);

            if (_hoverT > 0.01f)
                GraphicsExt.DrawShadow(g, rect, 12, (int)(8 * _hoverT), 60);

            var cardColor = Anim.LerpColor(Theme.Surface, Theme.SurfaceLight, _hoverT * 0.6f);
            using (var path = GraphicsExt.RoundedRect(rect, 12))
            using (var brush = new SolidBrush(cardColor))
                g.FillPath(brush, path);

            var borderColor = Anim.LerpColor(Theme.Border, Theme.Accent, _hoverT * 0.5f);
            using (var path2 = GraphicsExt.RoundedRect(rect, 12))
            using (var pen = new Pen(borderColor, 1))
                g.DrawPath(pen, path2);

            var iconRect = new Rectangle(16, -lift + 14, 44, 44);
            using (var path = GraphicsExt.RoundedRect(iconRect, 10))
            using (var brush = new SolidBrush(IconColor))
                g.FillPath(brush, path);
            TextRenderer.DrawText(g, Icon, new Font("Segoe UI", 16f), iconRect,
                CalcTextColor(IconColor), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            TextRenderer.DrawText(g, Title, new Font("Segoe UI", 11f, FontStyle.Bold),
                new Rectangle(16, -lift + 62, Width - 32, 22), Theme.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(g, Description, new Font("Segoe UI", 8.5f),
                new Rectangle(16, -lift + 84, Width - 32, 22), Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.WordEllipsis);

            if (_hoverT > 0.3f)
            {
                int arrowAlpha = (int)(200 * _hoverT);
                TextRenderer.DrawText(g, "›", new Font("Segoe UI", 14f, FontStyle.Bold),
                    new Rectangle(Width - 28, -lift + (Height - 24) / 2, 18, 24),
                    Color.FromArgb(arrowAlpha, Theme.Accent),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private Color CalcTextColor(Color bg)
        {
            double brightness = (bg.R * 0.299 + bg.G * 0.587 + bg.B * 0.114) / 255.0;
            return brightness > 0.55 ? Color.FromArgb(10, 14, 16) : Color.White;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _animTimer?.Stop(); _animTimer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
