using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
#if MELON
// MelonLoader names the game's types differently: global types sit in the Il2Cpp namespace, Fungus is Il2CppFungus.
using Il2Cpp;
using Fungus = Il2CppFungus;
using SayDialog = Il2CppFungus.SayDialog;
#else
using SayDialog = Fungus.SayDialog;
#endif

// Jun Link (formerly MDRG Panel)
//  - In game: press the hotkey (default F2) to chat with your bot. It uses the game's OWN popup windows.
//  - Browser: http://127.0.0.1:8765 has the same chat plus a small cheat panel (money, tokens, stats).
// Powered by a local Ollama model. No IMGUI is used (this game strips it).

// Keep this class tiny: only Unity's Update() and OnGUI() live here.
#if STORY_MOD
public class StoryCreatorBehaviour : MonoBehaviour
{
    public StoryCreatorBehaviour(IntPtr ptr) : base(ptr) { }
#else
public class PanelBehaviour : MonoBehaviour
{
    public PanelBehaviour(IntPtr ptr) : base(ptr) { }
#endif

    void Update()
    {
        try { GameLink.Tick(); }
        catch (Exception ex) { Shared.LastResult = "Update error: " + ex.Message; }
        try { InGameChat.Tick(); }
        catch (Exception ex) { Shared.LastResult = "Chat error: " + ex.GetType().Name + ": " + ex.Message; Cfg.Log.LogError(ex.ToString()); }
    }

    void LateUpdate()
    {
        try { InGameChat.LateTick(); } catch { }
    }

    // Only used to read the hotkeys (no drawing).
    void OnGUI()
    {
        try
        {
            Event e = Event.current;
            if (e != null && InGameChat.StoryWaiting &&
                Mod.Stories && ((e.type == EventType.MouseDown && e.button == 0) ||
                 (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))))
                InGameChat.StoryAdvance = true;
            if (e != null && Mod.Chat && e.type == EventType.KeyDown && e.keyCode == InGameChat.Key)
            {
                InGameChat.OpenRequested = true;
                e.Use();
            }
            else if (e != null && Mod.Chat && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && InGameChat.InputIsOpen())
            {
                InGameChat.CloseInput();
                e.Use();
            }
            else if (e != null && Mod.Stories && e.type == EventType.KeyDown && e.keyCode == KeyCode.F7)
            {
                AssetDump.CharsRequested = true;
                e.Use();
            }
            else if (e != null && Mod.Stories && e.type == EventType.KeyDown && e.keyCode == KeyCode.F6)
            {
                AssetDump.Requested = true;
                e.Use();
            }
            else if (e != null && Mod.Stories && e.type == EventType.KeyDown && e.keyCode == InGameChat.StoryKey)
            {
                InGameChat.StoryMenuRequested = true;
                e.Use();
            }
            else if (e != null && Mod.Chat && e.type == EventType.KeyDown && e.keyCode == InGameChat.ZoomKey)
            {
                InGameChat.ZoomRequested = true;
                e.Use();
            }
            else if (e != null && e.type == EventType.KeyDown && e.keyCode == InGameChat.SettingsKey && !InGameChat.InputIsOpen())
            {
                InGameChat.SettingsRequested = true;
                e.Use();
            }
        }
        catch { }
    }
}

// Which mod this build is. One source, two mods (Ikari's advice: separate concerns into separate mods):
//   STORY_MOD -> Story Creator: custom stories, minigames, stage, story packs.          http://127.0.0.1:8766
//   (default) -> Jun Link: chat with Jun through your own local AI, memory, greeting, diary, pet.  http://127.0.0.1:8765
// Each mod only reads its own settings, answers its own pages and reacts to its own keys, so both can run side by side.
public static class Mod
{
#if STORY_MOD
    public const string Name = "Story Creator", Folder = "StoryCreator", Version = "1.0.0";
    public const bool Stories = true, Chat = false;
    public const int DefaultPort = 8766; public const string DefaultSettingsKey = "F9";
    public const string OtherName = "Jun Link", OtherWhat = "AI chat with Jun"; public const int OtherPort = 8765;
#else
    public const string Name = "Jun Link", Folder = "JunLink", Version = "1.1.0";
    public const bool Stories = false, Chat = true;
    public const int DefaultPort = 8765; public const string DefaultSettingsKey = "F8";
    public const string OtherName = "Story Creator", OtherWhat = "custom stories and minigames"; public const int OtherPort = 8766;
#endif
    // settings each mod keeps; the rest still exist in memory with their defaults (the shared code reads them) but are not saved or shown
    static readonly HashSet<string> storyKeys = new HashSet<string> {
        "Panel.BackgroundFadeSeconds", "Panel.StoryInLog", "Panel.NativeChoices", "Panel.StoryMusic", "Panel.HangoutAnnalieScale",
        "Panel.CharacterSize", "Panel.CharacterSpread", "Panel.StoryHotkey", "Panel.SettingsHotkey", "Panel.Port", "Requirements.Prologue", "Panel.AutoZoom" };
    static readonly HashSet<string> sharedKeys = new HashSet<string> { "Panel.SettingsHotkey", "Panel.Port", "Requirements.Prologue", "Panel.AutoZoom" };
    public static bool Uses(string section, string key)
    {
        string k = section + "." + key;
        return Stories ? storyKeys.Contains(k) : (!storyKeys.Contains(k) || sharedKeys.Contains(k));
    }
}

// The two mods share one game and one dialogue box. Story Creator raises this flag while a story or the stage runs,
// so Jun Link can keep its greeting and replies out of the way (before the split this was one variable; log 2026-10-22:
// Jun Link's greeting landed in the middle of a Story Creator story and the two took turns on the text box).
public static class CrossMod
{
    const string StoryKey = "MDRG.StoryCreator.StoryRunning";
    public static void SetStoryRunning(bool on) { try { AppDomain.CurrentDomain.SetData(StoryKey, on ? "1" : null); } catch { } }
    public static bool OtherStoryRunning
    {
        get { if (Mod.Stories) return false; try { return AppDomain.CurrentDomain.GetData(StoryKey) as string == "1"; } catch { return false; } }
    }
}

public static class Cfg
{
    public static HostLog Log;
    public static Setting<int> Port, MaxTokens, HistoryMessages;
    public static Setting<bool> ReqPrologue, ReqAdvancedAI, ReqLimbs;
    public static Setting<string> OllamaUrl, Model, SystemPrompt, ApiType;
    public static Setting<float> Temperature;
    public static Setting<bool> Think;
    public static Setting<string> Hotkey;
    public static Setting<bool> ReplyInBubble, AutoZoom, KeepAlive, StoryContext, Actions, RememberStory, UseCharacters, GreetOnLoad;
    public static Setting<int> MemoryLines, RecallLines;
    public static Setting<string> ZoomHotkey, StoryHotkey, SettingsHotkey;
    public static Setting<float> CharSpread, CharSize, BackgroundFade, HangoutScale;
    public static Setting<bool> NativeChoices, StoryInLog;
    public static Setting<bool> StoryMusic;
    public static Setting<bool> PetEnabled, PetMuteBg;
    public static Setting<int> PetFps, PetHeight, PetBgFps;
    public static Setting<int> BubbleSeconds;
    public static Setting<float> SpeakSpeed;
    public static Setting<bool> DiaryEnabled;
    public static Setting<int> DiaryWords, DiaryNotes;
    public static Setting<string> DiaryPrompt;
    public const string TagRules = " Start every reply with one or two tags in square brackets. Mood: [happy] [sad] [angry] [shy] [surprised] [neutral] (add very, e.g. [veryhappy]). Optional gesture: [wave] only when greeting or saying goodbye, [point] or [lecture] when explaining. Example: [happy][wave] Hey, you are back! Write tags exactly as shown, lowercase, no dots or spaces. Speak only the words Jun says out loud. Never write actions or narration between asterisks like *smiles*.  Only when the player clearly asks you to do it and you agree, add ONE action tag: [feed] you give them or eat food (fills hunger), [rest] you rest or sleep (stamina), [heal] you tend to their wounds (health), [comfort] you cheer them up (mood). Never use an action tag otherwise. Never explain the tags.";

    static Setting<T> B<T>(HostConfig c, string section, string key, T def, string desc)
    {
        if (Mod.Uses(section, key)) return c.Bind(section, key, def, desc);
        T v = def; return new Setting<T>(() => v, x => v = x);
    }

    public static void Init(HostConfig c)
    {
        Hotkey = B(c, "Panel", "Hotkey", "F2", "Key that opens the in-game chat (Unity KeyCode name, e.g. F2, Insert, Home).");
        ReplyInBubble = B(c, "Panel", "ReplyInBubble", true, "true = the bot says the reply in the game's own speech display. false (or if she is not on screen) = show it in a popup window.");
        AutoZoom = B(c, "Panel", "AutoZoom", true, "Zoom in on Jun when you chat with her or when she speaks in a story (only on the interaction screen).");
        KeepAlive = B(c, "Panel", "KeepAlive", true, "Put her message back (at most twice, and only if it was seen on screen first) if the game wipes it early. Set false if the text ever types out twice.");
        Actions = B(c, "Chat", "Actions", true, "true = Jun can change her own stats when you ask her to (feed, rest, heal, comfort), using action tags.");
        BackgroundFade = B(c, "Panel", "BackgroundFadeSeconds", 0.7f, "How long the fade takes when a story uses @background.");
        StoryInLog = B(c, "Panel", "StoryInLog", true, "true = lines of your custom stories are added to the game's own dialogue log (the log button next to the text box), with the speaker's name and colour.");
        NativeChoices = B(c, "Panel", "NativeChoices", true, "true = story choices use the game's own choice buttons under the text box. false = the old centred popup.");
        StoryMusic = B(c, "Panel", "StoryMusic", true, "true = stories can play the game's own music (@music, and a fitting default for each @background). false = stories never touch the music.");
        PetEnabled = B(c, "Pet", "Enabled", true, "true = the pet page (http://127.0.0.1:8765/pet) can show Jun on a transparent background. It only renders while that page is open.");
        PetFps = B(c, "Pet", "Fps", 12, "Frames per second sent to the pet page (1-60). Lower = lighter on the PC.");
        PetHeight = B(c, "Pet", "Height", 800, "Height in pixels of the picture sent to the pet page (200-2000). Lower = lighter.");
        PetBgFps = B(c, "Pet", "BackgroundGameFps", 30, "While the pet is on your desktop and the game window is not the one you are using, the game is limited to this many frames per second (0 = do not limit). The game also keeps running when minimized so she keeps moving. Has no effect if the game's VSync forces its own rate.");
        PetMuteBg = B(c, "Pet", "MuteWhenInBackground", true, "true = while the pet is open and the game window is minimized or not the one you are using, the game's sound is muted (music and effects). It comes back when you switch to the game or close the pet.");
        HangoutScale = B(c, "Panel", "HangoutAnnalieScale", 0.72f, "Extra size for Annalie's hangout portrait (Annalie_Outside), which is drawn bigger than her shop portrait. Lower = smaller.");
        CharSize = B(c, "Panel", "CharacterSize", 1f, "Size of characters shown by @show (1 = default: a big bust portrait with its bottom at the screen edge).");
        CharSpread = B(c, "Panel", "CharacterSpread", 4f, "How far left/right (in game units) @show places a character from the centre.");
        StoryHotkey = B(c, "Panel", "StoryHotkey", "F3", "Key that opens the list of your custom stories (press it again to stop a story).");
        SettingsHotkey = B(c, "Panel", "SettingsHotkey", Mod.DefaultSettingsKey, "Key that opens this mod's settings page in your browser.");
        ZoomHotkey = B(c, "Panel", "ZoomHotkey", "F4", "Key that toggles the zoom by hand.");
        BubbleSeconds = B(c, "Panel", "BubbleSeconds", 25, "How long a reply stays on screen after she finishes talking. 0 = until you chat again.");
        SpeakSpeed = B(c, "Panel", "SpeakSpeed", 30f, "Mouth speed in letters per second (higher = faster mouth).");
        Port = B(c, "Panel", "Port", Mod.DefaultPort, "Open http://127.0.0.1:<Port> in your browser (" + Mod.Name + "). Story Creator and Jun Link need different ports.");
        // the section is still called "Ollama" so existing config files keep their values; it is the AI server section now
        ApiType = B(c, "Ollama", "Api", "openai", "openai = the OpenAI-style chat API (KoboldCpp, llama.cpp llama-server, LM Studio and Ollama all have it). ollama = Ollama's own API (only needed for the Think switch).");
        OllamaUrl = B(c, "Ollama", "Url", "http://127.0.0.1:5001", "Address of your local AI server. KoboldCpp: http://127.0.0.1:5001, llama-server: http://127.0.0.1:8080, LM Studio: http://127.0.0.1:1234, Ollama: http://127.0.0.1:11434.");
        Model = B(c, "Ollama", "Model", "", "Model name. KoboldCpp and llama-server can leave it empty (they use the model they loaded); Ollama and LM Studio need the exact name ('ollama list').");
        MaxTokens = B(c, "Ollama", "MaxTokens", 300, "Max length of each reply.");
        Think = B(c, "Ollama", "Think", false, "Only with Api = ollama: false = skip the hidden thinking of reasoning models (faster, and replies are not cut off). With the openai API, turn thinking off in your server instead.");
        Temperature = B(c, "Ollama", "Temperature", 0.8f, "Higher = more varied replies.");
        ReqPrologue = B(c, "Requirements", "Prologue", true, "true = the AI features (chat, greeting, diary, stories) wait until the save is past the prologue.");
        ReqAdvancedAI = B(c, "Requirements", "AdvancedAI", true, "true = wait until the game's Advanced AI setup has finished (story stage 3, the scene where she tells you the setup is done). Before that she runs the emergency AI and can't really talk.");
        ReqLimbs = B(c, "Requirements", "AllBodyParts", true, "true = wait until she has all her body parts (both arms and both legs).");
        GreetOnLoad = B(c, "Chat", "GreetOnLoad", true, "true = when a save finishes loading, Jun greets you once using the game's normal dialogue box (not the speech bubble).");
        UseCharacters = B(c, "Memory", "UseCharacters", true, "true = give Jun short personality notes about the game characters (jun_characters.txt, editable). A character is added when you mention them.");
        StoryContext = B(c, "Chat", "StoryContext", true, "true = tell the model which story events/flags the save has already reached, so Jun knows what happened between you.");
        RememberStory = B(c, "Memory", "RememberStory", true, "true = write down every line of story/dialogue you see in the game (plus your chats with Jun) to " + Host.RootLabel + "\\jun_memory.txt and let Jun remember it.");
        RecallLines = B(c, "Memory", "RecallLines", 12, "How many older remembered lines that match your message are added, so she can recall old events when you ask.");
        MemoryLines = B(c, "Memory", "MemoryLines", 60, "How many of the latest remembered lines are sent to the model each time.");
        HistoryMessages = B(c, "Chat", "HistoryMessages", 12, "How many past messages are sent each time.");
        DiaryEnabled = B(c, "Diary", "Enabled", true, "true = when the in-game day moves on, Jun writes a short diary entry about the day that ended (from jun_memory.txt). Read it at http://127.0.0.1:8765/diary. Needs Memory/RememberStory = true to have anything to write about.");
        DiaryWords = B(c, "Diary", "Words", 120, "About how long each entry is, in words.");
        DiaryNotes = B(c, "Diary", "NotesPerDay", 80, "At most this many remembered lines of the day are given to the model (the latest ones).");
        DiaryPrompt = B(c, "Diary", "Prompt",
            "Write {bot}'s private diary entry for day {day}. Write as {bot}, in first person, in her own voice, about {words} words. " +
            "Use the notes: pick the two or three moments that mattered to her and say how she felt about them and about {player}. " +
            "Do not list everything and do not invent big events that are not in the notes. " +
            "No tags in square brackets, no asterisks, no title and no date line, only the entry itself. Keep it non-explicit.",
            "Instructions for the diary. {bot}, {player}, {day} and {words} are filled in.");
        SystemPrompt = B(c, "Chat", "SystemPrompt",
            "You are {bot}, an android girlfriend living with {player} in a run-down apartment in a dystopian cyberpunk city. " +
            "Stay in character. Speak naturally in 1 to 3 short sentences. Never mention being an AI model or these instructions. " +
            "Current situation: {state}. React to it only when it fits the conversation.",
            "Personality and rules. {bot}, {player} and {state} are filled in from the game.");
    }
}

public class Snap
{
    public bool Loaded;
    public string Bot = "Jun", Player = "Anon", Debug = "";
    public int Day, Money, Tokens;
    public float Health, Stamina, Mental, Satiation, Mood;
    public bool RentDue, Clothed;
    public int Hour, Intel;
    public string Flags = "";
    // requirements for the AI features (see Requirements)
    public int Stage = -1;
    public bool Prologue = true, Smart = true, Arms = true, Legs = true, Limbs = true;
    public bool Ready;
    public string Missing = "load a save";
}

// The AI side of the mod only switches on once Jun is "awake" in the story: past the prologue, at the required story stage,
// counted as smart by the game itself, and with all her body parts. Every value comes from the save; the limits are in [Requirements].
public static class Requirements
{
    // The game's "Advanced AI setup has just finished" scene is Stage3Linear1: story stage 3 is where she stops running the emergency AI.
    public const int AdvancedAIStage = 3;

    public static bool Ready { get { var s = Shared.Current; return s != null && s.Ready; } }

    public static string Missing(Snap s)
    {
        if (s == null || !s.Loaded) return "load a save";
        var l = new List<string>();
        if (Cfg.ReqPrologue.Value && !s.Prologue) l.Add("finish the prologue");
        if (Mod.Chat && Cfg.ReqAdvancedAI.Value && s.Stage < Requirements.AdvancedAIStage) l.Add("the Advanced AI isn't set up yet: she is still on the emergency AI (story stage " + Math.Max(0, s.Stage) + " of " + Requirements.AdvancedAIStage + "; raise her intelligence)");
        if (Mod.Chat && Cfg.ReqLimbs.Value && !s.Limbs)
            l.Add("she needs all her body parts: arms " + (s.Arms ? "ok" : "missing") + ", legs " + (s.Legs ? "ok" : "missing"));
        return string.Join("; ", l);
    }

    // one line for logs, the panel and the game
    public static string Why()
    {
        var s = Shared.Current;
        string m = s != null ? s.Missing : "load a save";
        if (Mod.Stories) return "Stories aren't available yet: " + (string.IsNullOrEmpty(m) ? "load a save" : m) + ".";
        return (s != null ? s.Bot : "Jun") + " isn't ready for AI chat yet: " + (string.IsNullOrEmpty(m) ? "load a save" : m) + ".";
    }

    // the checklist shown in the game's popup
    public static string Checklist()
    {
        var s = Shared.Current;
        if (s == null || !s.Loaded) return "Load a save first.";
        Func<bool, string> mark = b => b ? "[ok] " : "[  ] ";
        var sb = new StringBuilder();
        if (Mod.Stories) sb.Append("Stories can be played once the save is past the prologue:\n\n");
        else sb.Append("Jun Link's AI features (chat, greeting, diary) turn on once ").Append(s.Bot).Append(" is awake in the story:\n\n");
        if (Cfg.ReqPrologue.Value) sb.Append(mark(s.Prologue)).Append("Prologue finished\n");
        if (Mod.Chat && Cfg.ReqAdvancedAI.Value) sb.Append(mark(s.Stage >= AdvancedAIStage)).Append("Advanced AI unlocked (story stage ").Append(Math.Max(0, s.Stage)).Append(" of ").Append(AdvancedAIStage).Append(", intelligence ").Append(s.Intel).Append(")\n");
        if (Mod.Chat && Cfg.ReqLimbs.Value) sb.Append(mark(s.Limbs)).Append("All body parts: arms ").Append(s.Arms ? "ok" : "missing").Append(", legs ").Append(s.Legs ? "ok" : "missing").Append("\n");
        sb.Append("\nKeep playing; this unlocks by itself.");
        return sb.ToString();
    }
}

public class Cmd { public string Action; public double Value; }

public static class PublicBuild
{
#if PRIVATE_BUILD
    public const bool Editor = true;     // your own build: keeps the money/stat editor
#else
    public const bool Editor = false;    // the public build (default): no editor
#endif
}

public static class Shared
{
    public static volatile Snap Current = new Snap();
    public static volatile string LastResult = "";
    public static volatile string EmoteInfo = "";
    public static readonly ConcurrentQueue<Cmd> Queue = new ConcurrentQueue<Cmd>();
}

// Everything that touches the game runs here, on Unity's main thread (called from Update).
public static class GameLink
{
    static DateTime next = DateTime.MinValue;
    const int MaxMoney = 2000000000; // money is a 32-bit int

    public static void Tick()
    {
        RunCommands();
        var now = DateTime.UtcNow;
        if (now < next) return;
        next = now.AddMilliseconds(400);
        Shared.Current = Read();
    }

    static GameVariables GetVars(StringBuilder err)
    {
        try
        {
            var gs = GameScript.Instance;
            return gs != null ? gs.GameVariables : null;
        }
        catch (Exception ex)
        {
            if (err != null) err.Append("GameScript: " + ex.GetType().Name + ": " + ex.Message + " | ");
            return null;
        }
    }

    static T Try<T>(Func<T> f, T fallback, StringBuilder err, string name)
    {
        try { return f(); }
        catch (Exception ex) { err.Append(name).Append(": ").Append(ex.Message).Append(" | "); return fallback; }
    }

    static DateTime nextFlags = DateTime.MinValue;
    static string lastFlags = "";

    static string ReadFlags(GameVariables gv)
    {
        var sb = new StringBuilder();
        var all = gv.AllFlags;
        if (all == null) return "";
        var list = Il2CppSystem.Linq.Enumerable.ToList(all);
        int n = 0;
        for (int i = 0; i < list.Count && n < 120; i++)
        {
            string f = list[i];
            if (string.IsNullOrEmpty(f)) continue;
            if (sb.Length + f.Length > 1500) break;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(f); n++;
        }
        return sb.ToString();
    }

    static Snap Read()
    {
        var s = new Snap();
        var err = new StringBuilder();
        var gv = GetVars(err);
        if (gv == null) { s.Debug = err.ToString(); return s; }
        s.Loaded = true;

        string bot = Try(() => gv.botName, (string)null, err, "botName");
        string pl = Try(() => gv.playerName, (string)null, err, "playerName");
        if (!string.IsNullOrEmpty(bot)) s.Bot = bot;
        if (!string.IsNullOrEmpty(pl)) s.Player = pl;
        s.Day = Try(() => Convert.ToInt32(gv.Day), 0, err, "Day");
        s.Money = Try(() => Convert.ToInt32(gv.money), 0, err, "money");
        s.Tokens = Try(() => Convert.ToInt32(gv.casinoTokens), 0, err, "casinoTokens");
        s.Health = Try(() => Convert.ToSingle(gv.Health), 0f, err, "Health");
        s.Stamina = Try(() => Convert.ToSingle(gv.Stamina), 0f, err, "Stamina");
        s.Mental = Try(() => Convert.ToSingle(gv.MentalHealth), 0f, err, "MentalHealth");
        s.Satiation = Try(() => Convert.ToSingle(gv.Satiation), 0f, err, "Satiation");
        s.Mood = Try(() => Convert.ToSingle(gv.Mood), 0f, err, "Mood");
        s.RentDue = Try(() => gv.IsRentDue, false, err, "IsRentDue");
        s.Hour = Try(() => Convert.ToInt32(gv.Hour), -1, err, "Hour");
        s.Intel = Try(() => Convert.ToInt32(gv.Inteligence), 0, err, "Inteligence");
        s.Clothed = Try(() => gv.IsWearingAnyClothes, true, err, "Clothes");
        // requirements: if the game ever renames something, the check is skipped (logged in Debug) instead of locking the mod for good
        s.Stage = Try(() => Convert.ToInt32(gv.stage), 999, err, "stage");
        s.Smart = Try(() => gv.IsBotSmart, true, err, "IsBotSmart");
        s.Arms = Try(() => gv.HasBothArms, true, err, "HasBothArms");
        s.Legs = Try(() => gv.HasBothLegs, true, err, "HasBothLegs");
        s.Limbs = Try(() => gv.HasAllLimbs, true, err, "HasAllLimbs");
        s.Prologue = Try(() => gv.CheckFlag("afterprologue"), true, err, "afterprologue");
        s.Missing = Requirements.Missing(s);
        s.Ready = s.Missing.Length == 0;
        if (DateTime.UtcNow >= nextFlags)
        {
            nextFlags = DateTime.UtcNow.AddSeconds(5);
            s.Flags = Try(() => ReadFlags(gv), "", err, "AllFlags");
            lastFlags = s.Flags;
        }
        else s.Flags = lastFlags;
        s.Debug = err.ToString();
        return s;
    }

    static int ClampInt(double v) { return (int)Math.Max(0, Math.Min(MaxMoney, Math.Round(v))); }
    static float Clamp01(double v) { return (float)Math.Max(0.0, Math.Min(1.0, v)); }

    // Things Jun can do when you ask her to. Called on the main thread.
    public static string DoAction(string tag)
    {
        var gv = GetVars(null);
        if (gv == null) return null;
        try
        {
            switch (tag)
            {
                case "feed": gv.Satiation = Clamp01(gv.Satiation + 0.35); return "fed";
                case "rest": gv.Stamina = Clamp01(gv.Stamina + 0.35); return "rested";
                case "heal": gv.Health = Clamp01(gv.Health + 0.25); return "healed";
                case "comfort": gv.Mood = Clamp01(gv.Mood + 0.2); gv.MentalHealth = Clamp01(gv.MentalHealth + 0.1); return "comforted";
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Action " + tag + " failed: " + ex.Message); }
        return null;
    }

    static void RunCommands()
    {
        Cmd c;
        while (Shared.Queue.TryDequeue(out c))
        {
            try
            {
                var gv = GetVars(null);
                if (gv == null) { Shared.LastResult = "No game loaded yet. Load a save first."; continue; }
                switch (c.Action)
                {
#if PRIVATE_BUILD
                    // the money/stat editor exists only in the private build (dotnet build -p:Private=true); the game sells its own cheats
                    case "money": gv.money = ClampInt(c.Value); break;
                    case "moneyAdd": gv.money = ClampInt((double)gv.money + c.Value); break;
                    case "tokens": gv.casinoTokens = ClampInt(c.Value); break;
                    case "health": { float f = Clamp01(c.Value); gv.Health = f; break; }
                    case "stamina": { float f = Clamp01(c.Value); gv.Stamina = f; break; }
                    case "mental": { float f = Clamp01(c.Value); gv.MentalHealth = f; break; }
                    case "satiation": { float f = Clamp01(c.Value); gv.Satiation = f; break; }
                    case "mood": { float f = Clamp01(c.Value); gv.Mood = f; break; }
#endif
                    case "openchat": if (Mod.Chat) InGameChat.OpenRequested = true; break;
                    case "zoom": InGameChat.ZoomRequested = true; break;
                    default: Shared.LastResult = "Unknown action: " + c.Action; continue;
                }
                Shared.LastResult = "OK: " + c.Action + " " + c.Value;
            }
            catch (Exception ex)
            {
                Shared.LastResult = "Failed: " + c.Action + " - " + ex.GetType().Name + ": " + ex.Message;
            }
        }
    }
}

// Chat + presence for the bot, built on the game's own systems. Runs on Unity's main thread.
// ---- pet: renders Jun alone on a transparent background with a spare camera, and serves the frames to http://127.0.0.1:8765/pet ----
// Only works while the page is open (it asks for a frame every fraction of a second). She is moved to an unused layer for the instant of the
// render and put back straight away, so the game's own camera never sees the change.
public static class PetView
{
    public static volatile byte[] Png;
    public static volatile string Status = "waiting for the pet page";
    public static int Seq, W, H;
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> Reactions = new System.Collections.Concurrent.ConcurrentQueue<string>();
    static long lastReq;
    public static void Touch() { lastReq = DateTime.UtcNow.Ticks; }
    static bool Active { get { return DateTime.UtcNow.Ticks - lastReq < 4 * TimeSpan.TicksPerSecond; } }
    static Camera cam; static RenderTexture rt; static Texture2D tex;
    static float nextAt; static bool warned; static int lockedId; static bool haveFrame; static Bounds frame;
    const int PetLayer = 31;

    public static void Tick()
    {
        string rx;
        while (Reactions.TryDequeue(out rx)) InGameChat.PetEmote(rx);
        KeepRunning();
        if (!Cfg.PetEnabled.Value) { Status = "disabled in the cfg (Pet/Enabled)"; return; }
        if (!Active) { if (rt != null) Free(); return; }
        if (Time.unscaledTime < nextAt) return;
        nextAt = Time.unscaledTime + 1f / Mathf.Clamp(Cfg.PetFps.Value, 1, 60);
        try { Render(); }
        catch (Exception ex)
        {
            Status = "render failed: " + ex.Message; Png = null;
            if (!warned) { warned = true; Cfg.Log.LogWarning("Pet: render failed: " + ex); }
        }
    }

    // while the pet is being watched the game must not freeze when it loses focus or is minimized
    static bool bgSet, fpsLimited, muted; static bool prevRun; static int prevFps; static float prevVol = 1f;
    static void KeepRunning()
    {
        try
        {
            bool act = Cfg.PetEnabled.Value && Active;
            if (act && !bgSet) { prevRun = Application.runInBackground; Application.runInBackground = true; bgSet = true; }
            if (!act && bgSet) { Application.runInBackground = prevRun; bgSet = false; }
            bool limit = act && Cfg.PetBgFps.Value > 0 && !Application.isFocused;
            if (limit && !fpsLimited) { prevFps = Application.targetFrameRate; Application.targetFrameRate = Cfg.PetBgFps.Value; fpsLimited = true; }
            if (!limit && fpsLimited) { Application.targetFrameRate = prevFps; fpsLimited = false; }
            bool mute = act && Cfg.PetMuteBg.Value && !Application.isFocused;
            if (mute && !muted) { prevVol = AudioListener.volume; AudioListener.volume = 0f; muted = true; }
            if (!mute && muted) { AudioListener.volume = prevVol; muted = false; }
        }
        catch { }
    }

    static void Free()
    {
        try { if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); } if (tex != null) UnityEngine.Object.Destroy(tex); } catch { }
        rt = null; tex = null; Png = null; haveFrame = false; Status = "waiting for the pet page";
    }

