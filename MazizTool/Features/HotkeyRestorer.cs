using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using MazizTool.Native;

namespace MazizTool.Features
{
    public static class HotkeyRestorer
    {
        public class HotkeyBlock
        {
            public string Name { get; set; }
            public string RegistryPath { get; set; }
            public string ValueName { get; set; }
            public int RestoreValue { get; set; }
        }

        public static List<HotkeyBlock> KnownBlocks = new List<HotkeyBlock>
        {
            new HotkeyBlock { Name = "Task Manager", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System", ValueName = "DisableTaskMgr", RestoreValue = 0 },
            new HotkeyBlock { Name = "Registry Editor", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System", ValueName = "DisableRegistryTools", RestoreValue = 0 },
            new HotkeyBlock { Name = "Command Prompt", RegistryPath = @"Software\Policies\Microsoft\Windows\System", ValueName = "DisableCMD", RestoreValue = 0 },
            new HotkeyBlock { Name = "Control Panel", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoControlPanel", RestoreValue = 0 },
            new HotkeyBlock { Name = "Folder Options", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoFolderOptions", RestoreValue = 0 },
            new HotkeyBlock { Name = "Run Dialog", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoRun", RestoreValue = 0 },
            new HotkeyBlock { Name = "Search", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoFind", RestoreValue = 0 },
            new HotkeyBlock { Name = "Shutdown Options", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoClose", RestoreValue = 0 },
            new HotkeyBlock { Name = "Logoff", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "StartMenuLogOff", RestoreValue = 0 },
            new HotkeyBlock { Name = "Taskbar Context Menu", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoTrayContextMenu", RestoreValue = 0 },
            new HotkeyBlock { Name = "Desktop Cleanup", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoDesktop", RestoreValue = 0 },
            new HotkeyBlock { Name = "Change Password", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System", ValueName = "DisableChangePassword", RestoreValue = 0 },
            new HotkeyBlock { Name = "Lock Workstation", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\System", ValueName = "DisableLockWorkstation", RestoreValue = 0 },
            new HotkeyBlock { Name = "Context Menu", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoViewContextMenu", RestoreValue = 0 },
            new HotkeyBlock { Name = "System Tray Icons", RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", ValueName = "NoTrayItemsDisplay", RestoreValue = 0 },
            new HotkeyBlock { Name = "USB Storage", RegistryPath = @"SYSTEM\CurrentControlSet\Services\USBSTOR", ValueName = "Start", RestoreValue = 3 },
            new HotkeyBlock { Name = "Safe Mode Restrictions", RegistryPath = @"SYSTEM\CurrentControlSet\Control\SafeBoot\Option", ValueName = "OptionValue", RestoreValue = 0 },
        };

        public static int FixAllHotkeyBlocks()
        {
            int fixedCount = 0;
            foreach (var block in KnownBlocks)
            {
                try
                {
                    if (block.RegistryPath.StartsWith("SYSTEM"))
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(block.RegistryPath, true))
                        {
                            if (key != null)
                            {
                                var val = key.GetValue(block.ValueName);
                                if (val != null)
                                {
                                    key.SetValue(block.ValueName, block.RestoreValue, RegistryValueKind.DWord);
                                    key.DeleteValue(block.ValueName, false);
                                }
                                fixedCount++;
                                key.Close();
                            }
                        }
                    }
                    else
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(block.RegistryPath, true))
                        {
                            if (key != null)
                            {
                                var val = key.GetValue(block.ValueName);
                                if (val != null && (int)val != block.RestoreValue)
                                {
                                    key.DeleteValue(block.ValueName, false);
                                    fixedCount++;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            return fixedCount;
        }

        public static bool RestoreTaskManager()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisableTaskMgr", false); } catch { }
                    }
                }
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisableTaskMgr", false); } catch { }
                    }
                }
                Win32.SystemParametersInfo(Win32.SPI_SETDISABLETASKMGR, 0, IntPtr.Zero, Win32.SPIF_UPDATEINIFILE | Win32.SPIF_SENDCHANGE);
                Win32.SystemParametersInfo(Win32.SPI_SETSCREENSAVERRUNNING, 0, IntPtr.Zero, 0);
                return true;
            }
            catch { return false; }
        }

        public static bool RestoreRegistryEditor()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisableRegistryTools", false); } catch { }
                    }
                }
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisableRegistryTools", false); } catch { }
                    }
                }
                Win32.SystemParametersInfo(Win32.SPI_SETDISABLEREGISTRYTOOLS, 0, IntPtr.Zero, Win32.SPIF_UPDATEINIFILE | Win32.SPIF_SENDCHANGE);
                return true;
            }
            catch { return false; }
        }

        public static bool RestoreHotkeys()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("NoWinKeys", false); } catch { }
                        try { key.DeleteValue("NoKeyHoover", false); } catch { }
                    }
                }
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("NoWinKeys", false); } catch { }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public static bool EnableAltTab()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer", true))
                {
                    if (key != null)
                    {
                        key.SetValue("AltTabSettings", 1, RegistryValueKind.DWord);
                    }
                }
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisablePreviewDesktop", false); } catch { }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public static bool EnableCtrlAltDel()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisableLockWorkstation", false); } catch { }
                        try { key.DeleteValue("DisableChangePassword", false); } catch { }
                    }
                }
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("DisableCAD", false); } catch { }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public static bool RestoreAll()
        {
            RestoreTaskManager();
            RestoreRegistryEditor();
            RestoreHotkeys();
            EnableAltTab();
            EnableCtrlAltDel();
            FixAllHotkeyBlocks();
            return true;
        }
    }
}
