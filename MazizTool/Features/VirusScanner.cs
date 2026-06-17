using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MazizTool.Features
{
    public class VirusScanner
    {
        public List<ScanResult> Results { get; private set; } = new List<ScanResult>();
        public int FilesScanned { get; private set; }
        public int ThreatsFound { get; private set; }
        public bool IsScanning { get; private set; }
        public event Action<string, int> OnProgress;
        public event Action<ScanResult> OnThreatFound;
        public event Action OnScanComplete;

        private static readonly HashSet<string> KnownMalwareHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            "a5b1cfad8acb496e24e0e6c1346e89973e5fb78908c21279bcdfb22cda55bf76",
        };

        private static readonly List<string> MaliciousNames = new List<string>
        {
            "trojan", "malware", "ransomware", "keylogger", "spyware",
            "backdoor", "rootkit", "worm", "dropper", "downloader",
            "cryptominer", "stealer", "botnet", "banker", "rat",
            "injector", "exploit", "packed", "obfuscated", "suspicious"
        };

        private static readonly List<string> RiskFileExtensions = new List<string>
        {
            ".exe", ".dll", ".scr", ".pif", ".com", ".bat", ".cmd",
            ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".msi",
            ".hta", ".cpl", ".msc", ".jar", ".ps1", ".sys"
        };

        private static readonly List<string> SuspiciousPaths = new List<string>
        {
            @"\AppData\Roaming\",
            @"\AppData\Local\Temp\",
            @"\Windows\Temp\",
            @"\ProgramData\",
            @"\Users\Public\",
            @"\RECYCLER\",
            @"\System Volume Information\",
            @"\$Recycle.Bin\"
        };

        public class ScanResult
        {
            public string FilePath { get; set; }
            public string ThreatName { get; set; }
            public ThreatLevel Level { get; set; }
            public string Description { get; set; }
            public DateTime ScanTime { get; set; }
        }

        public enum ThreatLevel
        {
            Low,
            Medium,
            High,
            Critical
        }

        public void StopScan()
        {
            IsScanning = false;
        }

        public async Task ScanAsync(string scanPath = null)
        {
            IsScanning = true;
            Results.Clear();
            FilesScanned = 0;
            ThreatsFound = 0;

            var drives = string.IsNullOrEmpty(scanPath)
                ? DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName).ToList()
                : new List<string> { scanPath };

            foreach (var drive in drives)
            {
                if (!IsScanning) break;
                await ScanDirectoryAsync(drive);
            }

            IsScanning = false;
            OnScanComplete?.Invoke();
        }

        private async Task ScanDirectoryAsync(string path)
        {
            if (!IsScanning) return;
            try
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    if (!IsScanning) return;
                    await ScanFileAsync(file);
                }

                var dirs = Directory.GetDirectories(path);
                foreach (var dir in dirs)
                {
                    if (!IsScanning) return;
                    if (IsSafeDirectory(dir)) continue;
                    await ScanDirectoryAsync(dir);
                }
            }
            catch { }
        }

        private bool IsSafeDirectory(string path)
        {
            var name = Path.GetFileName(path).ToLower();
            return name == "windows" || name == "program files" || name == "program files (x86)" ||
                   name == "system32" || name == "syswow64" || name == "winnt";
        }

        private async Task ScanFileAsync(string filePath)
        {
            FilesScanned++;
            OnProgress?.Invoke(filePath, FilesScanned);

            var ext = Path.GetExtension(filePath).ToLower();
            if (!RiskFileExtensions.Contains(ext)) return;

            try
            {
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > 500 * 1024 * 1024) return;

                var result = CheckFileName(filePath);
                if (result != null) { AddThreat(result); return; }

                result = CheckFilePath(filePath);
                if (result != null) { AddThreat(result); return; }

                if (fileInfo.Length < 100 * 1024 * 1024)
                {
                    result = await CheckFileHashAsync(filePath);
                    if (result != null) { AddThreat(result); return; }
                }

                result = CheckSuspiciousAttributes(filePath);
                if (result != null) AddThreat(result);
            }
            catch { }
        }

        private ScanResult CheckFileName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
            foreach (var name in MaliciousNames)
            {
                if (fileName.Contains(name))
                {
                    return new ScanResult
                    {
                        FilePath = filePath,
                        ThreatName = "Suspicious filename",
                        Level = ThreatLevel.Medium,
                        Description = $"File name contains suspicious pattern: '{name}'",
                        ScanTime = DateTime.Now
                    };
                }
            }
            return null;
        }

        private ScanResult CheckFilePath(string filePath)
        {
            var path = filePath.ToLower();
            foreach (var suspPath in SuspiciousPaths)
            {
                if (path.Contains(suspPath.ToLower()))
                {
                    var ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".exe" || ext == ".dll" || ext == ".scr")
                    {
                        return new ScanResult
                        {
                            FilePath = filePath,
                            ThreatName = "Suspicious location",
                            Level = ThreatLevel.High,
                            Description = $"Executable in suspicious directory: {suspPath}",
                            ScanTime = DateTime.Now
                        };
                    }
                }
            }
            return null;
        }

        private async Task<ScanResult> CheckFileHashAsync(string filePath)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                using (var sha256 = SHA256.Create())
                {
                    var hash = BitConverter.ToString(await sha256.ComputeHashAsync(stream)).Replace("-", "").ToLower();
                    if (KnownMalwareHashes.Contains(hash))
                    {
                        return new ScanResult
                        {
                            FilePath = filePath,
                            ThreatName = "Known malware signature",
                            Level = ThreatLevel.Critical,
                            Description = "File hash matches known malware database",
                            ScanTime = DateTime.Now
                        };
                    }
                }
            }
            catch { }
            return null;
        }

        private ScanResult CheckSuspiciousAttributes(string filePath)
        {
            try
            {
                var attr = File.GetAttributes(filePath);
                if (attr.HasFlag(FileAttributes.Hidden) && attr.HasFlag(FileAttributes.System))
                {
                    var ext = Path.GetExtension(filePath).ToLower();
                    if (ext == ".exe" || ext == ".dll")
                    {
                        return new ScanResult
                        {
                            FilePath = filePath,
                            ThreatName = "Hidden system file",
                            Level = ThreatLevel.Medium,
                            Description = "Hidden+System attribute executable - common malware technique",
                            ScanTime = DateTime.Now
                        };
                    }
                }
            }
            catch { }
            return null;
        }

        private void AddThreat(ScanResult result)
        {
            Results.Add(result);
            ThreatsFound++;
            OnThreatFound?.Invoke(result);
        }

        public static List<Process> GetSuspiciousProcesses()
        {
            var suspicious = new List<Process>();
            var processes = Process.GetProcesses();
            var knownSystemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "system", "system idle process", "registry", "smss.exe", "csrss.exe",
                "wininit.exe", "winlogon.exe", "services.exe", "lsass.exe", "svchost.exe",
                "dwm.exe", "explorer.exe", "taskhostw.exe", "runtimebroker.exe", "shellexperiencehost.exe",
                "searchindexer.exe", "securityhealthsystray.exe", "sihost.exe", "ctfmon.exe",
                "spoolsv.exe", "fontdrvhost.exe", "wlms.exe"
            };

            foreach (var proc in processes)
            {
                try
                {
                    var name = proc.ProcessName.ToLower();
                    if (knownSystemProcesses.Contains(name + ".exe") || knownSystemProcesses.Contains(name))
                        continue;

                    bool suspiciousFlag = false;

                    if (string.IsNullOrEmpty(proc.MainModule?.FileName))
                        suspiciousFlag = true;

                    if (!suspiciousFlag)
                    {
                        var fileName = proc.MainModule.FileName.ToLower();
                        foreach (var susp in SuspiciousPaths)
                        {
                            if (fileName.Contains(susp.ToLower()))
                            {
                                suspiciousFlag = true;
                                break;
                            }
                        }
                        if (!suspiciousFlag)
                        {
                            foreach (var mal in MaliciousNames)
                            {
                                if (fileName.Contains(mal))
                                {
                                    suspiciousFlag = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (suspiciousFlag)
                        suspicious.Add(proc);
                }
                catch { }
            }
            return suspicious;
        }
    }
}
