#if MELON
using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;
using Il2Cpp;
using TMP = Il2CppTMPro;

// ---- Stream mode (Jun Link 1.3): Jun streams as a VTuber inside the game ----
// Anon's PC desktop gets a "streamhub.ic" shortcut; the page opens inside the game's own browser (title, description,
// stream rules, Go Live). Going live returns to the room (the game only draws her there) and opens the studio overlay:
// video with scenes (Chatting, Close-up, Fishing, Mod Duty, BRB), live chat with AI chatters, donations, superchats,
// subscribers, VIP and guest visits, a stats menu, and an end-of-stream summary that goes into her memory.
// SFW and separate from the game's own (lewd) stream; nothing of the game's stream system is touched. Viewers and chat are made up;
// the donations are paid out as in-game money once per in-game day (Stream/Payout, with the platform's cut and a daily cap).
// Switch it off with Stream/Enabled = false.

public static class StreamMode
{
    public static bool Live;
    static bool patched;
    static readonly List<Il2CppSystem.Action> keep = new List<Il2CppSystem.Action>();   // callbacks the game holds (so they are not collected)
    static volatile bool endWanted;
    public static string Title = "", Description = "";
    static float startAt = -1f, startGiveUp;
    // streamhub.ic rule: politely dressed. Uses the game's own idea of "covered" (the same stats it uses for going outside).
    internal static bool dressLogged;
    public static string DressProblem()
    {
        try
        {
            var gv = GameScript.Instance != null ? GameScript.Instance.GameVariables : null;
            if (gv == null) return null;
            if (gv.IsNaked) return Bot() + " isn't wearing clothes";
            if (gv.IsThereCumOnHer) return Bot() + " needs to freshen up first";
            var es = gv.GetGameStats() != null ? gv.GetGameStats().EquipmentGameStats : null;
            if (es != null)
            {
                if (!dressLogged)
                {
                    dressLogged = true;
                    Cfg.Log.LogInfo("Stream: outfit cover: upper " + es.GetCover(SusArea.UpperBody).ToString("0.00") + ", chest " + es.GetCover(SusArea.Boobs).ToString("0.00") + ", lower " + es.GetCover(SusArea.LowerBody).ToString("0.00") + ", crotch " + es.GetCover(SusArea.Pussy).ToString("0.00") + ", clothed " + es.WouldBeConsideredClothed());
                }
                // no top/bottom at all, or clothes on but showing too much (the game counts that outfit as "clothed", the stream does not)
                bool topOn = es.IsCovered(SusArea.UpperBody), bottomOn = es.IsCovered(SusArea.LowerBody);
                if (!topOn && !bottomOn) return Bot() + " needs a top and a bottom on";
                if (!topOn) return Bot() + " needs a top on";
                if (!bottomOn) return Bot() + " needs something on her bottom half";
                // chest/crotch cover comes from the bra and panties; under a dress or a top and skirt those don't matter for the stream,
                // so only the outer clothes (top and bottom) count. The two values stay in the log above.
                if (!es.WouldBeConsideredClothed()) return Bot() + " needs proper clothes, not just underwear";
            }
        }
        catch (Exception ex) { if (!dressLogged) { dressLogged = true; Cfg.Log.LogWarning("Stream: could not check her outfit (" + ex.Message + "), allowing the stream"); } }
        return null;
    }

    public static void RequestStart() { startAt = Time.unscaledTime + 1.2f; startGiveUp = Time.unscaledTime + 8f; }
    static float liveSince;

    // ---------- hooks: the streamhub.ic icon on Anon's PC desktop, and the game's own lines while live ----------
    public static void Init()
    {
        if (patched || !Mod.Chat) return;
        patched = true;
        var h = new HarmonyLib.Harmony("orgnlflvr.junlink.stream");
        try
        {
            var pc = AccessTools.Method(typeof(PCStaticGui), "OnEnable");
            if (pc != null) { h.Patch(pc, postfix: new HarmonyMethod(typeof(StreamSite), nameof(StreamSite.DesktopPostfix))); Cfg.Log.LogInfo("Stream: streamhub.ic icon hooked to the PC desktop"); }
            else Cfg.Log.LogWarning("Stream: PCStaticGui.OnEnable not found, so there is no streamhub.ic icon");
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not hook the PC desktop (" + ex.Message + ")"); }
        try
        {
            var say = AccessTools.Method(typeof(Il2CppFungus.SayDialog), "Say");
            if (say != null) h.Patch(say, prefix: new HarmonyMethod(typeof(StreamMode), nameof(SayPrefix)));
            else Cfg.Log.LogWarning("Stream: SayDialog.Say not found, so the game's own lines are not held back during a stream");
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not hook SayDialog.Say (" + ex.Message + ")"); }
    }

    // ---------- while live, the game's own lines stay quiet (Jun Link's and Story Creator's own lines still show) ----------
    static readonly List<KeyValuePair<string, float>> mine = new List<KeyValuePair<string, float>>();
    static int heldBack;
    public static void Mine(string t)
    {
        if (string.IsNullOrEmpty(t)) return;
        mine.Add(new KeyValuePair<string, float>(Key(t), Time.unscaledTime));
        if (mine.Count > 30) mine.RemoveAt(0);
    }
    static string Key(string t)
    {
        t = System.Text.RegularExpressions.Regex.Replace(t ?? "", @"<[^>]*>|\{[^}]*\}", "").Trim();
        return t.Length > 40 ? t.Substring(0, 40) : t;
    }
    static bool IsMine(string t)
    {
        string k = Key(t); if (k.Length == 0) return true;   // empty / clearing the box: let it through
        foreach (var m in mine) if (Time.unscaledTime - m.Value < 120f && (m.Key.StartsWith(k) || k.StartsWith(m.Key))) return true;
        return false;
    }
    // Say(text, isNewText, clearPrevious, waitForInput, fadeWhenDone, onComplete): skipped lines still call onComplete, so the game never waits for them
    static bool SayPrefix(string __0, Il2CppSystem.Action __5, ref Coroutine __result)
    {
        try
        {
            if (!Live || CrossMod.OtherStoryRunning || IsMine(__0)) return true;
            heldBack++;
            if (heldBack <= 20) Cfg.Log.LogInfo("Stream: held back a game line while live: " + Key(__0));
            __result = null;
            try { if (__5 != null) __5.Invoke(); } catch { }
            return false;
        }
        catch { return true; }
    }

    // ---------- every frame (from the behaviour's Update) ----------
    public static void Tick()
    {
        if (!Mod.Chat) return;
        if (!patched) Init();
        StreamSite.Tick();
        StreamLog.Tick();
        if (startAt > 0f)
        {
            PetView.Touch(); PetView.StreamHi = true;
            float now = Time.unscaledTime;
            if (now >= startAt && (PetView.Tex != null || now > startGiveUp)) { startAt = -1f; GoLive(); }
        }
        if (askEnd) { askEnd = false; try { if (confirmBox != null) confirmBox.SetActive(true); } catch { } }
        if (keepStreaming) { keepStreaming = false; try { if (confirmBox != null) confirmBox.SetActive(false); } catch { } }
        if (summaryWanted) { summaryWanted = false; ShowSummary(); }
        if (fadeWanted) { fadeWanted = false; StartFade(); }
        TickFade();
        if (endWanted) { endWanted = false; EndStream(); }
        if (Live) TickOverlay();
    }

    static void GoLive()
    {
        if (Live) return;
        try
        {
            BuildOverlay(); faceLogged = false; Live = true; PetView.StreamHi = true; InGameChat.StreamLive = true; liveSince = Time.unscaledTime; heldBack = 0; ResetStream(); Cfg.Log.LogInfo("Stream: live");
            try { var d = Il2CppFungus.SayDialog.ActiveSayDialog; if (d != null && d.gameObject.activeInHierarchy) { d.Stop(); d.gameObject.SetActive(false); } } catch { }   // close whatever the game was saying
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not go live (" + ex.Message + ")"); DestroyOverlay(); }
    }

    public static void EndStream()
    {
        if (!Live) return;
        SaveChannel();
        Live = false; PetView.StreamHi = false; InGameChat.StreamLive = false; InGameChat.StreamPlain = false;
        ShowGameUi();
        DestroyOverlay();
        Cfg.Log.LogInfo("Stream: ended after " + Mathf.RoundToInt(Time.unscaledTime - liveSince) + " s, peak " + peakViewers + " viewers, " + donations + " donations, " + superchats + " superchats ($" + donated + " in all), " + newSubs + " new subscribers (" + subs + " now), " + Mathf.FloorToInt(likes) + " likes, " + heldBack + " game lines held back, " + answers + " answers from " + Bot() + ", " + aiBatches + " AI chat batches");
    }

    // ---------- overlay (step 1: badge, viewers, timer, empty chat, End stream button) ----------
    static GameObject root; static TMP.TextMeshProUGUI viewersText, chatText, alertText, faceStatus; static Image alertBox; static RawImage faceImg; static bool faceLogged;
    static Il2CppSystem.Object endAction;

    internal static RectTransform R(string name, Transform parent, float w, float h, float x, float y)
    {
        var go = new GameObject(name); var rt = go.AddComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, y);
        return rt;
    }
    internal static Image Box(string name, Transform parent, float w, float h, float x, float y, Color c)
    {
        var im = R(name, parent, w, h, x, y).gameObject.AddComponent<Image>(); im.color = c; im.raycastTarget = false; return im;
    }
    internal static TMP.TextMeshProUGUI Text(Transform parent, TMP.TMP_FontAsset font, string s, float size, float w, float h, float x, float y, TMP.TextAlignmentOptions al)
    {
        var tx = R("text", parent, w, h, x, y).gameObject.AddComponent<TMP.TextMeshProUGUI>();
        if (font != null) tx.font = font;
        tx.text = s; tx.fontSize = size; tx.color = Color.white; tx.alignment = al; tx.raycastTarget = false; tx.enableWordWrapping = true;
        return tx;
    }
    internal static TMP.TMP_FontAsset Font()
    {
        try
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP.TextMeshProUGUI>()) try { if (t.font != null && t.gameObject.activeInHierarchy) return t.font; } catch { }
            foreach (var f in Resources.FindObjectsOfTypeAll<TMP.TMP_FontAsset>()) if (f != null) return f;
        }
        catch { }
        return null;
    }

    static void BuildOverlay()
    {
        DestroyOverlay();
        root = new GameObject("JunLinkStream");
        var cv = root.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 120;
        var sc = root.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920f, 1080f); sc.matchWidthOrHeight = 1f;
        root.AddComponent<GraphicRaycaster>();
        var top = root.transform; var font = Font();
        sceneButtons.Clear(); sceneActions.Clear(); scene = "chatting";
        Color page = new Color(0.06f, 0.04f, 0.09f, 1f), panel = new Color(0.11f, 0.07f, 0.16f, 1f), head = new Color(0.16f, 0.09f, 0.23f, 1f), red = new Color(0.86f, 0.1f, 0.25f, 1f), dim = new Color(1f, 1f, 1f, 0.6f);

        // the studio page covers the whole screen (the room stays underneath, so the game keeps drawing her for the camera)
        Box("page", top, 4000f, 3000f, 0f, 0f, page).raycastTarget = true;

