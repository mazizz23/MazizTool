using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using MazizTool.Native;

namespace MazizTool.Features
{
    public static class SystemTools
    {
        public static bool RestoreExeAssociations()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c assoc .exe=exefile && ftype exefile=\"%1\" %*",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }).WaitForExit(3000);

                using (var key = Registry.ClassesRoot.OpenSubKey(@"exefile\shell\open\command", true))
                {
                    if (key != null)
                    {
                        key.SetValue("", "\"%1\" %*");
                        key.SetValue("IsolatedCommand", "\"%1\" %*");
                    }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c assoc .com=comfile && assoc .scr=scrfile && assoc .bat=batfile && assoc .cmd=cmdfile && assoc .vbs=VBSFile && assoc .reg=regfile",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }).WaitForExit(3000);

                return true;
            }
            catch { return false; }
        }

        public static bool FixGroupPolicies()
        {
            try
            {
                string[] policyKeys = new[]
                {
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System",
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer",
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\ActiveDesktop",
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Attachments",
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Comdlg32",
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\NonEnum",
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\Uninstall",
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\WindowsUpdate",
                    @"Software\Policies\Microsoft\Windows\System",
                };

                foreach (var policyKey in policyKeys)
                {
                    try
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(policyKey))
                        {
                            if (key != null)
                            {
                                Registry.CurrentUser.DeleteSubKeyTree(policyKey, false);
                            }
                        }
                    }
                    catch { }

                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(policyKey))
                        {
                            if (key != null)
                            {
                                Registry.LocalMachine.DeleteSubKeyTree(policyKey, false);
                            }
                        }
                    }
                    catch { }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "gpupdate.exe",
                    Arguments = "/force",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                })?.WaitForExit(5000);

                return true;
            }
            catch { return false; }
        }

        public static bool ResetNetworkSettings()
        {
            try
            {
                var commands = new[]
                {
                    "ipconfig /release",
                    "ipconfig /renew",
                    "ipconfig /flushdns",
                    "netsh winsock reset",
                    "netsh int ip reset",
                    "netsh winhttp reset proxy",
                    "netsh advfirewall reset",
                };

                foreach (var cmd in commands)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {cmd}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    })?.WaitForExit(5000);
                }
                return true;
            }
            catch { return false; }
        }

        public static bool RestoreHiddenFiles()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Hidden", 1, RegistryValueKind.DWord);
                        key.SetValue("ShowSuperHidden", 1, RegistryValueKind.DWord);
                        key.SetValue("HideFileExt", 0, RegistryValueKind.DWord);
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public static bool ClearTempFiles()
        {
            try
            {
                var tempPaths = new[]
                {
                    Path.GetTempPath(),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), "Content.IE5"),
                };

                foreach (var path in tempPaths)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            foreach (var file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }
                    }
                    catch { }
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cleanmgr.exe",
                    Arguments = "/sagerun:1",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                return true;
            }
            catch { return false; }
        }

        public static bool CreateRestorePoint(string description)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wmic.exe",
                    Arguments = $"/Namespace:\\\\root\\default Path SystemRestore Call CreateRestorePoint \"{description}\", 100, 7",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi)?.WaitForExit(10000);
                return true;
            }
            catch { return false; }
        }

        public static bool RestoreSystemFiles()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sfc.exe",
                    Arguments = "/scannow",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Normal
                };
                Process.Start(psi);
                return true;
            }
            catch { return false; }
        }

        public static bool FixHostsFile()
        {
            try
            {
                string hostsPath = Path.Combine(Environment.SystemDirectory, @"drivers\etc\hosts");
                string defaultHosts = @"# Copyright (c) 1993-2009 Microsoft Corp.
#
# This is a sample HOSTS file used by Microsoft TCP/IP for Windows.
#
# This file contains the mappings of IP addresses to host names. Each
# entry should be kept on an individual line. The IP address should
# be placed in the first column followed by the corresponding host name.
# The IP address and the host name should be separated by at least one
# space.
#
# Additionally, comments (such as these) may be inserted on individual
# lines or following the machine name denoted by a '#' symbol.
#
# For example:
#
#      102.54.94.97     rhino.acme.com          # source server
#       38.25.63.10     x.acme.com              # x client host

# localhost name resolution is handled within DNS itself.
#	127.0.0.1       localhost
#	::1             localhost
";
                var attr = File.GetAttributes(hostsPath);
                File.SetAttributes(hostsPath, attr & ~FileAttributes.ReadOnly);
                File.WriteAllText(hostsPath, defaultHosts);
                File.SetAttributes(hostsPath, attr);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "ipconfig.exe",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit(3000);

                return true;
            }
            catch { return false; }
        }

        public static bool EnableSafeModeNetworking()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "bcdedit.exe",
                    Arguments = "/set {default} safeboot network",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public static bool DisableSafeMode()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "bcdedit.exe",
                    Arguments = "/deletevalue {default} safeboot",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public static bool CheckDisk()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "chkdsk.exe",
                    Arguments = "C: /f /r",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Normal
                };
                Process.Start(psi);
                return true;
            }
            catch { return false; }
        }

        public static string GetSystemInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"OS: {Environment.OSVersion}");
            info.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
            info.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
            info.AppendLine($"Machine: {Environment.MachineName}");
            info.AppendLine($"User: {Environment.UserName}");
            info.AppendLine($"Domain: {Environment.UserDomainName}");
            info.AppendLine($"Processors: {Environment.ProcessorCount}");
            info.AppendLine($"CLR: {Environment.Version}");
            info.AppendLine($"System Dir: {Environment.SystemDirectory}");
            info.AppendLine($"Working Set: {Environment.WorkingSet / 1024 / 1024} MB");

            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                info.AppendLine($"Drive {drive.Name}: {drive.AvailableFreeSpace / 1024 / 1024 / 1024} GB free / {drive.TotalSize / 1024 / 1024 / 1024} GB total [{drive.DriveFormat}]");
            }

            return info.ToString();
        }

        public static bool RebuildIconCache()
        {
            try
            {
                var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
                if (Directory.Exists(cachePath))
                {
                    foreach (var f in Directory.GetFiles(cachePath, "iconcache*"))
                        try { File.Delete(f); } catch { }
                    foreach (var f in Directory.GetFiles(cachePath, "thumbcache*"))
                        try { File.Delete(f); } catch { }
                }
                Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = "/c ie4uinit.exe -show", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public static bool RepairWMI()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "winmgmt.exe", Arguments = "/salvagerepository", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(10000);
                Process.Start(new ProcessStartInfo { FileName = "winmgmt.exe", Arguments = "/resetrepository", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(10000);
                return true;
            }
            catch { return false; }
        }

        public static bool ResetFirewall()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "netsh.exe", Arguments = "advfirewall reset", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000);
                return true;
            }
            catch { return false; }
        }

        public static bool FixCOMRegistration()
        {
            try
            {
                var cmds = new[] { "regsvr32 /s shell32.dll", "regsvr32 /s ole32.dll", "regsvr32 /s actxprxy.dll" };
                foreach (var c in cmds)
                    Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = $"/c {c}", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000);
                return true;
            }
            catch { return false; }
        }

        public static bool RebuildFontCache()
        {
            try
            {
                var psi = new ProcessStartInfo { FileName = "net.exe", Arguments = "stop FontCache", UseShellExecute = false, CreateNoWindow = true };
                Process.Start(psi)?.WaitForExit(5000);
                var cacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"FontCache-S-1-5-21.dat");
                try { File.Delete(cacheFile); } catch { }
                psi = new ProcessStartInfo { FileName = "net.exe", Arguments = "start FontCache", UseShellExecute = false, CreateNoWindow = true };
                Process.Start(psi)?.WaitForExit(5000);
                return true;
            }
            catch { return false; }
        }

        public static bool FixPrintSpooler()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "net.exe", Arguments = "stop spooler", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000);
                var spoolDir = Path.Combine(Environment.SystemDirectory, @"spool\PRINTERS");
                if (Directory.Exists(spoolDir))
                    foreach (var f in Directory.GetFiles(spoolDir)) try { File.Delete(f); } catch { }
                Process.Start(new ProcessStartInfo { FileName = "net.exe", Arguments = "start spooler", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000);
                return true;
            }
            catch { return false; }
        }

        public static bool FixTaskManager()
        {
            try
            {
                var psi = new ProcessStartInfo { FileName = "reg.exe", Arguments = @"add HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\System /v DisableTaskMgr /t REG_DWORD /d 0 /f", UseShellExecute = false, CreateNoWindow = true };
                Process.Start(psi)?.WaitForExit(3000);
                psi = new ProcessStartInfo { FileName = "reg.exe", Arguments = @"add HKLM\Software\Microsoft\Windows\CurrentVersion\Policies\System /v DisableTaskMgr /t REG_DWORD /d 0 /f", UseShellExecute = false, CreateNoWindow = true };
                Process.Start(psi)?.WaitForExit(3000);
                Win32.SystemParametersInfo(Win32.SPI_SETDISABLETASKMGR, 0, IntPtr.Zero, Win32.SPIF_UPDATEINIFILE | Win32.SPIF_SENDCHANGE);
                return true;
            }
            catch { return false; }
        }

        public static bool RestoreExplorer()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("explorer")) try { p.Kill(); } catch { }
                System.Threading.Thread.Sleep(1000);
                Process.Start("explorer.exe");
                return true;
            }
            catch { return false; }
        }
    }
}
