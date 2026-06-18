using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        private Panel headerPanel;
        private Panel contentPanel;
        private Panel bottomBar;
        private Panel currentView;
        private Panel logoPanel;
        private Label headerTitle;
        private Label headerSub;
        private Button backButton;
        private TextBox searchBox;
        private SystemFileIntegrity integrityScanner;
        private RegistryScanner registryScanner;
        private ServiceScanner serviceScanner;
        private FileAnalyzer fileAnalyzer;
        private HijackRemover hijackRemover;
        private System.Windows.Forms.Timer statusTimer;
        private bool isHome = true;

        private const int HeaderHeight = 70;
        private const int BottomBarHeight = 28;

        private struct ModuleDef
        {
            public string Tag, Icon, Title, Subtitle, Category;
            public Color Color;
        }

        private ModuleDef[] modules = new ModuleDef[]
        {
            new ModuleDef { Tag="Sys File Integrity", Icon="◆", Title="Repair Files", Subtitle="SFC / DISM / system file verification", Category="RECOVERY", Color=Color.FromArgb(45,212,191) },
            new ModuleDef { Tag="Registry Scan", Icon="ƒ", Title="Scan Registry", Subtitle="Full registry persistence & hijack scan", Category="RECOVERY", Color=Color.FromArgb(16,185,129) },
            new ModuleDef { Tag="Service Scan", Icon="⚙", Title="Scan Services", Subtitle="Detect suspicious Windows services", Category="RECOVERY", Color=Color.FromArgb(6,182,212) },
            new ModuleDef { Tag="Hijack Remover", Icon="⬚", Title="Fix Hijacks", Subtitle="Browser / DNS / proxy / Winsock / WMI", Category="RECOVERY", Color=Color.FromArgb(245,158,11) },
            new ModuleDef { Tag="Task Manager", Icon="▤", Title="Tasks", Subtitle="View & manage running processes", Category="MALWARE", Color=Color.FromArgb(45,212,191) },
            new ModuleDef { Tag="Process Killer", Icon="✖", Title="Kill Process", Subtitle="Force-kill protected / malicious processes", Category="MALWARE", Color=Color.FromArgb(244,63,94) },
            new ModuleDef { Tag="Startup Manager", Icon="↻", Title="Startup", Subtitle="Remove auto-start entries & tasks", Category="MALWARE", Color=Color.FromArgb(245,158,11) },
            new ModuleDef { Tag="File Analyzer", Icon="⌬", Title="Analyze File", Subtitle="PE header / hash / signature / API scan", Category="MALWARE", Color=Color.FromArgb(6,182,212) },
            new ModuleDef { Tag="Hotkey Fix", Icon="⌨", Title="Fix Hotkeys", Subtitle="Unblock Alt+Tab / Ctrl+Alt+Del / TaskMgr", Category="UNLOCK", Color=Color.FromArgb(16,185,129) },
            new ModuleDef { Tag="Font Protect", Icon="Aa", Title="Fix Fonts", Subtitle="Restore system fonts if tampered by malware", Category="UNLOCK", Color=Color.FromArgb(45,212,191) },
            new ModuleDef { Tag="UAC Bypass", Icon="🔓", Title="Elevate", Subtitle="5 UAC bypass methods + privilege escalation", Category="UNLOCK", Color=Color.FromArgb(245,158,11) },
            new ModuleDef { Tag="Registry Editor", Icon="📝", Title="Registry", Subtitle="Quick registry navigation & policy repair", Category="UNLOCK", Color=Color.FromArgb(6,182,212) },
            new ModuleDef { Tag="Hosts Editor", Icon="🌐", Title="Hosts File", Subtitle="Edit / reset Windows hosts file + flush DNS", Category="TOOLS", Color=Color.FromArgb(45,212,191) },
            new ModuleDef { Tag="System Tools", Icon="🔧", Title="Tools", Subtitle="16 repair utilities: icon cache, WMI, firewall...", Category="TOOLS", Color=Color.FromArgb(16,185,129) },
            new ModuleDef { Tag="File Browser", Icon="📁", Title="Files", Subtitle="Browse / search / manage files & folders", Category="TOOLS", Color=Color.FromArgb(6,182,212) },
            new ModuleDef { Tag="Explorer", Icon="▢", Title="Explorer", Subtitle="Launch Windows Explorer", Category="LAUNCH", Color=Color.FromArgb(45,212,191) },
            new ModuleDef { Tag="CMD", Icon="›_", Title="CMD", Subtitle="Launch Command Prompt as admin", Category="LAUNCH", Color=Color.FromArgb(245,158,11) },
            new ModuleDef { Tag="PowerShell", Icon="PS", Title="PowerShell", Subtitle="Launch PowerShell as admin", Category="LAUNCH", Color=Color.FromArgb(6,182,212) },
            new ModuleDef { Tag="About", Icon="ℹ", Title="About", Subtitle="Version info & credits", Category="INFO", Color=Color.FromArgb(100,116,130) },
        };

        public MainForm()
        {
            try
            {
                InitializeForm();
                SetupHeader();
                SetupContent();
                SetupBottomBar();
                integrityScanner = new SystemFileIntegrity();
                registryScanner = new RegistryScanner();
                serviceScanner = new ServiceScanner();
                fileAnalyzer = new FileAnalyzer();
                hijackRemover = new HijackRemover();
                ShowHome();
                statusTimer = new System.Windows.Forms.Timer { Interval = 2000 };
                statusTimer.Tick += (s, e) => UpdateStatusBar();
                statusTimer.Start();
                _ = CheckUpdateAsync();
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
            Size = new Size(1280, 760);
            MinimumSize = new Size(800, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;
            Font = Theme.UIFont;
            FormBorderStyle = FormBorderStyle.Sizable;
            DoubleBuffered = true;
            SetDarkTitleBar();
            SetAppIcon();
        }

        private void SetDarkTitleBar()
        {
            try { int d = 1; Win32.DwmSetWindowAttribute(Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref d, 4); } catch { }
        }

        private void SetAppIcon()
        {
            try
            {
                var bmp = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.FromArgb(8, 12, 16));
                    var rect = new Rectangle(2, 2, 28, 28);
                    var path = GraphicsExt.RoundedRect(rect, 7);
                    using (var brush = new LinearGradientBrush(rect, Theme.Accent, Theme.AccentDark, 90f))
                        g.FillPath(brush, path);
                    TextRenderer.DrawText(g, "MZ", new Font("Segoe UI", 10f, FontStyle.Bold), rect,
                        Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    path.Dispose();
                }
                Icon = Icon.FromHandle(bmp.GetHicon());
                bmp.Dispose();
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
            headerPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Theme.Border, 1))
                    g.DrawLine(pen, 0, HeaderHeight - 1, headerPanel.Width, HeaderHeight - 1);
            };

            logoPanel = new Panel
            {
                Size = new Size(44, 44),
                Location = new Point(20, 13),
                BackColor = Theme.Surface
            };
            logoPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, 43, 43);
                var path = GraphicsExt.RoundedRect(rect, 11);
                using (var brush = new LinearGradientBrush(rect, Theme.Accent, Theme.AccentDark, 90f))
                    g.FillPath(brush, path);
                TextRenderer.DrawText(g, "MZ", new Font("Segoe UI", 14f, FontStyle.Bold), rect,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                path.Dispose();
            };
            headerPanel.Controls.Add(logoPanel);

            headerTitle = new Label
            {
                Text = "MazizTool",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(76, 14),
                BackColor = Theme.Surface
            };
            headerSub = new Label
            {
                Text = "System Recovery & Anti-Malware Hub",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(76, 38),
                BackColor = Theme.Surface
            };
            headerPanel.Controls.Add(headerTitle);
            headerPanel.Controls.Add(headerSub);

            backButton = new Button
            {
                Text = "← Back",
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.SurfaceLight,
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Size = new Size(76, 32),
                Location = new Point(20, 19),
                Cursor = Cursors.Hand,
                Visible = false
            };
            backButton.FlatAppearance.BorderSize = 1;
            backButton.FlatAppearance.BorderColor = Theme.Border;
            backButton.Click += (s, e) => ShowHome();
            headerPanel.Controls.Add(backButton);

            searchBox = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(280, 32),
                Location = new Point(headerPanel.Width - 320, 19),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                PlaceholderText = "Search modules..."
            };
            searchBox.VisibleChanged += (s, e) =>
            {
                if (searchBox.Visible) searchBox.Location = new Point(headerPanel.Width - 320, 19);
            };
            headerPanel.Resize += (s, e) => { searchBox.Location = new Point(headerPanel.Width - 320, 19); };
            searchBox.TextChanged += (s, e) => { if (isHome) FilterHomeCards(searchBox.Text); };
            headerPanel.Controls.Add(searchBox);

            Controls.Add(headerPanel);
        }

        private void SetupContent()
        {
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(24, 80, 24, 8)
            };
            Controls.Add(contentPanel);
        }

        private void SetupBottomBar()
        {
            bottomBar = new Panel
            {
                Height = BottomBarHeight,
                Dock = DockStyle.Bottom,
                BackColor = Theme.Surface
            };
            bottomBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Theme.Border, 1))
                    g.DrawLine(pen, 0, 0, bottomBar.Width, 0);
                TextRenderer.DrawText(g, "MazizTool v6.0", new Font("Segoe UI", 8f),
                    new Rectangle(16, 6, 200, 16), Theme.TextMuted, TextFormatFlags.Left);
                var dotRect = new Rectangle(232, 9, 6, 6);
                using (var path = GraphicsExt.RoundedRect(dotRect, 3))
                using (var brush = new SolidBrush(Theme.Emerald))
                    g.FillPath(brush, path);
                TextRenderer.DrawText(g, "ready", new Font("Segoe UI", 8f),
                    new Rectangle(244, 6, 60, 16), Theme.TextMuted, TextFormatFlags.Left);
            };
            var memLabel = new Label
            {
                Font = new Font("Segoe UI", 8f), ForeColor = Theme.TextMuted,
                AutoSize = true, Anchor = AnchorStyles.Right, Name = "memLabel", BackColor = Theme.Surface
            };
            bottomBar.Controls.Add(memLabel);
            bottomBar.Resize += (s, e) => { memLabel.Location = new Point(bottomBar.Width - 180, 6); };
            Controls.Add(bottomBar);
        }

        private void UpdateStatusBar()
        {
            try
            {
                var proc = Process.GetCurrentProcess();
                var lbl = bottomBar.Controls.Find("memLabel", false).FirstOrDefault() as Label;
                if (lbl != null) lbl.Text = $"mem:{proc.WorkingSet64 / 1024 / 1024}MB · procs:{Process.GetProcesses().Length}";
            }
            catch { }
        }

        private void ShowHome()
        {
            isHome = true;
            backButton.Visible = false;
            logoPanel.Visible = true;
            headerTitle.Text = "MazizTool";
            headerSub.Text = "System Recovery & Anti-Malware Hub";
            searchBox.Visible = true;
            searchBox.Text = "";

            currentView?.Dispose();
            currentView = null;
            contentPanel.Controls.Clear();

            var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(0, 60, 0, 0) };

            var welcomeCard = new Panel
            {
                Size = new Size(1130, 70),
                Dock = DockStyle.Top,
                BackColor = Theme.Surface,
                Margin = new Padding(0, 0, 0, 12)
            };
            welcomeCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, welcomeCard.Width - 1, welcomeCard.Height - 1);
                var welcomePath = GraphicsExt.RoundedRect(rect, 12);
                using (var brush = new LinearGradientBrush(rect, Theme.Surface, Theme.SurfaceLight, 0f))
                    g.FillPath(brush, welcomePath);
                using (var pen = new Pen(Theme.Border, 1))
                    g.DrawPath(pen, welcomePath);
                welcomePath.Dispose();
                TextRenderer.DrawText(g, "Welcome to MazizTool", new Font("Segoe UI", 13f, FontStyle.Bold),
                    new Rectangle(20, 12, 400, 24), Theme.TextPrimary, TextFormatFlags.Left);
                TextRenderer.DrawText(g, "Select a module below or use search to find what you need", new Font("Segoe UI", 9f),
                    new Rectangle(20, 38, 600, 20), Theme.TextSecondary, TextFormatFlags.Left);
                var statRect = new Rectangle(welcomeCard.Width - 180, 20, 160, 40);
                using (var statPath = GraphicsExt.RoundedRect(statRect, 8))
                using (var statBrush = new SolidBrush(Theme.AccentDarker))
                    g.FillPath(statBrush, statPath);
                TextRenderer.DrawText(g, $"{modules.Length} modules", new Font("Segoe UI", 11f, FontStyle.Bold),
                    statRect, Theme.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            p.Controls.Add(welcomeCard);

            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true,
                BackColor = Theme.Background,
                Padding = new Padding(0, 24, 0, 8)
            };

            string lastCat = "";
            foreach (var mod in modules)
            {
                if (mod.Tag == "About") continue;
                if (mod.Category != lastCat)
                {
                    if (lastCat != "") flowPanel.Controls.Add(SeparatorRow(1130));
                    var catLabel = new Label
                    {
                        Text = mod.Category,
                        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                        ForeColor = Theme.Accent,
                        AutoSize = true,
                        Margin = new Padding(4, 8, 8, 4),
                        BackColor = Theme.Background
                    };
                    flowPanel.Controls.Add(catLabel);
                    lastCat = mod.Category;
                }
                var card = new FeatureCard
                {
                    Icon = mod.Icon,
                    Title = mod.Title,
                    Subtitle = mod.Subtitle,
                    Category = mod.Category,
                    IconBg = mod.Color,
                    Size = new Size(272, 140),
                    Margin = new Padding(8, 8, 8, 8),
                    Tag = mod.Tag
                };
                card.Click += (s, e) => NavigateTo(mod.Tag);
                flowPanel.Controls.Add(card);
            }

            p.Controls.Add(flowPanel);

            contentPanel.Controls.Add(p);
            currentView = p;
        }

        private void FilterHomeCards(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                foreach (Control c in contentPanel.Controls)
                    if (c is Panel p)
                        foreach (Control fc in p.Controls)
                            if (fc is FlowLayoutPanel fp)
                                foreach (FeatureCard card in fp.Controls) card.Visible = true;
                return;
            }
            var q = query.ToLower();
            foreach (Control c in contentPanel.Controls)
                if (c is Panel p)
                    foreach (Control fc in p.Controls)
                        if (fc is FlowLayoutPanel fp)
                            foreach (FeatureCard card in fp.Controls)
                                card.Visible = card.Title.ToLower().Contains(q) || card.Subtitle.ToLower().Contains(q) || card.Category.ToLower().Contains(q);
        }

        private void NavigateTo(string tag)
        {
            if (tag == "Explorer") { LaunchExplorer(); return; }
            if (tag == "CMD") { LaunchCmd(); return; }
            if (tag == "PowerShell") { LaunchPowerShell(); return; }
            if (tag == "About") { ShowAbout(); return; }

            isHome = false;
            backButton.Visible = true;
            logoPanel.Visible = false;
            searchBox.Visible = false;

            var mod = modules.FirstOrDefault(m => m.Tag == tag);
            headerTitle.Text = mod.Title;
            headerSub.Text = mod.Subtitle;

            currentView?.Dispose();
            currentView = null;
            contentPanel.Controls.Clear();

            var p = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, AutoScroll = true, Padding = new Padding(16, 40, 16, 12) };
            currentView = p;

            try
            {
                switch (tag)
                {
                    case "Sys File Integrity": ShowSystemFileIntegrity(p); break;
                    case "Registry Scan": ShowRegistryScanner(p); break;
                    case "Service Scan": ShowServiceScanner(p); break;
                    case "Hijack Remover": ShowHijackRemover(p); break;
                    case "Task Manager": ShowTaskManager(p); break;
                    case "Process Killer": ShowProcessKiller(p); break;
                    case "Startup Manager": ShowStartupManager(p); break;
                    case "File Analyzer": ShowFileAnalyzer(p); break;
                    case "Hotkey Fix": ShowHotkeyFix(p); break;
                    case "Font Protect": ShowFontProtect(p); break;
                    case "UAC Bypass": ShowUacBypass(p); break;
                    case "Registry Editor": ShowRegistryEditor(p); break;
                    case "Hosts Editor": ShowHostsEditor(p); break;
                    case "System Tools": ShowSystemTools(p); break;
                    case "File Browser": ShowFileBrowser(p); break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Module error: " + ex.Message, "MazizTool");
            }

            contentPanel.Controls.Add(p);
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
                ForeColor = Theme.Accent, AutoSize = true, Location = new Point(x, y), BackColor = Theme.Surface
            };
        }

        private Label Hint(string text, int x, int y)
        {
            return new Label
            {
                Text = text, Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(x, y), BackColor = Theme.Background
            };
        }

        private void Toast(string msg, Color? color = null, int duration = 2500) => ToastNotif.Show(this, msg, color ?? Theme.Accent, duration);

        private void SaveLog(string title, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) { Toast("Nothing to save."); return; }
            using (var sfd = new SaveFileDialog { Filter = "Text files|*.txt|Log files|*.log|All|*.*", FileName = title + ".txt" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, content);
                    Toast("Log saved: " + Path.GetFileName(sfd.FileName));
                }
            }
        }

        private bool Confirm(string msg)
        {
            return MessageBox.Show(msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private Panel SeparatorRow(int width)
        {
            var sep = new Panel { Height = 1, Width = width, BackColor = Theme.Border, Margin = new Padding(0, 4, 0, 4) };
            sep.Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                    e.Graphics.DrawLine(pen, 0, 0, sep.Width, 0);
            };
            return sep;
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
            var hTitle = new Label { Text = title, Font = Theme.HeaderFont, ForeColor = Theme.Accent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0), BackColor = Theme.Surface };
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

        private void ShowSystemFileIntegrity(Panel p)
        {
            int y = 60;
            var c1 = Card(8, y, 1130, 56);
            var sfc = Btn("SFC /SCANNOW", Theme.Accent, 16, 10, 180, 36);
            sfc.Click += (s, e) => { var f = CreateOutputForm("SFC /scannow"); _ = RunSfcInForm(f); };
            var dScan = Btn("DISM /SCAN", Theme.Accent, 202, 10, 180, 36);
            dScan.Click += (s, e) => { var f = CreateOutputForm("DISM /ScanHealth"); _ = RunDismScanInForm(f); };
            var dRest = Btn("DISM /RESTORE", Theme.Accent, 388, 10, 180, 36);
            dRest.Click += (s, e) => { var f = CreateOutputForm("DISM /RestoreHealth"); _ = RunDismInForm(f); };
            var dCl = Btn("DISM /CLEANUP", Theme.Accent, 574, 10, 180, 36);
            dCl.Click += (s, e) => { var f = CreateOutputForm("DISM /StartComponentCleanup"); _ = RunDismCleanupInForm(f); };
            c1.Controls.Add(sfc); c1.Controls.Add(dScan); c1.Controls.Add(dRest); c1.Controls.Add(dCl);
            p.Controls.Add(c1);
            y += 68;
            var c2 = Card(8, y, 1130, 56);
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
            foreach (var c in panel.Controls.OfType<Button>().ToList()) if (c.Text.Contains("Log")) { panel.Controls.Remove(c); c.Dispose(); }
            var box = new TextBox
            {
                Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(6, 10, 12),
                ForeColor = Theme.Accent, Font = new Font("Cascadia Code", 9f), BorderStyle = BorderStyle.None,
                Location = new Point(8, y + 36), Size = new Size(panel.Width - 48, panel.Height - y - 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                ScrollBars = ScrollBars.Both, WordWrap = false, Text = text
            };
            panel.Controls.Add(box);
            var saveBtn = new Button
            {
                Text = "Save Log", FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface,
                ForeColor = Theme.Accent, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Size = new Size(90, 28), Location = new Point(8, y + 4), Cursor = Cursors.Hand
            };
            saveBtn.FlatAppearance.BorderSize = 1;
            saveBtn.FlatAppearance.BorderColor = Theme.Border;
            saveBtn.Click += (s, e) => SaveLog("output", text);
            panel.Controls.Add(saveBtn);
            box.BringToFront();
        }

        private void ShowRegistryScanner(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 56);
            var scan = Btn("Scan Registry", Theme.Accent, 16, 10, 180, 36);
            var fix = Btn("Fix All Policies", Theme.Danger, 202, 10, 180, 36);
            fix.Click += (s, e) => { HotkeyRestorer.FixAllHotkeyBlocks(); SystemTools.FixGroupPolicies(); Toast("Policies fixed."); };
            c.Controls.Add(scan); c.Controls.Add(fix);
            p.Controls.Add(c);
            y += 68;
            var status = Hint("● ready", 12, y); p.Controls.Add(status); y += 24;
            var grid = Grid(8, y, 1130, 400);
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
        }

        private void ShowServiceScanner(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 56);
            var scan = Btn("Scan Services", Theme.Accent, 16, 10, 180, 36);
            var suspChk = new CheckBox { Text = "suspicious only", ForeColor = Theme.TextSecondary, Font = Theme.MonoFont, Location = new Point(206, 16), AutoSize = true, BackColor = Theme.Surface };
            var stop = Btn("Stop", Theme.Warning, 350, 10, 100, 36);
            var dis = Btn("Disable", Theme.Warning, 456, 10, 110, 36);
            var del = Btn("Delete", Theme.Danger, 572, 10, 110, 36);
            c.Controls.Add(scan); c.Controls.Add(suspChk); c.Controls.Add(stop); c.Controls.Add(dis); c.Controls.Add(del);
            p.Controls.Add(c);
            y += 68;
            var status = Hint("● ready", 12, y); p.Controls.Add(status); y += 24;
            var grid = Grid(8, y, 1130, 400);
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
                if (verb == "Delete" && !Confirm("Delete selected services?")) return;
                foreach (ListViewItem it in grid.SelectedItems)
                    if (it.Tag is ServiceScanner.ServiceInfo svc) a(svc.Name);
                Toast(verb + " executed.");
            }
            stop.Click += (s, e) => Act(ServiceScanner.StopService, "Stop");
            dis.Click += (s, e) => Act(ServiceScanner.DisableService, "Disable");
            del.Click += (s, e) => Act(ServiceScanner.DeleteService, "Delete");
            p.Controls.Add(grid);
        }

        private void ShowHijackRemover(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 56);
            var scan = Btn("Scan Hijacks", Theme.Accent, 16, 10, 180, 36);
            var fp = Btn("Fix Proxy", Theme.Accent, 202, 10, 150, 36);
            var fw = Btn("Fix Winsock", Theme.Accent, 358, 10, 150, 36);
            var fdns = Btn("Reset DNS", Theme.Accent, 514, 10, 150, 36);
            c.Controls.Add(scan); c.Controls.Add(fp); c.Controls.Add(fw); c.Controls.Add(fdns);
            p.Controls.Add(c);
            y += 68;
            var status = Hint("● ready", 12, y); p.Controls.Add(status); y += 24;
            var grid = Grid(8, y, 1130, 400);
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
        }

        private void ShowFileAnalyzer(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 60);
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
            var result = new TextBox
            {
                Location = new Point(8, y + 72), Size = new Size(1130, 400),
                Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(6, 10, 12),
                ForeColor = Theme.Accent, Font = new Font("Cascadia Code", 9f), BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Both, WordWrap = false
            };
            var analyze = Btn("Analyze", Theme.Accent, 524, 14, 140, 32);
            var saveFile = Btn("Save Log", Theme.Accent, 672, 14, 100, 32);
            saveFile.Click += (s, e) => SaveLog(Path.GetFileName(pathInput.Text), result.Text);
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
            c.Controls.Add(pathInput); c.Controls.Add(analyze); c.Controls.Add(saveFile);
            p.Controls.Add(c);
            p.Controls.Add(result);
        }

        private void ShowTaskManager(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 56);
            var refresh = Btn("Refresh", Theme.Accent, 16, 10, 110, 36);
            var kill = Btn("Kill", Theme.Danger, 132, 10, 100, 36);
            var killSys = Btn("Kill Non-Sys", Theme.Warning, 238, 10, 140, 36);
            c.Controls.Add(refresh); c.Controls.Add(kill); c.Controls.Add(killSys);
            p.Controls.Add(c);
            y += 68;
            var lv = Grid(8, y, 1130, 400);
            lv.Columns.Add("PID", 70); lv.Columns.Add("Name", 180); lv.Columns.Add("Path", 360);
            lv.Columns.Add("Mem(MB)", 90); lv.Columns.Add("Threads", 70);
            refresh.Click += (s, e) => RefreshTaskList(lv);
            kill.Click += (s, e) =>
            {
                foreach (ListViewItem it in lv.SelectedItems)
                    if (it.Tag is ProcessKiller.ProcessInfo pi) ProcessKiller.KillProcess(pi.Id);
                RefreshTaskList(lv);
            };
            killSys.Click += (s, e) => { if (Confirm("Kill all non-system processes?")) { ProcessKiller.KillNonSystemProcesses(); RefreshTaskList(lv); } };
            p.Controls.Add(lv);
            RefreshTaskList(lv);
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

        private void ShowProcessKiller(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 60);
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
            var title = new Label { Text = "SUSPICIOUS PROCESSES", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Theme.Accent, AutoSize = true, Location = new Point(8, y), BackColor = Theme.Background };
            p.Controls.Add(title);
            y += 24;
            var lv = Grid(8, y, 1130, 240);
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
        }

        private void ShowStartupManager(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 56);
            var refresh = Btn("Refresh", Theme.Accent, 16, 10, 110, 36);
            var remove = Btn("Remove", Theme.Danger, 132, 10, 110, 36);
            var clean = Btn("Clean All", Theme.Warning, 248, 10, 130, 36);
            c.Controls.Add(refresh); c.Controls.Add(remove); c.Controls.Add(clean);
            p.Controls.Add(c);
            y += 68;
            var lv = Grid(8, y, 1130, 400);
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
        }

        private void ShowRegistryEditor(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 60);
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
                Location = new Point(8, y), Size = new Size(1130, 220),
                Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(6, 10, 12),
                ForeColor = Theme.Accent, Font = new Font("Cascadia Code", 9f), BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical, WordWrap = false
            };
            go.Click += (s, e) => { result.Clear(); NavigateRegistry(pathInput.Text, result); };
            p.Controls.Add(result);
            y += 232;
            var re = Btn("Open regedit", Theme.Accent, 8, y, 150, 36);
            re.Click += (s, e) => { HotkeyRestorer.RestoreRegistryEditor(); try { Process.Start("regedit.exe"); } catch { } };
            var fp = Btn("Fix Policies", Theme.Warning, 166, y, 150, 36);
            fp.Click += (s, e) => { SystemTools.FixGroupPolicies(); HotkeyRestorer.FixAllHotkeyBlocks(); Toast("Policies fixed."); };
            p.Controls.Add(re); p.Controls.Add(fp);
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

        private void ShowFileBrowser(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 60);
            var pathInput = new TextBox
            {
                Location = new Point(16, 14), Size = new Size(500, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f), Text = @"C:\"
            };
            var browse = Btn("Browse", Theme.Accent, 524, 14, 100, 32);
            c.Controls.Add(pathInput); c.Controls.Add(browse);
            p.Controls.Add(c);
            y += 72;
            var c2 = Card(8, y, 1130, 52);
            var searchInput = new TextBox
            {
                Location = new Point(16, 10), Size = new Size(300, 32),
                BackColor = Theme.InputBg, ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 9f),
                PlaceholderText = "search files..."
            };
            var search = Btn("Search", Theme.Accent, 322, 10, 100, 32);
            c2.Controls.Add(searchInput); c2.Controls.Add(search);
            p.Controls.Add(c2);
            y += 64;
            var lv = Grid(8, y, 1130, 400);
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

        private void ShowHostsEditor(Panel p)
        {
            int y = 60;
            var hostsPath = Path.Combine(Environment.SystemDirectory, "drivers", "etc", "hosts");
            var content = new TextBox
            {
                Location = new Point(8, y), Size = new Size(1130, 320),
                Multiline = true, BackColor = Color.FromArgb(6, 10, 12), ForeColor = Theme.Accent,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Cascadia Code", 10f),
                ScrollBars = ScrollBars.Vertical, WordWrap = false,
                Text = File.Exists(hostsPath) ? File.ReadAllText(hostsPath) : ""
            };
            p.Controls.Add(content);
            y += 330;
            var save = Btn("Save", Theme.Accent, 8, y, 100, 36);
            var reload = Btn("Reload", Theme.Accent, 116, y, 100, 36);
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
        }

        private void ShowUacBypass(Panel p)
        {
            int y = 60;
            var c = Card(8, y, 1130, 60);
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
            var priv = Btn("Enable SeDebugPriv", Theme.Accent, 8, y2 + 46, 300, 40);
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
        }

        private void ShowHotkeyFix(Panel p)
        {
            int y = 60;
            var actions = new (string, Color, Action)[]
            {
                ("Fix All Restrictions", Theme.Accent, () => { HotkeyRestorer.RestoreAll(); SystemTools.FixGroupPolicies(); Toast("All restrictions removed."); }),
                ("Enable Alt+Tab", Theme.Accent, () => { HotkeyRestorer.EnableAltTab(); Toast("Alt+Tab restored."); }),
                ("Enable Ctrl+Alt+Del", Theme.Accent, () => { HotkeyRestorer.EnableCtrlAltDel(); Toast("Ctrl+Alt+Del restored."); }),
                ("Restore TaskMgr", Theme.Accent, () => { HotkeyRestorer.RestoreTaskManager(); Toast("Task Manager unblocked."); }),
                ("Restore Regedit", Theme.Accent, () => { HotkeyRestorer.RestoreRegistryEditor(); Toast("Regedit unblocked."); }),
                ("Restore Hotkeys", Theme.Accent, () => { HotkeyRestorer.RestoreHotkeys(); Toast("Hotkeys restored."); }),
                ("Fix Group Policies", Theme.Warning, () => { SystemTools.FixGroupPolicies(); Toast("Policies reset."); }),
                ("Show Hidden Files", Theme.Accent, () => { SystemTools.RestoreHiddenFiles(); Toast("Hidden files shown."); }),
            };
            int bw = (1130 - 8) / 2;
            for (int i = 0; i < actions.Length; i++)
            {
                var (name, color, act) = actions[i];
                int col = i % 2, row = i / 2;
                var b = Btn(name, color, 8 + col * (bw + 8), y + row * 48, bw, 42);
                b.Click += (s, e) => act();
                p.Controls.Add(b);
            }
        }

        private void ShowFontProtect(Panel p)
        {
            int y = 60;
            bool tampered = FontProtector.IsSystemFontTampered();
            var c = Card(8, y, 1130, 100);
            var status = new Label
            {
                Text = tampered ? "⚠  FONT TAMPERING DETECTED" : "✓  system fonts appear normal",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = tampered ? Theme.Danger : Theme.Emerald,
                AutoSize = true, Location = new Point(16, 16), BackColor = Theme.Surface
            };
            var info = new Label
            {
                Text = "malware may substitute Segoe UI with blank fonts to hide text.\nthis restores registry font settings & broadcasts WM_FONTCHANGE.",
                Font = Theme.MonoFont, ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(16, 48), BackColor = Theme.Surface
            };
            c.Controls.Add(status); c.Controls.Add(info);
            p.Controls.Add(c);
            y += 112;
            var restore = Btn("Restore System Fonts", Theme.Accent, 8, y, 260, 42);
            restore.Click += (s, e) => { FontProtector.RestoreSystemFonts(); Toast("Fonts restored. Reboot recommended."); };
            p.Controls.Add(restore);
        }

        private void ShowSystemTools(Panel p)
        {
            int y = 60;
            var tools = new (string, Color, Action)[]
            {
                ("Restore .exe Assoc", Theme.Accent, () => { SystemTools.RestoreExeAssociations(); Toast("Done."); }),
                ("Reset Network", Theme.Accent, () => { SystemTools.ResetNetworkSettings(); Toast("Done."); }),
                ("Clear Temp Files", Theme.Accent, () => { SystemTools.ClearTempFiles(); Toast("Done."); }),
                ("Create Restore Pt", Theme.Accent, () => { SystemTools.CreateRestorePoint("MazizTool"); Toast("Done."); }),
                ("CHKDSK C: /f /r", Theme.Warning, () => { if (Confirm("Run CHKDSK C: /f /r? (may require reboot)")) SystemTools.CheckDisk(); }),
                ("Safe Mode +Net", Theme.Accent, () => { if (Confirm("Enable Safe Mode with Networking? (reboot needed)")) { SystemTools.EnableSafeModeNetworking(); Toast("Safe mode enabled."); } }),
                ("Disable Safe Mode", Theme.Accent, () => { if (Confirm("Disable Safe Mode boot flag?")) { SystemTools.DisableSafeMode(); Toast("Done."); } }),
                ("Fix Hosts File", Theme.Accent, () => { SystemTools.FixHostsFile(); Toast("Done."); }),
                ("Show Hidden Files", Theme.Accent, () => { SystemTools.RestoreHiddenFiles(); Toast("Done."); }),
                ("Rebuild Icon Cache", Theme.Accent, () => { SystemTools.RebuildIconCache(); Toast("Icon cache rebuilt."); }),
                ("Repair WMI", Theme.Warning, () => { if (Confirm("Repair WMI repository?")) { SystemTools.RepairWMI(); Toast("WMI repaired."); } }),
                ("Reset Firewall", Theme.Warning, () => { if (Confirm("Reset Windows Firewall to defaults?")) { SystemTools.ResetFirewall(); Toast("Firewall reset."); } }),
                ("Fix COM Reg", Theme.Accent, () => { SystemTools.FixCOMRegistration(); Toast("COM fixed."); }),
                ("Rebuild Font Cache", Theme.Accent, () => { SystemTools.RebuildFontCache(); Toast("Font cache rebuilt."); }),
                ("Fix Print Spooler", Theme.Accent, () => { SystemTools.FixPrintSpooler(); Toast("Print spooler fixed."); }),
                ("Restart Explorer", Theme.Accent, () => { if (Confirm("Restart Windows Explorer?")) { SystemTools.RestoreExplorer(); Toast("Explorer restarted."); } }),
            };
            int bw = (1130 - 8) / 2;
            for (int i = 0; i < tools.Length; i++)
            {
                var (name, color, act) = tools[i];
                int col = i % 2, row = i / 2;
                var b = Btn(name, color, 8 + col * (bw + 8), y + row * 48, bw, 42);
                b.Click += (s, e) => act();
                p.Controls.Add(b);
            }
        }

        private void ShowAbout()
        {
            using (var dlg = new Form
            {
                Text = "About MazizTool",
                Size = new Size(440, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = Theme.UIFont
            })
            {
                try { int d = 1; Win32.DwmSetWindowAttribute(dlg.Handle, Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref d, 4); } catch { }
                var logo = new Label
                {
                    Text = "MZ", Font = new Font("Segoe UI", 32f, FontStyle.Bold),
                    ForeColor = Theme.Accent, TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Top, Height = 90, BackColor = Theme.Surface
                };
                logo.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    var rect = new Rectangle(logo.Width / 2 - 30, 14, 60, 60);
                    var path = GraphicsExt.RoundedRect(rect, 15);
                    using (var brush = new LinearGradientBrush(rect, Theme.Accent, Theme.AccentDark, 90f))
                        g.FillPath(brush, path);
                    TextRenderer.DrawText(g, "MZ", new Font("Segoe UI", 22f, FontStyle.Bold), rect,
                        Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    path.Dispose();
                };
                dlg.Controls.Add(logo);
                var info = new Label
                {
                    Text = "MazizTool v6.0\nSystem Recovery & Anti-Malware Hub",
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Theme.TextPrimary, Dock = DockStyle.Top, Height = 50,
                    TextAlign = ContentAlignment.MiddleCenter, BackColor = Theme.Surface
                };
                dlg.Controls.Add(info);
                var desc = new Label
                {
                    Text = "19 recovery modules · SFC/DISM · Registry & Service scanners\nTask Manager · Process Killer · File Analyzer\nHotkey Restorer · UAC Bypass · Hijack Remover · System Tools\n\nBuilt with .NET 8 · Windows Forms",
                    Font = new Font("Segoe UI", 9f), ForeColor = Theme.TextMuted,
                    Dock = DockStyle.Top, Height = 120, TextAlign = ContentAlignment.MiddleCenter, BackColor = Theme.Surface
                };
                dlg.Controls.Add(desc);
                var ok = new Button
                {
                    Text = "OK", FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent,
                    ForeColor = Color.Black, Size = new Size(100, 34),
                    Location = new Point(170, 280), Cursor = Cursors.Hand
                };
                ok.FlatAppearance.BorderSize = 0;
                ok.Click += (s, e) => dlg.Close();
                dlg.Controls.Add(ok);
                dlg.ShowDialog(this);
            }
        }

        private void LaunchExplorer() { try { Process.Start("explorer.exe"); } catch { } }
        private void LaunchCmd() { try { Process.Start(new ProcessStartInfo("cmd.exe") { UseShellExecute = true, Verb = "runas" }); } catch { } }
        private void LaunchPowerShell() { try { Process.Start(new ProcessStartInfo("powershell.exe") { UseShellExecute = true, Verb = "runas" }); } catch { } }

        private async Task CheckUpdateAsync()
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("MazizTool/6.5");
                    var json = await client.GetStringAsync("https://api.github.com/repos/mazizz23/MazizTool/releases/latest");
                    var tagLine = json.Split('\n').FirstOrDefault(l => l.Contains("\"tag_name\""));
                    if (tagLine != null)
                    {
                        var latest = tagLine.Split(':')[1].Trim().Trim('"', ',').Trim();
                        BeginInvokeIfCreated(() =>
                        {
                            Toast("Update available: " + latest, Theme.Warning, 4000);
                            var lbl = bottomBar.Controls.Find("memLabel", false).FirstOrDefault() as Label;
                            if (lbl != null) lbl.Text = "update: " + latest;
                        });
                    }
                }
            }
            catch { }
        }

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