        // header: logo, a search bar (just for looks), her avatar on the right
        if (circleSprite == null) circleSprite = Shape(false);
        if (heartSprite == null) heartSprite = Shape(true);
        Box("header", top, 1920f, 70f, 0f, 505f, head);
        Text(top, font, "<b><color=#E8294D>stream</color>hub</b>", 34f, 260f, 60f, -755f, 505f, TMP.TextAlignmentOptions.Left);
        {
            Color field = new Color(0.08f, 0.05f, 0.12f, 1f), seg = new Color(0.25f, 0.16f, 0.36f, 1f), line = new Color(1f, 1f, 1f, 0.75f);
            Box("searchField", top, 560f, 42f, -35f, 505f, field);
            Text(top, font, "Search", 20f, 520f, 42f, -35f, 505f, TMP.TextAlignmentOptions.Left).color = new Color(1f, 1f, 1f, 0.35f);
            Box("searchBtn", top, 70f, 42f, 280f, 505f, seg);
            var ring = Box("lensRing", top, 20f, 20f, 277f, 508f, line); ring.sprite = circleSprite;
            var hole = Box("lensHole", top, 14f, 14f, 277f, 508f, seg); hole.sprite = circleSprite;
            var handle = Box("lensHandle", top, 4f, 10f, 286f, 498f, line); handle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        {
            var ring = Box("meRing", top, 48f, 48f, 917f, 505f, new Color(0.91f, 0.16f, 0.3f, 1f)); ring.sprite = circleSprite;
            var mask = Box("meMask", ring.transform, 42f, 42f, 0f, 0f, panel); mask.sprite = circleSprite;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            topAvatarImg = R("meAvatar", mask.transform, 42f, 42f, 0f, 0f).gameObject.AddComponent<RawImage>(); topAvatarImg.raycastTarget = false;
            topAvatarImg.color = new Color(1f, 1f, 1f, 0f);                // shows once her picture is taken
        }

        // video: 1280x720
        var video = R("video", top, 1280f, 720f, VideoX, VideoY);
        video.gameObject.AddComponent<Image>().color = panel;
        video.gameObject.AddComponent<RectMask2D>();
        StreamFx.BuildBackground(video);
        StreamFx.BuildGame(video, font);
        // facecam (Fishing scene): a 16:9 frame bottom right; the studio background shows behind her, cropped to head and shoulders
        faceFrame = Box("faceFrame", video, 436f, 251f, 412f, -224f, new Color(0.86f, 0.1f, 0.25f, 1f)).gameObject;
        var camMask = R("faceMask", faceFrame.transform, 424f, 239f, 0f, 0f); camMask.gameObject.AddComponent<RectMask2D>();
        camBg = R("faceBg", camMask, 424f, 239f, 0f, 0f).gameObject.AddComponent<RawImage>(); camBg.raycastTarget = false;
        camImg = R("faceJun", camMask, 300f, 400f, 0f, 0f).gameObject.AddComponent<RawImage>(); camImg.raycastTarget = false;
        faceFrame.SetActive(false);
        faceImg = R("jun", video, 600f, 700f, 0f, 0f).gameObject.AddComponent<RawImage>(); faceImg.raycastTarget = false;
        faceStatus = Text(video, font, "", 24f, 1000f, 80f, 0f, 0f, TMP.TextAlignmentOptions.Center); faceStatus.color = dim;
        var brb = Box("brb", video, 1280f, 720f, 0f, 0f, new Color(0.07f, 0.04f, 0.11f, 1f)); brbPanel = brb.gameObject;
        Text(brb.transform, font, "<b>BE RIGHT BACK</b>", 90f, 1200f, 130f, 0f, 30f, TMP.TextAlignmentOptions.Center);
        Text(brb.transform, font, "chat is still open, she'll be back in a moment", 28f, 1200f, 50f, 0f, -60f, TMP.TextAlignmentOptions.Center).color = dim;
        brbPanel.SetActive(false);
        alertBox = Box("alert", video, 560f, 92f, 330f, 300f, new Color(0f, 0f, 0f, 0f));          // top right: her speech box is on the left
        alertText = Text(video, font, "", 24f, 530f, 84f, 330f, 300f, TMP.TextAlignmentOptions.Center);

        BuildModHud(video, font);
        // her speech box, floating beside her head inside the video, in the game's own speech-box style (copied from its SayDialog)
        BuildBubble(video, font);
        // under the video, like a video site: title, then description
        var tt = Text(top, font, "<b>" + Esc(Title) + "</b>", 32f, 1280f, 46f, VideoX, -330f, TMP.TextAlignmentOptions.Left); tt.enableWordWrapping = false; tt.overflowMode = TMP.TextOverflowModes.Ellipsis;
        var dt = Text(top, font, "<noparse>" + Description + "</noparse>", 20f, 1280f, 30f, VideoX, -458f, TMP.TextAlignmentOptions.Left); dt.color = dim; dt.enableWordWrapping = false; dt.overflowMode = TMP.TextOverflowModes.Ellipsis;
        BuildChannelRow(top, font);
        SceneButton(top, font, "chatting", "Chatting", -865f);
        SceneButton(top, font, "closeup", "Close-up", -717f);
        SceneButton(top, font, "gaming", "Fishing", -569f);
        SceneButton(top, font, "mod", "Mod Duty", -421f);
        SceneButton(top, font, "brb", "BRB", -273f);
        {
            var brt = R("bgButton", top, 230f, 44f, -80f, -500f);
            var frt = R("bgFolder", top, 130f, 44f, 105f, -500f);
            var fimg = frt.gameObject.AddComponent<Image>(); fimg.color = new Color(0.18f, 0.12f, 0.26f, 0.95f);
            var fb = frt.gameObject.AddComponent<Button>(); fb.targetGraphic = fimg;
            var fua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() => folderWanted = true)); fb.onClick.AddListener(fua); sceneActions.Add(fua);
            Text(frt, font, "<b>+ My BG</b>", 19f, 125f, 44f, 0f, 0f, TMP.TextAlignmentOptions.Center);
            var bimg = brt.gameObject.AddComponent<Image>(); bimg.color = new Color(0.18f, 0.12f, 0.26f, 0.95f);
            var bb = brt.gameObject.AddComponent<Button>(); bb.targetGraphic = bimg;
            var bua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() => bgWanted = true)); bb.onClick.AddListener(bua); sceneActions.Add(bua);
            var bl = Text(brt, font, "", 20f, 218f, 44f, 0f, 0f, TMP.TextAlignmentOptions.Center); bl.enableAutoSizing = true; bl.fontSizeMin = 14f; bl.fontSizeMax = 20f; bl.enableWordWrapping = false; StreamFx.SetLabel(bl);
        }
        {
            var ert = R("end", top, 160f, 44f, 260f, -500f);
            var eimg = ert.gameObject.AddComponent<Image>(); eimg.color = new Color(0.62f, 0.09f, 0.2f, 1f);
            var eb = ert.gameObject.AddComponent<Button>(); eb.targetGraphic = eimg;
            var ua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() => askEnd = true));
            eb.onClick.AddListener(ua); endAction = ua;
            Text(ert, font, "<b>End stream</b>", 19f, 160f, 44f, 0f, 0f, TMP.TextAlignmentOptions.Center);
        }
        try { sceneButtons["chatting"].color = red; } catch { }

        // chat column on the right
        Box("chat", top, 560f, 952f, 660f, -46f, panel);
        Box("chatHead", top, 560f, 50f, 660f, 405f, head);
        Text(top, font, "<b>Live chat</b>", 22f, 520f, 50f, 660f, 405f, TMP.TextAlignmentOptions.Left);
        likesHead = Text(top, font, "", 18f, 300f, 50f, 775f, 405f, TMP.TextAlignmentOptions.Right); likesHead.color = dim;
        // pinned message at the top of chat, like the real sites
        Box("pinned", top, 540f, 48f, 660f, 354f, new Color(0.2f, 0.13f, 0.28f, 1f));
        Text(top, font, "<size=80%><color=#FFD54F><b>PINNED</b></color></size>  <alpha=#CC>welcome! be kind, have fun, and no spoilers :)", 17f, 515f, 48f, 660f, 354f, TMP.TextAlignmentOptions.Left);
        var clip = R("chatClip", top, 530f, 798f, 660f, -87f);
        clip.gameObject.AddComponent<RectMask2D>();
        chatText = Text(clip, font, "<i>chat is warming up...</i>", 20f, 515f, 798f, 0f, 0f, TMP.TextAlignmentOptions.BottomLeft);
        chatText.overflowMode = TMP.TextOverflowModes.Overflow;
        chatText.paragraphSpacing = 7f; chatText.lineSpacing = -4f;
        // click a message: she answers that viewer next
        try
        {
            var hit = clip.gameObject.AddComponent<Image>(); hit.color = new Color(0f, 0f, 0f, 0f); hit.raycastTarget = true;
            var trig = clip.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry(); entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            var cb = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData>>(new Action<UnityEngine.EventSystems.BaseEventData>(ChatClicked));
            entry.callback.AddListener(cb); pickAction = cb;
            trig.triggers.Add(entry);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: chat is not clickable (" + ex.Message + ")"); }
        var hint = Text(top, font, "click a message and " + Esc(Bot()) + " answers it", 15f, 520f, 22f, 660f, -504f, TMP.TextAlignmentOptions.Center); hint.color = new Color(1f, 1f, 1f, 0.35f);
        BuildDrawer(top, font);
        BuildHamburger(top, font);
        BuildConfirm(top, font);
        if (font == null) Cfg.Log.LogWarning("Stream: no game font found, so the studio has no text");
    }

    const float VideoX = -300f, VideoY = 70f;
    static TMP.TextMeshProUGUI subText; static float subUntil; static CanvasGroup bubbleGroup; static RectTransform bubbleRt;
    static Sprite rounded, tail;

    // her answer in a speech box beside her head; it grows with the text and fades out after a while
    public static void Subtitle(string text)
    {
        try
        {
            if (subText == null || bubbleRt == null) return;
            subText.text = "<noparse>" + (text ?? "") + "</noparse>";
            LayoutBubble();
            bubbleGroup.alpha = 1f;
            subUntil = Time.unscaledTime + 4f + (text ?? "").Length / 14f;
        }
        catch { }
    }

    // size and place the box for the current scene (also when the scene changes while she is talking).
    // The top stays put and the box grows down, so it never runs out of the video; very long answers shrink the text (down to 16).
    static void LayoutBubble()
    {
        try
        {
            if (subText == null || bubbleRt == null) return;
            float tw = BubbleW - 36f;
            subText.enableAutoSizing = false; subText.fontSize = 22f;
            float need = subText.GetPreferredValues(subText.text, tw, 0f).y + 34f;
            float maxH = scene == "gaming" ? 260f : 300f;
            float h = Mathf.Clamp(need, 70f, maxH);
            if (need > maxH) { subText.enableAutoSizing = true; subText.fontSizeMin = 16f; subText.fontSizeMax = 22f; }
            bubbleRt.sizeDelta = new Vector2(BubbleW, h);
            subText.rectTransform.sizeDelta = new Vector2(tw, h - 22f);
            bool close = scene == "closeup";
            bubbleRt.anchoredPosition = scene == "gaming" ? new Vector2(400f, 300f) : new Vector2(close ? -360f : -290f, close ? 280f : 290f);
            // alerts: top right normally; in Fishing her box is there, so they go top left under the fishing info
            var ap = scene == "gaming" ? new Vector2(-330f, 170f) : new Vector2(330f, 300f);
            if (alertBox != null) alertBox.rectTransform.anchoredPosition = ap;
            if (alertText != null) alertText.rectTransform.anchoredPosition = ap;
        }
        catch { }
    }

    const float BubbleW = 440f;
    // Copies the look of the game's own speech box: its background and name-tab pictures, colours and font.
    // Falls back to the same colours drawn by hand if the game's box cannot be read.
    static void BuildBubble(RectTransform video, TMP.TMP_FontAsset font)
    {
        Sprite bgSprite = null, tabSprite = null; Color bg = new Color(0.14f, 0.05f, 0.22f, 0.97f), tab = new Color(0.86f, 0.1f, 0.38f, 1f), ink = Color.white, nameInk = Color.white;
        TMP.TMP_FontAsset textFont = font, nameFont = font; string got = "hand-made";
        try
        {
            var d = InGameChat.GameSpeechBox();
            if (d != null)
            {
                Image big = null, small = null; float bigA = 0f;
                foreach (var im in d.GetComponentsInChildren<Image>(true))
                {
                    var r = im.rectTransform.rect; float a = r.width * r.height;
                    if (im.sprite == null || a <= 0f) continue;
                    if (a > bigA) { if (big != null && (small == null)) small = big; bigA = a; big = im; }
                }
                foreach (var im in d.GetComponentsInChildren<Image>(true))
                {
                    if (im == big || im.sprite == null) continue;
                    var r = im.rectTransform.rect;
                    if (r.width > 40f && r.height > 20f && r.width * r.height < bigA * 0.25f && (small == null || r.width * r.height > small.rectTransform.rect.width * small.rectTransform.rect.height)) small = im;
                }
                if (big != null) { bgSprite = big.sprite; bg = big.color; got = "the game's box (" + big.name; }
                if (small != null && small != big) { tabSprite = small.sprite; tab = small.color; got += ", tab " + small.name; }
                TMP.TextMeshProUGUI story = null, name = null;
                foreach (var t in d.GetComponentsInChildren<TMP.TextMeshProUGUI>(true))
                {
                    var r = t.rectTransform.rect;
                    if (story == null || r.width * r.height > story.rectTransform.rect.width * story.rectTransform.rect.height) story = t;
                }
                foreach (var t in d.GetComponentsInChildren<TMP.TextMeshProUGUI>(true)) if (t != story && (name == null || t.rectTransform.rect.width < name.rectTransform.rect.width)) name = t;
                if (story != null && story.font != null) { textFont = story.font; ink = story.color; }
                if (name != null && name.font != null) { nameFont = name.font; nameInk = name.color; }
                if (big != null) got += ")";
            }
        }
        catch (Exception ex) { got = "hand-made (" + ex.Message + ")"; }
        if (bg.a < 0.5f) bg.a = 0.95f;

        var bub = R("bubble", video, BubbleW, 120f, -290f, 290f); bub.pivot = new Vector2(0.5f, 1f);
        bubbleGroup = bub.gameObject.AddComponent<CanvasGroup>(); bubbleGroup.alpha = 0f; bubbleGroup.blocksRaycasts = false;
        var bimg = bub.gameObject.AddComponent<Image>(); bimg.color = bg; bimg.raycastTarget = false;
        if (bgSprite != null) { bimg.sprite = bgSprite; bimg.type = bgSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple; }
        // the name tab sticks out at the top left, like the game's
        var tabRt = R("nameTab", bub, 96f, 36f, 0f, 0f);
        tabRt.anchorMin = tabRt.anchorMax = new Vector2(0f, 1f); tabRt.pivot = new Vector2(0f, 0f); tabRt.anchoredPosition = new Vector2(-4f, -6f);
        var timg = tabRt.gameObject.AddComponent<Image>(); timg.color = tab; timg.raycastTarget = false;
        if (tabSprite != null) { timg.sprite = tabSprite; timg.type = tabSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple; }
        var nt = Text(tabRt, nameFont, "<b>" + Esc(Bot()) + "</b>", 21f, 90f, 34f, 0f, 0f, TMP.TextAlignmentOptions.Center); nt.color = nameInk;
        nt.rectTransform.anchorMin = Vector2.zero; nt.rectTransform.anchorMax = Vector2.one; nt.rectTransform.offsetMin = nt.rectTransform.offsetMax = Vector2.zero;
        subText = Text(bub, textFont, "", 22f, BubbleW - 36f, 100f, 0f, 0f, TMP.TextAlignmentOptions.MidlineLeft);
        subText.rectTransform.anchorMin = subText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); subText.color = ink; subText.fontStyle = TMP.FontStyles.Bold;
        bubbleRt = bub;
        Cfg.Log.LogInfo("Stream: speech box style from " + got);
    }

    // ---------- step 2: canned chat, viewers, donations and superchats (step 3 swaps in AI chatters and Jun's answers) ----------
    class Msg { public int Id; public string Name, Text; public int Amount; public bool Super, Answered, Sub, Player, Vip, Picked, Bad, Deleted, Missed, Oops, Guest; public float Expire; }
    static int msgIds; static volatile int pickWanted = -1; static Il2CppSystem.Object pickAction;
    static readonly List<Msg> chat = new List<Msg>();
    static readonly System.Random rnd = new System.Random();
    static float viewers, viewerTarget, nextMsg, nextDonation, nextSuper, alertUntil;
    static int peakViewers, donations, donated, superchats;

    // all made up for this mod
    static readonly string[] Names = { "byte_muncher", "neonmoth", "toastedcircuit", "lil_static", "quietpixel", "rustbucket_ray", "sleepy_kernel", "ghost_in_the_shelf",
        "midnight_ramen", "solder_queen", "cozy_overclock", "404_sleep", "velvet_voltage", "patchnotes", "lofi_larry", "mint_capacitor", "dustbunny_9", "orbit_snack",
        "kettle_of_bits", "softreset", "blinky_cursor", "paper_lantern", "humming_fridge", "night_shift_nico" };
    static readonly string[] Lines = { "hiii {bot}!", "just got here, what did i miss", "first time watching, she's so cute", "the glasses look good today",
        "{bot} how was your day?", "lurking while i eat dinner", "is that the ghost decoration again lol", "chat is so cozy tonight", "hello from the night shift",
        "can she sing something later?", "i love this stream", "the room is so dark, did the power go out again?", "{bot} drink some water. or oil. whatever robots drink",
        "o7", "LMAO", "yesss", "she's actually so calm, love it", "who else is here every stream", "how long is stream today?", "{bot} say hi to chat!",
        "this is my comfort stream", "the halloween stuff is so nice", "i should be sleeping", "brb getting snacks", "back!", "hi chat", "w stream",
        "what's your favourite food {bot}?", "any plans for the weekend?", "clip that", "the vibes are immaculate", "she blinked, chat", "hello hello" };
    static readonly string[] SuperLines = { "keep being you, {bot}!", "thanks for the streams, they help me relax after work", "what's the best thing that happened to you this week?",
        "buy yourself something nice!", "first superchat, hi {bot}!", "can you say goodnight to me? i have an exam tomorrow", "happy to be here, love the room decorations",
        "for the electricity bill", "{bot} what do you dream about?", "you made my bad day better" };

    static void ResetStream()
    {
        chat.Clear(); viewers = 8 + rnd.Next(10); peakViewers = (int)viewers; donations = donated = superchats = 0;
        viewerTarget = 120 + rnd.Next(180);
        float now = Time.unscaledTime;
        nextMsg = now + 1.5f; nextDonation = now + 25f + rnd.Next(20); nextSuper = now + 50f + rnd.Next(30); alertUntil = 0f;
        Redraw();
        ResetAi();
        ResetChannel();
        ResetVips(); ResetMod(); ResetGuests(); chatCount = 0; given.Clear(); highlights.Clear(); ended = false; liveSeconds = 0;
    }

    static string Bot() { return Shared.Current != null ? Shared.Current.Bot : "Jun"; }
    static string Pick(string[] a) { return a[rnd.Next(a.Length)].Replace("{bot}", Bot()); }

    // superchat colours by amount, like YouTube: blue, teal, yellow, orange, red
    static string Tier(int amount) { return amount >= 50 ? "#D50000" : amount >= 20 ? "#F57C00" : amount >= 10 ? "#FFB300" : amount >= 5 ? "#00BFA5" : "#1E88E5"; }
    static string NameColour(string n) { string[] c = { "#FF8FB1", "#8FD3FF", "#B5F58F", "#FFD58F", "#D3A6FF", "#8FFFE0", "#FFB08F" }; int h = 0; foreach (char ch in n) h = h * 31 + ch; return c[(h & 0x7fffffff) % c.Length]; }
    static string Esc(string t) { return "<noparse>" + (t ?? "").Replace("</noparse>", "") + "</noparse>"; }

    public static void AddChat(string name, string text, int amount = 0, bool super = false)
    {
        AddMsg(new Msg { Name = name, Text = text, Amount = amount, Super = super });
    }
    static void AddMsg(Msg msg)
    {
        msg.Id = ++msgIds;
        if (!msg.Sub && !msg.Bad && msg.Amount == 0) chatCount++;
        chat.Add(msg);
        while (chat.Count > 40) chat.RemoveAt(0);
        Redraw();
    }

    static void Redraw()
    {
        if (chatText == null) return;
        if (chat.Count == 0) { chatText.text = "<i><alpha=#99>chat is warming up...</i>"; return; }
        var sb = new System.Text.StringBuilder();
        int from = Math.Max(0, chat.Count - 34);
        for (int i = from; i < chat.Count; i++)
        {
            var m = chat[i];
            if (m.Deleted) { sb.Append("<alpha=#55><i>" + Esc(m.Name) + ": " + (m.Oops ? "message deleted (oops)" : "message deleted by a moderator") + "</i><alpha=#FF>\n"); continue; }
            if (m.Bad) { sb.Append("<link=\"" + m.Id + "\"><b><color=" + NameColour(m.Name) + ">" + Esc(m.Name) + "</color></b>: " + Esc(m.Text) + "</link>\n"); continue; }
            if (m.Guest)
            {
                if (m.Sub) sb.Append("<mark=#6A3FB544>" + Badge("GUEST", "#7B3FE4") + "<b><color=#D6A6FF>" + Esc(m.Name) + "</color></b> " + Esc(m.Text) + "</mark>\n");
                else if (m.Super) sb.Append("<mark=#7B3FE455>" + Badge("GUEST", "#7B3FE4") + "<b><color=#D6A6FF>$" + m.Amount + " " + Esc(m.Name) + "</color></b>\n" + Esc(m.Text) + "</mark>\n");
                else sb.Append("<link=\"" + m.Id + "\">" + (m.Picked ? "<mark=#E8294D44>" + Badge("PICKED", "#E8294D") : "") + Badge("GUEST", "#7B3FE4") + "<b><color=#D6A6FF>" + Esc(m.Name) + "</color></b>: " + Esc(m.Text) + (m.Picked ? "</mark>" : "") + "</link>\n");
                continue;
            }
            if (m.Super && m.Vip) sb.Append("<mark=#C9A22766>" + Badge("VIP", "#B8860B") + "<b><color=#FFE08A>$" + m.Amount + " " + Esc(m.Name) + "</color></b>" + CeoOf(m.Name) + "\n" + Esc(m.Text) + "</mark>\n");
            else if (m.Vip) sb.Append("<link=\"" + m.Id + "\">" + (m.Picked ? "<mark=#E8294D44>" + Badge("PICKED", "#E8294D") : "") + Badge("VIP", "#B8860B") + "<b><color=#FFE08A>" + Esc(m.Name) + "</color></b>" + CeoOf(m.Name) + ": " + Esc(m.Text) + (m.Picked ? "</mark>" : "") + "</link>\n");
            else if (m.Super) sb.Append("<mark=" + Tier(m.Amount) + "55><b><color=" + Tier(m.Amount) + ">$" + m.Amount + "</color> " + Esc(m.Name) + "</b>\n" + Esc(m.Text) + "</mark>\n");
            else if (m.Amount > 0) sb.Append("<color=" + Tier(m.Amount) + "><b>" + Esc(m.Name) + " donated $" + m.Amount + "</b></color>\n");
            else if (m.Sub) sb.Append("<mark=#2E7D3255><color=#7CE38B><b>" + Esc(m.Name) + "</b></color> " + Esc(m.Text) + "</mark>\n");
            else if (m.Player) sb.Append("<mark=#FFFFFF14>" + Badge("HOME", "#E8294D") + "<b><color=#FFFFFF>" + Esc(m.Name) + "</color></b>: " + Esc(m.Text) + "</mark>\n");
            else if (m.Picked) sb.Append("<link=\"" + m.Id + "\"><mark=#E8294D44>" + Badge("PICKED", "#E8294D") + "<b><color=" + NameColour(m.Name) + ">" + Esc(m.Name) + "</color></b>: " + Esc(m.Text) + "</mark></link>\n");
            else sb.Append("<link=\"" + m.Id + "\"><margin-left=16><line-indent=-16>" + Badges(m.Name) + "<b><color=" + NameColour(m.Name) + ">" + Esc(m.Name) + "</color></b><alpha=#DD>: " + Esc(m.Text) + "<alpha=#FF></line-indent></margin></link>\n");
        }
        chatText.text = sb.ToString();
    }

    static void Alert(string text, string colour)
    {
        try
        {
            alertText.text = text;
            Color c; if (!ColorUtility.TryParseHtmlString(colour, out c)) c = Color.white; c.a = 0.92f;
            alertBox.color = c; alertUntil = Time.unscaledTime + 5f;
        }
        catch { }
    }

    static int Amount(bool super)
    {
        int r = rnd.Next(100);
        if (!super) return r < 60 ? 1 + rnd.Next(4) : r < 90 ? 5 : 10;
        return r < 35 ? 2 + rnd.Next(3) : r < 65 ? 5 + rnd.Next(5) : r < 85 ? 10 + rnd.Next(10) : r < 96 ? 20 + rnd.Next(30) : 50 + rnd.Next(51);
    }

    // ---------- step 3: AI chatters, and Jun answering chat, donations and superchats ----------
    // One AI request at a time (KoboldCpp answers one at a time): either a batch of chatters or one answer from Jun.
    class Ask { public int Prio; public string Name, Text, Prompt; }
    static readonly List<Ask> asks = new List<Ask>();
    static readonly Queue<Msg> aiChat = new Queue<Msg>();
    static volatile bool chatterBusy; static bool answering; static float answerStarted, nextAnswerAt, nextChatterAt;
    static string lastJunLine = "";
    static int aiBatches, answers;
    static readonly System.Net.Http.HttpClient Http = MakeHttp();
    static System.Net.Http.HttpClient MakeHttp() { var c = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(2) }; c.DefaultRequestHeaders.ConnectionClose = true; return c; }
    static readonly System.Text.RegularExpressions.Regex ChatLine = new System.Text.RegularExpressions.Regex(@"^\W*([A-Za-z0-9_]{3,20})\s*:\s*(.{2,120})$");
    static readonly System.Text.RegularExpressions.Regex NotSfw = new System.Text.RegularExpressions.Regex(@"\b(sex|sexy|nsfw|cum|nude|naked|horny|lewd|boob|tits|ass|dick|pussy|fuck|kill|die)\w*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    static void ResetAi()
    {
        asks.Clear(); aiChat.Clear(); answering = false; chatterBusy = false; lastJunLine = ""; aiBatches = answers = 0;
        float now = Time.unscaledTime;
        nextAnswerAt = now + 4f;       // her opening line
        nextChatterAt = now + 12f;
        asks.Add(new Ask { Prio = 5, Name = "", Text = "", Prompt = "You just went live" + (Title.Length > 0 ? " with the stream title \"" + Title + "\"" : "") + ". Open the stream: say hi to chat in your own words, one or two short sentences." });
    }

    // F2 while live: your message goes into chat and she answers it next
    public static void PlayerSays(string text)
    {
        string me = Shared.Current != null ? Shared.Current.Player : "Anon";
        AddMsg(new Msg { Name = me, Text = text, Player = true });
        asks.Add(new Ask { Prio = 4, Name = me, Text = text, Prompt = "Your chat has a message from " + me + " (the person you live with, typing in your chat): \"" + text + "\". Answer it out loud to the stream, one or two short sentences." });
        if (nextAnswerAt > Time.unscaledTime + 2f) nextAnswerAt = Time.unscaledTime + 2f;
    }

    static void QueueDonation(string name, int amount)
    {
        asks.Add(new Ask { Prio = 2, Name = name, Text = "", Prompt = name + " just donated $" + amount + " (stream money). Thank them by name in one short sentence." });
    }
    static void QueueSuperchat(string name, int amount, string text)
    {
        string size = amount >= 50 ? "a huge" : amount >= 20 ? "a big" : amount >= 10 ? "a nice" : "a small";
        asks.Add(new Ask { Prio = 3, Name = name, Text = text, Prompt = name + " sent " + size + " $" + amount + " superchat (stream money) with the message: \"" + text + "\". Read it out, thank them by name, and answer it. The bigger the amount, the bigger your reaction. Two short sentences at most." });
    }

    static Ask PickChat()
    {
        // a recent viewer message, questions first, never the same one twice
        Msg best = null; int seen = 0;
        for (int i = chat.Count - 1; i >= 0 && seen < 10; i--, seen++)
        {
            var m = chat[i];
            if (m.Amount > 0 || m.Answered || m.Sub || m.Player || m.Vip || m.Bad || m.Deleted || string.IsNullOrEmpty(m.Text)) continue;
            if (best == null || (m.Text.Contains("?") && !best.Text.Contains("?"))) best = m;
        }
        if (best == null) return null;
        best.Answered = true;
        return new Ask { Prio = 1, Name = best.Name, Text = best.Text, Prompt = "Your chat has a message from " + best.Name + ": \"" + best.Text + "\". Answer it out loud to the stream, one or two short sentences, and you may use their name." };
    }

    static void TickAi(float now)
    {
        if (answering && now - answerStarted > 60f) { answering = false; Cfg.Log.LogWarning("Stream: no answer from the AI in 60 s, moving on"); }
        bool aiFree = !answering && !chatterBusy && !PanelServer.Busy && InGameChat.PendingReply == null;

        if (aiFree && now >= nextAnswerAt)
        {
            Ask a = null;
            if (asks.Count > 0) { asks.Sort((x, y) => y.Prio.CompareTo(x.Prio)); a = asks[0]; asks.RemoveAt(0); }
            else a = PickChat();
            if (a != null)
            {
                answering = true; answerStarted = now;
                InGameChat.StreamPlain = true;
                string bot = Bot();
                PanelServer.AskAsync("(Game event) Stream: you are live-streaming as a cute, wholesome VTuber to your viewers right now (a stream from your room on streamhub.ic; " +
                    (Shared.Current != null ? Shared.Current.Player : "Anon") + " is nearby, off camera" + (Title.Length > 0 ? "; stream title: \"" + Title + "\"" : "") + "). Keep it SFW and friendly. " + (scene == "closeup" ? "The camera is close on your face. " : scene == "gaming" ? "You are playing a cozy fishing game on stream right now, your face is in a small facecam. " : "") + a.Prompt + " Be expressive: always start with a mood tag, and often add a gesture tag too, e.g. [happy][wave] when greeting or thanking someone, [shy] when complimented, [surprised] at big news, [point] or [lecture] when explaining. Speak only to chat; no actions.");
                return;
            }
            nextAnswerAt = now + 3f;
        }
        if (aiFree && aiChat.Count < 3 && now >= nextChatterAt) StartChatters();
    }

    // the fishing game reports catches and misses: chat reacts right away, she reacts with her next line
    static readonly string[] Wins = { "W", "nice catch!", "LETS GOOO", "clip it", "fisherman jun", "huge", "she's a natural", "gg" };
    static readonly string[] Losses = { "noooo", "unlucky", "it was right there", "rip", "next one!", "F" };
    // her face reacts right away (her answer comes a few seconds later); now and then she makes a face or waves at chat by herself
    static float nextIdleFace, lastEmote;
    static readonly string[] IdleFaces = { "happy", "happy", "shy", "neutral", "happy wave", "veryhappy" };
    static void Emote(string tags) { try { InGameChat.PetEmote(tags); lastEmote = Time.unscaledTime; } catch { } }
    static void TickIdleFace(float now)
    {
        if (now < nextIdleFace) return;
        nextIdleFace = now + 12f + rnd.Next(10);
        if (answering || now - lastEmote < 6f || now - answerStarted < 8f || scene == "brb") return;
        Emote(IdleFaces[rnd.Next(IdleFaces.Length)]);
    }

    public static void GameEvent(bool caught, string what, int cm)
    {
        int n = 1 + rnd.Next(3);
        for (int i = 0; i < n; i++) lock (aiChat) aiChat.Enqueue(new Msg { Name = Pick(Names), Text = what == "old boot" ? (i == 0 ? "LMAO a boot" : "boot of the year") : caught ? Wins[rnd.Next(Wins.Length)] : Losses[rnd.Next(Losses.Length)] });
        nextMsg = Mathf.Min(nextMsg, Time.unscaledTime + 0.3f);
        if (caught && what == "Golden Koi") { viewerTarget += 40f; Highlight("caught a rare Golden Koi (" + cm + " cm)"); }
        Emote(!caught ? "sad" : what == "old boot" ? "surprised" : what == "Golden Koi" ? "veryhappy surprised" : "veryhappy");
        string p = !caught ? "The fish just got away in your fishing game on stream. React in one short sentence."
            : what == "old boot" ? "You just fished up an old boot in your fishing game on stream. React in one short, funny sentence."
            : "You just caught a " + what + " (" + cm + " cm) in your fishing game on stream, at " + StreamFx.SpotName + (what == "Golden Koi" ? ", a very rare fish" : "") + ". React in one short sentence.";
        asks.RemoveAll(x => x.Prio == 3 && x.Name == "" && x.Text == "fish");
        asks.Add(new Ask { Prio = 3, Name = "", Text = "fish", Prompt = p });
        if (nextAnswerAt > Time.unscaledTime + 3f) nextAnswerAt = Time.unscaledTime + 3f;
    }

    // called by InGameChat when her stream answer was shown (or failed)
    public static void Answered(string text, bool error)
    {
        answering = false;
        float now = Time.unscaledTime;
        if (error) { nextAnswerAt = now + 10f; Cfg.Log.LogWarning("Stream: answer failed (" + text + ")"); return; }
        answers++; lastJunLine = text ?? ""; Subtitle(lastJunLine);
        bool playerWaiting = asks.Exists(x => x.Prio == 4);
        nextAnswerAt = now + (playerWaiting ? 4f : 14f + rnd.Next(7));
        nextChatterAt = Mathf.Min(nextChatterAt, now + 2f);     // chat reacts to what she said
    }

    static void StartChatters()
    {
        chatterBusy = true; nextChatterAt = Time.unscaledTime + 20f;
        string bot = Bot(), said = lastJunLine;
        var recent = new System.Text.StringBuilder();
        for (int i = Math.Max(0, chat.Count - 6); i < chat.Count; i++) if (chat[i].Amount == 0) recent.Append(chat[i].Name).Append(": ").Append(chat[i].Text).Append('\n');
        string ctx = recent.ToString();
        System.Threading.Tasks.Task.Run(async () =>
        {
            int added = 0;
            try
            {
                string system = "You write the live chat of a cozy, SFW VTuber stream. The streamer is " + bot + ", a shy, polite robot girl. " +
                    "Write 6 new chat messages from different viewers, one per line, exactly in the form  username: message  . " +
                    "Usernames are made-up lowercase handles (letters, digits, underscores, 3 to 18 characters). Messages are short (2 to 14 words), casual and friendly, " +
                    "sometimes a question to " + bot + ", sometimes reacting to what she just said. No emoji, no hashtags, nothing sexual, rude or mean. Output only the 6 lines.";
                string user = (said.Length > 0 ? bot + " just said: \"" + said + "\"\n" : "The stream just started.\n") + (ctx.Length > 0 ? "Recent chat:\n" + ctx : "");
                string body = Llm.Body(new object[] { new { role = "system", content = system }, new { role = "user", content = user } }, false, 300);
                using (var resp = await Http.PostAsync(Llm.ChatUrl(), new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json")))
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode) throw new Exception("the AI server returned " + (int)resp.StatusCode);
                    string text = System.Text.RegularExpressions.Regex.Replace(Llm.FullText(json) ?? "", @"<think>[\s\S]*?</think>", "");
                    foreach (var raw in text.Split('\n'))
                    {
                        var m = ChatLine.Match(raw.Trim());
                        if (!m.Success) continue;
                        string name = m.Groups[1].Value.ToLowerInvariant(), msg = m.Groups[2].Value.Trim().Trim('"');
                        if (NotSfw.IsMatch(msg) || NotSfw.IsMatch(name) || name == bot.ToLowerInvariant()) continue;
                        lock (aiChat) aiChat.Enqueue(new Msg { Name = name, Text = msg });
                        added++;
                    }
                }
                aiBatches++;
            }
            catch (Exception ex) { Cfg.Log.LogWarning("Stream: chatters failed (" + ex.Message + "), canned chat keeps going"); }
            finally { chatterBusy = false; }
        });
    }

    static Msg NextChat()
    {
        lock (aiChat) if (aiChat.Count > 0) return aiChat.Dequeue();
        return new Msg { Name = Pick(Names), Text = Pick(Lines) };
    }

    // ---------- cam view: the game's own buttons are hidden while live (like the game's own stream screen) ----------
    // Each hidden button gets a CanvasGroup (invisible, not clickable) that is removed again when the stream ends,
    // so the game's own on/off state of its buttons is never touched. Popups are left alone (the game may wait for an answer).
    static readonly List<CanvasGroup> hiddenUi = new List<CanvasGroup>();
    static float nextHide;
    static bool InPopup(Component c)
    {
        try { return c.GetComponentInParent<SimplePopup>() != null || c.GetComponentInParent<TimeoutSimplePopup>() != null || c.GetComponentInParent<ContextMenuPopup>() != null; }
        catch { return true; }
    }
    static void HideOne(GameObject go)
    {
        if (go == null || (root != null && go.transform.IsChildOf(root.transform))) return;
        foreach (var g in hiddenUi) if (g != null && g.gameObject == go) return;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) return;                                    // the game's own group: leave it
        cg = go.AddComponent<CanvasGroup>(); cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
        hiddenUi.Add(cg);
    }
    static void TickHideUi(float now)
    {
        if (now < nextHide) return;
        nextHide = now + 0.5f;
        int before = hiddenUi.Count;
        try { foreach (var bl in UnityEngine.Object.FindObjectsOfType<ButtonList>()) if (bl != null && !InPopup(bl)) HideOne(bl.gameObject); } catch { }
        try { foreach (var b in UnityEngine.Object.FindObjectsOfType<CommonButtonHandler>()) if (b != null && !InPopup(b)) HideOne(b.gameObject); } catch { }
        if (hiddenUi.Count != before && before == 0) Cfg.Log.LogInfo("Stream: hid " + hiddenUi.Count + " of the game's buttons for the cam view");
    }
    static void ShowGameUi()
    {
        foreach (var g in hiddenUi) { try { if (g != null) UnityEngine.Object.Destroy(g); } catch { } }
        hiddenUi.Clear();
    }

    // ---------- scenes: Chatting / Close-up / BRB (Gaming comes later) ----------
    static string scene = "chatting"; static GameObject faceFrame; static volatile bool bgWanted, folderWanted; static RawImage camBg, camImg;
    static GameObject brbPanel; static readonly Dictionary<string, Image> sceneButtons = new Dictionary<string, Image>();
    static readonly List<Il2CppSystem.Object> sceneActions = new List<Il2CppSystem.Object>();
    static volatile string sceneWanted;

    static void SceneButton(Transform top, TMP.TMP_FontAsset font, string id, string label, float x)
    {
        var rt = R("scene_" + id, top, 140f, 44f, x, -500f);
        var img = rt.gameObject.AddComponent<Image>(); img.color = new Color(0.24f, 0.13f, 0.36f, 0.95f);
        var btn = rt.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
        var ua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() => sceneWanted = id));
        btn.onClick.AddListener(ua); sceneActions.Add(ua);
        Text(rt, font, "<b>" + label + "</b>", 20f, 140f, 44f, 0f, 0f, TMP.TextAlignmentOptions.Center);
        sceneButtons[id] = img;
    }

    static void SetScene(string id)
    {
        if (id == scene) return;
        string was = scene; scene = id;
        foreach (var kv in sceneButtons) try { kv.Value.color = kv.Key == id ? new Color(0.86f, 0.1f, 0.25f, 1f) : new Color(0.24f, 0.13f, 0.36f, 0.95f); } catch { }
        try { if (brbPanel != null) brbPanel.SetActive(id == "brb"); } catch { }
        StreamFx.Show(id == "gaming");
        ShowMod(id == "mod");
        LayoutBubble();
        try { if (faceFrame != null) faceFrame.SetActive(id == "gaming"); } catch { }
        if (id == "gaming") asks.Add(new Ask { Prio = 2, Name = "", Text = "", Prompt = "You just switched the stream to a fishing game you are going to play. Tell chat, one short excited sentence." });
        if (id == "brb") { viewerTarget = Mathf.Max(20f, viewerTarget * 0.7f); }
        if (was == "brb") { viewerTarget = viewerTarget / 0.7f; asks.Add(new Ask { Prio = 5, Name = "", Text = "", Prompt = "You are back from a short break. Tell chat you're back, one short sentence." }); nextAnswerAt = Time.unscaledTime + 1f; }
        if (id == "mod") { string me = Shared.Current != null ? Shared.Current.Player : "Anon"; asks.Add(new Ask { Prio = 2, Name = "", Text = "", Prompt = me + " (off camera) just became the moderator of your chat and will delete spam. Tell chat, one short playful sentence." }); }
        if (id == "closeup" && was != "brb") asks.Add(new Ask { Prio = 2, Name = "", Text = "", Prompt = "The camera just zoomed in close on your face. React to it shyly, one short sentence." });
        Cfg.Log.LogInfo("Stream: scene " + id);
    }

    static void TickFacecam()
    {
        try
        {
            PetView.Touch();                        // keeps the pet render running while live
            var t = PetView.Tex;
            if (faceImg != null)
            {
                faceImg.texture = t; faceImg.color = t != null ? Color.white : new Color(0f, 0f, 0f, 0f);
                if (t != null && PetView.H > 0)
                {
                    bool close = scene == "closeup", game = scene == "gaming";
                    var uv = close ? new UnityEngine.Rect(0.12f, 0.56f, 0.76f, 0.44f) : new UnityEngine.Rect(0f, 0f, 1f, 1f);
                    faceImg.uvRect = uv;
                    float h = close ? 700f : 690f, w = h * (PetView.W * uv.width) / (PetView.H * uv.height);
                    faceImg.rectTransform.sizeDelta = new Vector2(w, h);
                    faceImg.rectTransform.anchoredPosition = new Vector2(0f, close ? 0f : -10f);
                    if (game) faceImg.color = new Color(0f, 0f, 0f, 0f);       // in the Fishing scene she is only in the facecam
                    if (camImg != null && game)
                    {
                        // the top 60% of her (head and shoulders), 560 high, its top just under the frame's top
                        camImg.texture = t; camImg.uvRect = new UnityEngine.Rect(0f, 0.4f, 1f, 0.6f);
                        float ch = 560f, cw = ch * (PetView.W * 1f) / (PetView.H * 0.6f);
                        camImg.rectTransform.sizeDelta = new Vector2(cw, ch);
                        camImg.rectTransform.anchoredPosition = new Vector2(0f, 120f - ch * 0.5f + 20f);
                        if (camBg != null) camBg.texture = StreamFx.CurrentTex;
                    }
                }
            }
            if (faceStatus != null) faceStatus.text = t != null ? "" : "camera: " + PetView.Status;
            if (!faceLogged && Time.unscaledTime - liveSince > 5f) { faceLogged = true; Cfg.Log.LogInfo("Stream: camera after 5 s: " + (t != null ? "picture " + PetView.W + "x" + PetView.H : "none") + " (" + PetView.Status + ")"); }
            if (subUntil > 0f && Time.unscaledTime > subUntil && bubbleGroup != null)
            {
                bubbleGroup.alpha = Mathf.MoveTowards(bubbleGroup.alpha, 0f, Time.unscaledDeltaTime * 2.5f);
                if (bubbleGroup.alpha <= 0f) subUntil = 0f;
            }
        }
        catch { }
    }

    static void TickOverlay()
    {
        try
        {
            float now = Time.unscaledTime, dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (ended) { TickHideUi(now); TickFacecam(); TickNote(); return; }           // summary on screen: the stream is over, nothing moves
            TickDrawer(now); TickVips(now); TickPick(now); TickMod(now); TickGuests(now);
            // viewers drift up towards a target and wobble a little
            viewers = Mathf.MoveTowards(viewers, viewerTarget, dt * 2.2f) + ((float)rnd.NextDouble() - 0.5f) * dt * 6f;
            if (rnd.Next(2000) == 0) viewerTarget = Mathf.Max(30f, viewerTarget + rnd.Next(-40, 80));
            int v = Mathf.Max(0, Mathf.RoundToInt(viewers)); if (v > peakViewers) peakViewers = v;
            int s = Mathf.FloorToInt(now - liveSince); liveSeconds = s;
            if (viewersText != null) viewersText.text = Num(v) + " watching  ·  " + Clock(s);
            TickChannel(now, dt, v);

            // the busier the stream, the faster chat moves
            if (now >= nextMsg)
            {
                var nm = NextChat(); AddChat(nm.Name, nm.Text);
                nextMsg = now + Mathf.Lerp(3.5f, 1.0f, Mathf.Clamp01(v / 300f)) * (0.6f + (float)rnd.NextDouble() * 0.8f);
            }
            if (now >= nextDonation)
            {
                string n = Pick(Names); int a = Amount(false);
                donations++; donated += a; Track(n, a);
                AddChat(n, "", a);
                Alert("<b>" + Esc(n) + "</b> donated <b>$" + a + "</b>!", Tier(a));
                QueueDonation(n, a); Emote("happy wave");
                nextDonation = now + 35f + rnd.Next(40);
            }
            if (now >= nextSuper)
            {
                string n = Pick(Names), t = Pick(SuperLines); int a = Amount(true);
                superchats++; donated += a; Track(n, a);
                AddChat(n, t, a, true);
                Alert("<b>$" + a + "  " + Esc(n) + "</b>\n<size=80%>" + Esc(t) + "</size>", Tier(a));
                QueueSuperchat(n, a, t); Emote(a >= 20 ? "veryhappy wave" : "happy");
                nextSuper = now + 70f + rnd.Next(60);
            }
            if (sceneWanted != null) { string w = sceneWanted; sceneWanted = null; SetScene(w); }
            TickHideUi(now);
            if (bgWanted) { bgWanted = false; StreamFx.NextBackground(); }
            if (folderWanted) { folderWanted = false; StreamFx.OpenFolder(); }
            TickIdleFace(now);
            if (scene == "gaming") StreamFx.Tick(now);
            TickFacecam();
            if (scene != "brb") TickAi(now);
            if (alertUntil > 0f && now > alertUntil)
            {
                alertUntil = 0f;
                if (alertText != null) alertText.text = "";
                if (alertBox != null) alertBox.color = new Color(0f, 0f, 0f, 0f);
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: overlay tick failed (" + ex.Message + ")"); }
    }

    // ---------- channel: subscribers (kept between streams), likes, badges ----------
    static TMP.TextMeshProUGUI likesText, subsText, likesHead; static RawImage avatarImg; static RectTransform heartRt;
    static RenderTexture avatarRt; static bool avatarTaken; static RawImage topAvatarImg;
    static GameObject confirmBox; static volatile bool askEnd, keepStreaming;
    static readonly List<Il2CppSystem.Object> confirmActions = new List<Il2CppSystem.Object>();

    // "are you sure?" before ending: a dark veil over the studio with two buttons
    static void BuildConfirm(Transform top, TMP.TMP_FontAsset font)
    {
        confirmActions.Clear();
        var veil = Box("confirm", top, 4000f, 3000f, 0f, 0f, new Color(0f, 0f, 0f, 0.6f)); veil.raycastTarget = true; confirmBox = veil.gameObject;
        Box("confirmPanel", veil.transform, 620f, 250f, 0f, 0f, new Color(0.16f, 0.09f, 0.23f, 1f));
        Box("confirmBar", veil.transform, 620f, 6f, 0f, 122f, new Color(0.91f, 0.16f, 0.3f, 1f));
        Text(veil.transform, font, "<b>End the stream?</b>", 30f, 560f, 50f, 0f, 60f, TMP.TextAlignmentOptions.Center);
        Text(veil.transform, font, "are you sure you want to end the stream?", 21f, 560f, 40f, 0f, 15f, TMP.TextAlignmentOptions.Center).color = new Color(1f, 1f, 1f, 0.7f);
        ConfirmButton(veil.transform, font, "Keep streaming", -140f, new Color(0.32f, 0.2f, 0.5f, 1f), () => keepStreaming = true);
        ConfirmButton(veil.transform, font, "End stream", 140f, new Color(0.86f, 0.1f, 0.25f, 1f), () => summaryWanted = true);
        confirmBox.SetActive(false);
    }
    static void ConfirmButton(Transform parent, TMP.TMP_FontAsset font, string label, float x, Color c, Action a)
    {
        var rt = R("confirm_" + label, parent, 240f, 54f, x, -70f);
        var img = rt.gameObject.AddComponent<Image>(); img.color = c;
        var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
        var ua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(a); b.onClick.AddListener(ua); confirmActions.Add(ua);
        Text(rt, font, "<b>" + label + "</b>", 22f, 240f, 54f, 0f, 0f, TMP.TextAlignmentOptions.Center);
    }
    static int subs = -1, newSubs, nextMilestone; static float likes, nextSub, nextGift, heartPop;
    static readonly HashSet<string> subbedNow = new HashSet<string>();
    static readonly string[] Mods = { "patchnotes", "solder_queen" };
    static string ChannelFile { get { return System.IO.Path.Combine(Host.RootPath, "stream_channel.txt"); } }
    static string Num(int n)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        if (n >= 100000) return (n / 1000).ToString(ci) + "K";
        if (n >= 10000) return (n / 1000f).ToString("0.0", ci) + "K";
        return n.ToString("#,0", ci);
    }
    static string Badge(string label, string colour) { return "<size=68%><mark=" + colour + "DD><b><color=#FFFFFF> " + label + " </color></b></mark></size> "; }
    static int Hash(string n) { int h = 7; foreach (char ch in n ?? "") h = h * 131 + ch; return h & 0x7fffffff; }
    static bool IsMember(string n) { return subbedNow.Contains(n) || Hash(n) % 100 < 40; }
    static string Badges(string n)
    {
        string b = "";
        if (Array.IndexOf(Mods, n) >= 0) b += Badge("MOD", "#2F6FED");
        if (IsMember(n)) b += Badge(subbedNow.Contains(n) ? "NEW" : "SUB", "#2E9D4A");
        return b;
    }

    // The channel lives in the save itself, as story flags the game saves and loads with everything else:
    //   JunLink_StreamSubs_<n>        her subscriber count (one flag, replaced)
    //   JunLink_StreamPaid_D<day>     that in-game day's stream was already paid out
    // So loading an older save brings back that save's subscribers, and the payout can't be repeated by reloading.
    // stream_channel.txt is only the fallback (no save loaded) and the starting count for a save that never streamed.
    const string SubsFlag = "JunLink_StreamSubs_", PaidFlag = "JunLink_StreamPaid_D";
    static GameVariables GV() { try { return GameScript.Instance != null ? GameScript.Instance.GameVariables : null; } catch { return null; } }
    static List<string> ModFlags(GameVariables gv)
    {
        var l = new List<string>();
        try { var f = gv.flags; for (int i = 0; i < f.Count; i++) { var n = f[i] != null ? f[i].Name : null; if (n != null && n.StartsWith("JunLink_", StringComparison.Ordinal)) l.Add(n); } }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not read the save's flags (" + ex.Message + ")"); }
        return l;
    }
    static void LoadChannel()
    {
        int fileSubs = 300 + rnd.Next(60);
        try
        {
            if (System.IO.File.Exists(ChannelFile))
                foreach (var line in System.IO.File.ReadAllLines(ChannelFile))
                {
                    int v; var kv = line.Split('=');
                    if (kv.Length == 2 && kv[0].Trim() == "subscribers" && int.TryParse(kv[1].Trim(), out v) && v >= 0) fileSubs = v;
                }
        }
        catch { }
        subs = fileSubs; subsFromSave = false;
        var gv = GV(); if (gv == null) return;
        foreach (var n in ModFlags(gv))
        {
            int v;
            if (n.StartsWith(SubsFlag, StringComparison.Ordinal) && int.TryParse(n.Substring(SubsFlag.Length), out v) && v >= 0) { subs = v; subsFromSave = true; }
        }
        Cfg.Log.LogInfo("Stream: channel has " + subs + " subscribers (" + (subsFromSave ? "from the save" : "new for this save") + ")");
    }
    static bool subsFromSave;
    static bool PaidToday(int day) { var gv = GV(); try { return gv != null && gv.CheckFlag(PaidFlag + day); } catch { return false; } }
    static void SaveChannel()
    {
        var gv = GV();
        if (gv != null)
        {
            try
            {
                foreach (var n in ModFlags(gv)) if (n.StartsWith(SubsFlag, StringComparison.Ordinal)) gv.RemoveFlag(n);
                gv.AddFlag(SubsFlag + subs);
            }
            catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not write the channel into the save (" + ex.Message + ")"); }
        }
        try { System.IO.File.WriteAllText(ChannelFile, "# Jun's streamhub.ic channel (Jun Link stream mode). The real count is kept in your save;\n# this is only the starting count for a save that never streamed.\nsubscribers=" + subs + "\n"); }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not save the channel (" + ex.Message + ")"); }
    }
    static void ResetChannel()
    {
        LoadChannel();
        newSubs = 0; subbedNow.Clear(); avatarTaken = false; likes = 0f; heartPop = 0f;
        nextMilestone = (subs / 100 + 1) * 100;
        float now = Time.unscaledTime; nextSub = now + 20f + rnd.Next(25); nextGift = now + 180f + rnd.Next(180);
        ShowChannel();
    }
    static void ShowChannel()
    {
        try
        {
            if (subsText != null) subsText.text = Num(subs) + " subscribers" + (newSubs > 0 ? "   <color=#7CE38B>+" + newSubs + " this stream</color>" : "");
            int l = Mathf.FloorToInt(likes);
            if (likesText != null) likesText.text = "<b>" + Num(l) + "</b>";
            if (likesHead != null) likesHead.text = Num(l) + " likes";
        }
        catch { }
    }

    static void NewSub(string name, bool gifted)
    {
        subs++; newSubs++; subbedNow.Add(name);
        if (!gifted) AddMsg(new Msg { Name = name, Text = "just subscribed! welcome to the club", Sub = true });
        if (subs >= nextMilestone)
        {
            int m = nextMilestone; nextMilestone += 100; Highlight("the channel reached " + m + " subscribers");
            Alert("<b>" + Num(m) + " SUBSCRIBERS!</b>", "#2E7D32"); Emote("veryhappy wave");
            asks.Add(new Ask { Prio = 3, Name = name, Text = "", Prompt = "Your channel just reached " + m + " subscribers during this stream. Celebrate it with chat, excited but short, one or two sentences." });
        }
    }

    static void TickChannel(float now, float dt, int v)
    {
        // likes trickle in with the viewer count; a heart pops when they cross a round number
        int before = Mathf.FloorToInt(likes);
        likes += v * dt * (0.0025f + (float)rnd.NextDouble() * 0.004f);
        if (Mathf.FloorToInt(likes) / 25 != before / 25) heartPop = 1f;
        if (heartRt != null) { heartPop = Mathf.MoveTowards(heartPop, 0f, dt * 3f); float k = 1f + 0.35f * heartPop; heartRt.localScale = new Vector3(k, k, 1f); }

        if (now >= nextSub)
        {
            string n = Pick(Names); if (subbedNow.Contains(n)) n = Pick(Names);
            NewSub(n, false);
            Alert("<b>" + Esc(n) + "</b> just subscribed!", "#2E7D32");
            asks.Add(new Ask { Prio = 2, Name = n, Text = "", Prompt = n + " just subscribed to your channel. Welcome them by name in one short, happy sentence." });
            Emote("happy");
            nextSub = now + Mathf.Lerp(80f, 30f, Mathf.Clamp01(v / 300f)) * (0.6f + (float)rnd.NextDouble() * 0.8f);
        }
        if (now >= nextGift)
        {
            string g = Pick(Names); int k = rnd.Next(100) < 70 ? 5 : 10;
            AddMsg(new Msg { Name = g, Text = "gifted " + k + " subs to the community!", Sub = true });
            for (int i = 0; i < k; i++) NewSub(Pick(Names), true);
            Highlight(g + " gifted " + k + " subs to chat");
            Alert("<b>" + Esc(g) + "</b> gifted <b>" + k + " subs</b>!", "#1B5E20");
            asks.Add(new Ask { Prio = 3, Name = g, Text = "", Prompt = g + " just gifted " + k + " subscriptions to other viewers in your chat. Thank them by name, impressed, in one or two short sentences." });
            Emote("veryhappy wave");
            nextGift = now + 240f + rnd.Next(240);
        }
        ShowChannel();

        // channel avatar: one still picture of her face, taken once a second after going live
        try
        {
            var tex = PetView.Tex;
            if (!avatarTaken && avatarImg != null && tex != null && PetView.W > 0 && now > liveSince + 1f)
            {
                avatarTaken = true;
                float uh = 0.24f, uw = Mathf.Min(1f, uh * PetView.H / (float)PetView.W);
                if (avatarRt == null) { avatarRt = new RenderTexture(160, 160, 0, RenderTextureFormat.ARGB32); avatarRt.filterMode = FilterMode.Bilinear; avatarRt.Create(); }
                Graphics.Blit(tex, avatarRt, new Vector2(uw, uh), new Vector2(0.5f - uw * 0.5f, 0.66f));
                avatarImg.texture = avatarRt; avatarImg.uvRect = new UnityEngine.Rect(0f, 0f, 1f, 1f);
                if (topAvatarImg != null) { topAvatarImg.texture = avatarRt; topAvatarImg.color = Color.white; }
            }
        }
        catch { }
    }

    static Sprite circleSprite, heartSprite;
    static Sprite Shape(bool heart)
    {
        const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.hideFlags = HideFlags.DontUnloadUnusedAsset;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float a = 0f;
            for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)       // 4x4 supersampling for soft edges
            {
                float u = (x + (sx + 0.5f) / 4f) / n * 2f - 1f, v = (y + (sy + 0.5f) / 4f) / n * 2f - 1f;
                bool inside;
                if (heart) { float hx = u * 1.25f, hy = v * 1.3f + 0.15f; float q = hx * hx + hy * hy - 1f; inside = q * q * q - hx * hx * hy * hy * hy <= 0f; }
                else inside = u * u + v * v <= 1f;
                if (inside) a += 1f / 16f;
            }
            px[y * n + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply();
        var s = Sprite.Create(t, new UnityEngine.Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f); s.hideFlags = HideFlags.DontUnloadUnusedAsset; return s;
    }

    // under the title: her round avatar, channel name, subscribers; a like pill on the right
    static void BuildChannelRow(Transform top, TMP.TMP_FontAsset font)
    {
        if (circleSprite == null) circleSprite = Shape(false);
        if (heartSprite == null) heartSprite = Shape(true);
        const float y = -405f, left = VideoX - 640f;
        var ring = Box("avatarRing", top, 60f, 60f, left + 30f, y, new Color(0.91f, 0.16f, 0.3f, 1f)); ring.sprite = circleSprite;
        var mask = Box("avatarMask", ring.transform, 54f, 54f, 0f, 0f, new Color(0.11f, 0.07f, 0.16f, 1f)); mask.sprite = circleSprite;
        mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        avatarImg = R("avatar", mask.transform, 54f, 54f, 0f, 0f).gameObject.AddComponent<RawImage>(); avatarImg.raycastTarget = false;
        var nm = Text(top, font, "<b>" + Esc(Bot()) + "</b>", 24f, 500f, 30f, left + 75f + 250f, y + 13f, TMP.TextAlignmentOptions.Left); nm.enableWordWrapping = false;
        float nameW = 60f; try { nameW = nm.GetPreferredValues(nm.text).x; } catch { }
        float bx = left + 75f + nameW + 12f + 50f;                      // badge is 100 wide, 12 px after the name
        Box("partner", top, 100f, 24f, bx, y + 13f, new Color(0.42f, 0.25f, 0.71f, 1f));
        Text(top, font, "<b>PARTNER</b>", 14f, 100f, 24f, bx, y + 13f, TMP.TextAlignmentOptions.Center).characterSpacing = 4f;
        subsText = Text(top, font, "", 18f, 500f, 26f, left + 75f + 250f, y - 15f, TMP.TextAlignmentOptions.Left); subsText.color = new Color(1f, 1f, 1f, 0.6f); subsText.enableWordWrapping = false;
        // LIVE, viewers and time, then the like pill at the right edge of the video
        float right = VideoX + 640f;
        Box("live", top, 90f, 34f, right - 520f, y, new Color(0.86f, 0.1f, 0.25f, 1f));
        Text(top, font, "<b>● LIVE</b>", 19f, 90f, 34f, right - 520f, y, TMP.TextAlignmentOptions.Center);
        viewersText = Text(top, font, "0 watching  ·  00:00", 20f, 300f, 40f, right - 315f, y, TMP.TextAlignmentOptions.Left);
        viewersText.enableWordWrapping = false;
        Box("likePill", top, 150f, 46f, right - 75f, y, new Color(0.2f, 0.13f, 0.28f, 1f));
        var heart = Box("heart", top, 26f, 26f, right - 75f - 42f, y, new Color(0.91f, 0.16f, 0.3f, 1f)); heart.sprite = heartSprite; heartRt = heart.rectTransform;
        likesText = Text(top, font, "0", 22f, 90f, 46f, right - 75f + 22f, y, TMP.TextAlignmentOptions.Left); likesText.enableWordWrapping = false;
        ShowChannel();
    }

    // ---------- black transition back to the room: fade to black, close the studio, fade back in ----------
    static volatile bool fadeWanted; static GameObject fadeRoot; static Image fadeImg; static int fadePhase; static float fadeAt;
    const float FadeOut = 0.6f, FadeHold = 0.35f, FadeIn = 0.8f;
    static void StartFade()
    {
        if (fadePhase != 0) return;
        try
        {
            fadeRoot = new GameObject("JunLinkStreamFade");
            var cv = fadeRoot.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 32000;
            fadeRoot.AddComponent<GraphicRaycaster>();
            fadeImg = Box("black", fadeRoot.transform, 8000f, 6000f, 0f, 0f, new Color(0f, 0f, 0f, 0f)); fadeImg.raycastTarget = true;
            fadePhase = 1; fadeAt = Time.unscaledTime;
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: fade failed (" + ex.Message + ")"); EndFade(); endWanted = true; }
    }
    static void TickFade()
    {
        if (fadePhase == 0) return;
        try
        {
            float t = Time.unscaledTime - fadeAt;
            if (fadePhase == 1)
            {
                fadeImg.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / FadeOut));
                if (t >= FadeOut) { EndStream(); fadePhase = 2; fadeAt = Time.unscaledTime; }          // the studio closes behind the black
            }
            else if (fadePhase == 2) { if (t >= FadeHold) { fadePhase = 3; fadeAt = Time.unscaledTime; } }
            else
            {
                fadeImg.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / FadeIn));
                if (t >= FadeIn) EndFade();
            }
        }
        catch { EndFade(); if (Live) EndStream(); }
    }
    static void EndFade()
    {
        try { if (fadeRoot != null) UnityEngine.Object.Destroy(fadeRoot); } catch { }
        fadeRoot = null; fadeImg = null; fadePhase = 0;
    }

    // ---------- Mod Duty: spam and rude messages show up in chat; click them before they stay up too long ----------
    static GameObject modHud; static TMP.TextMeshProUGUI modText, modFlash; static float modFlashUntil, nextBad;
    static int modRemoved, modMissed, modWrong, modStreak, modBest, modMissRun, modThanked;
    static readonly string[] BadNames = { "fr33_v_bux", "cheap_followers_99", "xX_spam_Xx", "bot_48213", "gift_card_guy", "lol_lol_lol_lol", "totally_not_a_bot", "crypto_kevin", "user8817263", "caps_lock_carl" };
    static readonly string[] BadLines = {
        "FREE V-BUCKS at totally-real-gift.ic", "buy 1000 followers cheap!! dm me", "check my channel pls pls pls pls",
        "WIN A FREE GPU >>> clickhere.ic <<<", "crypto giveaway!! send 1 coin get 2 back", "follow4follow?? follow4follow??",
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "LOLOLOLOLOLOLOLOLOLOLOLOLOLOLOL", "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
        "this stream is boring lol", "robots can't stream, cringe", "skill issue streamer", "unfollowed. bye",
        "first first first first first first first", "hot singles in your server room", "your PC has 37 viruses, download fix.ic" };
    static void BuildModHud(RectTransform video, TMP.TMP_FontAsset font)
    {
        var box = Box("modHud", video, 760f, 64f, 0f, -318f, new Color(0.07f, 0.04f, 0.11f, 0.88f)); modHud = box.gameObject;
        Box("modBar", box.transform, 760f, 4f, 0f, 30f, new Color(0.18f, 0.55f, 0.95f, 1f));
        modText = Text(box.transform, font, "", 22f, 730f, 60f, 0f, -2f, TMP.TextAlignmentOptions.Center); modText.enableWordWrapping = false;
        modFlash = Text(video, font, "", 30f, 900f, 50f, 0f, -255f, TMP.TextAlignmentOptions.Center);
        modFlash.outlineWidth = 0.2f; modFlash.outlineColor = new Color32(20, 10, 30, 255);
        modHud.SetActive(false); modFlash.gameObject.SetActive(false);
    }
    static void ResetMod() { modRemoved = modMissed = modWrong = modStreak = modBest = modMissRun = modThanked = 0; nextBad = 0f; modFlashUntil = 0f; }
    static void ModHudText()
    {
        try { if (modText != null) modText.text = "<b><color=#6FB4FF>MOD DUTY</color></b>   removed <b>" + modRemoved + "</b>   missed <b>" + modMissed + "</b>   streak <b>" + modStreak + "</b>   best <b>" + modBest + "</b>"; } catch { }
    }
    static void ModFlash(string t) { try { modFlash.text = t; modFlash.gameObject.SetActive(true); modFlashUntil = Time.unscaledTime + 1.6f; } catch { } }
    static void ShowMod(bool on)
    {
        try { if (modHud != null) modHud.SetActive(on); if (!on && modFlash != null) modFlash.gameObject.SetActive(false); } catch { }
        if (on) { ModHudText(); nextBad = Time.unscaledTime + 3f; }
    }
    static void TickMod(float now)
    {
        if (modFlashUntil > 0f && now > modFlashUntil) { modFlashUntil = 0f; try { modFlash.gameObject.SetActive(false); } catch { } }
        // missed: still up when its time ran out
        bool changed = false;
        foreach (var m in chat)
        {
            if (!m.Bad || m.Deleted || m.Missed || now < m.Expire) continue;
            m.Missed = true; changed = true;
            if (scene != "mod") continue;                                  // left Mod Duty: no penalty for what was still up
            modMissed++; modStreak = 0; modMissRun++;
            viewerTarget = Mathf.Max(20f, viewerTarget - 6f);
            ModFlash("<color=#FF6B6B>missed one... chat got spammed</color>");
            if (modMissRun == 3)
            {
                string me = Shared.Current != null ? Shared.Current.Player : "Anon";
                asks.Add(new Ask { Prio = 3, Name = "", Text = "mod", Prompt = "Some spam slipped through in your chat while " + me + " is on moderator duty. React in one short sentence, lightly teasing your moderator " + me + "." });
            }
        }
        if (changed) ModHudText();
        if (scene != "mod") return;
        if (now >= nextBad)
        {
            float k = Mathf.Clamp01(modStreak / 15f);
            AddMsg(new Msg { Name = BadNames[rnd.Next(BadNames.Length)], Text = BadLines[rnd.Next(BadLines.Length)], Bad = true, Expire = now + Mathf.Lerp(7f, 4.5f, k) });
            nextBad = now + Mathf.Lerp(7f, 3.5f, k) * (0.7f + (float)rnd.NextDouble() * 0.6f);
        }
    }
    // a click in chat while on Mod Duty: delete spam, or oops
    static void ModClick(Msg m, float now)
    {
        string me = Shared.Current != null ? Shared.Current.Player : "Anon";
        if (m.Bad && !m.Deleted)
        {
            m.Deleted = true; modRemoved++; modStreak++; modMissRun = 0; if (modStreak > modBest) modBest = modStreak;
            viewerTarget += 2f;
            ModFlash(m.Missed ? "<color=#FFD54F>removed (a bit late)</color>" : modStreak >= 5 && modStreak % 5 == 0 ? "<color=#7CE38B>+1  streak " + modStreak + "!</color>" : "<color=#7CE38B>+1 removed</color>");
            if (modRemoved % 5 == 0 && modRemoved > modThanked)
            {
                modThanked = modRemoved;
                asks.Add(new Ask { Prio = 3, Name = "", Text = "mod", Prompt = "Your moderator " + me + " has cleaned " + modRemoved + " spam messages out of your chat so far. Thank them, one short happy sentence." });
            }
        }
        else if (!m.Bad && !m.Deleted)
        {
            modWrong++; modStreak = 0;
            ModFlash("<color=#FF6B6B>oops, that was a real viewer!</color>");
            if (modWrong == 1 || modWrong % 3 == 0)
                asks.Add(new Ask { Prio = 3, Name = m.Name, Text = "mod", Prompt = "Your moderator " + me + " just accidentally deleted a nice message from " + m.Name + ". Apologise to " + m.Name + " and tease your moderator gently, one short sentence." });
            m.Deleted = true; m.Oops = true;
        }
        Redraw(); ModHudText();
        if (nextAnswerAt > now + 2f && asks.Exists(x => x.Text == "mod")) nextAnswerAt = now + 2f;
    }

    // ---------- guests: people Jun knows from the city drop into chat (a few per stream). Editable in stream_guests.txt ----------
    class Guest { public string Name, Who; public string[] Lines; public int DonMin, DonMax; public bool Used; }
    static readonly List<Guest> guests = new List<Guest>();
    static Guest guestHere; static readonly Queue<string> guestLines = new Queue<string>(); static float nextGuest, nextGuestLine, guestAiUntil; static int guestsThisStream;
    static volatile string[] guestAi; static volatile bool guestAiBusy; static bool guestDonated, guestGotLines; static int guestSaid, guestToken;
    static string GuestFile { get { return System.IO.Path.Combine(Host.RootPath, "stream_guests.txt"); } }
    const string GuestDefaults =
