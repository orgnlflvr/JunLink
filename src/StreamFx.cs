#if MELON
using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;
using TMP = Il2CppTMPro;

// ---- Stream extras (Jun Link 1.3): studio backgrounds and the Fishing scene ----
// Everything here is drawn by the mod (own pixel art). Your own pictures for the background can go in UserData\JunLink\stream_backgrounds (.png / .jpg).
public static class StreamFx
{
    // ================= backgrounds =================
    static readonly List<KeyValuePair<string, Texture2D>> bgs = new List<KeyValuePair<string, Texture2D>>();
    static int bgIndex;
    static RawImage bgImg; static TMP.TextMeshProUGUI bgLabel;

    static Texture2D NewTex(int w, int h) { var t = new Texture2D(w, h, TextureFormat.RGBA32, false); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; t.hideFlags = HideFlags.DontUnloadUnusedAsset; return t; }
    static Color Mix(Color a, Color b, float t) { return Color.Lerp(a, b, Mathf.Clamp01(t)); }
    static Color Hex(string h) { Color c; return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.magenta; }

    static readonly List<KeyValuePair<string, Texture2D>> made = new List<KeyValuePair<string, Texture2D>>();
    static Texture2D apartment;
    public static string Folder { get { return Path.Combine(Host.RootPath, "stream_backgrounds"); } }

    // the list for this stream: your apartment (a fresh picture of the room), the drawn ones, then your own pictures (read again every stream)
    static void MakeBackgrounds()
    {
        string keep = bgs.Count > 0 && bgIndex < bgs.Count ? bgs[bgIndex].Key : "Your apartment";
        MakeDrawn();
        bgs.Clear();
        try { var shot = PetView.RoomShot(1280, 720); if (shot != null) { if (apartment != null) UnityEngine.Object.Destroy(apartment); apartment = shot; } } catch (Exception ex) { Cfg.Log.LogWarning("Stream: room picture failed (" + ex.Message + ")"); }
        if (apartment != null) bgs.Add(new KeyValuePair<string, Texture2D>("Your apartment", apartment));
        bgs.AddRange(made);
        LoadOwn();
        bgIndex = Math.Max(0, bgs.FindIndex(x => x.Key == keep));
        Cfg.Log.LogInfo("Stream: " + bgs.Count + " backgrounds (" + (apartment != null ? "with" : "without") + " your apartment)");
    }

    static void MakeDrawn()
    {
        if (made.Count > 0) return;
        var bgs = made;
        const int W = 320, H = 180;
        var r = new System.Random(11);

        // cozy room: warm wall, a window with the night outside, string lights, a shelf
        var a = NewTex(W, H);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            Color c = Mix(Hex("#3a2141"), Hex("#6b3a4f"), y / (float)H);
            if (y < 34) c = Mix(Hex("#2a1720"), Hex("#3b2029"), y / 34f);                                  // floor
            if (x > 210 && x < 290 && y > 70 && y < 150)                                                   // window
            {
                c = Mix(Hex("#141a3a"), Hex("#2d2a5c"), (y - 70) / 80f);
                if (x == 250 || y == 110) c = Hex("#2a1a24");
                if ((x - 272) * (x - 272) + (y - 135) * (y - 135) < 30) c = Hex("#f4e9c0");               // moon
                if (r.Next(140) == 0) c = Color.white;
            }
            if ((x == 210 || x == 290) && y >= 70 && y <= 150 || (y == 70 || y == 150) && x >= 210 && x <= 290) c = Hex("#2a1a24");
            if (y > 92 && y < 96 && x > 30 && x < 120) c = Hex("#2a1a24");                                // shelf
            if (y >= 96 && y < 112 && x > 40 && x < 112 && (x / 9) % 2 == 0) c = Hex((x / 9) % 4 == 0 ? "#c7556b" : "#5a8fb0");   // books
            float wave = 160f + 6f * Mathf.Sin(x / 14f);                                                    // string lights
            if (Mathf.Abs(y - wave) < 1f) c = Hex("#1d1018");
            if (x % 16 == 8 && Mathf.Abs(y - wave + 3) < 2f) c = Hex(((x / 16) % 3) == 0 ? "#ffd56b" : ((x / 16) % 3) == 1 ? "#ff8fb1" : "#9fe3ff");
            a.SetPixel(x, y, c);
        }
        a.Apply(); bgs.Add(new KeyValuePair<string, Texture2D>("Cozy room", a));

