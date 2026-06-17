using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MazizTool.Features;
using MazizTool.Native;

namespace MazizTool
{
    public partial class MainForm : Form
    {
        private Panel sidebarPanel;
        private Panel contentPanel;
        private Panel headerPanel;
        private Label titleLabel;
        private Label statusLabel;
        private Panel currentFeaturePanel;
        private Dictionary<string, Button> navButtons = new Dictionary<string, Button>();
        private Button activeNavButton;
        private VirusScanner scanner;
        private SystemFileIntegrity integrityScanner;
        private RegistryScanner registryScanner;
        private ServiceScanner serviceScanner;
        private FileAnalyzer fileAnalyzer;
        private HijackRemover hijackRemover;
        private System.Windows.Forms.Timer statusTimer;

        private const int SidebarWidth = 230;
        private const int HeaderHeight = 50;

        public MainForm()
        {
            InitializeForm();
            SetupSidebar();
            SetupHeader();
            SetupContent();
            scanner = new VirusScanner();
            scanner.OnProgress += OnScanProgress;
            scanner.OnThreatFound += OnThreatFound;
            scanner.OnScanComplete += OnScanComplete;
            integrityScanner = new SystemFileIntegrity();
            registryScanner = new RegistryScanner();
            serviceScanner = new ServiceScanner();
            fileAnalyzer = new FileAnalyzer();
            hijackRemover = new HijackRemover();
            ShowDashboard();
            statusTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            statusTimer.Tick += (s, e) => UpdateStatusBar();
            statusTimer.Start();
        }

        private void InitializeForm()
        {
            Text = "MazizTool — System Recovery & Anti-Malware Hub";
            Size = new Size(1320, 820);
            MinimumSize = new Size(1000, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;
            Font = Theme.UIFont;
            FormBorderStyle = FormBorderStyle.Sizable;
            DoubleBuffered = true;
            SetDarkTitleBar();
        }

        private void SetDarkTitleBar()
        {
            try
            {
                int useDark = 1;
                Win32.DwmSetWindowAttribute(Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, 4);
            }
            catch { }
        }

        private void SetupHeader()
        {
            headerPanel = new Panel
            {
                Height = HeaderHeight,
                Dock = DockStyle.Top,
                BackColor = Theme.Surface
            };

            titleLabel = new Label
            {
                Text = "██ MazizTool",
                Font = Theme.LogoFont,
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(16, 8)
            };

            statusLabel = new Label
            {
                Text = "> ready_",
                Font = Theme.MonoFont,
                ForeColor = Theme.AccentDim,
                AutoSize = true,
                Location = new Point(SidebarWidth + 16, HeaderHeight - 22),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(pen, 0, HeaderHeight - 1, headerPanel.Width, HeaderHeight - 1);
            };
            Controls.Add(headerPanel);
            Controls.Add(statusLabel);
            statusLabel.BringToFront();
        }

        private void SetupSidebar()
        {
            sidebarPanel = new Panel
            {
                Width = SidebarWidth,
                Dock = DockStyle.Left,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 8, 0, 0)
            };

            var logoPanel = new Panel
            {
                Height = 64,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };
            var logoLabel = new Label
            {
                Text = "▓▓▓\nMZ",
                Font = new Font("Consolas", 16f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = false,
                Size = new Size(40, 48),
                Location = new Point(12, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var logoNameLabel = new Label
            {
                Text = "MAZIZTOOL",
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(58, 14)
            };
            var logoVerLabel = new Label
            {
                Text = "// recovery_hub v2.0",
                Font = new Font("Consolas", 8f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(58, 30)
            };
            var logoStatusLabel = new Label
            {
                Text = "[ ROOT ]",
                Font = new Font("Consolas", 8f, FontStyle.Bold),
                ForeColor = Theme.Success,
                AutoSize = true,
                Location = new Point(58, 44)
            };
            logoPanel.Controls.Add(logoLabel);
            logoPanel.Controls.Add(logoNameLabel);
            logoPanel.Controls.Add(logoVerLabel);
            logoPanel.Controls.Add(logoStatusLabel);
            sidebarPanel.Controls.Add(logoPanel);

            AddSep();
            AddSectionLabel("// RECOVERY");
            AddNavButton("Dashboard", "[#]", 0);
            AddNavButton("Sys File Integrity", "[SFC]", 1);
            AddNavButton("Registry Scan", "[REG]", 2);
            AddNavButton("Service Scan", "[SVC]", 3);
            AddNavButton("Hijack Remover", "[HJK]", 4);

            AddSep();
            AddSectionLabel("// MALWARE");
            AddNavButton("Virus Scanner", "[AV]", 5);
            AddNavButton("Task Manager", "[TM]", 6);
            AddNavButton("Process Killer", "[PK]", 7);
            AddNavButton("Startup Manager", "[SU]", 8);
            AddNavButton("File Analyzer", "[PE]", 9);

            AddSep();
            AddSectionLabel("// UNLOCK");
            AddNavButton("Hotkey Fix", "[KEY]", 10);
            AddNavButton("Font Protect", "[FNT]", 11);
            AddNavButton("UAC Bypass", "[UAC]", 12);
            AddNavButton("Registry Editor", "[RE]", 13);
            AddNavButton("Hosts Editor", "[HST]", 14);

            AddSep();
            AddSectionLabel("// SYSTEM");
            AddNavButton("System Tools", "[SYS]", 15);
            AddNavButton("File Browser", "[DIR]", 16);
            AddNavButton("Explorer", "[EX]", 17);
            AddNavButton("CMD", "[>]", 18);
            AddNavButton("PowerShell", "[PS]", 19);

            Controls.Add(sidebarPanel);
        }

        private void AddSectionLabel(string text)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Consolas", 7f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Size = new Size(SidebarWidth - 8, 18),
                Dock = DockStyle.Top,
                Padding = new Padding(12, 4, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            sidebarPanel.Controls.Add(lbl);
        }

        private void AddSep()
        {
            var sep = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Theme.Border };
            sidebarPanel.Controls.Add(sep);
        }

        private void AddNavButton(string text, string tag, int index)
        {
            var btn = new Button
            {
                Text = $"  {tag}  {text}",
                FlatStyle = FlatStyle.Flat,
                Height = 30,
                Dock = DockStyle.Top,
                Font = new Font("Consolas", 9f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Surface,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = text
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.SurfaceLight;
            btn.FlatAppearance.MouseDownBackColor = Theme.AccentDark;

            btn.Click += (s, e) => NavigateTo(text);
            btn.MouseEnter += (s, e) => { if (btn != activeNavButton) btn.BackColor = Theme.SurfaceLight; };
            btn.MouseLeave += (s, e) => { if (btn != activeNavButton) btn.BackColor = Theme.Surface; };

            navButtons[text] = btn;
            sidebarPanel.Controls.Add(btn);
            sidebarPanel.Controls.SetChildIndex(btn, index);
        }

        private void SetupContent()
        {
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(16)
            };
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
            headerPanel.BringToFront();
            statusLabel.BringToFront();
        }

        private void NavigateTo(string feature)
        {
            if (activeNavButton != null)
            {
                activeNavButton.BackColor = Theme.Surface;
                activeNavButton.ForeColor = Theme.TextSecondary;
            }
            if (navButtons.TryGetValue(feature, out var btn))
            {
                btn.BackColor = Theme.AccentDark;
                btn.ForeColor = Theme.Accent;
                activeNavButton = btn;
            }

            currentFeaturePanel?.Dispose();
            currentFeaturePanel = null;

            switch (feature)
            {
                case "Dashboard": ShowDashboard(); break;
                case "Sys File Integrity": ShowSystemFileIntegrity(); break;
                case "Registry Scan": ShowRegistryScanner(); break;
                case "Service Scan": ShowServiceScanner(); break;
                case "Hijack Remover": ShowHijackRemover(); break;
                case "Virus Scanner": ShowVirusScanner(); break;
                case "Task Manager": ShowTaskManager(); break;
                case "Process Killer": ShowProcessKiller(); break;
                case "Startup Manager": ShowStartupManager(); break;
                case "File Analyzer": ShowFileAnalyzer(); break;
                case "Hotkey Fix": ShowHotkeyFix(); break;
                case "Font Protect": ShowFontProtect(); break;
                case "UAC Bypass": ShowUacBypass(); break;
                case "Registry Editor": ShowRegistryEditor(); break;
                case "Hosts Editor": ShowHostsEditor(); break;
                case "System Tools": ShowSystemTools(); break;
                case "File Browser": ShowFileBrowser(); break;
                case "Explorer": LaunchExplorer(); break;
                case "CMD": LaunchCmd(); break;
                case "PowerShell": LaunchPowerShell(); break;
            }
            statusLabel.Text = "> " + feature.ToLower().Replace(' ', '_') + "_";
        }

        private void UpdateStatusBar()
        {
            try
            {
                var proc = Process.GetCurrentProcess();
                statusLabel.Text = $"> {statusLabel.Text.Split('|')[0].Trim().TrimEnd('_')} | mem:{proc.WorkingSet64 / 1024 / 1024}MB | procs:{Process.GetProcesses().Length}_";
            }
            catch { }
        }

        private Panel CreateFeaturePanel(string title, string subtitle = "")
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                AutoScroll = true
            };

            var titleLbl = new Label
            {
                Text = $"┌─[ {title} ]",
                Font = Theme.HeaderFont,
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(8, 8)
            };

            panel.Controls.Add(titleLbl);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subLbl = new Label
                {
                    Text = $"└─ {subtitle}",
                    Font = new Font("Consolas", 8f),
                    ForeColor = Theme.TextMuted,
                    AutoSize = true,
                    Location = new Point(8, 32)
                };
                panel.Controls.Add(subLbl);
            }

            return panel;
        }

        private void ShowDashboard()
        {
            var panel = CreateFeaturePanel("Dashboard", "system overview & one-click recovery operations");
            int y = 64;

            var infoPanel = new Panel
            {
                Location = new Point(8, y),
                Size = new Size(640, 220),
                BackColor = Theme.Surface,
                Padding = new Padding(12)
            };
            infoPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.AccentDark, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, infoPanel.Width - 1, infoPanel.Height - 1);
            };
            var infoTitle = new Label
            {
                Text = "// SYSTEM_INFO",
                Font = new Font("Consolas", 8f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Location = new Point(8, 4),
                AutoSize = true
            };
            var infoTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = Theme.MonoFont,
                Location = new Point(8, 24),
                Size = new Size(620, 188),
                Text = SystemTools.GetSystemInfo()
            };
            infoPanel.Controls.Add(infoTitle);
            infoPanel.Controls.Add(infoTextBox);
            panel.Controls.Add(infoPanel);
            y += 232;

