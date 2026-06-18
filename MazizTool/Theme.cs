using System.Drawing;

namespace MazizTool
{
    public static class Theme
    {
        public static Color Background = Color.FromArgb(8, 18, 14);
        public static Color Surface = Color.FromArgb(14, 28, 22);
        public static Color SurfaceLight = Color.FromArgb(22, 42, 34);
        public static Color SurfaceElevated = Color.FromArgb(28, 54, 44);
        public static Color Accent = Color.FromArgb(45, 212, 191);
        public static Color AccentHover = Color.FromArgb(94, 234, 212);
        public static Color AccentDim = Color.FromArgb(20, 184, 166);
        public static Color AccentDark = Color.FromArgb(13, 148, 136);
        public static Color AccentDarker = Color.FromArgb(9, 90, 80);
        public static Color Emerald = Color.FromArgb(16, 185, 129);
        public static Color EmeraldLight = Color.FromArgb(52, 211, 153);
        public static Color EmeraldDark = Color.FromArgb(5, 150, 105);
        public static Color Danger = Color.FromArgb(244, 63, 94);
        public static Color DangerDim = Color.FromArgb(190, 24, 93);
        public static Color Warning = Color.FromArgb(245, 158, 11);
        public static Color Info = Color.FromArgb(6, 182, 212);
        public static Color TextPrimary = Color.FromArgb(230, 247, 244);
        public static Color TextSecondary = Color.FromArgb(148, 184, 168);
        public static Color TextMuted = Color.FromArgb(100, 130, 116);
        public static Color Border = Color.FromArgb(30, 58, 52);
        public static Color BorderLight = Color.FromArgb(48, 78, 72);
        public static Color InputBg = Color.FromArgb(10, 22, 18);
        public static Color Shadow = Color.FromArgb(0, 0, 0, 90);

        public static Font UIFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font UIFontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static Font TitleFont = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static Font HeaderFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static Font MonoFont = new Font("Cascadia Code", 9f, FontStyle.Regular);
        public static Font MonoBold = new Font("Cascadia Code", 9f, FontStyle.Bold);
        public static Font LogoFont = new Font("Segoe UI", 20f, FontStyle.Bold);
    }
}