"# Guests who can drop into Jun's stream chat (a few per stream, each once).\n" +
"# One per line:  name in chat | who they are and how they talk | backup lines (split with ;;) | donation in $ (optional, e.g. 200-500)\n" +
"# The AI writes their messages from the description; the backup lines are used when the AI is busy or off.\n" +
"# Put a # in front of a line to switch that guest off. Big Daddy and Shanice are off at first: you meet them later in the story\n" +
"# (remove the # once you have met them).\n" +
"Lady Annalie | owner of the small clothing shop. Bubbly, dramatic, flirty and teasing, easily excited, loves fashion, singing and gossip, friends with Jun | darling, that outfit on camera?? i'm fainting ;; Lady Annalie is HERE, everyone act fabulous ;; Jun sing something!! i'll do backup vocals |\n" +
"# Big Daddy | rich, overworked businessman who shouts, is scatterbrained and very generous with cash, adores his daughter (his Little Angel) | LITTLE ROBOT GIRL!! BIG DADDY IS WATCHING!! ;; what are these tiny donations, step aside ;; my Little Angel says hi!! | 200-500\n" +
"Melissa | the supermarket cashier, secretly an android too. Precise, literal, monotone and formal, counts and measures everything, warms up slowly, Jun's friend | Greetings. I am observing this stream on my break. ;; Your chat produces 3.4 messages per second. Acceptable. ;; Hello Jun. This is Melissa. Good work. |\n" +
"Landlord | the greedy, loud, scheming landlord of the building. Sees fees and money-making chances everywhere, always about rent | nice stream. does it make money? asking for rent reasons ;; streaming uses electricity. that's a fee ;; tell your roommate rent is due | 1-1\n" +
"# Shanice | works at the church repairing bot parts. Cheerful, mischievous, a gossip who loves to tease, close to Priestbot | JUN!! hiii, don't tell Priestbot i'm watching ;; chat she's even cuter in person btw ;; i should be fixing bots rn lol |\n";

    static void LoadGuests()
    {
        guests.Clear();
        string text = GuestDefaults;
        try
        {
            if (System.IO.File.Exists(GuestFile)) text = System.IO.File.ReadAllText(GuestFile, System.Text.Encoding.UTF8);
            else System.IO.File.WriteAllText(GuestFile, GuestDefaults, new System.Text.UTF8Encoding(false));
        }
        catch { }
        foreach (var raw in text.Split('\n'))
        {
            var l = raw.Trim(); if (l.Length == 0 || l.StartsWith("#")) continue;
            var p = l.Split('|'); if (p.Length < 2 || p[0].Trim().Length == 0) continue;
            var g = new Guest { Name = p[0].Trim(), Who = p[1].Trim(), Lines = new string[0] };
            if (p.Length > 2) { var ls = new List<string>(); foreach (var x in p[2].Split(new[] { ";;" }, StringSplitOptions.None)) if (x.Trim().Length > 0) ls.Add(x.Trim()); g.Lines = ls.ToArray(); }
            if (p.Length > 3) { var d = p[3].Trim().Split('-'); int a, b; if (d.Length >= 1 && int.TryParse(d[0].Trim(), out a)) { g.DonMin = a; g.DonMax = d.Length > 1 && int.TryParse(d[1].Trim(), out b) ? Math.Max(a, b) : a; } }
            guests.Add(g);
        }
        Cfg.Log.LogInfo("Stream: " + guests.Count + " guests can drop by (" + GuestFile + ")");
    }
    static void ResetGuests()
    {
        LoadGuests();
        guestHere = null; guestLines.Clear(); guestAi = null; guestsThisStream = 0; guestDonated = false;
        nextGuest = Time.unscaledTime + 40f + rnd.Next(30);
    }
    static bool IsGuest(string name) { foreach (var g in guests) if (g.Name == name) return true; return false; }

    static void TickGuests(float now)
    {
        // a new guest arrives
        if (guestHere == null && now >= nextGuest && guestsThisStream < 3)
        {
            var free = new List<Guest>(); foreach (var g in guests) if (!g.Used) free.Add(g);
            if (free.Count == 0) { nextGuest = float.MaxValue; return; }
            var gst = free[rnd.Next(free.Count)]; gst.Used = true; guestHere = gst; guestsThisStream++; guestDonated = false;
            guestLines.Clear(); guestAi = null; guestGotLines = false; guestSaid = 0; guestToken++;
            AddMsg(new Msg { Name = gst.Name, Text = "joined the chat", Sub = true, Guest = true });
            lock (aiChat) aiChat.Enqueue(new Msg { Name = Pick(Names), Text = "wait, " + gst.Name + " is here??" });
            Highlight(gst.Name + " dropped into the chat");
            Cfg.Log.LogInfo("Stream: guest " + gst.Name);
            AskGuestLines(gst);
            guestAiUntil = now + 10f;                 // wait up to 10 s for the AI, then use the backup lines
            nextGuestLine = now + 3f;
            return;
        }
        if (guestHere == null) return;
        var cur = guestHere;
        // lines: the AI's if they come within 10 s, otherwise the backup lines (only one set per visit)
        if (!guestGotLines)
        {
            var ai = guestAi;
            if (ai != null) { foreach (var l in ai) guestLines.Enqueue(l); guestGotLines = true; }
            else if (now > guestAiUntil) { foreach (var l in cur.Lines) guestLines.Enqueue(l); guestGotLines = true; }
            guestAi = null;
        }
        if (guestGotLines && guestLines.Count == 0 && now > nextGuestLine) { guestHere = null; nextGuest = now + 120f + rnd.Next(120); return; }   // visit over
        if (now < nextGuestLine || guestLines.Count == 0) return;
        string text = guestLines.Dequeue();
        int said = ++guestSaid;
        AddMsg(new Msg { Name = cur.Name, Text = text, Guest = true, Answered = true });
        string me = Shared.Current != null ? Shared.Current.Player : "Anon";
        if (said == 1)
        {
            asks.Add(new Ask { Prio = 4, Name = cur.Name, Text = text, Prompt = cur.Name + " (someone you know from the city: " + cur.Who + ") is in your stream chat and wrote: \"" + text + "\". React to seeing them in your chat, answer them by name, one or two short sentences." });
            Emote("surprised happy");
        }
        else if (rnd.Next(100) < 60)
            asks.Add(new Ask { Prio = 3, Name = cur.Name, Text = text, Prompt = cur.Name + " (" + cur.Who + ") wrote in your chat: \"" + text + "\". Answer them by name, one short sentence." });
        // a donation from guests who give money (after their first line)
        if (said == 1 && cur.DonMax > 0 && !guestDonated)
        {
            guestDonated = true;
            int a = cur.DonMin + rnd.Next(cur.DonMax - cur.DonMin + 1); if (a >= 20) a = a / 10 * 10;
            superchats++; donated += a; Track(cur.Name, a);
            string dt = a <= 1 ? "for your trouble" : "for the stream!!";
            AddMsg(new Msg { Name = cur.Name, Text = dt, Amount = a, Super = true, Guest = true });
            Alert("<size=75%><color=#D6A6FF>GUEST</color></size>  <b>$" + a + "  " + Esc(cur.Name) + "</b>", a >= 50 ? "#6A3FB5" : "#4A2D7A");
            asks.Add(new Ask { Prio = 3, Name = cur.Name, Text = dt, Prompt = cur.Name + " (" + cur.Who + ") just sent a $" + a + " superchat (stream money)" + (a <= 1 ? ", which is a hilariously small amount for them" : "") + ". React and thank them by name, one or two short sentences." });
            Highlight(cur.Name + " sent a $" + a + " superchat");
        }
        if (nextAnswerAt > now + 2f) nextAnswerAt = now + 2f;
        nextGuestLine = now + 18f + rnd.Next(18);
    }

    // the AI writes 3 messages in the guest's voice
    static void AskGuestLines(Guest g)
    {
        guestAiBusy = true;
        string bot = Bot(), title = Title; int token = guestToken;
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                string system = "You write live-stream chat messages for a story game. Output only the messages, one per line, no names, no quotes, no numbering.";
                string user = g.Name + " is " + g.Who + ". " + g.Name + " is watching " + bot + "'s cozy, SFW VTuber stream" + (title.Length > 0 ? " titled \"" + title + "\"" : "") + " on streamhub.ic (a made-up site) and types in the chat. " +
                    bot + " is an android girl " + g.Name + " knows from the city. Write 3 short chat messages from " + g.Name + " (each 3 to 18 words), clearly in " + g.Name + "'s own voice and personality. Nothing sexual or mean.";
                string body = Llm.Body(new object[] { new { role = "system", content = system }, new { role = "user", content = user } }, false, 200);
                using (var resp = await Http.PostAsync(Llm.ChatUrl(), new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json")))
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode) throw new Exception("the AI server returned " + (int)resp.StatusCode);
                    string text = System.Text.RegularExpressions.Regex.Replace(Llm.FullText(json) ?? "", @"<think>[\s\S]*?</think>", "");
                    var got = new List<string>();
                    foreach (var raw in text.Split('\n'))
                    {
                        string t = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"^(\d+[.)]\s*|[-*]\s*)", "");
                        int c = t.IndexOf(':'); if (c > 0 && c < 24 && t.Substring(0, c).IndexOf(' ') < 0 || (c > 0 && t.Substring(0, c).Trim() == g.Name)) t = t.Substring(c + 1).Trim();
                        t = t.Trim().Trim('"').Trim();
                        if (t.Length < 2 || t.Length > 160 || NotSfw.IsMatch(t)) continue;
                        got.Add(t); if (got.Count == 3) break;
                    }
                    if (got.Count > 0 && token == guestToken) guestAi = got.ToArray();
                }
            }
            catch (Exception ex) { Cfg.Log.LogWarning("Stream: guest lines from the AI failed (" + ex.Message + "), using the backup lines"); }
            finally { guestAiBusy = false; }
        });
    }

    // ---------- picking a chat message ----------
    static void ChatClicked(UnityEngine.EventSystems.BaseEventData d)
    {
        try
        {
            if (chatText == null || ended) return;
            var pe = d.Cast<UnityEngine.EventSystems.PointerEventData>();
            int li = TMP.TMP_TextUtilities.FindIntersectingLink(chatText, new Vector3(pe.position.x, pe.position.y, 0f), null);
            if (li < 0) return;
            int id; if (int.TryParse(chatText.textInfo.linkInfo[li].GetLinkID(), out id)) pickWanted = id;
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: chat click failed (" + ex.Message + ")"); }
    }
    static void TickPick(float now)
    {
        int id = pickWanted; if (id < 0) return; pickWanted = -1;
        Msg m = null; foreach (var x in chat) if (x.Id == id) { m = x; break; }
        if (m == null) return;
        if (scene == "mod") { ModClick(m, now); return; }
        if (m.Picked || m.Bad || m.Deleted || string.IsNullOrEmpty(m.Text)) return;
        foreach (var x in chat) x.Picked = false;                        // one pick at a time
        m.Picked = true; m.Answered = true; Redraw();
        asks.RemoveAll(x => x.Prio == 5 && x.Text == "pick");
        string me = Shared.Current != null ? Shared.Current.Player : "Anon";
        string who = ""; if (m.Guest) foreach (var g in guests) if (g.Name == m.Name) who = " (someone you know from the city: " + g.Who + ")";
        asks.Add(new Ask { Prio = 5, Name = m.Name, Text = "pick", Prompt = me + " (off camera, helping run your stream) picked this chat message for you to answer, from " + m.Name + who + ": \"" + m.Text + "\". Answer " + m.Name + " by name, out loud to the stream, one or two short sentences." });
        if (nextAnswerAt > now + 1f) nextAnswerAt = now + 1f;
        Emote("point");
        Cfg.Log.LogInfo("Stream: picked a message from " + m.Name);
    }

    // ---------- stats drawer (hamburger), end-of-stream summary ----------
    static GameObject drawer, summaryBox; static TMP.TextMeshProUGUI drawerText; static volatile bool drawerWanted, summaryWanted; static bool ended; static float drawerNext;
    static int chatCount; static readonly Dictionary<string, int> given = new Dictionary<string, int>();
    static readonly List<string> highlights = new List<string>();
    static void Highlight(string h) { if (highlights.Count < 12) highlights.Add(h); }
    static string Facts()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Title \"" + Title + "\". Streamed for " + Mathf.Max(1, Mathf.RoundToInt(liveSeconds / 60f)) + " minutes, up to " + peakViewers + " viewers, " + Mathf.FloorToInt(likes) + " likes, " + newSubs + " new subscribers (" + subs + " in all). ");
        if (donations + superchats > 0) sb.Append(donations + " donations and " + superchats + " superchats, $" + donated + " in all; biggest supporter " + TopName() + ". ");
        if (paidNow > 0) sb.Append("The stream earned $" + paidNow + " of real money for the household after the platform's cut. ");
        if (StreamFx.Catches > 0) sb.Append("Played the fishing game: " + StreamFx.Catches + " catches" + (StreamFx.Best.Length > 0 ? ", best a " + StreamFx.Best : "") + ". ");
        if (modRemoved + modMissed > 0) sb.Append((Shared.Current != null ? Shared.Current.Player : "Anon") + " was her chat moderator for a while: removed " + modRemoved + " spam messages, missed " + modMissed + (modWrong > 0 ? ", and deleted " + modWrong + " nice message" + (modWrong > 1 ? "s" : "") + " by accident" : "") + ". ");
        if (highlights.Count > 0) sb.Append("Highlights: " + string.Join("; ", highlights) + ".");
        return sb.ToString().Trim();
    }
    static void Track(string name, int amount) { int v; given.TryGetValue(name, out v); given[name] = v + amount; }
    static string TopSupporter()
    {
        string n = null; int best = 0;
        foreach (var kv in given) if (kv.Value > best) { best = kv.Value; n = kv.Key; }
        return n == null ? "nobody yet" : Esc(n) + "  ($" + best + ")";
    }
    static string TopName()
    {
        string n = null; int best = 0;
        foreach (var kv in given) if (kv.Value > best) { best = kv.Value; n = kv.Key; }
        return n == null ? "nobody" : n + " ($" + best + ")";
    }
    static string Clock(int s) { return s >= 3600 ? (s / 3600) + ":" + (s % 3600 / 60).ToString("00") + ":" + (s % 60).ToString("00") : (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }
    static int liveSeconds;
    static string StatRow(string k, string v) { return "<alpha=#99>" + k + "<alpha=#FF><pos=50%><b><nobr>" + v + "</nobr></b>\n"; }
    static string StatsText(bool summary)
    {
        int v = Mathf.Max(0, Mathf.RoundToInt(viewers));
        var sb = new System.Text.StringBuilder();
        sb.Append(StatRow(summary ? "Streamed for" : "Live for", Clock(liveSeconds)));
        if (!summary) sb.Append(StatRow("Watching now", Num(v)));
        sb.Append(StatRow("Peak viewers", Num(peakViewers)));
        sb.Append(StatRow("Likes", Num(Mathf.FloorToInt(likes))));
        sb.Append(StatRow("New subscribers", "+" + newSubs + "  <alpha=#99>(" + Num(subs) + " total)"));
        sb.Append(StatRow("Donations", donations + "  <alpha=#99>·<alpha=#FF>  superchats " + superchats));
        sb.Append(StatRow("Raised", "$" + Num(donated)));
        if (summary && payoutNote.Length > 0) sb.Append(StatRow("Paid to you", payoutNote));
        sb.Append(StatRow("Top supporter", TopSupporter()));
        sb.Append(StatRow("Chat messages", Num(chatCount)));
        sb.Append(StatRow(Bot() + " answered", answers.ToString()));
        if (modRemoved + modMissed + modWrong > 0)
        {
            sb.Append(StatRow("Spam removed", modRemoved + "  <alpha=#99>missed " + modMissed));
            sb.Append(StatRow("Best mod streak", modBest.ToString()));
        }
        sb.Append(StatRow("Fish caught", StreamFx.Catches + (StreamFx.Best.Length > 0 ? "  <alpha=#99>best " + StreamFx.Best : "")));
        return sb.ToString();
    }

    static void BuildHamburger(Transform top, TMP.TMP_FontAsset font)
    {
        var hrt = R("menu", top, 48f, 48f, -918f, 505f);
        var himg = hrt.gameObject.AddComponent<Image>(); himg.color = new Color(1f, 1f, 1f, 0f);
        var hb = hrt.gameObject.AddComponent<Button>(); hb.targetGraphic = himg;
        var ua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(new Action(() => drawerWanted = true)); hb.onClick.AddListener(ua); confirmActions.Add(ua);
        for (int i = -1; i <= 1; i++) Box("bar" + i, hrt, 28f, 4f, 0f, i * 9f, new Color(1f, 1f, 1f, 0.9f));
    }
    static void BuildDrawer(Transform top, TMP.TMP_FontAsset font)
    {
        var d = Box("drawer", top, 470f, 560f, -725f, 190f, new Color(0.13f, 0.08f, 0.19f, 0.97f)); drawer = d.gameObject; d.raycastTarget = true;
        Box("drawerBar", d.transform, 470f, 5f, 0f, 277f, new Color(0.91f, 0.16f, 0.3f, 1f));
        Text(d.transform, font, "<b>Stream stats</b>", 26f, 420f, 50f, 0f, 235f, TMP.TextAlignmentOptions.Left);
        drawerText = Text(d.transform, font, "", 19f, 420f, 460f, 0f, -20f, TMP.TextAlignmentOptions.TopLeft);
        drawerText.lineSpacing = 18f;
        drawer.SetActive(false);
    }
    static void TickDrawer(float now)
    {
        if (drawerWanted) { drawerWanted = false; try { drawer.SetActive(!drawer.activeSelf); drawerNext = 0f; } catch { } }
        try { if (drawer != null && drawer.activeSelf && now >= drawerNext) { drawerNext = now + 0.5f; drawerText.text = StatsText(false); } } catch { }
    }

    static TMP.TextMeshProUGUI noteText; static string noteShown;
    // ---------- payout: the donations become real in-game money, once per in-game day ----------
    static string payoutNote = ""; static int paidNow;
    static void Payout()
    {
        payoutNote = ""; paidNow = 0;
        if (!Cfg.StreamPayout.Value) return;
        try
        {
            var gv = GameScript.Instance != null ? GameScript.Instance.GameVariables : null;
            var snap = Shared.Current; int day = snap != null ? snap.Day : -1;
            if (gv == null || day <= 0) return;
            if (PaidToday(day)) { payoutNote = "$0  <alpha=#99>(already paid today)"; return; }
            int pct = Mathf.Clamp(Cfg.StreamPayoutPercent.Value, 0, 100);
            int rent = 0; try { rent = gv.WeeklyRent; } catch { }
            int cap = Cfg.StreamPayoutCap.Value > 0 ? Cfg.StreamPayoutCap.Value : Mathf.Max(20, rent * 15 / 100);
            int pay = Mathf.Min(cap, donated * pct / 100);
            if (pay <= 0) { payoutNote = "$0"; return; }
            gv.AddMoney(pay);
            gv.AddFlag(PaidFlag + day); paidNow = pay;
            payoutNote = "$" + Num(pay) + "  <alpha=#99>(" + pct + "% after the platform's cut" + (donated * pct / 100 > cap ? ", daily cap $" + Num(cap) : "") + ")";
            Cfg.Log.LogInfo("Stream: paid out $" + pay + " of $" + donated + " donated (" + pct + "%, cap $" + cap + ", weekly rent $" + rent + ")");
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: payout failed (" + ex.Message + ")"); payoutNote = ""; }
    }
    static void TickNote()
    {
        try
        {
            if (noteText == null) return;
            string want = StreamLog.NoteBusy ? "<alpha=#88>" + Esc(Bot()) + " is writing a note about the stream..." : !string.IsNullOrEmpty(StreamLog.Note) ? "<b>" + Esc(Bot()) + "'s note:</b>  " + Esc(StreamLog.Note) : "";
            if (want != noteShown) { noteShown = want; noteText.text = want; }
        }
        catch { }
    }
    static void ShowSummary()
    {
        if (ended || root == null) return;
        ended = true;
        Payout();
        try { if (confirmBox != null) confirmBox.SetActive(false); if (drawer != null) drawer.SetActive(false); } catch { }
        try
        {
            var font = Font(); var top = root.transform;
            var veil = Box("summary", top, 4000f, 3000f, 0f, 0f, new Color(0.03f, 0.02f, 0.05f, 0.85f)); veil.raycastTarget = true; summaryBox = veil.gameObject;
            Box("sumPanel", veil.transform, 760f, 820f, 0f, 0f, new Color(0.13f, 0.08f, 0.19f, 1f));
            Box("sumBar", veil.transform, 760f, 6f, 0f, 407f, new Color(0.91f, 0.16f, 0.3f, 1f));
            Text(veil.transform, font, "<b>Stream ended</b>", 40f, 680f, 60f, 0f, 350f, TMP.TextAlignmentOptions.Center);
            var t = Text(veil.transform, font, "<noparse>" + Title + "</noparse>", 22f, 680f, 36f, 0f, 305f, TMP.TextAlignmentOptions.Center); t.color = new Color(1f, 1f, 1f, 0.6f);
            t.enableWordWrapping = false; t.overflowMode = TMP.TextOverflowModes.Ellipsis;
            var st = Text(veil.transform, font, StatsText(true), 20f, 600f, 400f, 0f, 55f, TMP.TextAlignmentOptions.TopLeft); st.lineSpacing = 4f;
            Box("noteLine", veil.transform, 600f, 2f, 0f, -100f, new Color(1f, 1f, 1f, 0.12f));
            noteText = Text(veil.transform, font, "", 18f, 600f, 190f, 0f, -210f, TMP.TextAlignmentOptions.TopLeft);
            noteText.color = new Color(1f, 0.92f, 0.96f, 0.9f); noteText.fontStyle = TMP.FontStyles.Italic; noteShown = null;
            ConfirmButton(veil.transform, font, "Back to the room", 0f, new Color(0.86f, 0.1f, 0.25f, 1f), () => fadeWanted = true);
            var back = veil.transform.Find("confirm_Back to the room"); if (back != null) ((RectTransform)back).anchoredPosition = new Vector2(0f, -340f);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: summary failed (" + ex.Message + ")"); endWanted = true; }
        try { StreamLog.Record(Title, Facts()); } catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not remember the stream (" + ex.Message + ")"); }
        Cfg.Log.LogInfo("Stream: summary shown");
    }

    // ---------- VIP guests: two made-up tech bosses who drop by once per stream ----------
    class Vip { public string Name, Who, Ceo; public string[] Supers, Chats; public bool Used; }
    static string CeoOf(string name) { foreach (var v in Vips) if (v.Name == name) return " <size=75%><alpha=#99>(" + v.Ceo + ")<alpha=#FF></size>"; return ""; }
    static readonly Vip[] Vips = {
        new Vip { Name = "Mason Huang", Ceo = "CEO of NoVidia", Who = "a (fictional) boss of a big graphics-chip company, always in a leather jacket",
            Supers = new[] { "your frame pacing is flawless. who made your graphics card? asking for a friend",
                             "every robot deserves more cores. here, have a few",
                             "the jacket stays on, the stream stays on. keep going!",
                             "accelerated computing, accelerated cuteness",
                             "i'd sign your GPU but you're already a masterpiece" },
            Chats = new[] { "this render quality... chef's kiss", "chat, check your VRAM", "just dropped by between keynotes", "more polygons for this girl please" } },
        new Vip { Name = "Adam Altman", Ceo = "CEO of OpenMind", Who = "a (fictional) boss of a big AI research lab",
            Supers = new[] { "honestly impressive reasoning for a robot girl. we should talk",
                             "i've seen the future and it's this stream",
                             "quick question: how big is your context window?",
                             "chat, this is what good alignment looks like",
                             "compute well spent. keep it up!" },
            Chats = new[] { "this stream passes my eval", "is she running locally? respect", "feeling the vibes", "the next version will be even cuter" } },
    };
    static readonly string[] VipReactions = { "WAIT is that THE {vip}??", "no way {vip} is here", "chat be cool, {vip} is watching", "VIP IN CHAT", "{vip} pls notice me too", "{bot} you're famous now" };
    static float nextVip, nextVipChat; static Vip vipHere;
    static void ResetVips() { foreach (var v in Vips) v.Used = false; vipHere = null; nextVip = Time.unscaledTime + 150f + rnd.Next(150); nextVipChat = 0f; }
    static void TickVips(float now)
    {
        if (now >= nextVip)
        {
            var free = new List<Vip>(); foreach (var x in Vips) if (!x.Used) free.Add(x);
            if (free.Count == 0) { nextVip = float.MaxValue; return; }
            var v = free[rnd.Next(free.Count)]; v.Used = true; vipHere = v;
            int a = 100 + 50 * rnd.Next(9);                                       // $100 .. $500
            string text = v.Supers[rnd.Next(v.Supers.Length)];
            superchats++; donated += a; Track(v.Name, a);
            AddMsg(new Msg { Name = v.Name, Text = text, Amount = a, Super = true, Vip = true });
            Alert("<size=75%><color=#FFE08A>VIP</color></size>  <b>$" + a + "  " + Esc(v.Name) + "</b> <size=70%>(" + v.Ceo + ")</size>\n<size=80%>" + Esc(text) + "</size>", "#8A6A12");
            asks.Add(new Ask { Prio = 4, Name = v.Name, Text = text, Prompt = v.Name + " (" + v.Ceo + "), " + v.Who + ", just sent a $" + a + " superchat (stream money) with the message: \"" + text + "\". You are starstruck and excited but stay yourself: thank them by name and answer the message, two short sentences at most." });
            Emote("veryhappy surprised");
            viewerTarget += 60f;
            int n = 2 + rnd.Next(2);
            for (int i = 0; i < n; i++) lock (aiChat) aiChat.Enqueue(new Msg { Name = Pick(Names), Text = VipReactions[rnd.Next(VipReactions.Length)].Replace("{vip}", v.Name).Replace("{bot}", Bot()) });
            nextMsg = Mathf.Min(nextMsg, now + 0.5f);
            nextVipChat = now + 12f + rnd.Next(10);
            nextVip = now + 240f + rnd.Next(240);
            Cfg.Log.LogInfo("Stream: VIP " + v.Name + " ($" + a + ")");
            Highlight(v.Name + " (" + v.Ceo + ", " + v.Who.Replace("(fictional) ", "") + ") dropped by and sent a $" + a + " superchat: \"" + text + "\"");
        }
        if (vipHere != null && nextVipChat > 0f && now >= nextVipChat)
        {
            AddMsg(new Msg { Name = vipHere.Name, Text = vipHere.Chats[rnd.Next(vipHere.Chats.Length)], Vip = true });
            nextVipChat = rnd.Next(100) < 50 ? now + 25f + rnd.Next(30) : 0f;      // sometimes a second line, then quiet
        }
    }

    static void DestroyOverlay()
    {
        try { if (root != null) UnityEngine.Object.Destroy(root); } catch { }
        StreamFx.Forget(); faceFrame = null;
        root = null; viewersText = chatText = alertText = likesText = subsText = likesHead = null; avatarImg = topAvatarImg = null; confirmBox = null; confirmActions.Clear(); askEnd = keepStreaming = false; drawer = summaryBox = null; drawerText = null; noteText = null; modHud = null; modText = modFlash = null; pickAction = null; pickWanted = -1; ended = false; drawerWanted = summaryWanted = false;
        try { if (avatarRt != null) { avatarRt.Release(); UnityEngine.Object.Destroy(avatarRt); } } catch { }
        avatarRt = null; alertBox = null; faceImg = null; faceStatus = null; subText = null; subUntil = 0f; bubbleGroup = null; bubbleRt = null; endAction = null; brbPanel = null; sceneButtons.Clear(); sceneActions.Clear();
    }
}
// ---- stream memory: after every stream the facts go into her memory (jun_memory.txt, so chat and the day's diary know),
// and she writes a short note about it, kept in jun_streams.jsonl and shown on the diary page ----
public static class StreamLog
{
    public class Entry
    {
        public int day { get; set; }
        public string written { get; set; }
        public string bot { get; set; }
        public string title { get; set; }
        public string facts { get; set; }
        public string note { get; set; }
    }
    static readonly object gate = new object();
    static string PathName { get { return System.IO.Path.Combine(Host.RootPath, "jun_streams.jsonl"); } }
    static readonly System.Net.Http.HttpClient Http = MakeHttp();
    static System.Net.Http.HttpClient MakeHttp() { var c = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(3) }; c.DefaultRequestHeaders.ConnectionClose = true; return c; }
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> Pending = new System.Collections.Concurrent.ConcurrentQueue<string>();
    public static void Tick() { string m; while (Pending.TryDequeue(out m)) { try { StoryMemory.Add(m); } catch { } } }
    public static volatile string Note;            // her note, once written (the summary card shows it)
    public static volatile bool NoteBusy;

    public static List<Entry> All()
    {
        var l = new List<Entry>();
        lock (gate)
        {
            try
            {
                if (System.IO.File.Exists(PathName))
                    foreach (var line in System.IO.File.ReadAllLines(PathName, System.Text.Encoding.UTF8))
                        if (line.Trim().Length > 0) try { var e = System.Text.Json.JsonSerializer.Deserialize<Entry>(line); if (e != null) l.Add(e); } catch { }
            }
            catch { }
        }
        l.Reverse();                                  // newest first
        return l;
    }
    static void Save(Entry e)
    {
        lock (gate)
        {
            var all = All(); all.Reverse();
            all.RemoveAll(x => x.written == e.written);
            all.Add(e);
            if (all.Count > 200) all.RemoveRange(0, all.Count - 200);
            var sb = new System.Text.StringBuilder();
            foreach (var x in all) sb.Append(System.Text.Json.JsonSerializer.Serialize(x)).Append('\n');
            try { System.IO.File.WriteAllText(PathName, sb.ToString(), new System.Text.UTF8Encoding(false)); } catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not save " + PathName + " (" + ex.Message + ")"); }
        }
    }

    // main thread, when the stream ends
    public static void Record(string title, string facts)
    {
        var s = Shared.Current;
        string bot = s != null ? s.Bot : "Jun", player = s != null ? s.Player : "Anon";
        int day = s != null ? s.Day : 0;
        try { StoryMemory.Add("Stream: " + bot + " live-streamed as a VTuber on streamhub.ic (a made-up site with made-up viewers) while " + player + " watched off camera. " + facts); } catch { }
        var e = new Entry { day = day, written = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), bot = bot, title = title, facts = facts, note = "" };
        Save(e);
        Note = null; NoteBusy = false;
        if (!Cfg.DiaryEnabled.Value) { Cfg.Log.LogInfo("Stream: remembered (diary is off, so no note)"); return; }
        if (!Requirements.Ready) { Cfg.Log.LogInfo("Stream: remembered, no note: " + Requirements.Why()); return; }
        NoteBusy = true;
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                string system = "You are " + bot + ". You just finished a live stream as a cute, wholesome VTuber on streamhub.ic, from your room, while " + player + " watched off camera. The site and the viewers are made up, but the donations pay real money for your household. " +
                    "Write a short private diary note about this stream: 2 to 4 sentences, under 70 words, first person, in your own voice, about how it went and how you feel about it. No lists, no emojis, no tags, no title." + Characters.For(bot, "");
                string body = Llm.Body(new object[] { new { role = "system", content = system }, new { role = "user", content = "What happened on the stream:\n" + facts } }, false, Math.Max(Cfg.MaxTokens.Value, 220));
                using (var resp = await Http.PostAsync(Llm.ChatUrl(), new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json")))
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    if (!resp.IsSuccessStatusCode) throw new Exception("the AI server returned " + (int)resp.StatusCode);
                    string t = System.Text.RegularExpressions.Regex.Replace(Llm.FullText(json) ?? "", @"<think>[\s\S]*?</think>", "");
                    t = System.Text.RegularExpressions.Regex.Replace(t, @"\[[a-zA-Z ]{1,20}\]|\*[^*\n]{1,80}\*", "");
                    t = System.Text.RegularExpressions.Regex.Replace(t, @"\s{2,}", " ").Trim().Trim('"');
                    if (t.Length == 0) throw new Exception("the model sent no text");
                    e.note = t; Save(e); Note = t;
                    Pending.Enqueue("Stream: " + bot + "'s private feelings about it afterwards (her own notes; when talking about the stream, use these as feelings and say it in new words, never recite them): " + t);         // written to memory on the main thread
                    Cfg.Log.LogInfo("Stream: " + bot + " wrote a note about the stream (" + t.Length + " chars)");
                }
            }
            catch (System.Net.Http.HttpRequestException) { Note = ""; Cfg.Log.LogWarning("Stream: no note, " + Llm.NotReachable); }
            catch (Exception ex) { Note = ""; Cfg.Log.LogWarning("Stream: no note (" + ex.Message + ")"); }
            finally { NoteBusy = false; }
        });
    }
}

