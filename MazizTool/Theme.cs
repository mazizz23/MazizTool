using System.Drawing;

namespace MazizTool
{
    public static class Theme
    {
        public static Color Background = Color.FromArgb(8, 12, 8);
        public static Color Surface = Color.FromArgb(14, 20, 14);
        public static Color SurfaceLight = Color.FromArgb(22, 32, 22);
        public static Color Accent = Color.FromArgb(0, 255, 65);
        public static Color AccentHover = Color.FromArgb(51, 255, 102);
        public static Color AccentDim = Color.FromArgb(0, 170, 43);
        public static Color AccentDark = Color.FromArgb(0, 110, 28);
        public static Color Danger = Color.FromArgb(255, 45, 57);
        public static Color Success = Color.FromArgb(0, 255, 65);
        public static Color Warning = Color.FromArgb(255, 176, 0);
        public static Color Info = Color.FromArgb(0, 200, 255);
        public static Color TextPrimary = Color.FromArgb(200, 255, 200);
        public static Color TextSecondary = Color.FromArgb(120, 180, 120);
        public static Color TextMuted = Color.FromArgb(80, 120, 80);
        public static Color Border = Color.FromArgb(0, 80, 25);
        public static Color BorderLight = Color.FromArgb(0, 120, 40);
        public static Color InputBg = Color.FromArgb(10, 16, 10);
        public static Color ScrollbarTrack = Color.FromArgb(8, 12, 8);
        public static Color ScrollbarThumb = Color.FromArgb(0, 80, 25);
        public static Color ScrollbarThumbHover = Color.FromArgb(0, 140, 45);
        public static Color Glow = Color.FromArgb(0, 255, 65, 40);

        public static Font UIFont = new Font("Consolas", 9f, FontStyle.Regular);
        public static Font UIFontBold = new Font("Consolas", 9f, FontStyle.Bold);
        public static Font TitleFont = new Font("Consolas", 18f, FontStyle.Bold);
        public static Font HeaderFont = new Font("Consolas", 13f, FontStyle.Bold);
        public static Font MonoFont = new Font("Consolas", 9f, FontStyle.Regular);
        public static Font LogoFont = new Font("Consolas", 22f, FontStyle.Bold);
    }
}