        // night city: sky, stars, skyline with lit windows, a neon sign
        var b = NewTex(W, H);
        int[] tops = new int[W]; int bx = 0; while (bx < W) { int bw = 14 + r.Next(26), bh = 40 + r.Next(80); for (int i = bx; i < Math.Min(W, bx + bw); i++) tops[i] = bh; bx += bw + r.Next(4); }
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            Color c = Mix(Hex("#0b0f2a"), Hex("#3a1a52"), 1f - y / (float)H);
            if (y > 120 && r.Next(220) == 0) c = Color.white;
            if (y < tops[x]) { c = Hex("#120c1e"); if (x % 6 < 3 && y % 8 < 4 && y > 8 && r.Next(5) < 2) c = Hex(r.Next(3) == 0 ? "#ffb35c" : "#ffe08a"); }
            if (y > 95 && y < 108 && x > 40 && x < 92) c = (y == 96 || y == 107 || x == 41 || x == 91) ? Hex("#ff3d8a") : Hex("#120c1e");
            b.SetPixel(x, y, c);
        }
        b.Apply(); bgs.Add(new KeyValuePair<string, Texture2D>("Night city", b));

        // synthwave: sunset, striped sun, grid floor
        var s = NewTex(W, H);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            Color c;
            if (y >= 60)
            {
                c = Mix(Hex("#ff7a59"), Hex("#2b0f4a"), (y - 60) / 120f);
                float dx = x - 160, dy = y - 70; if (dx * dx + dy * dy < 50 * 50 && !(y < 100 && y % 8 < 3)) c = Mix(Hex("#ffe36b"), Hex("#ff4f8b"), (100 - y) / 40f);
            }
            else
            {
                c = Hex("#1a0b2e");
                float depth = 60f / (61 - y);                                                               // perspective grid
                if (((int)(depth * 6)) % 6 == 0) c = Hex("#ff3dd2");
                float gx = (x - 160) / (float)(61 - y) * 6f; if (Mathf.Abs(gx - Mathf.Round(gx)) < 0.06f * (61 - y) / 30f + 0.02f) c = Hex("#ff3dd2");
            }
            s.SetPixel(x, y, c);
        }
        s.Apply(); bgs.Add(new KeyValuePair<string, Texture2D>("Synthwave", s));

        // pastel: soft gradient with dots
        var p = NewTex(W, H);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            Color c = Mix(Hex("#f6c7d8"), Hex("#c9c4f5"), (x + y) / (float)(W + H));
            if ((x % 20 - 10) * (x % 20 - 10) + (y % 20 - 10) * (y % 20 - 10) < 6 && ((x / 20 + y / 20) % 2 == 0)) c = Hex("#fff3f8");
            p.SetPixel(x, y, c);
        }
        p.Apply(); bgs.Add(new KeyValuePair<string, Texture2D>("Pastel", p));

        // plain studio
        var q = NewTex(4, 4); for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) q.SetPixel(x, y, Hex(y < 1 ? "#26162f" : "#1c1126")); q.Apply();
        bgs.Add(new KeyValuePair<string, Texture2D>("Plain", q));

    }

    static void LoadOwn()
    {
        try
        {
            string dir = Folder;
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant(); if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;
                var data = File.ReadAllBytes(f);
                var arr = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(data.Length); for (int i = 0; i < data.Length; i++) arr[i] = data[i];
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, false); t.hideFlags = HideFlags.DontUnloadUnusedAsset;
                if (ImageConversion.LoadImage(t, arr)) bgs.Add(new KeyValuePair<string, Texture2D>(Path.GetFileNameWithoutExtension(f), t));
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: own backgrounds could not be read (" + ex.Message + ")"); }
    }

    public static Texture CurrentTex { get { return bgs.Count > 0 && bgIndex < bgs.Count ? bgs[bgIndex].Value : null; } }
    public static void OpenFolder()
    {
        try { Directory.CreateDirectory(Folder); Application.OpenURL("file:///" + Folder.Replace('\\', '/')); Cfg.Log.LogInfo("Stream: opened " + Folder); }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not open the folder (" + ex.Message + ")"); }
    }

    public static void BuildBackground(RectTransform video)
    {
        MakeBackgrounds();
        bgImg = StreamMode.R("background", video, 1280f, 720f, 0f, 0f).gameObject.AddComponent<RawImage>(); bgImg.raycastTarget = false;
        if (bgIndex >= bgs.Count) bgIndex = 0;
        bgImg.texture = bgs[bgIndex].Value;
    }
    public static void SetLabel(TMP.TextMeshProUGUI t) { bgLabel = t; Label(); }
    static bool fishing;
    static void Label()
    {
        try
        {
            if (bgLabel == null) return;
            if (fishing) bgLabel.text = "<b>Spot: " + SpotName + "</b>";
            else if (bgs.Count > 0) bgLabel.text = "<b>BG: " + bgs[bgIndex].Key + "</b>";
        }
        catch { }
    }
    // in the Fishing scene the button changes the fishing spot instead
    public static void NextBackground()
    {
        if (fishing) { NextSpot(); return; }
        if (bgs.Count == 0) return;
        bgIndex = (bgIndex + 1) % bgs.Count;
        try { if (bgImg != null) bgImg.texture = bgs[bgIndex].Value; } catch { }
        Label();
        Cfg.Log.LogInfo("Stream: background " + bgs[bgIndex].Key);
    }

    // ---- fishing spots: the game's own fishing places (their pictures are read from the game while it runs, nothing is copied) ----
    class Spot { public string Name; public Sprite Sprite; }
    static readonly List<Spot> spots = new List<Spot>(); static int spotIndex; static RawImage lakeImg; static GameObject drawnDock;
    public static string SpotName { get { return spots.Count > 0 ? spots[spotIndex].Name : "Lake"; } }
    // The game's fishing-joint picture (Sunny Beach), through its generated sprite list (the same way Story Creator reads the game's music).
    // A marker file guards against a crash: it is written before the game is asked and removed after; if it is still there
    // at the next try, the game's pictures are skipped and the drawn lake is used.
    static bool spotsLoaded;
    static string GuardFile { get { return Path.Combine(Host.RootPath, "stream_spots.lock"); } }
    static void LoadSpots()
    {
        if (spotsLoaded) return;
        spotsLoaded = true; spots.Clear();
        bool skip = false;
        try { skip = File.Exists(GuardFile); } catch { }
        if (skip) Cfg.Log.LogWarning("Stream: the game's fishing pictures crashed last time, using the drawn lake (delete " + GuardFile + " to try again)");
        else
        {
            try { File.WriteAllText(GuardFile, "reading the game's fishing pictures"); } catch { }
            try
            {
                var fb = Il2Cpp.SpriteTextureDataGenerated.Instance.FishingBackgrounds;
                AddSpot("Sunny Beach", () => fb.FishingJoint_SunnyBeach.Get());
            }
            catch (Exception ex) { Cfg.Log.LogWarning("Stream: the game's fishing pictures could not be read (" + ex.Message + ")"); }
            try { File.Delete(GuardFile); } catch { }
        }
        spots.Add(new Spot { Name = "Lake (drawn)", Sprite = null });
        if (spotIndex >= spots.Count) spotIndex = 0;
        Cfg.Log.LogInfo("Stream: " + (spots.Count - 1) + " fishing spots from the game");
    }
    static void AddSpot(string name, Func<Sprite> get)
    {
        try { var sp = get(); if (sp != null) { sp.hideFlags = HideFlags.DontUnloadUnusedAsset; spots.Add(new Spot { Name = name, Sprite = sp }); } }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: fishing spot " + name + " failed (" + ex.Message + ")"); }
    }
    static void NextSpot() { if (spots.Count == 0) return; spotIndex = (spotIndex + 1) % spots.Count; ShowSpot(); Label(); Cfg.Log.LogInfo("Stream: fishing spot " + SpotName); }
    static void ShowSpot()
    {
        try
        {
            if (lakeImg == null) return;
            var sp = spots.Count > 0 ? spots[spotIndex].Sprite : null;
            if (sp == null) { lakeImg.texture = Lake(); lakeImg.uvRect = new UnityEngine.Rect(0f, 0f, 1f, 1f); if (drawnDock != null) drawnDock.SetActive(true); return; }
            var tex = sp.texture; var r = sp.textureRect;
            float tw = tex.width, th = tex.height;
            // fill the 16:9 video: crop the picture's longer side, keep the middle
            float want = 1280f / 720f, have = r.width / r.height;
            float w = r.width, h = r.height, x = r.x, y = r.y;
            if (have > want) { w = r.height * want; x += (r.width - w) * 0.5f; } else { h = r.width / want; y += (r.height - h) * 0.5f; }
            lakeImg.texture = tex; lakeImg.uvRect = new UnityEngine.Rect(x / tw, y / th, w / tw, h / th);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: fishing spot picture failed (" + ex.Message + ")"); }
    }
    public static int Catches { get { return catches; } }
    public static string Best { get { return best.Length > 0 ? best + " " + bestCm + " cm" : ""; } }

    // ================= fishing =================
    class Fish { public string Name, Body, Belly, Fin; public int Min, Max, Weight; public bool Junk; }
    static readonly Fish[] Fishes = {
        new Fish { Name = "Minnow", Body = "#b8c4d0", Belly = "#e8eef3", Fin = "#8a97a6", Min = 4, Max = 9, Weight = 28 },
        new Fish { Name = "Perch", Body = "#9fb83c", Belly = "#f0e38a", Fin = "#e0732c", Min = 12, Max = 30, Weight = 24 },
        new Fish { Name = "Bass", Body = "#4f7f3a", Belly = "#d6e2a8", Fin = "#3b5e2a", Min = 25, Max = 55, Weight = 18 },
        new Fish { Name = "Catfish", Body = "#6b5f57", Belly = "#c9bba8", Fin = "#4b423c", Min = 40, Max = 90, Weight = 12 },
        new Fish { Name = "Rainbow Trout", Body = "#7f9c8f", Belly = "#ffd2dc", Fin = "#e05a8a", Min = 30, Max = 60, Weight = 9 },
        new Fish { Name = "Golden Koi", Body = "#f2b632", Belly = "#fff0b8", Fin = "#e0662c", Min = 50, Max = 80, Weight = 3 },
        new Fish { Name = "Old Boot", Junk = true, Weight = 6 },
    };
    static readonly string[] FishArt = {
        "......ff........",
        "....bbbbbb...f..",
        "..bbbbbbbbbb.ff.",
        ".bkbbbbbbbbbbff.",
        "bbbbbbbbbbbbbfff",
        ".wwwwwwwwwwwbff.",
        "..wwwwwwwwww.ff.",
        "....wwwwww...f..",
        "......ff........",
    };
    static readonly string[] BootArt = {
        "..dddddd....",
        "..dbbbbd....",
        "..dbbbbd....",
        "..dbbbbd....",
        "..dbbbbd....",
        "..dbbbbbddd.",
        ".dbbbbbbbbbd",
        ".dbbbbbbbbbd",
        "dlllllllllld",
        "dddddddddddd",
    };
    static readonly string[] BobberArt = { "..rr..", ".rrrr.", "rrrrrr", "wwwwww", ".wwww.", "..ww..", "..dd..", "..dd.." };

    static Sprite Pixel(string[] rows, Func<char, Color> pal)
    {
        int h = rows.Length, w = rows[0].Length; var t = NewTex(w, h);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) t.SetPixel(x, h - 1 - y, x < rows[y].Length ? pal(rows[y][x]) : new Color(0f, 0f, 0f, 0f));
        t.Apply();
        var s = Sprite.Create(t, new UnityEngine.Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f); s.hideFlags = HideFlags.DontUnloadUnusedAsset; return s;
    }
    static readonly Dictionary<string, Sprite> fishSprites = new Dictionary<string, Sprite>();
    static Sprite FishSprite(Fish f)
    {
        Sprite s;
        if (fishSprites.TryGetValue(f.Name, out s)) return s;
        if (f.Junk) s = Pixel(BootArt, c => c == 'd' ? Hex("#2a1d14") : c == 'b' ? Hex("#6b4a2f") : c == 'l' ? Hex("#a37a4e") : new Color(0f, 0f, 0f, 0f));
        else s = Pixel(FishArt, c => c == 'b' ? Hex(f.Body) : c == 'w' ? Hex(f.Belly) : c == 'f' ? Hex(f.Fin) : c == 'k' ? Hex("#111111") : new Color(0f, 0f, 0f, 0f));
        fishSprites[f.Name] = s; return s;
    }

    static Texture2D lakeTex;
    static Texture2D Lake()
    {
        if (lakeTex != null) return lakeTex;
        const int W = 320, H = 180; var t = NewTex(W, H); var r = new System.Random(5);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            Color c;
            float hill = 112f + 10f * Mathf.Sin(x / 23f) + 6f * Mathf.Sin(x / 9f);
            if (y > hill) c = Mix(Hex("#ffb38a"), Hex("#6a7fd0"), (y - 110) / 70f);                              // sky
            else if (y > 104) c = Mix(Hex("#2f5a4a"), Hex("#1f3f36"), (hill - y) / 12f);                        // hills
            else
            {
                c = Mix(Hex("#3b7ea8"), Hex("#1b3f66"), 1f - y / 104f);                                        // water
                if ((y + (x / 18)) % 13 == 0 && r.Next(3) > 0) c = Mix(c, Color.white, 0.25f);
            }
            if (x < 70 && y >= 22 && y < 30) c = ((x / 7) % 2 == 0) ? Hex("#7a5232") : Hex("#6a462a");          // dock planks
            if (x < 70 && y < 22 && (x % 22 == 4 || x % 22 == 5)) c = Hex("#4a301c");                            // dock posts
            t.SetPixel(x, y, c);
        }
        t.Apply(); lakeTex = t; return t;
    }

    static GameObject gameRoot; static RectTransform bobber, line, zone, marker, reelBar, catchRt; static Image catchImg;
    static TMP.TextMeshProUGUI prompt, hud, bang;
    static Il2CppSystem.Object clickAction; static volatile bool clicked;
    static int state; static float stateAt, biteAt, markerX, markerDir = 1f, markerSpeed;
    static Vector2 bobTarget; static int hits, misses, catches; static string best = ""; static int bestCm;
    static readonly System.Random rnd = new System.Random();
    static Fish hooked; static int hookedCm;
    static readonly Vector2 RodTip = new Vector2(-420f, 120f);

    public static void BuildGame(RectTransform video, TMP.TMP_FontAsset font)
    {
        gameRoot = StreamMode.R("fishing", video, 1280f, 720f, 0f, 0f).gameObject;
        var lake = StreamMode.R("lake", gameRoot.transform, 1280f, 720f, 0f, 0f).gameObject.AddComponent<RawImage>(); lake.texture = Lake(); lake.raycastTarget = true;   // clicks land here
        lakeImg = lake;
        var btn = lake.gameObject.AddComponent<Button>(); btn.targetGraphic = lake; btn.transition = Selectable.Transition.None;
        var ua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() => clicked = true)); btn.onClick.AddListener(ua); clickAction = ua;
        // rod (from the dock) and line
        var rod = StreamMode.Box("rod", gameRoot.transform, 8f, 240f, -480f, 20f, Hex("#3a2614")); rod.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        line = StreamMode.Box("line", gameRoot.transform, 3f, 10f, 0f, 0f, new Color(1f, 1f, 1f, 0.8f)).rectTransform; line.pivot = new Vector2(0.5f, 0f);
        var bimg = StreamMode.R("bobber", gameRoot.transform, 36f, 48f, 0f, 0f).gameObject.AddComponent<Image>(); bimg.raycastTarget = false;
        bimg.sprite = Pixel(BobberArt, c => c == 'r' ? Hex("#e8294d") : c == 'w' ? Color.white : c == 'd' ? Hex("#222222") : new Color(0f, 0f, 0f, 0f));
        bobber = bimg.rectTransform;
        bang = StreamMode.Text(gameRoot.transform, font, "<b>!</b>", 70f, 80f, 90f, 0f, 0f, TMP.TextAlignmentOptions.Center); bang.color = Hex("#ffd84d");
        // reel bar
        reelBar = StreamMode.Box("reel", gameRoot.transform, 600f, 40f, -250f, -275f, new Color(0.08f, 0.05f, 0.12f, 0.9f)).rectTransform;
        zone = StreamMode.Box("zone", reelBar, 110f, 34f, 0f, 0f, Hex("#3ccf6e")).rectTransform;
        marker = StreamMode.Box("marker", reelBar, 8f, 54f, 0f, 0f, Color.white).rectTransform;
        // texts
        prompt = StreamMode.Text(gameRoot.transform, font, "", 28f, 760f, 50f, -230f, 268f, TMP.TextAlignmentOptions.Left);
        prompt.outlineWidth = 0.2f; prompt.outlineColor = new Color32(20, 10, 30, 255);
        hud = StreamMode.Text(gameRoot.transform, font, "", 22f, 600f, 40f, -320f, 320f, TMP.TextAlignmentOptions.Left);
        hud.outlineWidth = 0.2f; hud.outlineColor = new Color32(20, 10, 30, 255);
        catchImg = StreamMode.R("catch", gameRoot.transform, 320f, 180f, -120f, 60f).gameObject.AddComponent<Image>(); catchImg.raycastTarget = false; catchRt = catchImg.rectTransform;
        catches = 0; best = ""; bestCm = 0;
        SetState(0);
        gameRoot.SetActive(false);
    }

    public static void Show(bool on)
    {
        if (on) { LoadSpots(); ShowSpot(); }                    // the game's pictures are only read when Fishing is first opened
        fishing = on; Label();
        try { if (gameRoot != null) gameRoot.SetActive(on); } catch { }
        if (on) SetState(0);
    }
    public static void Forget() { gameRoot = null; bobber = line = zone = marker = reelBar = catchRt = null; catchImg = null; prompt = hud = bang = null; clickAction = null; bgImg = null; bgLabel = null; lakeImg = null; drawnDock = null; fishing = false; }

    static void SetState(int s)
    {
        state = s; stateAt = Time.unscaledTime;
        try
        {
            bool reel = s == 4;
            reelBar.gameObject.SetActive(reel);
            bobber.gameObject.SetActive(s >= 1 && s <= 4);
            line.gameObject.SetActive(s >= 1 && s <= 4);
            bang.gameObject.SetActive(s == 3);
            catchImg.gameObject.SetActive(s == 5 && hooked != null && hooked != null);
            if (s == 0) prompt.text = "Click the lake to cast!";
            if (s == 2) prompt.text = "<alpha=#AA>waiting for a bite...";
            if (s == 3) prompt.text = "<color=#ffd84d>BITE! Click!</color>";
            if (s == 4) { hits = misses = 0; markerX = -300f; markerDir = 1f; markerSpeed = 520f + (hooked != null && !hooked.Junk ? hookedCm * 6f : 0f); MoveZone(); prompt.text = "Reel it in! Click in the green (0/3)"; }
        }
        catch { }
        Hud();
    }

    static void MoveZone() { try { zone.anchoredPosition = new Vector2(UnityEngine.Random.Range(-230f, 230f), 0f); } catch { } }
    static void Hud() { try { if (hud != null) hud.text = "<b>Fishing</b>   catches: " + catches + (best.Length > 0 ? "   best: " + best + " " + bestCm + " cm" : ""); } catch { } }

    static Fish Roll()
    {
        int total = 0; foreach (var f in Fishes) total += f.Weight;
        int n = rnd.Next(total);
        foreach (var f in Fishes) { if (n < f.Weight) return f; n -= f.Weight; }
        return Fishes[0];
    }

    // called every frame by StreamMode while the Fishing scene is on
    public static void Tick(float now)
    {
        if (gameRoot == null) return;
        bool click = clicked; clicked = false;
        float t = now - stateAt;
        try
        {
            if (state >= 1 && state <= 4)
            {
                Vector2 pos;
                if (state == 1) { float k = Mathf.Clamp01(t / 0.6f); pos = Vector2.Lerp(RodTip, bobTarget, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 120f); }
                else if (state == 3) pos = bobTarget + new Vector2(0f, -10f + Mathf.Sin(now * 30f) * 6f);
                else if (state == 4) pos = bobTarget + new Vector2(Mathf.Sin(now * 9f) * 14f, -8f);
                else pos = bobTarget + new Vector2(0f, Mathf.Sin(now * 3f) * 4f);
                bobber.anchoredPosition = pos;
                Vector2 d = pos - RodTip; line.anchoredPosition = RodTip; line.sizeDelta = new Vector2(3f, d.magnitude);
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
                bang.rectTransform.anchoredPosition = pos + new Vector2(0f, 70f);
            }
            switch (state)
            {
                case 0:
                    if (click) { bobTarget = new Vector2(UnityEngine.Random.Range(-330f, 120f), UnityEngine.Random.Range(-170f, -40f)); SetState(1); }
                    break;
                case 1:
                    if (t >= 0.6f) { biteAt = now + UnityEngine.Random.Range(2f, 6f); SetState(2); }
                    break;
                case 2:
                    if (now >= biteAt) { hooked = Roll(); hookedCm = hooked.Junk ? 0 : rnd.Next(hooked.Min, hooked.Max + 1); SetState(3); }
                    break;
                case 3:
                    if (click) SetState(4);
                    else if (t > 0.9f) Result(false, "Too slow... it got away.");
                    break;
                case 4:
                    markerX += markerDir * markerSpeed * Time.unscaledDeltaTime;
                    if (markerX > 290f) { markerX = 290f; markerDir = -1f; } else if (markerX < -290f) { markerX = -290f; markerDir = 1f; }
                    marker.anchoredPosition = new Vector2(markerX, 0f);
                    if (click)
                    {
                        if (Mathf.Abs(markerX - zone.anchoredPosition.x) <= 55f) { hits++; MoveZone(); markerSpeed *= 1.12f; }
                        else misses++;
                        prompt.text = "Reel it in! Click in the green (" + hits + "/3)" + (misses > 0 ? "   <color=#ff6b6b>slips " + misses + "/3</color>" : "");
                        if (hits >= 3) Result(true, null);
                        else if (misses >= 3) Result(false, "The line snapped... it got away.");
                    }
                    break;
                case 5:
                    if (t > 2.8f || (click && t > 0.8f)) SetState(0);
                    break;
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: fishing tick failed (" + ex.Message + ")"); SetState(0); }
    }

    static void Result(bool caught, string failText)
    {
        if (!caught || hooked == null)
        {
            hooked = null; SetState(5); prompt.text = failText ?? "It got away...";
            StreamMode.GameEvent(false, "", 0);
            return;
        }
        var f = hooked; catches++;
        if (!f.Junk && hookedCm > bestCm) { bestCm = hookedCm; best = f.Name; }
        SetState(5);
        try
        {
            catchImg.sprite = FishSprite(f);
            catchRt.sizeDelta = f.Junk ? new Vector2(180f, 150f) : new Vector2(320f, 180f);
            catchImg.gameObject.SetActive(true);
        }
        catch { }
        prompt.text = f.Junk ? "An old boot... lol" : (f.Name == "Golden Koi" ? "<color=#ffd84d>WOW! A Golden Koi!</color> " : "Caught a ") + (f.Junk ? "" : f.Name + "! " + hookedCm + " cm");
        Hud();
        StreamMode.GameEvent(true, f.Junk ? "old boot" : f.Name, hookedCm);
        hooked = null;
    }
}
#endif
