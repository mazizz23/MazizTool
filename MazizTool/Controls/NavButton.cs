using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MazizTool.Controls
{
    public class NavButton : Control
    {
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
                     ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Opaque, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 9f);
            BackColor = Theme.Surface;
            Height = 36;
            _animTimer = new Timer { Interval = 16 };
            _animTimer.Tick += Animate;
            Theme.ThemeChanged += () => Invalidate();
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
            if (Math.Abs(_hoverT - _targetHover) > 0.003f) { _hoverT += (_targetHover - _hoverT) * 0.18f; changed = true; }
            if (Math.Abs(_activeT - _targetActive) > 0.003f) { _activeT += (_targetActive - _activeT) * 0.12f; changed = true; }
            if (changed) Invalidate();
            else { _hoverT = _targetHover; _activeT = _targetActive; _animTimer.Stop(); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(4, 3, Width - 8, Height - 6);

            var bg = Theme.Surface;
            if (_hoverT > 0.01f)
                bg = Anim.LerpColor(bg, Theme.SurfaceLight, _hoverT * 0.6f);
            if (_activeT > 0.01f)
                bg = Anim.LerpColor(bg, Theme.SurfaceLight, _activeT * 0.8f);

            using (var path = GraphicsExt.RoundedRect(rect, 8))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            if (_activeT > 0.01f)
            {
                int barH = (int)((Height - 16) * _activeT);
                int barY = (Height - barH) / 2;
                var barRect = new Rectangle(8, barY, 3, barH);
                using (var barPath = GraphicsExt.RoundedRect(barRect, 2))
                using (var barBrush = new SolidBrush(Theme.Accent))
                    g.FillPath(barBrush, barPath);
            }

            var iconColor = Anim.LerpColor(Theme.TextSecondary, Theme.Accent, _activeT);
            if (_hoverT > 0 && _activeT < 0.5f)
                iconColor = Anim.LerpColor(iconColor, Theme.TextPrimary, _hoverT * 0.5f);
            var iconRect = new Rectangle(20, 0, 24, Height);
            TextRenderer.DrawText(g, Icon, new Font("Segoe UI", 11f), iconRect, iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var textColor = Anim.LerpColor(Theme.TextSecondary, Theme.TextPrimary, Math.Max(_hoverT, _activeT));
            var textRect = new Rectangle(48, 0, Width - 54, Height);
            TextRenderer.DrawText(g, Label, Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _animTimer?.Stop(); _animTimer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
