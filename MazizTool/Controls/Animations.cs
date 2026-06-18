using System;
using System.Collections.Generic;
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
        public static int Lerp(int a, int b, float t) => (int)(a + (b - a) * t);

        public static Color LerpColor(Color a, Color b, float t)
        {
            return Color.FromArgb(
                Lerp(a.A, b.A, t),
                Lerp(a.R, b.R, t),
                Lerp(a.G, b.G, t),
                Lerp(a.B, b.B, t));
        }
    }

    public class AnimTimer
    {
        private Timer _timer;
        private int _frame;
        private int _totalFrames;
        private Action<float> _onUpdate;
        private Action _onComplete;
        public bool IsRunning => _timer?.Enabled == true;

        public AnimTimer() { }

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
                if (t >= 1f) { t = 1f; Stop(); _onUpdate?.Invoke(t); _onComplete?.Invoke(); return; }
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
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawShadow(Graphics g, Rectangle rect, int radius, int depth = 8, Color? color = null)
        {
            var c = color ?? Color.FromArgb(0, 0, 0, 70);
            for (int i = depth; i > 0; i--)
            {
                int alpha = (int)(c.A * (1f - (float)i / depth) * 0.35f);
                using (var pen = new Pen(Color.FromArgb(alpha, 0, 0, 0), 1))
                {
                    var r = Rectangle.Inflate(rect, i, i);
                    using (var path = RoundedRect(r, radius + i))
                        g.DrawPath(pen, path);
                }
            }
        }
    }
}
