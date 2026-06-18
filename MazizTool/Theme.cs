using System;
using System.Collections.Generic;
using System.Drawing;

namespace MazizTool
{
    public class ColorPreset
    {
        public string Name;
        public Color Accent;
        public Color AccentHover;
        public Color AccentDark;
        public Color AccentDarker;
    }

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

        public static List<ColorPreset> Presets = new List<ColorPreset>
        {
            new ColorPreset { Name = "Emerald", Accent = Color.FromArgb(45, 212, 191), AccentHover = Color.FromArgb(94, 234, 212), AccentDark = Color.FromArgb(13, 148, 136), AccentDarker = Color.FromArgb(9, 90, 80) },
            new ColorPreset { Name = "Purple", Accent = Color.FromArgb(168, 85, 247), AccentHover = Color.FromArgb(192, 132, 252), AccentDark = Color.FromArgb(126, 34, 206), AccentDarker = Color.FromArgb(88, 28, 135) },
            new ColorPreset { Name = "Blue", Accent = Color.FromArgb(59, 130, 246), AccentHover = Color.FromArgb(96, 165, 250), AccentDark = Color.FromArgb(37, 99, 235), AccentDarker = Color.FromArgb(30, 64, 175) },
            new ColorPreset { Name = "Rose", Accent = Color.FromArgb(244, 63, 94), AccentHover = Color.FromArgb(251, 113, 133), AccentDark = Color.FromArgb(225, 29, 72), AccentDarker = Color.FromArgb(159, 18, 57) },
            new ColorPreset { Name = "Orange", Accent = Color.FromArgb(249, 115, 22), AccentHover = Color.FromArgb(251, 146, 60), AccentDark = Color.FromArgb(234, 88, 12), AccentDarker = Color.FromArgb(154, 52, 18) },
            new ColorPreset { Name = "Pink", Accent = Color.FromArgb(236, 72, 153), AccentHover = Color.FromArgb(244, 114, 182), AccentDark = Color.FromArgb(219, 39, 119), AccentDarker = Color.FromArgb(157, 23, 77) },
            new ColorPreset { Name = "Cyan", Accent = Color.FromArgb(6, 182, 212), AccentHover = Color.FromArgb(34, 211, 238), AccentDark = Color.FromArgb(8, 145, 178), AccentDarker = Color.FromArgb(14, 116, 144) },
            new ColorPreset { Name = "Amber", Accent = Color.FromArgb(245, 158, 11), AccentHover = Color.FromArgb(251, 191, 36), AccentDark = Color.FromArgb(217, 119, 6), AccentDarker = Color.FromArgb(180, 83, 9) },
            new ColorPreset { Name = "Lime", Accent = Color.FromArgb(132, 204, 22), AccentHover = Color.FromArgb(163, 230, 53), AccentDark = Color.FromArgb(101, 163, 13), AccentDarker = Color.FromArgb(77, 124, 15) },
            new ColorPreset { Name = "Red", Accent = Color.FromArgb(239, 68, 68), AccentHover = Color.FromArgb(248, 113, 113), AccentDark = Color.FromArgb(220, 38, 38), AccentDarker = Color.FromArgb(153, 27, 27) },
        };

        private static int _currentPreset = 0;
        public static int CurrentPresetIndex
        {
            get => _currentPreset;
            set
            {
                _currentPreset = value;
                var p = Presets[value];
                Accent = p.Accent;
                AccentHover = p.AccentHover;
                AccentDark = p.AccentDark;
                AccentDarker = p.AccentDarker;
                ThemeChanged?.Invoke();
            }
        }

        public static void ApplyPreset(int index)
        {
            if (index < 0 || index >= Presets.Count) return;
            CurrentPresetIndex = index;
        }

        public static Color WithAlpha(Color c, int a) => Color.FromArgb(a, c);
    }
}
