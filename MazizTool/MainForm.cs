using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MazizTool.Controls;
using MazizTool.Features;
using MazizTool.Native;

namespace MazizTool
{
    public partial class MainForm : Form
    {
        private Panel topBar;
        private Panel iconSidebar;
        private Panel contentPanel;
        private Panel bottomBar;
        private Panel currentView;
        private Dictionary<string, IconButton> iconButtons = new Dictionary<string, IconButton>();
        private IconButton activeIcon;
        private Label moduleTitleLabel;
        private Label moduleSubLabel;
        private VirusScanner scanner;
        private SystemFileIntegrity integrityScanner;
        private RegistryScanner registryScanner;
        private ServiceScanner serviceScanner;
        private FileAnalyzer fileAnalyzer;
        private HijackRemover hijackRemover;
        private System.Windows.Forms.Timer statusTimer;
        private Form colorPopover;

        private const int TopBarHeight = 56;
        private const int SidebarWidth = 64;
        private const int BottomBarHeight = 26;

        public MainForm()
        {
            try
            {
                InitializeForm();
                SetupTopBar();
                SetupIconSidebar();
                SetupContent();
                SetupBottomBar();
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
            catch (Exception ex)
            {
                MessageBox.Show("Constructor error:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "MazizTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeForm()
        {
            Text = "MazizTool";
            Size = new Size(1340, 840);
            MinimumSize = new Size(1020, 640);
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
            try { int d = 1; Win32.DwmSetWindowAttribute(Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref d, 4); } catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            GraphicsExt.DrawGradientBg(e.Graphics, ClientRectangle, Theme.BackgroundTop, Theme.Background);
        }

        private void SetupTopBar()
        {
            topBar = new Panel
            {
                Height = TopBarHeight,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };
            topBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Theme.Border, 1))
                    g.DrawLine(pen, 0, TopBarHeight - 1, topBar.Width, TopBarHeight - 1);

                var logoRect = new Rectangle(SidebarWidth + 20, 12, 32, 32);
                var logoPath = GraphicsExt.RoundedRect(logoRect, 9);
                using (var brush = new LinearGradientBrush(logoRect, Theme.Accent, Theme.AccentDark, 90f))
                    g.FillPath(brush, logoPath);
                logoPath.Dispose();
                TextRenderer.DrawText(g, "MZ", new Font("Segoe UI", 11f, FontStyle.Bold), logoRect,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            moduleTitleLabel = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(SidebarWidth + 64, 10),
                Name = "moduleTitle",
                BackColor = Color.Transparent
            };
            moduleSubLabel = new Label
            {
                Text = "system overview",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(SidebarWidth + 64, 33),
                Name = "moduleSub",
                BackColor = Color.Transparent
            };

            var themeBtn = new Panel
            {
                Size = new Size(40, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(topBar.Width - 56, 12),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            themeBtn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var swatchRect = new Rectangle(8, 8, 24, 16);
                var swatchPath = GraphicsExt.RoundedRect(swatchRect, 4);
                using (var brush = new LinearGradientBrush(swatchRect, Theme.Accent, Theme.AccentDark, 0f))
                    g.FillPath(brush, swatchPath);
                using (var pen = new Pen(Theme.BorderLight, 1))
                    g.DrawPath(pen, swatchPath);
                swatchPath.Dispose();
                TextRenderer.DrawText(g, "▾", new Font("Segoe UI", 8f),
                    new Rectangle(28, 0, 12, 32), Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            themeBtn.Click += (s, e) => ToggleThemePopover(themeBtn);
            themeBtn.Name = "themeBtn";
            topBar.Resize += (s, e) => { themeBtn.Location = new Point(topBar.Width - 56, 12); };

            topBar.Controls.Add(moduleTitleLabel);
            topBar.Controls.Add(moduleSubLabel);
            topBar.Controls.Add(themeBtn);
            Controls.Add(topBar);
        }

        private void ToggleThemePopover(Control anchor)
        {
            if (colorPopover != null && !colorPopover.IsDisposed)
            {
                colorPopover.Close();
                colorPopover = null;
                return;
            }

            var pop = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                BackColor = Theme.SurfaceElevated,
                Size = new Size(240, 300),
                Location = anchor.PointToScreen(new Point(-200, 36))
            };
            try { int d = 1; Win32.DwmSetWindowAttribute(pop.Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref d, 4); } catch { }
            pop.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pop.Width - 1, pop.Height - 1);
                var path = GraphicsExt.RoundedRect(rect, 12);
                using (var brush = new SolidBrush(Theme.SurfaceElevated))
                    g.FillPath(brush, path);
                using (var pen = new Pen(Theme.BorderLight, 1))
                    g.DrawPath(pen, path);
                path.Dispose();
            };

            var title = new Label
            {
                Text = "Accent color",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(16, 14)
            };
            pop.Controls.Add(title);

            for (int i = 0; i < Theme.Presets.Count; i++)
            {
                var preset = Theme.Presets[i];
                var idx = i;
                int row = i / 2, col = i % 2;
                var item = new Panel
                {
                    Size = new Size(104, 44),
                    Location = new Point(12 + col * 112, 40 + row * 50),
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                item.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    var swatchRect = new Rectangle(6, 12, 20, 20);
                    using (var path = GraphicsExt.RoundedRect(swatchRect, 6))
                    using (var brush = new SolidBrush(preset.Accent))
                        g.FillPath(brush, path);
                    if (Theme.CurrentPresetIndex == idx)
                    {
                        using (var pen = new Pen(Color.White, 2))
                        using (var path2 = GraphicsExt.RoundedRect(Rectangle.Inflate(swatchRect, 1, 1), 7))
                            g.DrawPath(pen, path2);
                    }
                    TextRenderer.DrawText(g, preset.Name, new Font("Segoe UI", 8.5f),
                        new Rectangle(32, 0, 68, 44), Theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                };
                item.Click += (s, e) =>
                {
                    Theme.ApplyPreset(idx);
                    pop.Close();
                    colorPopover = null;
                    Invalidate(true);
                    foreach (Control c in Controls) c.Invalidate(true);
                };
                pop.Controls.Add(item);
            }

            colorPopover = pop;
            pop.Deactivate += (s, e) => { pop.Close(); colorPopover = null; };
            pop.Show(this);
        }

        private void SetupIconSidebar()
        {
            iconSidebar = new Panel
            {
                Width = SidebarWidth,
                Dock = DockStyle.Left,
                BackColor = Color.Transparent
            };
            iconSidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(pen, iconSidebar.Width - 1, 0, iconSidebar.Width - 1, iconSidebar.Height);
            };

            var logoBtn = new Panel
            {
                Size = new Size(48, 48),
                Location = new Point(8, 8),
                BackColor = Color.Transparent
            };
            logoBtn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(2, 2, 44, 44);
                using (var path = GraphicsExt.RoundedRect(rect, 12))
                using (var brush = new LinearGradientBrush(rect, Theme.Accent, Theme.AccentDark, 90f))
                    g.FillPath(brush, path);
                TextRenderer.DrawText(g, "MZ", new Font("Segoe UI", 14f, FontStyle.Bold), rect,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            iconSidebar.Controls.Add(logoBtn);

            AddIcon("Dashboard", "◍", "Dashboard");
            AddIcon("Sys File Integrity", "◆", "SFC / DISM");
            AddIcon("Registry Scan", "ƒ", "Registry Scan");
            AddIcon("Service Scan", "⚙", "Service Scan");
            AddIcon("Hijack Remover", "⬚", "Hijack Remover");
            AddIcon("Virus Scanner", "🛡", "Virus Scanner");
            AddIcon("Task Manager", "▤", "Task Manager");
            AddIcon("Process Killer", "✖", "Process Killer");
            AddIcon("Startup Manager", "↻", "Startup Manager");
            AddIcon("File Analyzer", "⌬", "File Analyzer");
            AddIcon("Hotkey Fix", "⌨", "Hotkey Fix");
            AddIcon("Font Protect", "Aa", "Font Protect");
            AddIcon("UAC Bypass", "🔓", "UAC Bypass");
            AddIcon("Registry Editor", "📝", "Reg Editor");
            AddIcon("Hosts Editor", "🌐", "Hosts Editor");
            AddIcon("System Tools", "🔧", "System Tools");
            AddIcon("File Browser", "📁", "File Browser");
            AddIcon("Explorer", "▢", "Explorer");
            AddIcon("CMD", "›_", "CMD");
            AddIcon("PowerShell", "PS", "PowerShell");

            Controls.Add(iconSidebar);
        }

        private void AddIcon(string tag, string icon, string tooltip)
        {
            var btn = new IconButton
            {
                Icon = icon,
                Tooltip = tooltip,
                Size = new Size(48, 48),
                Location = new Point(8, 64 + iconButtons.Count * 52),
                Tag = tag
            };
            btn.Click += (s, e) => NavigateTo(tag);
            iconSidebar.Controls.Add(btn);
            iconButtons[tag] = btn;
        }

        private void SetupContent()
        {
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(24)
            };
            Controls.Add(contentPanel);
            contentPanel.BringToFront();
            topBar.BringToFront();
        }

        private void SetupBottomBar()
        {
            bottomBar = new Panel
            {
                Height = BottomBarHeight,
                Dock = DockStyle.Bottom,
                BackColor = Color.Transparent
            };
            bottomBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var pen = new Pen(Theme.Border, 1))
                    g.DrawLine(pen, 0, 0, bottomBar.Width, 0);
                TextRenderer.DrawText(g, "MazizTool v5.0  ·  ready", new Font("Segoe UI", 8f),
                    new Rectangle(12, 4, 400, 18), Theme.TextMuted, TextFormatFlags.Left);
            };
            var memLabel = new Label
            {
                Font = new Font("Segoe UI", 8f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Right,
                Name = "memLabel"
            };
            bottomBar.Controls.Add(memLabel);
            bottomBar.Resize += (s, e) => { memLabel.Location = new Point(bottomBar.Width - 200, 5); };
            Controls.Add(bottomBar);
        }

        private void NavigateTo(string feature)
        {
            if (activeIcon != null && iconButtons.TryGetValue(feature, out var existing) && existing == activeIcon && currentView != null && feature != "Dashboard") return;

            if (feature == "Explorer") { LaunchExplorer(); return; }
            if (feature == "CMD") { LaunchCmd(); return; }
            if (feature == "PowerShell") { LaunchPowerShell(); return; }

            foreach (var btn in iconButtons.Values) btn.SetActive(false);
            activeIcon = null;
            if (iconButtons.TryGetValue(feature, out var nb)) { nb.SetActive(true); activeIcon = nb; }

            try
            {
                currentView?.Dispose();
                currentView = null;
                contentPanel.Controls.Clear();
                BuildFeature(feature);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Navigate error: " + ex.Message, "MazizTool");
            }
            UpdateModuleHeader(feature);
        }

        private void UpdateModuleHeader(string feature)
        {
            if (moduleTitleLabel != null) moduleTitleLabel.Text = feature;
            var subs = new Dictionary<string, string>
            {
                {"Dashboard","system overview"}, {"Sys File Integrity","verify & repair system files"},
                {"Registry Scan","full registry persistence scan"}, {"Service Scan","analyze Windows services"},
                {"Hijack Remover","detect browser/DNS/proxy hijacks"}, {"Virus Scanner","signature + heuristic scan"},
                {"Task Manager","running processes"}, {"Process Killer","force-kill malicious processes"},
                {"Startup Manager","auto-start entries"}, {"File Analyzer","PE inspection & hashing"},
                {"Hotkey Fix","unblock disabled hotkeys"}, {"Font Protect","restore system fonts"},
                {"UAC Bypass","privilege escalation"}, {"Registry Editor","registry navigation"},
                {"Hosts Editor","edit hosts file"}, {"System Tools","recovery utilities"},
                {"File Browser","browse & manage files"}
            };
            if (moduleSubLabel != null) moduleSubLabel.Text = subs.TryGetValue(feature, out var sub) ? sub : "";
        }

        private void UpdateStatusBar()
        {
            try
            {
                var proc = Process.GetCurrentProcess();
                var lbl = bottomBar.Controls.Find("memLabel", false).FirstOrDefault() as Label;
                if (lbl != null)
                    lbl.Text = $"mem:{proc.WorkingSet64 / 1024 / 1024}MB · procs:{Process.GetProcesses().Length}";
            }
            catch { }
        }

        private void BuildFeature(string feature)
        {
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
            }
        }

