using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MazizTool.Features
{
    public class SystemFileIntegrity
    {
        public event Action<string> OnOutput;
        public event Action<int> OnProgress;
        public bool IsRunning { get; private set; }

        public void Stop() { IsRunning = false; }

        public async Task RunSfcScannowAsync()
        {
            IsRunning = true;
            OnOutput?.Invoke("[*] Starting SFC /scannow — verifies and replaces corrupted system files...");
            await RunSystemCommandAsync("sfc.exe", "/scannow");
            OnOutput?.Invoke("[*] SFC scan finished.");
            IsRunning = false;
        }

        public async Task RunDismScanHealthAsync()
        {
            IsRunning = true;
            OnOutput?.Invoke("[*] Starting DISM /ScanHealth — checks for component store corruption...");
            await RunSystemCommandAsync("dism.exe", "/Online /Cleanup-Image /ScanHealth");
            OnOutput?.Invoke("[*] DISM ScanHealth finished.");
            IsRunning = false;
        }

        public async Task RunDismRestoreHealthAsync()
        {
            IsRunning = true;
            OnOutput?.Invoke("[*] Starting DISM /RestoreHealth — repairs Windows component store from WU...");
            await RunSystemCommandAsync("dism.exe", "/Online /Cleanup-Image /RestoreHealth");
            OnOutput?.Invoke("[*] DISM RestoreHealth finished.");
            IsRunning = false;
        }

        public async Task RunDismStartComponentCleanupAsync()
        {
            IsRunning = true;
            OnOutput?.Invoke("[*] Starting DISM /StartComponentCleanup — cleans superseded components...");
            await RunSystemCommandAsync("dism.exe", "/Online /Cleanup-Image /StartComponentCleanup");
            OnOutput?.Invoke("[*] Component cleanup finished.");
            IsRunning = false;
        }

        private async Task RunSystemCommandAsync(string fileName, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Default,
                    StandardErrorEncoding = Encoding.Default
                };

                using (var proc = new Process { StartInfo = psi, EnableRaisingEvents = true })
                {
                    proc.Start();
                    proc.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null && IsRunning)
                        {
                            OnOutput?.Invoke(e.Data);
                            if (e.Data.Contains("%"))
                            {
                                int pct = ParsePercent(e.Data);
                                if (pct >= 0) OnProgress?.Invoke(pct);
                            }
                        }
                    };
                    proc.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null && IsRunning) OnOutput?.Invoke("[!] " + e.Data);
                    };
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    await Task.Run(() => proc.WaitForExit());
                    OnOutput?.Invoke($"[*] Exit code: {proc.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                OnOutput?.Invoke("[!] Error: " + ex.Message);
            }
        }

        private int ParsePercent(string line)
        {
            try
            {
                int idx = line.IndexOf('%');
                if (idx < 0) return -1;
                int start = idx - 1;
                while (start >= 0 && char.IsDigit(line[start])) start--;
                start++;
                if (start < idx && int.TryParse(line.Substring(start, idx - start), out int pct))
                    return pct;
            }
            catch { }
            return -1;
        }

        public class SystemFileCheck
        {
            public string FilePath { get; set; }
            public string Status { get; set; }
            public long Size { get; set; }
            public string Hash { get; set; }
            public bool Signed { get; set; }
            public bool Suspicious { get; set; }
            public string Reason { get; set; }
        }

        public static List<SystemFileCheck> CheckCriticalSystemFiles()
        {
            var results = new List<SystemFileCheck>();
            var sysDir = Environment.SystemDirectory;
            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var criticalFiles = new[]
            {
                Path.Combine(sysDir, "explorer.exe"),
                Path.Combine(sysDir, "winlogon.exe"),
                Path.Combine(sysDir, "userinit.exe"),
                Path.Combine(sysDir, "svchost.exe"),
                Path.Combine(sysDir, "lsass.exe"),
                Path.Combine(sysDir, "services.exe"),
                Path.Combine(sysDir, "csrss.exe"),
                Path.Combine(sysDir, "smss.exe"),
                Path.Combine(sysDir, "wininit.exe"),
                Path.Combine(sysDir, "taskmgr.exe"),
                Path.Combine(sysDir, "regedit.exe"),
                Path.Combine(sysDir, "cmd.exe"),
                Path.Combine(sysDir, "rundll32.exe"),
                Path.Combine(sysDir, "dllhost.exe"),
                Path.Combine(sysDir, "mmc.exe"),
                Path.Combine(winDir, "explorer.exe"),
                Path.Combine(winDir, "System32\\cmd.exe"),
            };

            foreach (var file in criticalFiles)
            {
                var check = new SystemFileCheck { FilePath = file };
                try
                {
                    if (!File.Exists(file))
                    {
                        check.Status = "MISSING";
                        check.Suspicious = true;
                        check.Reason = "Critical system file is missing — may be deleted by malware";
                        results.Add(check);
                        continue;
                    }

                    var fi = new FileInfo(file);
                    check.Size = fi.Length;
                    check.Hash = ComputeSHA256(file);
                    check.Signed = VerifySignature(file);

                    var attr = fi.Attributes;
                    if (attr.HasFlag(FileAttributes.Hidden))
                    {
                        check.Suspicious = true;
                        check.Reason = "Hidden attribute on system file";
                    }
                    if (fi.CreationTime > DateTime.Now.AddDays(-1))
                    {
                        check.Suspicious = true;
                        check.Reason = (check.Reason ?? "") + " | Recently modified (<24h)";
                    }
                    if (!check.Signed)
                    {
                        check.Suspicious = true;
                        check.Reason = (check.Reason ?? "") + " | Not digitally signed by Microsoft";
                    }

                    check.Status = check.Suspicious ? "SUSPICIOUS" : "OK";
                }
                catch (Exception ex)
                {
                    check.Status = "ERROR";
                    check.Reason = ex.Message;
                    check.Suspicious = true;
                }
                results.Add(check);
            }
            return results;
        }

        public static string ComputeSHA256(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                using (var sha = SHA256.Create())
                {
                    var hash = sha.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch { return "N/A"; }
        }

        public static bool VerifySignature(string filePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"Get-AuthenticodeSignature '{filePath}' | Select-Object -ExpandProperty Status\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    var output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit(5000);
                    return output.Equals("Valid", StringComparison.OrdinalIgnoreCase) ||
                           output.Equals("HashMismatch", StringComparison.OrdinalIgnoreCase) == false && output.Length > 0 && !output.Contains("NotSigned") && !output.Contains("UnknownError");
                }
            }
            catch { return false; }
        }

        public static List<string> CheckKnownDLLs()
        {
            var suspicious = new List<string>();
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs"))
                {
                    if (key != null)
                    {
                        foreach (var name in key.GetValueNames())
                        {
                            var val = key.GetValue(name)?.ToString();
                            if (val != null && !val.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                                !val.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                suspicious.Add($"KnownDLLs\\{name} = {val}");
                            }
                        }
                    }
                }
            }
            catch { }
            return suspicious;
        }

        public static List<string> CheckImageFileExecutionOptions()
        {
            var hijacked = new List<string>();
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"))
                {
                    if (key != null)
                    {
                        foreach (var sub in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(sub))
                            {
                                var debugger = subKey?.GetValue("Debugger");
                                if (debugger != null && !string.IsNullOrEmpty(debugger.ToString()))
                                {
                                    hijacked.Add($"IFEO[{sub}] -> Debugger = {debugger}");
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return hijacked;
        }

        public static List<string> CheckWinlogonPersistence()
        {
            var entries = new List<string>();
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"))
                {
                    if (key != null)
                    {
                        foreach (var name in new[] { "Shell", "Userinit", "AppInit_DLLs", "UIHost", "GinaDLL", "Taskman" })
                        {
                            var val = key.GetValue(name)?.ToString();
                            if (!string.IsNullOrEmpty(val))
                            {
                                bool suspicious = false;
                                if (name == "Shell" && !val.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
                                    suspicious = true;
                                if (name == "Userinit" && !val.Equals("C:\\Windows\\system32\\userinit.exe,", StringComparison.OrdinalIgnoreCase))
                                    suspicious = true;
                                if (name == "AppInit_DLLs" && !string.IsNullOrEmpty(val))
                                    suspicious = true;
                                if (suspicious)
                                    entries.Add($"[!] Winlogon\\{name} = {val}");
                                else
                                    entries.Add($"[OK] Winlogon\\{name} = {val}");
                            }
                        }
                    }
                }
            }
            catch { }
            return entries;
        }
    }
}