// ---- streamhub.ic: the fictional stream site, shown inside the game's own browser on Anon's PC ----
// The desktop gets a browser shortcut "streamhub.ic" (the game's own kind of icon). The game's browser does not know the address
// (it shows its 404 page); while it is on streamhub.ic, our Go live page is laid over its page area, so it looks and works like a real site.
public static class StreamSite
{
    static Sprite icon; static bool iconLogged, siteLogged;
    static readonly List<Il2CppSystem.Object> keep = new List<Il2CppSystem.Object>();
    static volatile bool liveWanted, titleWanted, descWanted, rollWanted;
    static volatile string rolled;
    static PCStaticGui lastPc; static BrowserPopup browser;
    static GameObject page; static TMP.TextMeshProUGUI titleText, descText, statusText, rulesText;
    static bool rolling; static float nextLook;
    static readonly System.Random rnd = new System.Random();
    static readonly string[] Titles = { "cozy late night chat", "first stream jitters", "answering your questions!", "just chatting with chat", "robot girl tries streaming",
        "come hang out", "tea, rain and chat", "ask me anything (nicely)", "quiet stream, loud chat", "my first stream ever??" };

    // PCStaticGui.OnEnable builds the desktop icons; ours is a browser shortcut next to the game's own
    public static void DesktopPostfix(PCStaticGui __instance)
    {
        try
        {
            if (!Mod.Chat || __instance == null || !Cfg.StreamEnabled.Value) return;
            lastPc = __instance;
            var ic = __instance.AddBrowserShortcutIcon("streamhub.ic", null);
            try { if (ic != null) { ic.iconImage.sprite = Icon(); ic.IconText.text = "streamhub.ic"; } } catch { }
            if (!iconLogged) { iconLogged = true; Cfg.Log.LogInfo("Stream: streamhub.ic shortcut added to the PC desktop"); }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not add the streamhub.ic shortcut (" + ex.Message + ")"); }
    }

    static Sprite Icon()
    {
        if (icon != null) return icon;
        int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float cx = Mathf.Max(Mathf.Abs(x - 31.5f) - 20f, 0f), cy = Mathf.Max(Mathf.Abs(y - 31.5f) - 20f, 0f);
                bool inside = cx * cx + cy * cy <= 100f;
                bool tri = x >= 24 && x <= 44 && Mathf.Abs(y - 31.5f) <= (44 - x) * 0.55f;
                t.SetPixel(x, y, !inside ? new Color(0f, 0f, 0f, 0f) : tri ? Color.white : new Color(0.9f, 0.16f, 0.3f, 1f));
            }
        t.Apply(); t.hideFlags = HideFlags.DontUnloadUnusedAsset;
        icon = Sprite.Create(t, new UnityEngine.Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f); icon.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return icon;
    }

