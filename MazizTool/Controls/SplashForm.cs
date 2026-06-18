using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MazizTool.Controls
{
    public class SplashForm : Form
    {
        private Timer _timer;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            Size = new Size(420, 280);
            BackColor = Color.FromArgb(14, 22, 28);
            DoubleBuffered = true;
            Opacity = 0;

            Paint += OnPaint;
            Load += (s, e) => FadeIn();
        }

        private void FadeIn()
        {
            var fadeTimer = new Timer { Interval = 16 };
            float opacity = 0;
            fadeTimer.Tick += (s, e) =>
            {
                opacity += 0.08f;
                if (opacity >= 1f) { opacity = 1f; fadeTimer.Stop(); FadeOut(); }
                Opacity = opacity;
            };
            fadeTimer.Start();
        }

        private void FadeOut()
        {
            _timer = new Timer { Interval = 2000 };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                var fadeTimer = new Timer { Interval = 16 };
                float opacity = 1f;
                fadeTimer.Tick += (s, e) =>
                {
                    opacity -= 0.08f;
                    if (opacity <= 0f) { opacity = 0f; Opacity = 0; fadeTimer.Stop(); Close(); }
                    Opacity = opacity;
                };
                fadeTimer.Start();
            };
            _timer.Start();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var brush = new SolidBrush(Color.FromArgb(14, 22, 28)))
                g.FillRectangle(brush, rect);

            var logoRect = new Rectangle(Width / 2 - 35, 50, 70, 70);
            var path = GraphicsExt.RoundedRect(logoRect, 18);
            using (var brush = new LinearGradientBrush(logoRect, Theme.Accent, Theme.AccentDark, 90f))
                g.FillPath(brush, path);
            TextRenderer.DrawText(g, "MZ", new Font("Segoe UI", 26f, FontStyle.Bold), logoRect,
                Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            path.Dispose();

            TextRenderer.DrawText(g, "MazizTool", new Font("Segoe UI", 24f, FontStyle.Bold),
                new Rectangle(0, 130, Width, 36), Theme.Accent, TextFormatFlags.HorizontalCenter);

            TextRenderer.DrawText(g, "System Recovery & Anti-Malware Hub",
                new Font("Segoe UI", 11f), new Rectangle(0, 166, Width, 28),
                Theme.TextPrimary, TextFormatFlags.HorizontalCenter);

            TextRenderer.DrawText(g, "v6.5",
                new Font("Segoe UI", 9f), new Rectangle(0, 200, Width, 20),
                Theme.TextMuted, TextFormatFlags.HorizontalCenter);

            Dot(196, g);
            Dot(210, g);
            Dot(224, g);
        }

        private void Dot(int x, Graphics g)
        {
            for (int i = 0; i < 3; i++)
            {
                int alpha = i == 2 ? 255 : 120;
                using (var brush = new SolidBrush(Color.FromArgb(alpha, Theme.Accent)))
                    g.FillEllipse(brush, x + i * 14, 234 + (i == 1 ? -2 : 0), 6, 6);
            }
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