            AddQuickActionButtons(panel, y);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void AddQuickActionButtons(Panel panel, int startY)
        {
            var quickTitle = new Label
            {
                Text = "// QUICK_OPERATIONS",
                Font = new Font("Consolas", 8f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(8, startY)
            };
            panel.Controls.Add(quickTitle);

            var actions = new (string Name, string Desc, Color Color, Action Action)[]
            {
                ("FIX ALL RESTRICTIONS", "Restore TM, Regedit, hotkeys, policies", Theme.Success, () => { HotkeyRestorer.RestoreAll(); SystemTools.FixGroupPolicies(); ResultBox("All restrictions fixed.", Theme.Success); }),
                ("SFC /SCANNOW", "Verify & replace corrupted system files", Theme.Accent, () => { var f = CreateOutputForm("SFC /scannow"); _ = RunSfcInForm(f); }),
                ("DISM /RESTOREHEALTH", "Repair Windows component store", Theme.Accent, () => { var f = CreateOutputForm("DISM /RestoreHealth"); _ = RunDismInForm(f); }),
                ("FIX .EXE ASSOC", "Restore executable associations", Theme.Warning, () => { SystemTools.RestoreExeAssociations(); ResultBox("EXE associations restored.", Theme.Success); }),
                ("FIX HOSTS FILE", "Reset hosts to default + flush DNS", Theme.Info, () => { SystemTools.FixHostsFile(); ResultBox("Hosts file reset.", Theme.Success); }),
                ("RESET NETWORK", "Winsock/DNS/proxy/firewall reset", Theme.Info, () => { SystemTools.ResetNetworkSettings(); ResultBox("Network reset.", Theme.Success); }),
                ("RESTORE POINT", "Create system restore checkpoint", Theme.Warning, () => { SystemTools.CreateRestorePoint("MazizTool_Restore"); ResultBox("Restore point created.", Theme.Success); }),
                ("KILL NON-SYS PROC", "Terminate non-critical processes", Theme.Danger, () => { ProcessKiller.KillNonSystemProcesses(); ResultBox("Non-system processes killed.", Theme.Danger); }),
            };

            int y = startY + 22;
            for (int i = 0; i < actions.Length; i++)
            {
                var (name, desc, color, action) = actions[i];
                int col = i % 2;
                int row = i / 2;
                var btn = new Button
                {
                    Text = name,
                    Size = new Size(310, 38),
                    Location = new Point(8 + col * 320, y + row * 46),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Surface,
                    ForeColor = color,
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    Tag = desc
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = color;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(color.R / 6, color.G / 6, color.B / 6);
                var tooltip = new ToolTip();
                tooltip.SetToolTip(btn, desc);
                btn.Click += (s, e) => action();
                panel.Controls.Add(btn);
            }
        }

        private Form CreateOutputForm(string title)
        {
            var f = new Form
            {
                Text = $"MazizTool :: {title}",
                Size = new Size(820, 560),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.Background,
                ForeColor = Theme.TextPrimary,
                Font = Theme.MonoFont
            };
            int dark = 1;
            try { Win32.DwmSetWindowAttribute(f.Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4); } catch { }

            var header = new Label
            {
                Text = $"┌─[ {title} ]",
                Font = Theme.HeaderFont,
                ForeColor = Theme.Accent,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(8, 6, 0, 0)
            };
            var output = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.Accent,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false
            };
            var progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 8,
                Style = ProgressBarStyle.Continuous,
                ForeColor = Theme.Accent,
                BackColor = Theme.Surface
            };
            f.Controls.Add(output);
            f.Controls.Add(progressBar);
            f.Controls.Add(header);
            f.Tag = output;
            f.Show();
            return f;
        }