    static string Bot() { return Shared.Current != null ? Shared.Current.Bot : "Jun"; }

    static Image Btn(Transform parent, TMP.TMP_FontAsset font, string label, float w, float h, float x, float y, Color c, Action onClick, float size = 26f)
    {
        var rt = StreamMode.R("btn", parent, w, h, x, y);
        var img = rt.gameObject.AddComponent<Image>(); img.color = c;
        var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
        var ua = DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(onClick); b.onClick.AddListener(ua); keep.Add(ua);
        StreamMode.Text(rt, font, label, size, w - 16f, h, 0f, 0f, TMP.TextAlignmentOptions.Center);
        return img;
    }

    static bool Alive(GameObject g) { if (g == null) return false; try { var _ = g.activeSelf; return true; } catch { return false; } }

    // every 0.3 s: is the game's browser open on streamhub.ic? then our page sits over its page area, otherwise it goes
    static void Look()
    {
        BrowserPopup b = null;
        try { foreach (var x in UnityEngine.Object.FindObjectsOfType<BrowserPopup>()) if (x != null && x.gameObject.activeInHierarchy) { b = x; break; } } catch { }
        string site = null; try { site = b != null ? b.CurrentWebsite : null; } catch { }
        bool want = b != null && site != null && site.IndexOf("streamhub", StringComparison.OrdinalIgnoreCase) >= 0 && !StreamMode.Live;
        if (!want) { if (Alive(page)) Remove(); browser = null; return; }
        if (Alive(page) && browser != null && browser.Pointer == b.Pointer) return;
        Remove(); browser = b;
        Transform area = null;
        try { area = b._websiteScrollRect != null ? (b._websiteScrollRect.viewport != null ? b._websiteScrollRect.viewport : b._websiteScrollRect.transform) : null; } catch { }
        if (area == null) { Cfg.Log.LogWarning("Stream: the browser's page area was not found"); return; }
        Build(area);
        if (!siteLogged) { siteLogged = true; Cfg.Log.LogInfo("Stream: streamhub.ic shown in the game's browser"); }
    }

