using System;
using System.Drawing;

namespace MazizTool
{
    public static class Theme
    {
        public static Color Background = Color.FromArgb(8, 12, 16);
        public static Color BackgroundTop = Color.FromArgb(12, 20, 26);
        public static Color Surface = Color.FromArgb(16, 22, 28);
        public static Color SurfaceLight = Color.FromArgb(22, 30, 38);
        public static Color SurfaceElevated = Color.FromArgb(28, 38, 48);

        public static Color Accent = Color.FromArgb(45, 212, 191);
        public static Color AccentHover = Color.FromArgb(94, 234, 212);
        public static Color AccentDark = Color.FromArgb(13, 148, 136);
        public static Color AccentDarker = Color.FromArgb(9, 90, 80);

        public static Color Danger = Color.FromArgb(244, 63, 94);
        public static Color Warning = Color.FromArgb(245, 158, 11);
        public static Color Info = Color.FromArgb(6, 182, 212);
        public static Color Success = Color.FromArgb(34, 197, 94);
        public static Color Emerald = Color.FromArgb(16, 185, 129);

        public static Color TextPrimary = Color.FromArgb(236, 244, 248);
        public static Color TextSecondary = Color.FromArgb(156, 178, 192);
        public static Color TextMuted = Color.FromArgb(96, 116, 130);
        public static Color Border = Color.FromArgb(28, 38, 48);
        public static Color BorderLight = Color.FromArgb(48, 64, 80);
        public static Color InputBg = Color.FromArgb(12, 18, 24);

        public static Font UIFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static Font UIFontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static Font TitleFont = new Font("Segoe UI", 18f, FontStyle.Bold);
        public static Font HeaderFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static Font MonoFont = new Font("Cascadia Code", 9f, FontStyle.Regular);
        public static Font LogoFont = new Font("Segoe UI", 20f, FontStyle.Bold);

        public static event Action ThemeChanged;

        public static Color WithAlpha(Color c, int a) => Color.FromArgb(a, c);
    }
}