        private async Task RunSfcInForm(Form f)
        {
            var output = f.Tag as TextBox;
            var progress = f.Controls.OfType<ProgressBar>().FirstOrDefault();
            integrityScanner.OnOutput += (line) => BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.SelectionStart = output.TextLength; output.ScrollToCaret(); });
            integrityScanner.OnProgress += (p) => BeginInvokeIfCreated(() => { if (progress != null) progress.Value = p; });
            await integrityScanner.RunSfcScannowAsync();
            BeginInvokeIfCreated(() => { if (progress != null) progress.Value = 100; output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine); });
        }

        private async Task RunDismInForm(Form f)
        {
            var output = f.Tag as TextBox;
            var progress = f.Controls.OfType<ProgressBar>().FirstOrDefault();
            integrityScanner.OnOutput += (line) => BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.SelectionStart = output.TextLength; output.ScrollToCaret(); });
            integrityScanner.OnProgress += (p) => BeginInvokeIfCreated(() => { if (progress != null) progress.Value = p; });
            await integrityScanner.RunDismRestoreHealthAsync();
            BeginInvokeIfCreated(() => { if (progress != null) progress.Value = 100; output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine); });
        }

        private void ResultBox(string msg, Color color)
        {
            MessageBox.Show(msg, "MazizTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowSystemFileIntegrity()
        {
            var panel = CreateFeaturePanel("System File Integrity", "SFC + DISM — verify & replace tampered system files");
            int y = 64;

            var sfcBtn = NewBtn("▶ SFC /SCANNOW", Theme.Accent, 8, y, 200, 34);
            sfcBtn.Click += (s, e) => { var f = CreateOutputForm("SFC /scannow"); _ = RunSfcInForm(f); };

            var dismScanBtn = NewBtn("▶ DISM /SCANHEALTH", Theme.Info, 216, y, 200, 34);
            dismScanBtn.Click += (s, e) => { var f = CreateOutputForm("DISM /ScanHealth"); _ = RunDismScanInForm(f); };

            var dismRestBtn = NewBtn("▶ DISM /RESTOREHEALTH", Theme.Success, 424, y, 220, 34);
            dismRestBtn.Click += (s, e) => { var f = CreateOutputForm("DISM /RestoreHealth"); _ = RunDismInForm(f); };

            var cleanupBtn = NewBtn("▶ DISM /CLEANUP", Theme.Warning, 652, y, 180, 34);
            cleanupBtn.Click += (s, e) => { var f = CreateOutputForm("DISM /StartComponentCleanup"); _ = RunDismCleanupInForm(f); };

            y += 48;
            var checkBtn = NewBtn("▶ CHECK CRITICAL FILES", Theme.Accent, 8, y, 240, 34);
            checkBtn.Click += (s, e) => PopulateCriticalFilesGrid(panel, y + 44);

            var ifeoBtn = NewBtn("▶ IFEO HIJACK CHECK", Theme.Danger, 256, y, 220, 34);
            ifeoBtn.Click += (s, e) =>
            {
                var hijacked = SystemFileIntegrity.CheckImageFileExecutionOptions();
                var winlogon = SystemFileIntegrity.CheckWinlogonPersistence();
                var sb = new StringBuilder();
                sb.AppendLine("// IMAGE FILE EXECUTION OPTIONS (debugger hijack):");
                if (hijacked.Count == 0) sb.AppendLine("  [OK] No IFEO debugger hijacks");
                else foreach (var h in hijacked) sb.AppendLine("  [!] " + h);
                sb.AppendLine();
                sb.AppendLine("// WINLOGON PERSISTENCE:");
                foreach (var w in winlogon) sb.AppendLine("  " + w);
                ShowTextInPanel(panel, y + 44, sb.ToString());
            };

            var knownDllsBtn = NewBtn("▶ KNOWN DLLS CHECK", Theme.Warning, 484, y, 200, 34);
            knownDllsBtn.Click += (s, e) =>
            {
                var dlls = SystemFileIntegrity.CheckKnownDLLs();
                var sb = new StringBuilder();
                sb.AppendLine("// KNOWN DLLS REGISTRY:");
                if (dlls.Count == 0) sb.AppendLine("  [OK] All KnownDLLs entries valid");
                else foreach (var d in dlls) sb.AppendLine("  [!] " + d);
                ShowTextInPanel(panel, y + 44, sb.ToString());
            };

            panel.Controls.Add(sfcBtn);
            panel.Controls.Add(dismScanBtn);
            panel.Controls.Add(dismRestBtn);
            panel.Controls.Add(cleanupBtn);
            panel.Controls.Add(checkBtn);
            panel.Controls.Add(ifeoBtn);
            panel.Controls.Add(knownDllsBtn);

            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private async Task RunDismScanInForm(Form f)
        {
            var output = f.Tag as TextBox;
            integrityScanner.OnOutput += (line) => BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.ScrollToCaret(); });
            await integrityScanner.RunDismScanHealthAsync();
            BeginInvokeIfCreated(() => output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine));
        }

        private async Task RunDismCleanupInForm(Form f)
        {
            var output = f.Tag as TextBox;
            integrityScanner.OnOutput += (line) => BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.ScrollToCaret(); });
            await integrityScanner.RunDismStartComponentCleanupAsync();
            BeginInvokeIfCreated(() => output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine));
        }

        private void PopulateCriticalFilesGrid(Panel panel, int y)
        {
            ShowTextInPanel(panel, y, "[*] Scanning critical system files... please wait.");
            Task.Run(() =>
            {
                var results = SystemFileIntegrity.CheckCriticalSystemFiles();
                var sb = new StringBuilder();
                sb.AppendLine("// CRITICAL SYSTEM FILE VERIFICATION");
                sb.AppendLine($"  Path                                              Status       Size      Signed");
                sb.AppendLine("  " + new string('-', 100));
                foreach (var r in results)
                {
                    string status = r.Suspicious ? "[!]" : "[OK]";
                    sb.AppendLine($"  {Path.GetFileName(r.FilePath),-46} {status} {r.Status,-10} {r.Size,9:N0}  {(r.Signed ? "yes" : "NO")}");
                    if (r.Suspicious && !string.IsNullOrEmpty(r.Reason))
                        sb.AppendLine($"        └─ {r.Reason}");
                }
                BeginInvokeIfCreated(() => ShowTextInPanel(panel, y, sb.ToString()));
            });
        }

        private void ShowTextInPanel(Panel panel, int y, string text)
        {
            foreach (var c in panel.Controls.OfType<TextBox>().ToList())
            {
                panel.Controls.Remove(c);
                c.Dispose();
            }
            var box = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.Accent,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Text = text
            };
            panel.Controls.Add(box);
            box.BringToFront();
        }

        private void ShowRegistryScanner()
        {
            var panel = CreateFeaturePanel("Registry Scanner", "full registry scan for malware persistence & hijacks");
            int y = 64;

            var scanBtn = NewBtn("▶ SCAN REGISTRY", Theme.Accent, 8, y, 200, 34);
            var fixBtn = NewBtn("▶ FIX ALL POLICIES", Theme.Danger, 216, y, 200, 34);
            fixBtn.Click += (s, e) => { HotkeyRestorer.FixAllHotkeyBlocks(); SystemTools.FixGroupPolicies(); ResultBox("All restrictive policies removed.", Theme.Success); };

            y += 44;
            var statusLbl = new Label
            {
                Text = "> idle",
                Font = Theme.MonoFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(8, y),
                AutoSize = true
            };

            y += 24;
            var grid = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            grid.Columns.Add("LVL", 50);
            grid.Columns.Add("Location", 280);
            grid.Columns.Add("Value", 140);
            grid.Columns.Add("Data", 280);
            grid.Columns.Add("Description", 260);

            scanBtn.Click += (s, e) =>
            {
                grid.Items.Clear();
                statusLbl.Text = "> scanning...";
                registryScanner.OnProgress += (line) => BeginInvokeIfCreated(() => statusLbl.Text = line);
                registryScanner.OnFinding += (f) => BeginInvokeIfCreated(() =>
                {
                    var item = new ListViewItem(f.Level.ToString().ToUpper().Substring(0, 4));
                    item.SubItems.Add(f.Location);
                    item.SubItems.Add(f.Value);
                    item.SubItems.Add(f.Data);
                    item.SubItems.Add(f.Description);
                    item.ForeColor = f.Level == RegistryScanner.ThreatLevel.Malicious ? Theme.Danger :
                                     f.Level == RegistryScanner.ThreatLevel.Suspicious ? Theme.Warning :
                                     f.Level == RegistryScanner.ThreatLevel.Info ? Theme.Info : Theme.TextSecondary;
                    grid.Items.Add(item);
                });
                Task.Run(() => registryScanner.Scan());
            };

            panel.Controls.Add(scanBtn);
            panel.Controls.Add(fixBtn);
            panel.Controls.Add(statusLbl);
            panel.Controls.Add(grid);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowServiceScanner()
        {
            var panel = CreateFeaturePanel("Service Scanner", "enumerate & analyze Windows services for malware persistence");
            int y = 64;

            var scanBtn = NewBtn("▶ SCAN SERVICES", Theme.Accent, 8, y, 200, 34);
            var suspOnlyChk = new CheckBox
            {
                Text = "suspicious only",
                ForeColor = Theme.TextSecondary,
                Font = Theme.MonoFont,
                Location = new Point(220, y + 6),
                AutoSize = true,
                Checked = false
            };
            var stopBtn = NewBtn("■ STOP", Theme.Warning, 360, y, 100, 34);
            var disableBtn = NewBtn("⊗ DISABLE", Theme.Warning, 468, y, 110, 34);
            var deleteBtn = NewBtn("✖ DELETE", Theme.Danger, 586, y, 110, 34);

            y += 44;
            var statusLbl = new Label
            {
                Text = "> idle",
                Font = Theme.MonoFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(8, y),
                AutoSize = true
            };

            y += 24;
            var grid = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            grid.Columns.Add("!", 24);
            grid.Columns.Add("Name", 160);
            grid.Columns.Add("Display Name", 180);
            grid.Columns.Add("State", 80);
            grid.Columns.Add("Start", 70);
            grid.Columns.Add("Binary Path", 360);
            grid.Columns.Add("Reason", 240);

            scanBtn.Click += (s, e) =>
            {
                grid.Items.Clear();
                statusLbl.Text = "> enumerating services via WMI...";
                serviceScanner.OnProgress += (line) => BeginInvokeIfCreated(() => statusLbl.Text = line);
                serviceScanner.OnService += (svc) => BeginInvokeIfCreated(() =>
                {
                    if (suspOnlyChk.Checked && !svc.Suspicious) return;
                    var item = new ListViewItem(svc.Suspicious ? "!" : "");
                    item.SubItems.Add(svc.Name);
                    item.SubItems.Add(svc.DisplayName);
                    item.SubItems.Add(svc.State);
                    item.SubItems.Add(svc.StartMode);
                    item.SubItems.Add(svc.BinaryPath.Length > 70 ? "..." + svc.BinaryPath.Substring(svc.BinaryPath.Length - 67) : svc.BinaryPath);
                    item.SubItems.Add(svc.Reason ?? "");
                    if (svc.Suspicious) item.ForeColor = Theme.Danger;
                    item.Tag = svc;
                    grid.Items.Add(item);
                });
                Task.Run(() => serviceScanner.Scan());
            };

            void ActOnSelected(Func<string, bool> act, string verb)
            {
                foreach (ListViewItem item in grid.SelectedItems)
                {
                    if (item.Tag is ServiceScanner.ServiceInfo svc)
                        act(svc.Name);
                }
                ResultBox($"{verb} executed.", Theme.Success);
            }
            stopBtn.Click += (s, e) => ActOnSelected(ServiceScanner.StopService, "Stop");
            disableBtn.Click += (s, e) => ActOnSelected(ServiceScanner.DisableService, "Disable");
            deleteBtn.Click += (s, e) => ActOnSelected(ServiceScanner.DeleteService, "Delete");

            panel.Controls.Add(scanBtn);
            panel.Controls.Add(suspOnlyChk);
            panel.Controls.Add(stopBtn);
            panel.Controls.Add(disableBtn);
            panel.Controls.Add(deleteBtn);
            panel.Controls.Add(statusLbl);
            panel.Controls.Add(grid);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowHijackRemover()
        {
            var panel = CreateFeaturePanel("Hijack Remover", "browser/DNS/proxy/Winsock/hosts/WMI/firewall hijack detection");
            int y = 64;

            var scanBtn = NewBtn("▶ SCAN HIJACKS", Theme.Accent, 8, y, 200, 34);
            var fixProxyBtn = NewBtn("✓ FIX PROXY", Theme.Info, 216, y, 160, 34);
            var fixWinsockBtn = NewBtn("✓ FIX WINSOCK", Theme.Info, 384, y, 170, 34);
            var resetDnsBtn = NewBtn("✓ RESET DNS", Theme.Info, 562, y, 160, 34);

            y += 44;
            var statusLbl = new Label
            {
                Text = "> idle",
                Font = Theme.MonoFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(8, y),
                AutoSize = true
            };

            y += 24;
            var grid = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            grid.Columns.Add("!", 24);
            grid.Columns.Add("Category", 100);
            grid.Columns.Add("Detail", 280);
            grid.Columns.Add("Value", 280);
            grid.Columns.Add("Fix", 260);

            scanBtn.Click += (s, e) =>
            {
                grid.Items.Clear();
                statusLbl.Text = "> scanning hijack vectors...";
                hijackRemover.OnProgress += (line) => BeginInvokeIfCreated(() => statusLbl.Text = line);
                hijackRemover.OnFinding += (f) => BeginInvokeIfCreated(() =>
                {
                    var item = new ListViewItem(f.Suspicious ? "!" : "");
                    item.SubItems.Add(f.Category);
                    item.SubItems.Add(f.Detail);
                    item.SubItems.Add(f.Value);
                    item.SubItems.Add(f.Fix);
                    if (f.Suspicious) item.ForeColor = Theme.Danger;
                    grid.Items.Add(item);
                });
                Task.Run(() => hijackRemover.Scan());
            };

            fixProxyBtn.Click += (s, e) => { hijackRemover.FixProxy(); ResultBox("Proxy reset.", Theme.Success); };
            fixWinsockBtn.Click += (s, e) => { hijackRemover.FixWinsock(); ResultBox("Winsock reset (reboot needed).", Theme.Success); };
            resetDnsBtn.Click += (s, e) => { hijackRemover.ResetDns(); ResultBox("DNS reset.", Theme.Success); };

            panel.Controls.Add(scanBtn);
            panel.Controls.Add(fixProxyBtn);
            panel.Controls.Add(fixWinsockBtn);
            panel.Controls.Add(resetDnsBtn);
            panel.Controls.Add(statusLbl);
            panel.Controls.Add(grid);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowFileAnalyzer()
        {
            var panel = CreateFeaturePanel("File Analyzer", "PE header inspection, hash, signature & suspicious API detection");
            int y = 64;

            var pathInput = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(500, 28),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                PlaceholderText = "C:\\path\\to\\suspect.exe"
            };
            pathInput.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "Executables|*.exe;*.dll;*.sys;*.scr|All|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) pathInput.Text = ofd.FileName;
            };

            var analyzeBtn = NewBtn("▶ ANALYZE", Theme.Accent, 516, y, 130, 28);
            y += 40;
            var resultBox = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.Accent,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            analyzeBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(pathInput.Text) || !File.Exists(pathInput.Text))
                { ResultBox("Pick a valid file.", Theme.Warning); return; }
                resultBox.Text = "[*] Analyzing...";
                Task.Run(() =>
                {
                    var info = fileAnalyzer.Analyze(pathInput.Text);
                    var report = fileAnalyzer.FormatReport(info);
                    BeginInvokeIfCreated(() => resultBox.Text = report);
                });
            };

            panel.Controls.Add(pathInput);
            panel.Controls.Add(analyzeBtn);
            panel.Controls.Add(resultBox);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowVirusScanner()
        {
            var panel = CreateFeaturePanel("Virus Scanner", "signature + heuristic malware detection");
            int y = 64;

            var pathInput = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(400, 28),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                Text = @"C:\Users"
            };

            var scanBtn = NewBtn("▶ START SCAN", Theme.Danger, 416, y, 130, 28);
            var quickBtn = NewBtn("⚡ QUICK", Theme.Warning, 552, y, 100, 28);

            y += 36;
            var progressBar = new ProgressBar
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, 6),
                Style = ProgressBarStyle.Continuous,
                ForeColor = Theme.Accent,
                BackColor = Theme.Surface
            };
            y += 12;
            var scanStatus = new Label
            {
                Text = "> ready",
                Font = Theme.MonoFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(8, y),
                AutoSize = true
            };

            y += 24;
            var resultList = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            resultList.Columns.Add("Threat", 200);
            resultList.Columns.Add("Level", 80);
            resultList.Columns.Add("File", 380);
            resultList.Columns.Add("Description", 240);

            scanBtn.Click += async (s, e) =>
            {
                if (scanner.IsScanning) { scanner.StopScan(); scanBtn.Text = "▶ START SCAN"; return; }
                scanBtn.Text = "■ STOP";
                resultList.Items.Clear();
                progressBar.Value = 0;
                await scanner.ScanAsync(pathInput.Text);
                scanBtn.Text = "▶ START SCAN";
            };

            quickBtn.Click += async (s, e) =>
            {
                if (scanner.IsScanning) return;
                resultList.Items.Clear();
                await scanner.ScanAsync(Path.GetTempPath());
                if (!scanner.IsScanning) await scanner.ScanAsync(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            };

            panel.Controls.Add(pathInput);
            panel.Controls.Add(scanBtn);
            panel.Controls.Add(quickBtn);
            panel.Controls.Add(progressBar);
            panel.Controls.Add(scanStatus);
            panel.Controls.Add(resultList);
            contentPanel.Tag = resultList;

            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
            contentPanel.Tag = resultList;
        }

        private void OnScanProgress(string file, int count)
        {
            BeginInvokeIfCreated(() =>
            {
                var panel = currentFeaturePanel;
                if (panel == null) return;
                var status = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith(">"));
                if (status != null) status.Text = $"> scanning ({count} files) ...";
            });
        }

        private void OnThreatFound(VirusScanner.ScanResult result)
        {
            BeginInvokeIfCreated(() =>
            {
                var resultList = contentPanel.Tag as ListView;
                if (resultList == null) return;
                var item = new ListViewItem(result.ThreatName);
                item.SubItems.Add(result.Level.ToString());
                item.SubItems.Add(result.FilePath);
                item.SubItems.Add(result.Description);
                item.ForeColor = result.Level == VirusScanner.ThreatLevel.Critical ? Theme.Danger :
                                 result.Level == VirusScanner.ThreatLevel.High ? Theme.Warning : Theme.TextSecondary;
                resultList.Items.Add(item);
            });
        }

        private void OnScanComplete()
        {
            BeginInvokeIfCreated(() =>
            {
                var panel = currentFeaturePanel;
                if (panel == null) return;
                var status = panel.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith(">"));
                if (status != null) status.Text = $"> scan_complete :: files:{scanner.FilesScanned} threats:{scanner.ThreatsFound}";
            });
        }

        private void ShowTaskManager()
        {
            var panel = CreateFeaturePanel("Task Manager", "internal process management");
            int y = 64;

            var refreshBtn = NewBtn("↻ REFRESH", Theme.Info, 8, y, 110, 28);
            var killBtn = NewBtn("✖ KILL", Theme.Danger, 124, y, 100, 28);
            var killSysBtn = NewBtn("⚡ KILL NON-SYS", Theme.Warning, 230, y, 140, 28);

            y += 36;
            var listView = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            listView.Columns.Add("PID", 70);
            listView.Columns.Add("Name", 180);
            listView.Columns.Add("Path", 360);
            listView.Columns.Add("Mem(MB)", 90);
            listView.Columns.Add("Threads", 70);

            refreshBtn.Click += (s, e) => RefreshTaskList(listView);
            killBtn.Click += (s, e) =>
            {
                foreach (ListViewItem item in listView.SelectedItems)
                    if (item.Tag is ProcessKiller.ProcessInfo pi) ProcessKiller.KillProcess(pi.Id);
                RefreshTaskList(listView);
            };
            killSysBtn.Click += (s, e) => { ProcessKiller.KillNonSystemProcesses(); RefreshTaskList(listView); };

            panel.Controls.Add(refreshBtn);
            panel.Controls.Add(killBtn);
            panel.Controls.Add(killSysBtn);
            panel.Controls.Add(listView);
            RefreshTaskList(listView);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void RefreshTaskList(ListView lv)
        {
            lv.Items.Clear();
            foreach (var proc in ProcessKiller.GetAllProcesses())
            {
                var item = new ListViewItem(proc.Id.ToString());
                item.SubItems.Add(proc.Name);
                item.SubItems.Add(proc.Path.Length > 60 ? "..." + proc.Path.Substring(proc.Path.Length - 57) : proc.Path);
                item.SubItems.Add((proc.MemoryBytes / 1024.0 / 1024.0).ToString("F1"));
                item.SubItems.Add(proc.Threads.ToString());
                item.Tag = proc;
                lv.Items.Add(item);
            }
        }

        private void ShowProcessKiller()
        {
            var panel = CreateFeaturePanel("Process Killer", "force-kill protected, hidden or malicious processes");
            int y = 64;

            var nameInput = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(280, 28),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                PlaceholderText = "process name (e.g. malware.exe)"
            };
            var killByNameBtn = NewBtn("✖ KILL BY NAME", Theme.Danger, 296, y, 160, 28);
            killByNameBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(nameInput.Text))
                { ProcessKiller.KillProcessByName(nameInput.Text.Trim()); ResultBox($"Killed: {nameInput.Text.Trim()}", Theme.Danger); }
            };

            y += 40;
            var killAllBtn = NewBtn("⚡ KILL ALL NON-SYSTEM", Theme.Warning, 8, y, 220, 36);
            killAllBtn.Click += (s, e) => { ProcessKiller.KillNonSystemProcesses(); ResultBox("Done.", Theme.Danger); };

            y += 48;
            var suspLbl = new Label
            {
                Text = "// SUSPICIOUS_PROCESSES",
                Font = new Font("Consolas", 8f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Location = new Point(8, y)
            };
            y += 22;
            var suspList = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, 260),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };
            suspList.Columns.Add("PID", 70);
            suspList.Columns.Add("Name", 200);
            suspList.Columns.Add("Path", 460);
            foreach (var proc in VirusScanner.GetSuspiciousProcesses())
            {
                try
                {
                    var item = new ListViewItem(proc.Id.ToString());
                    item.SubItems.Add(proc.ProcessName);
                    item.SubItems.Add(proc.MainModule?.FileName ?? "");
                    item.ForeColor = Theme.Danger;
                    suspList.Items.Add(item);
                }
                catch { }
            }

            y += 268;
            var killSuspBtn = NewBtn("✖ KILL ALL SUSPICIOUS", Theme.Danger, 8, y, 200, 32);
            killSuspBtn.Click += (s, e) =>
            {
                foreach (var proc in VirusScanner.GetSuspiciousProcesses())
                    try { proc.Kill(); } catch { }
                ResultBox("Suspicious processes killed.", Theme.Danger);
            };

            panel.Controls.Add(nameInput);
            panel.Controls.Add(killByNameBtn);
            panel.Controls.Add(killAllBtn);
            panel.Controls.Add(suspLbl);
            panel.Controls.Add(suspList);
            panel.Controls.Add(killSuspBtn);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowStartupManager()
        {
            var panel = CreateFeaturePanel("Startup Manager", "view & remove autorun entries (registry, folders, tasks)");
            int y = 64;

            var refreshBtn = NewBtn("↻ REFRESH", Theme.Info, 8, y, 110, 28);
            var removeBtn = NewBtn("✖ REMOVE", Theme.Danger, 124, y, 110, 28);
            var cleanupBtn = NewBtn("🧹 CLEAN ALL", Theme.Warning, 240, y, 140, 28);

            y += 36;
            var lv = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            lv.Columns.Add("Name", 180);
            lv.Columns.Add("Command", 360);
            lv.Columns.Add("Location", 160);
            lv.Columns.Add("Status", 80);

            void RefreshList()
            {
                lv.Items.Clear();
                foreach (var entry in StartupManager.GetStartupEntries())
                {
                    var item = new ListViewItem(entry.Name);
                    item.SubItems.Add(entry.Command.Length > 60 ? entry.Command.Substring(0, 57) + "..." : entry.Command);
                    item.SubItems.Add(entry.Location);
                    item.SubItems.Add(entry.Enabled ? "enabled" : "disabled");
                    item.Tag = entry;
                    lv.Items.Add(item);
                }
            }
            refreshBtn.Click += (s, e) => RefreshList();
            removeBtn.Click += (s, e) =>
            {
                foreach (ListViewItem item in lv.SelectedItems)
                    if (item.Tag is StartupManager.StartupEntry en) StartupManager.RemoveStartupEntry(en);
                RefreshList();
            };
            cleanupBtn.Click += (s, e) =>
            {
                int n = 0;
                foreach (var en in StartupManager.GetStartupEntries())
                    if (StartupManager.RemoveStartupEntry(en)) n++;
                RefreshList();
                ResultBox($"Removed {n} entries.", Theme.Success);
            };

            panel.Controls.Add(refreshBtn);
            panel.Controls.Add(removeBtn);
            panel.Controls.Add(cleanupBtn);
            panel.Controls.Add(lv);
            RefreshList();
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowRegistryEditor()
        {
            var panel = CreateFeaturePanel("Registry Editor", "quick navigation & repair");
            int y = 64;

            var pathInput = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(500, 28),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                Text = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies"
            };
            var navBtn = NewBtn("▶ GO", Theme.Accent, 516, y, 100, 28);
            y += 40;
            var resultBox = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, 240),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.Accent,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false
            };

            navBtn.Click += (s, e) => { resultBox.Clear(); NavigateRegistry(pathInput.Text, resultBox); };

            y += 252;
            var regEditBtn = NewBtn("▶ OPEN REGEDIT", Theme.Info, 8, y, 160, 28);
            regEditBtn.Click += (s, e) => { HotkeyRestorer.RestoreRegistryEditor(); try { Process.Start("regedit.exe"); } catch { } };
            var fixPolBtn = NewBtn("✓ FIX POLICIES", Theme.Warning, 176, y, 160, 28);
            fixPolBtn.Click += (s, e) => { SystemTools.FixGroupPolicies(); HotkeyRestorer.FixAllHotkeyBlocks(); ResultBox("Policies fixed.", Theme.Success); };

            panel.Controls.Add(pathInput);
            panel.Controls.Add(navBtn);
            panel.Controls.Add(resultBox);
            panel.Controls.Add(regEditBtn);
            panel.Controls.Add(fixPolBtn);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void NavigateRegistry(string path, TextBox resultBox)
        {
            try
            {
                Microsoft.Win32.RegistryKey key = null;
                string subKey = "";
                if (path.StartsWith("HKEY_CURRENT_USER\\") || path.StartsWith("HKCU\\"))
                { subKey = path.Replace("HKEY_CURRENT_USER\\", "").Replace("HKCU\\", ""); key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey); }
                else if (path.StartsWith("HKEY_LOCAL_MACHINE\\") || path.StartsWith("HKLM\\"))
                { subKey = path.Replace("HKEY_LOCAL_MACHINE\\", "").Replace("HKLM\\", ""); key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subKey); }
                else if (path.StartsWith("HKEY_CLASSES_ROOT\\") || path.StartsWith("HKCR\\"))
                { subKey = path.Replace("HKEY_CLASSES_ROOT\\", "").Replace("HKCR\\", ""); key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(subKey); }

                if (key == null && !string.IsNullOrEmpty(subKey)) { resultBox.Text = "[!] Key not found / access denied."; return; }
                var sb = new StringBuilder();
                sb.AppendLine($"// {path}");
                if (key != null)
                {
                    sb.AppendLine($"// SubKeys: {key.SubKeyCount}");
                    foreach (var n in key.GetSubKeyNames()) sb.AppendLine($"  [DIR] {n}");
                    sb.AppendLine($"// Values: {key.ValueCount}");
                    foreach (var n in key.GetValueNames())
                    {
                        var v = key.GetValue(n);
                        sb.AppendLine($"  {n} = {v}");
                    }
                    key.Close();
                }
                resultBox.Text = sb.ToString();
            }
            catch (Exception ex) { resultBox.Text = "[!] " + ex.Message; }
        }

        private void ShowFileBrowser()
        {
            var panel = CreateFeaturePanel("File Browser", "browse, search & manage files");
            int y = 64;

            var pathInput = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(500, 28),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                Text = @"C:\"
            };
            var browseBtn = NewBtn("📁", Theme.Info, 516, y, 40, 28);
            y += 34;
            var searchInput = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(220, 28),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                PlaceholderText = "search files..."
            };
            var searchBtn = NewBtn("🔍", Theme.Info, 234, y, 40, 28);

            y += 40;
            var fileList = new ListView
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, panel.Height - y - 16),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            fileList.Columns.Add("Name", 320);
            fileList.Columns.Add("Size", 100);
            fileList.Columns.Add("Modified", 160);
            fileList.Columns.Add("Type", 100);
            fileList.DoubleClick += (s, e) =>
            {
                if (fileList.SelectedItems.Count == 0) return;
                var tag = fileList.SelectedItems[0].Tag as string;
                if (tag == null) return;
                if (Directory.Exists(tag)) { pathInput.Text = tag; RefreshFileList(fileList, tag); }
                else if (File.Exists(tag)) { try { Process.Start(new ProcessStartInfo { FileName = tag, UseShellExecute = true }); } catch { } }
            };

            browseBtn.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                    if (fbd.ShowDialog() == DialogResult.OK) pathInput.Text = fbd.SelectedPath;
                RefreshFileList(fileList, pathInput.Text);
            };
            searchBtn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(searchInput.Text)) return;
                fileList.Items.Clear();
                try
                {
                    var results = Directory.GetFiles(pathInput.Text, $"*{searchInput.Text}*", SearchOption.AllDirectories).Take(200);
                    foreach (var file in results)
                    {
                        var fi = new FileInfo(file);
                        var item = new ListViewItem(fi.Name);
                        item.SubItems.Add(FormatSize(fi.Length));
                        item.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        item.SubItems.Add(fi.Extension);
                        item.Tag = file;
                        fileList.Items.Add(item);
                    }
                }
                catch { }
            };
            pathInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshFileList(fileList, pathInput.Text); };

            panel.Controls.Add(pathInput);
            panel.Controls.Add(browseBtn);
            panel.Controls.Add(searchInput);
            panel.Controls.Add(searchBtn);
            panel.Controls.Add(fileList);
            RefreshFileList(fileList, pathInput.Text);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void RefreshFileList(ListView list, string path)
        {
            list.Items.Clear();
            if (!Directory.Exists(path))
            {
                try { var p = Directory.GetParent(path); if (p != null) path = p.FullName; } catch { return; }
            }
            try
            {
                var up = new ListViewItem(".. [UP]");
                up.ForeColor = Theme.Accent; up.Tag = path; list.Items.Add(up);
                foreach (var dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        var di = new DirectoryInfo(dir);
                        if (di.Attributes.HasFlag(FileAttributes.Hidden) && di.Attributes.HasFlag(FileAttributes.System)) continue;
                        var item = new ListViewItem($"[DIR] {di.Name}");
                        item.ForeColor = Theme.Accent;
                        item.SubItems.Add("<DIR>");
                        item.SubItems.Add(di.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        item.SubItems.Add("Folder");
                        item.Tag = dir;
                        list.Items.Add(item);
                    }
                    catch { }
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        var item = new ListViewItem(fi.Name);
                        item.SubItems.Add(FormatSize(fi.Length));
                        item.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        item.SubItems.Add(fi.Extension);
                        item.Tag = file;
                        list.Items.Add(item);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0; double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
            return $"{size:F1} {sizes[order]}";
        }

        private void ShowHostsEditor()
        {
            var panel = CreateFeaturePanel("Hosts Editor", "edit & reset Windows hosts file");
            int y = 64;
            var hostsPath = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");
            var hostsContent = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(panel.Width - 40, 320),
                Multiline = true,
                BackColor = Color.FromArgb(6, 10, 6),
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10f),
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                Text = File.Exists(hostsPath) ? File.ReadAllText(hostsPath) : ""
            };
            y += 330;
            var saveBtn = NewBtn("💾 SAVE", Theme.Success, 8, y, 100, 28);
            var reloadBtn = NewBtn("↻ RELOAD", Theme.Info, 116, y, 100, 28);
            var resetBtn = NewBtn("✓ RESET", Theme.Warning, 224, y, 130, 28);
            var flushBtn = NewBtn("🧹 FLUSH DNS", Theme.Accent, 362, y, 130, 28);

            saveBtn.Click += (s, e) =>
            {
                try
                {
                    var attr = File.GetAttributes(hostsPath);
                    File.SetAttributes(hostsPath, attr & ~FileAttributes.ReadOnly);
                    File.WriteAllText(hostsPath, hostsContent.Text);
                    File.SetAttributes(hostsPath, attr);
                    Process.Start(new ProcessStartInfo { FileName = "ipconfig.exe", Arguments = "/flushdns", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000);
                    ResultBox("Hosts saved & DNS flushed.", Theme.Success);
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "MazizTool"); }
            };
            reloadBtn.Click += (s, e) => { if (File.Exists(hostsPath)) hostsContent.Text = File.ReadAllText(hostsPath); };
            resetBtn.Click += (s, e) => { SystemTools.FixHostsFile(); hostsContent.Text = File.ReadAllText(hostsPath); ResultBox("Hosts reset.", Theme.Success); };
            flushBtn.Click += (s, e) => { Process.Start(new ProcessStartInfo { FileName = "ipconfig.exe", Arguments = "/flushdns", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000); ResultBox("DNS flushed.", Theme.Success); };

            panel.Controls.Add(hostsContent);
            panel.Controls.Add(saveBtn);
            panel.Controls.Add(reloadBtn);
            panel.Controls.Add(resetBtn);
            panel.Controls.Add(flushBtn);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowUacBypass()
        {
            var panel = CreateFeaturePanel("UAC Bypass", "elevate privileges via multiple known techniques");
            int y = 64;

            var lbl = new Label { Text = "// command_to_elevate:", Font = new Font("Consolas", 8f, FontStyle.Bold), ForeColor = Theme.Accent, AutoSize = true, Location = new Point(8, y - 16) };
            var cmdInput = new TextBox
            {
                Location = new Point(8, y),
                Size = new Size(400, 28),
                BackColor = Theme.InputBg,
                ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f),
                Text = "cmd.exe"
            };

            var methods = UacBypass.GetAvailableMethods();
            for (int i = 0; i < methods.Count; i++)
            {
                var m = methods[i];
                var btn = NewBtn($"🔓 {m}", Theme.Accent, 8, y + 36 + i * 38, 280, 32);
                btn.Click += (s, e) => ResultBox(UacBypass.ExecuteWithUacBypass(cmdInput.Text, m) ? $"Bypass via {m} executed." : $"Bypass {m} failed.", Theme.Accent);
                panel.Controls.Add(btn);
            }

            int y2 = y + 36 + methods.Count * 38 + 8;
            var runAsBtn = NewBtn("🛡 RUN AS ADMIN", Theme.Warning, 8, y2, 280, 32);
            runAsBtn.Click += (s, e) => { UacBypass.ElevateProcess(cmdInput.Text); ResultBox("Elevation requested.", Theme.Warning); };
            var privBtn = NewBtn("🔑 ENABLE SeDebugPriv", Theme.Info, 8, y2 + 40, 280, 32);
            privBtn.Click += (s, e) =>
            {
                UacBypass.EnablePrivilege("SeDebugPrivilege");
                UacBypass.EnablePrivilege("SeTakeOwnershipPrivilege");
                UacBypass.EnablePrivilege("SeBackupPrivilege");
                UacBypass.EnablePrivilege("SeRestorePrivilege");
                UacBypass.EnablePrivilege("SeSecurityPrivilege");
                ResultBox("Privileges enabled.", Theme.Info);
            };

            panel.Controls.Add(lbl);
            panel.Controls.Add(cmdInput);
            panel.Controls.Add(runAsBtn);
            panel.Controls.Add(privBtn);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowHotkeyFix()
        {
            var panel = CreateFeaturePanel("Hotkey Restorer", "unblock disabled hotkeys: Alt+Tab, Ctrl+Alt+Del, WinKey, TaskMgr, Regedit");
            int y = 64;

            var actions = new (string, string, Color, Action)[]
            {
                ("FIX ALL RESTRICTIONS", "Restore all blocked keys, TM, Regedit", Theme.Success, () => { HotkeyRestorer.RestoreAll(); SystemTools.FixGroupPolicies(); ResultBox("All restrictions removed.", Theme.Success); }),
                ("ENABLE ALT+TAB", "Restore Alt+Tab switcher", Theme.Info, () => { HotkeyRestorer.EnableAltTab(); ResultBox("Alt+Tab restored.", Theme.Info); }),
                ("ENABLE CTRL+ALT+DEL", "Restore security screen", Theme.Info, () => { HotkeyRestorer.EnableCtrlAltDel(); ResultBox("Ctrl+Alt+Del restored.", Theme.Info); }),
                ("RESTORE TASKMGR", "Unblock taskmgr.exe", Theme.Warning, () => { HotkeyRestorer.RestoreTaskManager(); ResultBox("Task Manager unblocked.", Theme.Warning); }),
                ("RESTORE REGEDIT", "Unblock regedit.exe", Theme.Warning, () => { HotkeyRestorer.RestoreRegistryEditor(); ResultBox("Regedit unblocked.", Theme.Warning); }),
                ("RESTORE HOTKEYS", "Enable WinKeys", Theme.Accent, () => { HotkeyRestorer.RestoreHotkeys(); ResultBox("Hotkeys restored.", Theme.Accent); }),
                ("FIX GROUP POLICIES", "Reset restrictive policies", Theme.Danger, () => { SystemTools.FixGroupPolicies(); ResultBox("Policies reset.", Theme.Danger); }),
                ("RESTORE HIDDEN FILES", "Show hidden + system files", Theme.Info, () => { SystemTools.RestoreHiddenFiles(); ResultBox("Hidden files shown.", Theme.Info); }),
            };

            for (int i = 0; i < actions.Length; i++)
            {
                var (name, desc, color, action) = actions[i];
                var btn = new Button
                {
                    Text = name,
                    Size = new Size(310, 38),
                    Location = new Point(8 + (i % 2) * 320, y + (i / 2) * 46),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Surface,
                    ForeColor = color,
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = color;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(color.R / 6, color.G / 6, color.B / 6);
                var tt = new ToolTip(); tt.SetToolTip(btn, desc);
                btn.Click += (s, e) => action();
                panel.Controls.Add(btn);
            }

            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowFontProtect()
        {
            var panel = CreateFeaturePanel("Font Protection", "restore system fonts if tampered by malware");
            int y = 64;
            bool tampered = FontProtector.IsSystemFontTampered();
            var statusLbl = new Label
            {
                Text = tampered ? "[!] FONT TAMPERING DETECTED" : "[OK] system fonts appear normal",
                Font = new Font("Consolas", 10f, FontStyle.Bold),
                ForeColor = tampered ? Theme.Danger : Theme.Success,
                AutoSize = true,
                Location = new Point(8, y)
            };
            var restoreBtn = NewBtn("🔤 RESTORE SYSTEM FONTS", Theme.Accent, 8, y + 36, 260, 36);
            restoreBtn.Click += (s, e) => { FontProtector.RestoreSystemFonts(); ResultBox("Fonts restored. Reboot recommended.", Theme.Success); };
            var info = new Label
            {
                Text = "// malware may substitute Segoe UI with blank fonts to hide text.\n// this restores registry font settings & broadcasts WM_FONTCHANGE.",
                Font = Theme.MonoFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                Location = new Point(8, y + 84)
            };

            panel.Controls.Add(statusLbl);
            panel.Controls.Add(restoreBtn);
            panel.Controls.Add(info);
            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void ShowSystemTools()
        {
            var panel = CreateFeaturePanel("System Tools", "advanced recovery & repair utilities");
            int y = 64;

            var tools = new (string, string, Color, Action)[]
            {
                ("RESTORE .EXE ASSOC", "Fix executable associations", Theme.Warning, () => { SystemTools.RestoreExeAssociations(); ResultBox("Done.", Theme.Warning); }),
                ("RESET NETWORK", "Winsock+IP+DNS+Proxy+Firewall", Theme.Info, () => { SystemTools.ResetNetworkSettings(); ResultBox("Done.", Theme.Info); }),
                ("CLEAR TEMP", "Clean temp + cache", Theme.Accent, () => { SystemTools.ClearTempFiles(); ResultBox("Done.", Theme.Accent); }),
                ("CREATE RESTORE PT", "System restore checkpoint", Theme.Success, () => { SystemTools.CreateRestorePoint("MazizTool"); ResultBox("Done.", Theme.Success); }),
                ("SFC /SCANNOW", "Repair Windows files", Theme.Danger, () => SystemTools.RestoreSystemFiles()),
                ("CHKDSK C: /f /r", "Check disk (reboot)", Theme.Warning, () => SystemTools.CheckDisk()),
                ("SAFE MODE +NET", "Boot Safe Mode w/ Network", Theme.Info, () => { SystemTools.EnableSafeModeNetworking(); ResultBox("Safe mode enabled. Reboot to enter.", Theme.Info); }),
                ("DISABLE SAFE MODE", "Remove safe mode flag", Theme.Accent, () => { SystemTools.DisableSafeMode(); ResultBox("Done.", Theme.Accent); }),
                ("FIX HOSTS", "Reset hosts to default", Theme.Warning, () => { SystemTools.FixHostsFile(); ResultBox("Done.", Theme.Warning); }),
                ("SHOW HIDDEN FILES", "Show hidden + system files", Theme.Info, () => { SystemTools.RestoreHiddenFiles(); ResultBox("Done.", Theme.Info); }),
            };

            for (int i = 0; i < tools.Length; i++)
            {
                var (name, desc, color, action) = tools[i];
                var btn = new Button
                {
                    Text = name,
                    Size = new Size(310, 38),
                    Location = new Point(8 + (i % 2) * 320, y + (i / 2) * 46),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Surface,
                    ForeColor = color,
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = color;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(color.R / 6, color.G / 6, color.B / 6);
                var tt = new ToolTip(); tt.SetToolTip(btn, desc);
                btn.Click += (s, e) => action();
                panel.Controls.Add(btn);
            }

            currentFeaturePanel = panel;
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(panel);
        }

        private void LaunchExplorer() { try { Process.Start("explorer.exe"); } catch { } }
        private void LaunchCmd() { try { Process.Start(new ProcessStartInfo("cmd.exe") { UseShellExecute = true, Verb = "runas" }); } catch { } }
        private void LaunchPowerShell() { try { Process.Start(new ProcessStartInfo("powershell.exe") { UseShellExecute = true, Verb = "runas" }); } catch { } }

        private Button NewBtn(string text, Color border, int x, int y, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Surface,
                ForeColor = border,
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = border;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(border.R / 6, border.G / 6, border.B / 6);
            return btn;
        }

        private void BeginInvokeIfCreated(Action action)
        {
            try { if (IsHandleCreated && !IsDisposed) BeginInvoke(action); } catch { }
        }
    }
}