    static void Remove()
    {
        try { if (page != null) UnityEngine.Object.Destroy(page); } catch { }
        page = null; titleText = descText = statusText = null;
    }

    static void Build(Transform area)
    {
        if (StreamMode.Title.Length == 0) StreamMode.Title = Titles[rnd.Next(Titles.Length)];
        if (StreamMode.Description.Length == 0) StreamMode.Description = "hi chat! it's " + Bot() + ". come hang out :)";
        page = new GameObject("StreamHubSite");
        var rt = page.AddComponent<RectTransform>(); rt.SetParent(area, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; rt.SetAsLastSibling();
        page.AddComponent<Image>().color = new Color(0.07f, 0.05f, 0.1f, 1f);     // covers the game's 404 text
        var top = page.transform; var font = StreamMode.Font();
        Color field = new Color(0.17f, 0.12f, 0.24f, 1f), red = new Color(0.9f, 0.16f, 0.3f, 1f), dim = new Color(1f, 1f, 1f, 0.6f);
        if (!Requirements.Ready)
        {
            StreamMode.Text(top, font, "<b><color=#E8294D>stream</color>hub</b>", 60f, 900f, 80f, 0f, 120f, TMP.TextAlignmentOptions.Center);
            StreamMode.Text(top, font, "Not available yet:\n" + Requirements.Why(), 26f, 1100f, 200f, 0f, -40f, TMP.TextAlignmentOptions.Center).color = dim;
            return;
        }
        StreamMode.Text(top, font, "<b><color=#E8294D>stream</color>hub</b>", 60f, 600f, 80f, -320f, 300f, TMP.TextAlignmentOptions.Left);
        StreamMode.Text(top, font, "<b>Go live</b>  <alpha=#99><size=70%>as " + Bot() + "</size>", 30f, 600f, 44f, -320f, 240f, TMP.TextAlignmentOptions.Left);

        StreamMode.Text(top, font, "Stream title", 22f, 600f, 30f, -320f, 180f, TMP.TextAlignmentOptions.Left).color = dim;
        Btn(top, font, "", 720f, 56f, -260f, 138f, field, () => titleWanted = true);
        titleText = StreamMode.Text(top, font, "", 26f, 690f, 56f, -260f, 138f, TMP.TextAlignmentOptions.Left);
        Btn(top, font, "Random", 150f, 56f, 195f, 138f, new Color(0.32f, 0.2f, 0.5f, 1f), () => rollWanted = true, 22f);

        StreamMode.Text(top, font, "Description", 22f, 600f, 30f, -320f, 70f, TMP.TextAlignmentOptions.Left).color = dim;
        Btn(top, font, "", 890f, 120f, -175f, -2f, field, () => descWanted = true);
        descText = StreamMode.Text(top, font, "", 22f, 860f, 110f, -175f, -2f, TMP.TextAlignmentOptions.TopLeft);

        Btn(top, font, "<b>●  Go Live</b>", 300f, 72f, -470f, -180f, red, () => liveWanted = true, 30f);
        statusText = StreamMode.Text(top, font, "Click a field to change it.", 22f, 560f, 40f, 0f, -180f, TMP.TextAlignmentOptions.Left);
        statusText.color = dim;

        StreamMode.Box("camBox", top, 320f, 380f, 450f, 50f, field);
        StreamMode.Text(top, font, "Camera", 22f, 300f, 30f, 450f, 265f, TMP.TextAlignmentOptions.Left).color = dim;
        StreamMode.Text(top, font, "The camera turns on when you go live.", 20f, 280f, 120f, 450f, 50f, TMP.TextAlignmentOptions.Center);
        StreamMode.Box("rulesBox", top, 320f, 150f, 450f, -232f, field);
        rulesText = StreamMode.Text(top, font, "", 18f, 290f, 140f, 450f, -232f, TMP.TextAlignmentOptions.TopLeft);
        StreamMode.Text(top, font, (Cfg.StreamPayout.Value ? "streamhub.ic is a made-up site with made-up viewers. Donations are paid out to you in game money (once a day, minus the platform's cut)." : "streamhub.ic is a made-up site. Everything here is made up: viewers, chat and money."), 18f, 1200f, 30f, 0f, -330f, TMP.TextAlignmentOptions.Center).color = new Color(1f, 1f, 1f, 0.4f);
        Refresh();
    }

    static void Refresh()
    {
        try
        {
            if (titleText != null) titleText.text = "<noparse>" + StreamMode.Title + "</noparse>";
            if (descText != null) descText.text = "<noparse>" + StreamMode.Description + "</noparse>";
            if (rulesText != null)
            {
                bool ok = StreamMode.DressProblem() == null;
                rulesText.text = "<b>Stream rules</b>\n1. be kind to chat\n2. streamers must be politely dressed  " + (ok ? "<color=#7CE38B>(ok)</color>" : "<color=#FF6B6B>(not yet)</color>") + "\n3. keep it SFW";
            }
        }
        catch { }
    }

    static volatile bool refreshWanted;
    static void Ask(string title, string text, string current, Action<string> done)
    {
        var ui = UiOverlay.Instance; if (ui == null) return;
        var cb = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<string>>(new Action<string>(v => { string t = (v ?? "").Replace("\n", " ").Trim(); if (t.Length > 0) done(t.Length > 140 ? t.Substring(0, 140) : t); refreshWanted = true; }));
        keep.Add(cb); if (keep.Count > 60) keep.RemoveAt(0);
        ui.InputPopup(title, text, cb, current, default);      // a game popup: it opens on top of the browser
    }

    static void Roll()
    {
        if (rolling) return;
        rolling = true; if (statusText != null) statusText.text = "Thinking of a title...";
        string bot = Bot();
        System.Threading.Tasks.Task.Run(async () =>
        {
            string t = null;
            try
            {
                using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(60) })
                {
                    string body = Llm.Body(new object[] {
                        new { role = "system", content = "You name VTuber streams. Output only the title, nothing else." },
                        new { role = "user", content = "Write one short, fun, SFW stream title for " + bot + "'s cozy VTuber stream. " + bot + " is a shy, polite robot girl. Under 8 words, no emoji, no quotes." } }, false, 40);
                    using (var resp = await http.PostAsync(Llm.ChatUrl(), new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json")))
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        if (resp.IsSuccessStatusCode) t = System.Text.RegularExpressions.Regex.Replace(Llm.FullText(json) ?? "", @"<think>[\s\S]*?</think>", "").Trim().Split('\n')[0].Trim().Trim('"', '\'', '*').Trim();
                    }
                }
            }
            catch { }
            if (string.IsNullOrEmpty(t) || t.Length > 80) t = Titles[rnd.Next(Titles.Length)];
            rolled = t;
        });
    }

