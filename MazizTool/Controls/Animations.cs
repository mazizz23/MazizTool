using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MazizTool.Controls
{
    public static class Anim
    {
        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseInOut(float t) => t < 0.5f ? 2 * t * t : 1f - (float)Math.Pow(-2 * t + 2, 2) / 2f;
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1;
            return 1 + c3 * (float)Math.Pow(t - 1, 3) + c1 * (float)Math.Pow(t - 1, 2);
        }
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        public static Color LerpColor(Color a, Color b, float t)
        {
            if (t <= 0) return a;
            if (t >= 1) return b;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }

    public class AnimTimer
    {
        private Timer _timer;
        private int _frame;
        private int _totalFrames;
        private Action<float> _onUpdate;
        private Action _onComplete;

        public void Start(int durationMs, Action<float> onUpdate, Action onComplete = null)
        {
            Stop();
            _onUpdate = onUpdate;
            _onComplete = onComplete;
            _frame = 0;
            const int fps = 60;
            _totalFrames = Math.Max(1, durationMs * fps / 1000);
            _timer = new Timer { Interval = 1000 / fps };
            _timer.Tick += (s, e) =>
            {
                _frame++;
                float t = (float)_frame / _totalFrames;
                if (t >= 1f) { Stop(); _onUpdate?.Invoke(1f); _onComplete?.Invoke(); return; }
                _onUpdate?.Invoke(t);
            };
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer != null) { _timer.Stop(); _timer.Dispose(); _timer = null; }
        }
    }

    public static class GraphicsExt
    {
        public static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(rect); return path; }
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
            radius = d / 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath RoundedRect(RectangleF rect, int radius)
        {
            return RoundedRect(Rectangle.Round(rect), radius);
        }

        public static void FillRoundedRect(Graphics g, Rectangle rect, int radius, Color color)
        {
            using (var path = RoundedRect(rect, radius))
            using (var brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }

        public static void DrawRoundedRect(Graphics g, Rectangle rect, int radius, Color color, float width = 1f)
        {
            using (var path = RoundedRect(rect, radius))
            using (var pen = new Pen(color, width))
                g.DrawPath(pen, path);
        }

        public static void DrawGradientBg(Graphics g, Rectangle rect, Color top, Color bottom)
        {
            using (var brush = new LinearGradientBrush(
                new Point(rect.X, rect.Y),
                new Point(rect.X, rect.Bottom),
                top, bottom))
            {
                g.FillRectangle(brush, rect);
            }
        }

        public static void DrawShadow(Graphics g, Rectangle rect, int radius, int depth = 6, int alpha = 60)
        {
            for (int i = depth; i > 0; i--)
            {
                int a = (int)(alpha * (1f - (float)i / depth) * 0.4f);
                using (var pen = new Pen(Color.FromArgb(a, 0, 0, 0), 1))
                {
                    var r = Rectangle.Inflate(rect, i, i);
                    using (var path = RoundedRect(r, radius + i))
                        g.DrawPath(pen, path);
                }
            }
        }

        public static Color Lighten(Color c, int amount) => Color.FromArgb(
            Math.Min(255, c.R + amount), Math.Min(255, c.G + amount), Math.Min(255, c.B + amount));

        public static Color Darken(Color c, int amount) => Color.FromArgb(
            Math.Max(0, c.R - amount), Math.Max(0, c.G - amount), Math.Max(0, c.B - amount));
    }
}