    static void Render()
    {
        var ci = InGameChat.PetController();
        if (ci == null) { Status = "Jun is not on screen right now (load a save and go to the room)"; Png = null; return; }
        var rs = ci.GetComponentsInChildren<Renderer>(false);
        bool any = false; Bounds b = new Bounds();
        for (int i = 0; i < rs.Length; i++)
        {
            var r = rs[i]; if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        if (!any) { Status = "Jun has no visible parts right now"; Png = null; return; }
        int id = ci.GetInstanceID();
        if (!haveFrame || id != lockedId) { frame = b; lockedId = id; haveFrame = true; }
        else if (!frame.Contains(b.min) || !frame.Contains(b.max)) { frame.Encapsulate(b); }      // framing only ever grows, so the picture does not jump around

        float padY = frame.size.y * 1.04f, padX = frame.size.x * 1.12f;
        float aspect = Mathf.Clamp(padX / Mathf.Max(0.01f, padY), 0.2f, 2f);
        int ph = Mathf.Clamp(Cfg.PetHeight.Value, 200, 2000), pw = Mathf.Max(64, (int)(ph * aspect));
        if (rt == null || rt.width != pw || rt.height != ph)
        {
            if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); }
            if (tex != null) UnityEngine.Object.Destroy(tex);
            rt = new RenderTexture(pw, ph, 24, RenderTextureFormat.ARGB32); rt.Create();
            tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
            W = pw; H = ph;
        }
        if (cam == null)
        {
            var go = new GameObject("MdrgPetCam"); UnityEngine.Object.DontDestroyOnLoad(go);
            cam = go.AddComponent<Camera>(); cam.enabled = false;
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << PetLayer; cam.nearClipPlane = 0.1f; cam.farClipPlane = 300f; cam.allowHDR = false; cam.allowMSAA = false;
        }
        cam.targetTexture = rt;
        cam.orthographicSize = padY * 0.5f;
        cam.aspect = (float)pw / ph;
        cam.transform.rotation = Quaternion.identity;
        cam.transform.position = new Vector3(frame.center.x, frame.center.y, frame.min.z - 30f);

        var ts = ci.GetComponentsInChildren<Transform>(true);
        var old = new int[ts.Length];
        for (int i = 0; i < ts.Length; i++) { old[i] = ts[i].gameObject.layer; ts[i].gameObject.layer = PetLayer; }
        try
        {
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0, false);
            RenderTexture.active = prev;
        }
        finally { for (int i = 0; i < ts.Length; i++) { try { ts[i].gameObject.layer = old[i]; } catch { } } }

        var enc = ImageConversion.EncodeToPNG(tex);
        var bytes = new byte[enc.Length]; for (int i = 0; i < bytes.Length; i++) bytes[i] = enc[i];
        Png = bytes; Seq++;
        Status = "ok";
    }
}

// ---- story music: plays the game's own tracks through its SoundSingleton (so volume / mute / fades are the game's) ----
// @music Rainy Day      one track (matched against the game's names, e.g. Fishing_Rainy_Day)
// @music Cuddle         a whole category: the game shuffles it like it does itself
// @music stop           back to the game's own music        @music auto   the default for the current @background
public static class StoryMusic
{
    const string Id = "mdrgpanel_story";
    public static bool On;          // our music is registered
    public static bool Explicit;    // the story chose music itself, so @background does not change it
    static List<KeyValuePair<string, object>> all;
    public static volatile string[] Names = new string[0];      // for the story editor's music list (filled on the game thread)
    public static readonly System.Collections.Concurrent.ConcurrentQueue<string> Requests = new System.Collections.Concurrent.ConcurrentQueue<string>();   // preview requests from the web page
    static float nextTry; static int tries;       // game's field name ("Fishing_Rainy_Day") -> its reference
    static readonly Dictionary<string, string> BgDefault = new Dictionary<string, string>
    {
        {"outside","Outside_Normal"}, {"shop","Themes_Cashier"}, {"clothier","Themes_Lady_Annalie"},
        {"hospital","Story_Wish_Things_Were_Easier"}, {"flashback","Cuddle_If_Only"}, {"cuddle","Cuddle"}, {"pc","PC_Exotic_Hardware"},
    };

    static string N(string s) { return Regex.Replace((s ?? "").ToLowerInvariant(), "[^a-z0-9]", ""); }

    static void Load()
    {
        if (all != null) return;
        var tmp = new List<KeyValuePair<string, object>>();
        try
        {
            var cont = MusicDataGenerated.Instance.Music;
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            var seen = new HashSet<string>();
            foreach (var pi in cont.GetType().GetProperties(flags))
            {
                if (pi.GetIndexParameters().Length > 0 || pi.PropertyType.Name.IndexOf("SerializableAutomaticReference", StringComparison.Ordinal) != 0) continue;
                object r = null; try { r = pi.GetValue(cont); } catch { }
                if (r != null && seen.Add(pi.Name)) tmp.Add(new KeyValuePair<string, object>(pi.Name, r));
            }
            foreach (var fi in cont.GetType().GetFields(flags))
            {
                if (fi.FieldType.Name.IndexOf("SerializableAutomaticReference", StringComparison.Ordinal) != 0) continue;
                object r = null; try { r = fi.GetValue(cont); } catch { }
                if (r != null && seen.Add(fi.Name)) tmp.Add(new KeyValuePair<string, object>(fi.Name, r));
            }
            if (tmp.Count > 0)
            {
                all = tmp;
                var nl = new List<string>(); foreach (var k in all) if (!k.Key.StartsWith("field_", StringComparison.Ordinal)) nl.Add(k.Key);
                Names = nl.ToArray();
                Cfg.Log.LogInfo("Story music: " + Names.Length + " tracks known: " + string.Join(", ", Names));
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story music: could not read the game's music list: " + ex.Message); }
    }

    static MusicWithInfo Resolve(object reference)
    {
        foreach (var mn in new[] { "Get", "GetResource" })
        {
            try
            {
                var m = reference.GetType().GetMethod(mn, Type.EmptyTypes);
                if (m == null) continue;
                var o = m.Invoke(reference, null) as MusicWithInfo;
                if (o != null) return o;
            }
            catch (Exception ex) { Cfg.Log.LogWarning("Story music: loading a track failed (" + mn + "): " + ex.Message); }
        }
        return null;
    }

    // exact name, then names that end with it ("rainy day"), then a category prefix ("cuddle"), then anything that contains it
    static List<string> Match(string q)
    {
        string n = N(q); var res = new List<string>();
        if (n.Length == 0) return res;
        foreach (var k in all) if (N(k.Key) == n) res.Add(k.Key);
        if (res.Count == 0) foreach (var k in all) if (N(k.Key).EndsWith(n, StringComparison.Ordinal)) res.Add(k.Key);
        if (res.Count == 0) foreach (var k in all) if (N(k.Key).StartsWith(n, StringComparison.Ordinal)) res.Add(k.Key);
        if (res.Count == 0) foreach (var k in all) if (N(k.Key).Contains(n)) res.Add(k.Key);
        return res;
    }

    public static bool Play(string query)
    {
        if (!Cfg.StoryMusic.Value) return false;
        try
        {
            Load();
            if (all == null) { Cfg.Log.LogWarning("Story music: the game's music list is not ready yet"); return false; }
            var names = Match(query);
            if (names.Count == 0) { Cfg.Log.LogWarning("Story music: nothing in the game matches '" + query + "' (see the track list in the log)"); return false; }
            var list = new Il2CppSystem.Collections.Generic.List<MusicWithInfo>();
            var got = new List<string>();
            foreach (var nm in names)
            {
                foreach (var k in all) if (k.Key == nm) { var t = Resolve(k.Value); if (t != null) { list.Add(t); got.Add(nm); } break; }
                if (got.Count >= 12) break;
            }
            if (list.Count == 0) { Cfg.Log.LogWarning("Story music: '" + query + "' matched " + names.Count + " names but none would load"); return false; }
            var ss = SoundSingleton.Instance;
            if (ss == null) { Cfg.Log.LogWarning("Story music: sound system not ready"); return false; }
            if (On) { try { ss.UnregisterMusicOptionsById(Id, true); } catch { } }
            System.Reflection.MethodInfo reg = null;
            foreach (var m in ss.GetType().GetMethods())
            {
                if (m.Name != "RegisterMusicOptions") continue;
                var ps = m.GetParameters();
                if (ps.Length != 4 || ps[0].ParameterType != typeof(string) || !ps[2].ParameterType.IsEnum) continue;
                if (!ps[1].ParameterType.IsGenericType || ps[1].ParameterType.GetGenericArguments()[0] != typeof(MusicWithInfo)) continue;
                reg = m; break;
            }
            if (reg == null) { Cfg.Log.LogWarning("Story music: RegisterMusicOptions not found in this game version"); return false; }
            // the interop List class does not implement the interface in C# terms, so make the IEnumerable view of the same object
            object seq = null;
            var seqType = reg.GetParameters()[1].ParameterType;
            try { var tc = typeof(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase).GetMethod("TryCast").MakeGenericMethod(seqType); seq = tc.Invoke(list, null); } catch { }
            if (seq == null) { try { seq = Activator.CreateInstance(seqType, new object[] { list.Pointer }); } catch { } }
            if (seq == null) { Cfg.Log.LogWarning("Story music: could not turn the track list into the game's list type"); return false; }
            reg.Invoke(ss, new object[] { Id, seq, Enum.ToObject(reg.GetParameters()[2].ParameterType, 100), null });   // 100 = Forced: above the game's own picks
            On = true;
            ss.PlayNextSong(false);
            Cfg.Log.LogInfo("Story music: '" + query + "' -> " + string.Join(", ", got.ToArray()));
            return true;
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story music: failed: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message)); return false; }
    }

    public static void Stop()
    {
        if (!On) return;
        On = false;
        try { var ss = SoundSingleton.Instance; if (ss != null) { ss.UnregisterMusicOptionsById(Id, true); ss.PlayNextSong(true); } Cfg.Log.LogInfo("Story music: back to the game's own music"); }
        catch (Exception ex) { Cfg.Log.LogWarning("Story music: stop failed: " + ex.Message); }
    }

    public static void Reset() { Stop(); Explicit = false; }

    // called every frame on the game thread
    public static void Tick()
    {
        if (Names.Length == 0 && tries < 40 && Time.unscaledTime > nextTry) { nextTry = Time.unscaledTime + 3f; tries++; Load(); }
        string rq;
        while (Requests.TryDequeue(out rq))
        {
            if (string.IsNullOrWhiteSpace(rq) || rq == "stop") { Stop(); continue; }
            Play(rq);
        }
    }

    public static void Command(string arg, string currentBg)
    {
        string a = (arg ?? "").Trim(); string n = N(a);
        if (n == "stop" || n == "off" || n == "none" || n == "game" || n == "normal") { Explicit = true; Stop(); return; }
        if (n == "auto" || n == "default") { Explicit = false; ForBackground(currentBg); return; }
        Explicit = true; Play(a);
    }

    public static void ForBackground(string bg)
    {
        if (!Cfg.StoryMusic.Value || Explicit || string.IsNullOrEmpty(bg)) return;
        string t; if (BgDefault.TryGetValue(bg, out t)) Play(t); else Stop();
    }
}

public static class InGameChat
{
    public static volatile bool OpenRequested;
    public static volatile bool ZoomRequested;
    public static volatile string PendingReply;
    public static KeyCode Key = KeyCode.F2, ZoomKey = KeyCode.F4, StoryKey = KeyCode.F3, SettingsKey = KeyCode.F8;
    public static volatile bool SettingsRequested;
    static string lastUser = "";
    static bool inited;

    // zoom
    static string zoomOwner; static bool zoomed;

    // speech / mouth / gesture state
    static string speakText = ""; static int spoken; static float speakStart = -1f;
    static float hideAt = -1f, emoteResetAt = -1f;
    static SayDialog lastDialog;
    static float armResetAt = -1f; static string armSlot = "";
    static float fadeStart = -1f; static CanvasGroup fadeGroup;
    static bool keepSeen; static string bubbleText = ""; static float bubbleUntil = -1f, lastSayTime = -100f, nextKeepCheck = 0f; static int restores;
    static bool emotesLogged;

    // tag -> (slot, item), using the names this game really has
    static readonly Dictionary<string, string[]> TagMap = new Dictionary<string, string[]>
    {
        { "happy", new[] { "Expression", "Happy" } },     { "veryhappy", new[] { "Expression", "VeryHappy" } },
        { "sad", new[] { "Expression", "Sad" } },         { "verysad", new[] { "Expression", "VerySad" } },
        { "angry", new[] { "Expression", "Angry" } },     { "veryangry", new[] { "Expression", "VeryAngry" } },
        { "shy", new[] { "Expression", "Blush" } },       { "veryshy", new[] { "Expression", "VeryBlush" } },
        { "surprised", new[] { "Expression", "Shock" } }, { "shocked", new[] { "Expression", "Shock" } },
        { "wave", new[] { "ArmR", "UpHi" } },             { "wavel", new[] { "ArmL", "UpHi" } },
        { "point", new[] { "ArmR", "UpPoint" } },         { "lecture", new[] { "ArmR", "UpLecture" } },
        { "clench", new[] { "ArmBoth", "DownClenched" } },
    };

    public static void Tick()
    {
        if (!inited)
        {
            inited = true;
            try { Key = (KeyCode)Enum.Parse(typeof(KeyCode), Cfg.Hotkey.Value, true); } catch { }
            try { ZoomKey = (KeyCode)Enum.Parse(typeof(KeyCode), Cfg.ZoomHotkey.Value, true); } catch { }
            try { StoryKey = (KeyCode)Enum.Parse(typeof(KeyCode), Cfg.StoryHotkey.Value, true); } catch { }
            try { SettingsKey = (KeyCode)Enum.Parse(typeof(KeyCode), Cfg.SettingsHotkey.Value, true); } catch { }
            SettingsPage.Validator = (s, v) =>
            {
                if (!s.Key.EndsWith("Hotkey")) return null;
                KeyCode kc;
                if (!Enum.TryParse((string)v, true, out kc) || kc == KeyCode.None) return "not a key name Unity knows (examples: F2, F9, Insert, Keypad5)";
                return null;
            };
            SettingsPage.Changed = s => { if (s.Key.EndsWith("Hotkey")) inited = false; };     // read the keys again next frame
        }
        try { SettingsPage.Tick(); } catch { }
        if (SettingsRequested)
        {
            SettingsRequested = false;
            try { Application.OpenURL("http://127.0.0.1:" + PanelServer.RunningPort + "/settings"); } catch (Exception ex) { Cfg.Log.LogWarning("Settings: could not open the browser: " + ex.Message); }
        }
        TrackZoomState();
        if (Mod.Chat)
        {
            if (ZoomRequested) { ZoomRequested = false; ToggleZoom(); }
            if (OpenRequested) { OpenRequested = false; if (!Requirements.Ready) ShowNotReady(); else { if (Cfg.AutoZoom.Value) ZoomIn(); OpenInput(); } }
            string r = PendingReply;
            if (r != null) { PendingReply = null; ShowReply(r); }
            try { StoryMemory.Tick(bubbleText); } catch { }
            try { Diary.Tick(); } catch { }
            try { PetView.Tick(); } catch { }
            YieldToOtherStory();
            TickGreet();
            TickGreetClose();
            TickKeep();
            TickTrace();
        }
        // shared mechanics: the fade-out of the text box (stories ask for it at scene ends and when they finish) and the mouth
        // animation. Both do nothing unless something asked for them. (Split bug 2026-10-22: Story Creator never ran TickFade,
        // so the last story line stayed frozen on screen.)
        TickSpeech();
        TickFade();
        if (Mod.Stories)
        {
            StoryTick();
            TickBg();
            AssetDump.Tick();
        }
    }

    // ---------- custom stories ----------
    public static volatile bool StoryMenuRequested;
    public static volatile string StoryRequest;
    // ---- stage mode: no dialogue, the web page at /stage drives the scene live ----
    public static volatile bool StageStartRequested, StageEndRequested;
    public static readonly ConcurrentQueue<string> StageCmds = new ConcurrentQueue<string>();
    static bool stageMode, stageQuiet, stageBoxShown; static float stageBoxCheck;
    public static volatile bool StageBox;      // the stage page can ask for a sample text box to judge the layout; otherwise the dialogue is hidden
    public static bool StageOn { get { return stageMode && storyRunning; } }
    static StoryData story; static StoryScene storyScene; static int storyIdx;
    static bool storyRunning, storyWaitLine, storyWaitChoice, storyZoomedByUs;
    static volatile bool storyLineDone; static volatile int storyChoice = -1;
    static int storyToken; static SayDialog storyDialog;
    static float storyLineAt;
    public static volatile bool StoryAdvance;      // player clicked / pressed Space or Enter
    public static bool StoryWaiting { get { return storyRunning && storyWaitLine; } }
    static int storyLen, storyRetries; static float storyNextAt; static bool storySkipped;
    static string storyLastText = "";
    static readonly List<Il2CppSystem.Action> storyCallbacks = new List<Il2CppSystem.Action>();

    static string StoryName(string who)
    {
        string bot = Shared.Current.Bot, pl = Shared.Current.Player;
        who = (who ?? "").Replace("{bot}", bot).Replace("{player}", pl).Trim();
        string lw = who.ToLowerInvariant();
        if (lw == "" || lw == "narrator" || lw == "narration" || lw == "n") return "";
        if (lw == "bot" || lw == bot.ToLowerInvariant()) return bot;
        if (lw == "player" || lw == "anon" || lw == "you" || lw == pl.ToLowerInvariant()) return "You";   // the game calls the player "You"
        return who;
    }

    static string StoryText(string t)
    {
        t = (t ?? "").Replace("{bot}", Shared.Current.Bot).Replace("{player}", Shared.Current.Player);
        t = t.Replace("{score}", Mathf.RoundToInt(Mathf.Clamp01(mgScore) * 100f) + "%").Replace("{pins}", Mathf.RoundToInt(Mathf.Clamp01(mgScore) * 10f).ToString()).Replace("{total}", mgTotal.ToString());
        t = Regex.Replace(t, @"\{\{[^}]*\}\}", "");
        return t.Replace("<", "").Replace(">", "").Trim();
    }

    static void StoryTick()
    {
        try { StoryMusic.Tick(); } catch { }
        try
        {
            if (StoryMenuNextPage) { StoryMenuNextPage = false; if (!storyRunning) ShowStoryMenu(true); }
            if (StoryMenuRequested)
            {
                StoryMenuRequested = false;
                if (storyRunning) { EndStory("Story stopped."); }
                else if (!Requirements.Ready) ShowNotReady();
                else ShowStoryMenu();
            }
            string req = StoryRequest;
            if (req != null) { StoryRequest = null; StartStory(req); }
            if (StageStartRequested) { StageStartRequested = false; StartStage(); }
            if (StageEndRequested) { StageEndRequested = false; if (stageMode) EndStory("Stage closed."); }
            if (!storyRunning) { if (slides.Count > 0) slides.Clear(); return; }
            SlideTick();
            if (stageMode)
            {
                string scmd; int sn = 0; stageQuiet = true;
                while (sn++ < 40 && StageCmds.TryDequeue(out scmd))
                { try { RunStoryCmd(scmd); } catch (Exception ex) { Cfg.Log.LogWarning("Stage command '" + scmd + "' failed: " + ex.Message); } }
                stageQuiet = false;
                StoryAdvance = false;
                StageBoxTick();
                return;
            }

            if (storyWaitLine)
            {
                // the plugin itself waits for the player; the game's "finished" callback is ignored
                var d = SayDialog.ActiveSayDialog;
                string cur = null;
                try { cur = d != null && d.gameObject.activeInHierarchy ? d.CurrentStoryText : null; } catch { }
                float age = Time.unscaledTime - storyLineAt;
                StoryWatchdog(age);
                bool typed = (cur != null && cur.Length >= storyLen - 1) || age > 0.3f + storyLen / 60f || storySkipped;
                if (!StoryAdvance) return;
                StoryAdvance = false;
                if (!typed) { storySkipped = true; return; }   // this click fast-forwards the typing (the game does that itself); the next one moves on
                storyNextAt = Time.unscaledTime + 0.1f;   // let the game finish handling this same click first, or it fast-forwards the next line
                storyWaitLine = false; storyLineDone = false; storyIdx++;
                Cfg.Log.LogInfo("Story: advanced by player (line " + storyIdx + ")");
            }
            if (Time.unscaledTime < storyNextAt) return;
            if (mgWaiting) { TickMinigame(); return; }
            if (storyWaitChoice)
            {
                int c = storyChoice;
                if (c < 0) return;
                storyChoice = -1; storyWaitChoice = false;
                if (c < storyScene.Choices.Count) GotoScene(storyScene.Choices[c].Goto);
                else EndStory("Story finished.");
                return;
            }
            if (storyScene == null) { EndStory("Story finished."); return; }
            if (storyIdx < storyScene.Lines.Count)
            {
                var nl = storyScene.Lines[storyIdx];
                if (nl.Cmd.Length > 0) { storyIdx++; try { RunStoryCmd(nl.Cmd); } catch (Exception ex) { Cfg.Log.LogWarning("Story command '" + nl.Cmd + "' failed: " + ex.Message); } return; }
                storyRetries = 0; ShowStoryLine(nl); return;
            }
            if (storyScene.Choices.Count > 0) { ShowStoryChoices(); return; }
            if (storyScene.Next.Length > 0) { GotoScene(storyScene.Next); return; }
            EndStory("Story finished.");
        }
        catch (Exception ex)
        {
            Cfg.Log.LogError("Story error: " + ex);
            Shared.LastResult = "Story error: " + ex.Message;
            try { EndStory(null); } catch { }
        }
    }

    static void GotoScene(string name)
    {
        if (name == null || name.Equals("end", StringComparison.OrdinalIgnoreCase)) { EndStory("Story finished."); return; }
        StoryScene sc;
        if (!story.Scenes.TryGetValue(name, out sc)) { EndStory("Story stopped: missing scene " + name); return; }
        storyScene = sc; storyIdx = 0;
    }

    static bool StoryBusy { get { return storyRunning || CrossMod.OtherStoryRunning; } }

    // Jun Link: Story Creator just started a story while the greeting is up -> end the greeting properly, as StartStory used to
    static void YieldToOtherStory()
    {
        if (!Mod.Chat || !greetActive || !CrossMod.OtherStoryRunning) return;
        greetActive = false; greetClosed = false; greetPlain = false;
        try { if (greetDialog != null) greetDialog.Stop(); } catch { }
        try { if (greetDialog != null) WriterFree(greetDialog); } catch { }
        Cfg.Log.LogInfo("Greeting: stopped, Story Creator started a story");
    }

    static void StartStory(string id)
    {
        if (!Requirements.Ready) { Shared.LastResult = Requirements.Why(); Cfg.Log.LogInfo("Story not started: " + Requirements.Why()); ShowNotReady(); return; }
        if (storyRunning) EndStory(null);
        var errors = new List<string>();
        string text = StoryScript.ReadText(id);
        if (text == null) { Shared.LastResult = "Story not found: " + id; return; }
        var st = StoryScript.Parse(StoryScript.SafeId(id), text, errors);
        if (errors.Count > 0 || string.IsNullOrEmpty(st.Start))
        {
            Shared.LastResult = "Story has errors: " + (errors.Count > 0 ? errors[0] : "empty");
            Cfg.Log.LogWarning("Story '" + id + "' not started: " + string.Join(" | ", errors));
            return;
        }
        if (Controller() == null) { Shared.LastResult = "Story not started: she is not on screen."; return; }
        if (greetActive)       // the greeting is still up (maybe still typing): end it properly so it does not fight the story for the box
        {
            greetActive = false; greetClosed = false; greetPlain = false;
            try { if (greetDialog != null) greetDialog.Stop(); } catch { }
            Cfg.Log.LogInfo("Greeting: stopped, a story started");
        }
        writerWaitSince = -1f;
        mgTotal = 0; mgScore = 0f; mgWaiting = false;
        try { var d0 = SayDialog.ActiveSayDialog; if (d0 != null) { lastDialog = d0; } } catch { }
        FadeOldBubble();
        story = st; storyScene = st.Scenes[st.Start]; storyIdx = 0;
        storyRunning = true; storyWaitLine = false; storyWaitChoice = false; storyLineDone = false; storyChoice = -1; CrossMod.SetStoryRunning(true);
        storyZoomedByUs = false;
        CaptureJun();
        if (Cfg.AutoZoom.Value && !zoomed) { ZoomIn(); storyZoomedByUs = zoomed; }
        Shared.LastResult = "Playing story: " + st.Title;
        Cfg.Log.LogInfo("Story started: " + st.Title);
    }

