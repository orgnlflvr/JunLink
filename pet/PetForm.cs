using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
namespace JunLinkPet;

// The see-through window that shows Jun. Pictures come from the game mod (http://127.0.0.1:8765/pet/frame.png).
sealed class PetForm : Form
{
    const int BubbleH = 170, MinW = 330;
    readonly Settings S;
    readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    readonly CancellationTokenSource cts = new CancellationTokenSource();
    readonly System.Windows.Forms.Timer tick = new System.Windows.Forms.Timer { Interval = 250 };
    readonly NotifyIcon tray;
    readonly ContextMenuStrip menu = new ContextMenuStrip();
    readonly Random rnd = new Random();

    Surface surf; Bitmap pet; Rectangle petRect; int lastSeq = -1;
    bool allowShow, placed, gameTried, busy, told;
    string bubble = ""; bool thinking; DateTime bubbleUntil = DateTime.MinValue;
    bool mouseDown, dragged; Point downScreen, downLoc;
    TalkForm talk;

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x00080000 | 0x00000080 | 0x08000000; return cp; }   // LAYERED | TOOLWINDOW | NOACTIVATE
    }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override void SetVisibleCore(bool value) { base.SetVisibleCore(value && allowShow); }
    protected override void OnPaintBackground(PaintEventArgs e) { }

    public PetForm(Settings s)
    {
        S = s;
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; TopMost = S.TopMost;
        Text = "Jun"; Size = new Size(MinW, BubbleH + S.Height);
        var h = Handle;                                       // create the window now so background tasks can talk to it
        http.DefaultRequestHeaders.Add("X-Panel", "1");

        tray = new NotifyIcon { Icon = AppIcon(), Text = "Jun (waiting for the game)", Visible = true, ContextMenuStrip = menu };
        menu.Opening += (o, e) => BuildMenu();
        BuildMenu();
        tick.Tick += (o, e) => { if (bubble.Length > 0 && !thinking && DateTime.Now > bubbleUntil) { bubble = ""; Render(); } };
        tick.Start();
        _ = PollLoop();
    }

    // the Jun Link logo (pet.ico, built in), or Windows' plain one if it cannot be read
    static Icon AppIcon()
    {
        try { using (var st = typeof(PetForm).Assembly.GetManifestResourceStream("pet.ico")) if (st != null) return new Icon(st, 32, 32); } catch { }
        return SystemIcons.Application;
    }

    // ---------------- menu ----------------
    void BuildMenu()
    {
        menu.Items.Clear();
        menu.Items.Add("Talk to Jun...", null, (o, e) => ShowTalk());
        var size = new ToolStripMenuItem("Size");
        foreach (var kv in new[] { ("Small", 300), ("Medium", 450), ("Large", 600), ("Huge", 800) })
        {
            int v = kv.Item2; var it = new ToolStripMenuItem(kv.Item1 + " (" + v + " px)") { Checked = S.Height == v };
            it.Click += (o, e) => { S.Height = v; S.Save(); Render(); };
            size.DropDownItems.Add(it);
        }
        menu.Items.Add(size);
        menu.Items.Add(Check("Always on top", S.TopMost, v => { S.TopMost = v; TopMost = v; }));
        menu.Items.Add(Check("She answers clicks", S.Talk, v => S.Talk = v));
        menu.Items.Add(Check("Fix dark edges", S.Premultiplied, v => { S.Premultiplied = v; Render(true); }));
        menu.Items.Add(Check("Pause (the game stops drawing her)", S.Paused, v => { S.Paused = v; if (v) HidePet(); }));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Check("Start with Windows", Program.GetAutorun(), v => Program.SetAutorun(v)));
        menu.Items.Add(Check("Start the game with the pet", S.LaunchGame, v => { S.LaunchGame = v; if (v && S.GameExe.Length == 0) ChooseGame(); }));
        menu.Items.Add("Choose the game's .exe...", null, (o, e) => ChooseGame());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open the pet page in the browser", null, (o, e) => { try { Process.Start(new ProcessStartInfo(S.Url + "/pet") { UseShellExecute = true }); } catch { } });
        menu.Items.Add("Quit", null, (o, e) => Quit());
    }

    ToolStripMenuItem Check(string text, bool on, Action<bool> set)
    {
        var it = new ToolStripMenuItem(text) { Checked = on, CheckOnClick = true };
        it.CheckedChanged += (o, e) => { set(it.Checked); S.Save(); };
        return it;
    }

    void ChooseGame()
    {
        using (var d = new OpenFileDialog { Title = "Select the game's .exe (My Dystopian Robot Girlfriend)", Filter = "Game (*.exe)|*.exe" })
            if (d.ShowDialog() == DialogResult.OK) { S.GameExe = d.FileName; S.Save(); }
    }

    public void Quit()
    {
        cts.Cancel(); S.Save(); tray.Visible = false; tray.Dispose(); Application.Exit();
    }

    // ---------------- frames from the game ----------------
    async Task PollLoop()
    {
        while (!cts.IsCancellationRequested)
        {
            int delay = 30;
            try
            {
                if (S.Paused) { await Task.Delay(500); continue; }
                using (var r = await http.GetAsync(S.Url + "/pet/frame.png?seq=" + lastSeq, cts.Token))
                {
                    if ((int)r.StatusCode == 200)
                    {
                        string sq; if (r.Headers.TryGetValues("X-Seq", out var vs)) { sq = vs.First(); int.TryParse(sq, out lastSeq); }
                        var bytes = await r.Content.ReadAsByteArrayAsync();
                        BeginInvoke(new Action(() => OnFrame(bytes)));
                    }
                    else if ((int)r.StatusCode == 204)
                    {
                        if (r.Headers.TryGetValues("X-Pet", out var vp) && vp.First() == "none") { BeginInvoke(new Action(() => HidePet("Jun (load your save in the game)"))); delay = 800; }
                        else delay = 40;
                    }
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested) { return; }
            catch
            {
                BeginInvoke(new Action(() => HidePet("Jun (start the game with Jun Link)")));
                delay = 3000;
                if (S.LaunchGame && !gameTried) { gameTried = true; LaunchGame(); }
            }
            try { await Task.Delay(delay, cts.Token); } catch { return; }
        }
    }

    void LaunchGame()
    {
        try
        {
            if (S.GameExe.Length == 0 || !File.Exists(S.GameExe)) return;
            string name = Path.GetFileNameWithoutExtension(S.GameExe);
            if (Process.GetProcessesByName(name).Length > 0) return;
            var p = Process.Start(new ProcessStartInfo(S.GameExe, S.GameArgs) { WorkingDirectory = Path.GetDirectoryName(S.GameExe), UseShellExecute = false });
            if (p == null) return;
            Task.Run(async () =>
            {
                for (int i = 0; i < 120; i++)
                {
                    await Task.Delay(500);
                    try { p.Refresh(); if (p.MainWindowHandle != IntPtr.Zero) { await Task.Delay(1500); Native.ShowWindow(p.MainWindowHandle, 7); break; } } catch { break; }   // 7 = minimize without taking focus
                }
            });
            if (!told) { told = true; tray.ShowBalloonTip(4000, "Jun", "Starting the game minimized. Load your save and she will appear.", ToolTipIcon.Info); }
        }
        catch { }
    }

    void HidePet(string tip = null)
    {
        if (tip != null) tray.Text = tip.Length > 60 ? tip.Substring(0, 60) : tip;
        if (allowShow) { allowShow = false; Hide(); }
    }

    // the picture is the game's render with alpha; copy its bytes as they are into a premultiplied bitmap (or multiply them first)
    void OnFrame(byte[] bytes)
    {
        try
        {
            Bitmap nb;
            using (var ms = new MemoryStream(bytes))
            using (var src = new Bitmap(ms))
            {
                int w = src.Width, h = src.Height;
                nb = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                var sd = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var dd = nb.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
                try
                {
                    var row = new byte[w * 4];
                    for (int y = 0; y < h; y++)
                    {
                        Marshal.Copy(sd.Scan0 + y * sd.Stride, row, 0, row.Length);
                        if (!S.Premultiplied)
                            for (int i = 0; i < row.Length; i += 4) { int a = row[i + 3]; if (a < 255) { row[i] = (byte)(row[i] * a / 255); row[i + 1] = (byte)(row[i + 1] * a / 255); row[i + 2] = (byte)(row[i + 2] * a / 255); } }
                        Marshal.Copy(row, 0, dd.Scan0 + y * dd.Stride, row.Length);
                    }
                }
                finally { src.UnlockBits(sd); nb.UnlockBits(dd); }
            }
            var old = pet; pet = nb; if (old != null) old.Dispose();
            tray.Text = "Jun";
            Render(true);
        }
        catch (Exception ex) { tray.Text = "Jun (bad picture: " + ex.Message + ")"; }
    }

    // ---------------- drawing ----------------
    void Render(bool show = false)
    {
        if (pet == null) return;
        int ph = S.Height, pw = Math.Max(40, (int)Math.Round(pet.Width * (double)ph / pet.Height));
        int W = Math.Max(pw, MinW), H = BubbleH + ph;
        if (surf == null || surf.W != W || surf.H != H)
        {
            int cx = Left + Width / 2, bottom = Top + Height;
            if (surf != null) surf.Dispose();
            surf = new Surface(W, H);
            if (!placed) PlaceFirst(W, H);
            else SetBounds(cx - W / 2, bottom - H, W, H);              // keep her feet where they were
        }
        surf.Clear();
        int px = (W - pw) / 2;
        petRect = new Rectangle(px, BubbleH, pw, ph);
        surf.G.DrawImage(pet, petRect, 0, 0, pet.Width, pet.Height, GraphicsUnit.Pixel);
        DrawBubble(surf.G, W);
        if (show && !allowShow && !S.Paused) { allowShow = true; Show(); }
        surf.Present(Handle, Left, Top);
    }

    void PlaceFirst(int W, int H)
    {
        placed = true;
        var wa = Screen.PrimaryScreen.WorkingArea;
        int x = S.X, y = S.Y;
        var rect = new Rectangle(x, y, W, H);
        bool onScreen = x >= -10000 && Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(rect));
        if (!onScreen || S.X < 0) { x = wa.Right - W - 40; y = wa.Bottom - H + 10; }
        SetBounds(x, y, W, H);
    }

    void DrawBubble(Graphics g, int W)
    {
        if (bubble.Length == 0) return;
        using (var font = FontLoader.Body(10f))
        using (var tabFont = FontLoader.Heavy(8f))
        {
            const int maxW = 300;
            var m = g.MeasureString(bubble, font, maxW - 24);
            int bw = (int)Math.Min(maxW, m.Width + 28), bh = (int)Math.Min(BubbleH - 36, m.Height + 18);
            var box = new Rectangle((W - bw) / 2, BubbleH + 4 - bh, bw, bh);
            using (var sh = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillRectangle(sh, box.X + 4, box.Y + 4, box.Width, box.Height);
            using (var bg = new SolidBrush(Color.White)) g.FillRectangle(bg, box);
            using (var pen = new Pen(Color.FromArgb(34, 16, 59), 3f)) g.DrawRectangle(pen, box);
            var tab = new Rectangle(box.X, box.Y - 19, 46, 19);
            using (var cr = new SolidBrush(Color.FromArgb(211, 16, 79))) g.FillRectangle(cr, tab);
            using (var pen = new Pen(Color.FromArgb(34, 16, 59), 3f)) g.DrawRectangle(pen, tab);
            g.DrawString("JUN", tabFont, Brushes.White, tab.X + 7, tab.Y + 3);
            using (var fmt = new StringFormat { Trimming = StringTrimming.EllipsisWord })
            using (var tb = new SolidBrush(thinking ? Color.FromArgb(110, 90, 130) : Color.FromArgb(26, 16, 36)))
                g.DrawString(bubble, font, tb, new RectangleF(box.X + 12, box.Y + 8, box.Width - 22, box.Height - 12), fmt);
        }
    }

    public void SetBubble(string text, bool think, double secs)
    {
        bubble = text ?? ""; thinking = think;
        bubbleUntil = think ? DateTime.MaxValue : DateTime.Now.AddSeconds(secs);
        Render();
    }

    // ---------------- mouse: drag, click ----------------
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) { mouseDown = true; dragged = false; downScreen = Cursor.Position; downLoc = Location; }
        base.OnMouseDown(e);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (mouseDown)
        {
            var p = Cursor.Position; int dx = p.X - downScreen.X, dy = p.Y - downScreen.Y;
            if (!dragged && Math.Abs(dx) + Math.Abs(dy) > 6) dragged = true;
            if (dragged) Location = new Point(downLoc.X + dx, downLoc.Y + dy);
        }
        base.OnMouseMove(e);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && mouseDown)
        {
            mouseDown = false;
            if (dragged) { S.X = Left; S.Y = Top; S.Save(); }
            else Clicked(e.Location);
        }
        else if (e.Button == MouseButtons.Right) { BuildMenu(); menu.Show(Cursor.Position); }
        base.OnMouseUp(e);
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e) { ShowTalk(); base.OnMouseDoubleClick(e); }

    static readonly (string tags, string line)[] Head = {
        ("shy happy", "Head contact detected. Acceptable."), ("veryhappy", "That was a pleasant input. ...Do it again."), ("shy", "Recalibrating. My processor is warm.") };
    static readonly (string tags, string line)[] Body = {
        ("surprised", "Yes? I was idle."), ("wave", "Poke registered. Hello."), ("happy", "I am not a touchscreen. ...Carry on."), ("surprised", "Unexpected contact. Fascinating.") };

    void Clicked(Point p)
    {
        if (!petRect.Contains(p) || busy) return;
        var pool = (p.Y - petRect.Y) < petRect.Height * 0.30 ? Head : Body;
        var c = pool[rnd.Next(pool.Length)];
        React(c.tags);
        if (S.Talk) SetBubble(c.line, false, 2.8);
    }

    async void React(string tags)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, S.Url + "/api/pet/react") { Content = new StringContent("{\"tags\":\"" + tags + "\"}", Encoding.UTF8, "application/json") };
            (await http.SendAsync(req)).Dispose();
        }
        catch { }
    }

    // ---------------- talking ----------------
    void ShowTalk()
    {
        if (talk != null && !talk.IsDisposed) { talk.Activate(); return; }
        talk = new TalkForm(this, text => _ = SayToJun(text));
        talk.Location = new Point(Left + Width / 2 - talk.Width / 2, Top + Height - 70);
        talk.Show();
    }

    static (string tags, string text) Split(string acc)
    {
        string tags = "", text = acc;
        var m = Regex.Match(acc, @"^\s*((?:\[[A-Za-z .\-_]+\]\s*)+)");
        if (m.Success)
        {
            tags = string.Join(" ", Regex.Matches(m.Groups[1].Value, @"\[([A-Za-z .\-_]+)\]").Select(t => Regex.Replace(t.Groups[1].Value.ToLowerInvariant(), "[^a-z]", "")));
            text = acc.Substring(m.Length);
        }
        text = Regex.Replace(text, @"\*[^*]*\*", " "); text = Regex.Replace(text, @"[ \t]{2,}", " ").Trim();
        return (tags, text);
    }

    static Task<HttpResponseMessage> SendChat(HttpClient c, HttpRequestMessage req)
    {
        req.Headers.Add("X-Panel", "1");
        return c.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
    }

    async Task SayToJun(string message)
    {
        if (busy) return;
        busy = true; SetBubble("...", true, 0);
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, S.Url + "/api/chat") { Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { message }), Encoding.UTF8, "application/json") };
            using (var chat = new HttpClient { Timeout = TimeSpan.FromMinutes(3) })
            using (var r = await SendChat(chat, req))
            using (var st = await r.Content.ReadAsStreamAsync())
            {
                var dec = Encoding.UTF8.GetDecoder(); var buf = new byte[4096]; var chars = new char[4096]; var sb = new StringBuilder(); int n;
                while ((n = await st.ReadAsync(buf, 0, buf.Length)) > 0)
                {
                    int cn = dec.GetChars(buf, 0, n, chars, 0); sb.Append(chars, 0, cn);
                    string acc = sb.ToString();
                    if (Regex.IsMatch(acc, @"^\s*\[[^\]]*$")) continue;           // a tag is still arriving
                    if (Regex.IsMatch(acc, @"^\s*\[(busy|error|no reply)\]", RegexOptions.IgnoreCase)) continue;
                    var s = Split(acc); if (s.text.Length > 0) BeginInvoke(new Action(() => SetBubble(s.text, true, 0)));
                }
                string all = sb.ToString();
                if (Regex.IsMatch(all, @"^\s*\[busy\]", RegexOptions.IgnoreCase)) { BeginInvoke(new Action(() => SetBubble("(she is still thinking, try again in a moment)", false, 3.5))); busy = false; return; }
                if (Regex.IsMatch(all, @"^\s*\[(error|no reply)\]", RegexOptions.IgnoreCase)) { BeginInvoke(new Action(() => SetBubble("(she cannot answer right now)", false, 3.5))); busy = false; return; }
                var f = Split(all);
                if (f.tags.Length > 0) React(f.tags);
                string shown = f.text.Length > 0 ? f.text : all.Trim();
                BeginInvoke(new Action(() => SetBubble(shown.Length > 0 ? shown : "...", false, 4 + shown.Length * 0.055)));
            }
        }
        catch { BeginInvoke(new Action(() => SetBubble("(I cannot reach the game)", false, 3))); }
        busy = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { try { cts.Cancel(); } catch { } if (surf != null) surf.Dispose(); if (pet != null) pet.Dispose(); }
        base.Dispose(disposing);
    }
}
