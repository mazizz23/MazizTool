using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MazizTool.Features
{
    public class FileAnalyzer
    {
        public class PeInfo
        {
            public string FilePath { get; set; }
            public bool IsPe { get; set; }
            public bool Is64Bit { get; set; }
            public bool IsDotNet { get; set; }
            public DateTime CompilationTime { get; set; }
            public string Machine { get; set; }
            public string Subsystem { get; set; }
            public uint SizeOfCode { get; set; }
            public List<string> Imports { get; set; } = new List<string>();
            public List<string> SuspiciousImports { get; set; } = new List<string>();
            public string Sha256 { get; set; }
            public string Md5 { get; set; }
            public bool IsSigned { get; set; }
            public long FileSize { get; set; }
            public List<string> Flags { get; set; } = new List<string>();
        }

        private static readonly HashSet<string> SuspiciousApiNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CreateRemoteThread", "VirtualAllocEx", "WriteProcessMemory", "ReadProcessMemory",
            "SetWindowsHookExA", "SetWindowsHookExW", "GetAsyncKeyState", "GetKeyState",
            "CreateServiceA", "CreateServiceW", "ChangeServiceConfigA", "ChangeServiceConfigW",
            "RegCreateKeyExA", "RegSetValueExA", "RegCreateKeyExW", "RegSetValueExW",
            "URLDownloadToFileA", "URLDownloadToFileW", "InternetOpenA", "InternetOpenW",
            "WinExec", "ShellExecuteA", "ShellExecuteW", "CreateProcessA", "CreateProcessW",
            "LoadLibraryA", "LoadLibraryW", "GetProcAddress", "VirtualProtect",
            "CreateFileMappingA", "MapViewOfFile", "SetFileAttributesA", "SetFileAttributesW",
            "FindFirstFileA", "FindNextFileA", "DeleteFileA", "DeleteFileW",
            "OpenProcess", "TerminateProcess", "CreateToolhelp32Snapshot",
            "Process32First", "Process32Next", "Module32First", "Module32Next",
            "Wsastartup", "socket", "connect", "send", "recv", "bind", "listen", "accept",
            "CryptEncrypt", "CryptDecrypt", "BCryptEncrypt", "BCryptDecrypt",
            "IsDebuggerPresent", "CheckRemoteDebuggerPresent", "OutputDebugStringA",
            "NtUnmapViewOfSection", "NtSetInformationThread", "RtlAdjustPrivilege",
            "QueueUserAPC", "NtCreateThreadEx", "RtlCreateUserThread"
        };

        public PeInfo Analyze(string filePath)
        {
            var info = new PeInfo { FilePath = filePath };
            try
            {
                if (!File.Exists(filePath)) { info.IsPe = false; return info; }

                var fi = new FileInfo(filePath);
                info.FileSize = fi.Length;

                info.Sha256 = ComputeHash(filePath, SHA256.Create());
                info.Md5 = ComputeHash(filePath, MD5.Create());

                byte[] header = new byte[Math.Min(fi.Length, 4096)];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    fs.Read(header, 0, header.Length);

                ParsePeHeader(header, info);
                CheckSigned(filePath, info);

                if (info.IsPe && info.CompilationTime > DateTime.MinValue)
                {
                    if (info.CompilationTime > DateTime.Now.AddDays(1))
                        info.Flags.Add("Future timestamp (anti-forensic)");
                    if (info.CompilationTime < new DateTime(2000, 1, 1))
                        info.Flags.Add("Old timestamp (possible backdate)");
                }

                if (info.SuspiciousImports.Count > 5)
                    info.Flags.Add($"Heavy suspicious API usage ({info.SuspiciousImports.Count})");
                if (!info.IsSigned && info.IsPe)
                    info.Flags.Add("Unsigned executable");
            }
            catch (Exception ex)
            {
                info.Flags.Add("Error: " + ex.Message);
            }
            return info;
        }

        private string ComputeHash(string path, HashAlgorithm algo)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var hash = algo.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch { return "N/A"; }
        }

        private void ParsePeHeader(byte[] data, PeInfo info)
        {
            try
            {
                if (data.Length < 64) return;
                if (data[0] != 'M' || data[1] != 'Z') { info.IsPe = false; return; }
                info.IsPe = true;

                int e_lfanew = BitConverter.ToInt32(data, 0x3C);
                if (e_lfanew < 0 || e_lfanew + 24 > data.Length) return;

                if (data[e_lfanew] != 'P' || data[e_lfanew + 1] != 'E' || data[e_lfanew + 2] != 0 || data[e_lfanew + 3] != 0)
                    return;

                int machine = BitConverter.ToUInt16(data, e_lfanew + 4);
                info.Machine = machine switch
                {
                    0x14c => "x86 (i386)",
                    0x8664 => "x64 (AMD64)",
                    0xAA64 => "ARM64",
                    0x1C0 => "ARM",
                    _ => $"0x{machine:X4}"
                };
                info.Is64Bit = machine == 0x8664 || machine == 0xAA64;

                int numberOfSections = BitConverter.ToUInt16(data, e_lfanew + 6);
                int timeDateStamp = BitConverter.ToInt32(data, e_lfanew + 8);
                try { info.CompilationTime = DateTimeOffset.FromUnixTimeSeconds(timeDateStamp).LocalDateTime; }
                catch { }

                int sizeOfOptionalHeader = BitConverter.ToUInt16(data, e_lfanew + 20);
                int characteristics = BitConverter.ToUInt16(data, e_lfanew + 22);

                int optHdr = e_lfanew + 24;
                if (optHdr + 28 > data.Length) return;

                ushort magic = BitConverter.ToUInt16(data, optHdr);
                bool is64 = magic == 0x20b;
                info.SizeOfCode = BitConverter.ToUInt32(data, optHdr + 4);

                int subsystemOffset = is64 ? optHdr + 68 : optHdr + 68;
                if (subsystemOffset + 2 <= data.Length)
                {
                    ushort subsystem = BitConverter.ToUInt16(data, subsystemOffset);
                    info.Subsystem = subsystem switch
                    {
                        1 => "Native",
                        2 => "Windows GUI",
                        3 => "Windows Console",
                        7 => "POSIX",
                        9 => "Windows CE GUI",
                        10 => "EFI Application",
                        _ => $"0x{subsystem:X}"
                    };
                }

                int dataDirOffset = is64 ? optHdr + 112 : optHdr + 96;
                if (dataDirOffset + 8 <= data.Length)
                {
                    int importRva = BitConverter.ToInt32(data, dataDirOffset + 8);
                    int importSize = BitConverter.ToInt32(data, dataDirOffset + 12);
                    if (importSize > 0)
                        info.Flags.Add($"Imports directory present (size {importSize})");
                }

                info.IsDotNet = CheckDotNet(data, e_lfanew, is64, sizeOfOptionalHeader, numberOfSections);
            }
            catch { }
        }

        private bool CheckDotNet(byte[] data, int e_lfanew, bool is64, int sizeOptHdr, int numSections)
        {
            try
            {
                int sectionStart = e_lfanew + 24 + sizeOptHdr;
                int dataDirOffset = is64 ? e_lfanew + 24 + 112 : e_lfanew + 24 + 96;
                int comRva = BitConverter.ToInt32(data, dataDirOffset + 208);
                return comRva != 0;
            }
            catch { return false; }
        }

        private void CheckSigned(string path, PeInfo info)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"if((Get-AuthenticodeSignature '{path}').Status -eq 'Valid'){{'true'}}else{{'false'}}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    var outp = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit(5000);
                    info.IsSigned = outp.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { info.IsSigned = false; }
        }

        public string FormatReport(PeInfo info)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"File:       {info.FilePath}");
            sb.AppendLine($"Size:       {info.FileSize:N0} bytes");
            sb.AppendLine($"Is PE:      {info.IsPe}");
            if (info.IsPe)
            {
                sb.AppendLine($"Machine:    {info.Machine}");
                sb.AppendLine($"64-bit:     {info.Is64Bit}");
                sb.AppendLine($".NET:       {info.IsDotNet}");
                sb.AppendLine($"Subsystem:  {info.Subsystem}");
                sb.AppendLine($"Compiled:   {info.CompilationTime}");
                sb.AppendLine($"Code size:  {info.SizeOfCode:N0}");
                sb.AppendLine($"Signed:     {info.IsSigned}");
            }
            sb.AppendLine($"SHA-256:    {info.Sha256}");
            sb.AppendLine($"MD5:        {info.Md5}");
            if (info.Flags.Count > 0)
            {
                sb.AppendLine("FLAGS:");
                foreach (var f in info.Flags) sb.AppendLine($"  ! {f}");
            }
            return sb.ToString();
        }
    }
}
