using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace MazizTool.Features
{
    public class RegistryScanner
    {
        public class RegFinding
        {
            public string Location { get; set; }
            public string Value { get; set; }
            public string Data { get; set; }
            public ThreatLevel Level { get; set; }
            public string Description { get; set; }
        }

        public enum ThreatLevel { Safe, Info, Suspicious, Malicious }

        public event Action<string> OnProgress;
        public event Action<RegFinding> OnFinding;
        public List<RegFinding> Findings { get; private set; } = new List<RegFinding>();

        public void Scan()
        {
            Findings.Clear();
            ScanRunKeys();
            ScanWinlogon();
            ScanIFEO();
            ScanAppInit();
            ScanExplorerPolicies();
            ScanBrowserHijacks();
            ScanHostsRedirects();
            ScanSafeMode();
            ScanDisabledTools();
            ScanKnownDLLs();
            ScanActiveSetup();
            ScanShellExtensions();
            OnProgress?.Invoke("[*] Registry scan complete. Findings: " + Findings.Count);
        }

        private void AddFinding(RegFinding f)
        {
            Findings.Add(f);
            OnFinding?.Invoke(f);
        }

        private void ScanRunKeys()
        {
            OnProgress?.Invoke("[*] Scanning autorun entries...");
            var runKeys = new[]
            {
                Tuple.Create(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU\\Run"),
                Tuple.Create(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU\\RunOnce"),
                Tuple.Create(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKLM\\Run"),
                Tuple.Create(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM\\RunOnce"),
                Tuple.Create(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM\\WOW64\\Run"),
                Tuple.Create(Registry.CurrentUser, @"Software\Microsoft\Windows NT\CurrentVersion\Windows", "HKCU\\Windows"),
                Tuple.Create(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "HKCU\\StartupApproved"),
            };

            var suspiciousPaths = new[] { "\\temp\\", "\\appdata\\local\\temp\\", "\\users\\public\\", "%temp%", "%appdata%" };
            var suspiciousExts = new[] { ".scr", ".pif", ".bat", ".vbs", ".js", ".ps1", ".cmd" };

            foreach (var tuple in runKeys)
            {
                try
                {
                    using (var key = tuple.Item1.OpenSubKey(tuple.Item2))
                    {
                        if (key == null) continue;
                        foreach (var name in key.GetValueNames())
                        {
                            var data = key.GetValue(name)?.ToString() ?? "";
                            if (string.IsNullOrEmpty(data)) continue;

                            var lvl = ThreatLevel.Safe;
                            var desc = "Standard autorun entry";
                            var low = data.ToLower();

                            if (suspiciousPaths.Any(p => low.Contains(p)))
                            {
                                lvl = ThreatLevel.Malicious;
                                desc = "Autorun from Temp/AppData/Public — classic malware persistence";
                            }
                            else if (suspiciousExts.Any(low.Contains))
                            {
                                lvl = ThreatLevel.Suspicious;
                                desc = "Script-based autorun entry";
                            }
                            else if (low.Contains("regsvr32") && low.Contains("/s") && low.Contains("/u"))
                            {
                                lvl = ThreatLevel.Suspicious;
                                desc = "regsvr32 silent regsvr — possible squiblydoodle attack";
                            }
                            else if (low.Contains("powershell") && (low.Contains("-enc") || low.Contains("hidden") || low.Contains("bypass")))
                            {
                                lvl = ThreatLevel.Suspicious;
                                desc = "Suspicious PowerShell autorun (encoded/hidden)";
                            }
                            else if (name.Equals("", StringComparison.OrdinalIgnoreCase) || name.ToLower().Contains("default"))
                            {
                                lvl = ThreatLevel.Info;
                            }

                            AddFinding(new RegFinding
                            {
                                Location = tuple.Item3,
                                Value = name,
                                Data = data.Length > 90 ? data.Substring(0, 87) + "..." : data,
                                Level = lvl,
                                Description = desc
                            });
                        }
                    }
                }
                catch { }
            }
        }

        private void ScanWinlogon()
        {
            OnProgress?.Invoke("[*] Scanning Winlogon...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    if (key == null) return;
                    CheckValue(key, "Shell", "explorer.exe", "HKLM\\Winlogon\\Shell", ThreatLevel.Malicious);
                    CheckValue(key, "Userinit", @"C:\Windows\system32\userinit.exe,", "HKLM\\Winlogon\\Userinit", ThreatLevel.Malicious);
                    CheckValue(key, "AppInit_DLLs", "", "HKLM\\Winlogon\\AppInit_DLLs", ThreatLevel.Suspicious);
                    CheckValue(key, "GinaDLL", "", "HKLM\\Winlogon\\GinaDLL", ThreatLevel.Suspicious);
                    CheckValue(key, "Taskman", "", "HKLM\\Winlogon\\Taskman", ThreatLevel.Suspicious);
                    CheckValue(key, "UIHost", "logonui.exe", "HKLM\\Winlogon\\UIHost", ThreatLevel.Suspicious);
                }
            }
            catch { }
        }

        private void CheckValue(RegistryKey key, string name, string expected, string location, ThreatLevel level)
        {
            try
            {
                var val = key.GetValue(name)?.ToString() ?? "";
                if (string.IsNullOrEmpty(val) && string.IsNullOrEmpty(expected)) return;
                if (val.Equals(expected, StringComparison.OrdinalIgnoreCase)) return;
                AddFinding(new RegFinding
                {
                    Location = location,
                    Value = name,
                    Data = val,
                    Level = level,
                    Description = $"Winlogon {name} tampered (expected: '{expected}')"
                });
            }
            catch { }
        }

        private void ScanIFEO()
        {
            OnProgress?.Invoke("[*] Scanning Image File Execution Options (debugger hijack)...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"))
                {
                    if (key == null) return;
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(sub))
                        {
                            var dbg = subKey?.GetValue("Debugger");
                            if (dbg != null && !string.IsNullOrEmpty(dbg.ToString()))
                            {
                                AddFinding(new RegFinding
                                {
                                    Location = $"HKLM\\IFEO\\{sub}",
                                    Value = "Debugger",
                                    Data = dbg.ToString(),
                                    Level = ThreatLevel.Malicious,
                                    Description = $"Debugger hijack: '{sub}' redirected to '{dbg}'"
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanAppInit()
        {
            OnProgress?.Invoke("[*] Scanning AppInit_DLLs...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows"))
                {
                    if (key == null) return;
                    var appInit = key.GetValue("AppInit_DLLs")?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(appInit))
                    {
                        AddFinding(new RegFinding
                        {
                            Location = "HKLM\\Windows\\AppInit_DLLs",
                            Value = "AppInit_DLLs",
                            Data = appInit,
                            Level = ThreatLevel.Malicious,
                            Description = "DLLs injected into every process — common malware technique"
                        });
                    }
                }
            }
            catch { }
        }

        private void ScanExplorerPolicies()
        {
            OnProgress?.Invoke("[*] Scanning Explorer restrictions...");
            var policies = new[]
            {
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoRun", "Disable Run dialog"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoFind", "Disable Search"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel", "Disable Control Panel"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoFolderOptions", "Disable Folder Options"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoWinKeys", "Disable Windows hotkeys"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoViewContextMenu", "Disable right-click"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDesktop", "Hide desktop icons"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoTrayContextMenu", "Disable tray menu"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoSetTaskbar", "Disable taskbar settings"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoSetFolders", "Disable folder settings"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr", "Disable Task Manager"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools", "Disable Registry Editor"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCMD", "Disable Command Prompt"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableChangePassword", "Disable password change"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableLockWorkstation", "Disable Win+L"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\System", "NoDispCPL", "Disable Display Settings"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDrives", "Hide drives"),
                Tuple.Create(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoClose", "Disable Shutdown"),
            };

            foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (var p in policies)
                {
                    try
                    {
                        using (var key = hive.OpenSubKey(p.Item1))
                        {
                            if (key == null) continue;
                            var val = key.GetValue(p.Item2);
                            if (val != null && (int)val != 0)
                            {
                                AddFinding(new RegFinding
                                {
                                    Location = $"{(hive == Registry.CurrentUser ? "HKCU" : "HKLM")}\\{p.Item1}\\{p.Item2}",
                                    Value = p.Item2,
                                    Data = val.ToString(),
                                    Level = ThreatLevel.Malicious,
                                    Description = p.Item3 + " — restriction set by malware"
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private void ScanBrowserHijacks()
        {
            OnProgress?.Invoke("[*] Scanning browser hijacks...");
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\Main"))
                {
                    if (key != null)
                    {
                        var startPage = key.GetValue("Start Page")?.ToString();
                        if (startPage != null && !startPage.Contains("Microsoft") && !startPage.Contains("bing") && !startPage.Contains("msn") && !string.IsNullOrEmpty(startPage))
                        {
                            AddFinding(new RegFinding
                            {
                                Location = "HKCU\\IE\\Main\\Start Page",
                                Value = "Start Page",
                                Data = startPage,
                                Level = ThreatLevel.Suspicious,
                                Description = "IE start page modified — possible browser hijack"
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanHostsRedirects() { }

        private void ScanSafeMode()
        {
            OnProgress?.Invoke("[*] Scanning Safe Mode restrictions...");
            try
            {
                var safeBoot = @"SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal";
                using (var key = Registry.LocalMachine.OpenSubKey(safeBoot))
                {
                    if (key != null)
                    {
                        foreach (var sub in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(sub))
                            {
                                var val = subKey?.GetValue("")?.ToString();
                                if (val != null && val.ToLower().Contains("malware"))
                                {
                                    AddFinding(new RegFinding
                                    {
                                        Location = $"HKLM\\SafeBoot\\Minimal\\{sub}",
                                        Value = "(default)",
                                        Data = val,
                                        Level = ThreatLevel.Suspicious,
                                        Description = "Suspicious Safe Mode entry"
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SafeBoot\Option"))
                {
                    if (key != null)
                    {
                        AddFinding(new RegFinding
                        {
                            Location = "HKLM\\SafeBoot\\Option",
                            Value = "(key exists)",
                            Data = "",
                            Level = ThreatLevel.Info,
                            Description = "SafeBoot Option key present"
                        });
                    }
                }
            }
            catch { }
        }

        private void ScanDisabledTools()
        {
            OnProgress?.Invoke("[*] Scanning disabled security tools...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender"))
                {
                    if (key != null)
                    {
                        var disable = key.GetValue("DisableAntiSpyware");
                        if (disable != null && (int)disable == 1)
                        {
                            AddFinding(new RegFinding
                            {
                                Location = "HKLM\\Policies\\Windows Defender",
                                Value = "DisableAntiSpyware",
                                Data = "1",
                                Level = ThreatLevel.Malicious,
                                Description = "Windows Defender disabled via policy — common malware action"
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanKnownDLLs()
        {
            OnProgress?.Invoke("[*] Scanning KnownDLLs...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs"))
                {
                    if (key == null) return;
                    foreach (var name in key.GetValueNames())
                    {
                        if (name.ToLower() == "dlldirectory" || name.ToLower() == "reserved") continue;
                        var val = key.GetValue(name)?.ToString() ?? "";
                        if (val.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
                        AddFinding(new RegFinding
                        {
                            Location = "HKLM\\Session Manager\\KnownDLLs",
                            Value = name,
                            Data = val,
                            Level = ThreatLevel.Suspicious,
                            Description = "Unexpected KnownDLLs entry"
                        });
                    }
                }
            }
            catch { }
        }

        private void ScanActiveSetup()
        {
            OnProgress?.Invoke("[*] Scanning Active Setup...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Active Setup\Installed Components"))
                {
                    if (key == null) return;
                    foreach (var sub in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(sub))
                        {
                            var cmd = subKey?.GetValue("StubPath")?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(cmd))
                            {
                                var low = cmd.ToLower();
                                if (low.Contains("\\temp\\") || low.Contains("\\appdata\\") || low.Contains("powershell") && low.Contains("-enc"))
                                {
                                    AddFinding(new RegFinding
                                    {
                                        Location = $"HKLM\\Active Setup\\{sub}",
                                        Value = "StubPath",
                                        Data = cmd,
                                        Level = ThreatLevel.Suspicious,
                                        Description = "Suspicious Active Setup stub"
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void ScanShellExtensions()
        {
            OnProgress?.Invoke("[*] Scanning Shell extensions...");
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects"))
                {
                    if (key != null)
                    {
                        foreach (var sub in key.GetSubKeyNames())
                        {
                            AddFinding(new RegFinding
                            {
                                Location = $"HKLM\\BHO\\{sub}",
                                Value = "(BHO)",
                                Data = sub,
                                Level = ThreatLevel.Info,
                                Description = "Browser Helper Object registered"
                            });
                        }
                    }
                }
            }
            catch { }
        }

        public bool RemoveFinding(RegFinding finding)
        {
            try
            {
                var parts = finding.Location.Split(new[] { "\\" }, 2, StringSplitOptions.None);
                if (parts.Length < 2) return false;
                RegistryKey hive = parts[0] == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
                using (var key = hive.OpenSubKey(parts[1], true))
                {
                    if (key == null) return false;
                    key.DeleteValue(finding.Value, false);
                }
                return true;
            }
            catch { return false; }
        }
    }
}
