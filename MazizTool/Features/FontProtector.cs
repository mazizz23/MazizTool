using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MazizTool.Native;

namespace MazizTool.Features
{
    public static class FontProtector
    {
        private const int FR_PRIVATE = 0x10;
        private const int WM_FONTCHANGE = 0x001D;

        [DllImport("gdi32.dll")]
        private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

        [DllImport("gdi32.dll")]
        private static extern bool RemoveFontResourceEx(string lpFileName, uint fl, IntPtr pdv);

        public static bool RestoreSystemFonts()
        {
            try
            {
                RestoreRegistryFontSettings();
                RestoreSegoeUIFont();
                RestoreFontSmoothing();
                Win32.SendMessage(Win32.HWND_BROADCAST, WM_FONTCHANGE, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch { return false; }
        }

        private static void RestoreRegistryFontSettings()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Segoe UI (TrueType)", "segoeui.ttf");
                        key.SetValue("Segoe UI Bold (TrueType)", "segoeuib.ttf");
                        key.SetValue("Segoe UI Italic (TrueType)", "segoeuii.ttf");
                        key.SetValue("Segoe UI Bold Italic (TrueType)", "segoeuiz.ttf");
                        key.SetValue("Segoe UI Black (TrueType)", "seguibl.ttf");
                        key.SetValue("Segoe UI Light (TrueType)", "segoeuil.ttf");
                        key.SetValue("Segoe UI Semibold (TrueType)", "seguisb.ttf");
                        key.SetValue("Segoe UI Semilight (TrueType)", "segoeuisl.ttf");
                        key.SetValue("Segoe UI Symbol (TrueType)", "seguisym.ttf");
                        key.SetValue("Consolas (TrueType)", "consola.ttf");
                    }
                }
            }
            catch { }
        }

        private static void RestoreSegoeUIFont()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("Segoe UI", false); } catch { }
                        try { key.DeleteValue("MS Shell Dlg", false); } catch { }
                        try { key.DeleteValue("MS Shell Dlg 2", false); } catch { }
                        key.SetValue("MS Shell Dlg", "Microsoft Sans Serif");
                        key.SetValue("MS Shell Dlg 2", "Tahoma");
                    }
                }
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\FontSubstitutes", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("Segoe UI", false); } catch { }
                    }
                }
            }
            catch { }
        }

        private static void RestoreFontSmoothing()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        key.SetValue("FontSmoothing", "2");
                        key.SetValue("FontSmoothingType", 2, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        public static bool IsSystemFontTampered()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("Segoe UI");
                        if (val != null && !string.IsNullOrEmpty(val.ToString()))
                            return true;
                    }
                }
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\FontSubstitutes"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("Segoe UI");
                        if (val != null && !string.IsNullOrEmpty(val.ToString()))
                            return true;
                    }
                }
                return false;
            }
            catch { return true; }
        }
    }
}
