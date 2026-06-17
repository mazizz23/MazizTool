using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceProcess;

namespace MazizTool.Features
{
    public class ServiceScanner
    {
        public class ServiceInfo
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string BinaryPath { get; set; }
            public string State { get; set; }
            public string StartMode { get; set; }
            public string ServiceType { get; set; }
            public bool Suspicious { get; set; }
            public string Reason { get; set; }
        }

        public event Action<string> OnProgress;
        public event Action<ServiceInfo> OnService;
        public List<ServiceInfo> Services { get; private set; } = new List<ServiceInfo>();

        public void Scan()
        {
            Services.Clear();
            OnProgress?.Invoke("[*] Enumerating services via WMI...");
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
                foreach (var obj in searcher.Get())
                {
                    try
                    {
                        var svc = new ServiceInfo
                        {
                            Name = obj["Name"]?.ToString() ?? "",
                            DisplayName = obj["DisplayName"]?.ToString() ?? "",
                            BinaryPath = obj["PathName"]?.ToString() ?? "",
                            State = obj["State"]?.ToString() ?? "",
                            StartMode = obj["StartMode"]?.ToString() ?? "",
                            ServiceType = obj["ServiceType"]?.ToString() ?? ""
                        };

                        AnalyzeService(svc);
                        Services.Add(svc);
                        OnService?.Invoke(svc);
                    }
                    catch { }
                }
                OnProgress?.Invoke($"[*] Service scan complete. Total: {Services.Count}, Suspicious: {Services.Count(s => s.Suspicious)}");
            }
            catch (Exception ex)
            {
                OnProgress?.Invoke("[!] WMI error: " + ex.Message);
                FallbackEnum();
            }
        }

        private void FallbackEnum()
        {
            try
            {
                var services = ServiceController.GetServices();
                foreach (var sc in services)
                {
                    try
                    {
                        var svc = new ServiceInfo
                        {
                            Name = sc.ServiceName,
                            DisplayName = sc.DisplayName,
                            State = sc.Status.ToString(),
                            StartMode = "Unknown",
                            BinaryPath = ""
                        };
                        Services.Add(svc);
                        OnService?.Invoke(svc);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void AnalyzeService(ServiceInfo svc)
        {
            var path = svc.BinaryPath.ToLower();
            if (string.IsNullOrEmpty(path)) return;

            if (path.Contains("\\temp\\") || path.Contains("\\users\\public\\") || path.Contains("\\appdata\\"))
            {
                svc.Suspicious = true;
                svc.Reason = "Service runs from Temp/AppData/Public — classic malware";
            }
            else if (path.Contains("\\recycler\\") || path.Contains("$recycle.bin"))
            {
                svc.Suspicious = true;
                svc.Reason = "Service runs from Recycle Bin";
            }
            else if (path.Contains(".scr") || path.Contains(".pif"))
            {
                svc.Suspicious = true;
                svc.Reason = "Service uses SCR/PIF executable";
            }
            else if (path.Contains("powershell") && (path.Contains("-enc") || path.Contains("hidden") || path.Contains("bypass")))
            {
                svc.Suspicious = true;
                svc.Reason = "Encoded/hidden PowerShell service";
            }
            else if (path.Contains("cmd.exe") && path.Contains("/c"))
            {
                svc.Suspicious = true;
                svc.Reason = "Service launches cmd /c — wrapper service";
            }
            else if (!path.Contains("\\windows\\") && !path.Contains("\\program files"))
            {
                svc.Suspicious = true;
                svc.Reason = "Service binary outside Windows/Program Files";
            }
        }

        public static bool StopService(string name)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    if (sc.CanStop) sc.Stop();
                    return true;
                }
            }
            catch { return false; }
        }

        public static bool DisableService(string name)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"config \"{name}\" start= disabled",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public static bool DeleteService(string name)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete \"{name}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }
    }
}
