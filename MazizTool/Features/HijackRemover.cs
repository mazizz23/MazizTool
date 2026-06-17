using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Microsoft.Win32;

namespace MazizTool.Features
{
    public class HijackRemover
    {
        public class HijackFinding
        {
            public string Category { get; set; }
            public string Detail { get; set; }
            public string Value { get; set; }
            public bool Suspicious { get; set; }
            public string Fix { get; set; }
        }

        public event Action<string> OnProgress;
        public event Action<HijackFinding> OnFinding;
        public List<HijackFinding> Findings { get; private set; } = new List<HijackFinding>();

        public void Scan()
        {
            Findings.Clear();
            CheckProxySettings();
            CheckDnsSettings();
            CheckWinsockLsp();
            CheckHostsFile();
            CheckBrowserHijacks();
            CheckScheduledTasks();
            CheckFirewallRules();
            CheckWmiPersistence();
            OnProgress?.Invoke("[*] Hijack scan complete. Findings: " + Findings.Count);
        }

        private void Add(HijackFinding f)
        {
            Findings.Add(f);
            OnFinding?.Invoke(f);
        }

        private void CheckProxySettings()
        {
            OnProgress?.Invoke("[*] Checking proxy settings...");
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (key == null) return;
                    var enable = key.GetValue("ProxyEnable");
                    var server = key.GetValue("ProxyServer")?.ToString();
                    var overrideVal = key.GetValue("ProxyOverride")?.ToString();
                    if (enable != null && (int)enable == 1 && !string.IsNullOrEmpty(server))
                    {
                        Add(new HijackFinding
                        {
                            Category = "Proxy",
                            Detail = "System proxy enabled",
                            Value = server,
                            Suspicious = !server.Contains("127.0.0.1") && !server.Contains("localhost") && !server.Contains("local"),
                            Fix = "Reset Internet Settings / netsh winhttp reset proxy"
                        });
                    }
                }
            }
            catch { }
        }

        private void CheckDnsSettings()
        {
            OnProgress?.Invoke("[*] Checking DNS settings per adapter...");
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                foreach (var adapter in adapters)
                {
                    var dns = adapter.GetIPProperties().DnsAddresses;
                    foreach (var addr in dns)
                    {
                        bool suspicious = false;
                        var ip = addr.ToString();
                        if (ip.StartsWith("127.") && ip != "127.0.0.1") suspicious = true;
                        if (ip.StartsWith("0.")) suspicious = true;
                        if (ip == "::1") suspicious = true;
                        Add(new HijackFinding
                        {
                            Category = "DNS",
                            Detail = $"{adapter.Name} -> {ip}",
                            Value = ip,
                            Suspicious = suspicious,
                            Fix = "netsh interface ip set dns name=\"" + adapter.Name + "\" dhcp"
                        });
                    }
                }
            }
            catch { }
        }

        private void CheckWinsockLsp()
        {
            OnProgress?.Invoke("[*] Checking Winsock LSP entries...");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "winsock show catalog",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);
                    var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    int count = lines.Count(l => l.Contains("Entry Type") && l.Contains("Layered"));
                    Add(new HijackFinding
                    {
                        Category = "Winsock LSP",
                        Detail = $"LSP entries: {count}",
                        Value = count.ToString(),
                        Suspicious = count > 10,
                        Fix = "netsh winsock reset"
                    });
                }
            }
            catch { }
        }

        private void CheckHostsFile()
        {
            OnProgress?.Invoke("[*] Checking hosts file...");
            try
            {
                var hostsPath = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");
                if (!File.Exists(hostsPath)) return;
                var lines = File.ReadAllLines(hostsPath);
                int redirects = 0;
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                    if (trimmed.Contains("127.0.0.1") && (trimmed.Contains("google") || trimmed.Contains("microsoft") ||
                        trimmed.Contains("windowsupdate") || trimmed.Contains("antivirus") || trimmed.Contains("avast") ||
                        trimmed.Contains("kaspersky") || trimmed.Contains("virustotal") || trimmed.Contains("malwarebytes")))
                    {
                        redirects++;
                        Add(new HijackFinding
                        {
                            Category = "Hosts",
                            Detail = trimmed,
                            Value = trimmed,
                            Suspicious = true,
                            Fix = "Delete line / reset hosts file"
                        });
                    }
                    else if (!trimmed.Contains("127.0.0.1") && !trimmed.Contains("::1") && !trimmed.StartsWith("0.0.0.0"))
                    {
                        Add(new HijackFinding
                        {
                            Category = "Hosts",
                            Detail = trimmed,
                            Value = trimmed,
                            Suspicious = true,
                            Fix = "Suspicious redirect entry — review"
                        });
                    }
                }
            }
            catch { }
        }

        private void CheckBrowserHijacks()
        {
            OnProgress?.Invoke("[*] Checking browser hijack points...");
            var ieKeys = new[]
            {
                Tuple.Create(@"Software\Microsoft\Internet Explorer\Main", "Start Page"),
                Tuple.Create(@"Software\Microsoft\Internet Explorer\Main", "Default_Page_URL"),
                Tuple.Create(@"Software\Microsoft\Internet Explorer\Main", "Search Page"),
                Tuple.Create(@"Software\Microsoft\Internet Explorer\SearchScopes", "DefaultScope"),
            };
            foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                foreach (var k in ieKeys)
                {
                    try
                    {
                        using (var key = hive.OpenSubKey(k.Item1))
                        {
                            var val = key?.GetValue(k.Item2)?.ToString();
                            if (string.IsNullOrEmpty(val)) continue;
                            bool susp = !val.Contains("microsoft") && !val.Contains("bing") && !val.Contains("msn") &&
                                        (val.Contains("http") || val.Contains("www."));
                            Add(new HijackFinding
                            {
                                Category = "Browser",
                                Detail = $"{(hive == Registry.CurrentUser ? "HKCU" : "HKLM")}\\{k.Item1}\\{k.Item2}",
                                Value = val,
                                Suspicious = susp,
                                Fix = "Reset browser settings"
                            });
                        }
                    }
                    catch { }
                }
            }
        }

        private void CheckScheduledTasks()
        {
            OnProgress?.Invoke("[*] Checking scheduled tasks for persistence...");
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
                    proc.WaitForExit(5000);
                    var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = ParseCsvLine(lines[i]);
                        if (parts.Length < 9) continue;
                        var name = parts[0].Trim('"');
                        var cmd = parts[8].Trim('"');
                        var low = cmd.ToLower();
                        if (low.Contains("\\temp\\") || low.Contains("\\appdata\\local\\temp\\") ||
                            (low.Contains("powershell") && (low.Contains("-enc") || low.Contains("hidden"))))
                        {
                            Add(new HijackFinding
                            {
                                Category = "Task",
                                Detail = name,
                                Value = cmd.Length > 80 ? cmd.Substring(0, 77) + "..." : cmd,
                                Suspicious = true,
                                Fix = $"schtasks /delete /tn \"{name}\" /f"
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }
            result.Add(sb.ToString());
            return result.ToArray();
        }

        private void CheckFirewallRules()
        {
            OnProgress?.Invoke("[*] Checking firewall allow rules for suspicious apps...");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "advfirewall firewall show rule name=all dir=in",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(5000);
                    var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var block in blocks)
                    {
                        var low = block.ToLower();
                        if (block.Contains("Enable:                           Yes") &&
                            (low.Contains("\\temp\\") || low.Contains("\\appdata\\") || low.Contains("\\users\\public\\")))
                        {
                            var nameLine = block.Split('\n').FirstOrDefault(l => l.StartsWith("Rule Name:"));
                            Add(new HijackFinding
                            {
                                Category = "Firewall",
                                Detail = nameLine?.Replace("Rule Name:", "").Trim() ?? "Unknown",
                                Value = "Inbound allow from temp",
                                Suspicious = true,
                                Fix = "netsh advfirewall firewall delete rule"
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void CheckWmiPersistence()
        {
            OnProgress?.Invoke("[*] Checking WMI event subscriptions...");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wmic.exe",
                    Arguments = "/namespace:\\\\root\\subscription path __EventConsumer get * /format:list",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(5000);
                    if (!string.IsNullOrWhiteSpace(output) && output.Contains("CommandLineTemplate"))
                    {
                        Add(new HijackFinding
                        {
                            Category = "WMI",
                            Detail = "ActiveEventFilterConsumer",
                            Value = "WMI event subscription found",
                            Suspicious = true,
                            Fix = "Remove WMI subscription via root\\subscription"
                        });
                    }
                }
            }
            catch { }
        }

        public bool FixProxy()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true))
                {
                    key?.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    key?.DeleteValue("ProxyServer", false);
                    key?.DeleteValue("ProxyOverride", false);
                }
                Process.Start(new ProcessStartInfo { FileName = "netsh.exe", Arguments = "winhttp reset proxy", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public bool FixWinsock()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "netsh.exe", Arguments = "winsock reset", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000);
                return true;
            }
            catch { return false; }
        }

        public bool ResetDns()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "ipconfig.exe", Arguments = "/flushdns", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000);
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = "netsh.exe", Arguments = $"interface ip set dns name=\"{adapter.Name}\" dhcp", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(2000);
                    }
                    catch { }
                }
                return true;
            }
            catch { return false; }
        }
    }
}