    public static void Tick()
    {
        float now = Time.unscaledTime;
        if (now >= nextLook) { nextLook = now + 0.3f; try { Look(); } catch (Exception ex) { Cfg.Log.LogWarning("Stream: site check failed (" + ex.Message + ")"); nextLook = now + 5f; } }
        if (refreshWanted) { refreshWanted = false; Refresh(); }
        if (titleWanted) { titleWanted = false; Ask("Stream title", "Give your stream a title:", StreamMode.Title, t => StreamMode.Title = t); }
        if (descWanted) { descWanted = false; Ask("Description", "What is the stream about?", StreamMode.Description, t => StreamMode.Description = t); }
        if (rollWanted) { rollWanted = false; Roll(); }
        if (rolled != null) { StreamMode.Title = rolled; rolled = null; rolling = false; Refresh(); if (statusText != null) statusText.text = "New title! Click it to change it."; }
        if (liveWanted)
        {
            liveWanted = false;
            StreamMode.dressLogged = false;                                  // log the outfit once per Go Live click
            string dress = StreamMode.DressProblem();
            if (dress != null)
            {
                Cfg.Log.LogInfo("Stream: not going live, stream rules: " + dress);
                if (statusText != null) statusText.text = "<color=#FF6B6B>streamhub.ic rules: streamers must be politely dressed. " + dress + ".</color>";
                Refresh();
                return;
            }
            Cfg.Log.LogInfo("Stream: going live from streamhub.ic: \"" + StreamMode.Title + "\"");
            Remove();
            try { if (browser != null) browser.CloseFromUIOverlay(); } catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not close the browser (" + ex.Message + ")"); }
            browser = null;
            // back to the room: the game only draws her there, and the camera needs her
            try { if (lastPc != null && lastPc.ReturnButton != null) lastPc.ReturnButton.onClick.Invoke(); } catch (Exception ex) { Cfg.Log.LogWarning("Stream: could not leave the PC (" + ex.Message + ")"); }
            StreamMode.RequestStart();
        }
    }
}
#endif
