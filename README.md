# MazizTool

System recovery & anti-malware hub for Windows. Runs in Recovery Environment, WinPE, Safe Mode, or from USB.

## Features

- **System File Integrity** — `SFC /scannow`, `DISM /ScanHealth`, `DISM /RestoreHealth`, critical file verification, IFEO/Winlogon/KnownDLLs hijack detection
- **Registry Scanner** — full registry scan: autoruns, Winlogon, IFEO, AppInit_DLLs, Explorer policies, browser hijacks, SafeMode, disabled security tools
- **Service Scanner** — WMI service enumeration, suspicious path detection, stop/disable/delete
- **Hijack Remover** — proxy, DNS, Winsock LSP, hosts, browser, scheduled tasks, firewall rules, WMI persistence
- **Virus Scanner** — SHA-256 signatures + heuristic (suspicious paths, names, attributes)
- **Task Manager / Process Killer** — internal process manager, force-kill protected processes
- **Startup Manager** — registry + folder + scheduled task autoruns
- **File Analyzer** — PE header inspection, hash, signature, suspicious API imports
- **Hotkey Restorer** — unblock Alt+Tab, Ctrl+Alt+Del, WinKey, TaskMgr, Regedit
- **Font Protection** — restore Segoe UI if malware substitutes fonts
- **UAC Bypass** — FodHelper, ComputerDefaults, EventViewer, Sdclt, WSReset
- **Registry / Hosts Editor**, **File Browser**, **System Tools**, **Explorer/CMD/PowerShell** launchers

## Build

Requires .NET 8 SDK.

```cmd
build.bat
```

Or cross-compile from macOS/Linux:

```bash
dotnet publish MazizTool/MazizTool.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
```

Output: `publish/MazizTool.exe` — single self-contained file, no .NET runtime required.

## Download

Grab the latest EXE from [Releases](../../releases/latest).

## Tech

- C# / .NET 8 / Windows Forms
- Single-file self-contained deployment (win-x64)
- Dark green/black hacker console aesthetic