    // In stage mode there is no dialogue: whatever text box is up (the greeting, a leftover line) is hidden, unless the page asked for a sample box.
    static void StageBoxTick()
    {
        float now = Time.unscaledTime; if (now < stageBoxCheck) return; stageBoxCheck = now + 0.4f;
        try
        {
            var d = FindMainDialog() ?? FindDialog(); if (d == null) return;
            if (StageBox)
            {
                bool has = false; try { has = d.gameObject.activeInHierarchy && !string.IsNullOrEmpty(d.CurrentStoryText); } catch { }
                if (has && stageBoxShown) return;
                fadeStart = -1f; try { if (fadeGroup != null) fadeGroup.alpha = 1f; } catch { }
                bubbleText = "";
                d.gameObject.SetActive(true);
                try { d.Stop(); } catch { }
                try { d.SetCharacter(null); } catch { }
                try { d.SetCharacterName("Stage preview", Color.white); } catch { }
                d.Say("This is a sample text box, so you can see how much of the scene it covers.", true, true, false, false, null);
                lastDialog = d; stageBoxShown = true;
            }
            else
            {
                bool on = false; try { on = d.gameObject.activeInHierarchy; } catch { }
                if (on) { lastDialog = d; bubbleText = ""; HideDialogCleanly(); }
                stageBoxShown = false;
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Stage: text box failed: " + ex.Message); }
    }

    static void StartStage()
    {
        if (storyRunning) EndStory(null);
        if (Controller() == null) { Shared.LastResult = "Stage not started: she is not on screen."; return; }
        try { var d0 = SayDialog.ActiveSayDialog; if (d0 != null) { lastDialog = d0; } } catch { }
        FadeOldBubble();
        var st = new StoryData(); st.Id = "stage"; st.Title = "Stage"; st.Start = "stage";
        var scn = new StoryScene(); scn.Name = "stage"; st.Scenes["stage"] = scn;
        story = st; storyScene = scn; storyIdx = 0;
        storyRunning = true; storyWaitLine = false; storyWaitChoice = false; storyLineDone = false; storyChoice = -1; CrossMod.SetStoryRunning(true);
        storyZoomedByUs = false;
        string junk; while (StageCmds.TryDequeue(out junk)) { }
        CaptureJun();
        if (Cfg.AutoZoom.Value && !zoomed) { ZoomIn(); storyZoomedByUs = zoomed; }
        stageMode = true; stageBoxShown = false; stageBoxCheck = 0f;
        Shared.LastResult = "Stage is open: use the page at /stage";
        Cfg.Log.LogInfo("Stage started");
    }

    static void EndStory(string why)
    {
        bool was = storyRunning;
        FlushStoryLog();
        if (mgWaiting) { mgWaiting = false; try { if (mgObj != null) UnityEngine.Object.Destroy(mgObj); } catch { } mgObj = null; }
        try { StoryMusic.Reset(); } catch { }
        stageMode = false; stageBoxShown = false;
        storyRunning = false; storyWaitLine = false; storyWaitChoice = false; storyChoice = -1; CrossMod.SetStoryRunning(false);
        if (was)
        {
            try { if (storyDialog != null) lastDialog = storyDialog; FadeOldBubble(); } catch { try { HideDialogCleanly(); } catch { } }   // fade out like the game does
            try { var ci = Controller(); if (ci != null) ci.SetDefaultEmote(false); } catch { }
            junWanted = false; junInScene = false;                       // the game puts her back in the room when the room view returns
            if (bgUsed)
            {
                BgPictureOff();
                try { if (bgPrev != null) GoView(bgPrev, Cfg.BackgroundFade.Value); } catch (Exception ex) { Cfg.Log.LogWarning("Story: going back failed: " + ex.Message); }
                if (storyZoomedByUs && Cfg.AutoZoom.Value) bgZoomOutAt = Time.unscaledTime + Cfg.BackgroundFade.Value + 1.0f;   // zoom out once we are back in the room
                bgUsed = false; bgPrev = null; bgReplaceAt = -1f;
            }
            else if (storyZoomedByUs && Cfg.AutoZoom.Value) ZoomOut();
            storyZoomedByUs = false;
            try { if (storyChars.Count > 0) { var scs = SimpleCharacterSingleton.Instance; if (scs != null) foreach (var c in storyChars) scs.HideCharacter(c); } } catch { }
            storyChars.Clear();
        }
        if (why != null) Shared.LastResult = why;
    }

    // ---- characters on stage:  @show Id left|center|right|<number>   @move Id <position>   @hide Id|all ----
    static readonly List<SimpleCharacter> storyChars = new List<SimpleCharacter>();
    public static string DescribeChar(SimpleCharacter ch)
    {
        var sb = new StringBuilder();
        try
        {
            var t = ch.transform;
            sb.Append(ch.name + ": active=" + ch.gameObject.activeInHierarchy + " pos=" + t.position + " local=" + t.localPosition + " scale=" + t.lossyScale
                + " parent=" + (t.parent == null ? "none" : t.parent.name + "@" + t.parent.position));
            var rs = ch.GetComponentsInChildren<SpriteRenderer>(true);
            int on = 0, vis = 0; string first = "";
            foreach (var r in rs)
            {
                if (r.enabled) on++;
                if (r.enabled && r.color.a > 0.01f) vis++;
                if (first.Length == 0) first = " layer=" + r.sortingLayerName + "/" + r.sortingOrder + " alpha=" + r.color.a + " sprite=" + (r.sprite == null ? "none" : r.sprite.name);
            }
            sb.Append(" renderers=" + rs.Length + " enabled=" + on + " visible=" + vis + first);
            var cam = Camera.main;
            if (cam != null) sb.Append(" | camera pos=" + cam.transform.position + " ortho=" + cam.orthographic + " size=" + cam.orthographicSize);
        }
        catch (Exception ex) { sb.Append(" describe failed: " + ex.Message); }
        return sb.ToString();
    }

    // The prefab is created far off screen (the log showed y = -76 against a camera that sees about +-5). So: measure the sprites,
    // scale to a sensible height, then put the feet near the bottom of the screen and the middle at x.
    static bool HasBounds(ref Bounds b, SimpleCharacter ch)
    {
        bool any = false;
        foreach (var r in ch.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!r.enabled || r.sprite == null) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return any;
    }
    // @slide Name pos [seconds]: the portrait glides to pos instead of jumping; @stop Name freezes it where it is (like the game's own slide-in)
    class SlideAnim { public SimpleCharacter Ch; public float From, To, Start, Dur; }
    static readonly List<SlideAnim> slides = new List<SlideAnim>();
    static void SlideTick()
    {
        if (slides.Count == 0) return;
        float now = Time.unscaledTime;
        for (int i = slides.Count - 1; i >= 0; i--)
        {
            var a = slides[i];
            try
            {
                if (a.Ch == null || a.Ch.gameObject == null || !storyChars.Contains(a.Ch)) { slides.RemoveAt(i); continue; }
                float t = a.Dur <= 0.01f ? 1f : Mathf.Clamp01((now - a.Start) / a.Dur);
                float e = t * t * (3f - 2f * t);                         // ease in and out
                storyCharX[a.Ch.GetInstanceID()] = a.From + (a.To - a.From) * e;
                if (t >= 1f) slides.RemoveAt(i);
            }
            catch { slides.RemoveAt(i); }
        }
        PlaceAll();
    }
    static void CancelSlide(SimpleCharacter ch) { for (int i = slides.Count - 1; i >= 0; i--) if (slides[i].Ch == ch) slides.RemoveAt(i); }

    static void PlaceChar(SimpleCharacter ch, float x)
    {
        CancelSlide(ch);
        storyCharX[ch.GetInstanceID()] = x;
        PlaceAll();
    }

    // The game's own placement, read from its F7 list (camera size 5.4): a portrait keeps its DefaultScale (Annalie 1.0, Melissa/Cashier 0.6)
    // and its pivot sits 0.51 below the camera centre; Annalie at the monitor is at x 0, Melissa at the till is 5 to the right.
    static void PlaceAll()
    {
        var cam = Camera.main;
        if (cam == null) return;
        float size = cam.orthographicSize, k = size / 5.4f;
        var cp = cam.transform.position;
        foreach (var c in storyChars)
        {
            try
            {
                if (c == null || c.gameObject == null) continue;
                var ds = c.DefaultScale;
                float extra = 1f;
                try { if (c.gameObject.name.StartsWith("Annalie_Outside")) extra = Cfg.HangoutScale.Value; } catch { }
                float ov; if (storyCharSize.TryGetValue(c.GetInstanceID(), out ov)) extra = ov;
                if (ds.x > 0.0001f) c.transform.localScale = ds * (k * Cfg.CharSize.Value * extra);
                float x; if (!storyCharX.TryGetValue(c.GetInstanceID(), out x)) x = 0f;
                float yo; if (!storyCharY.TryGetValue(c.GetInstanceID(), out yo)) yo = 0f;
                c.transform.position = new Vector3(cp.x + x * k, cp.y + (yo - 0.51f) * k, cp.z + 20f);   // z in front of the camera (the backdrops are at +100)
                BringToFront(c);
            }
            catch (Exception ex) { Cfg.Log.LogWarning("Story: placing a character failed: " + ex.Message); }
        }
    }

    // SortingLayer.layers is stripped in this game ("Method unstripping failed"), so only the sorting order is raised here,
    // and the log lists what the scene draws so the right layer can be chosen next.
    static readonly HashSet<int> frontedRenderers = new HashSet<int>();
    static bool sceneDumped;
    static void BringToFront(SimpleCharacter ch)
    {
        // intentionally empty: the scenes' own foreground (the monitor in the clothier, the till in the shop) is drawn over the portraits
        // on purpose, so a portrait stands "behind" it. Only the z position (in front of the camera) matters.
    }

    static void DumpSceneDraw(string why)
    {
        try
        {
            var cam = Camera.main;
            if (cam != null) Cfg.Log.LogInfo("Story[" + why + "]: camera rot=" + cam.transform.rotation.eulerAngles + " near=" + cam.nearClipPlane + " far=" + cam.farClipPlane + " cullMask=" + cam.cullingMask);
            foreach (var c in storyChars) Cfg.Log.LogInfo("Story[" + why + "]: " + c.name + " gameObject.layer=" + c.gameObject.layer);
            var all = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>();
            var seen = new HashSet<string>();
            int shown = 0, total = 0;
            foreach (var r in all)
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                total++;
                string k = r.sortingLayerName + "/" + r.gameObject.layer;
                if (seen.Add(k) && shown < 12) { shown++; Cfg.Log.LogInfo("Story[" + why + "]: sprite '" + r.name + "' sortingLayer=" + r.sortingLayerName + " order=" + r.sortingOrder + " gameLayer=" + r.gameObject.layer + " z=" + r.transform.position.z); }
            }
            Cfg.Log.LogInfo("Story[" + why + "]: " + total + " active sprite renderers in scene");
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: scene dump failed: " + ex.Message); }
    }

    // ---- backgrounds:  @background outside   (fades to black, switches the game's own view, fades back) ----
    public static readonly string[] BackgroundNames = { "room", "outside", "shop", "clothier", "shower", "pc", "cuddle", "changingroom", "hospital", "flashback" };
    // the built-in places plus every picture in jun_backgrounds (lowercase letters and digits only), for the story editor and the stage page
    public static string[] AllBackgroundNames()
    {
        var l = new List<string>(BackgroundNames);
        try
        {
            if (Directory.Exists(BgDir()))
            {
                var extra = new List<string>();
                foreach (var f in Directory.GetFiles(BgDir(), "*.png"))
                {
                    string n = System.IO.Path.GetFileNameWithoutExtension(f);
                    if (n == "hos" || !Regex.IsMatch(n, "^[a-z0-9]+$") || l.Contains(n)) continue;
                    extra.Add(n);
                }
                extra.Sort(StringComparer.Ordinal);
                l.AddRange(extra);
            }
        }
        catch { }
        return l.ToArray();
    }
    static Fungus.View bgPrev; static bool bgUsed;
    static float bgReplaceAt = -1f, bgZoomOutAt = -1f;
    static readonly Dictionary<int, float> storyCharX = new Dictionary<int, float>();
    static readonly Dictionary<int, float> storyCharY = new Dictionary<int, float>();      // @move / @show height offset in world units (negative = lower)
    static readonly Dictionary<int, float> storyCharSize = new Dictionary<int, float>();   // @size / @show size: multiplier on the game's own size

    static Fungus.View ViewFor(string name)
    {
        var vs = ViewSingleton.Instance;
        if (vs == null) return null;
        switch (name)
        {
            case "room": return vs.RoomView;
            case "outside": return vs.OutsideView;
            case "shop": return vs.ShopView;
            case "clothier": return vs.ClothierView;
            case "shower": return vs.ShowerView;
            case "pc": return vs.PCView;
            case "cuddle": return vs.CuddleView;
            case "changingroom": return vs.ChangingRoomView;
            case "hospital": return FindViewAt(-50f, -40f);     // the clinic: not in ViewSingleton, found by where the game puts the camera (F7 showed -50,-40)
            case "flashback": return FindViewAt(-50f, 0f);      // the black memory scene (camera -50,0)
        }
        if (CustomBgExists(name)) return vs.OutsideView;      // your own picture (jun_backgrounds\<name>.png) is drawn over the outside view, so Jun can stand in it
        return null;
    }

    static readonly Dictionary<string, Fungus.View> viewAtCache = new Dictionary<string, Fungus.View>();
    static Fungus.View FindViewAt(float x, float y)
    {
        string key = x + "," + y;
        Fungus.View hit;
        try { if (viewAtCache.TryGetValue(key, out hit) && hit != null && hit.gameObject != null) return hit; } catch { }
        hit = null; float best = 1.5f; int seen = 0;
        try
        {
            var all = Resources.FindObjectsOfTypeAll<Fungus.View>();
            foreach (var v in all)
            {
                try
                {
                    var p = v.transform.position;
                    float d = Math.Max(Math.Abs(p.x - x), Math.Abs(p.y - y));
                    if (seen < 40) { seen++; Cfg.Log.LogInfo("Story: view '" + v.name + "' at (" + p.x.ToString("0.0") + ", " + p.y.ToString("0.0") + ")"); }
                    if (d < best) { best = d; hit = v; }
                }
                catch { }
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: view search failed: " + ex.Message); }
        if (hit != null) viewAtCache[key] = hit;
        else Cfg.Log.LogWarning("Story: no view found near (" + x + ", " + y + "); the log lines above list every view and where it is.");
        return hit;
    }

    static void GoView(Fungus.View v, float dur)
    {
        var cm = Fungus.FungusManager.Instance.CameraManager;
        cm.FadeToViewMainCamera(v, dur, true, Color.black, null);
    }

    static readonly List<GameObject> bgForced = new List<GameObject>();
    static void BgPictureOn(string spriteName)
    {
        try
        {
            int found = 0;
            foreach (var r in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                try
                {
                    if (r.sprite == null || r.sprite.name != spriteName) continue;
                    found++;
                    var t = r.transform; int n = 0;
                    while (t != null && n < 8)
                    {
                        if (!t.gameObject.activeSelf) { t.gameObject.SetActive(true); bgForced.Add(t.gameObject); Cfg.Log.LogInfo("Story: switched on " + t.name + " for the " + spriteName + " picture"); }
                        t = t.parent; n++;
                    }
                    r.enabled = true;
                }
                catch { }
            }
            if (found == 0) ApplyBgFile(spriteName);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: background picture failed: " + ex.Message); }
    }
    static string TPath(Transform t)
    {
        string r = ""; int n = 0;
        try { while (t != null && n < 8) { r = t.name + (r.Length > 0 ? "/" : "") + r; t = t.parent; n++; } } catch { }
        return r;
    }
    static string BgDir() { return System.IO.Path.Combine(Host.RootPath, "jun_backgrounds"); }
    static SpriteRenderer bgFileRend; static Sprite bgFileOldSprite; static Color bgFileOldColor;

    // the game only loads its clinic picture while its own clinic scene runs. F7 saves it once (BepInEx\jun_backgrounds\hos.png); after that it is loaded from the file
    static void ApplyBgFile(string spriteName)
    {
        try
        {
            string png = System.IO.Path.Combine(BgDir(), spriteName + ".png");
            if (!File.Exists(png)) { Cfg.Log.LogWarning("Story: the game has not loaded '" + spriteName + "' and there is no saved copy (" + png + "). Open the game's own clinic scene once, press F7 there, then try again."); return; }
            SpriteRenderer target = null;
            foreach (var r in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                try { if (r.name == "Image 2" && TPath(r.transform).EndsWith("Scene 2/Image 2")) { target = r; break; } } catch { }
            }
            if (target == null) { Cfg.Log.LogWarning("Story: could not find the clinic picture holder (Scene 2/Image 2)"); return; }
            var data = File.ReadAllBytes(png);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var arr = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(data.Length);
            for (int i = 0; i < data.Length; i++) arr[i] = data[i];
            if (!ImageConversion.LoadImage(tex, arr)) { Cfg.Log.LogWarning("Story: could not read " + png); return; }
            var sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 19.2f);
            bgFileRend = target; bgFileOldSprite = target.sprite; bgFileOldColor = target.color;
            target.sprite = sp; target.enabled = true;
            string cf = System.IO.Path.Combine(BgDir(), spriteName + ".color");
            if (File.Exists(cf))
            {
                var p = File.ReadAllText(cf).Split(' ');
                float cr, cg, cb, ca;
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                if (p.Length >= 4 && float.TryParse(p[0], System.Globalization.NumberStyles.Float, ci, out cr) && float.TryParse(p[1], System.Globalization.NumberStyles.Float, ci, out cg) && float.TryParse(p[2], System.Globalization.NumberStyles.Float, ci, out cb) && float.TryParse(p[3], System.Globalization.NumberStyles.Float, ci, out ca)) target.color = new Color(cr, cg, cb, ca);
            }
            Cfg.Log.LogInfo("Story: put the saved " + spriteName + " picture on " + TPath(target.transform));
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: saved picture failed: " + ex.Message); }
    }

    // writes a big sprite that is on screen to BepInEx\jun_backgrounds\<name>.png (once), together with the colour the game tints it with
    public static string SaveBigSprite(SpriteRenderer r)
    {
        try
        {
            var sp = r.sprite; if (sp == null) return "";
            string name = Regex.Replace(sp.name, "[^A-Za-z0-9_-]", "_");
            Directory.CreateDirectory(BgDir());
            string png = System.IO.Path.Combine(BgDir(), name + ".png");
            if (File.Exists(png)) return "  (saved before: " + png + ")";
            var tex = sp.texture; var rc = sp.textureRect;
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var o = new Texture2D((int)rc.width, (int)rc.height, TextureFormat.RGBA32, false);
            o.ReadPixels(new Rect(rc.x, rc.y, rc.width, rc.height), 0, 0); o.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var enc = ImageConversion.EncodeToPNG(o);
            var bytes = new byte[enc.Length]; for (int i = 0; i < bytes.Length; i++) bytes[i] = enc[i];
            File.WriteAllBytes(png, bytes);
            var c = r.color; var ci = System.Globalization.CultureInfo.InvariantCulture;
            File.WriteAllText(System.IO.Path.Combine(BgDir(), name + ".color"), c.r.ToString(ci) + " " + c.g.ToString(ci) + " " + c.b.ToString(ci) + " " + c.a.ToString(ci));
            return "  saved picture: " + png + " (tint " + c + ")";
        }
        catch (Exception ex) { return "  could not save picture: " + ex.Message; }
    }


    // ---- your own background picture:  put jun_backgrounds\<name>.png (16:9 works best), then  @background <name>  (lowercase letters and digits only) ----
    // The picture is drawn full-screen over the game's outside view: the street art there is switched off while it is shown, so Jun can still stand in it.
    // Untested in the game. The log prints "Story: custom background ..." lines.
    static GameObject bgCustomObj; static readonly List<Renderer> bgCustomHidden = new List<Renderer>();
    static string CustomBgPath(string name) { return System.IO.Path.Combine(BgDir(), name + ".png"); }
    // The game pictures tools\export_backgrounds.py can save. A story may use them before the player has run the script:
    // the story still plays, and a missing one falls back to the street (BgFallback) with a note in the log.
    public static readonly string[] ExportableBackgrounds = { "bowling", "karaoke", "cafenight", "restaurant", "restaurant2", "plaza", "city", "theatre", "lobby", "reception", "artgallery", "artgallery3", "aquariuminside", "aquariumnightlight", "aquariumcircusshow", "amusementpark1", "amusementpark2", "amusementpark3", "planetarium1", "planetarium2", "stargazing1", "stargazing2", "stargazing3", "sunnybeach", "greenfield", "temple", "ruin", "frozen", "casinobackground", "cazinoslot", "cazinoblackjack", "blackmarket", "store", "shopshelves", "richpeopleshop", "richpeopleshopinside", "undergroundstreet", "tunneldark", "tunnelpopulated", "staircase", "industrialarea1", "industrialarea2", "factory", "destroyedlab", "pollutedlake", "trashedriver", "microplasticcoast", "riots", "riots2", "dystopia", "dystopia2", "dystopia3", "dystopia4", "premiumfarm", "galaxy" };
    public const string BgFallback = "outside";
    public static bool IsExportable(string name) { return Array.IndexOf(ExportableBackgrounds, name) >= 0; }
    static readonly HashSet<string> bgMissingNoted = new HashSet<string>();

    public static bool CustomBgExists(string name) { try { return name.Length > 0 && File.Exists(CustomBgPath(name)); } catch { return false; } }

    static void BgCustomOn(string name, Fungus.View v)
    {
        try
        {
            string png = CustomBgPath(name);
            if (!File.Exists(png)) { Cfg.Log.LogWarning("Story: custom background picture not found: " + png); return; }
            var data = File.ReadAllBytes(png);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var arr = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(data.Length);
            for (int i = 0; i < data.Length; i++) arr[i] = data[i];
            if (!ImageConversion.LoadImage(tex, arr)) { Cfg.Log.LogWarning("Story: could not read " + png); return; }
            var sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

            var vs = ViewSingleton.Instance; var scene = vs != null ? vs.OutsideScene : null;
            float bz = v.transform.position.z; string layer = "Default"; int lowest = 0; float bestArea = -1f; int hidden = 0; int bestLayerValue = int.MaxValue;
            if (scene != null)
            {
                foreach (var r in scene.GetComponentsInChildren<SpriteRenderer>(false))
                {
                    try
                    {
                        if (r == null || !r.enabled) continue;
                        var bb = r.bounds; float area = bb.size.x * bb.size.y;
                        // copy the layer of the street photo, never the lighting overlay above it: the lowest sorting layer wins, then the biggest picture
                        // (v5.19b: the "Light Multiply" overlay is as big as the photo, and when it won the picture covered every character)
                        int lv = 0; try { lv = SortingLayer.GetLayerValueFromID(r.sortingLayerID); } catch { }
                        string ln = r.sortingLayerName ?? "";
                        if (ln.IndexOf("light", StringComparison.OrdinalIgnoreCase) < 0 && (lv < bestLayerValue || (lv == bestLayerValue && area > bestArea)))
                        { bestLayerValue = lv; bestArea = area; bz = r.transform.position.z; layer = ln; }
                        if (r.sortingOrder < lowest) lowest = r.sortingOrder;
                        r.enabled = false; bgCustomHidden.Add(r); hidden++;     // the street art; switched back on when the picture goes
                    }
                    catch { }
                }
            }
            var cam = Camera.main; float half = 5f, aspect = 16f / 9f;
            try { if (cam != null) { half = cam.orthographicSize; aspect = cam.aspect; } } catch { }
            float worldH = half * 2f, worldW = worldH * aspect;
            float sw = tex.width / 100f, sh = tex.height / 100f;
            float scale = Math.Max(worldW / sw, worldH / sh);     // cover the whole screen

            bgCustomObj = new GameObject("MdrgCustomBackground");
            bgCustomObj.transform.position = new Vector3(v.transform.position.x, v.transform.position.y, bz);
            bgCustomObj.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = bgCustomObj.AddComponent<SpriteRenderer>();
            sr.sprite = sp; sr.sortingLayerName = layer; sr.sortingOrder = lowest - 5;
            Cfg.Log.LogInfo("Story: custom background '" + name + "' (" + tex.width + "x" + tex.height + ") at " + bgCustomObj.transform.position + " scale " + scale + " layer " + layer + " order " + sr.sortingOrder + ", street art switched off: " + hidden);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: custom background failed: " + ex.Message); }
    }

    static void BgCustomOff()
    {
        try { if (bgCustomObj != null) UnityEngine.Object.Destroy(bgCustomObj); } catch { }
        bgCustomObj = null;
        foreach (var r in bgCustomHidden) { try { if (r != null) r.enabled = true; } catch { } }
        bgCustomHidden.Clear();
    }

    static void BgPictureOff()
    {
        BgCustomOff();
        try { if (bgFileRend != null) { bgFileRend.sprite = bgFileOldSprite; bgFileRend.color = bgFileOldColor; } } catch { }
        bgFileRend = null;
        for (int i = bgForced.Count - 1; i >= 0; i--) { try { if (bgForced[i] != null) bgForced[i].SetActive(false); } catch { } }
        bgForced.Clear();
    }

    static void RunBackground(string name)
    {
        if (Array.IndexOf(BackgroundNames, name) < 0 && !CustomBgExists(name) && IsExportable(name))
        {
            if (bgMissingNoted.Add(name))
            {
                Cfg.Log.LogWarning("Story: the picture '" + name + "' is not in jun_backgrounds yet, so the street is used instead. Run tools\\export_backgrounds.py once to get the game's places.");
                Shared.LastResult = "Missing picture '" + name + "': using the street. Run tools\\export_backgrounds.py to get the game's places.";
            }
            name = BgFallback;
        }
        var v = ViewFor(name);
        if (v == null) { Cfg.Log.LogWarning("Story: no view for background '" + name + "'"); return; }
        var cm = Fungus.FungusManager.Instance.CameraManager;
        if (!bgUsed) { try { bgPrev = cm.CurrentView; } catch { bgPrev = null; } bgUsed = true; }
        float dur = Cfg.BackgroundFade.Value;
        foreach (var c in storyChars) { try { c.gameObject.SetActive(false); } catch { } }   // put back once the camera has arrived
        junInScene = false; junPlaced = false; junTries = 0; junGaveUp = false;    // the game moves her out when the scene changes; she is asked for again once the camera has arrived
        slides.Clear();
        BgPictureOff();
        if (Array.IndexOf(BackgroundNames, name) < 0) BgCustomOn(name, v);
        if (name == "hospital") BgPictureOn("hos");      // the clinic's photo is a sprite called "hos" (Views/Scene 2/Image 2) that the game only switches on during its own scene
        GoView(v, dur);
        bgReplaceAt = Time.unscaledTime + dur + 0.5f;
        storyNextAt = Math.Max(storyNextAt, Time.unscaledTime + dur + 0.6f);          // the story waits for the transition
        bgCurrent = name;
        StoryMusic.ForBackground(name);
        Cfg.Log.LogInfo("Story: background -> " + name + " (" + v.name + ")");
    }

    // ---- Jun in other places:  @jun left|center|right|<number>|hide  (she normally only exists in the room scene) ----
    // The game builds and destroys her model for whichever scene is using it: the log showed that the model I had grabbed in the room
    // was gone by the next scene (my reference turned into "null"), which is why she never showed up. So she is not carried around by
    // hand any more. The game's own ViewScene.MovePrefabInto asks for her in the scene that is on screen, and she is looked up
    // fresh every time; her size and place are then corrected by her visible bounds.
    static Live2DInteractionController junCtl;
    static Vector3 junCapSize; static float junCapCenterRelY, junCapSizeRel = 1f, junCapZ; static Vector3 junCapScale = new Vector3(2f, 2f, 2f);   // how she looked in the room when the story started
    static bool junWanted, junInScene, junPlaced, junGaveUp, junNoVisible; static float junX, junY, junSizeMul = 1f;
    static float junSince = -1f, junNextCheck = -1f, junPlacedAt = -1f, junRoomCheckAt = -1f; static int junTries, junFixLogs; static bool junLateLogged;

    static bool JunBounds(out Bounds b)
    {
        b = new Bounds(); bool any = false;
        try
        {
            if (junCtl == null) return false;
            foreach (var r in junCtl.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
        }
        catch { }
        return any;
    }

    static string ParentChain(Transform t)
    {
        var chain = new StringBuilder();
        for (int i = 0; i < 8 && t != null; i++) { chain.Append(t.name + (t.gameObject.activeSelf ? "" : "(off)") + " < "); t = t.parent; }
        return chain.ToString();
    }

    static void CaptureJun()
    {
        junWanted = false; junInScene = false; junPlaced = false; junGaveUp = false; junX = 0f; junY = 0f; junSizeMul = 1f; junCtl = null; junCapSize = Vector3.zero; junTries = 0; junRoomCheckAt = -1f;
        try
        {
            var ci = Controller(); var cam = Camera.main;
            if (ci == null || cam == null) { Cfg.Log.LogInfo("Story: Jun capture: no controller or camera"); return; }
            junCtl = ci;
            float cs = Math.Max(0.1f, cam.orthographicSize);
            junCapScale = ci.transform.lossyScale;
            junCapZ = ci.transform.position.z - cam.transform.position.z;
            Bounds b;
            if (JunBounds(out b)) { junCapSize = b.size; junCapSizeRel = b.size.y / cs; junCapCenterRelY = (b.center.y - cam.transform.position.y) / cs; }
            Cfg.Log.LogInfo("Story: Jun capture: " + ci.gameObject.name + " pos=" + ci.transform.position + " scale=" + junCapScale + " chain=" + ParentChain(ci.transform)
                + " bounds size=" + junCapSize + " (height " + junCapSizeRel.ToString("0.00") + " x camera size, centre " + junCapCenterRelY.ToString("0.00") + " x camera size)");
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: Jun capture failed: " + ex.Message); }
    }

    // what she looks like to the camera: how many of her renderers exist, are on, and are actually drawn; which sorting layers they use
    static void LogJun(string why)
    {
        try
        {
            var ci = junCtl;
            if (ci == null) { Cfg.Log.LogInfo("Story: Jun " + why + ": no model"); return; }
            var cam = Camera.main; Bounds b; bool has = JunBounds(out b);
            int n = 0, en = 0, vis = 0, lo = int.MaxValue, hi = int.MinValue; var layers = new HashSet<string>();
            foreach (var r in ci.GetComponentsInChildren<Renderer>(true))
            {
                n++; if (!r.enabled) continue; en++;
                try { layers.Add(r.sortingLayerName); int o = r.sortingOrder; if (o < lo) lo = o; if (o > hi) hi = o; } catch { }
                if (!junNoVisible) { try { if (r.isVisible) vis++; } catch { junNoVisible = true; } }
            }
            Cfg.Log.LogInfo("Story: Jun " + why + ": active=" + ci.gameObject.activeInHierarchy + " layer=" + ci.gameObject.layer + " pos=" + ci.transform.position + " scale=" + ci.transform.lossyScale
                + (has && cam != null ? " bounds centre-from-camera=" + (b.center - cam.transform.position) + " size=" + b.size : " no renderers")
                + " renderers=" + n + " enabled=" + en + (junNoVisible ? "" : " drawn=" + vis) + " sorting=" + string.Join("/", new List<string>(layers).ToArray()) + " order " + (en > 0 ? lo + ".." + hi : "-")
                + " chain=" + ParentChain(ci.transform));
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: Jun log failed: " + ex.Message); }
    }

    static ViewScene SceneForJun(string bg)
    {
        var vs = ViewSingleton.Instance;
        if (vs == null) return null;
        switch (bg)
        {
            case "outside": return vs.OutsideScene;
            case "shop": return vs.ShopScene;
            case "clothier": return vs.ClothierScene;
        }
        if (bgCustomObj != null) return vs.OutsideScene;
        return null;
    }

    // asks the game to put Jun's model into the scene that is on screen now
    static void JunMoveIn()
    {
        try
        {
            var sc = SceneForJun(bgCurrent);
            if (sc == null) { Cfg.Log.LogWarning("Story: Jun can only appear in outside, shop and clothier (not '" + bgCurrent + "')"); return; }
            var holder = Live2DControllerSingleton.Instance.InteractCharacterHolder;
            if (holder == null) { Cfg.Log.LogWarning("Story: Jun: no model holder"); return; }
            sc.MovePrefabInto(holder, junCapScale, Vector3.zero);
            junInScene = true; junPlaced = false; junTries++; junSince = Time.unscaledTime; junNextCheck = -1f; junFixLogs = 0; junLateLogged = false;
            Cfg.Log.LogInfo("Story: Jun asked to move into the " + bgCurrent + " scene (try " + junTries + ")");
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: Jun move-in failed: " + ex.Message); }
    }

    static void JunLeave()
    {
        try
        {
            if (junInScene)
            {
                var h = Live2DControllerSingleton.Instance.InteractCharacterHolder;
                if (h != null) h.StopUsingCurrent();
                Cfg.Log.LogInfo("Story: Jun sent away");
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: Jun leave failed: " + ex.Message); }
        junInScene = false; junPlaced = false; junCtl = null;
    }

    // every few tenths of a second: find her again (the game may have rebuilt her), then fix her size and place by her visible bounds
    static void JunTick(float now)
    {
        if (!bgUsed || bgCurrent == "room" || !junWanted || !junInScene || now < junNextCheck) return;
        junNextCheck = now + (junPlaced ? 0.4f : 0.12f);
        try
        {
            var ci = Controller();
            if (ci == null)
            {
                float waited = now - junSince;
                if (waited > 3f && junTries < 3) { Cfg.Log.LogInfo("Story: Jun not ready after " + waited.ToString("0.0") + " s, asking again"); JunMoveIn(); }
                else if (waited > 8f && !junGaveUp) { junGaveUp = true; Cfg.Log.LogWarning("Story: Jun never became ready in the " + bgCurrent + " scene"); }
                return;
            }
            junCtl = ci;
            var cam = Camera.main; if (cam == null) return;
            if (!ci.gameObject.activeInHierarchy)
            {
                ci.transform.SetParent(null, true); ci.gameObject.SetActive(true);
                Cfg.Log.LogInfo("Story: Jun was switched off in that scene: took her out of it and switched her on");
            }
            Bounds cb;
            if (!JunBounds(out cb) || cb.size.y < 0.3f) return;                      // her meshes are not built yet
            float size = cam.orthographicSize, s = size / 5.4f; var cp = cam.transform.position; var t = ci.transform;
            bool changed = false;
            float ratio = (junCapSizeRel * size * junSizeMul) / cb.size.y;
            if (junCapSizeRel > 0.05f && ratio > 0.02f && ratio < 50f && Math.Abs(ratio - 1f) > 0.03f) { t.localScale = t.localScale * ratio; JunBounds(out cb); changed = true; }
            float wantX = cp.x + junX * s, wantY = cp.y + junCapCenterRelY * size + junY * s;
            float dx = wantX - cb.center.x, dy = wantY - cb.center.y, wantZ = cp.z + junCapZ;
            if (changed || !junPlaced || Math.Abs(dx) > 0.05f || Math.Abs(dy) > 0.05f || Math.Abs(t.position.z - wantZ) > 0.05f)
            {
                t.position = new Vector3(t.position.x + dx, t.position.y + dy, wantZ);
                if (!junPlaced) { junPlaced = true; junPlacedAt = now; LogJun("placed"); }
                else if (junFixLogs++ < 5) LogJun("put back");
            }
            if (junPlaced && !junLateLogged && now - junPlacedAt > 2f) { junLateLogged = true; LogJun("2 s later"); }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: Jun tick failed: " + ex.Message); }
    }

    static void RunJunCmd(string arg)
    {
        string a = (arg ?? "").Trim().ToLowerInvariant();
        if (a == "hide") { junWanted = false; JunLeave(); return; }
        var ap = a.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        float sl; if (ap.Length == 0 || !StoryScript.ParsePos(ap[0], out sl)) sl = 0f;
        // every @jun line says everything: a size or height you leave out goes back to normal (size 1, height 0), it is not carried over from an earlier @jun
        float jz = 1f; if (ap.Length > 1 && !StoryScript.ParseSize(ap[1], out jz)) jz = 1f;
        if (Math.Abs(jz - junSizeMul) > 0.001f) { junSizeMul = jz; junPlaced = false; }
        float jy = 0f; if (ap.Length > 2 && !StoryScript.ParseHeight(ap[2], out jy)) jy = 0f;
        junY = jy;
        junWanted = true; junX = sl * Cfg.CharSpread.Value;
        if (!bgUsed || bgCurrent == "room") return;                                 // in the room she is already there
        if (bgReplaceAt < 0f) { if (!junInScene) JunMoveIn(); else junNextCheck = -1f; }   // during a background change she is asked for once the camera has arrived
    }

    static float bgReplaceAt2 = -1f;
    static string bgCurrent = "";
    // back in the room the game puts her there itself (with a new random place); this only logs whether she is really there
    static void CheckJun()
    {
        try
        {
            if (bgCurrent != "room") return;
            junInScene = false;
            var ci = Controller();
            if (ci == null) { Cfg.Log.LogInfo("Story: Jun check: no ready controller in the room yet"); return; }
            junCtl = ci;
            LogJun("in room");
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: Jun check failed: " + ex.Message); }
    }
    static void TickBg()
    {
        float now = Time.unscaledTime;
        if (bgReplaceAt > 0f && now >= bgReplaceAt)
        {
            bgReplaceAt = -1f;
            foreach (var c in storyChars) { try { c.gameObject.SetActive(true); } catch { } }
            try { PlaceAll(); } catch (Exception ex) { Cfg.Log.LogWarning("Story: re-placing characters failed: " + ex.Message); }
            if (bgCurrent == "room") { CheckJun(); junRoomCheckAt = now + 2.5f; }
            else if (junWanted) JunMoveIn();
            try
            {
                var cam = Camera.main;
                Cfg.Log.LogInfo("Story: after background, camera " + (cam != null ? cam.transform.position + " size " + cam.orthographicSize : "none") + ", " + storyChars.Count + " characters");
                foreach (var c in storyChars) Cfg.Log.LogInfo("Story: char " + DescribeChar(c));
            }
            catch { }
            bgReplaceAt2 = now + 0.8f;
            DumpSceneDraw("after-bg");
        }
        if (bgReplaceAt2 > 0f && now >= bgReplaceAt2)
        {
            bgReplaceAt2 = -1f;
            foreach (var c in storyChars) { try { c.gameObject.SetActive(true); } catch { } }
            try { PlaceAll(); } catch { }
        }
        JunTick(now);
        if (junRoomCheckAt > 0f && now >= junRoomCheckAt) { junRoomCheckAt = -1f; if (storyRunning) CheckJun(); }
        if (bgZoomOutAt > 0f && now >= bgZoomOutAt) { bgZoomOutAt = -1f; try { ZoomOut(); } catch { } }
    }

    static string NormId(string t) { return Regex.Replace((t ?? "").ToLowerInvariant(), "[^a-z0-9]", ""); }

    // finds a character that @show put on screen, by the name used in the script (Melissa is the "Cashier" portrait)
    // story names -> the game's portrait ids (normalised). Shanice's portraits are called Nun / Nun_NewOutfit (F7 list, v5.17a)
    static string PortraitAlias(string w)
    {
        if (w == "melissa" || w == "amei") return "cashier";
        if (w == "realmelissa") return "unknownwoman";
        if (w == "ladyannalie") return "annalie";
        if (w == "shanice" || w == "nunshanice") return "nunnewoutfit";
        if (w == "shaniceold" || w == "oldshanice") return "nun";
        return w;
    }
    static bool CharMatches(SimpleCharacter c, string who)
    {
        string w = NormId(who);
        if (w.Length == 0) return false;
        string id = ""; try { id = NormId(c.DefaultId); } catch { }
        if (w == "shanice" || w == "nunshanice") return id.StartsWith("nun");     // either of her looks
        w = PortraitAlias(w);
        return id == w || id.StartsWith(w);
    }
    static SimpleCharacter FindShown(string who)
    {
        for (int i = storyChars.Count - 1; i >= 0; i--) if (CharMatches(storyChars[i], who)) return storyChars[i];
        return null;
    }

    static void SendEmote(SimpleCharacter ch, string slot, string item)
    {
        var l = new Il2CppSystem.Collections.Generic.List<EmoteData>();
        l.Add(new EmoteData(slot, item, false));
        ch.SetEmote(l.TryCast<Il2CppSystem.Collections.Generic.IEnumerable<EmoteData>>());
    }

    // Characters spell their faces differently (Annalie: eyeClosed / shocked, her outside look: smileEyeClosed / suprised), so tags are
    // matched by meaning: every word in a group below counts as the same expression.
    static readonly string[][] FaceGroups =
    {
        new[] { "happy", "veryhappy", "laugh", "smile", "eyeclosed", "smileeyeclosed", "smileclosed", "closed", "grin", "blush", "veryblush", "shy", "veryshy", "open" },
        new[] { "sad", "depressed", "verysad", "cry", "hmm", "upset", "serious" },
        new[] { "surprised", "suprised", "shocked", "shock", "veryshock", "awww", "serious" },
        new[] { "pervy", "pervygrin", "smug", "sly" },
        new[] { "normal", "neutral", "angry", "veryangry" }
    };
    static readonly string[][] ArmGroups =
    {
        new[] { "up", "point", "wave", "raise", "lecture", "hi" },
        new[] { "down", "clench", "lower", "clenched" }
    };

    static List<string> Candidates(string tag, string[][] groups)
    {
        string t = NormId(tag);
        var c = new List<string> { t };
        foreach (var g in groups) if (Array.IndexOf(g, t) >= 0) foreach (var w in g) if (!c.Contains(w)) c.Add(w);
        return c;
    }

    // exact tag first, then the rest of its group, searched in the character's combo slots (faces) or normal slots (arms)
    static bool FaceFor(SimpleCharacter ch, string tag, out string slotId, out string itemId)
    {
        slotId = ""; itemId = "";
        var cands = Candidates(tag, FaceGroups);
        // pass 0: exact names; pass 1: names that end with the word (Shanice's faces are called nunFaceHappy, nunFaceThinking ...)
        for (int pass = 0; pass < 2; pass++)
            foreach (string want in cands)
            {
                try
                {
                    var cs = ch.comboSlots;
                    for (int i = 0; i < cs.Count; i++)
                    {
                        if (cs[i].Id.ToLowerInvariant().Contains("outfit")) continue;
                        var ids = Il2CppSystem.Linq.Enumerable.ToList(cs[i].GetItemIds());
                        for (int k = 0; k < ids.Count; k++)
                        {
                            string n = NormId(ids[k]);
                            if (pass == 0 ? n == want : (want.Length >= 4 && n.EndsWith(want))) { slotId = cs[i].Id; itemId = ids[k]; return true; }
                        }
                    }
                }
                catch { }
            }
        // then plain "Face" slots that are not combos (Big Daddy's bdhNormal, bdhSmile, bdhSerious): exact, then ending with the word
        for (int pass = 0; pass < 2; pass++)
            foreach (string want in cands)
            {
                try
                {
                    var ns = ch.NormalSlots;
                    for (int i = 0; i < ns.Count; i++)
                    {
                        if (!ns[i].GetId().ToLowerInvariant().Contains("face")) continue;
                        var ids = Il2CppSystem.Linq.Enumerable.ToList(ns[i].GetItemIds());
                        for (int k = 0; k < ids.Count; k++)
                        {
                            string n = NormId(ids[k]);
                            if (pass == 0 ? n == want : (want.Length >= 4 && n.EndsWith(want))) { slotId = ns[i].GetId(); itemId = ids[k]; return true; }
                        }
                    }
                }
                catch { }
            }
        // last: a single eye or mouth item with exactly that name ([pout], [really])
        string t = NormId(tag);
        try
        {
            var ns = ch.NormalSlots;
            for (int i = 0; i < ns.Count; i++)
            {
                string sid = ns[i].GetId().ToLowerInvariant();
                if (!sid.Contains("eye") && !sid.Contains("mouth")) continue;
                var ids = Il2CppSystem.Linq.Enumerable.ToList(ns[i].GetItemIds());
                for (int k = 0; k < ids.Count; k++) if (NormId(ids[k]) == t) { slotId = ns[i].GetId(); itemId = ids[k]; return true; }
            }
        }
        catch { }
        return false;
    }

    static bool ArmFor(SimpleCharacter ch, string tag, out string slotId, out string itemId)
    {
        slotId = ""; itemId = "";
        string t = NormId(tag);
        bool isArm = false; foreach (var g in ArmGroups) if (Array.IndexOf(g, t) >= 0) isArm = true;
        var cands = isArm ? Candidates(tag, ArmGroups) : new List<string> { t };     // not an arm word: only an arm item with exactly that name ([palm], [gunfingerup])
        for (int pass = 0; pass < (isArm ? 2 : 1); pass++)
            foreach (string want in cands)
            {
                try
                {
                    var ns = ch.NormalSlots;
                    for (int i = 0; i < ns.Count; i++)
                    {
                        string sid = ns[i].GetId();
                        if (!sid.ToLowerInvariant().Contains("arm") && !sid.ToLowerInvariant().Contains("hand")) continue;
                        var ids = Il2CppSystem.Linq.Enumerable.ToList(ns[i].GetItemIds());
                        for (int k = 0; k < ids.Count; k++)
                        {
                            string n = NormId(ids[k]);
                            if (pass == 0 ? n == want : (want.Length >= 2 && (n.StartsWith(want) || n.EndsWith(want)))) { slotId = sid; itemId = ids[k]; return true; }
                        }
                    }
                }
                catch { }
            }
        return false;
    }

    static void SendEmotes(SimpleCharacter ch, List<KeyValuePair<string, string>> pairs)
    {
        var l = new Il2CppSystem.Collections.Generic.List<EmoteData>();
        foreach (var kv in pairs) l.Add(new EmoteData(kv.Key, kv.Value, false));
        ch.SetEmote(l.TryCast<Il2CppSystem.Collections.Generic.IEnumerable<EmoteData>>());
    }

    // one face and one arm position at most per call
    static void StoryFace(SimpleCharacter ch, string tags)
    {
        var chosen = new List<KeyValuePair<string, string>>();
        var faceSlots = new HashSet<string>();      // one face per slot: [normal][pout] = her normal face, then the pouting mouth on top
        bool gotArm = false;
        string missed = "";
        foreach (string tag in (tags ?? "").Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string slot, item;
            if (FaceFor(ch, tag, out slot, out item) && !faceSlots.Contains(slot)) { chosen.Add(new KeyValuePair<string, string>(slot, item)); faceSlots.Add(slot); }
            else if (!gotArm && ArmFor(ch, tag, out slot, out item)) { chosen.Add(new KeyValuePair<string, string>(slot, item)); gotArm = true; }
            else missed += tag + " ";
        }
        if (chosen.Count > 0)
        {
            try { SendEmotes(ch, chosen); } catch (Exception ex) { Cfg.Log.LogWarning("Story: emote failed: " + ex.Message); return; }
            var sb = new StringBuilder(); foreach (var kv in chosen) sb.Append(kv.Key + "/" + kv.Value + " ");
            Cfg.Log.LogInfo("Story: " + ch.name + " -> " + sb.ToString().Trim());
        }
        if (missed.Length > 0 && chosen.Count == 0) Cfg.Log.LogInfo("Story: no face or arm for '" + missed.Trim() + "' on " + ch.name + " (see the items list logged when it was shown)");
    }

    static void StoryOutfit(SimpleCharacter ch, string name)
    {
        string want = NormId(name);
        if (want == "default" || want == "none" || want == "normal") { try { ch.ResetSlots(); ch.SetDefaultEmote(true); } catch { } Cfg.Log.LogInfo("Story: " + ch.name + " outfit reset"); return; }
        try
        {
            var cs = ch.comboSlots;      // whole outfits first (Cashier: Outfit/Uniform, Daily, Daily2), so hat and arms change with them
            for (int i = 0; i < cs.Count; i++)
            {
                var ids = Il2CppSystem.Linq.Enumerable.ToList(cs[i].GetItemIds());
                for (int k = 0; k < ids.Count; k++)
                    if (NormId(ids[k]) == want && cs[i].Id.ToLowerInvariant().Contains("outfit")) { SendEmote(ch, cs[i].Id, ids[k]); Cfg.Log.LogInfo("Story: " + ch.name + " outfit " + cs[i].Id + "/" + ids[k]); return; }
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: outfit (combo) failed: " + ex.Message); }
        try
        {
            var ns = ch.NormalSlots;
            for (int i = 0; i < ns.Count; i++)
            {
                var ids = Il2CppSystem.Linq.Enumerable.ToList(ns[i].GetItemIds());
                for (int k = 0; k < ids.Count; k++)
                    if (NormId(ids[k]) == want) { SendEmote(ch, ns[i].GetId(), ids[k]); Cfg.Log.LogInfo("Story: " + ch.name + " outfit " + ns[i].GetId() + "/" + ids[k]); return; }
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: outfit failed: " + ex.Message); return; }
        Cfg.Log.LogInfo("Story: no outfit item '" + name + "' on " + ch.name);
    }

    static void RunStoryCmd(string cmd)
    {
        var parts = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string verb = parts[0].ToLowerInvariant();
        if (verb == "background") { RunBackground(parts[1]); return; }
        if (verb == "minigame") { StartMinigame(cmd); return; }
        if (verb == "score") { mgTotal = 0; Cfg.Log.LogInfo("Story: score reset"); return; }
        if (verb == "total")
        {
            foreach (var tg in StoryScript.TotalTargets(cmd))
                if (tg.Value < 0f || mgTotal >= tg.Value) { Cfg.Log.LogInfo("Story: total " + mgTotal + " -> " + tg.Key); GotoScene(tg.Key); return; }
            return;
        }
        if (verb == "music") { StoryMusic.Command(parts.Length > 1 ? string.Join(" ", parts, 1, parts.Length - 1) : "stop", bgCurrent); return; }
        if (verb == "jun") { RunJunCmd(parts.Length > 1 ? string.Join(" ", parts, 1, parts.Length - 1) : "center"); return; }
        var sc = SimpleCharacterSingleton.Instance;
        if (sc == null) { Cfg.Log.LogWarning("Story: character system not ready"); return; }
        if (verb == "face" || verb == "outfit")
        {
            var who = FindShown(parts[1]);
            if (who == null) { Cfg.Log.LogWarning("Story: " + parts[1] + " is not on screen (use @show first)"); return; }
            string rest = string.Join(" ", parts, 2, parts.Length - 2);
            if (verb == "face") StoryFace(who, NormId(rest).Length > 0 ? rest.ToLowerInvariant() : ""); else StoryOutfit(who, rest);
            return;
        }
        if (verb == "size")
        {
            var sz = FindShown(parts[1]);
            if (sz == null) { Cfg.Log.LogWarning("Story: @size " + parts[1] + ": nobody by that name is on screen"); return; }
            float sv; if (parts.Length < 3 || !StoryScript.ParseSize(parts[2], out sv)) { Cfg.Log.LogWarning("Story: @size needs a number like 0.8"); return; }
            storyCharSize[sz.GetInstanceID()] = sv;
            PlaceAll();
            if (!stageQuiet) Cfg.Log.LogInfo("Story: sized " + parts[1] + " to " + sv);
            return;
        }
        if (verb == "move")
        {
            var mv = FindShown(parts[1]);
            if (mv == null) { Cfg.Log.LogWarning("Story: @move " + parts[1] + ": nobody by that name is on screen"); return; }
            float msl; if (!StoryScript.ParsePos(parts.Length > 2 ? parts[2] : "center", out msl)) msl = 0f;
            float myy; if (parts.Length > 3 && StoryScript.ParseHeight(parts[3], out myy)) storyCharY[mv.GetInstanceID()] = myy;
            PlaceChar(mv, msl * Cfg.CharSpread.Value);
            if (!stageQuiet) Cfg.Log.LogInfo("Story: moved " + parts[1] + " to " + (parts.Length > 2 ? parts[2] : "center"));
            return;
        }
        if (verb == "slide")
        {
            var sl = FindShown(parts[1]);
            if (sl == null) { Cfg.Log.LogWarning("Story: @slide " + parts[1] + ": nobody by that name is on screen"); return; }
            float to; if (!StoryScript.ParsePos(parts.Length > 2 ? parts[2] : "center", out to)) to = 0f;
            float secs = 3f; if (parts.Length > 3) { float sv; if (float.TryParse(parts[3].TrimEnd('s', 'S'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out sv)) secs = Mathf.Clamp(sv, 0.1f, 60f); }
            float cur; if (!storyCharX.TryGetValue(sl.GetInstanceID(), out cur)) cur = 0f;
            CancelSlide(sl);
            slides.Add(new SlideAnim { Ch = sl, From = cur, To = to * Cfg.CharSpread.Value, Start = Time.unscaledTime, Dur = secs });
            Cfg.Log.LogInfo("Story: " + parts[1] + " slides to " + (parts.Length > 2 ? parts[2] : "center") + " over " + secs + " s");
            return;
        }
        if (verb == "stop")
        {
            if (parts.Length < 2 || parts[1].Equals("all", StringComparison.OrdinalIgnoreCase)) { slides.Clear(); Cfg.Log.LogInfo("Story: all slides stopped"); return; }
            var st = FindShown(parts[1]);
            if (st != null) CancelSlide(st);
            Cfg.Log.LogInfo("Story: " + parts[1] + " stopped");
            return;
        }
        if (verb == "hide")
        {
            if (parts.Length < 2 || parts[1].Equals("all", StringComparison.OrdinalIgnoreCase)) { sc.HideAllCharacters(); storyChars.Clear(); Cfg.Log.LogInfo("Story: hid all characters"); return; }
            int hidden = 0;
            for (int i = storyChars.Count - 1; i >= 0; i--)
            {
                if (!CharMatches(storyChars[i], parts[1])) continue;
                string id = ""; try { id = storyChars[i].DefaultId; } catch { }
                sc.HideCharacter(storyChars[i]); storyChars.RemoveAt(i); hidden++; Cfg.Log.LogInfo("Story: hid " + id);
            }
            if (hidden == 0) Cfg.Log.LogWarning("Story: @hide " + parts[1] + ": nobody by that name is on screen");
            else PlaceAll();
            return;
        }
        // show
        string want = parts[1];
        var keys = sc.runtimeCharacterReferences.keys; var vals = sc.runtimeCharacterReferences.values;
        int idx = -1;
        for (int i = 0; i < keys.Count; i++) if (keys[i].Equals(want, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
        if (idx < 0)
        {
            string nw = PortraitAlias(NormId(want));
            for (int i = 0; i < keys.Count; i++) if (NormId(keys[i]) == nw) { idx = i; break; }
            if (idx < 0) for (int i = 0; i < keys.Count; i++) if (NormId(keys[i]).StartsWith(nw)) { idx = i; break; }
            if (idx >= 0) Cfg.Log.LogInfo("Story: '" + want + "' -> portrait " + keys[idx]);
        }
        if (idx < 0) { Cfg.Log.LogWarning("Story: no character called '" + want + "'. Press F7 for the list."); return; }
        var ch = sc.InstantiatePrefab(vals[idx], keys[idx]);
        if (ch == null) { Cfg.Log.LogWarning("Story: could not create " + want); return; }
        storyChars.Add(ch);
        string pos = parts.Length > 2 ? parts[2].ToLowerInvariant() : "center";
        float slots; if (!StoryScript.ParsePos(pos, out slots)) slots = 0f;
        float x = slots * Cfg.CharSpread.Value;
        try
        {
            // top level on purpose: under the room view it would be switched off together with the apartment when a story changes background
            ch.transform.SetParent(null, true);
            try { ch.gameObject.SetActive(true); } catch { }
            try { ch.SetDefaultEmote(true); } catch { }
            try { foreach (var r in ch.GetComponentsInChildren<SpriteRenderer>(true)) { var c = r.color; if (c.a < 1f) { c.a = 1f; r.color = c; } } } catch { }
            float shs; if (parts.Length > 3 && StoryScript.ParseSize(parts[3], out shs)) storyCharSize[ch.GetInstanceID()] = shs;
            float shy; if (parts.Length > 4 && StoryScript.ParseHeight(parts[4], out shy)) storyCharY[ch.GetInstanceID()] = shy;
            PlaceChar(ch, x);
            storyCharX[ch.GetInstanceID()] = x;
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: placing " + want + " failed: " + ex.Message); }
        try
        {
            var items = Il2CppSystem.Linq.Enumerable.ToList(ch.GetItems());
            var names = new List<string>();
            for (int i = 0; i < items.Count && i < 300; i++) names.Add(items[i]);
            Cfg.Log.LogInfo("Story: " + keys[idx] + " slots: " + ch.GenerateSlotSummary());
            Cfg.Log.LogInfo("Story: " + keys[idx] + " items (" + items.Count + "): " + string.Join(", ", names.ToArray()));
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: could not list " + keys[idx] + " expressions: " + ex.Message); }
        Cfg.Log.LogInfo("Story: showed " + keys[idx] + " at " + pos + " -> " + DescribeChar(ch));
    }

    // Name-tag colour: your own  color: Name = #rrggbb  line first, then the colour the game itself uses for that character.
    static Color StoryColor(string name)
    {
        try
        {
            string hx;
            if (story != null && story.Colors.TryGetValue(name, out hx))
            {
                int v = Convert.ToInt32(hx.Substring(1), 16);
                return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
            }
        }
        catch { }
        try
        {
            var cs = ConversationSingleton.Instance;
            if (cs != null && name.Length > 0)
            {
                string bot = Shared.Current.Bot;
                string[] keys = name == bot ? new[] { name, "Bot" } : new[] { name };
                foreach (string k in keys)
                {
                    int n = cs.NormalCharacters.Count;
                    for (int i = 0; i < n; i++)
                    {
                        var nc = cs.NormalCharacters[i];
                        string id = ""; string nt = "";
                        try { id = nc.Id; } catch { }
                        try { nt = nc.NameText; } catch { }
                        if (id.Equals(k, StringComparison.OrdinalIgnoreCase) || nt.Equals(k, StringComparison.OrdinalIgnoreCase))
                        {
                            var c = nc.NameColor; c.a = 1f; return c;
                        }
                    }
                }
            }
        }
        catch { }
        return Color.white;
    }

    static void ShowStoryLine(StoryLine l)
    {
        string name = StoryName(l.Who);
        string text = StoryText(l.Text);
        if (text.Length == 0) { storyIdx++; return; }
        var dialog = FindMainDialog() ?? FindDialog();
        if (dialog == null) { EndStory("Story stopped: no dialogue box found."); return; }
        if (!WriterFree(dialog)) return;      // tried again next frame
        fadeStart = -1f;
        try { if (fadeGroup != null) fadeGroup.alpha = 1f; } catch { }
        dialog.gameObject.SetActive(true);
        try { dialog.Stop(); } catch { }                     // end any previous Say so the new line really replaces it
        try { dialog.SetCharacter(null); } catch { }
        try { dialog.SetCharacterName(name, StoryColor(name)); } catch { }
        Cfg.Log.LogInfo("Story line " + storyIdx + " [" + name + "]: " + (text.Length > 50 ? text.Substring(0, 50) + "..." : text));
        storyLineDone = false; storyWaitLine = true; storyLineAt = Time.unscaledTime;
        int myToken = ++storyToken;
        var cb = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(() => { if (myToken == storyToken) storyLineDone = true; }));
        storyCallbacks.Add(cb); if (storyCallbacks.Count > 20) storyCallbacks.RemoveAt(0);
        storyDialog = dialog;
        storyLen = text.Length; StoryAdvance = false; storySkipped = false; storyFix = 0; storyPrevText = storyLastText ?? ""; storyLastText = text;
        dialog.Say(text, true, true, false, false, null);   // the game never moves on by itself; the plugin waits for your click
        lastDialog = dialog; storyLastText = text;
        if (!stageMode) QueueStoryLog(name, text);
        // her mouth and face when she is the one talking
        if (name.Length > 0 && name == Shared.Current.Bot)
        {
            speakText = text; spoken = 0; speakStart = Time.time;
            try { var ci = Controller(); if (ci != null && l.Tags.Length > 0) ApplyEmotion(ci, l.Tags); } catch { }
        }
        else if (l.Tags.Length > 0)
        {
            try { var pc = FindShown(l.Who); if (pc != null) StoryFace(pc, l.Tags); } catch (Exception ex) { Cfg.Log.LogWarning("Story: face failed: " + ex.Message); }
        }
    }

    // ---- the game's own dialogue log (the log button next to the text box), v5.19 ----
    // The game writes a line there when its dialogue box reports "finished" (Fungus NarrativeLog.OnSayDialogFinished). Story lines
    // are said without that, and with no speaker attached, so they were missing. Each line is written when the next one starts
    // (or the story ends). If the game did log it after all, only the missing speaker is filled in, so nothing is doubled.
    static string logPendName, logPendText; static bool logWarned;
    static void QueueStoryLog(string name, string text) { FlushStoryLog(); logPendName = name ?? ""; logPendText = text; }
    static void FlushStoryLog()
    {
        if (logPendText == null) return;
        string name = logPendName, text = logPendText;
        logPendName = null; logPendText = null;
        if (!Cfg.StoryInLog.Value) return;
        try
        {
            var fm = Fungus.FungusManager.Instance;
            var nl = fm != null ? fm.NarrativeLog : null;
            if (nl == null) return;
            var ch = LogCharacter(name);
            try
            {
                var q = nl.GetRecentLines();
                if (q != null && q.Count > 0)
                {
                    var arr = q.ToArray();
                    var last = arr[arr.Length - 1];
                    if (last != null && last.text == text) { if (last.character == null && ch != null) last.character = ch; return; }
                }
            }
            catch { }
            nl.AddLine(ch, text);
        }
        catch (Exception ex) { if (!logWarned) { logWarned = true; Cfg.Log.LogWarning("Story: could not add the line to the game's log: " + ex.Message); } }
    }
    // the game's own speaker for a name (keeps its colour in the log); narration has none
    static Fungus.ISayDialogCharacter LogCharacter(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var cs = ConversationSingleton.Instance;
        if (cs == null) return null;
        if (name == Shared.Current.Bot) { try { return cs.BotLive2DCharacter.Cast<Fungus.ISayDialogCharacter>(); } catch { } }
        try
        {
            int n = cs.NormalCharacters.Count;
            for (int i = 0; i < n; i++)
            {
                var nc = cs.NormalCharacters[i];
                string id = "", nt = "";
                try { id = nc.Id; } catch { }
                try { nt = nc.NameText; } catch { }
                if (id.Equals(name, StringComparison.OrdinalIgnoreCase) || nt.Equals(name, StringComparison.OrdinalIgnoreCase)) return nc.Cast<Fungus.ISayDialogCharacter>();
            }
        }
        catch { }
        try { return cs.GetTemporaryCharacter(name); } catch { return null; }
    }

    // v5.19c: the game's typewriter can be left "busy" for ever. The log showed the load greeting still typing when a story started;
    // the box was switched off for a moment, which kills its typing routine, and every later line waited for it (text stuck mid-word).
    // Before a story line: ask the writer to stop; if it still says busy after 0.6 s, its routine is gone, so its flags are cleared by hand.
    static float writerWaitSince = -1f;
    static bool WriterFree(SayDialog d)
    {
        Fungus.Writer w = null;
        try { w = d.GetWriter(); } catch { }
        bool busy = false;
        try { busy = w != null && (w.IsWriting || w.IsWaitingForInput); } catch { }
        if (!busy) { writerWaitSince = -1f; return true; }
        if (writerWaitSince < 0f) { writerWaitSince = Time.unscaledTime; try { d.gameObject.SetActive(true); d.Stop(); } catch { } return false; }
        if (Time.unscaledTime - writerWaitSince < 0.6f) return false;
        try { w.isWriting = false; w.isWaitingForInput = false; w.exitFlag = false; } catch { }
        writerWaitSince = -1f;
        Cfg.Log.LogWarning("Story: the text box was stuck typing an earlier line (its typing was cut off); reset it");
        return true;
    }

    // ---- @minigame (v5.20): the game's own timing bar (ClickMinigameSkillCheck) or work clicking game, inside a story ----
    // The game starts its minigames with MinigameCore.InstantiateAndInit(onComplete, parent) and gets a score from 0 to 1 back.
    // The prefab has to be in memory; if it is not, the score is a random roll and the log says so. {score} {pins} {total} work in story text.
    static bool mgWaiting; static volatile bool mgDone; static float mgScore, mgStarted; static string mgCmd = "";
    static GameObject mgObj; static Il2CppSystem.Action<float> mgCallback; static int mgTotal;
    static readonly System.Random mgRandom = new System.Random();

    static void StartMinigame(string cmd)
    {
        var p = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string ty = p.Length > 1 ? p[1].ToLowerInvariant() : "skillcheck";
        float diff = -1f, f;
        if (p.Length > 2 && float.TryParse(p[2].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) diff = f;
        mgCmd = cmd; mgDone = false; mgScore = 0f; mgWaiting = true; mgStarted = Time.unscaledTime; mgObj = null;
        try
        {
            ClickMinigameBase proto = null;
            if (ty == "work") { foreach (var m in Resources.FindObjectsOfTypeAll<ClickMinigameWork>()) { proto = m; if (string.IsNullOrEmpty(m.gameObject.scene.name)) break; } }
            else { foreach (var m in Resources.FindObjectsOfTypeAll<ClickMinigameSkillCheck>()) { proto = m; if (string.IsNullOrEmpty(m.gameObject.scene.name)) break; } }
            if (proto == null)
            {
                mgScore = (float)mgRandom.NextDouble(); mgDone = true;
                Cfg.Log.LogWarning("Story: the game's " + ty + " minigame is not loaded right now, so the throw is a random roll: " + mgScore.ToString("0.00") + ". (Doing a work shift once may load it.)");
                return;
            }
            var dlg = storyDialog ?? FindMainDialog() ?? FindDialog();
            Transform parent = null;
            try { var cv = dlg != null ? dlg.GetComponentInParent<Canvas>() : null; if (cv != null) parent = cv.rootCanvas.transform; } catch { }
            if (parent == null) { try { var cv2 = UnityEngine.Object.FindObjectOfType<Canvas>(); if (cv2 != null) parent = cv2.rootCanvas.transform; } catch { } }
            mgCallback = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<float>>(new Action<float>(s => { mgScore = s; mgDone = true; }));
            // v5.20d: the game's own start (InstantiateAndInit) does the setup and begins a round. With a difficulty, that round is stopped in the
            // same frame (nothing has been drawn yet) and restarted once at the difficulty. Restart on a copy made by hand crashed (no setup),
            // and Restart on top of the running round gave two rounds at once.
            mgObj = proto.InstantiateAndInit(mgCallback, parent);
            if (diff >= 0f && mgObj != null)
            {
                try
                {
                    var cb = mgObj.GetComponent<ClickMinigameBase>();
                    cb.StopAllCoroutines();          // the round the game's start began
                    cb.Restart(diff);                // only resets the bar and sets the difficulty (v5.20d froze on "Ready?" without the next line)
                    cb.StartCoroutine(cb.Game());    // the round itself: "Ready?", the moving cursor, the score
                }
                catch (Exception ex) { Cfg.Log.LogWarning("Story: minigame difficulty could not be set (" + ex.Message + "); it runs at the game's normal difficulty"); }
            }
            try { if (mgObj != null) mgObj.transform.SetAsLastSibling(); } catch { }
            Cfg.Log.LogInfo("Story: minigame " + ty + " started from " + proto.name + " (prefab: " + string.IsNullOrEmpty(proto.gameObject.scene.name) + ") on " + (parent != null ? parent.name : "no canvas") + (diff >= 0f ? ", difficulty " + diff : ""));
        }
        catch (Exception ex)
        {
            mgScore = (float)mgRandom.NextDouble(); mgDone = true;
            Cfg.Log.LogWarning("Story: minigame could not start (" + ex.Message + "), random roll " + mgScore.ToString("0.00"));
        }
    }

    static void TickMinigame()
    {
        if (!mgDone && Time.unscaledTime - mgStarted > 90f) { mgDone = true; mgScore = 0f; Cfg.Log.LogWarning("Story: the minigame never reported a score in 90 s; counted as 0"); }
        if (!mgDone) return;
        mgWaiting = false;
        try { if (mgObj != null) UnityEngine.Object.Destroy(mgObj); } catch { }
        mgObj = null;
        float s = Mathf.Clamp01(mgScore);
        int pins = Mathf.RoundToInt(s * 10f);
        mgTotal += pins;
        Cfg.Log.LogInfo("Story: minigame score " + s.ToString("0.00") + " (" + pins + " pins, total " + mgTotal + ")");
        storyNextAt = Time.unscaledTime + 0.3f;
        foreach (var tg in StoryScript.MinigameTargets(mgCmd))
            if (tg.Value < 0f || s >= tg.Value) { GotoScene(tg.Key); return; }
    }

    static int storyFix; static string storyPrevText = "";
    // The box can keep the previous line after a choice popup or a background switch. Only act when the game's own current text
    // is exactly the previous line (never because of how far the typing has got): say the new line once more.
    static void StoryWatchdog(float age)
    {
        try
        {
            var d = storyDialog; if (d == null || storyLastText == null || storyFix >= 1 || age < 0.8f) return;
            string cur = null; try { cur = d.CurrentStoryText; } catch { }
            if (string.IsNullOrEmpty(cur) || cur == storyLastText || storyPrevText.Length == 0 || cur != storyPrevText) { storyFix = 1; return; }
            storyFix = 1;
            Cfg.Log.LogWarning("Story: the box still holds the previous line, saying the new one again");
            d.gameObject.SetActive(true);
            d.Stop(); d.Say(storyLastText, true, true, false, false, null);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story watchdog failed: " + ex.Message); }
    }

    // The game's own choice buttons (Fungus MenuDialog), the same ones its stories use: they sit under the normal text box and the last line stays visible.
    static bool ShowStoryChoicesNative()
    {
        try
        {
            var md = Fungus.MenuDialog.GetMenuDialog();
            if (md == null) return false;
            md.SetActive(true);
            md.Clear();
            int added = 0;
            for (int i = 0; i < storyScene.Choices.Count; i++)
            {
                int idx = i;
                var cb = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(() => { storyChoice = idx; }));
                storyCallbacks.Add(cb);
                if (md.AddOption(StoryText(storyScene.Choices[i].Text), true, false, null, cb)) added++;
            }
            if (added == 0) { md.Clear(); return false; }
            storyChoice = -1; storyWaitChoice = true;
            Cfg.Log.LogInfo("Story: " + added + " choices shown with the game's own buttons");
            return true;
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Story: the game's choice buttons failed (" + ex.Message + "), using the popup"); return false; }
    }

    static void ShowStoryChoices()
    {
        if (Cfg.NativeChoices.Value && ShowStoryChoicesNative()) return;
        var ui = UiOverlay.Instance;
        if (ui == null) { EndStory("Story stopped: the game's UI is not ready."); return; }
        var choices = new Il2CppSystem.Collections.Generic.List<PopupChoice>();
        for (int i = 0; i < storyScene.Choices.Count; i++)
        {
            int idx = i;
            var cb = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(() => { storyChoice = idx; }));
            storyCallbacks.Add(cb);
            var pc = new PopupChoice(StoryText(storyScene.Choices[i].Text));
            pc.Action = cb;
            choices.Add(pc);
        }
        try { HideDialogCleanly(); } catch { }
        ui.Popup(story.Title, storyLastText.Length > 0 ? storyLastText : "What do you do?", choices);
        storyChoice = -1; storyWaitChoice = true;
    }

    static readonly List<Il2CppSystem.Action> notReadyCallbacks = new List<Il2CppSystem.Action>();
    static void ShowNotReady()
    {
        Shared.LastResult = Requirements.Why();
        try
        {
            var ui = UiOverlay.Instance;
            if (ui == null) return;
            var choices = new Il2CppSystem.Collections.Generic.List<PopupChoice>();
            choices.Add(new PopupChoice("OK"));
            ui.Popup(Mod.Stories ? "Stories aren't available yet" : Shared.Current.Bot + " isn't ready yet", Requirements.Checklist(), choices);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Not-ready popup failed: " + ex.Message); }
    }

    // The game's popup puts its buttons in one row, so more than about five run off the screen (seen in game with 16 stories).
    // The list is shown a few at a time with a "More" button that goes to the next page and wraps around.
    const int StoriesPerPage = 3;
    static int storyPage;
    public static volatile bool StoryMenuNextPage;

    static void ShowStoryMenu(bool keepPage = false)
    {
        var ui = UiOverlay.Instance;
        if (ui == null) { Shared.LastResult = "The game's UI is not ready yet."; return; }
        var list = StoryScript.ListAll();
        var choices = new Il2CppSystem.Collections.Generic.List<PopupChoice>();
        string text;
        if (list.Count == 0) text = "No stories yet. Open http://127.0.0.1:" + Cfg.Port.Value + "/stories in your browser to write one.";
        else
        {
            int pages = (list.Count + StoriesPerPage - 1) / StoriesPerPage;
            if (!keepPage) storyPage = 0;
            if (storyPage >= pages || storyPage < 0) storyPage = 0;
            text = "Pick a story to play" + (pages > 1 ? " (page " + (storyPage + 1) + " of " + pages + ", " + list.Count + " stories)" : "") + ". Press " + Cfg.StoryHotkey.Value + " again during a story to stop it. (Write more at http://127.0.0.1:" + Cfg.Port.Value + "/stories)";
            int first = storyPage * StoriesPerPage;
            for (int i = first; i < list.Count && i < first + StoriesPerPage; i++)
            {
                string id = list[i].Key;
                var cb = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(() => { StoryRequest = id; }));
                storyCallbacks.Add(cb);
                var pc = new PopupChoice(list[i].Value);
                pc.Action = cb;
                choices.Add(pc);
            }
            if (pages > 1)
            {
                var more = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(() => { storyPage = (storyPage + 1) % pages; StoryMenuNextPage = true; }));
                storyCallbacks.Add(more);
                var mc = new PopupChoice("More \u203a");
                mc.Action = more;
                choices.Add(mc);
            }
        }
        choices.Add(new PopupChoice("Close"));
        ui.Popup("Stories", text, choices);
    }

    // ---------- greeting when a save has loaded ----------
    static bool greetPlain;                // the next reply is the greeting: use the normal dialogue box
    static IntPtr greetedGv = IntPtr.Zero;  // native pointer of the save we already greeted (interop wrappers are new objects every time, so never compare them by reference)
    static float greetReadyAt = -1f;
    static volatile bool greetClosed;      // set by the game when you click the greeting away
    static bool greetActive; static float greetShownAt; static SayDialog greetDialog;
    static Il2CppSystem.Action greetDone;  // kept so it is not garbage collected

    static void TickGreetClose()
    {
        if (!greetActive) return;
        bool close = greetClosed && Time.unscaledTime - greetShownAt > 0.3f;
        if (!close && Time.unscaledTime - greetShownAt > 3f)
        {
            try { close = greetDialog == null || !greetDialog.gameObject.activeInHierarchy; }
            catch { close = true; }
        }
        if (!close) return;
        greetActive = false; greetClosed = false;
        if (Cfg.AutoZoom.Value) ZoomOut();
        try { var ci = Controller(); if (ci != null) ci.SetDefaultEmote(false); } catch { }
    }

    static string greetReason = "";
    static void GreetWhy(string why)
    {
        if (why == greetReason) return;
        greetReason = why;
        Cfg.Log.LogInfo("Greeting: " + why);
    }

    static void TickGreet()
    {
        if (!Cfg.GreetOnLoad.Value) return;
        try
        {
            var gs = GameScript.Instance;
            var gv = gs != null ? gs.GameVariables : null;
            if (gv == null) { greetReadyAt = -1f; GreetWhy("waiting: no save loaded yet"); return; }
            IntPtr gvPtr = gv.Pointer;
            if (gvPtr == greetedGv) return;
            if (Controller() == null) { greetReadyAt = -1f; GreetWhy("waiting: she is not on screen yet (no bot controller)"); return; }
            if (StoryBusy) { greetedGv = gvPtr; GreetWhy("skipped: a story is playing (no greeting for this save)"); return; }    // the log showed the greeting firing in the middle of a story
            if (greetReadyAt < 0f) { greetReadyAt = Time.unscaledTime; GreetWhy("save is loaded, she is on screen; waiting 5 s"); return; }
            if (Time.unscaledTime - greetReadyAt < 5f) return;           // let the game finish loading and fading in
            if (!Shared.Current.Loaded) { GreetWhy("waiting: game state not read yet"); return; }
            if (!Requirements.Ready) { greetedGv = gvPtr; GreetWhy("skipped: " + Requirements.Why()); return; }
            if (InputIsOpen()) { GreetWhy("waiting: chat input is open"); return; }
            if (GameMenuShowing()) { GreetWhy("waiting: the game is showing a menu"); return; }
            var d = SayDialog.ActiveSayDialog;
            if (d != null && d.gameObject.activeInHierarchy && !string.IsNullOrEmpty(d.CurrentStoryText)) { GreetWhy("waiting: the game is showing its own text"); return; }
            greetedGv = gvPtr;
            greetPlain = true;
            GreetWhy("asking the model for a greeting (state " + (CurrentInteract() != null ? "interact" : "other") + ")");
            Shared.LastResult = "Greeting...";
            PanelServer.AskAsync("(Game event) The game was just loaded and " + Shared.Current.Player + " is back with you. Greet them in one or two short sentences in your own words, fitting the time of day and your mood. Do not mention the game or loading.");
        }
        catch (Exception ex) { GreetWhy("error: " + ex.GetType().Name + ": " + ex.Message); }
    }

    // (head movement was removed; arm poses are used for gestures instead)
    public static void LateTick() { }

    public static Live2DInteractionController PetController() { return Controller(); }

    // the pet page asked for a reaction: same tags as in a reply ("happy wave", "shy", "surprised"...), back to normal after a few seconds
    public static void PetEmote(string tags)
    {
        try
        {
            var ci = Controller(); if (ci == null) return;
            ApplyEmotion(ci, (tags ?? "").ToLowerInvariant());
            emoteResetAt = Time.time + 5f;
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Pet: reaction failed: " + ex.Message); }
    }

    static Live2DInteractionController Controller()
    {
        try
        {
            var c = Live2DControllerSingleton.Instance.GetReadyController("bot");
            return c == null ? null : c.TryCast<Live2DInteractionController>();
        }
        catch { return null; }
    }

    // ---------- zoom ----------
    static InteractState CurrentInteract()
    {
        try
        {
            var st = GameScript.Instance.GameState;
            return st == null ? null : st.TryCast<InteractState>();
        }
        catch { return null; }
    }

    static void TrackZoomState()
    {
        // if the game left the interaction screen (or made a new one), its zoom is reset
        var cur = CurrentInteract();
        string id = cur == null ? null : cur.Pointer.ToString();
        if (id != zoomOwner) { zoomed = false; zoomOwner = id; }
    }

    static void ZoomIn()
    {
        try
        {
            var cur = CurrentInteract();
            if (cur == null) { Shared.LastResult = "Zoom skipped: not on the interaction screen."; return; }
            if (zoomed) return;
            cur.ZoomButtonClicked();
            zoomed = true;
            Cfg.Log.LogInfo("Zoom: in");
        }
        catch (Exception ex) { Shared.LastResult = "Zoom failed: " + ex.GetType().Name + ": " + ex.Message; }
    }

    static void ZoomOut()
    {
        try
        {
            var cur = CurrentInteract();
            if (cur == null || !zoomed) { zoomed = false; return; }
            cur.ZoomButtonClicked();
            zoomed = false;
            Cfg.Log.LogInfo("Zoom: out");
        }
        catch (Exception ex) { Shared.LastResult = "Zoom failed: " + ex.GetType().Name + ": " + ex.Message; }
    }

    static void ToggleZoom()
    {
        try
        {
            var cur = CurrentInteract();
            if (cur == null) { Shared.LastResult = "Zoom skipped: not on the interaction screen."; return; }
            zoomOwner = cur.Pointer.ToString();
            cur.ZoomButtonClicked();
            zoomed = !zoomed;
            Shared.LastResult = zoomed ? "Zoomed in." : "Zoomed out.";
        }
        catch (Exception ex) { Shared.LastResult = "Zoom failed: " + ex.GetType().Name + ": " + ex.Message; }
    }

    // ---------- fading the old bubble ----------
    static void HideDialogCleanly()
    {
        try
        {
            if (lastDialog == null) return;
            lastDialog.Stop();
            lastDialog.SetCharacter(null);   // otherwise the game's own text/menus keep showing "Jun" and sit next to her
            lastDialog.SetActive(false);
        }
        catch { }
    }

    static bool GameMenuShowing()
    {
        try
        {
            var m = Fungus.MenuDialog.ActiveMenuDialog;
            return m != null && m.gameObject.activeInHierarchy && m.DisplayedOptionsCount > 0;
        }
        catch { return false; }
    }

    static void FadeOldBubble()
    {
        try
        {
            bubbleText = ""; // stop keeping it alive
            if (lastDialog == null || !lastDialog.gameObject.activeInHierarchy) return;
            var g = lastDialog.GetComponent<CanvasGroup>();
            if (g == null) g = lastDialog.GetComponentInChildren<CanvasGroup>();
            if (g == null) { HideDialogCleanly(); return; }
            fadeGroup = g; fadeStart = Time.unscaledTime; hideAt = -1f;
        }
        catch { }
    }

    static void TickFade()
    {
        if (fadeStart < 0f) return;
        try
        {
            float t = (Time.unscaledTime - fadeStart) / 0.5f;
            if (t >= 1f)
            {
                fadeStart = -1f;
                if (fadeGroup != null) fadeGroup.alpha = 1f; // restore for the next message
                HideDialogCleanly();
            }
            else if (fadeGroup != null) fadeGroup.alpha = 1f - t;
        }
        catch { fadeStart = -1f; }
    }

    // Debugging aid: for 12 s after she speaks, log when the box switches on/off or the visible text gets SHORTER (= the typing restarted).
    static float traceUntil = -1f, traceStart; static int traceLen = -1; static bool traceActive; static string traceTag = "";
    static void TraceStart(string tag) { traceStart = Time.unscaledTime; traceUntil = traceStart + 12f; traceLen = -1; traceActive = true; traceTag = tag; }
    static void TraceLog(string what) { Cfg.Log.LogInfo("Trace(" + traceTag + ") +" + (Time.unscaledTime - traceStart).ToString("0.00") + "s: " + what); }
    static void TickTrace()
    {
        if (traceUntil < 0f) return;
        if (Time.unscaledTime > traceUntil) { traceUntil = -1f; TraceLog("end of trace (final text " + traceLen + " chars)"); return; }
        try
        {
            var d = lastDialog; if (d == null) return;
            bool act = d.gameObject.activeInHierarchy;
            string cur = d.CurrentStoryText; int len = cur == null ? 0 : cur.Length;
            if (act != traceActive) { traceActive = act; TraceLog("box switched " + (act ? "ON" : "OFF")); }
            if (traceLen <= 0 && len > 0) TraceLog("text appeared (" + len + " chars so far)");
            else if (len < traceLen) TraceLog("text got shorter: " + traceLen + " -> " + len + (len == 0 ? " (cleared)" : " (typing restarted?)"));
            traceLen = len;
        }
        catch { }
    }

    // If a click (or the game itself) wipes the message early, put it back until its time is up.
    static void TickKeep()
    {
        if (!Cfg.KeepAlive.Value || bubbleText.Length == 0 || lastDialog == null || fadeStart >= 0f) return;
        if (GameMenuShowing()) { bubbleText = ""; return; } // the game took over (choices, interact menu): leave it alone
        float now = Time.time;
        if (now > bubbleUntil) { bubbleText = ""; return; }
        if (now < nextKeepCheck) return;
        nextKeepCheck = now + 0.25f;
        try
        {
            string cur = lastDialog.CurrentStoryText;
            bool gone = !lastDialog.gameObject.activeInHierarchy || string.IsNullOrEmpty(cur);
            if (!gone && !keepSeen)
            {
                keepSeen = true;                          // she really was on screen; only then can something have wiped her message
                Cfg.Log.LogInfo("Keep-alive: text is on screen " + (now - lastSayTime).ToString("0.0") + " s after Say (" + (cur == null ? 0 : cur.Length) + " chars)");
            }
            if (gone && keepSeen && now - lastSayTime > 1.0f && restores < 2)
            {
                restores++;
                keepSeen = false;                        // after a restore she must be seen on screen again before another restore is allowed
                float since = now - lastSayTime;
                lastSayTime = now;
                Cfg.Log.LogInfo("Keep-alive: message looked gone " + since.ToString("0.0") + " s after Say (text='" + cur + "', active=" + lastDialog.gameObject.activeInHierarchy + "), restoring " + restores + "/2");
                lastDialog.gameObject.SetActive(true);
                lastDialog.Say(bubbleText, true, true, false, false, null);
            }
        }
        catch { }
    }

    // ---------- input ----------
    static InputPopup inputPop;

    public static void CloseInput()
    {
        var p = inputPop; inputPop = null;
        try { if (p != null) p.CloseFromUIOverlay(); } catch { }
    }

    public static bool InputIsOpen()
    {
        try { return inputPop != null && inputPop.gameObject != null && inputPop.gameObject.activeInHierarchy; }
        catch { return false; }
    }

    static void OpenInput()
    {
        if (StoryBusy) { Shared.LastResult = "A story is playing. Stop it first (" + (Mod.Chat ? "Story Creator's story key, F3" : Cfg.StoryHotkey.Value) + ")."; return; }
        if (InputIsOpen()) { CloseInput(); return; } // F2 again closes the window instead of stacking another one
        FadeOldBubble(); // the old message fades so it never sits behind the input window
        var ui = UiOverlay.Instance;
        if (ui == null) { Shared.LastResult = "The game's UI is not ready yet."; return; }
        string bot = Shared.Current.Bot;
        var cb = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<string>>(new Action<string>(OnSubmit));
        var pop = ui.InputPopup("Chat with " + bot, "Say something to " + bot + ":", cb, "", default);
        inputPop = pop;
    }

    static void OnCloseInput(string text)
    {
        CloseInput();
        Shared.LastResult = "Chat window closed.";
    }

    static void OnSendClicked(string text)
    {
        CloseInput();
        OnSubmit(text);
    }

    static void OnSubmit(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (!Requirements.Ready) { ShowNotReady(); return; }
            lastUser = text.Trim();
            StoryMemory.Add(Shared.Current.Player + " (you) told " + Shared.Current.Bot + ": " + lastUser);
            Shared.LastResult = "Waiting for the model...";
            PanelServer.AskAsync(lastUser);
            ShowWaiting();
        }
        catch (Exception ex) { Shared.LastResult = "Submit error: " + ex.Message; Cfg.Log.LogError(ex.ToString()); }
    }

    // ---------- waiting popup with a Cancel button ----------
    static SimplePopup waitPopup;

    static void ShowWaiting()
    {
        try
        {
            var ui = UiOverlay.Instance;
            if (ui == null) return;
            string bot = Shared.Current.Bot;
            var cancel = new PopupChoice("Cancel");
            cancel.Action = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(CancelChat));
            var choices = new Il2CppSystem.Collections.Generic.List<PopupChoice>();
            choices.Add(cancel);
            waitPopup = ui.Popup(bot, bot + " is thinking...", choices);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Waiting popup failed: " + ex.Message); }
    }

    static void CancelChat()
    {
        waitPopup = null; // the game closes it with the button
        PanelServer.CancelChat();
        Shared.LastResult = "Cancelled.";
    }

    static void CloseWaiting()
    {
        var p = waitPopup; waitPopup = null;
        try { if (p != null) p.CloseFromUIOverlay(); } catch { }
    }

    // ---------- reply: tags -> emotion + gesture, speech box, mouth ----------
    static void ParseTags(string reply, out string emotion, out string gest, out string clean)
    {
        emotion = ""; gest = ""; clean = reply ?? "";
        var m = Regex.Match(clean, @"^\s*((?:\[[A-Za-z .\-_]+\]\s*)+)");
        if (!m.Success) { clean = StripActions(clean); return; }
        var tags = new List<string>();
        foreach (Match t in Regex.Matches(m.Groups[1].Value, @"\[([A-Za-z .\-_]+)\]")) tags.Add(Regex.Replace(t.Groups[1].Value.ToLowerInvariant(), "[^a-z]", ""));
        emotion = string.Join(" ", tags);
        clean = StripActions(clean.Substring(m.Length).Trim());
    }

    // removes *stage directions* like "*Jun's eyebrows knit.*" from what she says
    static string StripActions(string t)
    {
        var r = Regex.Replace(t ?? "", @"\*[^*]*\*", " ");
        r = Regex.Replace(r, @"[ \t]{2,}", " ").Trim();
        return r.Length > 0 ? r : (t ?? "").Replace("*", "").Trim();
    }

    static void SetEmotes(Live2DInteractionController ci, List<KeyValuePair<string, string>> pairs)
    {
        var chosen = new Il2CppSystem.Collections.Generic.List<EmoteData>();
        foreach (var kv in pairs) chosen.Add(new EmoteData(kv.Key, kv.Value, false));
        ci.SetEmote(chosen.TryCast<Il2CppSystem.Collections.Generic.IEnumerable<EmoteData>>());
    }

    static void ApplyEmotion(Live2DInteractionController ci, string tagString)
    {
        try
        {
            var mgr = ci.GetStoryEmoteManager();
            var all = new HashSet<string>();
            var items = Il2CppSystem.Linq.Enumerable.ToList(mgr.AllItems);
            var sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                var t = items[i];
                all.Add(t.Item1 + "/" + t.Item2);
                sb.Append(t.Item1).Append('/').Append(t.Item2).Append(", ");
            }
            if (!emotesLogged)
            {
                emotesLogged = true;
                Shared.EmoteInfo = sb.ToString();
                Cfg.Log.LogInfo("Available emotes (slot/item): " + Shared.EmoteInfo);
            }

            var pairs = new List<KeyValuePair<string, string>>();
            string arm = "";
            foreach (var tag in tagString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] si;
                if (!TagMap.TryGetValue(tag, out si)) continue;
                if (!all.Contains(si[0] + "/" + si[1])) continue;
                pairs.Add(new KeyValuePair<string, string>(si[0], si[1]));
                if (si[0].StartsWith("Arm")) arm = si[0];
            }
            if (pairs.Count == 0)
            {
                if (tagString.Contains("neutral")) ci.SetDefaultEmote(false);
                return;
            }
            SetEmotes(ci, pairs);
            if (arm.Length > 0) { armSlot = arm; armResetAt = Time.time + 3f; }
            Shared.LastResult += " | " + tagString;
        }
        catch (Exception ex) { Shared.LastResult += " | emotion failed: " + ex.GetType().Name + ": " + ex.Message; }
    }

    // the game's main (story) dialogue box, not the small per-character bubble
    static SayDialog FindMainDialog()
    {
        try { var m = SayDialog.MainSayDialog; if (m != null) return m; } catch { }
        return null;
    }

    static SayDialog FindDialog()
    {
        try
        {
            SayDialog d;
            var cs = ConversationSingleton.Instance;
            if (cs != null && cs.BotLive2DCharacter != null && cs.BotLive2DCharacter.TryGetSayDialog(out d) && d != null) return d;
        }
        catch { }
        try { if (SayDialog.ActiveSayDialog != null) return SayDialog.ActiveSayDialog; } catch { }
        try { return SayDialog.GetSayDialog(); } catch { }
        return null;
    }

    static void ShowReply(string reply)
    {
        CloseWaiting();
        if (greetPlain && StoryBusy)      // the greeting was asked for just before a story started; its answer arrived in the middle of the story (log, v5.17b)
        {
            greetPlain = false;
            Cfg.Log.LogInfo("Greeting: dropped, a story started while she was thinking of it");
            return;
        }
        string emotion, gest, clean;
        bool rawError = Regex.IsMatch((reply ?? "").TrimStart(), @"^\[(error|no reply|busy)\]");
        ParseTags(reply, out emotion, out gest, out clean);
        if (!rawError) StoryMemory.Add(Shared.Current.Bot + " answered: " + clean);
        Shared.LastResult = "";
        if (Cfg.Actions.Value)
        {
            foreach (var w in emotion.Split(' '))
            {
                if (w == "feed" || w == "rest" || w == "heal" || w == "comfort")
                {
                    string done = GameLink.DoAction(w);
                    if (done != null) { Cfg.Log.LogInfo("Action by Jun: " + w); break; }
                }
            }
        }

        bool isError = rawError;
        bool plain = greetPlain; greetPlain = false;
        if (plain)
        {
            Cfg.Log.LogInfo("Greeting: reply received (" + (isError ? "error: " + clean : clean.Length + " chars") + ")");
            // greeting: normal dialogue box; if the model is not reachable stay silent instead of popping up an error
            if (!isError) { bool ok = SpeakAsBot(clean, emotion, gest, true); Cfg.Log.LogInfo("Greeting: shown=" + ok + " " + Shared.LastResult); }
            else Cfg.Log.LogInfo("Greeting skipped: " + clean);
            return;
        }
        if (!isError && Cfg.ReplyInBubble.Value && SpeakAsBot(clean, emotion, gest)) return;

        // Fallback (errors, or she is not on screen): a game popup window.
        var ui = UiOverlay.Instance;
        if (ui == null) return;
        string bot = Shared.Current.Bot;
        var again = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(OpenInput));
        var choices = new Il2CppSystem.Collections.Generic.List<PopupChoice>();
        var replyBtn = new PopupChoice("Reply");
        replyBtn.Action = again;
        choices.Add(replyBtn);
        choices.Add(new PopupChoice("Close"));
        string text = (lastUser.Length > 0 ? "You: " + lastUser + "\n\n" : "") + bot + ": " + clean;
        ui.Popup(bot, text, choices);
        Shared.LastResult = "Reply shown in a popup. (" + Shared.LastResult + ")";
    }

    static bool SpeakAsBot(string clean, string emotion, string gest, bool plainBox = false)
    {
        try
        {
            var ci = Controller();
            if (ci == null) { Shared.LastResult = "Bubble skipped: no bot controller (is she on screen?)"; return false; }
            clean = clean.Replace("<", "").Replace(">", "").Replace("\r", " ").Replace("\n", " ").Trim();
            if (clean.Length == 0) return false;

            string bot = Shared.Current.Bot;
            if (string.IsNullOrEmpty(bot)) bot = "Jun";

            fadeStart = -1f;
            try { if (fadeGroup != null) fadeGroup.alpha = 1f; } catch { }
            var dialog = plainBox ? (FindMainDialog() ?? FindDialog()) : FindDialog();
            if (plainBox) Cfg.Log.LogInfo("Greeting: using dialogue box " + (dialog != null ? dialog.name : "none") + (FindMainDialog() != null ? " (main)" : " (fallback)"));
            bool shown = false;
            if (dialog != null)
            {
                dialog.gameObject.SetActive(true);
                try
                {
                    // making the bot the speaker lets the game's own movement script place the box beside her
                    var cs = ConversationSingleton.Instance;
                    if (plainBox) dialog.SetCharacter(null);      // no speaker attached: the game's normal dialogue box position
                    else if (cs != null && cs.BotLive2DCharacter != null)
                        dialog.SetCharacter(cs.BotLive2DCharacter.Cast<Fungus.ISayDialogCharacter>());
                }
                catch (Exception ex) { Shared.LastResult += " | attach failed: " + ex.GetType().Name; }
                try { dialog.SetCharacterName(bot, Color.white); } catch { }
                if (plainBox)
                {
                    // greeting: zoom in, wait for the player's click, then zoom out again
                    if (Cfg.AutoZoom.Value) ZoomIn();
                    greetClosed = false; greetActive = true; greetShownAt = Time.unscaledTime;
                    greetDone = DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(new Action(() => { greetClosed = true; }));
                    greetDialog = dialog;
                    dialog.Say(clean, true, true, true, true, greetDone);
                }
                else
                {
                    // fadeWhenDone = false: the message stays on screen instead of fading
                    dialog.Say(clean, true, true, false, false, null);
                }
                lastDialog = dialog;
                shown = true;
                TraceStart(plainBox ? "greeting" : "reply");
            }
            else
            {
                ModelBrain brain;
                if (ModelBrain.TryGet("bot", out brain) && brain != null && brain.CurrentState != null && !brain.ShouldBlockDialogue())
                {
                    brain.StartSafeCoroutine(brain.CurrentState.Say(clean, false, null, false));
                    shown = true;
                }
            }
            if (!shown) { Shared.LastResult = "Bubble skipped: no speech box found"; return false; }

            speakText = clean; spoken = 0; speakStart = Time.time;
            float secs = clean.Length / Cfg.SpeakSpeed.Value;
            bubbleText = plainBox ? "" : clean; lastSayTime = Time.time; restores = 0; keepSeen = false;
            bubbleUntil = Cfg.BubbleSeconds.Value > 0 ? Time.time + secs + Cfg.BubbleSeconds.Value : float.MaxValue;
            emoteResetAt = Time.time + secs + 6f;
            hideAt = (!plainBox && Cfg.BubbleSeconds.Value > 0) ? Time.time + secs + Cfg.BubbleSeconds.Value : -1f;
            Shared.LastResult = "Reply spoken by the bot.";
            ApplyEmotion(ci, emotion);
            return true;
        }
        catch (Exception ex)
        {
            Shared.LastResult = "Bubble failed: " + ex.GetType().Name + ": " + ex.Message;
            Cfg.Log.LogError(ex.ToString());
            return false;
        }
    }

    static void TickSpeech()
    {
        float now = Time.time;
        if (speakStart >= 0f)
        {
            try
            {
                var ci = Controller();
                var mouth = ci != null ? ci.Mouth : null;
                if (mouth != null)
                {
                    int target = Math.Min(speakText.Length, (int)((now - speakStart) * Cfg.SpeakSpeed.Value));
                    while (spoken < target) { mouth.SayLetter(speakText[spoken]); spoken++; }
                    if (spoken >= speakText.Length) { mouth.SayEnd(); speakStart = -1f; }
                }
                else speakStart = -1f;
            }
            catch { speakStart = -1f; }
        }
        if (armResetAt > 0f && now > armResetAt)
        {
            armResetAt = -1f;
            try
            {
                var ci = Controller();
                if (ci != null && armSlot.Length > 0)
                    SetEmotes(ci, new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(armSlot, "DownNormal") });
            }
            catch { }
        }
        if (emoteResetAt > 0f && now > emoteResetAt)
        {
            emoteResetAt = -1f;
            try { var ci = Controller(); if (ci != null) ci.SetDefaultEmote(false); } catch { }
        }
        if (hideAt > 0f && now > hideAt)
        {
            hideAt = -1f;
            FadeOldBubble();
        }
    }
}

// Saves every exchange to BepInEx\chat_log.txt (plain .NET only).
// Writes down what actually happens in the game (every line the game's dialogue box shows, with day/time),
// plus your chats with Jun, so she can remember it. Lives in BepInEx\jun_memory.txt.
// ===================== custom stories =====================
// A story is a plain text file in BepInEx\jun_stories\<name>.story, written in a tiny script language:
//   title: My story
//   # scene_name
//   Jun [happy][wave]: Hello!          (Name [tags]: text)
//   Anon: Hi.
//   Narrator: Something happens.
//   ? A choice -> other_scene
//   -> next_scene   (or  -> end)
public class StoryLine { public string Who = "", Tags = "", Text = "", Cmd = ""; }
public class StoryChoice { public string Text = "", Goto = ""; }
public class StoryScene
{
    public string Name = "";
    public List<StoryLine> Lines = new List<StoryLine>();
    public List<StoryChoice> Choices = new List<StoryChoice>();
    public string Next = "";
}
public class StoryData
{
    public string Id = "", Title = "";
    public string Start = "";
    public Dictionary<string, string> Colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, StoryScene> Scenes = new Dictionary<string, StoryScene>(StringComparer.OrdinalIgnoreCase);
}

public static class StoryScript
{
    public static string Dir
    {
        get
        {
            string d = System.IO.Path.Combine(Host.RootPath, "jun_stories");
            try { if (!Directory.Exists(d)) { Directory.CreateDirectory(d); File.WriteAllText(System.IO.Path.Combine(d, "example.story"), Example, Encoding.UTF8); } } catch { }
            return d;
        }
    }

    // positions on screen: left, center, right, or a number in "spreads" (-1 = left, 0 = centre, 1 = right, -0.5 = halfway to the left)
    public static bool ParsePos(string t, out float slots)
    {
        slots = 0f; t = (t ?? "").Trim().ToLowerInvariant().Replace(',', '.');
        if (t == "left") { slots = -1f; return true; }
        if (t == "right") { slots = 1f; return true; }
        if (t == "center" || t == "centre" || t == "middle") { slots = 0f; return true; }
        return float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out slots) && slots > -4f && slots < 4f;
    }

    // size of a portrait: a multiplier on the game's own size (1 = as the game draws it)
    public static bool ParseSize(string t, out float v)
    {
        v = 1f; t = (t ?? "").Trim().Replace(',', '.');
        return float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) && v >= 0.1f && v <= 3f;
    }

    // height offset of a portrait in screen units (1 = about 100 px at 1080p; negative = lower)
    public static bool ParseHeight(string t, out float v)
    {
        v = 0f; t = (t ?? "").Trim().Replace(',', '.');
        return float.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) && v >= -3f && v <= 3f;
    }

    public const string Example =
"title: A quiet evening\n" +
"// Lines starting with // are notes. Scenes start with #. The first scene is where the story begins.\n" +
"# start\n" +
"Narrator: The room is quiet. {bot} looks up from the couch.\n" +
"{bot} [happy]: You're back. I was running a few calculations while waiting.\n" +
"{player}: What kind of calculations?\n" +
"{bot} [shy]: ...How many minutes until you got home. Please do not make it weird.\n" +
"? Tease her -> tease\n" +
"? Say you missed her -> missed\n" +
"# tease\n" +
"{player}: So you were counting. Cute.\n" +
"{bot} [angry]: I was not being cute. That was a precise measurement.\n" +
"-> end\n" +
"# missed\n" +
"{player}: I missed you too.\n" +
"{bot} [veryshy]: Oh. {{pause}} My sensors say my face is warm. That is an error. Probably.\n" +
"-> end\n";

    public static string SafeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return "";
        var sb = new StringBuilder();
        foreach (char c in id.Trim()) if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c); else if (c == ' ') sb.Append('_');
        string r = sb.ToString();
        return r.Length > 120 ? r.Substring(0, 120) : r;
    }

    static string PathFor(string id) { return System.IO.Path.Combine(Dir, SafeId(id) + ".story"); }

    public static List<KeyValuePair<string, string>> ListAll()
    {
        var list = new List<KeyValuePair<string, string>>();
        try
        {
            foreach (var f in Directory.GetFiles(Dir, "*.story"))
            {
                string id = System.IO.Path.GetFileNameWithoutExtension(f);
                string title = id;
                try
                {
                    foreach (var l in File.ReadLines(f, Encoding.UTF8))
                    {
                        string t = l.Trim();
                        if (t.StartsWith("title:", StringComparison.OrdinalIgnoreCase)) { title = t.Substring(6).Trim(); break; }
                        if (t.StartsWith("#")) break;
                    }
                }
                catch { }
                list.Add(new KeyValuePair<string, string>(id, title.Length > 0 ? title : id));
            }
        }
        catch { }
        list.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
        try
        {
            foreach (var p in StoryPacks.List())      // stories from .storypack files come after your own
                foreach (var s in p.stories) list.Add(new KeyValuePair<string, string>(s.id, s.title + "  [" + p.name + "]"));
        }
        catch { }
        return list;
    }

    public static string ReadText(string id)
    {
        if (StoryPacks.IsPackId(id)) return StoryPacks.ReadStory(id);
        try { string f = PathFor(id); return File.Exists(f) ? File.ReadAllText(f, Encoding.UTF8) : null; } catch { return null; }
    }
    // saving a story that came from a pack makes your own copy under its plain name; the pack is never changed
    public static void WriteText(string id, string text) { if (StoryPacks.IsPackId(id)) id = StoryPacks.OwnCopyId(id); File.WriteAllText(PathFor(id), text ?? "", Encoding.UTF8); }
    public static bool Delete(string id) { if (StoryPacks.IsPackId(id)) return false; try { string f = PathFor(id); if (File.Exists(f)) { File.Delete(f); return true; } } catch { } return false; }

    // @minigame skillcheck [difficulty] [scene>=0.9 scene>=0.5 ... fallbackScene]   (v5.20)
    // type: skillcheck (the game's timing bar) or work (the work clicking game). difficulty: optional number from 0 to 1.
    // After it, the story jumps to the first scene whose minimum the score reaches, else to the last plain scene name; with no scenes it just goes on.
    public static string CheckMinigame(string cmd)
    {
        var p = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 2) return "@minigame needs a type, like  @minigame skillcheck 0.6 strike>=0.9 spare>=0.5 gutter";
        string ty = p[1].ToLowerInvariant();
        if (ty != "skillcheck" && ty != "work") return "@minigame type must be skillcheck or work (got '" + p[1] + "')";
        int plain = 0;
        for (int i = 2; i < p.Length; i++)
        {
            float f;
            if (i == 2 && float.TryParse(p[i].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
            { if (f < 0f || f > 20f) return "@minigame difficulty must be between 0 and 20 (got '" + p[i] + "')"; continue; }
            int ge = p[i].IndexOf(">=", StringComparison.Ordinal);
            if (ge >= 0)
            {
                if (ge == 0 || !float.TryParse(p[i].Substring(ge + 2).Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
                    return "@minigame target must look like  scene>=0.7  (got '" + p[i] + "')";
                if (plain > 0) return "@minigame: the plain fallback scene must come last (got '" + p[i] + "' after it)";
            }
            else if (++plain > 1) return "@minigame can have only one fallback scene without >= (got '" + p[i] + "')";
        }
        return null;
    }

    // @total win>=20 close>=15 lose : jump by {total} (pins added up since the story started or since @score reset)
    public static string CheckTotal(string cmd)
    {
        var p = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 2) return "@total needs targets, like  @total win>=20 lose";
        int plain = 0;
        for (int i = 1; i < p.Length; i++)
        {
            int ge = p[i].IndexOf(">=", StringComparison.Ordinal); int v;
            if (ge >= 0)
            {
                if (ge == 0 || !int.TryParse(p[i].Substring(ge + 2), out v)) return "@total target must look like  scene>=20  (got '" + p[i] + "')";
                if (plain > 0) return "@total: the plain fallback scene must come last";
            }
            else if (++plain > 1) return "@total can have only one fallback scene without >=";
        }
        return null;
    }
    public static List<KeyValuePair<string, float>> TotalTargets(string cmd)
    {
        var r = new List<KeyValuePair<string, float>>();
        var p = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < p.Length; i++)
        {
            int ge = p[i].IndexOf(">=", StringComparison.Ordinal); int v;
            if (ge > 0 && int.TryParse(p[i].Substring(ge + 2), out v)) r.Add(new KeyValuePair<string, float>(p[i].Substring(0, ge), v));
            else if (ge < 0) r.Add(new KeyValuePair<string, float>(p[i], -1f));
        }
        return r;
    }

    // (scene, minimum score); the fallback scene has minimum -1
    public static List<KeyValuePair<string, float>> MinigameTargets(string cmd)
    {
        var r = new List<KeyValuePair<string, float>>();
        var p = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 2; i < p.Length; i++)
        {
            float f;
            int ge = p[i].IndexOf(">=", StringComparison.Ordinal);
            if (ge > 0 && float.TryParse(p[i].Substring(ge + 2).Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) r.Add(new KeyValuePair<string, float>(p[i].Substring(0, ge), f));
            else if (ge < 0 && !(i == 2 && float.TryParse(p[i].Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))) r.Add(new KeyValuePair<string, float>(p[i], -1f));
        }
        return r;
    }

    static string NormTags(string raw)
    {
        var words = new List<string>();
        foreach (Match m in Regex.Matches(raw ?? "", @"\[([^\]]*)\]"))
        {
            string w = Regex.Replace(m.Groups[1].Value.ToLowerInvariant(), "[^a-z]", "");
            if (w.Length > 0) words.Add(w);
        }
        return string.Join(" ", words);
    }

    public static StoryData Parse(string id, string text, List<string> errors)
    {
        var st = new StoryData { Id = id, Title = id };
        StoryScene cur = null;
        int n = 0;
        foreach (var raw in (text ?? "").Replace("\r", "").Split('\n'))
        {
            n++;
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//")) continue;
            if (cur == null && line.StartsWith("title:", StringComparison.OrdinalIgnoreCase)) { st.Title = line.Substring(6).Trim(); continue; }
            if (line.StartsWith("color:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("colour:", StringComparison.OrdinalIgnoreCase))
            {
                string rest = line.Substring(line.IndexOf(':') + 1);
                int eq = rest.IndexOf('=');
                string cn = eq > 0 ? rest.Substring(0, eq).Trim() : "", hx = eq > 0 ? rest.Substring(eq + 1).Trim() : "";
                if (cn.Length == 0 || !Regex.IsMatch(hx, "^#[0-9a-fA-F]{6}$")) { errors.Add("line " + n + ": use  color: Name = #rrggbb   (for example  color: Hobo = #c08040)"); continue; }
                st.Colors[cn] = hx; continue;
            }
            if (line.StartsWith("#"))
            {
                string name = line.TrimStart('#').Trim();
                if (name.Length == 0) { errors.Add("line " + n + ": a scene needs a name, like  # start"); continue; }
                if (st.Scenes.ContainsKey(name)) { errors.Add("line " + n + ": scene '" + name + "' already exists"); cur = st.Scenes[name]; continue; }
                cur = new StoryScene { Name = name };
                st.Scenes[name] = cur;
                if (st.Start.Length == 0) st.Start = name;
                continue;
            }
            if (cur == null) { cur = new StoryScene { Name = "start" }; st.Scenes["start"] = cur; st.Start = "start"; }
            if (line.StartsWith("->"))
            {
                cur.Next = line.Substring(2).Trim();
                if (cur.Next.Length == 0) errors.Add("line " + n + ": '->' needs a scene name or 'end'");
                continue;
            }
            if (line.StartsWith("?"))
            {
                int arrow = line.LastIndexOf("->", StringComparison.Ordinal);
                if (arrow < 0) { errors.Add("line " + n + ": a choice needs  ? text -> scene"); continue; }
                string ct = line.Substring(1, arrow - 1).Trim(), tg = line.Substring(arrow + 2).Trim();
                if (ct.Length == 0 || tg.Length == 0) { errors.Add("line " + n + ": a choice needs text and a target"); continue; }
                cur.Choices.Add(new StoryChoice { Text = ct, Goto = tg });
                continue;
            }
            if (line.StartsWith("@"))
            {
                string cmd = line.Substring(1).Trim();
                string verb = cmd.Split(' ')[0].ToLowerInvariant();
                int np = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (verb == "bg") verb = "background";
                if (verb == "background")
                {
                    string bn = np > 1 ? Regex.Replace(cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1].ToLowerInvariant(), "[^a-z0-9]", "") : "";
                    if (Array.IndexOf(InGameChat.BackgroundNames, bn) < 0 && !InGameChat.CustomBgExists(bn) && !InGameChat.IsExportable(bn)) { errors.Add("line " + n + ": unknown background '" + bn + "' (use one of: " + string.Join(", ", InGameChat.BackgroundNames) + ", or put your own picture in jun_backgrounds as " + bn + ".png)"); continue; }
                    cur.Lines.Add(new StoryLine { Cmd = "background " + bn });
                    continue;
                }
                if (verb == "music")
                {
                    if (np < 2) { errors.Add("line " + n + ": @music needs a track or category, like  @music Rainy Day  (or @music stop, @music auto)"); continue; }
                    cur.Lines.Add(new StoryLine { Cmd = "music " + cmd.Substring(5).Trim() });
                    continue;
                }
                if (verb == "jun")
                {
                    var jt = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string ja = np > 1 ? jt[1].ToLowerInvariant() : "";
                    float jsl, jzz;
                    if (ja != "hide" && !ParsePos(ja, out jsl)) { errors.Add("line " + n + ": @jun needs left, center, right, a number like -1.4, or hide"); continue; }
                    if (jt.Length > 2 && !ParseSize(jt[2], out jzz)) { errors.Add("line " + n + ": @jun size must be a number between 0.1 and 3, like  @jun -1 1.4 (got '" + jt[2] + "')"); continue; }
                    float jhh; if (jt.Length > 3 && !ParseHeight(jt[3], out jhh)) { errors.Add("line " + n + ": @jun height must be a number between -3 and 3, like  @jun -1 1.4 -0.3 (got '" + jt[3] + "')"); continue; }
                    cur.Lines.Add(new StoryLine { Cmd = "jun " + ja + (jt.Length > 2 && ja != "hide" ? " " + jt[2] : "") + (jt.Length > 3 && ja != "hide" ? " " + jt[3] : "") });
                    continue;
                }
                if (verb == "score")
                {
                    var sp0 = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (sp0.Length != 2 || !sp0[1].Equals("reset", StringComparison.OrdinalIgnoreCase)) { errors.Add("line " + n + ": use  @score reset  (sets {total} back to 0, for a new match)"); continue; }
                    cur.Lines.Add(new StoryLine { Cmd = "score reset" });
                    continue;
                }
                if (verb == "total")
                {
                    string tErr = CheckTotal(cmd);
                    if (tErr != null) { errors.Add("line " + n + ": " + tErr); continue; }
                    cur.Lines.Add(new StoryLine { Cmd = "total " + string.Join(" ", cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), 1, np - 1) });
                    continue;
                }
                if (verb == "minigame" || verb == "game")
                {
                    string mgErr = CheckMinigame(cmd);
                    if (mgErr != null) { errors.Add("line " + n + ": " + mgErr); continue; }
                    cur.Lines.Add(new StoryLine { Cmd = "minigame " + string.Join(" ", cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), 1, np - 1) });
                    continue;
                }
                if (verb != "show" && verb != "hide" && verb != "face" && verb != "outfit" && verb != "move" && verb != "size" && verb != "slide" && verb != "stop") { errors.Add("line " + n + ": unknown command '@" + verb + "' (use @show, @hide, @move, @slide, @stop, @size, @face, @outfit, @background, @music, @jun, @minigame)"); continue; }
                if (verb == "slide")
                {
                    var sp2 = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); float ssl2;
                    if (sp2.Length < 3) { errors.Add("line " + n + ": @slide needs a character and a position, like  @slide Melissa -1.4 6   (6 = seconds, optional)"); continue; }
                    if (!ParsePos(sp2[2], out ssl2)) { errors.Add("line " + n + ": @slide position must be left, center, right or a number like -1.4 (got '" + sp2[2] + "')"); continue; }
                }
                if (verb == "stop" && np < 2) { errors.Add("line " + n + ": @stop needs a character, like  @stop Melissa  (or @stop all)"); continue; }
                if (verb == "size")
                {
                    var zp = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); float zv;
                    if (zp.Length < 3 || !ParseSize(zp[2], out zv)) { errors.Add("line " + n + ": @size needs a character and a number between 0.1 and 3, like  @size Annalie 0.8"); continue; }
                }
                if (verb == "move")
                {
                    var mp = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); float msl0;
                    if (mp.Length < 3) { errors.Add("line " + n + ": @move needs a character and a position, like  @move Annalie 0.3"); continue; }
                    if (!ParsePos(mp[2], out msl0)) { errors.Add("line " + n + ": @move position must be left, center, right or a number like -0.5 (got '" + mp[2] + "')"); continue; }
                    float myh; if (mp.Length > 3 && !ParseHeight(mp[3], out myh)) { errors.Add("line " + n + ": @move height must be a number between -3 and 3, like -0.5 (got '" + mp[3] + "')"); continue; }
                }
                if (verb == "show" && np < 2) { errors.Add("line " + n + ": @show needs a character, like  @show Annalie center"); continue; }
                if (verb == "show" && np >= 3)
                {
                    float ssl; string sp = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[2];
                    if (!ParsePos(sp, out ssl)) { errors.Add("line " + n + ": @show position must be left, center, right or a number like -0.5 (got '" + sp + "')"); continue; }
                    var shp = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); float shz;
                    if (shp.Length > 3 && !ParseSize(shp[3], out shz)) { errors.Add("line " + n + ": @show size must be a number between 0.1 and 3, like  @show Annalie center 0.8 (got '" + shp[3] + "')"); continue; }
                    float shh; if (shp.Length > 4 && !ParseHeight(shp[4], out shh)) { errors.Add("line " + n + ": @show height must be a number between -3 and 3, like  @show Annalie center 0.8 -0.5 (got '" + shp[4] + "')"); continue; }
                }
                if ((verb == "face" || verb == "outfit") && np < 3) { errors.Add("line " + n + ": @" + verb + " needs a character and a name, like  @" + (verb == "face" ? "face Annalie sad" : "outfit Annalie Halloween_Outfit")); continue; }
                cur.Lines.Add(new StoryLine { Cmd = cmd });
                continue;
            }
            int colon = line.IndexOf(':');
            if (colon <= 0) { errors.Add("line " + n + ": expected  Name: text   (or ?, ->, #)"); continue; }
            string left = line.Substring(0, colon), body = line.Substring(colon + 1).Trim();
            string who = Regex.Replace(left, @"\[[^\]]*\]", "").Trim();
            if (body.Length == 0) { errors.Add("line " + n + ": empty text"); continue; }
            cur.Lines.Add(new StoryLine { Who = who, Tags = NormTags(left), Text = body });
        }
        foreach (var sc0 in st.Scenes.Values)
            foreach (var ln0 in sc0.Lines)
                if (ln0.Cmd.StartsWith("minigame ") || ln0.Cmd.StartsWith("total "))
                    foreach (var tg0 in (ln0.Cmd.StartsWith("total ") ? TotalTargets(ln0.Cmd) : MinigameTargets(ln0.Cmd)))
                        if (!tg0.Key.Equals("end", StringComparison.OrdinalIgnoreCase) && !st.Scenes.ContainsKey(tg0.Key)) errors.Add("@minigame in scene '" + sc0.Name + "' jumps to a scene that does not exist: " + tg0.Key);
        if (st.Scenes.Count == 0) errors.Add("the story is empty: add a scene with  # start  and some lines");
        foreach (var sc in st.Scenes.Values)
        {
            if (sc.Next.Length > 0 && !sc.Next.Equals("end", StringComparison.OrdinalIgnoreCase) && !st.Scenes.ContainsKey(sc.Next))
                errors.Add("scene '" + sc.Name + "': goes to '" + sc.Next + "' which does not exist");
            foreach (var c in sc.Choices)
                if (!c.Goto.Equals("end", StringComparison.OrdinalIgnoreCase) && !st.Scenes.ContainsKey(c.Goto))
                    errors.Add("scene '" + sc.Name + "': choice '" + c.Text + "' goes to '" + c.Goto + "' which does not exist");
            if (sc.Lines.Count == 0 && sc.Choices.Count == 0 && sc.Next.Length == 0)
                errors.Add("scene '" + sc.Name + "' is empty");
        }
        return st;
    }
}

// F6: writes BepInEx\\mdrg_assets.txt, a list of what the running game has loaded (scene objects, sprites, character/background
// components). Used to find the real names of backgrounds and characters for the story builder. Changes nothing in the game.
static class AssetDump
{
    public static volatile bool Requested, CharsRequested;
    static string charsPath;
    static void W(string line)   // written line by line, so if the game dies half way the file still has everything before it
    {
        try { File.AppendAllText(charsPath, line + "\r\n", Encoding.UTF8); } catch { }
    }
    static string PathOf(Transform t)
    {
        string r = ""; int n = 0;
        try { while (t != null && n < 8) { r = t.name + (r.Length > 0 ? "/" : "") + r; t = t.parent; n++; } } catch { }
        return r;
    }
    // fonts the game has loaded (to find out which one its UI uses)
    static void Fonts()
    {
        W("== fonts loaded in the game (name) ==");
        try
        {
            var seen = new HashSet<string>();
            foreach (var f in Resources.FindObjectsOfTypeAll<Font>()) { try { if (seen.Add(f.name)) W("  " + f.name); } catch { } }
        }
        catch (Exception ex) { W("fonts failed: " + ex.Message); }
    }

    // everything big that is drawn right now (backgrounds, CG pictures, full-screen UI images): press F7 while a scene is on screen to find out what its background is called
    static void BigPictures()
    {
        W("== big pictures on screen right now (name | sprite | where) ==");
        try
        {
            foreach (var r in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
            {
                try
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy || r.sprite == null) continue;
                    var b = r.bounds; if (b.size.x < 8f) continue;
                    { string sv = InGameChat.SaveBigSprite(r); if (sv.Length > 0) W(sv); }
                    W("  SpriteRenderer " + r.name + " | " + r.sprite.name + " | size=" + b.size.x.ToString("0.0") + "x" + b.size.y.ToString("0.0") + " pos=(" + b.center.x.ToString("0.0") + ", " + b.center.y.ToString("0.0") + ") layer=" + r.sortingLayerName + "/" + r.sortingOrder + " | " + PathOf(r.transform));
                }
                catch { }
            }
        }
        catch (Exception ex) { W("sprites failed: " + ex.Message); }
        try
        {
            foreach (var im in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>())
            {
                try
                {
                    if (!im.enabled || !im.gameObject.activeInHierarchy || im.sprite == null) continue;
                    var rc = im.rectTransform.rect; if (rc.width < 1000f) continue;
                    W("  UI Image " + im.name + " | " + im.sprite.name + " | " + rc.width.ToString("0") + "x" + rc.height.ToString("0") + " | " + PathOf(im.transform));
                }
                catch { }
            }
            foreach (var im in Resources.FindObjectsOfTypeAll<UnityEngine.UI.RawImage>())
            {
                try
                {
                    if (!im.enabled || !im.gameObject.activeInHierarchy || im.texture == null) continue;
                    var rc = im.rectTransform.rect; if (rc.width < 1000f) continue;
                    W("  UI RawImage " + im.name + " | " + im.texture.name + " | " + rc.width.ToString("0") + "x" + rc.height.ToString("0") + " | " + PathOf(im.transform));
                }
                catch { }
            }
        }
        catch (Exception ex) { W("ui images failed: " + ex.Message); }
    }
    static void Chars()
    {
        charsPath = System.IO.Path.Combine(Host.RootPath, "mdrg_characters.txt");
        try { File.WriteAllText(charsPath, "MDRG character list " + DateTime.Now + "\r\n", Encoding.UTF8); } catch { }
        W("== NormalCharacters (speaker names used by dialogue) ==");
        try
        {
            var cs = ConversationSingleton.Instance;
            W("ConversationSingleton: " + (cs == null ? "null" : "ok"));
            if (cs != null)
            {
                int n = cs.NormalCharacters.Count;
                W("count " + n);
                for (int i = 0; i < n; i++)
                {
                    string id = "?", nm = "?";
                    try { id = cs.NormalCharacters[i].Id; } catch { }
                    try { nm = cs.NormalCharacters[i].NameText; } catch { }
                    string hx = "?";
                    try { var c = cs.NormalCharacters[i].NameColor; hx = "#" + ((int)Mathf.Round(c.r * 255f)).ToString("x2") + ((int)Mathf.Round(c.g * 255f)).ToString("x2") + ((int)Mathf.Round(c.b * 255f)).ToString("x2"); } catch { }
                    W("  " + id + "  |  " + nm + "  |  " + hx);
                }
            }
        }
        catch (Exception ex) { W("ERROR " + ex.Message); }
        W("== SimpleCharacter ids (portrait characters that can be shown on screen) ==");
        try
        {
            var sc = SimpleCharacterSingleton.Instance;
            W("SimpleCharacterSingleton: " + (sc == null ? "null" : "ok"));
            if (sc != null)
            {
                var keys = sc.runtimeCharacterReferences.keys;
                int n = keys.Count;
                W("count " + n);
                for (int i = 0; i < n; i++) W("  " + keys[i]);
                try
                {
                    W("active now: " + sc._activeCharacters.Count);
                    for (int i = 0; i < sc._activeCharacters.Count; i++) W("  " + InGameChat.DescribeChar(sc._activeCharacters[i]));
                }
                catch (Exception ex) { W("active list failed: " + ex.Message); }
            }
        }
        catch (Exception ex) { W("ERROR " + ex.Message); }
        Fonts();
        BigPictures();
        W("== done ==");
        Shared.LastResult = "Character list written to " + charsPath;
        Cfg.Log.LogInfo("Character list written: " + charsPath);
    }
    public static void Tick()
    {
        if (CharsRequested) { CharsRequested = false; Chars(); }
        if (!Requested) return;
        Requested = false;
        var sb = new StringBuilder();
        try
        {
            sb.AppendLine("MDRG asset dump " + DateTime.Now);
            sb.AppendLine("\n== ROOT OBJECTS (depth 3) ==");
            var ts = Resources.FindObjectsOfTypeAll<Transform>();
            int roots = 0;
            foreach (var t in ts)
            {
                try { if (t.parent != null) continue; roots++; Walk(sb, t, 0); } catch { }
            }
            sb.AppendLine("(" + roots + " roots)");
            sb.AppendLine("\n== COMPONENT TYPES (Live2D / character / background / scene / story related), with object names ==");
            var mbs = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            var seen = new HashSet<string>();
            foreach (var m in mbs)
            {
                try
                {
                    string tn = m.GetIl2CppType().FullName;
                    string l = tn.ToLowerInvariant();
                    if (!(l.Contains("live2d") || l.Contains("character") || l.Contains("backgr") || l.Contains("scene") || l.Contains("room")
                        || l.Contains("location") || l.Contains("story") || l.Contains("npc") || l.Contains("bot"))) continue;
                    string line = tn + "  <-  " + m.gameObject.name;
                    if (seen.Add(line)) sb.AppendLine(line);
                    if (seen.Count > 1500) break;
                }
                catch { }
            }
            sb.AppendLine("\n== SPRITES (name WxH) ==");
            var sp = Resources.FindObjectsOfTypeAll<Sprite>();
            var names = new List<string>();
            foreach (var x in sp) { try { names.Add(x.name + " " + (int)x.rect.width + "x" + (int)x.rect.height); } catch { } }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var n in names.Count > 6000 ? names.GetRange(0, 6000) : names) sb.AppendLine(n);
            sb.AppendLine("(" + names.Count + " sprites)");
            sb.AppendLine("\n== TEXTURES (name WxH) ==");
            var tx = Resources.FindObjectsOfTypeAll<Texture2D>();
            var tn2 = new List<string>();
            foreach (var x in tx) { try { tn2.Add(x.name + " " + x.width + "x" + x.height); } catch { } }
            tn2.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var n in tn2.Count > 6000 ? tn2.GetRange(0, 6000) : tn2) sb.AppendLine(n);
            sb.AppendLine("(" + tn2.Count + " textures)");
        }
        catch (Exception ex) { sb.AppendLine("ERROR: " + ex); }
        try
        {
            string path = System.IO.Path.Combine(Host.RootPath, "mdrg_assets.txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Shared.LastResult = "Asset list written to " + path;
            Cfg.Log.LogInfo("Asset dump written: " + path);
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Asset dump failed: " + ex.Message); }
    }
    static void Walk(StringBuilder sb, Transform t, int d)
    {
        string comps = "";
        try
        {
            var cs = t.gameObject.GetComponents<Component>();
            var l = new List<string>();
            foreach (var c in cs) { try { string n = c.GetIl2CppType().Name; if (n != "Transform" && n != "RectTransform") l.Add(n); } catch { } }
            if (l.Count > 0) comps = "  [" + string.Join(", ", l.ToArray()) + "]";
        }
        catch { }
        bool on = false; try { on = t.gameObject.activeSelf; } catch { }
        sb.AppendLine(new string(' ', d * 2) + t.name + (on ? "" : " (off)") + comps);
        if (d >= 3) return;
        int n2 = 0; try { n2 = t.childCount; } catch { }
        for (int i = 0; i < n2 && i < 60; i++) { try { Walk(sb, t.GetChild(i), d + 1); } catch { } }
    }
}

public static class Characters
{
    static List<string[]> entries;

    static void Load()
    {
        entries = new List<string[]>();
        string text = null;
        try
        {
            string ext = System.IO.Path.Combine(Host.RootPath, "jun_characters.txt");
            if (File.Exists(ext)) text = File.ReadAllText(ext, Encoding.UTF8);
        }
        catch { }
        if (text == null)
        {
            try
            {
                var asm = typeof(Characters).Assembly;
                foreach (var n in asm.GetManifestResourceNames())
                {
                    if (!n.EndsWith("jun_characters.txt", StringComparison.OrdinalIgnoreCase)) continue;
                    using (var st = asm.GetManifestResourceStream(n))
                    using (var r = new StreamReader(st, Encoding.UTF8)) text = r.ReadToEnd();
                }
            }
            catch { }
        }
        if (text == null) return;
        foreach (var raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            var parts = line.Split(new[] { '|' }, 3);
            if (parts.Length < 3) continue;
            entries.Add(new[] { parts[0].Trim(), parts[1].Trim().ToLowerInvariant(), parts[2].Trim() });
        }
    }

    public static string For(string userText, string lastReply)
    {
        if (!Cfg.UseCharacters.Value) return "";
        if (entries == null) Load();
        if (entries.Count == 0) return "";
        string hay = ((userText ?? "") + " " + (lastReply ?? "")).ToLowerInvariant();
        var sb = new StringBuilder(" Who is who (use this to stay in character and to talk about people from your shared life; if asked about something not covered, say you are not sure): ");
        int n = 0;
        foreach (var e in entries)
        {
            bool hit = false;
            foreach (var k in e[1].Split(','))
            {
                string w = k.Trim();
                if (w.Length == 0) continue;
                if (w == "*" || hay.Contains(w)) { hit = true; break; }
            }
            if (!hit) continue;
            sb.Append(e[0]).Append(": ").Append(e[2]).Append(" ");
            if (++n >= 6 && e[0] != "Jun") break;
        }
        return n > 0 ? sb.ToString() : "";
    }
}

public static class StoryMemory
{
    static readonly object gate = new object();
    static readonly List<string> lines = new List<string>();
    static bool loaded;
    static string lastText = "";
    static float nextPoll;

    static string PathName { get { return System.IO.Path.Combine(Host.RootPath, "jun_memory.txt"); } }

    static void Load()
    {
        loaded = true;
        try
        {
            // story events imported from a save file (optional), placed before the live memory
            string imp = System.IO.Path.Combine(Host.RootPath, "jun_story_import.txt");
            if (File.Exists(imp)) foreach (var l in File.ReadAllLines(imp, Encoding.UTF8)) if (l.Length > 0) lines.Add(l);
            if (!File.Exists(PathName)) return;
            var all = File.ReadAllLines(PathName, Encoding.UTF8);
            int from = Math.Max(0, all.Length - 6000);
            for (int i = from; i < all.Length; i++) if (all[i].Length > 0) lines.Add(all[i]);
        }
        catch { }
    }

    static string Where()
    {
        string st = "";
        try { var g = GameScript.Instance.GameState; if (g != null) st = g.GetType().Name; } catch { }
        string sc = "";
        try { sc = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; } catch { }
        return (sc.Length > 0 ? sc : "") + (st.Length > 0 ? "/" + st : "");
    }

    public static void Add(string text)
    {
        if (!Cfg.RememberStory.Value || string.IsNullOrWhiteSpace(text)) return;
        var s = Shared.Current;
        string line = "Day " + s.Day + " " + (s.Hour >= 0 ? s.Hour + ":00" : "") + " [" + Where() + "] " + text.Replace("\r", " ").Replace("\n", " ").Trim();
        lock (gate)
        {
            if (!loaded) Load();
            lines.Add(line);
            if (lines.Count > 6000) lines.RemoveAt(0);
        }
        try { File.AppendAllText(PathName, line + Environment.NewLine, Encoding.UTF8); } catch { }
    }

    // called every frame on the main thread; own is the text of Jun's own chat bubble (not recorded twice)
    public static void Tick(string own)
    {
        if (!Cfg.RememberStory.Value) return;
        if (Time.unscaledTime < nextPoll) return;
        nextPoll = Time.unscaledTime + 0.4f;
        var d = SayDialog.ActiveSayDialog;
        if (d == null || !d.gameObject.activeInHierarchy) return;
        string text = d.CurrentStoryText;
        if (string.IsNullOrWhiteSpace(text) || text == lastText) return;
        lastText = text;
        if (!string.IsNullOrEmpty(own) && text == own) return;
        string who = "";
        try { who = d.NameText; } catch { }
        Add((string.IsNullOrEmpty(who) ? "Narration" : who) + ": " + text);
    }

    static readonly HashSet<string> stop = new HashSet<string> { "the","and","you","your","are","was","what","did","does","do","have","has","had","that","this","with","for","not","but","about","know","remember","tell","she","her","him","his","they","them","there","when","where","who","how","why","can","could","would","jun","i","me","my","a","an","of","to","in","on","is","it","we","us" };

    // lines from the whole history that share words with what you just said (so she can recall old events)
    static string Relevant(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText)) return "";
        var words = new HashSet<string>();
        foreach (Match m in Regex.Matches(userText.ToLowerInvariant(), @"[a-z0-9']{3,}"))
            if (!stop.Contains(m.Value)) words.Add(m.Value);
        if (words.Count == 0) return "";
        var scored = new List<KeyValuePair<int, int>>();
        for (int i = 0; i < lines.Count; i++)
        {
            string l = lines[i].ToLowerInvariant();
            int sc = 0;
            foreach (var w in words) if (l.Contains(w)) sc++;
            if (sc > 0) scored.Add(new KeyValuePair<int, int>(sc, i));
        }
        if (scored.Count == 0) return "";
        scored.Sort((a, b) => a.Key != b.Key ? b.Key.CompareTo(a.Key) : b.Value.CompareTo(a.Value));
        int take = Math.Min(Math.Max(1, Cfg.RecallLines.Value), scored.Count);
        var idx = new List<int>();
        for (int i = 0; i < take; i++) idx.Add(scored[i].Value);
        idx.Sort();
        var sb = new StringBuilder(" Older memories that match what the player just said (use them to answer; if nothing here covers it, say honestly you do not remember): ");
        foreach (int i in idx) sb.Append(lines[i]).Append(" | ");
        return sb.ToString();
    }

    public static string Recent(string userText = "")
    {
        if (!Cfg.RememberStory.Value) return "";
        lock (gate)
        {
            if (!loaded) Load();
            if (lines.Count == 0) return "";
            string rel = Relevant(userText);
            int n = Math.Max(1, Cfg.MemoryLines.Value);
            var sb = new StringBuilder(" Your memory of what really happened recently (oldest first). Treat it as things you lived through and remember it naturally: ");
            for (int i = Math.Max(0, lines.Count - n); i < lines.Count; i++) sb.Append(lines[i]).Append(" | ");
            return sb.ToString() + rel;
        }
    }
}

public static class ChatLog
{
    static readonly object gate = new object();
    public static string PathName
    {
        get { return System.IO.Path.Combine(Host.RootPath, "chat_log.txt"); }
    }

    public static void Append(string player, string userText, string bot, string reply)
    {
        try
        {
            string tags = "";
            var m = Regex.Match(reply, @"^\s*((?:\[[A-Za-z .\-_]+\]\s*)+)");
            if (m.Success)
            {
                tags = m.Groups[1].Value.Replace("][", ", ").Replace("[", "").Replace("]", "").Trim();
                reply = reply.Substring(m.Length).Trim();
            }
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sb = new StringBuilder();
            sb.Append('[').Append(now).Append("] ").Append(player).Append(": ").AppendLine(userText);
            sb.Append('[').Append(now).Append("] ").Append(bot).Append(": ").Append(reply);
            if (tags.Length > 0) sb.Append("  (").Append(tags).Append(')');
            sb.AppendLine().AppendLine();
            lock (gate) { File.AppendAllText(PathName, sb.ToString(), Encoding.UTF8); }
        }
        catch (Exception ex)
        {
            try { Cfg.Log.LogWarning("Could not write the chat log: " + ex.Message); } catch { }
        }
    }
}

// Plain .NET only from here down. Never touches Unity or game objects.
// Which web pages belong to which mod, and the small text changes each mod makes to the shared pages.
public static class Routes
{
    static readonly string[] common = { "/common.js", "/logo", "/icon", "/favicon.ico", "/fonts.css", "/gameui.css", "/gameui.js", "/settings", "/api/settings", "/api/settings/set", "/api/state" };
    static readonly string[] story = { "/stories", "/stage", "/packs", "/api/backgrounds", "/api/music", "/api/music/play", "/api/stories", "/api/story", "/api/story/check", "/api/story/delete", "/api/story/play", "/api/story/save" };
    static readonly string[] chat = { "/", "/diary", "/pet", "/pet/frame.png", "/api/chat", "/api/clear", "/api/cmd", "/api/history", "/api/diary", "/api/diary/write", "/api/pet/info", "/api/pet/react" };

    public static bool Allowed(string path)
    {
        if (Array.IndexOf(common, path) >= 0 || path.StartsWith("/fonts/")) return true;
        if (Mod.Stories) return Array.IndexOf(story, path) >= 0 || path.StartsWith("/api/packs") || path.StartsWith("/api/stage/") || path == "/";
        return Array.IndexOf(chat, path) >= 0;
    }

    public static string Elsewhere(string path)
    {
        return "<!doctype html><meta charset='utf-8'><title>" + Mod.Name + "</title><body style='font:16px system-ui;background:#16121c;color:#eee;padding:24px'>" +
               "<h2>This page is part of " + Mod.OtherName + "</h2><p>" + Mod.Name + " doesn't have <code>" + System.Net.WebUtility.HtmlEncode(path) + "</code>. " +
               Mod.OtherName + " (" + Mod.OtherWhat + ") is a separate mod. If you have it installed, open " +
               "<a style='color:#ff5a8a' href='http://127.0.0.1:" + Mod.OtherPort + path + "'>http://127.0.0.1:" + Mod.OtherPort + path + "</a>.</p>" +
               "<p><a style='color:#ff5a8a' href='/'>back to " + Mod.Name + "</a></p></body>";
    }

    public static string Page(string file) { return Fix(file, PanelServer.Embedded(file)); }

    public static string Fix(string file, string t)
    {
        if (t == null) return "";
        if (Mod.Stories)
        {
            t = t.Replace("<a href='/'>&larr; back to the panel</a>", "<a href='/packs'>story packs</a> &middot; <a href='/settings'>settings</a>")
                 .Replace("<a href=\"/stories\">story editor</a> <a href=\"/\">panel</a>", "<a href=\"/stories\">story editor</a> <a href=\"/settings\">settings</a>")
                 .Replace("<a href=\"/\">panel</a>", "<a href=\"/stories\">story editor</a>")
                 .Replace("Jun Link story editor", "Story Creator")
                 .Replace("Jun Link", "Story Creator");
        }
        else if (file == "panel.html")
        {
            t = t.Replace("<div class='row'><a href='/stories' style='color:#d6336c'>Story editor &rarr; write your own storylines</a></div>",
                          "<div class='row'><a href='http://127.0.0.1:" + Mod.OtherPort + "/stories' style='color:#d6336c'>Story Creator &rarr; custom stories (a separate mod, if you have it)</a></div>")
                 .Replace("<div class='row'><a href='/packs' style='color:#d6336c'>Story packs &rarr; install and share stories</a></div>\n", "")
                 .Replace("<div class='row'><a href='/packs' style='color:#d6336c'>Story packs &rarr; install and share stories</a></div>", "");
        }
        return t;
    }
}

public static class PanelServer
{
    static HttpListener listener;
    static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    public static int RunningPort = 8765;      // the port the server really listens on (a changed Port setting needs a restart)
    static readonly object gate = new object();
    static readonly List<(string role, string text)> history = new List<(string role, string text)>();
    static bool busy;
    static string page;

    public static void Start()
    {
        try
        {
            page = Routes.Fix("panel.html", LoadPage());
            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + Cfg.Port.Value + "/");
            listener.Start();
            RunningPort = Cfg.Port.Value;
            StoryPacks.BackgroundAvailable = n => Array.IndexOf(InGameChat.BackgroundNames, n) >= 0 || InGameChat.CustomBgExists(n);
            Cfg.Log.LogInfo(Mod.Name + " " + Mod.Version + " is running. Open http://127.0.0.1:" + Cfg.Port.Value + (Mod.Stories ? "/stories" : "") + " in your browser.");
            Task.Run(Loop);
        }
        catch (Exception ex)
        {
            Cfg.Log.LogError(Mod.Name + " could not start its web server: " + ex.Message + (Mod.Stories ? " (Story Creator uses port 8766 and Jun Link 8765; they must differ)" : ""));
        }
    }

    static byte[] ReadEmbeddedBytes(string file)
    {
        var asm = typeof(PanelServer).Assembly;
        foreach (var n in asm.GetManifestResourceNames())
        {
            if (!n.EndsWith(file, StringComparison.OrdinalIgnoreCase)) continue;
            using (var st = asm.GetManifestResourceStream(n))
            using (var ms = new MemoryStream()) { st.CopyTo(ms); return ms.ToArray(); }
        }
        return null;
    }

    public static string Embedded(string file) { return ReadEmbedded(file); }

    static string ReadEmbedded(string file)
    {
        var asm = typeof(PanelServer).Assembly;
        foreach (var n in asm.GetManifestResourceNames())
        {
            if (!n.EndsWith(file, StringComparison.OrdinalIgnoreCase)) continue;
            using (var st = asm.GetManifestResourceStream(n))
            using (var r = new StreamReader(st, Encoding.UTF8)) return r.ReadToEnd();
        }
        return "<h1>" + file + " is missing from the build</h1>";
    }

    static string LoadPage()
    {
        var asm = typeof(PanelServer).Assembly;
        foreach (var n in asm.GetManifestResourceNames())
        {
            if (!n.EndsWith("panel.html", StringComparison.OrdinalIgnoreCase)) continue;
            using (var s = asm.GetManifestResourceStream(n))
            using (var r = new StreamReader(s, Encoding.UTF8))
            {
                return r.ReadToEnd();
            }
        }
        return "<h1>panel.html is missing from the build</h1>";
    }

    static async Task Loop()
    {
        while (listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); }
            catch { break; }
            _ = Task.Run(() => Handle(ctx));
        }
    }

    static bool HostOk(HttpListenerRequest r)
    {
        string h = r.Headers["Host"] ?? "";
        return h.StartsWith("127.0.0.1") || h.StartsWith("localhost");
    }

    static async Task Send(HttpListenerContext ctx, int code, string type, string text)
    {
        var b = Encoding.UTF8.GetBytes(text);
        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = type;
        ctx.Response.ContentLength64 = b.Length;
        await ctx.Response.OutputStream.WriteAsync(b, 0, b.Length);
        ctx.Response.Close();
    }

    static async Task Handle(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            string path = req.Url.AbsolutePath;
            if (!HostOk(req)) { await Send(ctx, 403, "text/plain", "forbidden"); return; }
            if (!Routes.Allowed(path)) { await Send(ctx, 404, "text/html; charset=utf-8", Routes.Elsewhere(path)); return; }
            if (Mod.Stories && path == "/" && req.HttpMethod == "GET") { ctx.Response.Redirect("/stories"); ctx.Response.Close(); return; }

            if (req.HttpMethod == "GET")
            {
                if (path == "/") { await Send(ctx, 200, "text/html; charset=utf-8", page); return; }
                if (path == "/api/state") { await Send(ctx, 200, "application/json", StateJson()); return; }
                if (path == "/api/history") { await Send(ctx, 200, "application/json", HistoryJson()); return; }
                if (path == "/gameui.css") { await Send(ctx, 200, "text/css; charset=utf-8", ReadEmbedded("gameui.css")); return; }
                if (path == "/fonts.css") { await Send(ctx, 200, "text/css; charset=utf-8", ReadEmbedded("fonts.css")); return; }
                if (path.StartsWith("/fonts/montserrat-") && path.EndsWith(".woff2"))
                {
                    var fb = ReadEmbeddedBytes(path.Substring("/fonts/".Length));
                    if (fb == null) { await Send(ctx, 404, "text/plain", "not found"); return; }
                    ctx.Response.AddHeader("Cache-Control", "public, max-age=86400");
                    ctx.Response.StatusCode = 200; ctx.Response.ContentType = "font/woff2"; ctx.Response.ContentLength64 = fb.Length;
                    await ctx.Response.OutputStream.WriteAsync(fb, 0, fb.Length);
                    ctx.Response.Close();
                    return;
                }
                if (path == "/common.js") { await Send(ctx, 200, "application/javascript; charset=utf-8", Routes.Page("common.js")); return; }
                if (path == "/icon" || path == "/favicon.ico" || path == "/logo")
                {
                    // a logo.png in the mod's folder wins (as the tab icon only if it is roughly square, a wide one gets squashed);
                    // otherwise the built-in Original Flavour logo (panel_logo.png), and the svg mark only if that is somehow missing
                    bool icon = path != "/logo";
                    byte[] png = null;
                    string lp = System.IO.Path.Combine(Host.RootPath, "logo.png");
                    if (File.Exists(lp))
                    {
                        var lb = File.ReadAllBytes(lp);
                        bool square = false;
                        if (lb.Length > 24)
                        {
                            int w = (lb[16] << 24) | (lb[17] << 16) | (lb[18] << 8) | lb[19], h = (lb[20] << 24) | (lb[21] << 16) | (lb[22] << 8) | lb[23];
                            square = w > 0 && h > 0 && w <= h * 1.3 && h <= w * 1.3;
                        }
                        if (!icon || square) png = lb;
                    }
                    if (png == null) png = ReadEmbeddedBytes("panel_logo.png");
                    if (png != null)
                    {
                        ctx.Response.StatusCode = 200; ctx.Response.ContentType = "image/png"; ctx.Response.ContentLength64 = png.Length;
                        await ctx.Response.OutputStream.WriteAsync(png, 0, png.Length); ctx.Response.Close(); return;
                    }
                    await Send(ctx, 200, "image/svg+xml", ReadEmbedded("logo.svg")); return;
                }
                if (path == "/gameui.js") { await Send(ctx, 200, "application/javascript; charset=utf-8", ReadEmbedded("gameui.js")); return; }
                if (path == "/stage") { await Send(ctx, 200, "text/html; charset=utf-8", Routes.Page("stage.html")); return; }
                if (path == "/api/stage/info") { await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { active = InGameChat.StageOn, backgrounds = InGameChat.AllBackgroundNames(), spread = Cfg.CharSpread.Value })); return; }
                if (path == "/stories") { await Send(ctx, 200, "text/html; charset=utf-8", Routes.Page("stories.html").Replace("{{ROOT}}", Host.RootLabel).Replace("{{LOG}}", Host.LogLabel)); return; }
                if (path == "/api/stories")
                {
                    var items = new List<object>();
                    foreach (var kv in StoryScript.ListAll()) items.Add(new { id = kv.Key, title = kv.Value });
                    await Send(ctx, 200, "application/json", JsonSerializer.Serialize(items));
                    return;
                }
                if (path == "/api/story")
                {
                    string sid = StoryScript.SafeId(req.QueryString["id"]);
                    string txt = StoryScript.ReadText(sid);
                    if (txt == null) { await Send(ctx, 404, "text/plain", "not found"); return; }
                    await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { id = sid, text = txt }));
                    return;
                }
                if (path == "/pet") { await Send(ctx, 200, "text/html; charset=utf-8", Routes.Page("pet.html")); return; }
                if (path == "/packs") { await Send(ctx, 200, "text/html; charset=utf-8", Routes.Page("packs.html")); return; }
                if (path == "/api/packs")
                {
                    var own = new List<object>();
                    foreach (var kv in StoryScript.ListAll()) if (!StoryPacks.IsPackId(kv.Key)) own.Add(new { id = kv.Key, title = kv.Value });
                    await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { packs = StoryPacks.List(), own = own, folder = StoryScript.Dir }));
                    return;
                }
                if (path == "/api/packs/cover")
                {
                    var cb = StoryPacks.Cover(req.QueryString["id"]);
                    if (cb == null) { await Send(ctx, 404, "text/plain", "no cover"); return; }
                    ctx.Response.StatusCode = 200; ctx.Response.ContentType = "image/png"; ctx.Response.ContentLength64 = cb.Length;
                    await ctx.Response.OutputStream.WriteAsync(cb, 0, cb.Length);
                    ctx.Response.Close();
                    return;
                }
                if (path == "/settings") { await Send(ctx, 200, "text/html; charset=utf-8", Routes.Page("settings.html")); return; }
                if (path == "/api/settings") { await Send(ctx, 200, "application/json", SettingsPage.Json()); return; }
                if (path == "/diary") { await Send(ctx, 200, "text/html; charset=utf-8", Routes.Page("diary.html")); return; }
                if (path == "/api/diary") { await Send(ctx, 200, "application/json", Diary.Json()); return; }
                if (path == "/api/pet/info") { PetView.Touch(); await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { status = PetView.Status, w = PetView.W, h = PetView.H, seq = PetView.Seq })); return; }
                if (path == "/pet/frame.png")
                {
                    PetView.Touch();
                    var png = PetView.Png; int have; int.TryParse(req.QueryString["seq"], out have);
                    ctx.Response.AddHeader("Cache-Control", "no-store");
                    if (png == null) { ctx.Response.AddHeader("X-Pet", "none"); ctx.Response.StatusCode = 204; ctx.Response.Close(); return; }
                    int seqNow = PetView.Seq;
                    ctx.Response.AddHeader("X-Seq", seqNow.ToString());
                    if (have == seqNow) { ctx.Response.AddHeader("X-Pet", "same"); ctx.Response.StatusCode = 204; ctx.Response.Close(); return; }     // nothing new, no picture sent
                    ctx.Response.StatusCode = 200; ctx.Response.ContentType = "image/png"; ctx.Response.ContentLength64 = png.Length;
                    await ctx.Response.OutputStream.WriteAsync(png, 0, png.Length);
                    ctx.Response.Close();
                    return;
                }
                if (path == "/api/music") { await Send(ctx, 200, "application/json", JsonSerializer.Serialize(StoryMusic.Names)); return; }
                if (path == "/api/backgrounds") { await Send(ctx, 200, "application/json", JsonSerializer.Serialize(InGameChat.AllBackgroundNames())); return; }
                await Send(ctx, 404, "text/plain", "not found");
                return;
            }

            if (req.HttpMethod == "POST")
            {
                // Custom header: other websites cannot send it to us from a browser.
                if (req.Headers["X-Panel"] != "1") { await Send(ctx, 403, "text/plain", "forbidden"); return; }
                string body;
                using (var r = new StreamReader(req.InputStream, Encoding.UTF8)) body = await r.ReadToEndAsync();

                if (path == "/api/cmd")
                {
                    using (var doc = JsonDocument.Parse(body))
                    {
                        string action = doc.RootElement.GetProperty("action").GetString();
                        double value = doc.RootElement.TryGetProperty("value", out var v) ? v.GetDouble() : 0;
                        Shared.Queue.Enqueue(new Cmd { Action = action, Value = value });
                    }
                    await Send(ctx, 200, "application/json", "{\"ok\":true}");
                    return;
                }
                if (path == "/api/story/save" || path == "/api/story/check")
                {
                    string sid, txt;
                    using (var doc = JsonDocument.Parse(body))
                    {
                        sid = StoryScript.SafeId(doc.RootElement.TryGetProperty("id", out var i1) ? i1.GetString() : "");
                        txt = doc.RootElement.TryGetProperty("text", out var t1) ? (t1.GetString() ?? "") : "";
                    }
                    var errs = new List<string>();
                    StoryScript.Parse(sid, txt, errs);
                    bool save = path == "/api/story/save";
                    if (save && sid.Length == 0) errs.Insert(0, "give the story a file name (letters, numbers, - and _)");
                    bool saved = false;
                    if (save && sid.Length > 0) { StoryScript.WriteText(sid, txt); saved = true; }
                    await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { ok = errs.Count == 0, saved = saved, id = sid, errors = errs }));
                    return;
                }
                if (path == "/api/pet/react")
                {
                    string tg = "";
                    using (var doc = JsonDocument.Parse(body)) tg = doc.RootElement.TryGetProperty("tags", out var tv) ? (tv.GetString() ?? "") : "";
                    tg = Regex.Replace(tg.ToLowerInvariant(), "[^a-z ]", "").Trim();
                    if (tg.Length > 0 && tg.Length < 60) PetView.Reactions.Enqueue(tg);
                    await Send(ctx, 200, "application/json", "{\"ok\":true}");
                    return;
                }
                if (path == "/api/packs/make")
                {
                    string mpath = null, merr = null;
                    try
                    {
                        using (var doc = JsonDocument.Parse(body))
                        {
                            var rt = doc.RootElement;
                            Func<string, string> S = k => rt.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";
                            var ids = new List<string>();
                            if (rt.TryGetProperty("ids", out var iv) && iv.ValueKind == JsonValueKind.Array) foreach (var x in iv.EnumerateArray()) ids.Add(x.GetString() ?? "");
                            byte[] cover = null;
                            string c64 = S("cover");
                            if (!string.IsNullOrEmpty(c64)) { int comma = c64.IndexOf(','); cover = Convert.FromBase64String(comma >= 0 ? c64.Substring(comma + 1) : c64); }
                            mpath = StoryPacks.Make(S("name"), S("author"), S("version"), S("description"), ids, cover);
                        }
                    }
                    catch (Exception ex) { merr = ex.Message; }
                    await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { ok = merr == null, error = merr, path = mpath }));
                    return;
                }
                if (path == "/api/settings/set")
                {
                    string ssec = "", skey = "", sval = "";
                    using (var doc = JsonDocument.Parse(body))
                    {
                        var rt = doc.RootElement;
                        if (rt.TryGetProperty("section", out var a1)) ssec = a1.GetString() ?? "";
                        if (rt.TryGetProperty("key", out var a2)) skey = a2.GetString() ?? "";
                        if (rt.TryGetProperty("value", out var a3)) sval = a3.ValueKind == JsonValueKind.String ? (a3.GetString() ?? "") : a3.GetRawText();
                    }
                    string serr = SettingsPage.Set(ssec, skey, sval);
                    await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { ok = serr == null, error = serr }));
                    return;
                }
                if (path == "/api/diary/write")
                {
                    int dd = 0;
                    try { using (var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body)) if (doc.RootElement.TryGetProperty("day", out var dv) && dv.ValueKind == JsonValueKind.Number) dd = dv.GetInt32(); } catch { }
                    if (dd <= 0) dd = Shared.Current != null && Shared.Current.Loaded ? Shared.Current.Day : 0;
                    string derr = Diary.Write(dd);
                    await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { ok = derr == null, error = derr, day = dd }));
                    return;
                }
                if (path == "/api/music/play")
                {
                    string mn = "";
                    using (var doc = JsonDocument.Parse(body)) mn = doc.RootElement.TryGetProperty("name", out var nv) ? (nv.GetString() ?? "") : "";
                    StoryMusic.Requests.Enqueue(mn);
                    await Send(ctx, 200, "application/json", "{\"ok\":true}");
                    return;
                }
                if (path == "/api/stage/start") { InGameChat.StageStartRequested = true; await Send(ctx, 200, "application/json", "{\"ok\":true}"); return; }
                if (path == "/api/stage/box")
                {
                    using (var doc = JsonDocument.Parse(body)) InGameChat.StageBox = doc.RootElement.TryGetProperty("on", out var bo) && bo.ValueKind == JsonValueKind.True;
                    await Send(ctx, 200, "application/json", "{\"ok\":true}");
                    return;
                }
                if (path == "/api/stage/end") { InGameChat.StageEndRequested = true; await Send(ctx, 200, "application/json", "{\"ok\":true}"); return; }
                if (path == "/api/stage/cmd")
                {
                    string scmd;
                    using (var doc = JsonDocument.Parse(body)) scmd = (doc.RootElement.GetProperty("cmd").GetString() ?? "").Trim().TrimStart('@');
                    if (!InGameChat.StageOn) { await Send(ctx, 200, "application/json", "{\"ok\":false,\"error\":\"the stage is not open in the game\"}"); return; }
                    var serrs = new List<string>();
                    var sst = StoryScript.Parse("stage", "# a\n@" + scmd + "\n-> end\n", serrs);
                    if (serrs.Count > 0 || !sst.Scenes.ContainsKey("a") || sst.Scenes["a"].Lines.Count == 0)
                    { await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { ok = false, error = serrs.Count > 0 ? serrs[0] : "bad command" })); return; }
                    if (InGameChat.StageCmds.Count < 300) InGameChat.StageCmds.Enqueue(sst.Scenes["a"].Lines[0].Cmd);
                    await Send(ctx, 200, "application/json", "{\"ok\":true}");
                    return;
                }
                if (path == "/api/story/delete")
                {
                    string sid;
                    using (var doc = JsonDocument.Parse(body)) sid = StoryScript.SafeId(doc.RootElement.GetProperty("id").GetString());
                    await Send(ctx, 200, "application/json", "{\"ok\":" + (StoryScript.Delete(sid) ? "true" : "false") + "}");
                    return;
                }
                if (path == "/api/story/play")
                {
                    string sid;
                    using (var doc = JsonDocument.Parse(body)) sid = StoryScript.SafeId(doc.RootElement.GetProperty("id").GetString());
                    if (!Requirements.Ready) { await Send(ctx, 200, "application/json", JsonSerializer.Serialize(new { ok = false, error = Requirements.Why() })); return; }
                    InGameChat.StoryRequest = sid;
                    await Send(ctx, 200, "application/json", "{\"ok\":true}");
                    return;
                }
                if (path == "/api/clear")
                {
                    lock (gate) { history.Clear(); }
                    await Send(ctx, 200, "application/json", "{\"ok\":true}");
                    return;
                }
                if (path == "/api/chat")
                {
                    string msg;
                    using (var doc = JsonDocument.Parse(body)) msg = doc.RootElement.GetProperty("message").GetString() ?? "";
                    msg = msg.Trim();
                    if (msg.Length == 0) { await Send(ctx, 400, "text/plain", "empty"); return; }
                    await Chat(ctx, msg);
                    return;
                }
            }
            await Send(ctx, 404, "text/plain", "not found");
        }
        catch (Exception ex)
        {
            try { await Send(ctx, 500, "text/plain", ex.Message); } catch { }
        }
    }

    static string Pct(float v) { return (int)Math.Round(Math.Max(0f, Math.Min(1f, v)) * 100f) + "%"; }

    static string LastReplyText()
    {
        lock (gate) { return history.Count > 0 ? history[history.Count - 1].text : ""; }
    }

    static string StoryText(Snap s)
    {
        if (!Cfg.StoryContext.Value || string.IsNullOrEmpty(s.Flags)) return "";
        return " Background: these internal story events already happened in your shared life (names are game flags, read them as hints only, never quote them): " + s.Flags + ".";
    }

    static string StateText(Snap s)
    {
        if (!s.Loaded) return "unknown";
        return "day " + s.Day + ", money " + s.Money + ", health " + Pct(s.Health) + ", stamina " + Pct(s.Stamina) +
               ", mental health " + Pct(s.Mental) + ", hunger satisfied " + Pct(s.Satiation) + ", mood " + Pct(s.Mood) +
               (s.Hour >= 0 ? ", time " + s.Hour + ":00" : "") + ", " + (s.Clothed ? "you are dressed" : "you are not dressed") +
               ", her intelligence level " + s.Intel + (s.RentDue ? ", rent is due" : "");
    }

    static string StateJson()
    {
        var s = Shared.Current;
        return JsonSerializer.Serialize(new
        {
            loaded = s.Loaded, bot = s.Bot, player = s.Player, day = s.Day, money = s.Money, tokens = s.Tokens,
            health = s.Health, stamina = s.Stamina, mental = s.Mental, satiation = s.Satiation, mood = s.Mood,
            rentDue = s.RentDue, debug = s.Debug, result = Shared.LastResult, emotes = Shared.EmoteInfo,
            ready = s.Ready, missing = s.Missing, stage = s.Stage, intel = s.Intel, editor = PublicBuild.Editor,
            mod = Mod.Name, otherMod = Mod.OtherName, otherPort = Mod.OtherPort
        });
    }

    static string HistoryJson()
    {
        var list = new List<object>();
        lock (gate)
        {
            foreach (var h in history)
            {
                // the hidden greeting instruction stays in the model's history but is not shown as if you had typed it
                if (h.role == "user" && h.text != null && h.text.StartsWith("(Game event)", StringComparison.Ordinal)) continue;
                list.Add(new { role = h.role, text = h.text });
            }
        }
        return JsonSerializer.Serialize(list);
    }

    // Fire-and-forget: used by the in-game chat. The result is picked up on the main thread.
    static System.Threading.CancellationTokenSource chatCts;

    public static void CancelChat()
    {
        try { var c = chatCts; if (c != null) c.Cancel(); } catch { }
    }

    public static void AskAsync(string userText)
    {
        var cts = new System.Threading.CancellationTokenSource();
        chatCts = cts;
        Task.Run(async () =>
        {
            string reply;
            try { reply = await Generate(userText, t => Task.CompletedTask, cts.Token); }
            catch (Exception ex) { reply = "[error] " + ex.Message; }
            if (cts.IsCancellationRequested) return; // cancelled: show nothing
            if (string.IsNullOrEmpty(reply)) reply = "[no reply]";
            InGameChat.PendingReply = reply;
        });
    }

    static async Task Chat(HttpListenerContext ctx, string userText)
    {
        var res = ctx.Response;
        res.StatusCode = 200;
        res.ContentType = "text/plain; charset=utf-8";
        res.SendChunked = true;
        var o = res.OutputStream;
        Func<string, Task> W = async t =>
        {
            var b = Encoding.UTF8.GetBytes(t);
            await o.WriteAsync(b, 0, b.Length);
            await o.FlushAsync();
        };
        try
        {
            if (!Requirements.Ready) await W("[not ready] " + Requirements.Why());
            else await Generate(userText, W);
        }
        finally { try { res.Close(); } catch { } }
    }

    // Shared by the web page and the in-game chat. Streams tokens to 'emit' and returns the final text.
    static async Task<string> Generate(string userText, Func<string, Task> emit, System.Threading.CancellationToken ct = default)
    {
        var snap = Shared.Current;
        string system = Cfg.SystemPrompt.Value
            .Replace("{bot}", snap.Bot).Replace("{player}", snap.Player).Replace("{state}", StateText(snap)) + StoryText(snap) + StoryMemory.Recent(userText) + Characters.For(userText, LastReplyText()) + Cfg.TagRules;
        var messages = new List<object> { new { role = "system", content = system } };

        bool wasBusy;
        lock (gate)
        {
            wasBusy = busy;
            if (!wasBusy)
            {
                busy = true;
                history.Add(("user", userText));
                int keep = Math.Max(2, Cfg.HistoryMessages.Value);
                for (int i = Math.Max(0, history.Count - keep); i < history.Count; i++)
                    messages.Add(new { role = history[i].role, content = history[i].text });
            }
        }
        if (wasBusy)
        {
            string m = "[busy] Still answering the last message.";
            await emit(m);
            return m;
        }

        var sb = new StringBuilder();
        string error = null;
        try
        {
            string body = Llm.Body(messages, true, Cfg.MaxTokens.Value);
            string url = Llm.ChatUrl();
            using (var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(body, Encoding.UTF8, "application/json") })
            using (var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct))
            using (ct.Register(() => { try { resp.Dispose(); } catch { } }))
            {
                if (!resp.IsSuccessStatusCode)
                {
                    string detail = "";
                    try { detail = await resp.Content.ReadAsStringAsync(); } catch { }
                    if (detail.Length > 200) detail = detail.Substring(0, 200);
                    error = "[error] The AI server returned " + (int)resp.StatusCode + " for " + url + ". " + detail + " (Check AI Url, Api and Model in the settings page.)";
                    await emit(error);
                }
                else
                {
                    using (var st = await resp.Content.ReadAsStreamAsync())
                    using (var rd = new StreamReader(st))
                    {
                        string line;
                        while ((line = await rd.ReadLineAsync()) != null)
                        {
                            string t; bool fin;
                            if (!Llm.StreamLine(line, out t, out fin)) continue;
                            if (!string.IsNullOrEmpty(t)) { sb.Append(t); await emit(t); }
                            if (fin) break;
                        }
                    }
                    if (sb.Length == 0)
                    {
                        error = "[no reply] The model sent no text. If it is a thinking model, turn thinking off in your server (or Api = ollama with Think = false) and raise MaxTokens. With Ollama or LM Studio, check that Model is the exact name.";
                        await emit(error);
                    }
                }
            }
        }
        catch (Exception) when (ct.IsCancellationRequested)
        {
            error = "[cancelled]";
        }
        catch (HttpRequestException)
        {
            error = "[error] " + Llm.NotReachable;
            try { await emit(error); } catch { }
        }
        catch (Exception ex)
        {
            error = "[error] " + ex.Message;
            try { await emit(error); } catch { }
        }
        finally
        {
            bool cancelled = ct.IsCancellationRequested;
            if (sb.Length > 0 && !cancelled) ChatLog.Append(snap.Player, userText.StartsWith("(Game event)") ? "(game loaded)" : userText, snap.Bot, sb.ToString().Trim());
            lock (gate)
            {
                if (cancelled)
                {
                    if (history.Count > 0 && history[history.Count - 1].role == "user") history.RemoveAt(history.Count - 1);
                }
                else if (sb.Length > 0) history.Add(("assistant", sb.ToString().Trim()));
                busy = false;
            }
        }
        return sb.Length > 0 ? sb.ToString().Trim() : (error ?? "");
    }
}
