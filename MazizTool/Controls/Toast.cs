using System;
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