        private Panel NewView()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoScroll = true, Padding = new Padding(8) };
            currentView = p;
            return p;
        }

        private MaterialButton Btn(string text, Color accent, int x, int y, int w, int h = 38)
        {
            return new MaterialButton
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, h),
                AccentColor = accent, Radius = 8, ElevationDepth = 4
            };
        }

        private MaterialCard Card(int x, int y, int w, int h)
        {
            return new MaterialCard
            {
                Location = new Point(x, y), Size = new Size(w, h),
                CardColor = Theme.Surface, Radius = 12, Elevation = 3, HasBorder = true
            };
        }

        private ListView Grid(int x, int y, int w, int h)
        {
            return new ListView
            {
                Location = new Point(x, y), Size = new Size(w, h), View = View.Details,
                FullRowSelect = true, GridLines = false, BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary, Font = new Font("Cascadia Code", 9f),
                BorderStyle = BorderStyle.None, HideSelection = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
        }

        private Label CardTitle(string text, int x = 16, int y = 12)
        {
            return new Label
            {
                Text = text, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Accent, AutoSize = true, Location = new Point(x, y)
            };
        }

        private Label Hint(string text, int x, int y)
        {
            return new Label
            {
                Text = text, Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(x, y)
            };
        }

        private void Toast(string msg, Color? color = null) => ToastNotif.Show(this, msg, color ?? Theme.Accent);

        private void ShowDashboard()
        {
            var p = NewView();
            int y = 8;

            var infoCard = Card(8, y, 540, 180);
            infoCard.Controls.Add(CardTitle("SYSTEM INFORMATION", 16, 12));
            var infoBox = new TextBox
            {
                Multiline = true, ReadOnly = true, BackColor = Theme.Surface,
                ForeColor = Theme.TextSecondary, BorderStyle = BorderStyle.None,
                Font = new Font("Cascadia Code", 8.5f), Location = new Point(16, 36),
                Size = new Size(510, 140), Text = SystemTools.GetSystemInfo()
            };
            infoCard.Controls.Add(infoBox);
            p.Controls.Add(infoCard);

            var quickCard = Card(560, y, 540, 180);
            quickCard.Controls.Add(CardTitle("QUICK ACTIONS", 16, 12));
            var ops = new (string, Color, Action)[]
            {
                ("Fix All", Theme.Emerald, () => { HotkeyRestorer.RestoreAll(); SystemTools.FixGroupPolicies(); Toast("All restrictions fixed."); }),
                ("SFC Scan", Theme.Accent, () => { var f = CreateOutputForm("SFC /scannow"); _ = RunSfcInForm(f); }),
                ("DISM Fix", Theme.Accent, () => { var f = CreateOutputForm("DISM /RestoreHealth"); _ = RunDismInForm(f); }),
                ("Fix EXE", Theme.Warning, () => { SystemTools.RestoreExeAssociations(); Toast("EXE assoc fixed."); }),
                ("Fix Hosts", Theme.Info, () => { SystemTools.FixHostsFile(); Toast("Hosts reset."); }),
                ("Reset Net", Theme.Info, () => { SystemTools.ResetNetworkSettings(); Toast("Network reset."); }),
                ("Restore Pt", Theme.Warning, () => { SystemTools.CreateRestorePoint("MazizTool"); Toast("Restore point created."); }),
                ("Kill NonSys", Theme.Danger, () => { ProcessKiller.KillNonSystemProcesses(); Toast("Non-sys killed."); }),
            };
            for (int i = 0; i < ops.Length; i++)
            {
                var (name, color, act) = ops[i];
                int col = i % 2, row = i / 2;
                var b = Btn(name, color, 16 + col * 262, 38 + row * 34, 258, 28);
                b.Click += (s, e) => act();
                quickCard.Controls.Add(b);
            }
            p.Controls.Add(quickCard);
            y += 192;

            var modulesTitle = new Label
            {
                Text = "MODULES", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.Accent, AutoSize = true, Location = new Point(8, y)
            };
            p.Controls.Add(modulesTitle);
            y += 28;

            var modules = new (string icon, string title, string desc, string tag, Color color)[]
            {
                ("◆", "SFC / DISM", "Verify system files", "Sys File Integrity", Theme.Accent),
                ("ƒ", "Registry Scan", "Find malware persistence", "Registry Scan", Theme.Emerald),
                ("⚙", "Service Scan", "Suspicious services", "Service Scan", Theme.Info),
                ("⬚", "Hijack Remover", "Browser/DNS/proxy", "Hijack Remover", Theme.Warning),
                ("🛡", "Virus Scanner", "Heuristic detection", "Virus Scanner", Theme.Danger),
                ("▤", "Task Manager", "Running processes", "Task Manager", Theme.Accent),
                ("✖", "Process Killer", "Force-kill malware", "Process Killer", Theme.Danger),
                ("↻", "Startup Manager", "Auto-start entries", "Startup Manager", Theme.Warning),
                ("⌬", "File Analyzer", "PE/hash/signature", "File Analyzer", Theme.Info),
                ("⌨", "Hotkey Fix", "Unblock hotkeys", "Hotkey Fix", Theme.Emerald),
                ("Aa", "Font Protect", "Restore fonts", "Font Protect", Theme.Accent),
                ("🔓", "UAC Bypass", "Privilege escalation", "UAC Bypass", Theme.Warning),
            };
            int cardW = 268, cardH = 110, gap = 12;
            for (int i = 0; i < modules.Length; i++)
            {
                var (icon, title, desc, tag, color) = modules[i];
                int col = i % 4, row = i / 4;
                var mc = new ModuleCard
                {
                    Icon = icon, Title = title, Description = desc, IconColor = color,
                    Size = new Size(cardW, cardH),
                    Location = new Point(8 + col * (cardW + gap), y + row * (cardH + gap)),
                    Tag = tag
                };
                mc.Click += (s, e) => NavigateTo(tag);
                p.Controls.Add(mc);
            }
            contentPanel.Controls.Add(p);
        }

        private Form CreateOutputForm(string title)
        {
            var f = new Form
            {
                Text = "MazizTool · " + title, Size = new Size(820, 560),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.Background, ForeColor = Theme.TextPrimary, Font = Theme.UIFont
            };
            try { int d = 1; Win32.DwmSetWindowAttribute(f.Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref d, 4); } catch { }
            var header = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Surface };
            header.Paint += (s, e) => { using (var pen = new Pen(Theme.Border, 1)) e.Graphics.DrawLine(pen, 0, 39, header.Width, 39); };
            var hTitle = new Label { Text = title, Font = Theme.HeaderFont, ForeColor = Theme.Accent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
            header.Controls.Add(hTitle);
            var output = new TextBox
            {
                Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(6, 10, 12),
                ForeColor = Theme.Accent, Font = new Font("Cascadia Code", 9f), BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false
            };
            var progress = new ProgressBar { Dock = DockStyle.Bottom, Height = 4, Style = ProgressBarStyle.Continuous, ForeColor = Theme.Accent, BackColor = Theme.Surface };
            f.Controls.Add(output); f.Controls.Add(progress); f.Controls.Add(header);
            f.Tag = output;
            f.Show();
            return f;
        }

        private async Task RunSfcInForm(Form f)
        {
            var output = f.Tag as TextBox;
            var progress = f.Controls.OfType<ProgressBar>().FirstOrDefault();
            void OnOut(string line) => f.BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.ScrollToCaret(); });
            void OnP(int pct) => f.BeginInvokeIfCreated(() => { if (progress != null) progress.Value = pct; });
            integrityScanner.OnOutput += OnOut;
            integrityScanner.OnProgress += OnP;
            await integrityScanner.RunSfcScannowAsync();
            integrityScanner.OnOutput -= OnOut;
            integrityScanner.OnProgress -= OnP;
            f.BeginInvokeIfCreated(() => { if (progress != null) progress.Value = 100; output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine); });
        }

        private async Task RunDismInForm(Form f)
        {
            var output = f.Tag as TextBox;
            var progress = f.Controls.OfType<ProgressBar>().FirstOrDefault();
            void OnOut(string line) => f.BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.ScrollToCaret(); });
            void OnP(int pct) => f.BeginInvokeIfCreated(() => { if (progress != null) progress.Value = pct; });
            integrityScanner.OnOutput += OnOut;
            integrityScanner.OnProgress += OnP;
            await integrityScanner.RunDismRestoreHealthAsync();
            integrityScanner.OnOutput -= OnOut;
            integrityScanner.OnProgress -= OnP;
            f.BeginInvokeIfCreated(() => { if (progress != null) progress.Value = 100; output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine); });
        }

        private void ShowSystemFileIntegrity()
        {
            var p = NewView();
            int y = 8;
            var c1 = Card(8, y, contentPanel.Width - 64, 56);
            var sfc = Btn("SFC /SCANNOW", Theme.Accent, 16, 10, 180, 36);
            sfc.Click += (s, e) => { var f = CreateOutputForm("SFC /scannow"); _ = RunSfcInForm(f); };
            var dScan = Btn("DISM /SCAN", Theme.Info, 202, 10, 180, 36);
            dScan.Click += (s, e) => { var f = CreateOutputForm("DISM /ScanHealth"); _ = RunDismScanInForm(f); };
            var dRest = Btn("DISM /RESTORE", Theme.Emerald, 388, 10, 180, 36);
            dRest.Click += (s, e) => { var f = CreateOutputForm("DISM /RestoreHealth"); _ = RunDismInForm(f); };
            var dCl = Btn("DISM /CLEANUP", Theme.Warning, 574, 10, 180, 36);
            dCl.Click += (s, e) => { var f = CreateOutputForm("DISM /StartComponentCleanup"); _ = RunDismCleanupInForm(f); };
            c1.Controls.Add(sfc); c1.Controls.Add(dScan); c1.Controls.Add(dRest); c1.Controls.Add(dCl);
            p.Controls.Add(c1);
            y += 68;

            var c2 = Card(8, y, contentPanel.Width - 64, 56);
            var chk = Btn("Check Critical Files", Theme.Accent, 16, 10, 220, 36);
            chk.Click += (s, e) => PopulateCriticalFilesGrid(p, y + 68);
            var ifeo = Btn("IFEO Hijack Check", Theme.Danger, 242, 10, 220, 36);
            ifeo.Click += (s, e) =>
            {
                var hij = SystemFileIntegrity.CheckImageFileExecutionOptions();
                var winl = SystemFileIntegrity.CheckWinlogonPersistence();
                var sb = new StringBuilder();
                sb.AppendLine("// IMAGE FILE EXECUTION OPTIONS:");
                sb.AppendLine(hij.Count == 0 ? "  [OK] No IFEO debugger hijacks" : string.Join(Environment.NewLine, hij.Select(h => "  [!] " + h)));
                sb.AppendLine(); sb.AppendLine("// WINLOGON PERSISTENCE:");
                foreach (var w in winl) sb.AppendLine("  " + w);
                ShowTextInPanel(p, y + 68, sb.ToString());
            };
            var kd = Btn("KnownDLLs Check", Theme.Warning, 468, 10, 220, 36);
            kd.Click += (s, e) =>
            {
                var dlls = SystemFileIntegrity.CheckKnownDLLs();
                var sb = new StringBuilder();
                sb.AppendLine("// KNOWN DLLS:");
                sb.AppendLine(dlls.Count == 0 ? "  [OK] All KnownDLLs entries valid" : string.Join(Environment.NewLine, dlls.Select(d => "  [!] " + d)));
                ShowTextInPanel(p, y + 68, sb.ToString());
            };
            c2.Controls.Add(chk); c2.Controls.Add(ifeo); c2.Controls.Add(kd);
            p.Controls.Add(c2);
            contentPanel.Controls.Add(p);
        }

        private async Task RunDismScanInForm(Form f)
        {
            var output = f.Tag as TextBox;
            void OnOut(string line) => f.BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.ScrollToCaret(); });
            integrityScanner.OnOutput += OnOut;
            await integrityScanner.RunDismScanHealthAsync();
            integrityScanner.OnOutput -= OnOut;
            f.BeginInvokeIfCreated(() => output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine));
        }

        private async Task RunDismCleanupInForm(Form f)
        {
            var output = f.Tag as TextBox;
            void OnOut(string line) => f.BeginInvokeIfCreated(() => { output.AppendText(line + Environment.NewLine); output.ScrollToCaret(); });
            integrityScanner.OnOutput += OnOut;
            await integrityScanner.RunDismStartComponentCleanupAsync();
            integrityScanner.OnOutput -= OnOut;
            f.BeginInvokeIfCreated(() => output.AppendText(Environment.NewLine + "[*] DONE." + Environment.NewLine));
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
                    if (r.Suspicious && !string.IsNullOrEmpty(r.Reason)) sb.AppendLine($"        └─ {r.Reason}");
                }
                BeginInvokeIfCreated(() => ShowTextInPanel(panel, y, sb.ToString()));
            });
        }

        private void ShowTextInPanel(Panel panel, int y, string text)
        {
            foreach (var c in panel.Controls.OfType<TextBox>().ToList()) { panel.Controls.Remove(c); c.Dispose(); }
            var box = new TextBox
            {
                Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(6, 10, 12),
                ForeColor = Theme.Accent, Font = new Font("Cascadia Code", 9f), BorderStyle = BorderStyle.None,
                Location = new Point(8, y), Size = new Size(panel.Width - 32, panel.Height - y - 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ScrollBars = ScrollBars.Both, WordWrap = false, Text = text
            };
            panel.Controls.Add(box);
            box.BringToFront();
        }

        private void ShowRegistryScanner()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 56);
            var scan = Btn("Scan Registry", Theme.Accent, 16, 10, 180, 36);
            var fix = Btn("Fix All Policies", Theme.Danger, 202, 10, 180, 36);
            fix.Click += (s, e) => { HotkeyRestorer.FixAllHotkeyBlocks(); SystemTools.FixGroupPolicies(); Toast("Policies fixed."); };
            c.Controls.Add(scan); c.Controls.Add(fix);
            p.Controls.Add(c);
            y += 68;
            var status = Hint("● ready", 12, y); p.Controls.Add(status); y += 24;
            var grid = Grid(8, y, contentPanel.Width - 64, contentPanel.Height - y - 40);
            grid.Columns.Add("LVL", 50); grid.Columns.Add("Location", 280); grid.Columns.Add("Value", 140);
            grid.Columns.Add("Data", 260); grid.Columns.Add("Description", 240);
            scan.Click += (s, e) =>
            {
                grid.Items.Clear(); status.Text = "● scanning...";
                registryScanner.OnProgress += (line) => BeginInvokeIfCreated(() => status.Text = "● " + line);
                registryScanner.OnFinding += (f) => BeginInvokeIfCreated(() =>
                {
                    var it = new ListViewItem(f.Level.ToString().ToUpper().Substring(0, 4));
                    it.SubItems.Add(f.Location); it.SubItems.Add(f.Value); it.SubItems.Add(f.Data); it.SubItems.Add(f.Description);
                    it.ForeColor = f.Level == RegistryScanner.ThreatLevel.Malicious ? Theme.Danger :
                                   f.Level == RegistryScanner.ThreatLevel.Suspicious ? Theme.Warning :
                                   f.Level == RegistryScanner.ThreatLevel.Info ? Theme.Info : Theme.TextSecondary;
                    grid.Items.Add(it);
                });
                Task.Run(() => registryScanner.Scan());
            };
            p.Controls.Add(grid);
            contentPanel.Controls.Add(p);
        }

        private void ShowServiceScanner()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 56);
            var scan = Btn("Scan Services", Theme.Accent, 16, 10, 180, 36);
            var suspChk = new CheckBox { Text = "suspicious only", ForeColor = Theme.TextSecondary, Font = Theme.MonoFont, Location = new Point(206, 16), AutoSize = true };
            var stop = Btn("Stop", Theme.Warning, 350, 10, 100, 36);
            var dis = Btn("Disable", Theme.Warning, 456, 10, 110, 36);
            var del = Btn("Delete", Theme.Danger, 572, 10, 110, 36);
            c.Controls.Add(scan); c.Controls.Add(suspChk); c.Controls.Add(stop); c.Controls.Add(dis); c.Controls.Add(del);
            p.Controls.Add(c);
            y += 68;
            var status = Hint("● ready", 12, y); p.Controls.Add(status); y += 24;
            var grid = Grid(8, y, contentPanel.Width - 64, contentPanel.Height - y - 40);
            grid.Columns.Add("!", 24); grid.Columns.Add("Name", 160); grid.Columns.Add("Display Name", 180);
            grid.Columns.Add("State", 80); grid.Columns.Add("Start", 70); grid.Columns.Add("Binary Path", 320); grid.Columns.Add("Reason", 220);
            scan.Click += (s, e) =>
            {
                grid.Items.Clear(); status.Text = "● enumerating...";
                serviceScanner.OnProgress += (line) => BeginInvokeIfCreated(() => status.Text = "● " + line);
                serviceScanner.OnService += (svc) => BeginInvokeIfCreated(() =>
                {
                    if (suspChk.Checked && !svc.Suspicious) return;
                    var it = new ListViewItem(svc.Suspicious ? "!" : "");
                    it.SubItems.Add(svc.Name); it.SubItems.Add(svc.DisplayName); it.SubItems.Add(svc.State);
                    it.SubItems.Add(svc.StartMode);
                    it.SubItems.Add(svc.BinaryPath.Length > 60 ? "..." + svc.BinaryPath.Substring(svc.BinaryPath.Length - 57) : svc.BinaryPath);
                    it.SubItems.Add(svc.Reason ?? "");
                    if (svc.Suspicious) it.ForeColor = Theme.Danger;
                    it.Tag = svc;
                    grid.Items.Add(it);
                });
                Task.Run(() => serviceScanner.Scan());
            };
            void Act(Func<string, bool> a, string verb)
            {
                foreach (ListViewItem it in grid.SelectedItems)
                    if (it.Tag is ServiceScanner.ServiceInfo svc) a(svc.Name);
                Toast(verb + " executed.");
            }
            stop.Click += (s, e) => Act(ServiceScanner.StopService, "Stop");
            dis.Click += (s, e) => Act(ServiceScanner.DisableService, "Disable");
            del.Click += (s, e) => Act(ServiceScanner.DeleteService, "Delete");
            p.Controls.Add(grid);
            contentPanel.Controls.Add(p);
        }

        private void ShowHijackRemover()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 56);
            var scan = Btn("Scan Hijacks", Theme.Accent, 16, 10, 180, 36);
            var fp = Btn("Fix Proxy", Theme.Info, 202, 10, 150, 36);
            var fw = Btn("Fix Winsock", Theme.Info, 358, 10, 150, 36);
            var fdns = Btn("Reset DNS", Theme.Info, 514, 10, 150, 36);
            c.Controls.Add(scan); c.Controls.Add(fp); c.Controls.Add(fw); c.Controls.Add(fdns);
            p.Controls.Add(c);
            y += 68;
            var status = Hint("● ready", 12, y); p.Controls.Add(status); y += 24;
            var grid = Grid(8, y, contentPanel.Width - 64, contentPanel.Height - y - 40);
            grid.Columns.Add("!", 24); grid.Columns.Add("Category", 100); grid.Columns.Add("Detail", 280);
            grid.Columns.Add("Value", 260); grid.Columns.Add("Fix", 240);
            scan.Click += (s, e) =>
            {
                grid.Items.Clear(); status.Text = "● scanning hijack vectors...";
                hijackRemover.OnProgress += (line) => BeginInvokeIfCreated(() => status.Text = "● " + line);
                hijackRemover.OnFinding += (f) => BeginInvokeIfCreated(() =>
                {
                    var it = new ListViewItem(f.Suspicious ? "!" : "");
                    it.SubItems.Add(f.Category); it.SubItems.Add(f.Detail); it.SubItems.Add(f.Value); it.SubItems.Add(f.Fix);
                    if (f.Suspicious) it.ForeColor = Theme.Danger;
                    grid.Items.Add(it);
                });
                Task.Run(() => hijackRemover.Scan());
            };
            fp.Click += (s, e) => { hijackRemover.FixProxy(); Toast("Proxy reset."); };
            fw.Click += (s, e) => { hijackRemover.FixWinsock(); Toast("Winsock reset (reboot needed)."); };
            fdns.Click += (s, e) => { hijackRemover.ResetDns(); Toast("DNS reset."); };
            p.Controls.Add(grid);
            contentPanel.Controls.Add(p);
        }

        private void ShowFileAnalyzer()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 60);
            var pathInput = new TextBox
            {
                Location = new Point(16, 14), Size = new Size(500, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f),
                PlaceholderText = "C:\\path\\to\\suspect.exe"
            };
            pathInput.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog { Filter = "Executables|*.exe;*.dll;*.sys;*.scr|All|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) pathInput.Text = ofd.FileName;
            };
            var analyze = Btn("Analyze", Theme.Accent, 524, 14, 140, 32);
            c.Controls.Add(pathInput); c.Controls.Add(analyze);
            p.Controls.Add(c);
            y += 72;
            var result = new TextBox
            {
                Location = new Point(8, y), Size = new Size(contentPanel.Width - 64, contentPanel.Height - y - 24),
                Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(6, 10, 12),
                ForeColor = Theme.Accent, Font = new Font("Cascadia Code", 9f), BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Both, WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            analyze.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(pathInput.Text) || !File.Exists(pathInput.Text)) { Toast("Pick a valid file."); return; }
                result.Text = "[*] Analyzing...";
                Task.Run(() =>
                {
                    var info = fileAnalyzer.Analyze(pathInput.Text);
                    var report = fileAnalyzer.FormatReport(info);
                    BeginInvokeIfCreated(() => result.Text = report);
                });
            };
            p.Controls.Add(result);
            contentPanel.Controls.Add(p);
        }

        private void ShowVirusScanner()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 60);
            var pathInput = new TextBox
            {
                Location = new Point(16, 14), Size = new Size(400, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f), Text = @"C:\Users"
            };
            var scan = Btn("Start Scan", Theme.Danger, 420, 14, 130, 32);
            var quick = Btn("Quick", Theme.Warning, 556, 14, 100, 32);
            c.Controls.Add(pathInput); c.Controls.Add(scan); c.Controls.Add(quick);
            p.Controls.Add(c);
            y += 72;
            var progress = new ProgressBar
            {
                Location = new Point(8, y), Size = new Size(contentPanel.Width - 64, 4),
                Style = ProgressBarStyle.Continuous, ForeColor = Theme.Accent, BackColor = Theme.Surface
            };
            p.Controls.Add(progress);
            y += 10;
            var status = Hint("● ready", 12, y); p.Controls.Add(status); y += 22;
            var grid = Grid(8, y, contentPanel.Width - 64, contentPanel.Height - y - 24);
            grid.Columns.Add("Threat", 200); grid.Columns.Add("Level", 80); grid.Columns.Add("File", 380); grid.Columns.Add("Description", 240);
            scan.Click += async (s, e) =>
            {
                if (scanner.IsScanning) { scanner.StopScan(); scan.Text = "Start Scan"; return; }
                scan.Text = "Stop"; grid.Items.Clear(); progress.Value = 0;
                await scanner.ScanAsync(pathInput.Text);
                scan.Text = "Start Scan";
            };
            quick.Click += async (s, e) =>
            {
                if (scanner.IsScanning) return;
                grid.Items.Clear(); progress.Value = 0;
                await scanner.ScanAsync(Path.GetTempPath());
                if (!scanner.IsScanning) await scanner.ScanAsync(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            };
            p.Controls.Add(grid);
            contentPanel.Tag = grid;
            contentPanel.Controls.Add(p);
        }

        private void OnScanProgress(string file, int count)
        {
            BeginInvokeIfCreated(() =>
            {
                if (currentView == null) return;
                var s = currentView.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("●"));
                if (s != null) s.Text = $"● scanning ({count} files)...";
            });
        }

        private void OnThreatFound(VirusScanner.ScanResult result)
        {
            BeginInvokeIfCreated(() =>
            {
                var grid = contentPanel.Tag as ListView;
                if (grid == null) return;
                var it = new ListViewItem(result.ThreatName);
                it.SubItems.Add(result.Level.ToString()); it.SubItems.Add(result.FilePath); it.SubItems.Add(result.Description);
                it.ForeColor = result.Level == VirusScanner.ThreatLevel.Critical ? Theme.Danger :
                               result.Level == VirusScanner.ThreatLevel.High ? Theme.Warning : Theme.TextSecondary;
                grid.Items.Add(it);
            });
        }

        private void OnScanComplete()
        {
            BeginInvokeIfCreated(() =>
            {
                if (currentView == null) return;
                var s = currentView.Controls.OfType<Label>().FirstOrDefault(l => l.Text.StartsWith("●"));
                if (s != null) s.Text = $"● done · files:{scanner.FilesScanned} threats:{scanner.ThreatsFound}";
            });
        }

        private void ShowTaskManager()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 56);
            var refresh = Btn("Refresh", Theme.Info, 16, 10, 110, 36);
            var kill = Btn("Kill", Theme.Danger, 132, 10, 100, 36);
            var killSys = Btn("Kill Non-Sys", Theme.Warning, 238, 10, 140, 36);
            c.Controls.Add(refresh); c.Controls.Add(kill); c.Controls.Add(killSys);
            p.Controls.Add(c);
            y += 68;
            var lv = Grid(8, y, contentPanel.Width - 64, contentPanel.Height - y - 24);
            lv.Columns.Add("PID", 70); lv.Columns.Add("Name", 180); lv.Columns.Add("Path", 360);
            lv.Columns.Add("Mem(MB)", 90); lv.Columns.Add("Threads", 70);
            refresh.Click += (s, e) => RefreshTaskList(lv);
            kill.Click += (s, e) =>
            {
                foreach (ListViewItem it in lv.SelectedItems)
                    if (it.Tag is ProcessKiller.ProcessInfo pi) ProcessKiller.KillProcess(pi.Id);
                RefreshTaskList(lv);
            };
            killSys.Click += (s, e) => { ProcessKiller.KillNonSystemProcesses(); RefreshTaskList(lv); };
            p.Controls.Add(lv);
            RefreshTaskList(lv);
            contentPanel.Controls.Add(p);
        }

        private void RefreshTaskList(ListView lv)
        {
            lv.Items.Clear();
            foreach (var proc in ProcessKiller.GetAllProcesses())
            {
                var it = new ListViewItem(proc.Id.ToString());
                it.SubItems.Add(proc.Name);
                it.SubItems.Add(proc.Path.Length > 60 ? "..." + proc.Path.Substring(proc.Path.Length - 57) : proc.Path);
                it.SubItems.Add((proc.MemoryBytes / 1024.0 / 1024.0).ToString("F1"));
                it.SubItems.Add(proc.Threads.ToString());
                it.Tag = proc;
                lv.Items.Add(it);
            }
        }

        private void ShowProcessKiller()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 60);
            var name = new TextBox
            {
                Location = new Point(16, 14), Size = new Size(280, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f),
                PlaceholderText = "process name (e.g. malware.exe)"
            };
            var kn = Btn("Kill by Name", Theme.Danger, 302, 14, 140, 32);
            kn.Click += (s, e) => { if (!string.IsNullOrWhiteSpace(name.Text)) { ProcessKiller.KillProcessByName(name.Text.Trim()); Toast("Killed: " + name.Text.Trim()); } };
            c.Controls.Add(name); c.Controls.Add(kn);
            p.Controls.Add(c);
            y += 72;
            var killAll = Btn("Kill All Non-System", Theme.Warning, 8, y, 220, 40);
            killAll.Click += (s, e) => { ProcessKiller.KillNonSystemProcesses(); Toast("Done."); };
            p.Controls.Add(killAll);
            y += 52;
            var title = new Label { Text = "SUSPICIOUS PROCESSES", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.Accent, AutoSize = true, Location = new Point(8, y) };
            p.Controls.Add(title);
            y += 24;
            var lv = Grid(8, y, contentPanel.Width - 64, 240);
            lv.Columns.Add("PID", 70); lv.Columns.Add("Name", 200); lv.Columns.Add("Path", 460);
            foreach (var proc in VirusScanner.GetSuspiciousProcesses())
            {
                try
                {
                    var it = new ListViewItem(proc.Id.ToString());
                    it.SubItems.Add(proc.ProcessName);
                    it.SubItems.Add(proc.MainModule?.FileName ?? "");
                    it.ForeColor = Theme.Danger;
                    lv.Items.Add(it);
                }
                catch { }
            }
            p.Controls.Add(lv);
            y += 248;
            var killSusp = Btn("Kill All Suspicious", Theme.Danger, 8, y, 200, 36);
            killSusp.Click += (s, e) =>
            {
                foreach (var proc in VirusScanner.GetSuspiciousProcesses())
                    try { proc.Kill(); } catch { }
                Toast("Suspicious processes killed.");
            };
            p.Controls.Add(killSusp);
            contentPanel.Controls.Add(p);
        }

        private void ShowStartupManager()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 56);
            var refresh = Btn("Refresh", Theme.Info, 16, 10, 110, 36);
            var remove = Btn("Remove", Theme.Danger, 132, 10, 110, 36);
            var clean = Btn("Clean All", Theme.Warning, 248, 10, 130, 36);
            c.Controls.Add(refresh); c.Controls.Add(remove); c.Controls.Add(clean);
            p.Controls.Add(c);
            y += 68;
            var lv = Grid(8, y, contentPanel.Width - 64, contentPanel.Height - y - 24);
            lv.Columns.Add("Name", 180); lv.Columns.Add("Command", 360); lv.Columns.Add("Location", 160); lv.Columns.Add("Status", 80);
            void RefreshList()
            {
                lv.Items.Clear();
                foreach (var en in StartupManager.GetStartupEntries())
                {
                    var it = new ListViewItem(en.Name);
                    it.SubItems.Add(en.Command.Length > 60 ? en.Command.Substring(0, 57) + "..." : en.Command);
                    it.SubItems.Add(en.Location);
                    it.SubItems.Add(en.Enabled ? "enabled" : "disabled");
                    it.Tag = en;
                    lv.Items.Add(it);
                }
            }
            refresh.Click += (s, e) => RefreshList();
            remove.Click += (s, e) =>
            {
                foreach (ListViewItem it in lv.SelectedItems)
                    if (it.Tag is StartupManager.StartupEntry en) StartupManager.RemoveStartupEntry(en);
                RefreshList();
            };
            clean.Click += (s, e) =>
            {
                int n = StartupManager.GetStartupEntries().Count(en => StartupManager.RemoveStartupEntry(en));
                RefreshList();
                Toast($"Removed {n} entries.");
            };
            p.Controls.Add(lv);
            RefreshList();
            contentPanel.Controls.Add(p);
        }

        private void ShowRegistryEditor()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 60);
            var pathInput = new TextBox
            {
                Location = new Point(16, 14), Size = new Size(500, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f),
                Text = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies"
            };
            var go = Btn("Go", Theme.Accent, 524, 14, 100, 32);
            c.Controls.Add(pathInput); c.Controls.Add(go);
            p.Controls.Add(c);
            y += 72;
            var result = new TextBox
            {
                Location = new Point(8, y), Size = new Size(contentPanel.Width - 64, 220),
                Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(6, 10, 12),
                ForeColor = Theme.Accent, Font = new Font("Cascadia Code", 9f), BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical, WordWrap = false
            };
            go.Click += (s, e) => { result.Clear(); NavigateRegistry(pathInput.Text, result); };
            p.Controls.Add(result);
            y += 232;
            var re = Btn("Open regedit", Theme.Info, 8, y, 150, 36);
            re.Click += (s, e) => { HotkeyRestorer.RestoreRegistryEditor(); try { Process.Start("regedit.exe"); } catch { } };
            var fp = Btn("Fix Policies", Theme.Warning, 166, y, 150, 36);
            fp.Click += (s, e) => { SystemTools.FixGroupPolicies(); HotkeyRestorer.FixAllHotkeyBlocks(); Toast("Policies fixed."); };
            p.Controls.Add(re); p.Controls.Add(fp);
            contentPanel.Controls.Add(p);
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
                    foreach (var n in key.GetValueNames()) { var v = key.GetValue(n); sb.AppendLine($"  {n} = {v}"); }
                    key.Close();
                }
                resultBox.Text = sb.ToString();
            }
            catch (Exception ex) { resultBox.Text = "[!] " + ex.Message; }
        }

        private void ShowFileBrowser()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 60);
            var pathInput = new TextBox
            {
                Location = new Point(16, 14), Size = new Size(500, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f), Text = @"C:\"
            };
            var browse = Btn("Browse", Theme.Info, 524, 14, 100, 32);
            c.Controls.Add(pathInput); c.Controls.Add(browse);
            p.Controls.Add(c);
            y += 72;
            var c2 = Card(8, y, contentPanel.Width - 64, 52);
            var searchInput = new TextBox
            {
                Location = new Point(16, 10), Size = new Size(300, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f),
                PlaceholderText = "search files..."
            };
            var search = Btn("Search", Theme.Info, 322, 10, 100, 32);
            c2.Controls.Add(searchInput); c2.Controls.Add(search);
            p.Controls.Add(c2);
            y += 64;
            var lv = Grid(8, y, contentPanel.Width - 64, contentPanel.Height - y - 24);
            lv.Columns.Add("Name", 320); lv.Columns.Add("Size", 100); lv.Columns.Add("Modified", 160); lv.Columns.Add("Type", 100);
            lv.DoubleClick += (s, e) =>
            {
                if (lv.SelectedItems.Count == 0) return;
                var tag = lv.SelectedItems[0].Tag as string;
                if (tag == null) return;
                if (Directory.Exists(tag)) { pathInput.Text = tag; RefreshFileList(lv, tag); }
                else if (File.Exists(tag)) { try { Process.Start(new ProcessStartInfo { FileName = tag, UseShellExecute = true }); } catch { } }
            };
            browse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                    if (fbd.ShowDialog() == DialogResult.OK) pathInput.Text = fbd.SelectedPath;
                RefreshFileList(lv, pathInput.Text);
            };
            search.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(searchInput.Text)) return;
                lv.Items.Clear();
                try
                {
                    var results = Directory.GetFiles(pathInput.Text, $"*{searchInput.Text}*", SearchOption.AllDirectories).Take(200);
                    foreach (var file in results)
                    {
                        var fi = new FileInfo(file);
                        var it = new ListViewItem(fi.Name);
                        it.SubItems.Add(FormatSize(fi.Length));
                        it.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        it.SubItems.Add(fi.Extension);
                        it.Tag = file;
                        lv.Items.Add(it);
                    }
                }
                catch { }
            };
            pathInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) RefreshFileList(lv, pathInput.Text); };
            p.Controls.Add(lv);
            RefreshFileList(lv, pathInput.Text);
            contentPanel.Controls.Add(p);
        }

        private void RefreshFileList(ListView list, string path)
        {
            list.Items.Clear();
            if (!Directory.Exists(path))
            {
                try { var pp = Directory.GetParent(path); if (pp != null) path = pp.FullName; } catch { return; }
            }
            try
            {
                var up = new ListViewItem(".. [UP]"); up.ForeColor = Theme.Accent; up.Tag = path; list.Items.Add(up);
                foreach (var dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        var di = new DirectoryInfo(dir);
                        if (di.Attributes.HasFlag(FileAttributes.Hidden) && di.Attributes.HasFlag(FileAttributes.System)) continue;
                        var it = new ListViewItem("[DIR] " + di.Name);
                        it.ForeColor = Theme.Accent;
                        it.SubItems.Add("<DIR>");
                        it.SubItems.Add(di.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        it.SubItems.Add("Folder");
                        it.Tag = dir;
                        list.Items.Add(it);
                    }
                    catch { }
                }
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        var it = new ListViewItem(fi.Name);
                        it.SubItems.Add(FormatSize(fi.Length));
                        it.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                        it.SubItems.Add(fi.Extension);
                        it.Tag = file;
                        list.Items.Add(it);
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
            var p = NewView();
            int y = 8;
            var hostsPath = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");
            var content = new TextBox
            {
                Location = new Point(8, y), Size = new Size(contentPanel.Width - 64, 320),
                Multiline = true, BackColor = Color.FromArgb(6, 10, 12), ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 10f),
                ScrollBars = ScrollBars.Vertical, WordWrap = false,
                Text = File.Exists(hostsPath) ? File.ReadAllText(hostsPath) : ""
            };
            p.Controls.Add(content);
            y += 330;
            var save = Btn("Save", Theme.Emerald, 8, y, 100, 36);
            var reload = Btn("Reload", Theme.Info, 116, y, 100, 36);
            var reset = Btn("Reset", Theme.Warning, 226, y, 130, 36);
            var flush = Btn("Flush DNS", Theme.Accent, 364, y, 130, 36);
            save.Click += (s, e) =>
            {
                try
                {
                    var attr = File.GetAttributes(hostsPath);
                    File.SetAttributes(hostsPath, attr & ~FileAttributes.ReadOnly);
                    File.WriteAllText(hostsPath, content.Text);
                    File.SetAttributes(hostsPath, attr);
                    Process.Start(new ProcessStartInfo { FileName = "ipconfig.exe", Arguments = "/flushdns", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000);
                    Toast("Hosts saved & DNS flushed.");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "MazizTool"); }
            };
            reload.Click += (s, e) => { if (File.Exists(hostsPath)) content.Text = File.ReadAllText(hostsPath); };
            reset.Click += (s, e) => { SystemTools.FixHostsFile(); content.Text = File.ReadAllText(hostsPath); Toast("Hosts reset."); };
            flush.Click += (s, e) => { Process.Start(new ProcessStartInfo { FileName = "ipconfig.exe", Arguments = "/flushdns", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(3000); Toast("DNS flushed."); };
            p.Controls.Add(save); p.Controls.Add(reload); p.Controls.Add(reset); p.Controls.Add(flush);
            contentPanel.Controls.Add(p);
        }

        private void ShowUacBypass()
        {
            var p = NewView();
            int y = 8;
            var c = Card(8, y, contentPanel.Width - 64, 60);
            var cmd = new TextBox
            {
                Location = new Point(16, 14), Size = new Size(400, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f), Text = "cmd.exe"
            };
            c.Controls.Add(cmd);
            p.Controls.Add(c);
            y += 72;
            var methods = UacBypass.GetAvailableMethods();
            for (int i = 0; i < methods.Count; i++)
            {
                var m = methods[i];
                var b = Btn(m.ToString(), Theme.Accent, 8, y + i * 46, 300, 40);
                b.Click += (s, e) => Toast(UacBypass.ExecuteWithUacBypass(cmd.Text, m) ? $"Bypass via {m} executed." : $"Bypass {m} failed.");
                p.Controls.Add(b);
            }
            int y2 = y + methods.Count * 46 + 10;
            var runAs = Btn("Run as Admin", Theme.Warning, 8, y2, 300, 40);
            runAs.Click += (s, e) => { UacBypass.ElevateProcess(cmd.Text); Toast("Elevation requested."); };
            var priv = Btn("Enable SeDebugPriv", Theme.Info, 8, y2 + 46, 300, 40);
            priv.Click += (s, e) =>
            {
                UacBypass.EnablePrivilege("SeDebugPrivilege");
                UacBypass.EnablePrivilege("SeTakeOwnershipPrivilege");
                UacBypass.EnablePrivilege("SeBackupPrivilege");
                UacBypass.EnablePrivilege("SeRestorePrivilege");
                UacBypass.EnablePrivilege("SeSecurityPrivilege");
                Toast("Privileges enabled.");
            };
            p.Controls.Add(runAs); p.Controls.Add(priv);
            contentPanel.Controls.Add(p);
        }

        private void ShowHotkeyFix()
        {
            var p = NewView();
            int y = 8;
            var actions = new (string, Color, Action)[]
            {
                ("Fix All Restrictions", Theme.Emerald, () => { HotkeyRestorer.RestoreAll(); SystemTools.FixGroupPolicies(); Toast("All restrictions removed."); }),
                ("Enable Alt+Tab", Theme.Info, () => { HotkeyRestorer.EnableAltTab(); Toast("Alt+Tab restored."); }),
                ("Enable Ctrl+Alt+Del", Theme.Info, () => { HotkeyRestorer.EnableCtrlAltDel(); Toast("Ctrl+Alt+Del restored."); }),
                ("Restore TaskMgr", Theme.Warning, () => { HotkeyRestorer.RestoreTaskManager(); Toast("Task Manager unblocked."); }),
                ("Restore Regedit", Theme.Warning, () => { HotkeyRestorer.RestoreRegistryEditor(); Toast("Regedit unblocked."); }),
                ("Restore Hotkeys", Theme.Accent, () => { HotkeyRestorer.RestoreHotkeys(); Toast("Hotkeys restored."); }),
                ("Fix Group Policies", Theme.Danger, () => { SystemTools.FixGroupPolicies(); Toast("Policies reset."); }),
                ("Show Hidden Files", Theme.Info, () => { SystemTools.RestoreHiddenFiles(); Toast("Hidden files shown."); }),
            };
            for (int i = 0; i < actions.Length; i++)
            {
                var (name, color, act) = actions[i];
                int col = i % 2, row = i / 2;
                var b = Btn(name, color, 8 + col * 350, y + row * 50, 340, 42);
                b.Click += (s, e) => act();
                p.Controls.Add(b);
            }
            contentPanel.Controls.Add(p);
        }

        private void ShowFontProtect()
        {
            var p = NewView();
            int y = 8;
            bool tampered = FontProtector.IsSystemFontTampered();
            var c = Card(8, y, contentPanel.Width - 64, 100);
            var status = new Label
            {
                Text = tampered ? "⚠  FONT TAMPERING DETECTED" : "✓  system fonts appear normal",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = tampered ? Theme.Danger : Theme.Emerald,
                AutoSize = true, Location = new Point(16, 16)
            };
            var info = new Label
            {
                Text = "malware may substitute Segoe UI with blank fonts to hide text.\nthis restores registry font settings & broadcasts WM_FONTCHANGE.",
                Font = Theme.MonoFont, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(16, 48)
            };
            c.Controls.Add(status); c.Controls.Add(info);
            p.Controls.Add(c);
            y += 112;
            var restore = Btn("Restore System Fonts", Theme.Accent, 8, y, 260, 42);
            restore.Click += (s, e) => { FontProtector.RestoreSystemFonts(); Toast("Fonts restored. Reboot recommended."); };
            p.Controls.Add(restore);
            contentPanel.Controls.Add(p);
        }

        private void ShowSystemTools()
        {
            var p = NewView();
            int y = 8;
            var tools = new (string, Color, Action)[]
            {
                ("Restore .exe Assoc", Theme.Warning, () => { SystemTools.RestoreExeAssociations(); Toast("Done."); }),
                ("Reset Network", Theme.Info, () => { SystemTools.ResetNetworkSettings(); Toast("Done."); }),
                ("Clear Temp", Theme.Accent, () => { SystemTools.ClearTempFiles(); Toast("Done."); }),
                ("Create Restore Pt", Theme.Emerald, () => { SystemTools.CreateRestorePoint("MazizTool"); Toast("Done."); }),
                ("CHKDSK C: /f /r", Theme.Warning, () => SystemTools.CheckDisk()),
                ("Safe Mode +Net", Theme.Info, () => { SystemTools.EnableSafeModeNetworking(); Toast("Safe mode enabled."); }),
                ("Disable Safe Mode", Theme.Accent, () => { SystemTools.DisableSafeMode(); Toast("Done."); }),
                ("Fix Hosts", Theme.Warning, () => { SystemTools.FixHostsFile(); Toast("Done."); }),
                ("Show Hidden Files", Theme.Info, () => { SystemTools.RestoreHiddenFiles(); Toast("Done."); }),
                ("Restore .com Assoc", Theme.Warning, () => { SystemTools.RestoreExeAssociations(); Toast("Done."); }),
            };
            for (int i = 0; i < tools.Length; i++)
            {
                var (name, color, act) = tools[i];
                int col = i % 2, row = i / 2;
                var b = Btn(name, color, 8 + col * 350, y + row * 50, 340, 42);
                b.Click += (s, e) => act();
                p.Controls.Add(b);
            }
            contentPanel.Controls.Add(p);
        }

        private void LaunchExplorer() { try { Process.Start("explorer.exe"); } catch { } }
        private void LaunchCmd() { try { Process.Start(new ProcessStartInfo("cmd.exe") { UseShellExecute = true, Verb = "runas" }); } catch { } }
        private void LaunchPowerShell() { try { Process.Start(new ProcessStartInfo("powershell.exe") { UseShellExecute = true, Verb = "runas" }); } catch { } }

        private void BeginInvokeIfCreated(Action a)
        {
            try { if (IsHandleCreated && !IsDisposed) BeginInvoke(a); } catch { }
        }
    }

    internal static class ControlExt
    {
        public static void BeginInvokeIfCreated(this Control c, Action a)
        {
            try { if (c != null && c.IsHandleCreated && !c.IsDisposed) c.BeginInvoke(a); } catch { }
        }
    }
}
