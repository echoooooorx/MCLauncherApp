using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

namespace MinecraftLauncher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => ShowFriendlyError(e.Exception, false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowFriendlyError(e.ExceptionObject as Exception, true);
            Application.Run(new MainForm());
        }
        static void ShowFriendlyError(Exception ex, bool terminating)
        {
            try
            {
                string msg = "Something went wrong" + (terminating ? ", and the program needs to close." : ", and the program will keep running.")
                    + "\n\n" + (ex != null ? ex.Message : "")
                    + "\n\nIf it keeps happening, just send this info to Naiwa.";
                InfoBox.Show(msg, "Minecraft Multiplayer Helper");
            }
            catch { }
        }
    }

    // ---------- 配置 ----------
    class Config
    {
        public string ServerDir = "D:\\MCServer";
        public string JavaPath = "";
        public string JarName = "server.jar";
        public string MemParams = "-Xms1G -Xmx2G";
        public int Port = 25565;
        public string FrpcPath = "frpc\\frpc.exe";
        public string FrpKey = "";
        public string TunnelId = "";
        public string VoiceTunnelId = "";
        public string PublicAddr = "";
        public string Pcl2Path = "";
        public string TunnelMode = "frpc"; // frpc / launcher
        public int WinX = -1;
        public int WinY = -1;
        // v6.11: backup / watchdog / schedule settings
        public int BackupKeep = 10;
        public bool BackupOnStart = false;
        public bool AutoRestart = true;
        public bool DailyBackup = false;
        public string DailyBackupTime = "03:00";
        public bool TimedCmdEnabled = false;
        public string TimedCmdTime = "23:00";
        public string TimedCmdText = "";
        public string LastDailyBackup = "";
        public string LastTimedCmd = "";
        public int LastTab = 0; // v6.13: remembered log tab

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"); }
        }

        static Dictionary<string, string> ReadDict(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path, Encoding.UTF8);
                var d = new Dictionary<string, string>();
                var ms = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]*)\"");
                foreach (Match m in ms) { d[m.Groups[1].Value] = JsonUnesc(m.Groups[2].Value); }
                return d.Count > 0 ? d : null;
            }
            catch { return null; }
        }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                // v6.10: if the main file is missing/truncated (an interrupted
                // write used to silently reset everything, FrpKey included),
                // fall back to the .bak the atomic Save keeps.
                var d = ReadDict(FilePath);
                var bak = ReadDict(FilePath + ".bak");
                if (bak != null && (d == null || bak.Count > d.Count)) d = bak;
                if (d != null)
                {
                    if (d.ContainsKey("ServerDir")) c.ServerDir = d["ServerDir"];
                    if (d.ContainsKey("JavaPath")) c.JavaPath = d["JavaPath"];
                    if (d.ContainsKey("JarName")) c.JarName = d["JarName"];
                    if (d.ContainsKey("MemParams")) c.MemParams = d["MemParams"];
                    if (d.ContainsKey("Port")) { int p; if (int.TryParse(d["Port"], out p)) c.Port = p; }
                    if (d.ContainsKey("FrpcPath")) c.FrpcPath = d["FrpcPath"];
                    if (d.ContainsKey("FrpKey")) c.FrpKey = d["FrpKey"];
                    if (d.ContainsKey("TunnelId")) c.TunnelId = d["TunnelId"];
                    if (d.ContainsKey("VoiceTunnelId")) c.VoiceTunnelId = d["VoiceTunnelId"];
                    if (d.ContainsKey("PublicAddr")) c.PublicAddr = d["PublicAddr"];
                    if (d.ContainsKey("Pcl2Path")) c.Pcl2Path = d["Pcl2Path"];
                    if (d.ContainsKey("TunnelMode")) c.TunnelMode = d["TunnelMode"];
                    if (d.ContainsKey("WinX")) { int wx; if (int.TryParse(d["WinX"], out wx)) c.WinX = wx; }
                    if (d.ContainsKey("WinY")) { int wy; if (int.TryParse(d["WinY"], out wy)) c.WinY = wy; }
                    if (d.ContainsKey("BackupKeep")) { int bk; if (int.TryParse(d["BackupKeep"], out bk) && bk >= 1 && bk <= 99) c.BackupKeep = bk; }
                    if (d.ContainsKey("BackupOnStart")) c.BackupOnStart = d["BackupOnStart"] == "true";
                    if (d.ContainsKey("AutoRestart")) c.AutoRestart = d["AutoRestart"] != "false";
                    if (d.ContainsKey("DailyBackup")) c.DailyBackup = d["DailyBackup"] == "true";
                    if (d.ContainsKey("DailyBackupTime")) c.DailyBackupTime = d["DailyBackupTime"];
                    if (d.ContainsKey("TimedCmdEnabled")) c.TimedCmdEnabled = d["TimedCmdEnabled"] == "true";
                    if (d.ContainsKey("TimedCmdTime")) c.TimedCmdTime = d["TimedCmdTime"];
                    if (d.ContainsKey("TimedCmdText")) c.TimedCmdText = d["TimedCmdText"];
                    if (d.ContainsKey("LastDailyBackup")) c.LastDailyBackup = d["LastDailyBackup"];
                    if (d.ContainsKey("LastTimedCmd")) c.LastTimedCmd = d["LastTimedCmd"];
                    if (d.ContainsKey("LastTab")) { int lt; if (int.TryParse(d["LastTab"], out lt) && lt >= 0 && lt <= 3) c.LastTab = lt; }
                }
            }
            catch { }
            AutoFill(c);
            return c;
        }

        public void Save()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"ServerDir\":\"" + JsonEsc(ServerDir) + "\",");
            sb.Append("\"JavaPath\":\"" + JsonEsc(JavaPath) + "\",");
            sb.Append("\"JarName\":\"" + JsonEsc(JarName) + "\",");
            sb.Append("\"MemParams\":\"" + JsonEsc(MemParams) + "\",");
            sb.Append("\"Port\":\"" + Port + "\",");
            sb.Append("\"FrpcPath\":\"" + JsonEsc(FrpcPath) + "\",");
            sb.Append("\"FrpKey\":\"" + JsonEsc(FrpKey) + "\",");
            sb.Append("\"TunnelId\":\"" + JsonEsc(TunnelId) + "\",");
            sb.Append("\"VoiceTunnelId\":\"" + JsonEsc(VoiceTunnelId) + "\",");
            sb.Append("\"PublicAddr\":\"" + JsonEsc(PublicAddr) + "\",");
            sb.Append("\"Pcl2Path\":\"" + JsonEsc(Pcl2Path) + "\",");
            sb.Append("\"TunnelMode\":\"" + JsonEsc(TunnelMode) + "\",");
            sb.Append("\"WinX\":\"" + WinX + "\",");
            sb.Append("\"WinY\":\"" + WinY + "\",");
            sb.Append("\"BackupKeep\":\"" + BackupKeep + "\",");
            sb.Append("\"BackupOnStart\":\"" + (BackupOnStart ? "true" : "false") + "\",");
            sb.Append("\"AutoRestart\":\"" + (AutoRestart ? "true" : "false") + "\",");
            sb.Append("\"DailyBackup\":\"" + (DailyBackup ? "true" : "false") + "\",");
            sb.Append("\"DailyBackupTime\":\"" + JsonEsc(DailyBackupTime) + "\",");
            sb.Append("\"TimedCmdEnabled\":\"" + (TimedCmdEnabled ? "true" : "false") + "\",");
            sb.Append("\"TimedCmdTime\":\"" + JsonEsc(TimedCmdTime) + "\",");
            sb.Append("\"TimedCmdText\":\"" + JsonEsc(TimedCmdText) + "\",");
            sb.Append("\"LastDailyBackup\":\"" + JsonEsc(LastDailyBackup) + "\",");
            sb.Append("\"LastTab\":\"" + LastTab + "\",");
            sb.Append("\"LastTimedCmd\":\"" + JsonEsc(LastTimedCmd) + "\"");
            sb.Append("}");
            // v6.10: write to a temp file, then swap atomically, keeping the
            // previous good file as .bak - a crash mid-write can no longer
            // truncate the only copy of the config.
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), Encoding.UTF8);
                if (File.Exists(FilePath)) File.Replace(tmp, FilePath, FilePath + ".bak");
                else File.Move(tmp, FilePath);
            }
            catch { }
        }

        static string JsonEsc(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\""); }

        static string JsonUnesc(string s) { if (s == null) return s; return s.Replace("\\\"", "\u0001").Replace("\\\\", "\\").Replace("\u0001", "\""); }

        // 从 start.bat / server.properties 自动补全空项
        static void AutoFill(Config c)
        {
            try
            {
                string bat = Path.Combine(c.ServerDir, "start.bat");
                if (File.Exists(bat) && string.IsNullOrEmpty(c.JavaPath))
                {
                    string txt = File.ReadAllText(bat, Encoding.UTF8);
                    var mj = Regex.Match(txt, "\"([^\"]+java\\.exe)\"");
                    if (mj.Success) c.JavaPath = mj.Groups[1].Value;
                    var mx = Regex.Match(txt, "-Xmx([0-9A-Za-z]+)");
                    var ms = Regex.Match(txt, "-Xms([0-9A-Za-z]+)");
                    if (mx.Success && ms.Success)
                        c.MemParams = "-Xms" + ms.Groups[1].Value + " -Xmx" + mx.Groups[1].Value;
                    var mj2 = Regex.Match(txt, "-jar\\s+([^\\s\"]+)\\.jar");
                    if (mj2.Success) c.JarName = mj2.Groups[1].Value + ".jar";
                }
                string props = Path.Combine(c.ServerDir, "server.properties");
                if (File.Exists(props))
                {
                    foreach (string line in File.ReadAllLines(props))
                    {
                        var mp = Regex.Match(line, "^server-port=(\\d+)$");
                        if (mp.Success) { int p; if (int.TryParse(mp.Groups[1].Value, out p)) c.Port = p; }
                    }
                }
            }
            catch { }
        }
    }

    // ---------- 自定义确认框 ----------
    // v6.9: same treatment as InfoBox - self-painted message, height fitted
    // to the measured text, amber status dot (this box always asks for a
    // real choice). The old fixed 440x150 TextLabel box read as a different
    // generation next to the rebuilt InfoBox.
    class ConfirmBox : DarkDialog
    {
        public bool ChoiceYes = false;
        readonly string _message;
        RoundButton _yes, _no;
        Font _msgFont;
        Rectangle _msgRect, _dotRect;

        public ConfirmBox(string message)
        {
            _message = message ?? "";
            InitChrome("Minecraft Multiplayer Helper", 440, 190);
            _yes = new RoundButton();
            _yes.Primary = true;
            _yes.Text = "Stop the server safely before exiting";
            _yes.BackColor = Pal.Green;
            _yes.ForeColor = Color.White;
            _yes.BackdropColor = Pal.Bg;
            _yes.SetBounds(20, 95, 180, 34);
            _yes.Click += (s, e) => { ChoiceYes = true; DialogResult = DialogResult.OK; };
            Controls.Add(_yes);
            _no = new RoundButton();
            _no.Text = "Cancel";
            _no.BackColor = Pal.Surface;
            _no.ForeColor = Color.White;
            _no.BackdropColor = Pal.Bg;
            _no.SetBounds(220, 95, 100, 34);
            _no.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(_no);
            AcceptButton = _yes;
            CancelButton = _no;
            FinishChrome();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                float sf = ClientSize.Width / 440f;
                _msgFont = new Font(Font.FontFamily, Font.Size + 1f, FontStyle.Regular);
                int textX = (int)Math.Round(42 * sf);
                int textW = ClientSize.Width - textX - (int)Math.Round(24 * sf);
                int textTop = (int)Math.Round((40 + 18) * sf);
                var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
                Size sz;
                using (var g = CreateGraphics())
                    sz = TextRenderer.MeasureText(g, _message, _msgFont, new Size(textW, int.MaxValue), flags);
                _msgRect = new Rectangle(textX, textTop, textW, sz.Height);
                int dotD = Math.Max(6, (int)Math.Round(8 * sf));
                _dotRect = new Rectangle((int)Math.Round(24 * sf), textTop + Math.Max(2, (int)Math.Round(4 * sf)), dotD, dotD);
                int btnTop = textTop + sz.Height + (int)Math.Round(20 * sf);
                int bottom = btnTop + _yes.Height + (int)Math.Round(18 * sf);
                _yes.Top = btnTop;
                _no.Top = btnTop;
                Size = new Size(Width, bottom);
                var wa = Screen.FromControl(this).WorkingArea;
                Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
                Invalidate();
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(Pal.Amber)) g.FillEllipse(b, _dotRect);
            if (_msgFont != null)
                TextRenderer.DrawText(g, _message, _msgFont, _msgRect, Pal.Text,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { try { if (_msgFont != null) _msgFont.Dispose(); } catch { } }
            base.Dispose(disposing);
        }
    }

    // ---------- 设置页 ----------
    class SettingsForm : DarkDialog
    {
        private TextBox tbServerDir, tbJava, tbJar, tbMem, tbPort, tbFrpc, tbKey, tbTunnelId, tbVoiceTunnelId, tbAddr, tbPcl2;
        private ComboBox cbTunnelMode;
        private Config _cfg;
        private TextLabel _errLbl;
        public bool ServerRunning = false;

        public SettingsForm(Config cfg)
        {
            _cfg = cfg;
            InitChrome("Settings", 620, 620);
            AutoScroll = true;

            int y = 14;
            int lx = 20, tx = 170, tw = 420;

            AddField("Server folder D:\\MCServer", "tbServerDir", cfg.ServerDir, ref y, lx, tx, tw, out tbServerDir);
            AddField("Full Java path (java.exe)", "tbJava", cfg.JavaPath, ref y, lx, tx, tw, out tbJava);
            AddField("jar file name", "tbJar", cfg.JarName, ref y, lx, tx, tw, out tbJar);
            AddField("Memory arguments", "tbMem", cfg.MemParams, ref y, lx, tx, tw, out tbMem);
            AddField("Port", "tbPort", cfg.Port.ToString(), ref y, lx, tx, tw, out tbPort);
            AddField("frpc.exe path", "tbFrpc", cfg.FrpcPath, ref y, lx, tx, tw, out tbFrpc);

            var lbMode = new TextLabel();
            lbMode.TopAlign = true;
            lbMode.Caption = "Tunnel mode";
            lbMode.TextShiftX = 2f; lbMode.TextShiftY = -0.67f;
            lbMode.SetBounds(lx, y, 140, 26);
            lbMode.BackColor = Color.Transparent; lbMode.BackdropColor = Pal.Bg; lbMode.ForeColor = Pal.Sub;
            Controls.Add(lbMode);
            cbTunnelMode = new ComboBox();
            cbTunnelMode.Items.Add("frpc command line (preferred)");
            cbTunnelMode.Items.Add("Open the official SakuraFrp launcher (fallback)");
            cbTunnelMode.SelectedIndex = (cfg.TunnelMode == "launcher") ? 1 : 0;
            cbTunnelMode.SetBounds(tx, y, tw, 26);
            cbTunnelMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTunnelMode.FlatStyle = FlatStyle.Flat;
            cbTunnelMode.BackColor = Pal.Surface;
            cbTunnelMode.ForeColor = Color.White;
            Controls.Add(cbTunnelMode);
            ThemedCombo.Attach(cbTunnelMode);
            y += 36;

            var lbKey = new TextLabel();
            lbKey.TopAlign = true;
            lbKey.Caption = "SakuraFrp access key";
            lbKey.TextShiftX = 2f; lbKey.TextShiftY = -0.67f;
            lbKey.SetBounds(lx, y, 140, 26);
            lbKey.BackColor = Color.Transparent; lbKey.BackdropColor = Pal.Bg; lbKey.ForeColor = Pal.Sub;
            Controls.Add(lbKey);
            tbKey = new TextBox();
            tbKey.SetBounds(tx, y, tw, 26);
            tbKey.BackColor = Pal.Surface;
            tbKey.ForeColor = Color.White;
            tbKey.BorderStyle = BorderStyle.FixedSingle;
            tbKey.UseSystemPasswordChar = true;
            tbKey.Text = cfg.FrpKey;
            Controls.Add(tbKey);
            y += 36;

            AddField("Tunnel ID", "tbTunnelId", cfg.TunnelId, ref y, lx, tx, tw, out tbTunnelId);
            AddField("Voice tunnel ID (optional)", "tbVoiceTunnelId", cfg.VoiceTunnelId, ref y, lx, tx, tw, out tbVoiceTunnelId);
            AddField("Public address (optional)", "tbAddr", cfg.PublicAddr, ref y, lx, tx, tw, out tbAddr);
            AddField("PCL2 exe path", "tbPcl2", cfg.Pcl2Path, ref y, lx, tx, tw, out tbPcl2);

            _errLbl = new TextLabel();
            _errLbl.TopAlign = true;
            _errLbl.WordWrap = true; // v6.9: a longer error may take two lines
            _errLbl.TextShiftX = 2f; _errLbl.TextShiftY = -0.67f;
            _errLbl.SetBounds(tx, y + 2, tw, 40);
            _errLbl.ForeColor = Pal.DangerText; // v6.14
            _errLbl.BackColor = Color.Transparent; _errLbl.BackdropColor = Pal.Bg;
            Controls.Add(_errLbl);

            var btnProps = new RoundButton();
            btnProps.Text = "Server Properties…";
            btnProps.BackColor = Pal.Surface;
            btnProps.ForeColor = Color.White;
            btnProps.SetBounds(20, y + 52, 180, 38);
            btnProps.Click += (s, e) =>
            {
                string dir = tbServerDir.Text.Trim();
                if (dir.Length == 0 || !System.IO.Directory.Exists(dir)) { InfoBox.Show("Set the correct server folder above first, then edit server properties.", "Server Properties"); return; }
                using (var pf = new PropsForm(dir, ServerRunning)) pf.ShowDialog(this);
            };
            Controls.Add(btnProps);

            var btnWizard = new RoundButton();
            btnWizard.Text = "First-run wizard";
            btnWizard.BackColor = Pal.Surface;
            btnWizard.ForeColor = Color.White;
            btnWizard.SetBounds(212, y + 52, 150, 38);
            btnWizard.Click += (s, e) =>
            {
                using (var wf = new WizardForm(_cfg, tbServerDir.Text.Trim()))
                {
                    if (wf.ShowDialog(this) == DialogResult.OK)
                    {
                        tbServerDir.Text = _cfg.ServerDir;
                        tbJava.Text = _cfg.JavaPath;
                        tbJar.Text = _cfg.JarName;
                        _errLbl.Caption = "";
                    }
                }
            };
            Controls.Add(btnWizard);

            var btnSave = new RoundButton();
            btnSave.Primary = true;
            btnSave.Text = "Save";
            btnSave.BackColor = Pal.Green;
            btnSave.ForeColor = Color.White;
            btnSave.SetBounds(374, y + 52, 100, 38);
            btnSave.Click += (s, e) => { if (Save()) DialogResult = DialogResult.OK; };
            Controls.Add(btnSave);

            var btnCancel = new RoundButton();
            btnCancel.Text = "Cancel";
            btnCancel.BackColor = Pal.Surface;
            btnCancel.ForeColor = Color.White;
            btnCancel.SetBounds(486, y + 52, 100, 38);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(btnCancel);

            ClientSize = new Size(620, Math.Min(y + 130, 710) + 40);
            AcceptButton = btnSave;
            FinishChrome();
        }

        void AddField(string label, string tag, string value, ref int y, int lx, int tx, int tw, out TextBox tb)
        {
            var lb = new TextLabel();
            lb.TopAlign = true;
            lb.Caption = label;
            lb.TextShiftX = 2f; lb.TextShiftY = -0.67f;
            lb.SetBounds(lx, y, 140, 26);
            lb.BackColor = Color.Transparent; lb.BackdropColor = Pal.Bg;
            lb.ForeColor = Pal.Sub;
            Controls.Add(lb);
            tb = new TextBox();
            tb.SetBounds(tx, y, tw, 26);
            tb.BackColor = Pal.Surface;
            tb.ForeColor = Color.White;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Text = value ?? "";
            var tbLocal = tb;
            tbLocal.TextChanged += (s, e) => { tbLocal.BackColor = Pal.Surface; };
            Controls.Add(tb);
            y += 36;
        }

        bool MarkInvalid(TextBox tb, string msg)
        {
            tb.BackColor = Gfx.Mix(Pal.Surface, Pal.Danger, 0.30);
            if (_errLbl != null && string.IsNullOrEmpty(_errLbl.Caption)) _errLbl.Caption = msg;
            return false;
        }

        bool ValidateSettings()
        {
            if (_errLbl != null) _errLbl.Caption = "";
            bool ok = true;
            string dir = tbServerDir.Text.Trim();
            if (dir.Length == 0 || !System.IO.Directory.Exists(dir)) ok = MarkInvalid(tbServerDir, "The server folder does not exist. Check the path.");
            string java = tbJava.Text.Trim();
            if (java.Length > 0 && !System.IO.File.Exists(java)) ok = MarkInvalid(tbJava, "The Java path does not exist. Check it.");
            string jar = tbJar.Text.Trim();
            if (jar.Length == 0) ok = MarkInvalid(tbJar, "The jar file name cannot be empty.");
            else if (dir.Length > 0 && System.IO.Directory.Exists(dir) && !System.IO.File.Exists(System.IO.Path.Combine(dir, jar))) ok = MarkInvalid(tbJar, "That jar file was not found in the server folder.");
            int p;
            if (!int.TryParse(tbPort.Text.Trim(), out p) || p < 1 || p > 65535) ok = MarkInvalid(tbPort, "The port must be a number between 1 and 65535.");
            string frpc = tbFrpc.Text.Trim();
            if (frpc.Length > 0 && !System.IO.File.Exists(frpc)) ok = MarkInvalid(tbFrpc, "The frpc.exe path does not exist. Check it.");
            if (cbTunnelMode.SelectedIndex == 0 && tbTunnelId.Text.Trim().Length == 0) ok = MarkInvalid(tbTunnelId, "In frpc mode, the tunnel ID cannot be empty.");
            string pcl = tbPcl2.Text.Trim();
            if (pcl.Length > 0 && !System.IO.File.Exists(pcl)) ok = MarkInvalid(tbPcl2, "The PCL2 path does not exist. Check it.");
            return ok;
        }

        bool Save()
        {
            if (!ValidateSettings()) return false;
            _cfg.ServerDir = tbServerDir.Text.Trim();
            _cfg.JavaPath = tbJava.Text.Trim();
            _cfg.JarName = tbJar.Text.Trim();
            _cfg.MemParams = tbMem.Text.Trim();
            int p; if (int.TryParse(tbPort.Text.Trim(), out p)) _cfg.Port = p;
            _cfg.FrpcPath = tbFrpc.Text.Trim();
            _cfg.FrpKey = tbKey.Text.Trim();
            _cfg.TunnelId = tbTunnelId.Text.Trim();
            _cfg.VoiceTunnelId = tbVoiceTunnelId.Text.Trim();
            _cfg.PublicAddr = tbAddr.Text.Trim();
            _cfg.Pcl2Path = tbPcl2.Text.Trim();
            _cfg.TunnelMode = (cbTunnelMode.SelectedIndex == 1) ? "launcher" : "frpc";
            _cfg.Save();
            return true;
        }
    }

    // ---------- 设计系统（v5 美化） ----------
    static class Pal
    {
        public static readonly Color Bg = Color.FromArgb(23, 26, 31);
        public static readonly Color TitleBar = Color.FromArgb(27, 31, 38);
        public static readonly Color Card = Color.FromArgb(32, 36, 44);
        public static readonly Color Inset = Color.FromArgb(20, 23, 28);
        public static readonly Color InsetBorder = Color.FromArgb(35, 39, 47);
        public static readonly Color Surface = Color.FromArgb(41, 46, 55);
        public static readonly Color Border = Color.FromArgb(44, 49, 58);
        public static readonly Color Text = Color.FromArgb(238, 241, 245);
        public static readonly Color Sub = Color.FromArgb(154, 165, 177);
        public static readonly Color Mute = Color.FromArgb(110, 118, 130); // v6.14: lifted (was 94,103,115, only 2.8:1)
        public static readonly Color Green = Color.FromArgb(59, 165, 93);
        public static readonly Color Amber = Color.FromArgb(250, 168, 26);
        public static readonly Color Danger = Color.FromArgb(237, 66, 69);
        public static readonly Color DangerText = Color.FromArgb(248, 113, 113); // v6.14: small-text danger (>=4.5:1 on card)
        public static readonly Color AddrGreen = Color.FromArgb(86, 216, 143);
        public static readonly Color InfoBlue = Color.FromArgb(72, 144, 245);
        public static readonly Color LogText = Color.FromArgb(203, 210, 219);
    }

    static class Gfx
    {
        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return p;
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        public static Color Mix(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
        public static Color Lighten(Color c, double t) { return Mix(c, Color.White, t); }
        public static Color Darken(Color c, double t) { return Mix(c, Color.Black, t); }
        // Two-layer focus ring (v6.4): light outer band + dark separator,
        // drawn inside the control bounds along its rounded path.
        public static void DrawFocusRing(Graphics g, Rectangle rect, int radius, float scale)
        {
            int ow = Math.Max(2, (int)Math.Round(2 * scale));
            int inset1 = Math.Max(1, ow / 2);
            var r1 = new Rectangle(rect.X + inset1, rect.Y + inset1, rect.Width - 2 * inset1, rect.Height - 2 * inset1);
            if (r1.Width <= 4 || r1.Height <= 4) return;
            using (var p = RoundRect(r1, Math.Max(1, radius - inset1)))
            using (var pen = new Pen(Pal.Text, ow)) g.DrawPath(pen, p);
            int iw = Math.Max(1, (int)Math.Round(1 * scale));
            int inset2 = inset1 + ow / 2 + iw / 2 + 1;
            var r2 = new Rectangle(rect.X + inset2, rect.Y + inset2, rect.Width - 2 * inset2, rect.Height - 2 * inset2);
            if (r2.Width <= 4 || r2.Height <= 4) return;
            using (var p = RoundRect(r2, Math.Max(1, radius - inset2)))
            using (var pen = new Pen(Pal.Bg, iw)) g.DrawPath(pen, p);
        }
    }

    // ---------- shared micro-animation clock (v6.4) ----------
    // One static 8 ms timer (with timeBeginPeriod(1)) drives every AnimFloat;
    // values are pure functions of wall-clock time, so a late tick jumps to
    // the correct value instead of accumulating error. MainForm mirrors the
    // system animation setting (SPI_GETCLIENTAREAANIMATION) into Enabled;
    // when off, every tween snaps straight to its end state.
    static class UiAnim
    {
        public static bool Enabled = true;
        static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
        static readonly System.Collections.Generic.List<AnimFloat> Active = new System.Collections.Generic.List<AnimFloat>();
        static System.Windows.Forms.Timer _timer;
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint uPeriod);
        [System.Runtime.InteropServices.DllImport("winmm.dll")] static extern uint timeEndPeriod(uint uPeriod);
        public static long Now { get { return Clock.ElapsedMilliseconds; } }
        public static double EaseOut(double p)
        {
            // cubic-bezier(0, 0, 0, 1) - the Fluent fast-out curve
            double t = p;
            for (int i = 0; i < 6; i++)
            {
                double u = 1 - t;
                double cx = 3 * u * t * t + t * t * t - p;
                double dx = 6 * u * t + 3 * t * t;
                if (Math.Abs(cx) < 1e-6 || Math.Abs(dx) < 1e-6) break;
                t -= cx / dx; if (t < 0) t = 0; if (t > 1) t = 1;
            }
            double v = 1 - t;
            return 3 * v * t * t + t * t * t;
        }
        internal static void Register(AnimFloat f)
        {
            if (!Active.Contains(f)) Active.Add(f);
            if (_timer == null)
            {
                _timer = new System.Windows.Forms.Timer();
                _timer.Interval = 8;
                _timer.Tick += (s, e) => TickAll();
            }
            if (!_timer.Enabled) { try { timeBeginPeriod(1); } catch { } _timer.Start(); }
        }
        internal static void Unregister(AnimFloat f)
        {
            Active.Remove(f);
            if (Active.Count == 0 && _timer != null && _timer.Enabled) { _timer.Stop(); try { timeEndPeriod(1); } catch { } }
        }
        static void TickAll()
        {
            var snapshot = Active.ToArray();
            foreach (var f in snapshot)
            {
                if (!Active.Contains(f)) continue;
                bool alive = false;
                try { alive = f.Step(); } catch { alive = false; }
                if (!alive) Active.Remove(f);
            }
            if (Active.Count == 0 && _timer != null && _timer.Enabled) { _timer.Stop(); try { timeEndPeriod(1); } catch { } }
        }
    }

    class AnimFloat
    {
        public double Value;
        public Action Changed;
        double _from, _to; long _t0; int _dur;
        bool _pulse;
        public AnimFloat(double v) { Value = v; _to = v; }
        public bool IsPulsing { get { return _pulse; } }
        public void Set(double target, int durMs)
        {
            _pulse = false;
            if (!UiAnim.Enabled || durMs <= 0) { Value = target; _to = target; UiAnim.Unregister(this); Fire(); return; }
            _from = Value; _to = target; _dur = durMs; _t0 = UiAnim.Now;
            UiAnim.Register(this);
        }
        // Press feedback: cover most of the distance on this very frame, then settle.
        public void JumpTo(double target, double fraction, int settleMs)
        {
            _pulse = false;
            Value = Value + (target - Value) * fraction;
            Fire();
            Set(target, settleMs);
        }
        public void StartPulse()
        {
            if (!UiAnim.Enabled) { Value = 1; return; }
            _pulse = true; _t0 = UiAnim.Now;
            UiAnim.Register(this);
        }
        public void StopPulse()
        {
            if (!_pulse && Value == 1) return;
            _pulse = false; Value = 1; UiAnim.Unregister(this); Fire();
        }
        internal bool Step()
        {
            if (_pulse)
            {
                double ph = (UiAnim.Now - _t0) % 1800 / 1800.0;
                Value = 0.775 + 0.225 * Math.Cos(ph * 2 * Math.PI);
                Fire();
                return true;
            }
            double p = _dur > 0 ? (UiAnim.Now - _t0) / (double)_dur : 1;
            if (p >= 1) { Value = _to; Fire(); return false; }
            Value = _from + (_to - _from) * UiAnim.EaseOut(p);
            Fire();
            return true;
        }
        void Fire() { if (Changed != null) { try { Changed(); } catch { } } }
    }

    static class DpiScale
    {
        public static float Measure(Control c)
        {
            try { using (var g = c.CreateGraphics()) return g.DpiX / 96f; }
            catch { return 1f; }
        }
        public static void ScaleChildren(Control parent, float s)
        {
            foreach (Control c in parent.Controls)
            {
                c.Bounds = new Rectangle((int)Math.Round(c.Left * s), (int)Math.Round(c.Top * s), (int)Math.Round(c.Width * s), (int)Math.Round(c.Height * s));
                ScaleChildren(c, s);
            }
        }
        public static void ScaleForm(Form f, int designW, int designH)
        {
            try
            {
                float s = Measure(f);
                if (s <= 0 || Math.Abs(s - 1f) < 0.01f) return;
                ScaleChildren(f, s);
                f.ClientSize = new Size((int)(designW * s), (int)(designH * s));
            }
            catch { }
        }
    }

    static class Fonts
    {
        public static Font Mono(float size, FontStyle style)
        {
            string[] names = { "Cascadia Mono", "Cascadia Code", "Consolas" };
            try
            {
                var installed = new System.Drawing.Text.InstalledFontCollection();
                foreach (string n in names)
                {
                    foreach (var f in installed.Families) { if (f.Name == n) return new Font(n, size, style); }
                }
            }
            catch { }
            return new Font("Consolas", size, style);
        }
    }

    class CardPanel : Panel
    {
        public int Radius = 12;
        public Color BackdropColor = Pal.Bg;
        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var back = new SolidBrush(BackdropColor)) e.Graphics.FillRectangle(back, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Radius))
            {
                using (var brush = new SolidBrush(Pal.Card)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(Pal.Border, 1f)) e.Graphics.DrawPath(pen, path);
            }
        }
        protected override void OnPaint(PaintEventArgs e) { }
    }

    class InsetPanel : Panel
    {
        public int Radius = 8;
        public bool FocusGlow = false;
        public Color BackdropColor = Pal.Bg;
        public InsetPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var back = new SolidBrush(BackdropColor)) e.Graphics.FillRectangle(back, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            bool lit = FocusGlow && ContainsFocus;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Radius))
            {
                using (var brush = new SolidBrush(Pal.Inset)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(lit ? Pal.Green : Pal.InsetBorder, 1f)) e.Graphics.DrawPath(pen, path);
            }
            if (lit)
            {
                var inner = new Rectangle(2, 2, Width - 5, Height - 5);
                using (var path2 = Gfx.RoundRect(inner, Radius - 2))
                using (var pen2 = new Pen(Color.FromArgb(45, Pal.Green), 1f)) e.Graphics.DrawPath(pen2, path2);
            }
        }
        protected override void OnPaint(PaintEventArgs e) { }
    }

    class RoundButton : Button
    {
        public bool Primary = false;
        public bool DangerHover = false;
        public int Radius = 8;
        public Color BackdropColor = Pal.Bg;
        bool _hover = false, _down = false;
        // _shade: 0 = normal, +1 = full hover lighten, -1 = full press darken
        readonly AnimFloat _shade = new AnimFloat(0);
        public RoundButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            _shade.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; _shade.Set(1, 120); Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; _shade.Set(0, 80); Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _down = true; _shade.JumpTo(-1, 0.7, 70); Invalidate(); } base.OnMouseDown(e); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; if (_hover) _shade.Set(1, 110); else _shade.Set(0, 80); Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { if (!Enabled) _shade.Set(0, 0); base.OnEnabledChanged(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var back = new SolidBrush(BackdropColor)) g.FillRectangle(back, ClientRectangle);
            Color baseCol = BackColor;
            if (!Enabled) baseCol = Gfx.Mix(baseCol, Pal.Mute, 0.55);
            Color fill = baseCol;
            double hAmt = 0;
            if (Enabled)
            {
                double sh = _shade.Value;
                hAmt = sh > 0 ? sh : 0;
                if (sh > 0.001) fill = Gfx.Lighten(baseCol, 0.09 * sh);
                else if (sh < -0.001) fill = Gfx.Darken(baseCol, 0.10 * -sh);
            }
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Radius))
            {
                Color top = Gfx.Lighten(fill, Primary ? 0.13 : 0.05);
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), top, fill, LinearGradientMode.Vertical))
                    g.FillPath(br, path);
                if (Primary)
                {
                    using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                        g.DrawLine(pen, Radius + 2, 1, Width - Radius - 2, 1);
                }
                else
                {
                    Color bc = DangerHover ? Gfx.Mix(Gfx.Lighten(fill, 0.16), Pal.Danger, hAmt) : Gfx.Lighten(fill, 0.16);
                    using (var pen = new Pen(bc, 1f)) g.DrawPath(pen, path);
                }
            }
            Color tc = ForeColor;
            if (!Enabled) tc = Gfx.Mix(tc, Pal.Mute, 0.4);
            if (DangerHover && Enabled && hAmt > 0) tc = Gfx.Mix(tc, Pal.Danger, hAmt);
            if (Enabled && Focused && ShowFocusCues) Gfx.DrawFocusRing(g, new Rectangle(0, 0, Width, Height), Radius, g.DpiX / 96f);
            TextRenderer.DrawText(g, Text, Font, rect, tc, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    class TitleBarPanel : Panel
    {
        public event EventHandler CloseClicked;
        public event EventHandler MinClicked;
        public event EventHandler DragRequested;
        // v6.6: reusable on dialogs — zones are width-relative (identical
        // geometry on the 560-wide main form), title/subtitle/min are settable.
        public string TitleText = "Minecraft Multiplayer Helper v6.20";
        public string SubText = "Dual tunnel · Game + Voice";
        public bool ShowMin = true;
        Image _icon;
        int _hover = 0, _down = 0;
        readonly AnimFloat _minAmt = new AnimFloat(0);
        readonly AnimFloat _closeAmt = new AnimFloat(0);
        Font _titleFont = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold);
        Font _verFont = new Font("Microsoft YaHei UI", 8f);
        public TitleBarPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Pal.TitleBar;
            TabStop = false;
            try { string ipng = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.png"); if (File.Exists(ipng)) _icon = Image.FromFile(ipng); } catch { }
            _minAmt.Changed = () => { if (!IsDisposed) Invalidate(); };
            _closeAmt.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        void RetargetZones()
        {
            double minT = _down == 1 ? 1.0 : (_hover == 1 ? 0.6 : 0.0);
            double closeT = (_down == 2 || _hover == 2) ? 1.0 : 0.0;
            _minAmt.Set(minT, minT > 0 ? 100 : 80);
            _closeAmt.Set(closeT, closeT > 0 ? 100 : 80);
        }
        float S { get { return Height / 40f; } }
        int ZoneAt(int x)
        {
            float s = S;
            if (x >= Width - 46 * s) return 2;
            if (ShowMin && x >= Width - 92 * s) return 1;
            return 0;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int z = ZoneAt(e.X);
            if (z != _hover) { _hover = z; RetargetZones(); Invalidate(); }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { if (_hover != 0 || _down != 0) { _hover = 0; _down = 0; Invalidate(); } RetargetZones(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int z = ZoneAt(e.X);
                if (z > 0) { _down = z; if (z == 2) _closeAmt.JumpTo(1, 0.7, 70); else _minAmt.JumpTo(1, 0.7, 70); RetargetZones(); Invalidate(); }
                else if (DragRequested != null) DragRequested(this, EventArgs.Empty);
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _down > 0)
            {
                int z = ZoneAt(e.X);
                int d = _down; _down = 0; RetargetZones(); Invalidate();
                if (z == d)
                {
                    if (d == 2 && CloseClicked != null) CloseClicked(this, EventArgs.Empty);
                    if (d == 1 && MinClicked != null) MinClicked(this, EventArgs.Empty);
                }
            }
            base.OnMouseUp(e);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = S;
            using (var back = new SolidBrush(Pal.TitleBar)) g.FillRectangle(back, ClientRectangle);
            double ca = _closeAmt.Value;
            if (ca > 0.003)
            {
                using (var b = new SolidBrush(Gfx.Mix(Pal.TitleBar, Color.FromArgb(232, 17, 35), ca))) g.FillRectangle(b, Width - 46 * s, 0, 46 * s, Height);
            }
            double ma = _minAmt.Value;
            if (ShowMin && ma > 0.003)
            {
                Color fill = ma <= 0.6 ? Gfx.Mix(Pal.TitleBar, Color.FromArgb(44, 49, 58), ma / 0.6)
                                       : Gfx.Mix(Color.FromArgb(44, 49, 58), Color.FromArgb(52, 58, 68), (ma - 0.6) / 0.4);
                using (var b = new SolidBrush(fill)) g.FillRectangle(b, Width - 92 * s, 0, 46 * s, Height);
            }
            if (_icon != null) g.DrawImage(_icon, new RectangleF(12 * s, 11 * s, 18 * s, 18 * s));
            TextRenderer.DrawText(g, TitleText, _titleFont, new Rectangle((int)(38 * s), 0, (int)(260 * s), Height), Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if (!string.IsNullOrEmpty(SubText))
                TextRenderer.DrawText(g, SubText, _verFont, new Rectangle((int)(270 * s), 0, Math.Max(0, (int)(Width - 100 * s - 270 * s)), Height), Pal.Mute, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            int half = Math.Max(4, Height / 10);
            if (ShowMin) DrawGlyph(g, (int)(Width - 69 * s), Height / 2, false, _minAmt.Value, half);
            DrawGlyph(g, (int)(Width - 23 * s), Height / 2, true, _closeAmt.Value, half);
        }
        void DrawGlyph(Graphics g, int cx, int cy, bool isClose, double amt, int half)
        {
            Color gc = Gfx.Mix(Pal.Sub, Color.White, amt);
            using (var pen = new Pen(gc, 1.6f))
            {
                if (isClose)
                {
                    g.DrawLine(pen, cx - half, cy - half, cx + half, cy + half);
                    g.DrawLine(pen, cx + half, cy - half, cx - half, cy + half);
                }
                else g.DrawLine(pen, cx - half, cy, cx + half, cy);
            }
        }
    }

    class StatusCard : CardPanel
    {
        public string ServerText = "Server: Not running";
        public Color ServerColor = Color.FromArgb(94, 103, 115);
        public string TunnelText = "SakuraFrp tunnel: Not running";
        public Color TunnelColor = Color.FromArgb(94, 103, 115);
        public string AddrText = "Public address: Not generated";
        public Color AddrColor = Color.FromArgb(94, 103, 115);
        public string StateText = "";
        Image _art;
        Font _stateFont = new Font("Microsoft YaHei UI", 9f);
        readonly AnimFloat _pulseSrv = new AnimFloat(1);
        readonly AnimFloat _pulseTun = new AnimFloat(1);
        public StatusCard()
        {
            Font = new Font("Microsoft YaHei UI", 9f);
            try { string png = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.png"); if (File.Exists(png)) _art = Image.FromFile(png); } catch { }
            _pulseSrv.Changed = () => { if (!IsDisposed) Invalidate(); };
            _pulseTun.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        // row 0 = server, 1 = tunnel: pulse while that row is in a
        // transitional ("starting") state, static dot otherwise (v6.4)
        public void SetPulse(int row, bool on)
        {
            var f = row == 0 ? _pulseSrv : _pulseTun;
            if (on) { if (!f.IsPulsing) f.StartPulse(); }
            else f.StopPulse();
        }
        float Cs { get { return Height / 148f; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Cs;
            DrawRow(g, s, 14, ServerText, ServerColor, _pulseSrv.Value);
            DrawRow(g, s, 46, TunnelText, TunnelColor, _pulseTun.Value);
            DrawRow(g, s, 78, AddrText, AddrColor, 1.0);
            if (!string.IsNullOrEmpty(StateText))
                TextRenderer.DrawText(g, StateText, _stateFont, new Rectangle((int)(18 * s), (int)(114 * s), (int)(484 * s), (int)(24 * s)), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            if (_art != null)
            {
                float boxX = 428 * s, boxY = 40 * s, boxW = 68 * s, boxH = 68 * s;
                float sc = Math.Min(boxW / _art.Width, boxH / _art.Height);
                float w = _art.Width * sc, h = _art.Height * sc;
                g.DrawImage(_art, boxX + (boxW - w) / 2f, boxY + (boxH - h) / 2f, w, h);
            }
        }
        void DrawRow(Graphics g, float s, int yLogical, string text, Color c, double pulse)
        {
            if (c == Color.Red) c = Pal.Danger;
            float rowX = 10 * s, rowY = yLogical * s, rowH = 28 * s;
            float cy = rowY + rowH / 2f;
            Color dc = pulse >= 0.999 ? c : Gfx.Mix(c, Pal.Card, 1 - pulse);
            using (var halo = new SolidBrush(Color.FromArgb((int)(58 * pulse), c))) g.FillEllipse(halo, rowX + 4, cy - 9, 18, 18);
            using (var dot = new SolidBrush(dc)) g.FillEllipse(dot, rowX + 9, cy - 4, 8, 8);
            var rect = new Rectangle((int)(rowX + 30), (int)rowY, (int)(330 * s - 30), (int)rowH);
            TextRenderer.DrawText(g, text ?? "", Font, rect, Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    class SegControl : Control
    {
        public string[] Items = new string[0];
        public int SelectedIndex = 0;
        public event EventHandler Changed;
        double _fade = 1.0;
        Font _boldFont;
        int _hoverIdx = -1;
        readonly AnimFloat _hoverAmt = new AnimFloat(0);
        readonly AnimFloat _selPos = new AnimFloat(0);
        public SegControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            _hoverAmt.Changed = () => { if (!IsDisposed) Invalidate(); };
            _selPos.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        // 1 = fully shown; 0 = fully blended into the card behind (v6.3 collapse fade)
        public double Fade
        {
            get { return _fade; }
            set { if (Math.Abs(value - _fade) > 0.004) { _fade = value; Invalidate(); } }
        }
        Color BlendC(Color c) { return _fade >= 0.999 ? c : Gfx.Mix(c, Pal.Card, 1.0 - _fade); }
        Font BoldFont()
        {
            if (_boldFont == null || _boldFont.Size != Font.Size || _boldFont.FontFamily.Name != Font.FontFamily.Name)
            {
                if (_boldFont != null) _boldFont.Dispose();
                _boldFont = new Font(Font, FontStyle.Bold);
            }
            return _boldFont;
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            using (var back = new SolidBrush(Pal.Card)) g.FillRectangle(back, ClientRectangle);
            if (_fade <= 0.004 || Items == null || Items.Length == 0) return;
            // Device-px geometry snapped to the pixel grid (the corner-dot fix):
            // logical tokens at this control's 32px reference height are
            // track radius 8, inset 2, segment radius 6 (concentric rule),
            // i.e. 12 / 3 / 9 device px at 150% scaling - all integers.
            double s = Height / 32.0; if (s <= 0) s = 1;
            int inset = Math.Max(1, (int)Math.Round(2 * s));
            int trackR = Math.Max(2, (int)Math.Round(8 * s));
            int segR = Math.Max(1, trackR - inset);
            var track = new Rectangle(0, 0, Width, Height);
            using (var p = Gfx.RoundRect(track, trackR))
            using (var b = new SolidBrush(BlendC(Pal.Inset))) g.FillPath(b, p);
            int n = Items.Length;
            float segW = (float)Width / n;
            double pos = _selPos.Value;
            if (pos < 0) pos = 0; if (pos > n - 1) pos = n - 1;
            // v6.5 filled bar: the segments partition the track exactly and
            // share one device-snapped boundary, so no dark gap can open
            // between them; the track path used as clip rounds the outer
            // corners. The selection is a filled thumb that SLIDES with
            // _selPos (190ms ease-out) instead of jumping between segments.
            var clipState = g.Save();
            using (var clip = Gfx.RoundRect(track, trackR)) g.SetClip(clip);
            if (_hoverIdx >= 0 && _hoverIdx < n && _hoverAmt.Value > 0.003 && Math.Abs(pos - _hoverIdx) > 0.6)
            {
                int hx0 = (int)Math.Round(_hoverIdx * segW), hx1 = (int)Math.Round((_hoverIdx + 1) * segW);
                using (var b = new SolidBrush(BlendC(Gfx.Mix(Pal.Inset, Color.White, 0.08 * _hoverAmt.Value)))) g.FillRectangle(b, hx0, 0, hx1 - hx0, Height);
            }
            int tx0 = (int)Math.Round(pos * segW), tx1 = (int)Math.Round((pos + 1) * segW);
            using (var b = new SolidBrush(BlendC(Color.FromArgb(52, 58, 68)))) g.FillRectangle(b, tx0, 0, tx1 - tx0, Height);
            g.Restore(clipState);
            using (var pen = new Pen(BlendC(Pal.Border), 1f))
            using (var p = Gfx.RoundRect(track, trackR)) g.DrawPath(pen, p);
            for (int i = 0; i < n; i++)
            {
                int bx0 = (int)Math.Round(i * segW);
                var sr = new Rectangle(bx0, 0, (int)Math.Round((i + 1) * segW) - bx0, Height);
                double emph = 1.0 - Math.Abs(pos - i); if (emph < 0) emph = 0;
                Color c = Gfx.Mix(Pal.Sub, Pal.Text, emph);
                if (i == _hoverIdx && emph < 0.5) c = Gfx.Mix(c, Pal.Text, _hoverAmt.Value * (1.0 - emph));
                TextRenderer.DrawText(g, Items[i], emph > 0.5 ? BoldFont() : Font, sr, BlendC(c), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
            if (Focused && ShowFocusCues) Gfx.DrawFocusRing(g, new Rectangle(0, 0, Width, Height), trackR, g.DpiX / 96f);
        }
        void Select(int idx)
        {
            if (idx == SelectedIndex) return;
            int fromIdx = SelectedIndex;
            SelectedIndex = idx;
            // v6.15: swap panes BEFORE starting the slide clock - the swap
            // is synchronous UI work and used to eat the head of the ease,
            // which read as a stutter on short (adjacent) moves.
            if (Changed != null) Changed(this, EventArgs.Empty);
            int steps = Math.Abs(idx - fromIdx); if (steps < 1) steps = 1;
            int dur = 140 + 40 * steps; if (dur > 260) dur = 260;
            _selPos.Set(idx, dur);
            _hoverAmt.Set(0, 80);
            Invalidate();
        }
        // v6.13: restore the remembered tab at startup via the click path
        public void Restore613(int idx) { if (idx > 0 && idx < Items.Length) Select(idx); }
        // v6.16: programmatic page switch through the normal click path
        public void SelectPublic(int idx) { if (idx >= 0 && idx < Items.Length) Select(idx); }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Items == null || Items.Length == 0) return;
            int idx = (int)(e.X / ((float)Width / Items.Length));
            if (idx < 0) idx = 0;
            if (idx >= Items.Length) idx = Items.Length - 1;
            if (idx != _hoverIdx)
            {
                _hoverIdx = idx;
                _hoverAmt.Set(idx != SelectedIndex ? 1 : 0, 120);
                Invalidate();
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIdx = -1;
            _hoverAmt.Set(0, 80);
            Invalidate();
            base.OnMouseLeave(e);
        }
        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right) return true;
            return base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Items == null || Items.Length == 0) return;
            int idx = SelectedIndex;
            if (e.KeyCode == Keys.Left) idx = Math.Max(0, SelectedIndex - 1);
            else if (e.KeyCode == Keys.Right) idx = Math.Min(Items.Length - 1, SelectedIndex + 1);
            else return;
            if (idx != SelectedIndex)
            {
                Select(idx);
                e.Handled = true;
            }
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Items == null || Items.Length == 0) return;
            int idx = (int)(e.X / ((float)Width / Items.Length));
            if (idx < 0) idx = 0;
            if (idx >= Items.Length) idx = Items.Length - 1;
            Select(idx);
        }
    }

    // Owner-drawn label (v6.5.2): on the user's machine, double-clicking a
    // plain WinForms Label copies its Text to the clipboard (verified with a
    // clipboard watcher: owner = this app, content = the label's text, for
    // both main-form labels). This control paints its caption itself and
    // keeps the string in a private field instead of Control.Text, so there
    // is no native label text for anything to copy.
    class TextLabel : Control
    {
        string _caption = "";
        public bool TopAlign = false;
        public bool WordWrap = false; // v6.6: multi-line wrapped text (dialog messages)
        public Color BackdropColor = Pal.Card;
        public float TextShiftX = 0, TextShiftY = 0; // logical px, tuned so the ink lands where the old Label put it
        public string Caption
        {
            get { return _caption; }
            set { string v = value ?? ""; if (v != _caption) { _caption = v; Invalidate(); } }
        }
        public TextLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
        }
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set { if (base.ForeColor != value) { base.ForeColor = value; Invalidate(); } }
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            Color bg = BackColor;
            if (bg.A == 0) bg = BackdropColor;
            using (var b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, ClientRectangle);
            if (_caption.Length > 0)
            {
                TextFormatFlags flags;
                if (WordWrap) flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
                else flags = TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | (TopAlign ? TextFormatFlags.Top : TextFormatFlags.VerticalCenter);
                float sc = e.Graphics.DpiX / 96f;
                var rc = ClientRectangle;
                rc.Offset((int)Math.Round(TextShiftX * sc), (int)Math.Round(TextShiftY * sc));
                TextRenderer.DrawText(e.Graphics, _caption, Font, rc, ForeColor, flags);
            }
        }
    }

    // ---------- 日志文本框（v6.6） ----------
    // Multiline EDIT with hidden native scrollbars: draws its own empty-state
    // placeholder after the base paint, scrolls itself on mouse wheel (the
    // EDIT ignores the wheel without bar styles), and reports scroll activity
    // so the slim overlay bars + jump-to-bottom chip can stay in sync.
    // ---------- v6.12 visual controls ----------
    // A single-line input with a painted hint (themed placeholder), drawn
    // the same way LogTextBox draws its empty-state text.
    class HintTextBox : TextBox
    {
        public string Hint = "";
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x000F && TextLength == 0 && !Focused && Hint.Length > 0)
            {
                try { using (var g = Graphics.FromHwnd(Handle)) TextRenderer.DrawText(g, Hint, Font, new Point(4, 3), Pal.Mute); } catch { }
            }
        }
    }

    // Owner-drawn checkbox in the app palette: the native checkbox paints
    // a bright white box that fights the dark theme everywhere it appears.
    class DarkCheckBox : Control
    {
        bool _checked;
        bool _hover;
        readonly AnimFloat _chkAmt614 = new AnimFloat(0); // v6.14: state-color lerp
        readonly AnimFloat _hovAmt614 = new AnimFloat(0);
        public event EventHandler CheckedChanged;
        public bool Checked
        {
            get { return _checked; }
            set { if (_checked != value) { _checked = value; _chkAmt614.Set(value ? 1 : 0, 120); Invalidate(); var h = CheckedChanged; if (h != null) h(this, EventArgs.Empty); } }
        }
        public DarkCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            _chkAmt614.Changed = () => { if (!IsDisposed) Invalidate(); };
            _hovAmt614.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        float S { get { return Height / 24f; } }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; _hovAmt614.Set(1, 120); Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _hovAmt614.Set(0, 120); Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location)) Checked = !Checked;
            base.OnMouseUp(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; }
            base.OnKeyDown(e);
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float sc = S;
            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, ClientRectangle);
            float bs = 14 * sc;
            float by = (Height - bs) / 2f;
            var box = new RectangleF(1, by, bs, bs);
            Color border = Gfx.Mix(Gfx.Mix(Pal.Border, Pal.Sub, _hovAmt614.Value), Pal.InfoBlue, _chkAmt614.Value); // v6.14
            Color fill = Gfx.Mix(Color.FromArgb(17, 20, 24), Pal.InfoBlue, _chkAmt614.Value); // v6.14
            if (!Enabled) { border = Pal.Mute; fill = Gfx.Mix(fill, Pal.Mute, 0.4); }
            using (var path = Gfx.RoundRect(Rectangle.Round(box), Math.Max(2, (int)(3 * sc))))
            {
                using (var fb = new SolidBrush(fill)) g.FillPath(fb, path);
                using (var pen = new Pen(border, Math.Max(1f, sc))) g.DrawPath(pen, path);
            }
            if (_chkAmt614.Value > 0.02)
            {
                var pts = new PointF[]
                {
                    new PointF(box.X + 3.2f * sc, box.Y + 7.2f * sc),
                    new PointF(box.X + 6.0f * sc, box.Y + 10.2f * sc),
                    new PointF(box.X + 10.8f * sc, box.Y + 3.6f * sc)
                };
                using (var pen = new Pen(Color.FromArgb((int)(255 * _chkAmt614.Value), Color.White), Math.Max(1.2f, 1.7f * sc)))
                {
                    pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawLines(pen, pts);
                }
            }
            if (Focused && ShowFocusCues) Gfx.DrawFocusRing(g, new Rectangle(0, 0, Width, Height), 4, sc);
            Color tc = Enabled ? ForeColor : Pal.Mute;
            int tx = (int)(bs + 8 * sc);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(tx, 0, Width - tx, Height), tc, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }
    }

    // Themed owner-draw for list boxes: the native selection is a solid
    // system-blue bar; ours is a rounded, low-tint accent surface.
    static class ThemedList
    {
        public static void Attach(ListBox lb)
        {
            lb.DrawMode = DrawMode.OwnerDrawFixed;
            lb.HandleCreated += (snd, ev) => { try { lb.ItemHeight = Math.Max(16, lb.Font.Height + 8); } catch { } };
            lb.DrawItem += (snd, e) =>
            {
                if (e.Index < 0) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(lb.BackColor)) g.FillRectangle(b, e.Bounds);
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                var rc = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 1, e.Bounds.Width - 4, e.Bounds.Height - 2);
                if (sel)
                {
                    using (var path = Gfx.RoundRect(rc, 5))
                    using (var b2 = new SolidBrush(Gfx.Mix(lb.BackColor, Pal.InfoBlue, 0.34))) g.FillPath(b2, path);
                }
                string txt = lb.Items[e.Index] != null ? lb.Items[e.Index].ToString() : "";
                TextRenderer.DrawText(g, txt, lb.Font, new Rectangle(rc.X + 8, rc.Y, rc.Width - 12, rc.Height), Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            };
        }
    }

    // Themed owner-draw for combo boxes: a focused native combo paints
    // its whole field system blue; ours stays on-palette in every state.
    static class ThemedCombo
    {
        public static void Attach(ComboBox cb)
        {
            cb.DrawMode = DrawMode.OwnerDrawFixed;
            cb.DrawItem += (snd, e) =>
            {
                var g = e.Graphics;
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = sel ? Gfx.Mix(cb.BackColor, Pal.InfoBlue, 0.38) : cb.BackColor;
                using (var b = new SolidBrush(bg)) g.FillRectangle(b, e.Bounds);
                if (e.Index >= 0 && e.Index < cb.Items.Count)
                {
                    string txt = cb.Items[e.Index] != null ? cb.Items[e.Index].ToString() : "";
                    TextRenderer.DrawText(g, txt, cb.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), cb.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
                }
            };
        }
    }

    class LogTextBox : TextBox
    {
        public string Placeholder = "";
        public event EventHandler Scrolled;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr wp, IntPtr lp);

        public int LineCount() { return Send(0xBA, 0, 0); }          // EM_GETLINECOUNT
        public int FirstVisible() { return Send(0xCE, 0, 0); }       // EM_GETFIRSTVISIBLELINE
        public int VisibleLines() { return Math.Max(1, ClientSize.Height / Math.Max(1, Font.Height)); }
        public void ScrollLines(int dy) { if (dy != 0) Send(0xB6, 0, dy); }   // EM_LINESCROLL vertical
        public void ScrollChars(int dx) { if (dx != 0) Send(0xB6, dx, 0); }   // EM_LINESCROLL horizontal (chars)
        // Horizontal scroll offset in pixels. GetScrollPos reports nothing
        // without a native bar style, but EM_POSFROMCHAR on the first visible
        // line's first character gives its (negative) x directly (QA-verified:
        // EM_LINESCROLL does scroll horizontally; only the API readout is dead).
        public int HOffsetPx()
        {
            if (!IsHandleCreated) return 0;
            int idx = Send(0xBB, FirstVisible(), 0); // EM_LINEINDEX
            if (idx < 0) idx = 0;
            int packed = Send(0xD6, idx, 0);          // EM_POSFROMCHAR
            int x = (short)(packed & 0xFFFF);
            return x < 0 ? -x : 0;
        }
        int Send(int msg, int wp, int lp)
        {
            if (!IsHandleCreated) return 0;
            try { return SendMessage(Handle, msg, (IntPtr)wp, (IntPtr)lp).ToInt32(); } catch { return 0; }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x020A) // WM_MOUSEWHEEL: no bar styles -> the EDIT won't scroll; do it manually
            {
                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                int notches = delta / 120; if (notches == 0) notches = delta > 0 ? 1 : -1;
                int lines = Math.Max(1, SystemInformation.MouseWheelScrollLines);
                ScrollLines(-notches * lines);
                if (Scrolled != null) Scrolled(this, EventArgs.Empty);
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == 0x020E) // WM_MOUSEHWHEEL (tilt wheel)
            {
                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                ScrollChars(delta > 0 ? -3 : 3);
                if (Scrolled != null) Scrolled(this, EventArgs.Empty);
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == 0x000F) // WM_PAINT: empty-state placeholder over the base paint
            {
                if (TextLength == 0 && Placeholder.Length > 0)
                {
                    try
                    {
                        using (var g = Graphics.FromHwnd(Handle))
                            TextRenderer.DrawText(g, Placeholder, Font, new Point(4, 3), Pal.Mute);
                    }
                    catch { }
                }
            }
            else if (m.Msg == 0x0115 || m.Msg == 0x0114) // WM_VSCROLL / WM_HSCROLL
            {
                if (Scrolled != null) Scrolled(this, EventArgs.Empty);
            }
        }
    }

    // ---------- 自绘细滚动条（v6.6） ----------
    // Slim dark overlay bar driving a LogTextBox whose native bars are hidden.
    // Vertical mode works in lines; horizontal mode works in characters and
    // reads the pixel offset via GetScrollPos (the EDIT still maintains it
    // with its bars hidden), converting through a measured character width.
    class SlimScrollBar : Control
    {
        public bool Horizontal = false;
        public LogTextBox Target;
        public Func<int> MaxChars; // horizontal only: longest line so far, in chars
        bool _hover = false, _drag = false;
        readonly AnimFloat _hovAmt614 = new AnimFloat(0); // v6.14: state-color lerp
        int _dragStartPos = 0, _dragStartFirst = 0;
        float _charW = 0;

        public SlimScrollBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            TabStop = false;
            _hovAmt614.Changed = () => { if (!IsDisposed) Invalidate(); };
        }
        public bool Needed
        {
            get
            {
                if (Target == null || !Target.IsHandleCreated) return false;
                if (!Horizontal) return Target.LineCount() > Target.VisibleLines();
                return (MaxChars != null ? MaxChars() : 0) > VisibleChars();
            }
        }
        public void Sync()
        {
            bool need = false;
            try { need = Needed; } catch { }
            if (Visible != need) Visible = need;
            if (need) Invalidate();
        }
        float CharWidth()
        {
            if (_charW > 0) return _charW;
            try { _charW = TextRenderer.MeasureText("MMMMMMMMMM", Target.Font).Width / 10f; } catch { }
            if (_charW <= 0) _charW = 7f;
            return _charW;
        }
        int VisibleChars()
        {
            if (Target == null) return 1;
            return Math.Max(1, (int)(Target.ClientSize.Width / CharWidth()));
        }
        int HFirst()
        {
            try { return (int)(Target.HOffsetPx() / CharWidth()); } catch { return 0; }
        }
        Rectangle ThumbRect()
        {
            var r = ClientRectangle;
            if (Target == null || !Target.IsHandleCreated) return new Rectangle(1, 1, Math.Max(1, r.Width - 2), Math.Max(1, r.Height - 2));
            if (!Horizontal)
            {
                int total = Target.LineCount(), vis = Target.VisibleLines();
                if (total <= vis) return new Rectangle(1, 0, Math.Max(1, r.Width - 2), r.Height);
                int th = Math.Max(28, (int)((double)r.Height * vis / total));
                int range = Math.Max(1, total - vis);
                int first = Math.Min(Target.FirstVisible(), range);
                int ty = r.Height > th ? (int)((double)(r.Height - th) * first / range) : 0;
                return new Rectangle(1, ty, Math.Max(1, r.Width - 2), th);
            }
            else
            {
                int vis = VisibleChars();
                int total = Math.Max(vis + 1, MaxChars != null ? MaxChars() : vis + 1);
                int tw = Math.Max(28, (int)((double)r.Width * vis / total));
                int range = Math.Max(1, total - vis);
                int first = Math.Min(HFirst(), range);
                int tx = r.Width > tw ? (int)((double)(r.Width - tw) * first / range) : 0;
                return new Rectangle(tx, 1, tw, Math.Max(1, r.Height - 2));
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var b = new SolidBrush(Pal.Inset)) e.Graphics.FillRectangle(b, ClientRectangle);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var t = ThumbRect();
            Color c = _drag ? Color.FromArgb(96, 106, 120) : Gfx.Mix(Color.FromArgb(58, 65, 76), Color.FromArgb(78, 86, 99), _hovAmt614.Value); // v6.14
            int rad = Math.Max(1, Math.Min(4, Math.Min(t.Width, t.Height) / 2));
            using (var path = Gfx.RoundRect(t, rad))
            using (var b = new SolidBrush(c)) e.Graphics.FillPath(b, path);
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; _hovAmt614.Set(1, 150); Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _hovAmt614.Set(0, 150); Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (Target != null)
            {
                if (Horizontal) Target.ScrollChars(e.Delta > 0 ? -3 : 3);
                else Target.ScrollLines(e.Delta > 0 ? -3 : 3);
                Sync();
            }
            base.OnMouseWheel(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && Target != null)
            {
                var t = ThumbRect();
                int pos = Horizontal ? e.X : e.Y;
                int tPos = Horizontal ? t.X : t.Y;
                int tLen = Horizontal ? t.Width : t.Height;
                if (pos >= tPos && pos < tPos + tLen)
                {
                    _drag = true; _dragStartPos = pos;
                    _dragStartFirst = Horizontal ? HFirst() : Target.FirstVisible();
                    Capture = true; Invalidate();
                }
                else
                {
                    int page = Horizontal ? VisibleChars() : Target.VisibleLines();
                    int dir = pos < tPos ? -1 : 1;
                    if (Horizontal) Target.ScrollChars(dir * page); else Target.ScrollLines(dir * page);
                    Sync();
                }
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_drag && Target != null)
            {
                var t = ThumbRect();
                if (!Horizontal)
                {
                    int total = Target.LineCount(), vis = Target.VisibleLines();
                    int track = Math.Max(1, ClientSize.Height - t.Height);
                    int want = _dragStartFirst + (int)Math.Round((double)(e.Y - _dragStartPos) / track * Math.Max(1, total - vis));
                    Target.ScrollLines(want - Target.FirstVisible());
                }
                else
                {
                    int vis = VisibleChars();
                    int total = Math.Max(vis + 1, MaxChars != null ? MaxChars() : vis + 1);
                    int track = Math.Max(1, ClientSize.Width - t.Width);
                    int want = _dragStartFirst + (int)Math.Round((double)(e.X - _dragStartPos) / track * Math.Max(1, total - vis));
                    Target.ScrollChars(want - HFirst());
                }
                Sync();
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_drag) { _drag = false; Capture = false; Invalidate(); }
            base.OnMouseUp(e);
        }
    }

    // ---------- 监控页（v6.7） ----------
    // Third tab of the log panel: server process state/memory/CPU/player
    // connections, tunnel (frpc) memory/CPU, whole-machine up/down throughput
    // and system memory. MainForm samples once a second into the text fields;
    // this panel only paints, in the same inset style as the log boxes.
    // Coordinates scale by Height/290 like StatusCard's Height/148 rule.
    class StatsPanel : Panel
    {
        public string SrvState = "Not running";
        public Color SrvStateColor = Pal.Mute;
        public string SrvMem = "—", SrvMemCap = "", SrvCpu = "—", SrvConn = "—";
        public string SrvPlayers = "—";
        public string SrvVerPing = "—";
        public event EventHandler ManageClicked;
        public double SrvMemFrac = -1;
        public double[] SrvMemHist = new double[0]; // v6.9: last ~60 samples of SrvMemFrac
        public string TunMem = "—", TunCpu = "—";
        public string NetDown = "—", NetUp = "—";
        public string SysMem = "—";
        Font _hdrFont, _valFont;
        public StatsPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = new Font("Microsoft YaHei UI", 9f);
            TabStop = false;
            // v6.11: player management entry, sits on the players row
            var mb = new RoundButton();
            mb.Text = "Players";
            mb.SetBounds(392, 120, 80, 24);
            mb.BackColor = Pal.Surface;
            mb.ForeColor = Pal.Text;
            mb.Font = new Font("Microsoft YaHei UI", 9f);
            mb.Click += (snd, ev) => { var h = ManageClicked; if (h != null) h(this, EventArgs.Empty); };
            Controls.Add(mb);
        }
        float S { get { return Height / 290f; } }
        int X(double v) { return (int)Math.Round(v * S); }
        Font HdrFont()
        {
            if (_hdrFont == null || _hdrFont.Size != Font.Size - 0.5f || _hdrFont.FontFamily.Name != Font.FontFamily.Name)
            {
                if (_hdrFont != null) _hdrFont.Dispose();
                _hdrFont = new Font(Font.FontFamily, Font.Size - 0.5f, FontStyle.Bold);
            }
            return _hdrFont;
        }
        Font ValFont()
        {
            if (_valFont == null || _valFont.Size != Font.Size)
            {
                if (_valFont != null) _valFont.Dispose();
                _valFont = Fonts.Mono(Font.Size, FontStyle.Regular);
            }
            return _valFont;
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var back = new SolidBrush(Pal.Card)) e.Graphics.FillRectangle(back, ClientRectangle);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Gfx.RoundRect(rect, Math.Max(2, X(8))))
            {
                using (var brush = new SolidBrush(Pal.Inset)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(Pal.InsetBorder, 1f)) e.Graphics.DrawPath(pen, path);
            }
        }
        void Header(Graphics g, int y, string text)
        {
            TextRenderer.DrawText(g, text, HdrFont(), new Rectangle(X(16), X(y), X(220), X(20)), Pal.Mute, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
        void Row(Graphics g, int y, string label, string value, Color vc)
        {
            TextRenderer.DrawText(g, label, Font, new Rectangle(X(16), X(y), X(92), X(21)), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, value, ValFont(), new Rectangle(X(112), X(y), Width - X(128), X(21)), vc, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
        void Bar(Graphics g, int y, double frac, string cap)
        {
            var track = new Rectangle(X(112), X(y), X(180), Math.Max(2, X(6)));
            using (var p = Gfx.RoundRect(track, Math.Max(1, X(3))))
            using (var b = new SolidBrush(Color.FromArgb(41, 46, 55))) g.FillPath(b, p);
            if (frac > 0)
            {
                int w = Math.Max(X(6), (int)(track.Width * Math.Min(1.0, frac)));
                var fill = new Rectangle(track.X, track.Y, Math.Min(w, track.Width), track.Height);
                using (var p = Gfx.RoundRect(fill, Math.Max(1, X(3))))
                using (var b = new SolidBrush(Pal.Green)) g.FillPath(b, p);
            }
            if (!string.IsNullOrEmpty(cap))
                TextRenderer.DrawText(g, cap, Font, new Rectangle(X(304), X(y) - X(6), Width - X(320), X(18)), Pal.Mute, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
        // v6.9: sparkline of the server memory fraction (same 0..cap scale as
        // the bar above it), drawn in the strip under the bar.
        void Spark(Graphics g, int y)
        {
            var rc = new Rectangle(X(112), X(y), X(180), X(10));
            using (var b = new SolidBrush(Color.FromArgb(41, 46, 55)))
                g.FillRectangle(b, new Rectangle(rc.X, rc.Bottom - 1, rc.Width, 1));
            var h = SrvMemHist;
            if (h == null || h.Length < 2) return;
            var pts = new PointF[h.Length];
            for (int i = 0; i < h.Length; i++)
            {
                // right-anchored: the newest sample sits at the right edge
                // and the line grows leftwards as history fills the minute
                float fx = rc.Right - (float)(h.Length - 1 - i) / 59f * rc.Width;
                double f = h[i]; if (f < 0) f = 0; if (f > 1) f = 1;
                float fy = rc.Bottom - 1 - (float)(f * (rc.Height - 2));
                pts[i] = new PointF(fx, fy);
            }
            using (var p = new Pen(Color.FromArgb(72, 144, 245), Math.Max(1f, S * 0.9f)))
                g.DrawLines(p, pts);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Header(g, 4, "Server process");
            Row(g, 22, "Status", SrvState, SrvStateColor);
            Row(g, 40, "Memory usage", SrvMem, Pal.Text);
            Bar(g, 60, SrvMemFrac, SrvMemCap);
            Spark(g, 71);
            Row(g, 86, "CPU", SrvCpu, Pal.Text);
            Row(g, 104, "Player connections", SrvConn, Pal.Text);
            // online player names (from the server log's join/leave lines)
            TextRenderer.DrawText(g, "Online players", Font, new Rectangle(X(16), X(122), X(92), X(21)), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, SrvPlayers, Font, new Rectangle(X(112), X(122), Width - X(212), X(21)), Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            Row(g, 140, "Version · Ping", SrvVerPing, Pal.Text);
            Header(g, 162, "Tunnel process (frpc)");
            Row(g, 180, "Memory usage", TunMem, Pal.Text);
            Row(g, 198, "CPU", TunCpu, Pal.Text);
            Header(g, 220, "Local");
            // net row: down/up side by side
            TextRenderer.DrawText(g, "Down", Font, new Rectangle(X(16), X(238), X(40), X(21)), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, NetDown, ValFont(), new Rectangle(X(58), X(238), X(150), X(21)), Pal.AddrGreen, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, "Up", Font, new Rectangle(X(256), X(238), X(40), X(21)), Pal.Sub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            TextRenderer.DrawText(g, NetUp, ValFont(), new Rectangle(X(298), X(238), X(150), X(21)), Pal.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            Row(g, 256, "System memory", SysMem, Pal.Text);
        }
    }

    // ---------- 深色对话框基类（v6.6） ----------
    // Same invisible-frame trick as MainForm: a real caption/thick-frame
    // window with a zeroed non-client area, so the TitleBarPanel is the only
    // chrome and DWM still supplies rounded corners + shadow.
    class DarkDialog : Form
    {
        protected TitleBarPanel Bar;
        Size _wantClient;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr wp, IntPtr lp);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);

        protected void InitChrome(string title, int contentW, int contentH)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Pal.Bg;
            ForeColor = Pal.Text;
            Font = new Font("Microsoft YaHei UI", 9f);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false; MinimizeBox = false;
            ClientSize = new Size(contentW, contentH + 40);
            Bar = new TitleBarPanel();
            Bar.TitleText = title;
            Bar.SubText = "";
            Bar.ShowMin = false;
            Bar.SetBounds(0, 0, contentW, 40);
            Bar.DragRequested += (s, e) => { try { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero); } catch { } };
            Bar.CloseClicked += (s, e) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(Bar);
            try { string ico = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico"); if (File.Exists(ico)) Icon = new Icon(ico); } catch { }
        }
        // Shift every non-titlebar control below the bar, scale, and keep the
        // bar frontmost. Call once at the end of the derived constructor,
        // after ClientSize has its final value.
        protected void FinishChrome()
        {
            foreach (Control c in Controls) if (c != Bar) c.Top += 40;
            Bar.BringToFront();
            DpiScale.ScaleForm(this, ClientSize.Width, ClientSize.Height);
            _wantClient = ClientSize;
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // A ClientSize assigned before the handle exists is converted
            // through the (invisible) frame metrics and inflates the window,
            // leaving dead background strips right/bottom. With the NC area
            // zeroed the window size IS the client size, so pin it here.
            try { if (_wantClient.Width > 0 && Size != _wantClient) Size = _wantClient; } catch { }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= 0x00C00000 | 0x00040000 | 0x00080000; // CAPTION | THICKFRAME | SYSMENU
                return cp;
            }
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0083) // WM_NCCALCSIZE: zero the non-client area
            {
                if (m.WParam != IntPtr.Zero) { m.Result = IntPtr.Zero; return; }
                base.WndProc(ref m);
                return;
            }
            if (m.Msg == 0x0086) { m.Result = (IntPtr)1; return; } // WM_NCACTIVATE: always active
            base.WndProc(ref m);
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int v = 2; DwmSetWindowAttribute(Handle, 33, ref v, 4); } catch { }
        }
    }

    // ---------- 信息弹窗（v6.8 重做排版） ----------
    // The old box was one fixed 440x168 template for everything: a short
    // message floated at the top over a void, and a long error text simply
    // clipped. Now the message is laid out in OnLoad with the real device
    // fonts and width it will be painted with: the dialog takes the height
    // its content needs, the first paragraph reads as a headline (bigger,
    // full-bright) and the rest as body (a step smaller, softer), and a
    // small status dot by the headline gives the box its one accent -
    // red for errors, amber for a real choice, blue for plain info.
    class InfoBox : DarkDialog
    {
        readonly string[] _paras;
        readonly Color _dotColor;
        readonly bool _twoBtn;
        RoundButton _ok, _no;
        Font _headFont, _bodyFont;
        readonly List<Rectangle> _rects = new List<Rectangle>();
        readonly List<bool> _isHead = new List<bool>();
        Rectangle _dotRect;
        static readonly Color SoftText = Color.FromArgb(198, 206, 216);
        static readonly Color InfoBlue = Color.FromArgb(72, 144, 245);

        InfoBox(string message, string title, string okText, string cancelText)
        {
            string norm = (message ?? "").Replace("\r\n", "\n");
            var parts = new List<string>();
            foreach (var p in norm.Split(new string[] { "\n\n" }, StringSplitOptions.None))
            {
                string t = p.Trim('\n');
                if (t.Length > 0) parts.Add(t);
            }
            if (parts.Count == 0) parts.Add("");
            _paras = parts.ToArray();
            _twoBtn = cancelText != null;
            if (title.IndexOf("Error") >= 0 || title == "Error" || norm.StartsWith("Something went wrong")) _dotColor = Pal.Danger;
            else if (_twoBtn) _dotColor = Pal.Amber;
            else _dotColor = InfoBlue;
            InitChrome(title, 440, 200);
            _ok = new RoundButton();
            _ok.Primary = true;
            _ok.Text = okText;
            _ok.BackColor = Pal.Green;
            _ok.ForeColor = Color.White;
            _ok.BackdropColor = Pal.Bg;
            _ok.Click += (s, e) => { DialogResult = DialogResult.OK; };
            if (!_twoBtn)
            {
                _ok.SetBounds(150, 124, 140, 34);
                Controls.Add(_ok);
                AcceptButton = _ok;
                CancelButton = _ok;
            }
            else
            {
                _ok.SetBounds(80, 124, 140, 34);
                Controls.Add(_ok);
                _no = new RoundButton();
                _no.Text = cancelText;
                _no.BackColor = Pal.Surface;
                _no.ForeColor = Color.White;
                _no.BackdropColor = Pal.Bg;
                _no.SetBounds(250, 124, 110, 34);
                _no.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
                Controls.Add(_no);
                AcceptButton = _ok;
                CancelButton = _no;
            }
            FinishChrome();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e); // DarkDialog pins the nominal size first
            try { LayoutMessage(); } catch { }
        }

        // Measure with the exact fonts/width used at paint time, then take
        // only the height the content needs and recenter (the nominal size
        // was centered before we knew the real one).
        void LayoutMessage()
        {
            float sf = ClientSize.Width / 440f;
            var fam = Font.FontFamily;
            bool multi = _paras.Length > 1;
            _headFont = new Font(fam, Font.Size + (multi ? 2f : 1f), FontStyle.Regular);
            _bodyFont = new Font(fam, Font.Size + 0.5f, FontStyle.Regular);
            int textX = (int)Math.Round(42 * sf);
            int textW = ClientSize.Width - textX - (int)Math.Round(24 * sf);
            int textTop = (int)Math.Round((40 + 18) * sf);
            int gap = (int)Math.Round(10 * sf);
            var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
            _rects.Clear(); _isHead.Clear();
            int y = textTop;
            using (var g = CreateGraphics())
            {
                for (int i = 0; i < _paras.Length; i++)
                {
                    bool head = i == 0;
                    var f = (multi && !head) ? _bodyFont : _headFont;
                    Size sz = TextRenderer.MeasureText(g, _paras[i], f, new Size(textW, int.MaxValue), flags);
                    _rects.Add(new Rectangle(textX, y, textW, sz.Height));
                    _isHead.Add(head);
                    y += sz.Height + gap;
                }
            }
            int textH = (y - gap) - textTop;
            var headRc = _rects[0];
            int dotD = Math.Max(6, (int)Math.Round(8 * sf));
            int dotY = headRc.Top + Math.Max(2, (int)Math.Round(4 * sf));
            _dotRect = new Rectangle((int)Math.Round(24 * sf), dotY, dotD, dotD);
            int btnTop = textTop + textH + (int)Math.Round(20 * sf);
            int bottom = btnTop + _ok.Height + (int)Math.Round(18 * sf);
            var wa = Screen.FromControl(this).WorkingArea;
            int maxH = (int)(wa.Height * 0.92);
            if (bottom > maxH) bottom = maxH;
            _ok.Top = btnTop;
            if (_no != null) _no.Top = btnTop;
            // Assign Size, not ClientSize: a ClientSize set after the handle
            // exists converts through the frame metrics and inflates the
            // window (+22x+56 of dead area). With the NC area zeroed the
            // window size IS the client size, so set it directly.
            Size = new Size(Width, bottom);
            Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(_dotColor)) g.FillEllipse(b, _dotRect);
            var flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding;
            for (int i = 0; i < _paras.Length && i < _rects.Count; i++)
            {
                bool head = _isHead[i];
                var f = (_paras.Length > 1 && !head) ? _bodyFont : _headFont;
                TextRenderer.DrawText(g, _paras[i], f, _rects[i], head ? Pal.Text : SoftText, flags);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { if (_headFont != null) _headFont.Dispose(); } catch { }
                try { if (_bodyFont != null) _bodyFont.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

        public static DialogResult Show(string message) { return Show(message, "Minecraft Multiplayer Helper", "OK", null); }
        public static DialogResult Show(string message, string title) { return Show(message, title, "OK", null); }
        public static DialogResult Show(string message, string title, string okText, string cancelText)
        {
            using (var f = new InfoBox(message, title, okText, cancelText)) return f.ShowDialog();
        }
    }

    // ---------- 主窗体 ----------
    // ---------- server.properties 图形编辑（v6.11） ----------
    // Edits only the handful of keys a home host actually touches, and
    // rewrites the file line by line so comments, unknown keys and their
    // order all survive. online-mode is shown but never written here:
    // this crew plays on offline accounts and it must stay false.
    class PropsForm : DarkDialog
    {
        string _dir;
        bool _running;
        System.Collections.Generic.List<string> _lines = new System.Collections.Generic.List<string>();
        System.Collections.Generic.Dictionary<string, string> _kv = new System.Collections.Generic.Dictionary<string, string>();
        ComboBox _cbDiff, _cbMode;
        TextBox _tbMax, _tbView, _tbSim, _tbSpawn, _tbMotd;
        DarkCheckBox _ckWhite, _ckPvp, _ckFly;

        public PropsForm(string serverDir, bool serverRunning)
        {
            _dir = serverDir; _running = serverRunning;
            InitChrome("Server Properties", 480, 428);
            string pf = System.IO.Path.Combine(_dir, "server.properties");
            bool have = System.IO.File.Exists(pf);
            if (have)
            {
                try
                {
                    foreach (var raw in System.IO.File.ReadAllLines(pf, Encoding.UTF8))
                    {
                        _lines.Add(raw);
                        var t = raw.Trim();
                        if (t.StartsWith("#") || t.StartsWith("!")) continue;
                        int eq = raw.IndexOf('=');
                        if (eq > 0) _kv[raw.Substring(0, eq).Trim()] = raw.Substring(eq + 1).Trim();
                    }
                }
                catch { }
            }
            int y = 14;
            _cbDiff = AddCombo("Difficulty", y, new string[] { "Peaceful", "Easy", "Normal", "Hard" }, new string[] { "peaceful", "easy", "normal", "hard" }, Get("difficulty", "easy")); y += 36;
            _cbMode = AddCombo("Game mode", y, new string[] { "Survival", "Creative", "Adventure", "Spectator" }, new string[] { "survival", "creative", "adventure", "spectator" }, Get("gamemode", "survival")); y += 36;
            _tbMax = AddNum("Max players", y, Get("max-players", "20")); y += 36;
            _tbView = AddNum("View distance (chunks)", y, Get("view-distance", "10")); y += 36;
            _tbSim = AddNum("Simulation distance (chunks)", y, Get("simulation-distance", "10")); y += 36;
            _tbSpawn = AddNum("Spawn protection radius", y, Get("spawn-protection", "16")); y += 36;
            _tbMotd = AddText("Server MOTD", y, Get("motd", "A Minecraft Server")); y += 36;
            _ckWhite = AddCheck("Enable whitelist (white-list)", y, Get("white-list", "false") == "true"); y += 32;
            _ckPvp = AddCheck("Allow PvP (pvp)", y, Get("pvp", "true") == "true"); y += 32;
            _ckFly = AddCheck("Allow flight (allow-flight)", y, Get("allow-flight", "false") == "true"); y += 36;
            var lbOn = new TextLabel();
            lbOn.Caption = "Online mode: Off (must stay off for offline multiplayer; cannot be changed here)";
            lbOn.SetBounds(20, y, 440, 22);
            lbOn.ForeColor = Pal.Mute; lbOn.BackColor = Pal.Bg; lbOn.Font = new Font("Microsoft YaHei UI", 9f);
            Controls.Add(lbOn); y += 32;
            var btnSave = new RoundButton();
            btnSave.Primary = true;
            btnSave.Text = "Save";
            btnSave.BackColor = Pal.Green; btnSave.ForeColor = Color.White; btnSave.BackdropColor = Pal.Bg;
            btnSave.SetBounds(240, y, 100, 34);
            btnSave.Click += (s2, e2) => { if (SaveProps()) DialogResult = DialogResult.OK; };
            Controls.Add(btnSave);
            var btnCancel = new RoundButton();
            btnCancel.Text = "Cancel";
            btnCancel.BackColor = Pal.Surface; btnCancel.ForeColor = Color.White; btnCancel.BackdropColor = Pal.Bg;
            btnCancel.SetBounds(352, y, 100, 34);
            btnCancel.Click += (s2, e2) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(btnCancel);
            if (!have)
            {
                btnSave.Enabled = false;
                InfoBox.Show("server.properties was not found in the server folder. It is created the first time the server starts successfully.", "Server Properties");
            }
            FinishChrome();
        }
        string Get(string k, string def) { string v; return _kv.TryGetValue(k, out v) ? v : def; }
        TextLabel AddLbl(string text, int y)
        {
            var l = new TextLabel();
            l.Caption = text;
            l.SetBounds(20, y, 140, 26);
            l.ForeColor = Pal.Sub; l.BackColor = Pal.Bg; l.Font = new Font("Microsoft YaHei UI", 9f);
            Controls.Add(l);
            return l;
        }
        ComboBox AddCombo(string label, int y, string[] items, string[] values, string cur)
        {
            AddLbl(label, y);
            var cb = new ComboBox();
            foreach (var it in items) cb.Items.Add(it);
            cb.SetBounds(170, y, 280, 26);
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.FlatStyle = FlatStyle.Flat;
            cb.BackColor = Pal.Surface; cb.ForeColor = Color.White;
            cb.Font = new Font("Microsoft YaHei UI", 9f);
            cb.Tag = values;
            int idx = Array.IndexOf(values, cur); cb.SelectedIndex = idx >= 0 ? idx : 0;
            Controls.Add(cb);
            ThemedCombo.Attach(cb);
            return cb;
        }
        TextBox AddBox(int y, string val, int w)
        {
            var t = new TextBox();
            t.SetBounds(170, y, w, 26);
            t.BackColor = Color.FromArgb(17, 20, 24);
            t.ForeColor = Pal.Text;
            t.BorderStyle = BorderStyle.None;
            t.Font = new Font("Microsoft YaHei UI", 9f);
            t.Text = val;
            Controls.Add(t);
            return t;
        }
        TextBox AddNum(string label, int y, string val) { AddLbl(label, y); return AddBox(y, val, 120); }
        TextBox AddText(string label, int y, string val) { AddLbl(label, y); return AddBox(y, val, 280); }
        DarkCheckBox AddCheck(string label, int y, bool val)
        {
            var c = new DarkCheckBox();
            c.Text = label;
            c.SetBounds(20, y, 430, 24);
            c.ForeColor = Pal.Text; c.BackColor = Pal.Bg;
            c.Font = new Font("Microsoft YaHei UI", 9f);
            c.Checked = val;
            Controls.Add(c);
            return c;
        }
        static bool IntIn(string txt, int lo, int hi, out int v)
        {
            return int.TryParse((txt ?? "").Trim(), out v) && v >= lo && v <= hi;
        }
        bool SaveProps()
        {
            int maxp, view, sim, spawn;
            if (!IntIn(_tbMax.Text, 1, 9999, out maxp)) { InfoBox.Show("Max players must be a number between 1 and 9999.", "Server Properties"); return false; }
            if (!IntIn(_tbView.Text, 2, 32, out view)) { InfoBox.Show("View distance must be a number between 2 and 32.", "Server Properties"); return false; }
            if (!IntIn(_tbSim.Text, 2, 32, out sim)) { InfoBox.Show("Simulation distance must be a number between 2 and 32.", "Server Properties"); return false; }
            if (!IntIn(_tbSpawn.Text, 0, 9999, out spawn)) { InfoBox.Show("Spawn protection radius must be a number between 0 and 9999.", "Server Properties"); return false; }
            var set = new System.Collections.Generic.Dictionary<string, string>();
            set["difficulty"] = ((string[])_cbDiff.Tag)[_cbDiff.SelectedIndex];
            set["gamemode"] = ((string[])_cbMode.Tag)[_cbMode.SelectedIndex];
            set["max-players"] = maxp.ToString();
            set["view-distance"] = view.ToString();
            set["simulation-distance"] = sim.ToString();
            set["spawn-protection"] = spawn.ToString();
            set["motd"] = _tbMotd.Text.Trim();
            set["white-list"] = _ckWhite.Checked ? "true" : "false";
            set["pvp"] = _ckPvp.Checked ? "true" : "false";
            set["allow-flight"] = _ckFly.Checked ? "true" : "false";
            var seen = new System.Collections.Generic.HashSet<string>();
            var outLines = new System.Collections.Generic.List<string>();
            foreach (var raw in _lines)
            {
                var t = raw.Trim();
                int eq = raw.IndexOf('=');
                if (eq > 0 && !t.StartsWith("#") && !t.StartsWith("!"))
                {
                    string k = raw.Substring(0, eq).Trim();
                    if (set.ContainsKey(k)) { outLines.Add(k + "=" + set[k]); seen.Add(k); continue; }
                }
                outLines.Add(raw);
            }
            foreach (var kv in set) if (!seen.Contains(kv.Key)) outLines.Add(kv.Key + "=" + kv.Value);
            try
            {
                string pf = System.IO.Path.Combine(_dir, "server.properties");
                try { System.IO.File.Copy(pf, pf + ".bak", true); } catch { }
                System.IO.File.WriteAllLines(pf, outLines.ToArray(), new UTF8Encoding(false));
            }
            catch (Exception ex) { InfoBox.Show("Save failed: " + ex.Message, "Server Properties"); return false; }
            if (_running) InfoBox.Show("Saved. The server is running, so these settings take effect on the next start.", "Server Properties");
            return true;
        }
    }

    // ---------- 玩家管理（v6.11） ----------
    // Every action is just a console command through the launcher's
    // existing stdin channel - the same thing the host could type into
    // the command box, without the typing.
    class PlayerForm : DarkDialog
    {
        ListBox _lb;
        System.Collections.Generic.List<string> _ops, _banned;
        string _me;
        Action<string> _exec;
        TextLabel _lbOps, _lbBanned;
        RoundButton _btnTp;

        public PlayerForm(System.Collections.Generic.List<string> online, System.Collections.Generic.List<string> ops,
            System.Collections.Generic.List<string> banned, string me, Action<string> exec)
        {
            _ops = ops; _banned = banned; _me = me; _exec = exec;
            InitChrome("Players", 460, 372);
            var head = new TextLabel();
            head.Caption = online.Count > 0 ? ("Online players (" + online.Count + "）") : "No players online right now";
            head.SetBounds(20, 12, 300, 22);
            head.ForeColor = Pal.Sub; head.BackColor = Pal.Bg;
            head.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            Controls.Add(head);
            _lb = new ListBox();
            _lb.SetBounds(20, 38, 196, 196);
            _lb.BackColor = Color.FromArgb(17, 20, 24);
            _lb.ForeColor = Pal.Text;
            _lb.BorderStyle = BorderStyle.None;
            _lb.Font = new Font("Microsoft YaHei UI", 9f);
            _lb.IntegralHeight = false;
            ThemedList.Attach(_lb);
            foreach (var n in online) _lb.Items.Add(n);
            if (online.Count > 0) _lb.SelectedIndex = 0;
            Controls.Add(_lb);
            int bx = 232, by = 38;
            _btnTp = AddBtn(bx, by, "Teleport to me", (s2, e2) => { string n = Sel(); if (n != null && _me != null) _exec("tp " + n + " " + _me); }); by += 38;
            AddBtn(bx, by, "Kick from server", (s2, e2) => { string n = Sel(); if (n != null && ConfirmDo("Kick " + n + " from the server?")) _exec("kick " + n); }); by += 38;
            AddBtn(bx, by, "Give OP", (s2, e2) => { string n = Sel(); if (n != null) { _exec("op " + n); if (!_ops.Contains(n)) _ops.Add(n); RefreshLists(); } }); by += 38;
            AddBtn(bx, by, "Remove OP", (s2, e2) => { string n = Sel(); if (n != null) { _exec("deop " + n); _ops.Remove(n); RefreshLists(); } }); by += 38;
            AddBtn(bx, by, "Ban", (s2, e2) => { string n = Sel(); if (n != null && ConfirmDo("Ban " + n + "? They will not be able to join (you can unban them later).")) { _exec("ban " + n); if (!_banned.Contains(n)) _banned.Add(n); RefreshLists(); } }); by += 38;
            AddBtn(bx, by, "Unban", (s2, e2) => { string n = Sel(); if (n != null) { _exec("pardon " + n); _banned.Remove(n); RefreshLists(); } });
            if (_me == null && _btnTp != null) { _btnTp.Enabled = false; }
            _lbOps = new TextLabel(); _lbOps.SetBounds(20, 244, 420, 20); _lbOps.ForeColor = Pal.Sub; _lbOps.BackColor = Pal.Bg; _lbOps.Font = new Font("Microsoft YaHei UI", 9f); Controls.Add(_lbOps);
            _lbBanned = new TextLabel(); _lbBanned.SetBounds(20, 268, 420, 20); _lbBanned.ForeColor = Pal.Sub; _lbBanned.BackColor = Pal.Bg; _lbBanned.Font = new Font("Microsoft YaHei UI", 9f); Controls.Add(_lbBanned);
            var hint = new TextLabel();
            hint.Caption = _me == null ? "Note: \"Teleport to me\" is unavailable while you are not online yourself." : "Actions are sent to the server as console commands, just like typing them in the command box.";
            hint.SetBounds(20, 292, 420, 20); hint.ForeColor = Pal.Mute; hint.BackColor = Pal.Bg; hint.Font = new Font("Microsoft YaHei UI", 9f);
            Controls.Add(hint);
            var close = new RoundButton();
            close.Text = "Close";
            close.SetBounds(340, 322, 100, 32);
            close.BackColor = Pal.Surface; close.ForeColor = Color.White; close.BackdropColor = Pal.Bg;
            close.Font = new Font("Microsoft YaHei UI", 9f);
            close.Click += (s2, e2) => { DialogResult = DialogResult.Cancel; };
            Controls.Add(close);
            CancelButton = close;
            RefreshLists();
            FinishChrome();
        }
        RoundButton AddBtn(int x, int y, string text, EventHandler onClick)
        {
            var b = new RoundButton();
            b.Text = text;
            b.SetBounds(x, y, 208, 30);
            b.BackColor = Pal.Surface; b.ForeColor = Pal.Text; b.BackdropColor = Pal.Bg;
            b.Font = new Font("Microsoft YaHei UI", 9f);
            b.Click += onClick;
            Controls.Add(b);
            return b;
        }
        string Sel() { return _lb != null && _lb.SelectedItem != null ? _lb.SelectedItem.ToString() : null; }
        bool ConfirmDo(string msg)
        {
            using (var c = new ConfirmBox(msg)) { var r = c.ShowDialog(this); return r == DialogResult.OK && c.ChoiceYes; }
        }
        void RefreshLists()
        {
            if (_lbOps != null) _lbOps.Caption = "OP list: " + (_ops.Count > 0 ? string.Join("、", _ops.ToArray()) : "(none)");
            if (_lbBanned != null) _lbBanned.Caption = "Ban list: " + (_banned.Count > 0 ? string.Join("、", _banned.ToArray()) : "(none)");
        }
    }

    // ---------- Minecraft status protocol (SLP) query (v6.11) ----------
    // The Server List Ping from the protocol documentation: handshake with
    // next-state 1, a status request, a JSON response, then ping/pong for
    // latency. Lets the monitor ask the server itself for its version,
    // MOTD, player count and player names instead of guessing from the
    // outside.
    class SlpResult
    {
        public bool Ok;
        public string Version = "";
        public string Motd = "";
        public int Online = -1;
        public int Max = -1;
        public System.Collections.Generic.List<string> Names = new System.Collections.Generic.List<string>();
        public long PingMs = -1;
    }

    static class Slp
    {
        static void WriteVarInt(Stream st, int v)
        {
            uint uv = (uint)v;
            while (true) { byte b = (byte)(uv & 0x7F); uv >>= 7; if (uv != 0) { st.WriteByte((byte)(b | 0x80)); } else { st.WriteByte(b); break; } }
        }
        static int ReadVarInt(Stream st)
        {
            int result = 0, shift = 0, b;
            do { b = st.ReadByte(); if (b < 0) throw new EndOfStreamException(); result |= (b & 0x7F) << shift; shift += 7; if (shift > 35) throw new InvalidDataException(); } while ((b & 0x80) != 0);
            return result;
        }
        static byte[] Packet(byte[] payload)
        {
            using (var ms = new MemoryStream()) { WriteVarInt(ms, payload.Length); ms.Write(payload, 0, payload.Length); return ms.ToArray(); }
        }
        static byte[] ReadPacket(Stream st, out int id)
        {
            int len = ReadVarInt(st);
            if (len <= 0 || len > (1 << 21)) throw new InvalidDataException();
            byte[] buf = new byte[len];
            int got = 0;
            while (got < len) { int r = st.Read(buf, got, len - got); if (r <= 0) throw new EndOfStreamException(); got += r; }
            using (var ms = new MemoryStream(buf)) { id = ReadVarInt(ms); byte[] rest = new byte[(int)(len - ms.Position)]; ms.Read(rest, 0, rest.Length); return rest; }
        }
        public static SlpResult Query(string host, int port, int timeoutMs)
        {
            var res = new SlpResult();
            try
            {
                using (var tcp = new System.Net.Sockets.TcpClient())
                {
                    var ar = tcp.BeginConnect(host, port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(timeoutMs)) return res;
                    tcp.EndConnect(ar);
                    var st = tcp.GetStream();
                    st.ReadTimeout = timeoutMs; st.WriteTimeout = timeoutMs;
                    byte[] hs;
                    using (var ms = new MemoryStream())
                    {
                        ms.WriteByte(0);
                        WriteVarInt(ms, -1);
                        byte[] addr = Encoding.UTF8.GetBytes(host);
                        WriteVarInt(ms, addr.Length); ms.Write(addr, 0, addr.Length);
                        ms.WriteByte((byte)(port >> 8)); ms.WriteByte((byte)(port & 0xFF));
                        WriteVarInt(ms, 1);
                        hs = ms.ToArray();
                    }
                    byte[] p1 = Packet(hs); st.Write(p1, 0, p1.Length);
                    byte[] req = Packet(new byte[] { 0 }); st.Write(req, 0, req.Length);
                    int id; byte[] body = ReadPacket(st, out id);
                    if (id != 0) return res;
                    string json = Encoding.UTF8.GetString(body);
                    var mv = Regex.Match(json, "\"version\"\\s*:\\s*\\{[^}]*\"name\"\\s*:\\s*\"([^\"]*)\"");
                    if (mv.Success) res.Version = mv.Groups[1].Value;
                    var mo = Regex.Match(json, "\"online\"\\s*:\\s*(\\d+)"); if (mo.Success) res.Online = int.Parse(mo.Groups[1].Value);
                    var mm = Regex.Match(json, "\"max\"\\s*:\\s*(\\d+)"); if (mm.Success) res.Max = int.Parse(mm.Groups[1].Value);
                    var md = Regex.Match(json, "\"description\"\\s*:\\s*\"([^\"]*)\"");
                    if (md.Success) res.Motd = md.Groups[1].Value;
                    else { var mt = Regex.Match(json, "\"description\"\\s*:\\s*\\{[^{}]*\"text\"\\s*:\\s*\"([^\"]*)\""); if (mt.Success) res.Motd = mt.Groups[1].Value; }
                    var msamp = Regex.Match(json, "\"sample\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                    if (msamp.Success) { foreach (Match mn in Regex.Matches(msamp.Groups[1].Value, "\"name\"\\s*:\\s*\"([^\"]*)\"")) res.Names.Add(mn.Groups[1].Value); }
                    res.Motd = Regex.Replace(res.Motd ?? "", "\u00a7.", "");
                    try
                    {
                        long payload = DateTime.Now.Ticks;
                        byte[] pb; using (var ms = new MemoryStream()) { ms.WriteByte(1); for (int i = 7; i >= 0; i--) ms.WriteByte((byte)(payload >> (8 * i))); pb = ms.ToArray(); }
                        byte[] pp = Packet(pb);
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        st.Write(pp, 0, pp.Length);
                        int pid; ReadPacket(st, out pid);
                        sw.Stop();
                        if (pid == 1) res.PingMs = sw.ElapsedMilliseconds;
                    }
                    catch { }
                    res.Ok = res.Online >= 0;
                }
            }
            catch { }
            return res;
        }
    }

    // ---------- v6.20: MC / Java version helpers ----------
    static class McJava
    {
        // The java.exe a check should look at: the configured one first,
        // then PATH. Returns null when nothing is found. Never throws.
        public static string ResolveJavaExe(Config cfg)
        {
            try
            {
                if (cfg != null && !string.IsNullOrEmpty(cfg.JavaPath) && File.Exists(cfg.JavaPath)) return cfg.JavaPath;
                string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (string dir in pathEnv.Split(';'))
                {
                    string d = dir.Trim().Trim('"');
                    if (d.Length == 0) continue;
                    string cand = Path.Combine(d, "java.exe");
                    if (File.Exists(cand)) return cand;
                }
            }
            catch { }
            return null;
        }

        // Run `java -version` and parse the major version (17, 21; a
        // "1.8.0_x" banner means 8). 0 = unknown. 5 s timeout, never throws.
        public static int GetJavaMajor(string javaExe)
        {
            if (string.IsNullOrEmpty(javaExe)) return 0;
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = javaExe;
                psi.Arguments = "-version";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                using (var p = Process.Start(psi))
                {
                    if (p == null) return 0;
                    // The banner goes to stderr; drain stdout on a side
                    // thread so a chatty JVM can never fill the pipe.
                    var drain = new Thread(() => { try { p.StandardOutput.ReadToEnd(); } catch { } });
                    drain.IsBackground = true;
                    drain.Start();
                    string err = "";
                    var reader = new Thread(() => { try { err = p.StandardError.ReadToEnd(); } catch { } });
                    reader.IsBackground = true;
                    reader.Start();
                    if (!p.WaitForExit(5000)) { try { p.Kill(); } catch { } return 0; }
                    reader.Join(1500);
                    return ParseJavaVersionText(err);
                }
            }
            catch { return 0; }
        }

        public static int ParseJavaVersionText(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var m = Regex.Match(text, "version\\s+\"(\\d+)(?:\\.(\\d+))?");
            if (!m.Success) return 0;
            int a, b = 0;
            if (!int.TryParse(m.Groups[1].Value, out a)) return 0;
            if (m.Groups[2].Success) int.TryParse(m.Groups[2].Value, out b);
            return a == 1 ? b : a; // 1.8.0_xxx is Java 8
        }

        // Minecraft version from a server jar's version.json ("name").
        // Vanilla / Paper / Fabric launch jars all carry one. Null when
        // unreadable. Never throws, never blocks on a corrupt jar.
        public static string ReadMcVersion(string jarPath)
        {
            try
            {
                if (string.IsNullOrEmpty(jarPath) || !File.Exists(jarPath)) return null;
                using (var fs = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    var entry = za.GetEntry("version.json");
                    if (entry == null) return null;
                    using (var es = entry.Open())
                    using (var sr = new StreamReader(es, Encoding.UTF8))
                    {
                        string json = sr.ReadToEnd();
                        var m = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                        if (m.Success) return m.Groups[1].Value.Trim();
                        var m2 = Regex.Match(json, "\"id\"\\s*:\\s*\"([^\"]+)\"");
                        if (m2.Success) return m2.Groups[1].Value.Trim();
                    }
                }
            }
            catch { }
            return null;
        }

        // Required Java major for an MC version name; 0 = unknown.
        public static int RequiredJava(string mcVersion)
        {
            int[] v = ParseMcVersion(mcVersion);
            if (v == null) return 0;
            if (v[0] >= 26) return 21; // year-based versions: best known estimate, see DESIGN_v620.md
            if (v[0] != 1) return 0;
            if (v[1] > 20) return 21;
            if (v[1] == 20) return v[2] >= 5 ? 21 : 17;
            if (v[1] >= 18) return 17;
            if (v[1] == 17) return 16;
            return 8;
        }

        // "1.20.1" -> [1,20,1]; snapshots / junk -> null.
        public static int[] ParseMcVersion(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var m = Regex.Match(s.Trim(), "^(\\d+)\\.(\\d+)(?:\\.(\\d+))?$");
            if (!m.Success) return null;
            int a, b, c = 0;
            if (!int.TryParse(m.Groups[1].Value, out a)) return null;
            if (!int.TryParse(m.Groups[2].Value, out b)) return null;
            if (m.Groups[3].Success) int.TryParse(m.Groups[3].Value, out c);
            return new int[] { a, b, c };
        }
    }

    // ---------- v6.20: download / hash / zip helpers ----------
    static class DlUtil
    {
        public static WebClient NewClient()
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            var wc = new WebClient();
            wc.Headers[HttpRequestHeader.UserAgent] = "MCLauncherApp/6.20";
            wc.Encoding = Encoding.UTF8;
            return wc;
        }

        // Small JSON GET with a hard timeout (WebClient has none of its
        // own for string downloads, so race it against a timer).
        public static string DownloadStringTimed(string url, int timeoutSec, out string err)
        {
            err = null;
            string result = null;
            string errLocal = null;
            var done = new ManualResetEvent(false);
            using (var wc = NewClient())
            {
                wc.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Error != null) errLocal = e.Error.Message;
                    else if (!e.Cancelled) result = e.Result;
                    done.Set();
                };
                try { wc.DownloadStringAsync(new Uri(url)); }
                catch (Exception ex) { err = ex.Message; return null; }
                if (!done.WaitOne(timeoutSec * 1000))
                {
                    try { wc.CancelAsync(); } catch { }
                    err = "request timed out after " + timeoutSec + " s: " + url;
                    return null;
                }
            }
            err = errLocal;
            return result;
        }

        // Big-file download with progress. Lands in "<dest>.part" first and
        // is only renamed to destPath after a clean finish - the "safe
        // promotion" rule; partial files are deleted on error/cancel.
        // Returns null on success, "cancelled" on cancel, else the error.
        public static string DownloadFile(WebClient wc, string url, string destPath, Action<int> progress, Func<bool> isCancelled)
        {
            string part = destPath + ".part";
            TryDelete(part);
            var done = new ManualResetEvent(false);
            Exception error = null;
            bool cancelled = false;
            DownloadProgressChangedEventHandler ph = (s, e) => { if (progress != null) { try { progress(e.ProgressPercentage); } catch { } } };
            System.ComponentModel.AsyncCompletedEventHandler ch = (s, e) => { error = e.Error; cancelled = e.Cancelled; done.Set(); };
            wc.DownloadProgressChanged += ph;
            wc.DownloadFileCompleted += ch;
            try
            {
                wc.DownloadFileAsync(new Uri(url), part);
                while (!done.WaitOne(200))
                {
                    if (isCancelled != null && isCancelled()) { try { wc.CancelAsync(); } catch { } }
                }
            }
            catch (Exception ex) { TryDelete(part); return ex.Message; }
            finally
            {
                wc.DownloadProgressChanged -= ph;
                wc.DownloadFileCompleted -= ch;
            }
            if (cancelled || (isCancelled != null && isCancelled())) { TryDelete(part); return "cancelled"; }
            if (error != null) { TryDelete(part); return error.Message; }
            try
            {
                if (File.Exists(destPath)) File.Delete(destPath);
                File.Move(part, destPath);
            }
            catch (Exception ex) { TryDelete(part); return ex.Message; }
            return null;
        }

        public static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        public static string Sha256File(string path) { return HashFile(path, SHA256.Create()); }
        public static string Sha1File(string path) { return HashFile(path, SHA1.Create()); }

        static string HashFile(string path, HashAlgorithm alg)
        {
            using (alg)
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] h = alg.ComputeHash(fs);
                var sb = new StringBuilder();
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // Value of "key" (a quoted string) appearing after startIndex in a
        // JSON text. Hand-rolled on purpose: no JSON library may be added.
        public static string JsonStrAfter(string json, int startIndex, string key)
        {
            if (json == null || startIndex < 0) return null;
            var m = Regex.Match(json.Substring(startIndex), "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        public static int JsonIndexOf(string json, string pattern)
        {
            if (json == null) return -1;
            var m = Regex.Match(json, pattern);
            return m.Success ? m.Index : -1;
        }

        // Fabric meta lists put "version" right before "stable" in each
        // entry, newest first: the first "stable": true match is the
        // newest stable build (loader list / installer list alike).
        public static string FirstStableVersion(string json)
        {
            if (json == null) return null;
            var m = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"stable\"\\s*:\\s*true");
            return m.Success ? m.Groups[1].Value : null;
        }

        // Zip extraction with a path-traversal guard. Returns error or null.
        public static string ExtractZip(string zipPath, string destDir)
        {
            try
            {
                string fullDest = Path.GetFullPath(destDir);
                using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var za = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (var entry in za.Entries)
                    {
                        string rel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        string target = Path.GetFullPath(Path.Combine(fullDest, rel));
                        if (!target.Equals(fullDest, StringComparison.OrdinalIgnoreCase) &&
                            !target.StartsWith(fullDest + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
                        if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(target); continue; }
                        string parent = Path.GetDirectoryName(target);
                        if (parent != null && parent.Length > 0) Directory.CreateDirectory(parent);
                        using (var es = entry.Open())
                        using (var os = new FileStream(target, FileMode.Create, FileAccess.Write))
                        {
                            es.CopyTo(os);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex) { return ex.Message; }
        }

        // Find bin\java.exe under root (depth-limited). Null when absent.
        public static string FindJavaExe(string root)
        {
            try
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
                string direct = Path.Combine(root, "bin\\java.exe");
                if (File.Exists(direct)) return direct;
                foreach (string sub in Directory.GetDirectories(root))
                {
                    string cand = Path.Combine(sub, "bin\\java.exe");
                    if (File.Exists(cand)) return cand;
                    foreach (string sub2 in Directory.GetDirectories(sub))
                    {
                        string cand2 = Path.Combine(sub2, "bin\\java.exe");
                        if (File.Exists(cand2)) return cand2;
                    }
                }
            }
            catch { }
            return null;
        }
    }

    // ---------- v6.20: first-run setup wizard ----------
    // Downloads a matching Java runtime (Adoptium) and a server jar
    // (Vanilla / Fabric / Paper), verifies hashes where upstream publishes
    // them, writes eula.txt only with explicit consent, and saves the
    // resulting paths back into the shared Config.
    class WizardForm : DarkDialog
    {
        readonly Config _cfg;
        ComboBox cbVersion;
        RadioButton rbVanilla, rbFabric, rbPaper;
        TextBox tbDir, tbLog;
        CheckBox chkEula;
        ProgressBar pb;
        TextLabel lblStep;
        RoundButton btnStart, btnCancelDl, btnClose;
        volatile bool _cancel;
        WebClient _wc;
        bool _running;
        public bool InstalledOk = false;

        // captured on the UI thread when Start is clicked
        string _ver, _dir;
        int _type; // 0 vanilla, 1 fabric, 2 paper
        bool _eulaAgreed;

        public WizardForm(Config cfg, string initialDir)
        {
            _cfg = cfg;
            InitChrome("First-run setup wizard", 660, 612);

            int lx = 20, tx = 170;
            AddLabel("Minecraft version", lx, 18);
            cbVersion = new ComboBox();
            cbVersion.DropDownStyle = ComboBoxStyle.DropDown;
            cbVersion.Items.AddRange(new object[] { "1.20.1", "1.21.1", "1.21.11", "1.21.4", "1.20.4", "1.20.6", "1.19.4", "1.18.2" });
            cbVersion.Text = "1.20.1";
            cbVersion.SetBounds(tx, 14, 300, 26);
            cbVersion.FlatStyle = FlatStyle.Flat;
            cbVersion.BackColor = Pal.Surface;
            cbVersion.ForeColor = Color.White;
            Controls.Add(cbVersion);
            ThemedCombo.Attach(cbVersion);
            AddHint("Any release version works", tx + 308, 18);

            AddLabel("Server type", lx, 56);
            rbVanilla = AddRadio("Vanilla", tx, 54);
            rbFabric = AddRadio("Fabric", tx + 90, 54);
            rbPaper = AddRadio("Paper", tx + 180, 54);
            rbFabric.Checked = true;

            AddLabel("Server folder", lx, 94);
            tbDir = new TextBox();
            tbDir.SetBounds(tx, 90, 340, 26);
            tbDir.BackColor = Pal.Surface;
            tbDir.ForeColor = Color.White;
            tbDir.BorderStyle = BorderStyle.FixedSingle;
            tbDir.Text = string.IsNullOrEmpty(initialDir) ? "D:\\MCServer" : initialDir;
            Controls.Add(tbDir);
            var btnBrowse = new RoundButton();
            btnBrowse.Text = "Browse...";
            btnBrowse.BackColor = Pal.Surface;
            btnBrowse.ForeColor = Color.White;
            btnBrowse.BackdropColor = Pal.Bg;
            btnBrowse.SetBounds(520, 88, 110, 30);
            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = "Pick the server folder (it does not need to exist yet - pick a parent and edit the path text)";
                    if (Directory.Exists(tbDir.Text.Trim())) fbd.SelectedPath = tbDir.Text.Trim();
                    if (fbd.ShowDialog(this) == DialogResult.OK) tbDir.Text = fbd.SelectedPath;
                }
            };
            Controls.Add(btnBrowse);

            chkEula = new CheckBox();
            chkEula.Text = "I agree to the Minecraft EULA";
            chkEula.ForeColor = Color.White;
            chkEula.BackColor = Color.Transparent;
            chkEula.SetBounds(tx, 128, 230, 24);
            Controls.Add(chkEula);
            var lnkEula = new LinkLabel();
            lnkEula.Text = "Read the EULA";
            lnkEula.LinkColor = Color.FromArgb(120, 180, 255);
            lnkEula.ActiveLinkColor = Color.FromArgb(160, 200, 255);
            lnkEula.BackColor = Color.Transparent;
            lnkEula.SetBounds(tx + 232, 130, 120, 24);
            lnkEula.LinkClicked += (s, e) => { try { Process.Start("https://www.minecraft.net/en-us/eula"); } catch { } };
            Controls.Add(lnkEula);
            AddHint("Without the tick you must set eula=true in eula.txt yourself before the first start", tx, 154);

            lblStep = new TextLabel();
            lblStep.TopAlign = true;
            lblStep.Caption = "Fill in the three items above and click Start install. Java and the server are downloaded and verified automatically.";
            lblStep.TextShiftX = 2f; lblStep.TextShiftY = -0.67f;
            lblStep.SetBounds(20, 186, 620, 24);
            lblStep.BackColor = Color.Transparent; lblStep.BackdropColor = Pal.Bg; lblStep.ForeColor = Pal.Sub;
            Controls.Add(lblStep);

            pb = new ProgressBar();
            pb.SetBounds(20, 212, 620, 16);
            pb.Minimum = 0; pb.Maximum = 100; pb.Value = 0;
            Controls.Add(pb);

            tbLog = new TextBox();
            tbLog.Multiline = true;
            tbLog.ReadOnly = true;
            tbLog.ScrollBars = ScrollBars.Vertical;
            tbLog.SetBounds(20, 238, 620, 300);
            tbLog.BackColor = Pal.Surface;
            tbLog.ForeColor = Color.White;
            tbLog.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(tbLog);

            btnStart = new RoundButton();
            btnStart.Primary = true;
            btnStart.Text = "Start install";
            btnStart.BackColor = Pal.Green;
            btnStart.ForeColor = Color.White;
            btnStart.BackdropColor = Pal.Bg;
            btnStart.SetBounds(20, 552, 140, 36);
            btnStart.Click += (s, e) => StartInstall();
            Controls.Add(btnStart);

            btnCancelDl = new RoundButton();
            btnCancelDl.Text = "Cancel";
            btnCancelDl.BackColor = Pal.Surface;
            btnCancelDl.ForeColor = Color.White;
            btnCancelDl.BackdropColor = Pal.Bg;
            btnCancelDl.SetBounds(172, 552, 100, 36);
            btnCancelDl.Enabled = false;
            btnCancelDl.Click += (s, e) =>
            {
                _cancel = true;
                try { if (_wc != null) _wc.CancelAsync(); } catch { }
                WizLog("Cancelling...");
            };
            Controls.Add(btnCancelDl);

            btnClose = new RoundButton();
            btnClose.Text = "Close";
            btnClose.BackColor = Pal.Surface;
            btnClose.ForeColor = Color.White;
            btnClose.BackdropColor = Pal.Bg;
            btnClose.SetBounds(520, 552, 120, 36);
            btnClose.Click += (s, e) => { DialogResult = InstalledOk ? DialogResult.OK : DialogResult.Cancel; };
            Controls.Add(btnClose);

            FinishChrome();
        }

        void AddLabel(string text, int x, int y)
        {
            var lb = new TextLabel();
            lb.TopAlign = true;
            lb.Caption = text;
            lb.TextShiftX = 2f; lb.TextShiftY = -0.67f;
            lb.SetBounds(x, y, 140, 26);
            lb.BackColor = Color.Transparent; lb.BackdropColor = Pal.Bg; lb.ForeColor = Pal.Sub;
            Controls.Add(lb);
        }

        void AddHint(string text, int x, int y)
        {
            var lb = new TextLabel();
            lb.TopAlign = true;
            lb.Caption = text;
            lb.TextShiftX = 2f; lb.TextShiftY = -0.67f;
            lb.SetBounds(x, y, 300, 24);
            lb.BackColor = Color.Transparent; lb.BackdropColor = Pal.Bg; lb.ForeColor = Pal.Sub;
            Controls.Add(lb);
        }

        RadioButton AddRadio(string text, int x, int y)
        {
            var rb = new RadioButton();
            rb.Text = text;
            rb.ForeColor = Color.White;
            rb.BackColor = Color.Transparent;
            rb.SetBounds(x, y, 84, 26);
            Controls.Add(rb);
            return rb;
        }

        void WizUi(Action a)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) BeginInvoke(a); else a(); } catch { }
        }

        void WizLog(string s)
        {
            WizUi(() =>
            {
                tbLog.AppendText(s + "\r\n");
                tbLog.SelectionStart = tbLog.Text.Length;
                tbLog.ScrollToCaret();
            });
        }

        void WizStep(string s) { WizUi(() => { lblStep.Caption = s; }); }

        void WizProgress(int pct)
        {
            WizUi(() =>
            {
                if (pct >= 0) { pb.Style = ProgressBarStyle.Continuous; pb.Value = Math.Min(100, pct); }
                else pb.Style = ProgressBarStyle.Marquee;
            });
        }

        void SetRunningUi(bool running)
        {
            WizUi(() =>
            {
                _running = running;
                btnStart.Enabled = !running;
                btnCancelDl.Enabled = running;
                cbVersion.Enabled = !running;
                tbDir.Enabled = !running;
                rbVanilla.Enabled = !running;
                rbFabric.Enabled = !running;
                rbPaper.Enabled = !running;
                chkEula.Enabled = !running;
                if (!running) { pb.Style = ProgressBarStyle.Continuous; }
            });
        }

        void StartInstall()
        {
            if (_running) return;
            _ver = cbVersion.Text.Trim();
            _dir = tbDir.Text.Trim();
            _type = rbPaper.Checked ? 2 : (rbFabric.Checked ? 1 : 0);
            _eulaAgreed = chkEula.Checked;
            if (_ver.Length == 0) { InfoBox.Show("Enter a Minecraft version first.", "First-run setup wizard"); return; }
            if (McJava.ParseMcVersion(_ver) == null) { InfoBox.Show("That does not look like a release version number. Enter something like 1.20.1 (snapshots are not supported by the wizard).", "First-run setup wizard"); return; }
            if (_dir.Length == 0) { InfoBox.Show("Enter a server folder first.", "First-run setup wizard"); return; }
            string existingJar = Path.Combine(_dir, "server.jar");
            if (File.Exists(existingJar))
            {
                var r = InfoBox.Show("The target folder already contains a server.jar. Continuing will overwrite it.\n\nYour world and mods are not touched (the wizard only replaces the jar and adds missing files). Continue?", "First-run setup wizard", "Overwrite", "Cancel");
                if (r != DialogResult.OK) return;
            }
            _cancel = false;
            SetRunningUi(true);
            WizLog("-- Install started: Minecraft " + _ver + " / " + (_type == 2 ? "Paper" : (_type == 1 ? "Fabric" : "Vanilla")) + " -> " + _dir);
            var t = new Thread(Run);
            t.IsBackground = true;
            t.Start();
        }

        void Run()
        {
            try
            {
                int[] vp = McJava.ParseMcVersion(_ver);
                int required = McJava.RequiredJava(_ver);
                if (required <= 0) { Fail("Cannot determine which Java Minecraft " + _ver + " needs; wizard aborted (nothing was changed)."); return; }
                if (vp != null && vp[0] >= 26) WizLog("Note: 26.x is Mojang's new year-based versioning; its official Java requirement may be higher than the Java 21 estimate used here - watch the server log after install.");

                // ---- Step 1: a suitable Java ----
                WizStep("Step 1: preparing Java " + required);
                string javaExe = null;
                int cfgMajor = 0;
                if (!string.IsNullOrEmpty(_cfg.JavaPath) && File.Exists(_cfg.JavaPath)) cfgMajor = McJava.GetJavaMajor(_cfg.JavaPath);
                if (cfgMajor >= required)
                {
                    javaExe = _cfg.JavaPath;
                    WizLog("The configured Java is major version " + cfgMajor + " and meets the requirement; using it: " + javaExe);
                }
                if (javaExe == null)
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string localRoot = Path.Combine(baseDir, "java\\" + required);
                    string found = DlUtil.FindJavaExe(localRoot);
                    if (found != null && McJava.GetJavaMajor(found) >= required)
                    {
                        javaExe = found;
                        WizLog("Using the previously downloaded Java next to the app: " + javaExe);
                    }
                }
                if (javaExe == null)
                {
                    if (_cancel) { Cancelled(); return; }
                    if (required != 17 && required != 21)
                    {
                        Fail("Minecraft " + _ver + " needs Java " + required + ", but this wizard can only auto-download Java 17 / 21. Install Java " + required + " manually, point Settings at its java.exe, then start the server.");
                        return;
                    }
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string javaRoot = Path.Combine(baseDir, "java");
                    Directory.CreateDirectory(javaRoot);
                    string zipPath = Path.Combine(javaRoot, "temurin" + required + ".zip");
                    string link = null, checksum = null, metaErr;
                    string meta = DlUtil.DownloadStringTimed("https://api.adoptium.net/v3/assets/latest/" + required + "/hotspot?os=windows&architecture=x64&image_type=jre&vendor=eclipse", 30, out metaErr);
                    if (meta != null)
                    {
                        int pkg = DlUtil.JsonIndexOf(meta, "\"package\"\\s*:");
                        if (pkg >= 0)
                        {
                            link = DlUtil.JsonStrAfter(meta, pkg, "link");
                            checksum = DlUtil.JsonStrAfter(meta, pkg, "checksum");
                        }
                    }
                    if (link == null)
                    {
                        link = "https://api.adoptium.net/v3/binary/latest/" + required + "/ga/windows/x64/jre/hotspot/normal/eclipse";
                        checksum = null;
                        WizLog("The Adoptium assets API gave no direct link (" + (metaErr ?? "no detail") + "); falling back to the official binary endpoint, which publishes no checksum - this Java download will not be hash-verified.");
                    }
                    WizLog("Downloading Java " + required + " (Eclipse Temurin JRE, about 40-50 MB)...");
                    string dlErr = DownloadWithProgress(link, zipPath, "Java " + required);
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail("Java download failed: " + dlErr); return; }
                    if (checksum != null)
                    {
                        WizLog("Verifying the Java zip SHA256...");
                        string actual = DlUtil.Sha256File(zipPath);
                        if (!string.Equals(actual, checksum, StringComparison.OrdinalIgnoreCase))
                        {
                            DlUtil.TryDelete(zipPath);
                            Fail("Java zip SHA256 mismatch; the download was deleted. Expected " + checksum + ", got " + actual + ". Check your network and retry.");
                            return;
                        }
                        WizLog("SHA256 verified.");
                    }
                    string destDir = Path.Combine(javaRoot, required.ToString());
                    WizLog("Extracting Java to " + destDir + " ...");
                    string exErr = DlUtil.ExtractZip(zipPath, destDir);
                    DlUtil.TryDelete(zipPath);
                    if (exErr != null) { Fail("Java extraction failed: " + exErr); return; }
                    javaExe = DlUtil.FindJavaExe(destDir);
                    if (javaExe == null) { Fail("Extraction finished but no bin\\java.exe was found - unexpected zip layout; aborted (config not saved)."); return; }
                    int got = McJava.GetJavaMajor(javaExe);
                    if (got < required) { Fail("The downloaded Java reports major version " + got + ", still below the required " + required + "; aborted (config not saved)."); return; }
                    WizLog("Java ready: major version " + got + ", " + javaExe);
                }
                if (_cancel) { Cancelled(); return; }

                // ---- Step 2: the server jar ----
                WizStep("Step 2: downloading the server jar");
                Directory.CreateDirectory(_dir);
                string jarDest = Path.Combine(_dir, "server.jar");
                if (_type == 0)
                {
                    string err;
                    string manifest = DlUtil.DownloadStringTimed("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json", 30, out err);
                    if (manifest == null) { Fail("Cannot fetch the Mojang version manifest: " + err); return; }
                    int vi = DlUtil.JsonIndexOf(manifest, "\"id\"\\s*:\\s*\"" + Regex.Escape(_ver) + "\"");
                    string verUrl = vi >= 0 ? DlUtil.JsonStrAfter(manifest, vi, "url") : null;
                    if (verUrl == null) { Fail("Minecraft " + _ver + " is not in the version manifest (check the number; releases only)."); return; }
                    string verJson = DlUtil.DownloadStringTimed(verUrl, 30, out err);
                    if (verJson == null) { Fail("Cannot fetch the version metadata for " + _ver + ": " + err); return; }
                    int si = DlUtil.JsonIndexOf(verJson, "\"server\"\\s*:\\s*\\{");
                    string jarUrl = si >= 0 ? DlUtil.JsonStrAfter(verJson, si, "url") : null;
                    string sha1 = si >= 0 ? DlUtil.JsonStrAfter(verJson, si, "sha1") : null;
                    if (jarUrl == null) { Fail("The version metadata has no server download."); return; }
                    WizLog("Downloading the vanilla server (official Mojang)...");
                    string dlErr = DownloadWithProgress(jarUrl, jarDest, "vanilla server");
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail("Server download failed: " + dlErr); return; }
                    if (sha1 != null)
                    {
                        WizLog("Verifying the server SHA1...");
                        string actual = DlUtil.Sha1File(jarDest);
                        if (!string.Equals(actual, sha1, StringComparison.OrdinalIgnoreCase))
                        {
                            DlUtil.TryDelete(jarDest);
                            Fail("Server SHA1 mismatch; the jar was deleted. Expected " + sha1 + ", got " + actual + ". Check your network and retry.");
                            return;
                        }
                        WizLog("SHA1 verified.");
                    }
                }
                else if (_type == 1)
                {
                    string err;
                    string loaders = DlUtil.DownloadStringTimed("https://meta.fabricmc.net/v2/versions/loader/" + _ver, 30, out err);
                    if (loaders == null) { Fail("Cannot fetch the Fabric loader list: " + err); return; }
                    string loader = DlUtil.FirstStableVersion(loaders);
                    if (loader == null) { Fail("Fabric has no stable loader for Minecraft " + _ver + " yet; pick another version."); return; }
                    string installers = DlUtil.DownloadStringTimed("https://meta.fabricmc.net/v2/versions/installer", 30, out err);
                    if (installers == null) { Fail("Cannot fetch the Fabric installer list: " + err); return; }
                    string installer = DlUtil.FirstStableVersion(installers);
                    if (installer == null) { Fail("The Fabric installer list looks wrong."); return; }
                    string jarUrl = "https://meta.fabricmc.net/v2/versions/loader/" + _ver + "/" + loader + "/" + installer + "/server/jar";
                    WizLog("Downloading the Fabric server launch jar (loader " + loader + ", installer " + installer + ")...");
                    WizLog("Note: Fabric publishes no hash for this jar, so it is protected by HTTPS only and is not hash-verified. On first start it downloads the vanilla server and libraries itself.");
                    string dlErr = DownloadWithProgress(jarUrl, jarDest, "Fabric server");
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail("Fabric server download failed: " + dlErr); return; }
                    try { Directory.CreateDirectory(Path.Combine(_dir, "mods")); } catch { }
                    WizLog("Created the mods folder. Reminder: Fabric servers usually also need the Fabric API mod in mods, and every player needs the same both-sides mods client-side.");
                }
                else
                {
                    string err;
                    string builds = DlUtil.DownloadStringTimed("https://fill.papermc.io/v3/projects/paper/versions/" + _ver + "/builds", 30, out err);
                    if (builds == null) { Fail("Cannot fetch the Paper build list (Fill API): " + err + ". Note: versions without Paper builds return an error here."); return; }
                    int si = DlUtil.JsonIndexOf(builds, "\"server:default\"");
                    string jarUrl = si >= 0 ? DlUtil.JsonStrAfter(builds, si, "url") : null;
                    string sha256 = si >= 0 ? DlUtil.JsonStrAfter(builds, si, "sha256") : null;
                    if (jarUrl == null) { Fail("No downloadable server found in the Paper build list."); return; }
                    WizLog("Downloading the Paper server (latest build)...");
                    string dlErr = DownloadWithProgress(jarUrl, jarDest, "Paper server");
                    if (dlErr == "cancelled") { Cancelled(); return; }
                    if (dlErr != null) { Fail("Paper server download failed: " + dlErr); return; }
                    if (sha256 != null)
                    {
                        WizLog("Verifying the server SHA256...");
                        string actual = DlUtil.Sha256File(jarDest);
                        if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase))
                        {
                            DlUtil.TryDelete(jarDest);
                            Fail("Server SHA256 mismatch; the jar was deleted. Expected " + sha256 + ", got " + actual + ". Check your network and retry.");
                            return;
                        }
                        WizLog("SHA256 verified.");
                    }
                }
                if (_cancel) { Cancelled(); return; }

                // ---- Step 3: starter files ----
                WizStep("Step 3: writing starter files");
                string eulaPath = Path.Combine(_dir, "eula.txt");
                if (_eulaAgreed)
                {
                    File.WriteAllText(eulaPath,
                        "#By changing the setting below to TRUE you are indicating your agreement to our EULA (https://aka.ms/MinecraftEULA).\r\n" +
                        "#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" +
                        "eula=true\r\n", Encoding.UTF8);
                    WizLog("Wrote eula.txt (eula=true, as you agreed).");
                }
                else
                {
                    WizLog("EULA not agreed: eula.txt was not written. On first start the server will generate eula=false and exit; set it to eula=true to start.");
                }
                string propsPath = Path.Combine(_dir, "server.properties");
                if (!File.Exists(propsPath))
                {
                    File.WriteAllText(propsPath,
                        "#Minecraft server properties\r\n" +
                        "#generated by MCLauncherApp first-run wizard\r\n" +
                        "server-port=25565\r\n" +
                        "online-mode=false\r\n", Encoding.UTF8);
                    WizLog("Wrote server.properties: server-port=25565, online-mode=false (required for offline multiplayer).");
                }
                else
                {
                    WizLog("A server.properties already exists and was left untouched. Make sure online-mode=false yourself, or offline accounts cannot join.");
                }

                // ---- Step 4: save config ----
                WizStep("Step 4: saving settings");
                _cfg.ServerDir = _dir;
                _cfg.JarName = "server.jar";
                if (javaExe != null) _cfg.JavaPath = javaExe;
                _cfg.Save();
                InstalledOk = true;
                WizLog("Done! Server folder: " + _dir + "; jar: server.jar; Java: " + (javaExe ?? "(previous setting kept)"));
                WizLog("Close the wizard, fill in the SakuraFrp access key and tunnel IDs in Settings, then click the one-click start button.");
                WizStep("Install complete");
                SetRunningUi(false);
            }
            catch (Exception ex)
            {
                Fail("Wizard error: " + ex.Message);
            }
        }

        string DownloadWithProgress(string url, string destPath, string label)
        {
            _wc = DlUtil.NewClient();
            try
            {
                int lastPct = -2;
                string err = DlUtil.DownloadFile(_wc, url, destPath,
                    pct =>
                    {
                        if (pct != lastPct)
                        {
                            lastPct = pct;
                            WizProgress(pct);
                            if (pct >= 0) WizStep("Downloading " + label + "... " + pct + "%");
                        }
                    },
                    () => _cancel);
                WizProgress(100);
                return err;
            }
            finally { try { _wc.Dispose(); } catch { } _wc = null; }
        }

        void Fail(string msg)
        {
            WizLog("Failed: " + msg);
            WizStep("Stopped");
            SetRunningUi(false);
        }

        void Cancelled()
        {
            WizLog("Cancelled. Settings were not saved; fully downloaded files stay in the target folder, partial files were cleaned up.");
            WizStep("Cancelled");
            SetRunningUi(false);
        }
    }

    class MainForm : Form
    {
        private Config _cfg;
        private StatusCard statusCard;
        private RoundButton btnStart, btnPcl2, btnStop, btnSettings, btnCopy;
        private HintTextBox tbAddr;
        private NotifyIcon _tray;
        private SegControl segLog;
        private LogTextBox tbServerLog, tbTunnelLog;
        private readonly AnimFloat _logFade = new AnimFloat(1);
        private TextBox tbCmd;
        private RoundButton btnCmdSend;
        // v6.6: slim overlay scrollbars + jump-to-bottom chip + command history
        private SlimScrollBar sbSrvV, sbSrvH, sbTunV, sbTunH;
        private RoundButton btnJumpBottom;
        // v6.11: log search row + error filter (server log only)
        private Panel searchRow;
        private HintTextBox tbSearch;
        private ToolTip _tips;
        private TextLabel lblFind;
        private DarkCheckBox chkErrOnly;
        private readonly System.Collections.Generic.List<string> _srvLines = new System.Collections.Generic.List<string>();
        private readonly object _logLock613 = new object(); // v6.13: on-disk log mirror
        private bool _timedDeferred614 = false; // v6.14: timed-command deferral notice shown
        private bool _srvFilterErr = false;
        private System.Windows.Forms.Timer _logSyncTimer;
        private int _maxCharsSrv = 0, _maxCharsTun = 0;
        private readonly System.Collections.Generic.List<string> _cmdHist = new System.Collections.Generic.List<string>();
        private int _histIdx = -1;
        private string _histDraft = "";
        // v6.7: stats tab sampling state
        private StatsPanel statsPanel;
        private System.Windows.Forms.Timer _statsTimer;
        private long _lastNetRx = 0, _lastNetTx = 0;
        private DateTime _lastNetT = DateTime.MinValue;
        private TimeSpan _lastSrvCpu = TimeSpan.Zero; private DateTime _lastSrvCpuT = DateTime.MinValue;
        private TimeSpan _lastTunCpu = TimeSpan.Zero; private DateTime _lastTunCpuT = DateTime.MinValue;
        private string _xmxKey = null; private int _xmxMb = -1;
        // v6.9: online player names (server log join/leave) + memory history
        private readonly System.Collections.Generic.List<string> _onlineNames = new System.Collections.Generic.List<string>();
        private readonly System.Collections.Generic.List<double> _memHist = new System.Collections.Generic.List<double>();
        private SlpResult _slp;
        private volatile bool _stoppingIntentional = false;
        private volatile bool _stopping = false;
        private readonly System.Collections.Generic.List<DateTime> _crashTimes = new System.Collections.Generic.List<DateTime>();
        private DateTime _slpAt = DateTime.MinValue;
        private volatile bool _slpBusy = false;
        private System.Windows.Forms.Timer _copyTimer;
        private CardPanel logPanel;
        private InsetPanel logInset, cmdInset;
        private TextLabel logToggleLbl, cmdLbl;
        private int _goodW = 0, _goodH = 0;
        private float _uiScale = 1f;
        private bool logCollapsed = false;
        // v6.3 animation engine: 8ms timer paced by timeBeginPeriod(1), Stopwatch time-based easing
        private bool _animActive = false;
        private bool _animExpanding = false;
        private double _animFromH = 0, _animToH = 0, _animFadeFrom = 1.0;
        private int _animDurMs = 0;
        private int _animLastH = -1;
        private double _animLastFade = -1;
        private readonly System.Diagnostics.Stopwatch _animClock = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer _animTimer;
        private bool _sysClientAnim = true;
        private readonly AnimFloat _lblAmt = new AnimFloat(0);

        // 进程
        private Process _server;
        // v6.11: backup tab controls + schedule timer
        private InsetPanel backupPanel;
        private RoundButton btnBackupNow, btnRestoreBackup, btnDeleteBackup;
        private ListBox backupList;
        private TextLabel lblBackupState, lblBackupInfo;
        private TextBox tbKeep, tbDailyTime, tbTimedTime, tbTimedCmd;
        private DarkCheckBox chkAutoRestart, chkBackupOnStart, chkDailyBackup, chkTimedCmd;
        private readonly System.Collections.Generic.List<string> _backupFiles = new System.Collections.Generic.List<string>();
        private bool _bkInit = false;
        private System.Windows.Forms.Timer _schedTimer;
        private Process _frpc;
        private Process _frpcVoice;
        private volatile bool _voiceUp = false;
        private volatile bool _serverDone = false;
        private volatile bool _tunnelUp = false;
        private volatile bool _frpcBundled = false; // 检测到 SakuraFrp 启动器套装版 frpc（不能直接运行）
        private volatile bool _starting = false;

        public MainForm()
        {
            _cfg = Config.Load();
            BuildUi();
            _uiScale = DpiScale.Measure(this);
            DpiScale.ScaleForm(this, 560, 800);
            RefreshStatus();
            // v6.13: tray icon, log-mirror housekeeping, remembered tab
            InitTray613();
            CleanupOldLogs613();
            if (_cfg.LastTab > 0 && segLog != null) segLog.Restore613(_cfg.LastTab);
            AppendServerLog("[Launcher] Started. Logs are also saved in the logs folder next to the program (kept for 14 days).");
        }

        // v6.13: tray icon - tooltip mirrors server state; double-click or
        // the menu restores the window; the exit item goes through the
        // normal close path (OnClosing asks + safe-stops first).
        void InitTray613()
        {
            try
            {
                _tray = new NotifyIcon();
                try
                {
                    string ico2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico");
                    if (File.Exists(ico2)) _tray.Icon = new Icon(ico2);
                    else if (Icon != null) _tray.Icon = Icon;
                }
                catch { }
                if (_tray.Icon == null) _tray.Icon = SystemIcons.Application;
                _tray.Text = "Minecraft Multiplayer Helper · Stopped";
                var menu = new ContextMenuStrip();
                var miShow = new ToolStripMenuItem("Show main window");
                miShow.Click += (s2, e2) => { try { Show(); WindowState = FormWindowState.Normal; Activate(); } catch { } };
                var miExit = new ToolStripMenuItem("Safe exit");
                miExit.Click += (s2, e2) => { try { Close(); } catch { } };
                menu.Items.Add(miShow);
                menu.Items.Add(miExit);
                _tray.ContextMenuStrip = menu;
                _tray.DoubleClick += (s2, e2) => { try { Show(); WindowState = FormWindowState.Normal; Activate(); } catch { } };
                _tray.Visible = true;
                FormClosed += (s2, e2) => { try { if (_tray != null) { _tray.Visible = false; _tray.Dispose(); } } catch { } };
            }
            catch { }
        }

        // ---------- UI（v5 美化） ----------
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, [System.Runtime.InteropServices.In] ref Rectangle lprcUpdate, IntPtr hrgnUpdate, uint flags);
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        static extern uint timeBeginPeriod(uint uPeriod);
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        static extern uint timeEndPeriod(uint uPeriod);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SystemParametersInfo(uint action, uint param, ref bool value, uint winini);
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        void DragMove()
        {
            try { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } catch { }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var br = new SolidBrush(Pal.Bg)) e.Graphics.FillRectangle(br, e.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var pen = new Pen(Pal.Border, 1f)) e.Graphics.DrawRectangle(pen, rect);
        }

        void OnFormLoad()
        {
            TryDwmRound();
            try
            {
                if (_cfg.WinX >= 0 && _cfg.WinY >= 0)
                {
                    var probe = new Rectangle(_cfg.WinX, _cfg.WinY, 80, 80);
                    bool onScreen = false;
                    foreach (var sc in Screen.AllScreens) { if (sc.WorkingArea.IntersectsWith(probe)) { onScreen = true; break; } }
                    if (onScreen) { StartPosition = FormStartPosition.Manual; Location = new Point(_cfg.WinX, _cfg.WinY); }
                }
            }
            catch { }
            try
            {
                _goodW = Size.Width; _goodH = Size.Height;
                _uiScale = ClientSize.Width / 560f;
                if (_uiScale <= 0) _uiScale = 1f;
                MinimumSize = new Size(ClientSize.Width, (int)(494 * _uiScale));
            }
            catch { }
            // v6.6: command history + a slow sync for the overlay scrollbars
            LoadCmdHistory();
            try
            {
                _logSyncTimer = new System.Windows.Forms.Timer();
                _logSyncTimer.Interval = 250;
                _logSyncTimer.Tick += (s, e) => { if (logInset != null && logInset.Visible) SyncLogBars(); };
                _logSyncTimer.Start();
            }
            catch { }
            // v6.7: stats sampling at 1 Hz, only while the tab is showing
            try
            {
                _statsTimer = new System.Windows.Forms.Timer();
                _statsTimer.Interval = 1000;
                _statsTimer.Tick += (s, e) => { if (statsPanel != null && statsPanel.Visible) SampleStats(); };
                _statsTimer.Start();
            }
            catch { }
            // v6.11: schedule checks (daily backup / timed command)
            try
            {
                _schedTimer = new System.Windows.Forms.Timer();
                _schedTimer.Interval = 20000;
                _schedTimer.Tick += (s, e) => CheckSchedules();
                _schedTimer.Start();
            }
            catch { }
        }

        void TryDwmRound()
        {
            try { int v = 2; DwmSetWindowAttribute(Handle, 33, ref v, 4); } catch { }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            try { Invalidate(true); } catch { }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            try { Invalidate(true); } catch { }
        }

        // Invisible frame: keep the standard frame styles so DWM treats this
        // as a normal app window (and animates minimize/restore), while
        // WM_NCCALCSIZE zeroes the frame so nothing visible changes.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00C00000 | 0x00040000 | 0x00020000 | 0x00080000; // WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_SYSMENU
                // v6.19: WS_EX_COMPOSITED - present the whole client area
                // atomically from an off-screen buffer. The slowed-tween
                // localization (2026-11-05) showed mid-expand frames with
                // strips of unpainted background: the resize was presented
                // before the panel and the native log TextBoxes painted
                // the newly exposed strip. Compositing makes a
                // half-painted frame undisplayable instead of racing it.
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                // v6.19: CS_VREDRAW | CS_HREDRAW - invalidate the whole
                // client area on every resize step so no strip is left
                // waiting for a late paint (live class style measured
                // 0x00000008 before this change: neither style was set).
                cp.ClassStyle |= 0x0001 | 0x0002; // CS_VREDRAW | CS_HREDRAW
                // v6.5: CS_VREDRAW/CS_HREDRAW removed - redrawing the whole
                // client area on every height change made the native log
                // TextBoxes strobe during the log height tween. Resize now
                // bit-copies the static content and paints only the newly
                // exposed strip; FinishLogAnim still lands with a full repaint.
                return cp;
            }
        }

        protected override void SetClientSizeCore(int x, int y)
        {
            if (IsHandleCreated)
            {
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, x, y, 0x0002 | 0x0004 | 0x0010); // NOMOVE | NOZORDER | NOACTIVATE
                return;
            }
            base.SetClientSizeCore(x, y);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0083) // WM_NCCALCSIZE: client area = whole window rect
            {
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == 0x0086) // WM_NCACTIVATE: suppress frame flash on activation
            {
                m.LParam = (IntPtr)(-1);
            }
            if (m.Msg == 0x001A) // WM_SETTINGCHANGE: user may have toggled system animations
            {
                try { bool b = true; if (SystemParametersInfo(0x1042, 0, ref b, 0)) _sysClientAnim = b; } catch { } UiAnim.Enabled = _sysClientAnim;
            }
            if (m.Msg == 0x0005 && m.WParam == (IntPtr)1 && _animActive) // minimized mid-animation: land on the target
            {
                FinishLogAnim(_animToH, _animExpanding);
            }
            base.WndProc(ref m);
            if (m.Msg == 0x0005 || m.Msg == 0x0047) // WM_SIZE / WM_WINDOWPOSCHANGED: heal any phantom frame-inflation after the burst settles
            {
                try { BeginInvoke(new Action(HealSize)); } catch { }
            }
        }

        // Restoring from minimized makes the framework re-apply the client size
        // inflated by frame metrics this form does not actually have (NCCALCSIZE
        // zeroes them). Snap back to the last known-good window size.
        void HealSize()
        {
            if (_goodW <= 0) return;
            if (WindowState != FormWindowState.Normal) return;
            if (_animActive) return;
            if (Size.Width == _goodW && Size.Height == _goodH) return;
            try { SetWindowPos(Handle, IntPtr.Zero, 0, 0, _goodW, _goodH, 0x0002 | 0x0004 | 0x0010); } catch { }
        }

        void BuildUi()
        {
            Text = "Minecraft Multiplayer Helper v6.20";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Pal.Bg;
            ForeColor = Pal.Text;
            Font = new Font("Microsoft YaHei UI", 9f);
            Size = new Size(560, 800);
            MinimumSize = new Size(560, 494);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            DoubleBuffered = true;
            try { if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico"))) Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets\\icon.ico")); } catch { }
            Load += (s, e) => { OnFormLoad(); };

            // ---- 自定义标题栏 ----
            var titleBar = new TitleBarPanel();
            titleBar.SetBounds(0, 0, 560, 40);
            titleBar.DragRequested += (s, e) => { DragMove(); };
            titleBar.CloseClicked += (s, e) => { Close(); };
            titleBar.MinClicked += (s, e) => { WindowState = FormWindowState.Minimized; };
            Controls.Add(titleBar);

            // ---- 状态卡片 ----
            statusCard = new StatusCard();
            statusCard.SetBounds(20, 56, 520, 148);
            Controls.Add(statusCard);

            // ---- 主按钮 ----
            btnStart = new RoundButton();
            btnStart.Primary = true;
            btnStart.Radius = 8;
            btnStart.Text = "Start Multiplayer";
            btnStart.SetBounds(20, 220, 520, 52);
            btnStart.BackColor = Pal.Green;
            btnStart.ForeColor = Color.White;
            btnStart.Font = new Font("Microsoft YaHei UI", 12f, FontStyle.Bold);
            btnStart.Click += (s, e) => StartAll();
            Controls.Add(btnStart);

            // ---- 公网地址 ----
            var addrLbl = new TextLabel();
            addrLbl.BackdropColor = Pal.Bg;
            addrLbl.TopAlign = true;
            addrLbl.Caption = "Public address (send to friends): ";
            addrLbl.TextShiftX = 1.33f; addrLbl.TextShiftY = -0.67f;
            addrLbl.SetBounds(20, 290, 220, 18);
            addrLbl.BackColor = Color.Transparent;
            addrLbl.ForeColor = Pal.Sub;
            addrLbl.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            Controls.Add(addrLbl);

            var addrInset = new InsetPanel();
            addrInset.SetBounds(20, 314, 392, 38);
            Controls.Add(addrInset);

            tbAddr = new HintTextBox();
            tbAddr.Hint = "The public address will appear here once the tunnel starts";
            tbAddr.SetBounds(12, 10, 368, 22);
            tbAddr.BackColor = Pal.Inset;
            tbAddr.ForeColor = Pal.AddrGreen;
            tbAddr.BorderStyle = BorderStyle.None;
            tbAddr.Font = Fonts.Mono(11f, FontStyle.Bold);
            tbAddr.ReadOnly = true;
            tbAddr.TabStop = false;
            tbAddr.DoubleClick += (s, e) => { try { tbAddr.SelectAll(); } catch { } }; // v6.6: double-click selects the whole address
            addrInset.Controls.Add(tbAddr);

            btnCopy = new RoundButton();
            btnCopy.Text = "Copy address for friends";
            btnCopy.SetBounds(420, 314, 120, 38);
            btnCopy.BackColor = Pal.Surface;
            btnCopy.ForeColor = Color.White;
            btnCopy.Font = new Font("Microsoft YaHei UI", 9f);
            btnCopy.Click += (s, e) => CopyAddr();
            Controls.Add(btnCopy);

            // ---- 次要按钮（等宽三列） ----
            btnPcl2 = new RoundButton();
            btnPcl2.Text = "Open PCL2";
            btnPcl2.SetBounds(20, 368, 168, 38);
            btnPcl2.BackColor = Pal.Surface;
            btnPcl2.ForeColor = Color.White;
            btnPcl2.Click += (s, e) => OpenPcl2();
            Controls.Add(btnPcl2);

            btnStop = new RoundButton();
            btnStop.DangerHover = true;
            btnStop.Text = "Stop server safely";
            btnStop.SetBounds(196, 368, 168, 38);
            btnStop.BackColor = Pal.Surface;
            btnStop.ForeColor = Color.White;
            btnStop.Click += (s, e) =>
            {
                if (_stopping) return;
                _stopping = true;
                Thread tStop = new Thread(() => { try { SafeStop(); } finally { _stopping = false; } });
                tStop.IsBackground = true;
                tStop.Start();
            };
            Controls.Add(btnStop);

            btnSettings = new RoundButton();
            btnSettings.Text = "Settings";
            btnSettings.SetBounds(372, 368, 168, 38);
            btnSettings.BackColor = Pal.Surface;
            btnSettings.ForeColor = Color.White;
            btnSettings.Click += (s, e) => OpenSettings();
            Controls.Add(btnSettings);

            // ---- 日志卡片 ----
            logPanel = new CardPanel();
            logPanel.SetBounds(20, 422, 520, 358);
            Controls.Add(logPanel);

            logToggleLbl = new TextLabel();
            logToggleLbl.Caption = "▾  Log";
            logToggleLbl.TextShiftX = -0.67f;
            logToggleLbl.SetBounds(16, 10, 120, 32);
            logToggleLbl.BackColor = Pal.Card;
            logToggleLbl.ForeColor = Pal.Sub;
            logToggleLbl.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            logToggleLbl.Cursor = Cursors.Hand;
            logToggleLbl.Click += (s, e) => ToggleLog();
            logPanel.Controls.Add(logToggleLbl);
            _lblAmt.Changed = () => { if (!IsDisposed && logToggleLbl != null) logToggleLbl.ForeColor = Gfx.Mix(Pal.Sub, Pal.Text, _lblAmt.Value); };
            logToggleLbl.MouseEnter += (s, e) => _lblAmt.Set(1, 120);
            logToggleLbl.MouseLeave += (s, e) => _lblAmt.Set(0, 80);

            segLog = new SegControl();
            segLog.SetBounds(216, 10, 288, 32);
            segLog.Font = new Font("Microsoft YaHei UI", 9f);
            segLog.Items = new string[] { "Server Log", "Tunnel Log", "Status", "Backups" };
            segLog.Changed += (s, e) => ShowLogPage();
            logPanel.Controls.Add(segLog);

            logInset = new InsetPanel();
            logInset.SetBounds(16, 86, 488, 208);
            logInset.BackdropColor = Pal.Card;
            logPanel.Controls.Add(logInset);

            tbServerLog = MakeLogBox("No logs yet · Server output will appear here");
            tbServerLog.SetBounds(8, 6, 472, 196);
            logInset.Controls.Add(tbServerLog);

            tbTunnelLog = MakeLogBox("No logs yet · Tunnel output will appear here");
            tbTunnelLog.SetBounds(8, 6, 472, 196);
            tbTunnelLog.Visible = false;
            logInset.Controls.Add(tbTunnelLog);

            // v6.6: slim overlay scrollbars (vertical overlays the box's right
            // edge; horizontal along its bottom) + jump-to-bottom chip
            sbSrvV = new SlimScrollBar(); sbSrvV.Target = tbServerLog; sbSrvV.SetBounds(472, 6, 8, 188);
            sbSrvH = new SlimScrollBar(); sbSrvH.Horizontal = true; sbSrvH.Target = tbServerLog; sbSrvH.MaxChars = () => _maxCharsSrv; sbSrvH.SetBounds(8, 194, 464, 8);
            sbTunV = new SlimScrollBar(); sbTunV.Target = tbTunnelLog; sbTunV.SetBounds(472, 6, 8, 188);
            sbTunH = new SlimScrollBar(); sbTunH.Horizontal = true; sbTunH.Target = tbTunnelLog; sbTunH.MaxChars = () => _maxCharsTun; sbTunH.SetBounds(8, 194, 464, 8);
            logInset.Controls.Add(sbSrvV); logInset.Controls.Add(sbSrvH);
            logInset.Controls.Add(sbTunV); logInset.Controls.Add(sbTunH);
            btnJumpBottom = new RoundButton();
            btnJumpBottom.Text = "↓ Back to bottom";
            btnJumpBottom.BackColor = Pal.Surface;
            btnJumpBottom.ForeColor = Pal.Text;
            btnJumpBottom.BackdropColor = Pal.Inset;
            btnJumpBottom.Radius = 8;
            btnJumpBottom.SetBounds(378, 162, 96, 26);
            btnJumpBottom.Visible = false;
            btnJumpBottom.Click += (s, e) => JumpToBottom();
            logInset.Controls.Add(btnJumpBottom);
            // v6.11: search row above the server log (page 0 only)
            searchRow = new Panel();
            searchRow.SetBounds(16, 52, 488, 28);
            searchRow.BackColor = Pal.Card;
            logPanel.Controls.Add(searchRow);
            tbSearch = new HintTextBox();
            tbSearch.Hint = "Search logs…";
            tbSearch.SetBounds(0, 2, 218, 24);
            tbSearch.BackColor = Color.FromArgb(17, 20, 24);
            tbSearch.ForeColor = Pal.Text;
            tbSearch.BorderStyle = BorderStyle.None;
            tbSearch.Font = new Font("Microsoft YaHei UI", 9f);
            tbSearch.KeyDown += (s2, e2) => { if (e2.KeyCode == Keys.Enter) { e2.SuppressKeyPress = true; FindInLog(!e2.Shift); } };
            tbSearch.TextChanged += (s2, e2) => { if (lblFind != null) lblFind.Caption = ""; };
            searchRow.Controls.Add(tbSearch);
            var btnPrev = new RoundButton();
            btnPrev.Text = "↑";
            btnPrev.SetBounds(224, 0, 44, 28);
            btnPrev.BackColor = Pal.Surface; btnPrev.ForeColor = Pal.Text; btnPrev.BackdropColor = Pal.Card;
            btnPrev.Font = new Font("Microsoft YaHei UI", 9f);
            btnPrev.Click += (s2, e2) => FindInLog(false);
            searchRow.Controls.Add(btnPrev);
            var btnNext = new RoundButton();
            btnNext.Text = "↓";
            btnNext.SetBounds(272, 0, 44, 28);
            btnNext.BackColor = Pal.Surface; btnNext.ForeColor = Pal.Text; btnNext.BackdropColor = Pal.Card;
            btnNext.Font = new Font("Microsoft YaHei UI", 9f);
            btnNext.Click += (s2, e2) => FindInLog(true);
            searchRow.Controls.Add(btnNext);
            _tips = new ToolTip();
            _tips.SetToolTip(btnPrev, "Previous");
            _tips.SetToolTip(btnNext, "Next");
            _tips.SetToolTip(tbSearch, "Search the log; press Enter to jump to the next match");
            lblFind = new TextLabel();
            lblFind.Caption = "";
            lblFind.SetBounds(322, 2, 60, 24);
            lblFind.ForeColor = Pal.Sub; lblFind.BackColor = Pal.Card;
            lblFind.Font = new Font("Microsoft YaHei UI", 9f);
            searchRow.Controls.Add(lblFind);
            chkErrOnly = new DarkCheckBox();
            chkErrOnly.Text = "Warnings/errors only";
            chkErrOnly.SetBounds(386, 2, 102, 24);
            chkErrOnly.ForeColor = Pal.Text; chkErrOnly.BackColor = Pal.Card;
            chkErrOnly.Font = new Font("Microsoft YaHei UI", 8f);
            chkErrOnly.CheckedChanged += (s2, e2) => SetSrvFilter(chkErrOnly.Checked);
            searchRow.Controls.Add(chkErrOnly);
            logInset.Controls.SetChildIndex(sbSrvV, 0);
            logInset.Controls.SetChildIndex(sbSrvH, 0);
            logInset.Controls.SetChildIndex(sbTunV, 0);
            logInset.Controls.SetChildIndex(sbTunH, 0);
            logInset.Controls.SetChildIndex(btnJumpBottom, 0);

            // v6.7: stats tab page, same footprint as log inset + command row
            statsPanel = new StatsPanel();
            statsPanel.ManageClicked += (s, e) => OpenPlayerManager();
            statsPanel.SetBounds(16, 52, 488, 290);
            statsPanel.Visible = false;
            logPanel.Controls.Add(statsPanel);

            // v6.11: backup tab (world backups + schedules + watchdog toggle)
            backupPanel = new InsetPanel();
            backupPanel.SetBounds(16, 52, 488, 290);
            backupPanel.BackdropColor = Pal.Card;
            backupPanel.Visible = false;
            logPanel.Controls.Add(backupPanel);
            BuildBackupPanel();

            cmdLbl = new TextLabel();
            cmdLbl.TopAlign = true;
            cmdLbl.Caption = "Server command";
            cmdLbl.TextShiftX = 2f; cmdLbl.TextShiftY = -0.67f;
            cmdLbl.SetBounds(16, 312, 72, 26);
            cmdLbl.BackColor = Pal.Card;
            cmdLbl.ForeColor = Pal.Sub;
            cmdLbl.Font = new Font("Microsoft YaHei UI", 9f);
            logPanel.Controls.Add(cmdLbl);

            cmdInset = new InsetPanel();
            cmdInset.FocusGlow = true;
            cmdInset.SetBounds(92, 308, 320, 34);
            cmdInset.BackdropColor = Pal.Card;
            logPanel.Controls.Add(cmdInset);

            tbCmd = new TextBox();
            tbCmd.SetBounds(10, 7, 300, 22);
            tbCmd.BackColor = Pal.Inset;
            tbCmd.ForeColor = Pal.Text;
            tbCmd.BorderStyle = BorderStyle.None;
            tbCmd.Font = Fonts.Mono(9f, FontStyle.Regular);
            tbCmd.KeyDown += CmdKeyDown;
            tbCmd.GotFocus += (s, e) => { cmdInset.Invalidate(); };
            tbCmd.LostFocus += (s, e) => { cmdInset.Invalidate(); };
            cmdInset.Controls.Add(tbCmd);

            btnCmdSend = new RoundButton();
            btnCmdSend.Text = "Send";
            btnCmdSend.SetBounds(420, 308, 84, 34);
            btnCmdSend.BackColor = Pal.Surface;
            btnCmdSend.BackdropColor = Pal.Card;
            btnCmdSend.ForeColor = Color.White;
            btnCmdSend.Click += (s, e) => SendServerCommand();
            logPanel.Controls.Add(btnCmdSend);

            try { bool b = true; if (SystemParametersInfo(0x1042, 0, ref b, 0)) _sysClientAnim = b; } catch { } UiAnim.Enabled = _sysClientAnim;
            _animTimer = new System.Windows.Forms.Timer();
            _animTimer.Interval = 8;
            _animTimer.Tick += (s, e) => AnimFrame();

            KeyPreview = true;
            Shown += (s, e) => { if (string.IsNullOrEmpty(_cfg.FrpKey)) { AppendState("First run: fill in the SakuraFrp access key and tunnel ID in Settings, and check the PCL2 path."); } };
            Shown += (s, e) => { BeginInvoke(new Action(() => { try { OfferWizardIfNeeded(); } catch { } })); };
            // v6.6 trial: one full repaint right after the first frame settles,
            // to normalize any first-paint edge rasterization (QA measures it).
            Shown += (s, e) => { BeginInvoke(new Action(() => { try { Invalidate(true); Update(); } catch { } })); };
            FormClosing += OnClosing;
        }

        // v6.11: page 0 carries the search row, so the log inset sits
        // lower and shorter there; the other log page keeps the full area.
        void LayoutLogChrome(int page)
        {
            if (logInset == null) return;
            // Runtime bounds are DEVICE pixels in this app (the build-time
            // logical values were scaled once by the DPI pass), so every
            // number here goes through the scale factor.
            float sc = _uiScale > 0 ? _uiScale : 1f;
            Func<double, int> D = v => (int)Math.Round(v * sc);
            bool srv = page == 0;
            if (srv)
            {
                logInset.SetBounds(D(16), D(86), D(488), D(208));
                tbServerLog.SetBounds(D(8), D(6), D(472), D(196));
                tbTunnelLog.SetBounds(D(8), D(6), D(472), D(196));
                sbSrvV.SetBounds(D(472), D(6), D(8), D(188)); sbSrvH.SetBounds(D(8), D(194), D(464), D(8));
                sbTunV.SetBounds(D(472), D(6), D(8), D(188)); sbTunH.SetBounds(D(8), D(194), D(464), D(8));
                if (btnJumpBottom != null) btnJumpBottom.SetBounds(D(378), D(162), D(96), D(26));
            }
            else
            {
                logInset.SetBounds(D(16), D(52), D(488), D(242));
                tbServerLog.SetBounds(D(8), D(6), D(472), D(230));
                tbTunnelLog.SetBounds(D(8), D(6), D(472), D(230));
                sbSrvV.SetBounds(D(472), D(6), D(8), D(222)); sbSrvH.SetBounds(D(8), D(228), D(464), D(8));
                sbTunV.SetBounds(D(472), D(6), D(8), D(222)); sbTunH.SetBounds(D(8), D(228), D(464), D(8));
                if (btnJumpBottom != null) btnJumpBottom.SetBounds(D(378), D(196), D(96), D(26));
            }
        }

        static bool IsErrLine(string ln)
        {
            return ln != null && (ln.Contains("/WARN") || ln.Contains("/ERROR") || ln.Contains("/FATAL") || ln.Contains("Exception") || ln.Contains("[Watchdog]") || ln.Contains("[Backup] Failed"));
        }

        void SetSrvFilter(bool on)
        {
            _srvFilterErr = on;
            try
            {
                var keep = on ? _srvLines.FindAll(IsErrLine) : _srvLines;
                tbServerLog.Text = keep.Count > 0 ? string.Join(Environment.NewLine, keep.ToArray()) + Environment.NewLine : "";
                tbServerLog.SelectionStart = tbServerLog.TextLength;
                tbServerLog.ScrollToCaret();
                SyncLogBars();
            }
            catch { }
        }

        void FindInLog(bool next)
        {
            if (tbSearch == null || tbServerLog == null) return;
            string q = tbSearch.Text;
            if (q.Length == 0 || tbServerLog.TextLength == 0) { if (lblFind != null) lblFind.Caption = ""; return; }
            string text = tbServerLog.Text;
            var hits = new System.Collections.Generic.List<int>();
            int scan = 0;
            while (scan <= text.Length - q.Length)
            {
                int h = text.IndexOf(q, scan, StringComparison.OrdinalIgnoreCase);
                if (h < 0) break;
                hits.Add(h);
                scan = h + Math.Max(1, q.Length);
            }
            if (hits.Count == 0) { lblFind.Caption = "No results"; return; }
            int idx;
            if (next)
            {
                int pos = tbServerLog.SelectionStart + tbServerLog.SelectionLength;
                idx = hits.FindIndex(h => h >= pos);
                if (idx < 0) idx = 0;
            }
            else
            {
                idx = -1;
                for (int i = hits.Count - 1; i >= 0; i--) { if (hits[i] < tbServerLog.SelectionStart) { idx = i; break; } }
                if (idx < 0) idx = hits.Count - 1;
            }
            tbServerLog.Focus();
            tbServerLog.SelectionStart = hits[idx];
            tbServerLog.SelectionLength = q.Length;
            tbServerLog.ScrollToCaret();
            lblFind.Caption = (idx + 1) + "/" + hits.Count;
        }

        void ShowLogPage()
        {
            int page = segLog == null ? 0 : segLog.SelectedIndex;
            if (_cfg != null && _cfg.LastTab != page) { _cfg.LastTab = page; try { _cfg.Save(); } catch { } } // v6.13
            bool stats = page == 2;
            bool backup = page == 3;
            if (tbServerLog != null) tbServerLog.Visible = page == 0;
            if (tbTunnelLog != null) tbTunnelLog.Visible = page == 1;
            // v6.7: the stats tab replaces the log inset AND the command row;
            // v6.11: the backup tab does the same with its own panel
            if (logInset != null) logInset.Visible = !stats && !backup;
            if (statsPanel != null) statsPanel.Visible = stats;
            if (backupPanel != null) backupPanel.Visible = backup;
            if (searchRow != null) searchRow.Visible = page == 0;
            if (cmdLbl != null) cmdLbl.Visible = !stats && !backup;
            if (cmdInset != null) cmdInset.Visible = !stats && !backup;
            if (btnCmdSend != null) btnCmdSend.Visible = !stats && !backup;
            if (backup) RefreshBackupList();
            LayoutLogChrome(page);
            // v6.5: the incoming log fades in over ~180ms instead of popping
            // (text color ramps from the inset background color to LogText);
            // the outgoing page vanishing is masked by the sliding tab thumb.
            if (_logFade.Changed == null)
            {
                _logFade.Changed = () =>
                {
                    if (IsDisposed) return;
                    var c = Gfx.Mix(Pal.Inset, Pal.LogText, _logFade.Value);
                    try { if (tbServerLog != null) tbServerLog.ForeColor = c; if (tbTunnelLog != null) tbTunnelLog.ForeColor = c; } catch { }
                };
            }
            if (!stats && _sysClientAnim && UiAnim.Enabled) { _logFade.Set(0.12, 0); _logFade.Set(1, 180); }
            if (stats) SampleStats();
            SyncLogBars();
        }

        // ---------- 状态监控采样（v6.7） ----------
        [System.Runtime.InteropServices.DllImport("iphlpapi.dll", SetLastError = true)]
        static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwSize, bool bOrder, int ulAf, int TableClass, uint Reserved);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] MEMORYSTATUSEX ms);
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        class MEMORYSTATUSEX
        {
            public uint dwLength = 64;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        static string FmtBytes(double b)
        {
            if (b < 1024) return ((long)b) + " B";
            double kb = b / 1024.0;
            if (kb < 1024) return kb.ToString("0.0") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("0.0") + " MB";
            return (mb / 1024.0).ToString("0.00") + " GB";
        }
        static string FmtDur(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            return ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
        }
        static double CpuRate(TimeSpan cpu, DateTime t, ref TimeSpan lastCpu, ref DateTime lastT)
        {
            double pct = -1;
            if (lastT != DateTime.MinValue)
            {
                double wall = (t - lastT).TotalSeconds;
                if (wall > 0.2) pct = (cpu - lastCpu).TotalSeconds / wall / Environment.ProcessorCount * 100.0;
                if (pct < 0) pct = 0;
            }
            lastCpu = cpu; lastT = t;
            return pct;
        }
        int XmxMb()
        {
            string mp = _cfg.MemParams ?? "";
            if (mp == _xmxKey) return _xmxMb;
            _xmxKey = mp; _xmxMb = -1;
            var m = Regex.Match(mp, @"-Xmx(\d+)\s*([gGmMkK]?)");
            if (m.Success)
            {
                long v = long.Parse(m.Groups[1].Value);
                string u = m.Groups[2].Value.ToLower();
                if (u == "g") _xmxMb = (int)(v * 1024);
                else if (u == "k") _xmxMb = (int)(v / 1024);
                else if (u == "m") _xmxMb = (int)v;
                else _xmxMb = (int)(v / 1048576); // bare number = bytes
            }
            return _xmxMb;
        }
        int CountConns(int pid, int port)
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                int size = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 5, 0);
                if (size <= 0) return 0;
                buf = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
                if (GetExtendedTcpTable(buf, ref size, false, 2, 5, 0) != 0) return 0;
                int n = System.Runtime.InteropServices.Marshal.ReadInt32(buf);
                int count = 0;
                long basePtr = buf.ToInt64() + 4;
                for (int i = 0; i < n; i++)
                {
                    long row = basePtr + (long)i * 24; // MIB_TCPROW_OWNER_PID
                    int state = System.Runtime.InteropServices.Marshal.ReadInt32((IntPtr)(row + 0));
                    int lportRaw = System.Runtime.InteropServices.Marshal.ReadInt32((IntPtr)(row + 8));
                    int opid = System.Runtime.InteropServices.Marshal.ReadInt32((IntPtr)(row + 20));
                    int lport = ((lportRaw & 0xFF) << 8) | ((lportRaw >> 8) & 0xFF);
                    if (opid == pid && lport == port && state == 5) count++; // ESTABLISHED
                }
                return count;
            }
            catch { return 0; }
            finally { if (buf != IntPtr.Zero) System.Runtime.InteropServices.Marshal.FreeHGlobal(buf); }
        }

        void SampleStats()
        {
            if (statsPanel == null) return;
            try
            {
                DateTime now = DateTime.Now;
                // ---- server process ----
                if (ProcAlive(_server))
                {
                    try
                    {
                        _server.Refresh();
                        statsPanel.SrvState = "Running · " + FmtDur(now - _server.StartTime);
                        statsPanel.SrvStateColor = Color.FromArgb(64, 199, 116);
                        long ws = _server.WorkingSet64;
                        statsPanel.SrvMem = FmtBytes(ws);
                        int xmx = XmxMb();
                        if (xmx > 0)
                        {
                            statsPanel.SrvMemFrac = ws / (xmx * 1048576.0);
                            statsPanel.SrvMemCap = "";
                        }
                        else { statsPanel.SrvMemFrac = -1; statsPanel.SrvMemCap = ""; }
                        double pc = CpuRate(_server.TotalProcessorTime, now, ref _lastSrvCpu, ref _lastSrvCpuT);
                        statsPanel.SrvCpu = pc < 0 ? "—" : pc.ToString("0.0") + " %";
                        statsPanel.SrvConn = CountConns(_server.Id, _cfg.Port) + " player(s)";
                        // v6.9: memory history for the sparkline + player names
                        if (statsPanel.SrvMemFrac >= 0)
                        {
                            _memHist.Add(statsPanel.SrvMemFrac);
                            while (_memHist.Count > 60) _memHist.RemoveAt(0);
                            statsPanel.SrvMemHist = _memHist.ToArray();
                        }
                        statsPanel.SrvPlayers = _onlineNames.Count > 0 ? string.Join("、", _onlineNames.ToArray()) : "—";
                        // v6.11: poll the server's own status protocol every
                        // few seconds; its answer outranks the log-derived
                        // names (and knows version / max / latency too)
                        if (!_slpBusy && (now - _slpAt).TotalSeconds >= 4)
                        {
                            _slpBusy = true; _slpAt = now;
                            int sport = _cfg.Port;
                            System.Threading.ThreadPool.QueueUserWorkItem(_q =>
                            {
                                var r = Slp.Query("127.0.0.1", sport, 1500);
                                _slp = r; _slpBusy = false;
                            });
                        }
                        var slpNow = _slp;
                        if (slpNow != null && slpNow.Ok)
                        {
                            if (slpNow.Names.Count > 0) statsPanel.SrvPlayers = string.Join("、", slpNow.Names.ToArray());
                            else if (slpNow.Online > 0) statsPanel.SrvPlayers = slpNow.Online + " player(s) online (list not public)";
                            string vp = slpNow.Version ?? "";
                            if (slpNow.Max > 0) vp += (vp.Length > 0 ? " · " : "") + slpNow.Online + "/" + slpNow.Max + " player(s)";
                            if (slpNow.PingMs >= 0) vp += (vp.Length > 0 ? " · " : "") + "Ping " + slpNow.PingMs + " ms";
                            if (vp.Length > 0) statsPanel.SrvVerPing = vp;
                        }
                    }
                    catch { }
                }
                else
                {
                    statsPanel.SrvState = "Not running"; statsPanel.SrvStateColor = Pal.Mute;
                    statsPanel.SrvMem = "—"; statsPanel.SrvMemFrac = -1; statsPanel.SrvMemCap = "";
                    statsPanel.SrvCpu = "—"; statsPanel.SrvConn = "—";
                    statsPanel.SrvPlayers = "—"; statsPanel.SrvVerPing = "—"; _slp = null;
                    if (_memHist.Count > 0) { _memHist.Clear(); statsPanel.SrvMemHist = new double[0]; }
                    if (_onlineNames.Count > 0) _onlineNames.Clear();
                    _lastSrvCpuT = DateTime.MinValue;
                }
                // ---- tunnel processes ----
                long tws = 0; TimeSpan tcpu = TimeSpan.Zero; bool anyTun = false;
                foreach (var p in new Process[] { _frpc, _frpcVoice })
                {
                    if (ProcAlive(p)) { try { p.Refresh(); tws += p.WorkingSet64; tcpu += p.TotalProcessorTime; anyTun = true; } catch { } }
                }
                if (anyTun)
                {
                    statsPanel.TunMem = FmtBytes(tws);
                    double pc = CpuRate(tcpu, now, ref _lastTunCpu, ref _lastTunCpuT);
                    statsPanel.TunCpu = pc < 0 ? "—" : pc.ToString("0.0") + " %";
                }
                else { statsPanel.TunMem = "—"; statsPanel.TunCpu = "—"; _lastTunCpuT = DateTime.MinValue; }
                // ---- whole-machine throughput (per-process bytes are not
                // exposed by the framework; this is the honest machine total)
                long rx = 0, tx = 0;
                try
                {
                    foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                        if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
                        try { var s = ni.GetIPv4Statistics(); rx += s.BytesReceived; tx += s.BytesSent; } catch { }
                    }
                }
                catch { }
                if (_lastNetT != DateTime.MinValue)
                {
                    double dt = (now - _lastNetT).TotalSeconds;
                    if (dt > 0.2)
                    {
                        statsPanel.NetDown = FmtBytes((rx - _lastNetRx) / dt) + "/s";
                        statsPanel.NetUp = FmtBytes((tx - _lastNetTx) / dt) + "/s";
                    }
                }
                _lastNetRx = rx; _lastNetTx = tx; _lastNetT = now;
                // ---- system memory ----
                try
                {
                    var ms = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(ms))
                    {
                        double tot = ms.ullTotalPhys, used = tot - ms.ullAvailPhys;
                        statsPanel.SysMem = FmtBytes(used) + " / " + FmtBytes(tot) + " · " + ms.dwMemoryLoad + " %";
                    }
                }
                catch { }
                statsPanel.Invalidate();
            }
            catch { }
        }

        // ---------- v6.3 animation engine ----------
        // Time-based tween on an 8ms pacing timer (timeBeginPeriod(1) makes it real):
        // position is a pure function of wall-clock time, so a late frame jumps
        // to the correct place instead of drifting or crawling at the tail.
        static double CubicBezier(double x1, double y1, double x2, double y2, double x)
        {
            double t = x;
            for (int i = 0; i < 8; i++)
            {
                double u = 1 - t;
                double cx = 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t - x;
                double dx = 3 * u * u * x1 + 6 * u * t * (x2 - x1) + 3 * t * t * (1 - x2);
                if (Math.Abs(cx) < 1e-6 || Math.Abs(dx) < 1e-6) break;
                t -= cx / dx;
                if (t < 0) t = 0; if (t > 1) t = 1;
            }
            double v = 1 - t;
            return 3 * v * v * t * y1 + 3 * v * t * t * y2 + t * t * t;
        }

        void StartLogAnim(bool expanding)
        {
            double toH = (expanding ? 800 : 494) * _uiScale;
            double fromH = Height;
            double full = Math.Abs(800 - 494) * _uiScale;
            double frac = full > 0 ? Math.Abs(toH - fromH) / full : 0;
            if (!_sysClientAnim || frac < 0.01) { FinishLogAnim(toH, expanding); return; }
            _animDurMs = Math.Max(60, (int)((expanding ? 300 : 200) * frac));
            _animFromH = fromH;
            _animToH = toH;
            _animExpanding = expanding;
            _animFadeFrom = segLog.Fade;
            _animLastH = -1;
            _animLastFade = -1;
            _animActive = true;
            SuspendLayout();
            try { timeBeginPeriod(1); } catch { }
            _animClock.Restart();
            _animTimer.Start();
            AnimFrame();
        }

        void AnimFrame()
        {
            if (!_animActive) return;
            double t = _animClock.Elapsed.TotalMilliseconds;
            double p = _animDurMs > 0 ? t / _animDurMs : 1;
            if (p >= 1) { FinishLogAnim(_animToH, _animExpanding); return; }
            double e = _animExpanding ? CubicBezier(0.2, 0, 0, 1, p) : CubicBezier(0.4, 0, 1, 1, p);
            double hd = _animFromH + (_animToH - _animFromH) * e;
            if (p > 0.8 && Math.Abs(hd - _animToH) <= 2.0) { FinishLogAnim(_animToH, _animExpanding); return; }
            int h = (int)Math.Round(hd);
            // Segmented tabs fade with the panel: out over the first 60% of a
            // collapse, in over 20%-80% of an expand; seeded from the current
            // fade so a mid-flight reversal never jumps.
            double fp = _animExpanding ? (p - 0.2) / 0.6 : p / 0.6;
            if (fp < 0) fp = 0; if (fp > 1) fp = 1;
            double f = _animExpanding ? _animFadeFrom + (1.0 - _animFadeFrom) * fp
                                      : _animFadeFrom * (1.0 - fp);
            if (Math.Abs(f - _animLastFade) > 0.004)
            {
                _animLastFade = f;
                segLog.Fade = f;
                if (!_animExpanding && f <= 0.004 && segLog.Visible) segLog.Visible = false;
            }
            if (h != _animLastH)
            {
                _animLastH = h;
                ApplyAnimHeight(h);
            }
        }

        void ApplyAnimHeight(int h) { ApplyAnimHeight(h, false); }

        void ApplyAnimHeight(int h, bool landing)
        {
            int oldPanelH = logPanel.Height;
            logPanel.Height = h - (int)(442 * _uiScale);
            // v6.14: mid-flight stripe fix - when the panel grows, force
            // the newly uncovered strip (and the child controls inside it)
            // to repaint in this frame instead of trusting the bit-copy.
            // v6.17: the strip repaint moved to after the form SetWindowPos
            // below (see the RedrawWindow call there).
            // v6.5 flicker fix: animation frames no longer force a full
            // repaint (no SWP_NOCOPYBITS, no Invalidate(true)) - the native
            // log TextBoxes were erasing and redrawing their whole text on
            // every frame, which reads as strobe flicker while the panel
            // opens/closes. Plain bit-copy resize leaves the static content
            // untouched and only the newly exposed strip paints. The landing
            // frame still gets one full repaint so the settled image is exact.
            try { SetWindowPos(Handle, IntPtr.Zero, 0, 0, Width, h, 0x0002 | 0x0004 | 0x0010); } catch { }
            if (!landing && logPanel.Height > oldPanelH && logPanel.Visible)
            {
                // v6.17: panel.Update() (the v6.15 approach) repaints only the
                // panel itself; the child windows inside the newly exposed
                // strip (inset panel, native log TextBox, scrollbars) still
                // painted a frame late - the stripe on expand. RedrawWindow
                // with RDW_INVALIDATE | RDW_ALLCHILDREN | RDW_UPDATENOW makes
                // every child in the strip paint synchronously in this frame,
                // and running it after SetWindowPos means the clip geometry
                // is already final when they paint.
                try
                {
                    var strip = new Rectangle(0, oldPanelH, logPanel.Width, logPanel.Height - oldPanelH);
                    RedrawWindow(logPanel.Handle, ref strip, IntPtr.Zero, 0x0001 | 0x0080 | 0x0100);
                }
                catch { }
            }
            if (landing) { Invalidate(true); Update(); }
        }

        void FinishLogAnim(double toH, bool expanding)
        {
            bool wasActive = _animActive;
            _animActive = false;
            if (wasActive)
            {
                try { timeEndPeriod(1); } catch { }
                _animClock.Stop();
                _animTimer.Stop();
                ResumeLayout(false);
            }
            int h = (int)Math.Round(toH);
            ApplyAnimHeight(h, true);
            _goodW = Width; _goodH = h;
            segLog.Fade = expanding ? 1.0 : 0.0;
            segLog.Visible = expanding;
            segLog.Enabled = expanding;
            // v6.7: the stats tab swaps the log chrome for the stats panel -
            // restore whichever the current tab uses, not always the log set
            bool statsLand = segLog.SelectedIndex == 2;
            bool backupLand = segLog.SelectedIndex == 3;
            logInset.Visible = expanding && !statsLand && !backupLand;
            cmdLbl.Visible = expanding && !statsLand && !backupLand;
            cmdInset.Visible = expanding && !statsLand && !backupLand;
            btnCmdSend.Visible = expanding && !statsLand && !backupLand;
            if (statsPanel != null) statsPanel.Visible = expanding && statsLand;
            if (backupPanel != null) backupPanel.Visible = expanding && backupLand;
            if (searchRow != null) searchRow.Visible = expanding && segLog.SelectedIndex == 0;
            // v6.14: landing hardening (stripe reported 2026-10-28 on the
            // live session; five probe variants could not reproduce it, so
            // this is defense-in-depth, not a claimed root-cause fix):
            // rebuild the settled frame from current bounds and repaint the
            // whole log chrome recursively, whatever the mid-flight frames
            // under load may have skipped.
            if (expanding)
            {
                LayoutLogChrome(segLog.SelectedIndex);
                if (logInset != null) logInset.Invalidate(true);
            }
            Invalidate(true);
            Update();
        }

        LogTextBox MakeLogBox(string placeholder)
        {
            var tb = new LogTextBox();
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.ScrollBars = ScrollBars.None; // v6.6: native bars hidden; SlimScrollBar overlays drive it
            tb.WordWrap = false;
            tb.BackColor = Pal.Inset;
            tb.ForeColor = Pal.LogText;
            tb.BorderStyle = BorderStyle.None;
            tb.Font = Fonts.Mono(9f, FontStyle.Regular);
            tb.Placeholder = placeholder;
            tb.Scrolled += (s, e) => SyncLogBars();
            return tb;
        }

        // v6.6: keep the overlay bars + jump chip in sync with the visible log
        void SyncLogBars()
        {
            try
            {
                if (sbSrvV == null || tbServerLog == null) return;
                if (logInset != null && !logInset.Visible)
                {
                    // stats tab: the whole inset (boxes, bars, chip) is hidden
                    if (btnJumpBottom != null) btnJumpBottom.Visible = false;
                    return;
                }
                bool tun = tbTunnelLog != null && tbTunnelLog.Visible;
                if (tun) { sbSrvV.Visible = false; sbSrvH.Visible = false; sbTunV.Sync(); sbTunH.Sync(); }
                else { sbTunV.Visible = false; sbTunH.Visible = false; sbSrvV.Sync(); sbSrvH.Sync(); }
                if (btnJumpBottom != null)
                {
                    LogTextBox tb = tun ? tbTunnelLog : tbServerLog;
                    bool up = false;
                    if (tb != null && tb.IsHandleCreated)
                    {
                        int total = tb.LineCount(), vis = tb.VisibleLines(), first = tb.FirstVisible();
                        up = total > vis && first + vis < total - 1;
                    }
                    if (btnJumpBottom.Visible != up) btnJumpBottom.Visible = up;
                }
            }
            catch { }
        }

        void JumpToBottom()
        {
            LogTextBox tb = (tbTunnelLog != null && tbTunnelLog.Visible) ? tbTunnelLog : tbServerLog;
            if (tb == null) return;
            try { tb.SelectionStart = tb.TextLength; tb.ScrollToCaret(); } catch { }
            SyncLogBars();
        }

        void ToggleLog()
        {
            logCollapsed = !logCollapsed;
            logToggleLbl.Caption = logCollapsed ? "▸  Log" : "▾  Log";
            bool expanding = !logCollapsed;
            if (expanding)
            {
                // Reveal the content up front: the growing window uncovers the
                // log box and command row from the bottom edge, and the tabs
                // fade in from the card color - nothing pops.
                // v6.7: on the stats tab the panel is what gets uncovered.
                bool statsPage = segLog.SelectedIndex == 2;
                bool backupPage = segLog.SelectedIndex == 3;
                logInset.Visible = !statsPage && !backupPage;
                cmdLbl.Visible = !statsPage && !backupPage;
                cmdInset.Visible = !statsPage && !backupPage;
                btnCmdSend.Visible = !statsPage && !backupPage;
                if (statsPanel != null) statsPanel.Visible = statsPage;
                if (backupPanel != null) backupPanel.Visible = backupPage;
                if (searchRow != null) searchRow.Visible = segLog.SelectedIndex == 0;
                segLog.Visible = true;
                if (!_animActive && _sysClientAnim) segLog.Fade = 0.0;
            }
            segLog.Enabled = false;
            StartLogAnim(expanding);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.F)
            {
                e.SuppressKeyPress = true;
                try
                {
                    if (segLog != null && segLog.SelectedIndex != 0) segLog.SelectPublic(0);
                    if (logCollapsed) ToggleLog();
                    if (tbSearch != null) { tbSearch.Focus(); tbSearch.SelectAll(); }
                }
                catch { }
                return;
            }
            if (e.Control && e.KeyCode == Keys.L) { ToggleLog(); e.SuppressKeyPress = true; }
        }

        // ---------- 工具 ----------
        void Ui(Action a)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) Invoke(a); else a(); } catch { }
        }

        int SendTb(TextBox tb, int msg, int wp, int lp)
        {
            try { return SendMessage(tb.Handle, msg, wp, lp).ToInt32(); } catch { return 0; }
        }

        // Append without yanking the view: follow the tail only when already at
        // the bottom; if the user scrolled up to read, keep their place.
        void AppendLogLine(TextBox tb, string s)
        {
            // v6.19: one repaint per appended line - the native EDIT
            // repaints on append, on scroll and on caret moves, which
            // strobes while the server log streams. WM_SETREDRAW off
            // batches the whole mutation; the matching re-enable and
            // Invalidate are at the end of this method.
            SendTb(tb, 0x000B, 0, 0);        // WM_SETREDRAW off
            int total = SendTb(tb, 0xBA, 0, 0);        // EM_GETLINECOUNT
            int first = SendTb(tb, 0xCE, 0, 0);        // EM_GETFIRSTVISIBLELINE
            int visible = Math.Max(1, tb.ClientSize.Height / Math.Max(1, tb.Font.Height));
            bool atBottom = total <= visible || first + visible >= total - 1;
            tb.AppendText(s + Environment.NewLine);
            total++;
            // v6.10: the 2000-line cap must run even when the user is scrolled
            // up reading history - it used to sit inside the atBottom branch,
            // so a long session read mid-game grew the box without bound
            // (soak: 6,301 lines / 607 KB retained). Rebuilding Lines resets
            // the view to the top, so afterwards scroll back to where the
            // reader was, shifted by the number of dropped lines.
            bool trimmed = false;
            if (total > 2100)
            {
                try
                {
                    string[] all = tb.Lines;
                    if (all.Length > 2000)
                    {
                        int drop = all.Length - 2000;
                        string[] keep = new string[2000];
                        Array.Copy(all, drop, keep, 0, 2000);
                        tb.Lines = keep;
                        trimmed = true;
                        if (!atBottom)
                        {
                            int target = Math.Max(0, first - drop);
                            if (target > 0) SendTb(tb, 0xB6, 0, target);   // EM_LINESCROLL down from the reset top
                        }
                    }
                }
                catch { }
            }
            if (atBottom)
            {
                tb.SelectionStart = tb.TextLength;
                tb.ScrollToCaret();
            }
            else if (!trimmed)
            {
                int first2 = SendTb(tb, 0xCE, 0, 0);
                int delta = first2 - first;
                if (delta != 0) SendTb(tb, 0xB6, 0, -delta);   // EM_LINESCROLL back
            }
            SendTb(tb, 0x000B, 1, 0);        // v6.19: WM_SETREDRAW on
            try { tb.Invalidate(); } catch { }
        }

        // v6.13: mirror both log panes to logs\launcher-yyyyMMdd.log so a
        // launcher crash/restart no longer loses the tunnel diagnostics.
        void PersistLog613(string tag, string s)
        {
            try
            {
                lock (_logLock613)
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    string path = Path.Combine(dir, "launcher-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                    File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss") + " [" + tag + "] " + s + "\r\n", Encoding.UTF8);
                }
            }
            catch { }
        }

        void CleanupOldLogs613()
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(dir)) return;
                var cutoff = DateTime.Now.AddDays(-14);
                foreach (string f in Directory.GetFiles(dir, "launcher-*.log"))
                {
                    try { if (File.GetLastWriteTime(f) < cutoff) File.Delete(f); } catch { }
                }
            }
            catch { }
        }

        void AppendServerLog(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            PersistLog613("Server", s);
            if (s.Length > _maxCharsSrv) _maxCharsSrv = s.Length;
            Ui(() =>
            {
                // v6.11: keep a bounded in-memory copy of the server log so
                // the error filter can rebuild the view at any time
                _srvLines.Add(s);
                while (_srvLines.Count > 2000) _srvLines.RemoveAt(0);
                if (_srvFilterErr && !IsErrLine(s)) return;
                AppendLogLine(tbServerLog, s);
            });
        }

        void AppendTunnelLog(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            PersistLog613("Tunnel", s);
            if (s.Length > _maxCharsTun) _maxCharsTun = s.Length;
            Ui(() => { AppendLogLine(tbTunnelLog, s); });
        }

        void AppendState(string s)
        {
            Ui(() => { if (statusCard != null) { statusCard.StateText = s; statusCard.Invalidate(); } });
        }

        // v6.11: the one place that writes to the server's stdin; the
        // command box, backups, schedules and player actions all go
        // through here.
        bool SendCmd(string cmd)
        {
            if (!ProcAlive(_server)) return false;
            try { _server.StandardInput.WriteLine(cmd); _server.StandardInput.Flush(); return true; }
            catch { return false; }
        }

        void SendServerCommand()
        {
            if (tbCmd == null) return;
            string cmd = tbCmd.Text.Trim();
            if (cmd.Length == 0) return;
            if (!ProcAlive(_server)) { AppendState("The server is not running; the command cannot be sent."); return; }
            try
            {
                if (!SendCmd(cmd)) { AppendState("Failed to send command: the server process is gone."); return; }
                AppendServerLog("> " + cmd);
                RecordCmd(cmd);
                tbCmd.Clear();
                tbCmd.Focus();
            }
            catch (Exception ex) { AppendState("Failed to send command: " + ex.Message); }
        }

        // ---------- 玩家管理入口（v6.11） ----------
        System.Collections.Generic.List<string> ReadNameList(string file)
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                string p = Path.Combine(_cfg.ServerDir, file);
                if (File.Exists(p))
                {
                    string txt = File.ReadAllText(p, Encoding.UTF8);
                    foreach (Match m in Regex.Matches(txt, "\"name\"\\s*:\\s*\"([^\"]+)\""))
                        if (!list.Contains(m.Groups[1].Value)) list.Add(m.Groups[1].Value);
                }
            }
            catch { }
            return list;
        }
        void OpenPlayerManager()
        {
            if (!ProcAlive(_server)) { InfoBox.Show("The server is not running; player management is available after the server starts.", "Players"); return; }
            var online = new System.Collections.Generic.List<string>();
            var slp = _slp;
            if (slp != null && slp.Ok && slp.Names.Count > 0) online.AddRange(slp.Names);
            else online.AddRange(_onlineNames);
            string me = null;
            foreach (var n in online) { if (n.StartsWith("oihg")) { me = n; break; } }
            var ops = ReadNameList("ops.json");
            var banned = ReadNameList("banned-players.json");
            using (var f = new PlayerForm(online, ops, banned, me, cmd =>
            {
                if (SendCmd(cmd)) AppendServerLog("> " + cmd);
                else AppendState("Command failed: the server process is gone.");
            }))
                f.ShowDialog(this);
        }

        // ---------- 世界备份（v6.11） ----------
        // The pattern the mature panels use: never copy a world the server
        // is actively writing. When the server is up we first ask it to
        // flush and pause saving (save-off / save-all flush), copy, then
        // always save-on again in a finally.
        string LevelName()
        {
            try
            {
                string pf = Path.Combine(_cfg.ServerDir, "server.properties");
                if (File.Exists(pf))
                {
                    foreach (var line in File.ReadAllLines(pf))
                    {
                        var t = line.Trim();
                        if (t.StartsWith("level-name=")) { string v = t.Substring("level-name=".Length).Trim(); if (v.Length > 0) return v; }
                    }
                }
            }
            catch { }
            return "world";
        }
        string WorldPath() { return Path.Combine(_cfg.ServerDir, LevelName()); }
        string BackupDirPath() { return Path.Combine(_cfg.ServerDir, "backups"); }

        private volatile bool _backupBusy = false;
        public bool BackupBusy { get { return _backupBusy; } }

        void DoBackup(string reason) { DoBackup(reason, null); }

        // Runs the zip on a worker thread; doneMsg (nullable) runs on the UI
        // thread afterwards. Synchronous core is BackupCore so the
        // start-of-server path can run it inline on its own thread.
        void DoBackup(string reason, Action doneUi)
        {
            if (_backupBusy) { AppendState("A backup is already in progress."); return; }
            _backupBusy = true;
            SetBackupUi();
            var th = new Thread(() => { BackupCore(reason); Ui(() => { _backupBusy = false; SetBackupUi(); RefreshBackupList(); if (doneUi != null) doneUi(); }); });
            th.IsBackground = true;
            th.Start();
        }

        void BackupCore(string reason)
        {
            string world = WorldPath();
            bool running = ProcAlive(_server) && _serverDone;
            try
            {
                if (!Directory.Exists(world)) { Ui(() => AppendState("World folder not found: " + world + ", cannot back up.")); return; }
                string bdir = BackupDirPath();
                Directory.CreateDirectory(bdir);
                if (running)
                {
                    SendCmd("save-off");
                    SendCmd("save-all flush");
                    Ui(() => AppendServerLog("[Backup] Server auto-save paused and flushed; packaging the world…"));
                    Thread.Sleep(2000);
                }
                string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string zip = Path.Combine(bdir, "world_" + stamp + ".zip");
                int files = 0, skipped = 0;
                long bytes = 0;
                using (var fs = new FileStream(zip, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var za = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (var f in Directory.GetFiles(world, "*", SearchOption.AllDirectories))
                    {
                        string rel = f.Substring(world.Length).TrimStart('\\', '/').Replace('\\', '/');
                        try
                        {
                            var entry = za.CreateEntry(rel, System.IO.Compression.CompressionLevel.Optimal);
                            using (var es = entry.Open())
                            using (var src = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            {
                                src.CopyTo(es);
                                bytes += src.Length;
                            }
                            files++;
                        }
                        catch { skipped++; }
                    }
                }
                Ui(() => AppendServerLog("[Backup] Done: " + Path.GetFileName(zip) + "（" + files + " files" + (skipped > 0 ? ", skipped " + skipped + " file(s) in use" : "") + "）"));
                Ui(() => AppendState("World backup complete (" + reason + "）。"));
                PruneBackups();
            }
            catch (Exception ex)
            {
                Ui(() => { AppendServerLog("[Backup] Failed: " + ex.Message); AppendState("Backup failed: " + ex.Message); });
            }
            finally
            {
                if (running) { SendCmd("save-on"); Ui(() => AppendServerLog("[Backup] Server auto-save resumed.")); }
            }
        }

        void PruneBackups()
        {
            try
            {
                var zips = new DirectoryInfo(BackupDirPath()).GetFiles("world_*.zip");
                Array.Sort(zips, (a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal));
                for (int i = _cfg.BackupKeep; i < zips.Length; i++) { try { zips[i].Delete(); } catch { } }
            }
            catch { }
        }

        void RestoreBackup(string zipPath)
        {
            if (ProcAlive(_server)) { InfoBox.Show("You must stop the server before restoring: swapping world files while the server runs will corrupt the save.\n\nClick \"Stop server safely\" first, then restore.", "Restore Backup"); return; }
            string name = Path.GetFileName(zipPath);
            bool yes = false;
            using (var c = new ConfirmBox("Restore the world to backup \"" + name + "\"?\n\nThe current world will not be deleted: it is renamed and set aside first (world_prerestore_<time>); delete it yourself once everything checks out. If the restore was wrong, you can swap it back."))
            {
                var r = c.ShowDialog(this);
                yes = (r == DialogResult.OK && c.ChoiceYes);
            }
            if (!yes) return;
            if (_backupBusy) { AppendState("A backup task is already in progress."); return; }
            _backupBusy = true; SetBackupUi();
            var th = new Thread(() =>
            {
                string world = WorldPath();
                string result;
                try
                {
                    string aside = world + "_prerestore_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    if (Directory.Exists(world)) Directory.Move(world, aside);
                    Directory.CreateDirectory(world);
                    using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var za = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read))
                    {
                        foreach (var entry in za.Entries)
                        {
                            if (entry.FullName.EndsWith("/")) continue;
                            string rel = entry.FullName.Replace('/', '\\');
                            string fullWorld = Path.GetFullPath(world);
                            string dest = Path.GetFullPath(Path.Combine(fullWorld, rel));
                            if (!dest.StartsWith(fullWorld + "\\", StringComparison.OrdinalIgnoreCase)) continue;
                            Directory.CreateDirectory(Path.GetDirectoryName(dest));
                            using (var es = entry.Open())
                            using (var ds = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None))
                                es.CopyTo(ds);
                        }
                    }
                    result = "Restored to backup \"" + name + "\". The old world was kept at " + Path.GetFileName(aside) + "; once the new world checks out, you can delete it.";
                }
                catch (Exception ex) { result = "Restore failed: " + ex.Message + "(The old world may already have been renamed. Do not start the server yet; check the server folder.)"; }
                Ui(() => { _backupBusy = false; SetBackupUi(); RefreshBackupList(); InfoBox.Show(result, "Restore Backup"); AppendServerLog("[Backup] " + result); });
            });
            th.IsBackground = true;
            th.Start();
        }

        // ---------- 备份页签 UI（v6.11） ----------
        TextBox BkBox(int x, int y, int w)
        {
            var t = new TextBox();
            t.SetBounds(x, y, w, 24);
            t.BackColor = Color.FromArgb(17, 20, 24);
            t.ForeColor = Pal.Text;
            t.BorderStyle = BorderStyle.None;
            t.Font = new Font("Microsoft YaHei UI", 9f);
            return t;
        }
        DarkCheckBox BkCheck(int x, int y, int w, string text)
        {
            var c = new DarkCheckBox();
            c.Text = text;
            c.SetBounds(x, y, w, 24);
            c.ForeColor = Pal.Text;
            c.BackColor = Pal.Inset;
            c.Font = new Font("Microsoft YaHei UI", 9f);
            return c;
        }
        TextLabel BkLabel(int x, int y, int w, string text, Color col)
        {
            var l = new TextLabel();
            l.Caption = text;
            l.SetBounds(x, y, w, 24);
            l.ForeColor = col;
            l.BackColor = Pal.Inset;
            l.Font = new Font("Microsoft YaHei UI", 9f);
            return l;
        }

        void BuildBackupPanel()
        {
            _bkInit = true;
            var head = BkLabel(16, 10, 220, "World backups", Pal.Text);
            head.Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold);
            backupPanel.Controls.Add(head);
            btnBackupNow = new RoundButton();
            btnBackupNow.Text = "Back up now";
            btnBackupNow.SetBounds(356, 8, 116, 28);
            btnBackupNow.BackColor = Pal.Surface; btnBackupNow.ForeColor = Pal.Text; btnBackupNow.BackdropColor = Pal.Inset;
            btnBackupNow.Font = new Font("Microsoft YaHei UI", 9f);
            btnBackupNow.Click += (s, e) => DoBackup("Manual backup");
            backupPanel.Controls.Add(btnBackupNow);
            lblBackupState = BkLabel(16, 38, 456, "—", Pal.Sub);
            backupPanel.Controls.Add(lblBackupState);
            backupList = new ListBox();
            backupList.SetBounds(16, 62, 456, 92);
            backupList.BackColor = Color.FromArgb(17, 20, 24);
            backupList.ForeColor = Pal.Text;
            backupList.BorderStyle = BorderStyle.None;
            backupList.Font = new Font("Microsoft YaHei UI", 9f);
            backupList.IntegralHeight = false;
            ThemedList.Attach(backupList);
            backupList.SelectedIndexChanged += (s, e) => SetBackupUi();
            backupList.DoubleClick += (s, e) => { int i = backupList.SelectedIndex; if (i >= 0 && i < _backupFiles.Count) RestoreBackup(_backupFiles[i]); };
            backupPanel.Controls.Add(backupList);
            btnRestoreBackup = new RoundButton();
            btnRestoreBackup.Text = "Restore selected";
            btnRestoreBackup.SetBounds(16, 160, 104, 28);
            btnRestoreBackup.BackColor = Pal.Surface; btnRestoreBackup.ForeColor = Pal.Text; btnRestoreBackup.BackdropColor = Pal.Inset;
            btnRestoreBackup.Font = new Font("Microsoft YaHei UI", 9f);
            btnRestoreBackup.Click += (s, e) => { int i = backupList.SelectedIndex; if (i >= 0 && i < _backupFiles.Count) RestoreBackup(_backupFiles[i]); else InfoBox.Show("Select a backup from the list above first.", "Restore Backup"); };
            backupPanel.Controls.Add(btnRestoreBackup);
            btnDeleteBackup = new RoundButton();
            btnDeleteBackup.Text = "Delete selected";
            btnDeleteBackup.SetBounds(128, 160, 104, 28);
            btnDeleteBackup.BackColor = Pal.Surface; btnDeleteBackup.ForeColor = Pal.Text; btnDeleteBackup.BackdropColor = Pal.Inset;
            btnDeleteBackup.Font = new Font("Microsoft YaHei UI", 9f);
            btnDeleteBackup.Click += (s, e) =>
            {
                int i = backupList.SelectedIndex;
                if (i < 0 || i >= _backupFiles.Count) { InfoBox.Show("Select a backup from the list above first.", "Delete Backup"); return; }
                bool yes = false;
                using (var c = new ConfirmBox("Delete backup \"" + Path.GetFileName(_backupFiles[i]) + "\"? It cannot be recovered after deletion."))
                { var r = c.ShowDialog(this); yes = (r == DialogResult.OK && c.ChoiceYes); }
                if (!yes) return;
                try { File.Delete(_backupFiles[i]); } catch (Exception ex) { InfoBox.Show("Delete failed: " + ex.Message, "Delete Backup"); }
                RefreshBackupList();
            };
            backupPanel.Controls.Add(btnDeleteBackup);
            lblBackupInfo = BkLabel(244, 162, 228, "", Pal.Sub);
            backupPanel.Controls.Add(lblBackupInfo);
            backupPanel.Controls.Add(BkLabel(16, 198, 62, "Keep the latest ", Pal.Sub));
            tbKeep = BkBox(142, 198, 44); tbKeep.Text = _cfg.BackupKeep.ToString(); tbKeep.TextAlign = HorizontalAlignment.Center;
            tbKeep.Leave += (s, e) => { int v; if (int.TryParse(tbKeep.Text.Trim(), out v) && v >= 1 && v <= 99) { _cfg.BackupKeep = v; _cfg.Save(); PruneBackups(); RefreshBackupList(); } tbKeep.Text = _cfg.BackupKeep.ToString(); };
            backupPanel.Controls.Add(tbKeep);
            backupPanel.Controls.Add(BkLabel(192, 198, 20, " backups", Pal.Sub));
            chkAutoRestart = BkCheck(218, 198, 128, "Auto-restart on crash");
            chkAutoRestart.Checked = _cfg.AutoRestart;
            chkAutoRestart.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.AutoRestart = chkAutoRestart.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkAutoRestart);
            chkBackupOnStart = BkCheck(352, 198, 120, "Auto-backup before server start");
            chkBackupOnStart.Checked = _cfg.BackupOnStart;
            chkBackupOnStart.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.BackupOnStart = chkBackupOnStart.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkBackupOnStart);
            chkDailyBackup = BkCheck(16, 226, 118, "Daily auto-backup");
            chkDailyBackup.Checked = _cfg.DailyBackup;
            chkDailyBackup.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.DailyBackup = chkDailyBackup.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkDailyBackup);
            tbDailyTime = BkBox(142, 226, 56); tbDailyTime.Text = _cfg.DailyBackupTime; tbDailyTime.TextAlign = HorizontalAlignment.Center;
            tbDailyTime.Leave += (s, e) => { TimeSpan ts; if (TimeSpan.TryParse(tbDailyTime.Text.Trim(), out ts)) { _cfg.DailyBackupTime = tbDailyTime.Text.Trim(); _cfg.Save(); } tbDailyTime.Text = _cfg.DailyBackupTime; };
            backupPanel.Controls.Add(tbDailyTime);
            backupPanel.Controls.Add(BkLabel(206, 226, 266, "While the app is open, back up automatically at the set time", Pal.Sub)); // v6.14: meaningful text
            chkTimedCmd = BkCheck(16, 254, 118, "Scheduled command");
            chkTimedCmd.Checked = _cfg.TimedCmdEnabled;
            chkTimedCmd.CheckedChanged += (s, e) => { if (_bkInit) return; _cfg.TimedCmdEnabled = chkTimedCmd.Checked; _cfg.Save(); };
            backupPanel.Controls.Add(chkTimedCmd);
            tbTimedTime = BkBox(142, 254, 56); tbTimedTime.Text = _cfg.TimedCmdTime; tbTimedTime.TextAlign = HorizontalAlignment.Center;
            tbTimedTime.Leave += (s, e) => { TimeSpan ts; if (TimeSpan.TryParse(tbTimedTime.Text.Trim(), out ts)) { _cfg.TimedCmdTime = tbTimedTime.Text.Trim(); _cfg.Save(); } tbTimedTime.Text = _cfg.TimedCmdTime; };
            backupPanel.Controls.Add(tbTimedTime);
            tbTimedCmd = BkBox(206, 254, 266); tbTimedCmd.Text = _cfg.TimedCmdText;
            tbTimedCmd.Leave += (s, e) => { if (_bkInit) return; _cfg.TimedCmdText = tbTimedCmd.Text; _cfg.Save(); };
            backupPanel.Controls.Add(tbTimedCmd);
            _bkInit = false;
            RefreshBackupList();
        }

        void SetBackupUi()
        {
            if (btnBackupNow == null) return;
            bool en = !_backupBusy;
            btnBackupNow.Enabled = en;
            if (btnRestoreBackup != null) btnRestoreBackup.Enabled = en;
            if (btnDeleteBackup != null) btnDeleteBackup.Enabled = en;
            if (lblBackupState != null && _backupBusy) lblBackupState.Caption = "Backing up… (large worlds may take a minute or two)";
        }

        void RefreshBackupList()
        {
            if (backupList == null) return;
            _backupFiles.Clear();
            backupList.Items.Clear();
            long total = 0;
            try
            {
                string bdir = BackupDirPath();
                if (Directory.Exists(bdir))
                {
                    var zips = new DirectoryInfo(bdir).GetFiles("world_*.zip");
                    Array.Sort(zips, (a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal));
                    foreach (var f in zips)
                    {
                        _backupFiles.Add(f.FullName);
                        total += f.Length;
                        string when = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
                        var m = Regex.Match(f.Name, @"world_(\d{4})(\d{2})(\d{2})_(\d{2})(\d{2})(\d{2})\.zip");
                        if (m.Success) when = m.Groups[1].Value + "-" + m.Groups[2].Value + "-" + m.Groups[3].Value + " " + m.Groups[4].Value + ":" + m.Groups[5].Value;
                        backupList.Items.Add(when + " · " + FmtBytes(f.Length));
                    }
                }
            }
            catch { }
            if (lblBackupInfo != null) lblBackupInfo.Caption = _backupFiles.Count > 0 ? ("Total " + _backupFiles.Count + " backups · " + FmtBytes(total)) : "No backups yet";
            if (lblBackupState != null && !_backupBusy)
                lblBackupState.Caption = _backupFiles.Count > 0 ? "Latest: " + backupList.Items[0].ToString() : "Click \"Back up now\" to snapshot the world.";
            SetBackupUi();
        }

        // ---------- 计划任务（v6.11 简化版） ----------
        static bool TimeReached(string hhmm)
        {
            TimeSpan t;
            if (!TimeSpan.TryParse(hhmm, out t)) return false;
            return DateTime.Now.TimeOfDay >= t;
        }
        void CheckSchedules()
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (_cfg.DailyBackup && _cfg.LastDailyBackup != today && TimeReached(_cfg.DailyBackupTime) && !_backupBusy)
                {
                    _cfg.LastDailyBackup = today; _cfg.Save();
                    AppendServerLog("[Schedule] Daily backup time reached; starting auto-backup.");
                    DoBackup("Daily auto-backup");
                }
                if (_cfg.TimedCmdEnabled && _cfg.LastTimedCmd != today && TimeReached(_cfg.TimedCmdTime)
                    && ProcAlive(_server) && _serverDone && !string.IsNullOrEmpty(_cfg.TimedCmdText))
                {
                    string c = _cfg.TimedCmdText.Trim();
                    // v6.14: a stop/restart command waits until nobody is
                    // online instead of kicking players mid-session
                    bool destructive = c.StartsWith("stop") || c.StartsWith("restart");
                    if (destructive && _onlineNames != null && _onlineNames.Count > 0)
                    {
                        if (!_timedDeferred614)
                        {
                            _timedDeferred614 = true;
                            AppendServerLog("[Schedule] A scheduled command is due, but " + _onlineNames.Count + " player(s) are still online; postponing until the server is empty.");
                        }
                    }
                    else
                    {
                        _timedDeferred614 = false;
                        _cfg.LastTimedCmd = today; _cfg.Save();
                        if (c.Length > 0 && SendCmd(c)) AppendServerLog("[Schedule] Sent scheduled command: " + c);
                    }
                }
            }
            catch { }
        }

        // ---------- 指令历史（v6.6） ----------
        void CmdKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SendServerCommand(); return; }
            if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                if (_cmdHist.Count == 0) return;
                if (_histIdx < 0) { _histDraft = tbCmd.Text; _histIdx = _cmdHist.Count - 1; }
                else if (_histIdx > 0) _histIdx--;
                tbCmd.Text = _cmdHist[_histIdx];
                tbCmd.SelectionStart = tbCmd.TextLength;
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                if (_histIdx < 0) return;
                _histIdx++;
                if (_histIdx >= _cmdHist.Count) { _histIdx = -1; tbCmd.Text = _histDraft; }
                else tbCmd.Text = _cmdHist[_histIdx];
                tbCmd.SelectionStart = tbCmd.TextLength;
            }
        }

        string CmdHistPath() { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cmdhistory.txt"); }

        void LoadCmdHistory()
        {
            try
            {
                if (!File.Exists(CmdHistPath())) return;
                foreach (string line in File.ReadAllLines(CmdHistPath(), System.Text.Encoding.UTF8))
                {
                    string t = line.Trim();
                    if (t.Length > 0 && (_cmdHist.Count == 0 || _cmdHist[_cmdHist.Count - 1] != t)) _cmdHist.Add(t);
                }
                while (_cmdHist.Count > 50) _cmdHist.RemoveAt(0);
            }
            catch { }
        }

        void RecordCmd(string cmd)
        {
            if (_cmdHist.Count == 0 || _cmdHist[_cmdHist.Count - 1] != cmd) _cmdHist.Add(cmd);
            while (_cmdHist.Count > 50) _cmdHist.RemoveAt(0);
            _histIdx = -1; _histDraft = "";
            try { File.WriteAllLines(CmdHistPath(), _cmdHist.ToArray(), System.Text.Encoding.UTF8); } catch { }
        }

        void RefreshStatus()
        {
            bool sRun = ProcAlive(_server);
            bool sDone = sRun && _serverDone;
            // 服务端灯
            if (statusCard != null)
            {
                if (sRun && sDone) { statusCard.ServerText = "Server: Running"; statusCard.ServerColor = Color.FromArgb(64, 199, 116); }
                else if (sRun) { statusCard.ServerText = "Server: Starting…"; statusCard.ServerColor = Color.FromArgb(250, 168, 26); }
                else { statusCard.ServerText = "Server: Not running"; statusCard.ServerColor = Color.FromArgb(94, 103, 115); }
            }

            bool tRun = ProcAlive(_frpc) || ProcAlive(_frpcVoice);
            if (statusCard != null)
            {
                if (tRun && _tunnelUp) { statusCard.TunnelText = "SakuraFrp tunnel: Connected"; statusCard.TunnelColor = Color.FromArgb(64, 199, 116); }
                else if (tRun) { statusCard.TunnelText = "SakuraFrp tunnel: Starting…"; statusCard.TunnelColor = Color.FromArgb(250, 168, 26); }
                else { statusCard.TunnelText = "SakuraFrp tunnel: Not running"; statusCard.TunnelColor = Color.FromArgb(94, 103, 115); }
            }

            bool gameUp = ProcAlive(_frpc) && _tunnelUp;
            if (statusCard != null)
            {
                if (string.IsNullOrEmpty(_cfg.PublicAddr)) { statusCard.AddrText = "Public address: Not generated"; statusCard.AddrColor = Color.FromArgb(94, 103, 115); }
                else if (gameUp) { statusCard.AddrText = "Public address: Ready"; statusCard.AddrColor = Color.FromArgb(64, 199, 116); }
                else { statusCard.AddrText = "Public address: Saved (tunnel not running)"; statusCard.AddrColor = Color.FromArgb(154, 165, 177); }
                statusCard.SetPulse(0, sRun && !sDone);
                statusCard.SetPulse(1, tRun && !_tunnelUp);
                statusCard.Invalidate();
            }

            if (_tray != null) { string tt = "Minecraft Multiplayer Helper · " + (sDone ? "Running" : ((sRun || tRun) ? "Starting" : "Stopped")); if (_tray.Text != tt) _tray.Text = tt; } // v6.13
            string pa = _cfg.PublicAddr ?? "";
            if (tbAddr.Text != pa) tbAddr.Text = pa; // v6.6: don't clobber a selection on every refresh
            btnCopy.Enabled = !string.IsNullOrEmpty(_cfg.PublicAddr);
            btnStop.Enabled = sRun || tRun;
            if (tbCmd != null) { tbCmd.Enabled = sRun; btnCmdSend.Enabled = sRun; }
        }

        void CopyAddr()
        {
            if (string.IsNullOrEmpty(_cfg.PublicAddr)) { InfoBox.Show("No public address yet. Start multiplayer first.", "Notice"); return; }
            try
            {
                Clipboard.SetText(_cfg.PublicAddr);
                AppendState("Public address copied to the clipboard — just send it to your friends.");
                // v6.9: the button itself confirms for a moment - the state
                // line alone was easy to miss
                btnCopy.Text = "Copied ✓";
                if (_copyTimer == null)
                {
                    _copyTimer = new System.Windows.Forms.Timer();
                    _copyTimer.Interval = 1200;
                    _copyTimer.Tick += (s, e) => { _copyTimer.Stop(); btnCopy.Text = "Copy address for friends"; };
                }
                _copyTimer.Stop();
                _copyTimer.Start();
            }
            catch { }
        }

        void OpenPcl2()
        {
            if (string.IsNullOrEmpty(_cfg.Pcl2Path) || !File.Exists(_cfg.Pcl2Path)) { InfoBox.Show("PCL2 not found. Set PCL2's exe path in Settings.", "Notice"); OpenSettings(); return; }
            try { Process.Start(new ProcessStartInfo(_cfg.Pcl2Path) { WorkingDirectory = Path.GetDirectoryName(_cfg.Pcl2Path) }); }
            catch (Exception ex) { InfoBox.Show("Failed to open PCL2: " + ex.Message, "Error"); }
        }

        void OpenSettings()
        {
            using (var f = new SettingsForm(_cfg) { ServerRunning = ProcAlive(_server) })
            {
                f.ShowDialog(this);
            }
            RefreshStatus();
        }

        // v6.11: run an action once after a delay, on the UI thread
        void Delayed(int ms, Action a)
        {
            var t = new System.Windows.Forms.Timer();
            t.Interval = ms;
            t.Tick += (s2, e2) => { t.Stop(); t.Dispose(); try { a(); } catch { } };
            t.Start();
        }

        // v6.11 crash watchdog: the server reached Done and then died
        // without anyone asking it to stop. Report it plainly, and (when
        // enabled) restart it - but never forever: 3 crashes inside 10
        // minutes means something is broken, not unlucky.
        void HandleServerCrash(int exitCode)
        {
            var now = DateTime.Now;
            _crashTimes.RemoveAll(t => (now - t).TotalMinutes > 10);
            _crashTimes.Add(now);
            bool again = _cfg.AutoRestart && _crashTimes.Count <= 3;
            string guess614 = GuessCrash614();
            AppendServerLog("[Watchdog] The server exited unexpectedly (exit code " + exitCode + "); this was not a normal shutdown." + (guess614.Length > 0 ? "Possible cause: " + guess614 + "。" : ""));
            if (!_cfg.AutoRestart)
            {
                AppendState("The server exited unexpectedly" + (guess614.Length > 0 ? ", possible cause: " + guess614 + "。" : ". Check the bottom of the server log for the reason.") + "(Watchdog auto-restart is off)");
                return;
            }
            if (!again)
            {
                AppendServerLog("[Watchdog] Crashed " + _crashTimes.Count + " times within 10 minutes; auto-restart stopped. Most likely a save or mod problem.");
                AppendState("The server kept crashing; auto-restart has stopped. Check the log." + (guess614.Length > 0 ? " Possible cause: " + guess614 + "。" : ""));
                return;
            }
            AppendState("The server exited unexpectedly; the watchdog will restart it in 5 seconds…");
            Delayed(5000, () => { if (!ProcAlive(_server) && !_stoppingIntentional) StartAll(); });
        }

        // v6.14: guess the crash cause from the recent log tail, in
        // plain words (borrowed from MC Server Manager's crash diagnosis)
        string GuessCrash614()
        {
            try
            {
                int n = _srvLines.Count;
                var tail = new System.Text.StringBuilder();
                for (int i = Math.Max(0, n - 40); i < n; i++) tail.Append(_srvLines[i]).Append('\n');
                string t = tail.ToString();
                if (t.Contains("Failed to bind to port") || t.Contains("Address already in use")) return "Port " + _cfg.Port + " is already in use — probably another server instance was not shut down cleanly";
                if (t.Contains("OutOfMemoryError") || t.Contains("Could not create the Java Virtual Machine") || t.Contains("insufficient memory")) return "Out of memory, or a bad memory setting (-Xmx)";
                if (t.Contains("UnsupportedClassVersionError")) return "Wrong Java version — this server needs Java 17";
                if (t.Contains("single server tick")) return "The server froze for over 60 seconds and was stopped by the game's built-in watchdog";
                if (t.Contains("ModLoadingException") || t.Contains("Failed to load mod") || t.Contains("requires fabric") || t.Contains("Missing or unsupported mandatory dependencies")) return "A mod failed to load; find the mod named in the log errors";
                if (t.Contains("Failed to load eula")) return "The EULA has not been accepted";
            }
            catch { }
            return "";
        }

        // ---------- 端口检查 ----------
        bool PortInUse(int port)
        {
            try
            {
                var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                l.Start();
                l.Stop();
                return false;
            }
            catch { return true; }
        }

        // ---------- 一键启动 ----------
        // ---------- v6.20: first-run wizard entry ----------
        bool _wizardOffered = false;

        void OfferWizardIfNeeded()
        {
            if (_wizardOffered) return;
            _wizardOffered = true;
            bool need = !Directory.Exists(_cfg.ServerDir) || !File.Exists(Path.Combine(_cfg.ServerDir, _cfg.JarName));
            if (!need) return;
            var r = InfoBox.Show("There is no usable server yet: the configured server folder does not exist, or it contains no server jar.\n\nThe first-run setup wizard downloads a matching Java and server (Vanilla / Fabric / Paper) for you, writes the EULA and the server.properties needed for offline play, with a progress bar and hash verification throughout.", "First-run setup wizard", "Open wizard", "Later");
            if (r == DialogResult.OK) OpenWizard();
        }

        void OpenWizard()
        {
            using (var wf = new WizardForm(_cfg, _cfg.ServerDir))
            {
                if (wf.ShowDialog(this) == DialogResult.OK)
                {
                    AppendState("First-run wizard finished: server and Java are ready.");
                    RefreshStatus();
                }
            }
        }

        // ---------- v6.20: pre-start Java version check ----------
        // Returns false only when the user cancels at the mismatch dialog.
        // Every "unknown" outcome logs its reason and lets the start
        // proceed - the check must never become a new way to break a
        // working setup, and it must never crash on a bad jar.
        bool CheckJavaVersion(string jarPath)
        {
            string mcVer = McJava.ReadMcVersion(jarPath);
            int required = mcVer != null ? McJava.RequiredJava(mcVer) : 0;
            string javaExe = McJava.ResolveJavaExe(_cfg);
            int have = javaExe != null ? McJava.GetJavaMajor(javaExe) : 0;
            string haveText = have > 0 ? have.ToString() : "unknown";
            if (mcVer == null)
            {
                AppendServerLog("[Java check] Cannot read the server jar's version info (version.json); skipping the version check. Current Java major version: " + haveText + ".");
                return true;
            }
            if (required <= 0)
            {
                AppendServerLog("[Java check] Server version " + mcVer + " is not in the version table; skipping the version check. Current Java major version: " + haveText + ".");
                return true;
            }
            if (have <= 0)
            {
                AppendServerLog("[Java check] Server " + mcVer + " needs Java " + required + ", but the current Java version could not be measured; starting anyway (watch the server log).");
                return true;
            }
            if (have >= required)
            {
                AppendServerLog("[Java check] Passed: server " + mcVer + " needs Java " + required + ", current Java is " + have + ".");
                return true;
            }
            AppendServerLog("[Java check] Mismatch: server " + mcVer + " needs Java " + required + ", current Java is only " + have + "; waiting for the user's decision.");
            bool proceed = false;
            Ui(() =>
            {
                string msg = "The Java version is too old - the server will most likely fail to start.\n\nServer version: " + mcVer + ", which needs Java " + required + " or newer. The Java about to be used is major version " + have + ".\n\nSuggestion: pick a Java " + required + " java.exe in Settings, or let the first-run setup wizard download a matching Java automatically.\nChoosing Start anyway launches with the current Java regardless; see the server log for the resulting errors.";
                proceed = InfoBox.Show(msg, "Java version mismatch", "Start anyway", "Cancel start") == DialogResult.OK;
            });
            AppendServerLog(proceed ? "[Java check] The user chose to start anyway." : "[Java check] The user cancelled the start.");
            return proceed;
        }

        void StartAll()
        {
            if (_starting) return;
            if (ProcAlive(_server) && _serverDone) { AppendState("The server is already running; no need to start it again."); btnStart.Text = "Multiplayer running"; btnStart.BackColor = Color.FromArgb(71, 77, 88); RefreshStatus(); return; }

            _starting = true;
            btnStart.Enabled = false;
            btnStart.Text = "Starting…";
            btnStart.BackColor = Color.FromArgb(72, 144, 245);
            Thread t = new Thread(() => DoStart());
            t.IsBackground = true;
            t.Start();
        }

        void DoStart()
        {
            try
            {
                // 1. 检查目录和文件
                if (!Directory.Exists(_cfg.ServerDir)) { FailState("Server folder does not exist: " + _cfg.ServerDir + ". Check the server folder in Settings."); return; }
                if (!File.Exists(Path.Combine(_cfg.ServerDir, _cfg.JarName))) { FailState("Server jar not found: " + _cfg.JarName + ". Check the jar file name in Settings."); return; }
                if (string.IsNullOrEmpty(_cfg.JavaPath) || !File.Exists(_cfg.JavaPath)) { FailState("Java (java.exe) not found. Re-select the Java path in Settings — usually under .minecraft\\runtime or in the Java install folder."); return; }

                // v6.20: Java version check (blocking dialog when too old; the user may still start)
                if (!CheckJavaVersion(Path.Combine(_cfg.ServerDir, _cfg.JarName))) { FailState("Start cancelled: the Java version is below what the server needs. Switch to a matching Java, or let the first-run setup wizard download one, then try again."); return; }

                // 2. 端口检查
                if (!ProcAlive(_server))
                {
                    if (PortInUse(_cfg.Port))
                    {
                        FailState("Port " + _cfg.Port + " is already in use. The last server may not have shut down cleanly — click \"Stop server safely\" or restart this app; do not kill Java processes in Task Manager yourself.");
                        return;
                    }
                }

                // v6.11: optional pre-start backup, inline on this worker
                // thread (the server is not up yet, so the world is quiet)
                if (_cfg.BackupOnStart && !_backupBusy)
                {
                    _backupBusy = true;
                    try { Ui(() => AppendState("Auto-backing up before server start…")); BackupCore("Auto-backup before server start"); }
                    catch { }
                    _backupBusy = false;
                    Ui(() => SetBackupUi());
                }

                // 3. 启动服务端
                AppendState("Starting the Minecraft server…");
                _serverDone = false;
                _stoppingIntentional = false;
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = _cfg.JavaPath;
                psi.Arguments = _cfg.MemParams + " -jar " + _cfg.JarName + " nogui";
                psi.WorkingDirectory = _cfg.ServerDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardInput = true;
                psi.StandardErrorEncoding = Encoding.UTF8;
                try { psi.StandardOutputEncoding = Encoding.UTF8; } catch { }

                _server = new Process();
                _server.StartInfo = psi;
                _server.OutputDataReceived += ServerOutput;
                _server.ErrorDataReceived += ServerError;
                _server.Exited += (s, e) =>
                {
                    int code = -999; try { code = ((Process)s).ExitCode; } catch { }
                    bool wasDone = _serverDone;
                    Ui(() =>
                    {
                        AppendServerLog("[Process exited]");
                        if (!_stoppingIntentional && wasDone) HandleServerCrash(code);
                        RefreshStatus();
                    });
                };
                _server.EnableRaisingEvents = true;
                if (!_server.Start()) { FailState("Cannot start the Java process. Check that the Java path is correct."); return; }
                _server.BeginOutputReadLine();
                _server.BeginErrorReadLine();

                // 等待 Done，最多 90 秒
                AppendState("Waiting for the server to be ready (watching for \"Done\")…");
                int waited = 0;
                while (!_serverDone && waited <= 90)
                {
                    if (!ProcAlive(_server)) { FailState("The server process exited before \"Done\" appeared. Most likely: 1) wrong Java path or memory arguments; 2) an error in the save/world or mods (check the bottom of the server log). Do not touch world or mods."); return; }
                    Thread.Sleep(1000);
                    waited++;
                }
                if (!_serverDone) { FailState("No \"Done\" after 90 seconds. Possibly not enough memory, an oversized world, or a stuck start. Check the bottom of the server log; do not touch world or mods."); return; }

                Ui(() => { btnStart.BackColor = Color.FromArgb(59, 165, 93); btnStart.Text = "Multiplayer running"; RefreshStatus(); });

                // 4. 启动隧道
                StartTunnel();

                // 5. 全成功
                if (_tunnelUp && !string.IsNullOrEmpty(_cfg.PublicAddr))
                {
                    Ui(() => { btnStart.BackColor = Color.FromArgb(71, 77, 88); AppendState("Ready. Click \"Copy address for friends\" and send the address to your friends."); });
                }
                else
                {
                    Ui(() => { btnStart.Text = "Start Multiplayer"; btnStart.BackColor = Color.FromArgb(59, 165, 93); });
                }
            }
            catch (Exception ex)
            {
                FailState("Start failed: " + ex.Message);
            }
            finally
            {
                _starting = false;
                Ui(() => { btnStart.Enabled = true; if (btnStart.Text == "Starting…") btnStart.Text = "Start Multiplayer"; RefreshStatus(); });
            }
        }

        void StartTunnel()
        {
            if (_cfg.TunnelMode == "launcher")
            {
                Ui(() => AppendTunnelLog("[Fallback] Opening the official SakuraFrp launcher — click Start Tunnel in it."));
                try { Process.Start(new ProcessStartInfo(_cfg.FrpcPath) { WorkingDirectory = Path.GetDirectoryName(_cfg.FrpcPath) }); }
                catch (Exception ex) { Ui(() => AppendTunnelLog("Failed to open the SakuraFrp launcher: " + ex.Message)); }
                return;
            }

            // frpc 命令行
            if (!File.Exists(_cfg.FrpcPath)) { Ui(() => AppendTunnelLog("frpc.exe not found: " + _cfg.FrpcPath + ". Check it in Settings, or switch the tunnel mode to the official launcher.")); return; }
            if (string.IsNullOrEmpty(_cfg.FrpKey)) { Ui(() => AppendTunnelLog("The access key is empty. Fill in the SakuraFrp access key in Settings.")); return; }
            if (string.IsNullOrEmpty(_cfg.TunnelId)) { Ui(() => AppendTunnelLog("The tunnel ID is empty. Fill it in in Settings.")); return; }

            if (ProcAlive(_frpc)) { StartVoiceTunnel(); return; }
            AppendState("Starting the SakuraFrp tunnel…");
            _tunnelUp = false;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = _cfg.FrpcPath;
            psi.Arguments = "-f " + _cfg.FrpKey + ":" + _cfg.TunnelId;
            psi.WorkingDirectory = Path.GetDirectoryName(_cfg.FrpcPath);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardErrorEncoding = Encoding.UTF8;
            try { psi.StandardOutputEncoding = Encoding.UTF8; } catch { }

            _frpc = new Process();
            _frpc.StartInfo = psi;
            _frpc.OutputDataReceived += FrpcOutput;
            _frpc.ErrorDataReceived += FrpcError;
            _frpc.EnableRaisingEvents = true;
            _frpc.Exited += (s, e) => { Ui(() =>
            {
                AppendTunnelLog("[frpc exited]");
                if (!_stoppingIntentional && _serverDone && ProcAlive(_server) && _cfg.TunnelMode == "frpc")
                {
                    AppendTunnelLog("[Watchdog] The game tunnel exited unexpectedly; reconnecting in 5 seconds.");
                    Delayed(5000, () => { if (!_stoppingIntentional && ProcAlive(_server)) StartTunnel(); });
                }
                RefreshStatus();
            }); };
            if (!_frpc.Start()) { Ui(() => AppendTunnelLog("frpc failed to start. Check the frpc.exe path.")); return; }
            _frpc.BeginOutputReadLine();
            _frpc.BeginErrorReadLine();

            int waited = 0;
            while (!_tunnelUp && waited <= 40)
            {
                if (!ProcAlive(_frpc))
                {
                    if (_frpcBundled)
                    {
                        Ui(() => AppendTunnelLog("The current frpc.exe is the bundled build that ships with the SakuraFrp launcher. That build cannot be run directly by external programs (only via the SakuraFrp Service), so it exits immediately. This has nothing to do with your access key, tunnel ID, or node. In Settings, point the frpc path at a standalone frpc, or switch the tunnel mode to the official launcher."));
                    }
                    else
                    {
                        Ui(() => AppendTunnelLog("The frpc process exited. Possibly: 1) wrong access key or tunnel ID; 2) a node problem; 3) the account needs verification or check-in. Check the key and tunnel ID in Settings, or switch the tunnel mode to the official launcher."));
                    }
                    break;
                }
                Thread.Sleep(1000);
                waited++;
            }
            if (_frpcBundled && !_tunnelUp)
            {
                Ui(() => { AppendState("This frpc is the launcher's bundled build and cannot run directly. In Settings, switch to a standalone frpc or the official launcher mode."); });
            }

            StartVoiceTunnel();
        }

        void StartVoiceTunnel()
        {
            if (string.IsNullOrEmpty(_cfg.VoiceTunnelId)) { Ui(() => AppendTunnelLog("[Voice] No voice tunnel ID set; skipping the voice tunnel.")); return; }
            if (ProcAlive(_frpcVoice)) return;
            _voiceUp = false;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = _cfg.FrpcPath;
            psi.Arguments = "-f " + _cfg.FrpKey + ":" + _cfg.VoiceTunnelId;
            psi.WorkingDirectory = Path.GetDirectoryName(_cfg.FrpcPath);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardErrorEncoding = Encoding.UTF8;
            try { psi.StandardOutputEncoding = Encoding.UTF8; } catch { }

            _frpcVoice = new Process();
            _frpcVoice.StartInfo = psi;
            _frpcVoice.OutputDataReceived += VoiceFrpcOutput;
            _frpcVoice.ErrorDataReceived += VoiceFrpcError;
            _frpcVoice.EnableRaisingEvents = true;
            _frpcVoice.Exited += (s, e) => { Ui(() =>
            {
                AppendTunnelLog("[Voice frpc exited]");
                if (!_stoppingIntentional && _serverDone && ProcAlive(_server))
                {
                    AppendTunnelLog("[Watchdog] The voice tunnel exited unexpectedly; reconnecting in 5 seconds.");
                    Delayed(5000, () => { if (!_stoppingIntentional && ProcAlive(_server)) StartVoiceTunnel(); });
                }
                RefreshStatus();
            }); };
            if (!_frpcVoice.Start()) { Ui(() => AppendTunnelLog("[Voice] frpc failed to start. Check the frpc.exe path.")); return; }
            _frpcVoice.BeginOutputReadLine();
            _frpcVoice.BeginErrorReadLine();

            int waited = 0;
            while (!_voiceUp && waited <= 20)
            {
                if (!ProcAlive(_frpcVoice)) { Ui(() => AppendTunnelLog("[Voice] The frpc process exited. Check that the voice tunnel ID is correct.")); break; }
                Thread.Sleep(1000);
                waited++;
            }
            if (_voiceUp) Ui(() => AppendState("The voice tunnel is connected."));
        }

        void VoiceFrpcOutput(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog("[Voice] " + e.Data);
            TryParseVoice(e.Data);
        }
        void VoiceFrpcError(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog("[Voice][ERR] " + e.Data);
            TryParseVoice(e.Data);
        }
        void TryParseVoice(string line)
        {
            if (Regex.IsMatch(line, @"代理启动成功|start proxy success|login success|隧道.*启动成功|\[.*\]\s*tunnel.*success"))
                _voiceUp = true;
        }

        // v6.9: keep the online-player list from the server log's own
        // join/leave lines (the count itself still comes from the TCP table)
        void TrackPlayerLine(string line)
        {
            try
            {
                var m = Regex.Match(line, @"([A-Za-z0-9_]{3,16}) joined the game");
                if (m.Success)
                {
                    string n = m.Groups[1].Value;
                    Ui(() => { if (!_onlineNames.Contains(n)) _onlineNames.Add(n); });
                    return;
                }
                m = Regex.Match(line, @"([A-Za-z0-9_]{3,16}) left the game");
                if (m.Success)
                {
                    string n = m.Groups[1].Value;
                    Ui(() => { _onlineNames.Remove(n); });
                }
            }
            catch { }
        }

        void ServerOutput(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendServerLog(e.Data);
            TrackPlayerLine(e.Data);
            if (Regex.IsMatch(e.Data, "Done \\(") && e.Data.Contains("For help"))
                _serverDone = true;
        }
        void ServerError(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendServerLog("[ERR] " + e.Data);
            if (Regex.IsMatch(e.Data, "Done \\(") && e.Data.Contains("For help"))
                _serverDone = true;
        }
        void FrpcOutput(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog(e.Data);
            TryParseFrpc(e.Data);
        }
        void FrpcError(object s, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            AppendTunnelLog("[ERR] " + e.Data);
            TryParseFrpc(e.Data);
        }

        void TryParseFrpc(string line)
        {
            // 检测 SakuraFrp 启动器套装版 frpc（不能由外部程序直接调用）
            if (line.Contains("not intended to be run directly") || line.Contains("other than SakuraFrp Service") || line.Contains("part of SakuraFrp Launcher"))
            {
                _frpcBundled = true;
            }
            // 成功字样
            if (Regex.IsMatch(line, @"代理启动成功|start proxy success|login success|隧道.*启动成功|\[.*\]\s*tunnel.*success"))
                _tunnelUp = true;

            // 解析公网地址：形如 xxxx.natfrp.cloud:port 或 域名:port
            if (string.IsNullOrEmpty(_cfg.PublicAddr))
            {
                var m = Regex.Match(line, "\\b[a-z0-9][a-z0-9-]*\\.(?:natfrp\\.cloud|frp-[a-z0-9]+\\.com|gofrp\\.org|nathub\\.app|frp-a\\.com|\\w+\\.frp-\\w+\\.com):\\d+\\b");
                if (!m.Success) m = Regex.Match(line, "\\b[a-z0-9][a-z0-9-]*\\.\\w+\\.\\w{2,4}:\\d{4,5}\\b");
                if (m.Success && !m.Value.StartsWith("127.0.0.1"))
                {
                    string addr = m.Value;
                    _cfg.PublicAddr = addr;
                    _cfg.Save();
                    Ui(() => { tbAddr.Text = addr; AppendState("Public address obtained automatically: " + addr); RefreshStatus(); });
                }
            }
        }

        void FailState(string msg)
        {
            Ui(() => {
                AppendState(msg);
                InfoBox.Show(msg, "Error");
                if (statusCard != null) { statusCard.ServerText = "Server: Error"; statusCard.ServerColor = Color.Red; statusCard.TunnelText = "SakuraFrp tunnel: Error"; statusCard.TunnelColor = Color.Red; statusCard.SetPulse(0, false); statusCard.SetPulse(1, false); statusCard.Invalidate(); }
                btnStart.Text = "Start Multiplayer"; btnStart.BackColor = Color.FromArgb(59, 165, 93);
            });
        }

        // ---------- 安全关服 ----------
        void SafeStop()
        {
            _stoppingIntentional = true;
            if (!ProcAlive(_server))
            {
                KillFrpc();
                Ui(() => { AppendState("The server is not running; the tunnel has been stopped."); RefreshStatus(); });
                return;
            }

            Ui(() => { btnStop.Enabled = false; });
            AppendState("Stopping safely: sending stop to the server and waiting for the save…");
            try { _server.StandardInput.WriteLine("stop"); _server.StandardInput.Flush(); }
            catch { }

            AppendServerLog(">> stop sent; waiting for the save and exit (up to 60 seconds)");

            // 最多等 60 秒
            int waited = 0;
            bool exited = false;
            while (waited <= 60)
            {
                if (!ProcAlive(_server)) { exited = true; break; }
                Thread.Sleep(1000);
                waited++;
            }

            if (exited)
            {
                AppendState("The server stopped normally (save complete). Closing the tunnel…");
                Ui(() => AppendServerLog("[Server process exited normally]"));
            }
            else
            {
                DialogResult res = DialogResult.Cancel;
                Ui(() => { res = InfoBox.Show("The server still has not exited after 60 seconds. Force-ending it may lose the last few minutes of progress. Force end it?", "Safe shutdown timed out", "Force end", "Wait longer"); });
                if (res == DialogResult.OK)
                {
                    try { _server.Kill(); } catch { }
                    AppendState("The server process was force-ended (recent progress may be lost).");
                }
                else
                {
                    AppendState("Force end cancelled; the server is still running.");
                }
            }

            KillFrpc();
            bool serverGone = !ProcAlive(_server);
            Ui(() => { btnStart.Text = "Start Multiplayer"; btnStart.BackColor = Color.FromArgb(59, 165, 93); btnStop.Enabled = true; RefreshStatus(); if (serverGone) AppendState("All stopped: the server and the tunnel are closed."); });
        }

        // HasExited throws on a Process that was constructed but whose
        // Start() failed (no associated process). Never read it raw on
        // the frpc processes: a blocked start must not crash stop/status.
        static bool ProcAlive(Process p)
        {
            if (p == null) return false;
            try { return !p.HasExited; } catch { return false; }
        }

        void KillFrpc()
        {
            if (ProcAlive(_frpc)) { try { _frpc.Kill(); } catch { } }
            _frpc = null;
            _tunnelUp = false;
            if (ProcAlive(_frpcVoice)) { try { _frpcVoice.Kill(); } catch { } }
            _frpcVoice = null;
            _voiceUp = false;
        }

        // ---------- 关闭拦截 ----------
        void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (_animActive)
            {
                _animActive = false;
                if (_animTimer != null) _animTimer.Stop();
                try { timeEndPeriod(1); } catch { }
                _animClock.Stop();
            }
            try { if (WindowState == FormWindowState.Normal) { _cfg.WinX = Location.X; _cfg.WinY = Location.Y; _cfg.Save(); } } catch { }
            bool serverRunning = ProcAlive(_server);
            bool frpcRunning = ProcAlive(_frpc) || ProcAlive(_frpcVoice);
            if (serverRunning || frpcRunning)
            {
                using (var c = new ConfirmBox("The server is still running. Closing directly may skip saving — stop it safely first?"))
                {
                    var r = c.ShowDialog(this);
                    if (r == DialogResult.OK && c.ChoiceYes)
                    {
                        _stoppingIntentional = true;
                        // 安全关服
                        try { if (ProcAlive(_server)) { _server.StandardInput.WriteLine("stop"); _server.StandardInput.Flush(); } } catch { }
                        int waited = 0;
                        while (ProcAlive(_server) && waited <= 60) { Thread.Sleep(1000); waited++; }
                        KillFrpc();
                        e.Cancel = false;
                    }
                    else
                    {
                        e.Cancel = true; // 取消关闭
                    }
                }
            }
        }
    }
}
