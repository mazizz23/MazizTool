using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using MazizTool.Native;

namespace MazizTool.Features
{
    public static class ProcessKiller
    {
        public class ProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public long MemoryBytes { get; set; }
            public string User { get; set; }
            public DateTime StartTime { get; set; }
            public int Threads { get; set; }
        }

        public static List<ProcessInfo> GetAllProcesses()
        {
            var list = new List<ProcessInfo>();
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    list.Add(new ProcessInfo
                    {
                        Id = proc.Id,
                        Name = proc.ProcessName,
                        Path = proc.MainModule?.FileName ?? "",
                        MemoryBytes = proc.WorkingSet64,
                        Threads = proc.Threads.Count,
                        StartTime = proc.StartTime
                    });
                }
                catch
                {
                    list.Add(new ProcessInfo
                    {
                        Id = proc.Id,
                        Name = proc.ProcessName,
                        Path = "",
                        MemoryBytes = 0,
                        Threads = 1
                    });
                }
            }
            return list.OrderBy(p => p.Name).ToList();
        }

        public static string GetProcessUser(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                return GetProcessOwner(processId);
            }
            catch { return "N/A"; }
        }

        private static string GetProcessOwner(int processId)
        {
            try
            {
                IntPtr processHandle = IntPtr.Zero;
                try
                {
                    processHandle = Win32.OpenProcess(0x0400 | 0x0010, false, processId);
                    if (processHandle == IntPtr.Zero) return "N/A";
                    IntPtr tokenHandle;
                    if (!Win32.OpenProcessToken(processHandle, 8, out tokenHandle))
                        return "N/A";
                    Win32.CloseHandle(tokenHandle);
                }
                finally
                {
                    if (processHandle != IntPtr.Zero)
                        Win32.CloseHandle(processHandle);
                }
            }
            catch { }
            return "SYSTEM";
        }

        public static bool KillProcess(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                proc.Kill();
                proc.WaitForExit(3000);
                return true;
            }
            catch { }

            try
            {
                IntPtr hProcess = Win32.OpenProcess(
                    Win32.PROCESS_TERMINATE | Win32.PROCESS_QUERY_INFORMATION,
                    false, processId);
                if (hProcess != IntPtr.Zero)
                {
                    bool result = Win32.TerminateProcess(hProcess, 1);
                    Win32.CloseHandle(hProcess);
                    return result;
                }
            }
            catch { }

            return false;
        }

        public static bool KillProcessByName(string processName)
        {
            bool anyKilled = false;
            var processes = Process.GetProcessesByName(processName);
            foreach (var proc in processes)
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                    anyKilled = true;
                }
                catch
                {
                    try
                    {
                        IntPtr hProcess = Win32.OpenProcess(Win32.PROCESS_TERMINATE, false, proc.Id);
                        if (hProcess != IntPtr.Zero)
                        {
                            Win32.TerminateProcess(hProcess, 1);
                            Win32.CloseHandle(hProcess);
                            anyKilled = true;
                        }
                    }
                    catch { }
                }
            }
            return anyKilled;
        }

        public static bool KillProcessTree(int processId)
        {
            try
            {
                var childProcesses = GetChildProcesses(processId);
                foreach (var child in childProcesses)
                {
                    KillProcessTree(child);
                }
                KillProcess(processId);
                return true;
            }
            catch { return false; }
        }

        private static List<int> GetChildProcesses(int parentId)
        {
            var children = new List<int>();
            try
            {
                var searcher = new ManagementObjectSearcher(
                    $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {parentId}");
                foreach (var obj in searcher.Get())
                {
                    children.Add(Convert.ToInt32(obj["ProcessId"]));
                }
            }
            catch { }
            return children;
        }

        public static bool KillNonSystemProcesses()
        {
            var sysProcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "system", "system idle process", "registry", "smss", "csrss",
                "wininit", "winlogon", "services", "lsass", "svchost",
                "dwm", "explorer", "taskhostw", "runtimebroker", "shellexperiencehost",
                "searchindexer", "securityhealthsystray", "sihost", "ctfmon",
                "spoolsv", "fontdrvhost", "wlms", "msmpeng", "nis", "mpcmdrun",
                "securityhealthservice", "wudfhost", "dashost"
            };

            int killed = 0;
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    if (sysProcs.Contains(proc.ProcessName.ToLower())) continue;
                    if (proc.Id <= 4) continue;

                    proc.Kill();
                    killed++;
                }
                catch { }
            }
            return killed > 0;
        }

        public static bool ResumeProcess(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                foreach (ProcessThread thread in proc.Threads)
                {
                    IntPtr hThread = Win32.OpenProcess(0x0002, false, thread.Id);
                    if (hThread != IntPtr.Zero) Win32.CloseHandle(hThread);
                }
                return true;
            }
            catch { return false; }
        }

        public static bool SuspendProcess(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                foreach (ProcessThread thread in proc.Threads)
                {
                    IntPtr hThread = Win32.OpenProcess(0x0002, false, thread.Id);
                    if (hThread != IntPtr.Zero) Win32.CloseHandle(hThread);
                }
                return true;
            }
            catch { return false; }
        }
    }

    public class StartupManager
    {
        public class StartupEntry
        {
            public string Name { get; set; }
            public string Command { get; set; }
            public string Location { get; set; }
            public bool Enabled { get; set; }
            public string Publisher { get; set; }
        }

        public static List<StartupEntry> GetStartupEntries()
        {
            var entries = new List<StartupEntry>();
            entries.AddRange(GetRegistryStartups(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU\\Run"));
            entries.AddRange(GetRegistryStartups(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU\\RunOnce"));
            entries.AddRange(GetRegistryStartups(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKLM\\Run"));
            entries.AddRange(GetRegistryStartups(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM\\RunOnce"));
            entries.AddRange(GetRegistryStartups(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM\\WOW6432\\Run"));
            entries.AddRange(GetStartupFolderEntries());
            entries.AddRange(GetScheduledTasks());
            return entries;
        }

        private static List<StartupEntry> GetRegistryStartups(RegistryKey hive, string path, string location)
        {
            var entries = new List<StartupEntry>();
            try
            {
                using (var key = hive.OpenSubKey(path))
                {
                    if (key == null) return entries;
                    foreach (var name in key.GetValueNames())
                    {
                        var value = key.GetValue(name)?.ToString() ?? "";
                        entries.Add(new StartupEntry
                        {
                            Name = name,
                            Command = value,
                            Location = location,
                            Enabled = true
                        });
                    }
                }
            }
            catch { }
            return entries;
        }

        private static List<StartupEntry> GetStartupFolderEntries()
        {
            var entries = new List<StartupEntry>();
            try
            {
                var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                var startupPath = Path.Combine(startMenu, "Programs", "Startup");
                if (Directory.Exists(startupPath))
                {
                    foreach (var file in Directory.GetFiles(startupPath, "*.lnk"))
                    {
                        entries.Add(new StartupEntry
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Command = file,
                            Location = "Startup Folder",
                            Enabled = true
                        });
                    }
                }
                var commonStartup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", "Startup");
                if (Directory.Exists(commonStartup))
                {
                    foreach (var file in Directory.GetFiles(commonStartup, "*.lnk"))
                    {
                        entries.Add(new StartupEntry
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Command = file,
                            Location = "Common Startup Folder",
                            Enabled = true
                        });
                    }
                }
            }
            catch { }
            return entries;
        }

        private static List<StartupEntry> GetScheduledTasks()
        {
            var entries = new List<StartupEntry>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/query /fo CSV /v",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();
                    var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(',');
                        if (parts.Length > 2)
                        {
                            entries.Add(new StartupEntry
                            {
                                Name = parts[0].Trim('"'),
                                Command = parts.Length > 8 ? parts[8].Trim('"') : "",
                                Location = "Scheduled Task",
                                Enabled = parts.Length > 6 && parts[6].Trim('"') == "Ready"
                            });
                        }
                    }
                }
            }
            catch { }
            return entries;
        }

        public static bool RemoveStartupEntry(StartupEntry entry)
        {
            try
            {
                if (entry.Location.StartsWith("HKCU"))
                {
                    var path = entry.Location.Replace("HKCU\\", "").Replace("\\", "\\");
                    path = path.Replace("Run", @"Software\Microsoft\Windows\CurrentVersion\Run");
                    path = path.Replace("RunOnce", @"Software\Microsoft\Windows\CurrentVersion\RunOnce");
                    using (var key = Registry.CurrentUser.OpenSubKey(path, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(entry.Name, false);
                            return true;
                        }
                    }
                }
                else if (entry.Location.StartsWith("HKLM"))
                {
                    var path = entry.Location.Replace("HKLM\\", "");
                    path = path.Replace("Run", @"Software\Microsoft\Windows\CurrentVersion\Run");
                    path = path.Replace("RunOnce", @"Software\Microsoft\Windows\CurrentVersion\RunOnce");
                    path = path.Replace("WOW6432", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion");
                    using (var key = Registry.LocalMachine.OpenSubKey(path, true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue(entry.Name, false);
                            return true;
                        }
                    }
                }
                else if (entry.Location.Contains("Startup Folder") && File.Exists(entry.Command))
                {
                    File.Delete(entry.Command);
                    return true;
                }
                else if (entry.Location == "Scheduled Task")
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/delete /tn \"{entry.Name}\" /f",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    var proc = Process.Start(psi);
                    proc.WaitForExit();
                    return proc.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }
    }
}
