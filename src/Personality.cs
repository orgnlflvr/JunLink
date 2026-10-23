using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if MELON
using Il2Cpp;
#endif

// ---- Personality from her diary (Jun Link 1.2) ----
// Your own AI writes Jun's lines for the game's personality triggers (greeting, headpat, cuddle, kissing, shower ...),
// from her diary and her feelings right now, and saves them as a normal game personality mod (mod.json + script.lua).
// The game loads it at start; it shows up as a personality module in the Lady Parts shop. Nothing is hooked live.
// Only everyday / affectionate triggers are written; everything else keeps the game's own lines.
public static class Personality
{
    public static volatile string Status = "";
    public static volatile bool Busy;
    public static volatile int Done, Total;
    public static string GameModPath;          // the game's own mod folder, read on the game thread
    public static string LastFolder = "";

    // a fresh connection for every request: KoboldCpp closes the kept-alive one after a reply, and the next request on it failed
    // (log 2026-10-23: the first set was written, then "can't reach the AI server")
    static readonly HttpClient Http = NewClient();
    static HttpClient NewClient() { var c = new HttpClient { Timeout = TimeSpan.FromMinutes(5) }; c.DefaultRequestHeaders.ConnectionClose = true; return c; }
    const string FolderName = "JunLink_Personality";

    // trigger, what happens ({bot} {player} filled in)
    static readonly string[][] Triggers = {
        new[] { "Interact_Greeting",       "{player} comes over to {bot} to spend time with her, and she greets him" },
        new[] { "Interact_Headpat",        "{player} pats {bot} on the head" },
        new[] { "Interact_Handholding",    "{player} takes {bot}'s hand and holds it" },
        new[] { "Interact_FaceStroke",     "{player} gently strokes {bot}'s face" },
        new[] { "Cuddle_Beginning",        "{player} and {bot} lie down in bed to cuddle" },
        new[] { "Cuddle_Headpat",          "{player} pats {bot}'s head while they cuddle in bed" },
        new[] { "Cuddle_Handholding",      "{player} holds {bot}'s hand while they cuddle in bed" },
        new[] { "Cuddle_FaceStroke",       "{player} strokes {bot}'s face while they cuddle in bed" },
        new[] { "Cuddle_CloseRandom",      "{bot} is cuddled up close to {player} in bed and says something cozy" },
        new[] { "Cuddle_FarRandom",        "{bot} lies in bed a little apart from {player} and says something" },
        new[] { "Cuddle_BeforeFallAsleep", "{bot} is about to fall asleep in {player}'s arms" },
        new[] { "Cuddle_AfterWakeUp",      "{bot} wakes up next to {player}" },
        new[] { "Cuddle_AnonFellAsleep",   "{player} fell asleep while cuddling and {bot} whispers to him" },
        new[] { "Kissing_KissGeneric",     "{player} kisses {bot}" },
        new[] { "Kissing_KissHair",        "{player} kisses {bot}'s hair" },
        new[] { "Kissing_KissEye",         "{player} kisses {bot}'s eyelid" },
        new[] { "Kissing_KissArm",         "{player} kisses {bot}'s arm" },
        new[] { "Shower_Beginning",        "{player} and {bot} start a shower together; he is going to wash her" },
        new[] { "Shower_Scrubbing",        "{player} scrubs {bot} clean in the shower" },
        new[] { "Shower_SoapInEyes",       "soap got into {bot}'s eyes in the shower" },
        new[] { "Shower_AfterWashing",     "the shower is over and {bot} is clean" },
    };
    // opt-in (checkbox on the diary page): consensual adult moments. Every non-consent trigger of the game ("Rape" in the name,
    // and Cowgirl_AnonAfterFirstInsertRapeConsent) is left out on purpose and always keeps the game's own lines.
    // Choice / question triggers (Cowgirl_WhereToCumQuestion...) and Blowjob_SwallowWithMl (fills in an amount) are left out: they need the game's own format.
    static readonly string[][] AdultTriggers = {
        new[] { "Kissing_KissTummy",  "{player} kisses {bot}'s tummy" },
        new[] { "Kissing_KissLeg",    "{player} kisses {bot}'s leg" },
        new[] { "Kissing_KissThigh",  "{player} kisses {bot}'s inner thigh" },
        new[] { "Kissing_KissFoot",   "{player} kisses {bot}'s foot" },
        new[] { "Kissing_KissBoobs",  "{player} kisses {bot}'s breasts" },
        new[] { "Kissing_KissAss",    "{player} kisses {bot}'s butt" },
        new[] { "Kissing_KissPelvic", "{player} kisses {bot} between her legs" },
        new[] { "Frick_StartFrick",   "{bot} and {player} start having sex" },
        new[] { "Frick_Moan",         "{bot} moans during sex with {player}" },
        new[] { "Frick_Headpat",      "{player} pats {bot}'s head during sex" },
        new[] { "Frick_Handholding",  "{player} holds {bot}'s hand during sex" },
        new[] { "Frick_BoobFondle",   "{player} fondles {bot}'s breasts during sex" },
        new[] { "Frick_NipplePull",   "{player} pulls {bot}'s nipples during sex" },
        new[] { "Frick_ClitRub",      "{player} rubs {bot}'s clit during sex" },
        new[] { "Frick_PussySpread",  "{player} spreads {bot} open during sex" },
        new[] { "Frick_AnusSpread",   "{player} spreads {bot}'s butt during sex" },
        new[] { "Frick_FingeringDuring",         "{player} fingers {bot}" },
        new[] { "Frick_FingeringBotCumming",     "{bot} climaxes from {player}'s fingers" },
        new[] { "Frick_FingeringAnusDuring",     "{player} fingers {bot}'s butt" },
        new[] { "Frick_FingeringAnusBotCumming", "{bot} climaxes from {player}'s fingers in her butt" },
        new[] { "Frick_EdgingOn",     "{player} starts edging, holding back on purpose" },
        new[] { "Frick_EdgingOff",    "{player} stops edging" },
        new[] { "Frick_BotCumming",   "{bot} climaxes during sex" },
        new[] { "Frick_CumInside",    "{player} finishes inside {bot}" },
        new[] { "Frick_CumInsideAnal","{player} finishes inside {bot}'s butt" },
        new[] { "Frick_CumOutside",   "{player} pulls out and finishes on {bot}" },
        new[] { "Frick_CumThighjob",  "{player} finishes between {bot}'s thighs" },
        new[] { "Frick_CumTogether",  "{bot} and {player} climax at the same time" },
        new[] { "Frick_FlacidBoredReaction", "{player} went soft and {bot} reacts, a bit disappointed" },
        new[] { "Frick_UpsetOnlySheMoves",   "{bot} is the only one moving during sex and teases {player} about it" },
        new[] { "Frick_GenericFuckBrainStateRandomDialogueCockInside",     "{bot} says something during sex with {player} inside her" },
        new[] { "Frick_GenericFuckBrainStateRandomDialogueCockInsideAnal", "{bot} says something during anal sex with {player}" },
        new[] { "Frick_GenericFuckBrainStateRandomDialogueCockOutside",    "{bot} says something during foreplay, {player} not inside yet" },
        new[] { "Cowgirl_RemovePants",         "{bot} takes off {player}'s pants, eager to ride him" },
        new[] { "Cowgirl_BeforeFirstInsert",   "{bot} is on top of {player}, just before she lowers herself onto him" },
        new[] { "Cowgirl_BeforeFirstInsertBotLead", "{bot} takes the lead on top of {player}, just before she lowers herself onto him" },
        new[] { "Cowgirl_EdgingBreak",         "{bot} is riding {player} and slows down so he can hold on" },
        new[] { "Cowgirl_EdgingForceStopped",  "{bot} stops riding {player} to keep him on the edge" },
        new[] { "Cowgirl_FlacidReaction",      "{player} went soft while {bot} rides him and she reacts" },
        new[] { "Cowgirl_MadeMcCumInsideWhileCummingHerself", "{bot} climaxes while riding {player} and he finishes inside her at the same time" },
        new[] { "Cowgirl_BeforeSwitchToBlowjobBotLead", "{bot} gets off {player} to use her mouth instead" },
        new[] { "Blowjob_EdgingAlmostCame",    "{bot} is using her mouth on {player} and stops right before he finishes" },
        new[] { "Blowjob_EdgingForceStop",     "{bot} stops using her mouth on {player} to keep him on the edge" },
        new[] { "Blowjob_EdgingHadToSlowdown", "{bot} slows down with her mouth because {player} is close" },
        new[] { "Blowjob_SwallowFull",         "{bot} swallowed everything after using her mouth on {player}" },
        new[] { "Blowjob_SwallowHalf",         "{bot} swallowed about half after using her mouth on {player}" },
        new[] { "Blowjob_SwallowLittle",       "{bot} swallowed only a little after using her mouth on {player}" },
        new[] { "Blowjob_SwallowAlmostEmpty",  "{player} had almost nothing left to give when {bot} used her mouth on him" },
        new[] { "Blowjob_SwitchToCowgirlBotLead", "{bot} stops using her mouth and climbs on top of {player} to ride him" },
        new[] { "Shower_AfterSex",             "{bot} and {player} shower together after sex" },
        new[] { "Shower_AfterSexDirty",        "{bot} is still messy from sex at the start of the shower" },
        new[] { "Shower_AfterSexDirtyCleanedUp","{player} washed {bot} clean after sex" },
        new[] { "Shower_AfterSexDirtyLeaveCum", "{bot} chooses to stay a little messy after sex" },
    };

    // these also get a second set of lines for when she misses him a lot (game longing >= 5; "very high" is above 9.5)
    static readonly string[] LongingToo = { "Interact_Greeting", "Cuddle_Beginning" };

    // game thread (GameLink.Read): remember where the game keeps its mods
    public static void Capture()
    {
#if MELON
        if (GameModPath != null) return;
        try { var m = ModsSingleton.Instance; if (m != null && !string.IsNullOrEmpty(m.ModPath)) { GameModPath = m.ModPath; Cfg.Log.LogInfo("Personality: the game's mod folder is " + GameModPath); } } catch { }
#endif
    }

    public static string Json()
    {
        return System.Text.Json.JsonSerializer.Serialize(new { busy = Busy, done = Done, total = Total, status = Status, folder = LastFolder, gameFolder = GameModPath ?? "" });
    }

    public static bool Adult; static List<Moment> moments;

    // ---- the list of moments, editable: UserData\JunLink\personality_triggers.txt ----
    // made from the built-in lists on first use; add any trigger from the game's list there, one per line:
    //   TriggerName | what happens ({bot} {player} are filled in)      18+ in front = only with the adult checkbox
    // Non-consent triggers ("Rape" in the name) are always skipped, even if added by hand.
    public class Moment { public string Name, What; public bool Lewd; }
    static string ListPath { get { return Path.Combine(Host.RootPath, "personality_triggers.txt"); } }

    static List<Moment> LoadMoments()
    {
        if (!File.Exists(ListPath))
        {
            var sb = new StringBuilder();
            sb.Append("# Moments Jun Link writes her lines for (Personality from her diary, on the diary page).\n");
            sb.Append("# One per line:   TriggerName | what happens in that moment     ({bot} and {player} are filled in)\n");
            sb.Append("# Put 18+ in front of a line for adult moments: those are only written when the adult checkbox is ticked.\n");
            sb.Append("# Add your own with any trigger name from the game's personality trigger list. Delete a line or put # in front to skip it.\n");
            sb.Append("# Triggers with \"Rape\" in the name are always skipped. Question / choice triggers may need the game's own format.\n\n");
            foreach (var t in Triggers) sb.Append(t[0]).Append(" | ").Append(t[1]).Append('\n');
            sb.Append('\n');
            foreach (var t in AdultTriggers) sb.Append("18+ ").Append(t[0]).Append(" | ").Append(t[1]).Append('\n');
            File.WriteAllText(ListPath, sb.ToString(), new UTF8Encoding(false));
        }
        var r = new List<Moment>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadAllLines(ListPath, Encoding.UTF8))
        {
            string l = raw.Trim();
            if (l.Length == 0 || l.StartsWith("#")) continue;
            bool lewd = false;
            if (l.StartsWith("18+")) { lewd = true; l = l.Substring(3).Trim(); }
            int bar = l.IndexOf('|');
            string name = (bar >= 0 ? l.Substring(0, bar) : l).Trim(), what = bar >= 0 ? l.Substring(bar + 1).Trim() : "";
            if (!Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9_]*$")) { Cfg.Log.LogWarning("Personality: skipped '" + raw + "' (not a trigger name)"); continue; }
            if (name.IndexOf("Rape", StringComparison.OrdinalIgnoreCase) >= 0) { Cfg.Log.LogInfo("Personality: skipped " + name + " (non-consent triggers are never written)"); continue; }
            if (!seen.Add(name)) continue;
            if (what.Length == 0) what = "the moment the game calls " + name;
            r.Add(new Moment { Name = name, What = what, Lewd = lewd });
        }
        return r;
    }
    public static string Start(bool adult)
    {
        if (Busy) return "she is already writing it";
        var snap = Shared.Current;
        if (snap == null || !snap.Loaded) return "no save is loaded";
        if (!Requirements.Ready) return Requirements.Why();
        Adult = adult;
        List<Moment> list;
        try { list = LoadMoments().Where(m => adult || !m.Lewd).ToList(); } catch (Exception ex) { return "could not read " + ListPath + ": " + ex.Message; }
        if (list.Count == 0) return "personality_triggers.txt has no moments in it";
        moments = list;
        Busy = true; Done = 0; Total = list.Count + list.Count(m => Array.IndexOf(LongingToo, m.Name) >= 0); Status = "starting...";
        Task.Run(async () =>
        {
            try { await Generate(snap); }
            catch (HttpRequestException ex) { Status = Llm.NotReachable + " (" + Inner(ex) + ")"; Cfg.Log.LogWarning("Personality: " + ex); }
            catch (Exception ex) { Status = "failed: " + ex.Message; Cfg.Log.LogWarning("Personality: " + ex); }
            finally { Busy = false; }
        });
        return null;
    }

    static string Context(Snap s)
    {
        var sb = new StringBuilder();
        sb.Append("How she feels right now: ");
        sb.Append(string.IsNullOrEmpty(s.Bond) ? "she cares about " + s.Player : "towards " + s.Player + ": " + s.Bond);
        sb.Append(s.MissesALot ? "; she misses him badly." : s.Longing >= 5f ? "; she misses him." : ".");
        float h = s.HornyMax > 0.01f ? s.Horny / s.HornyMax : s.Horny;
        if (Adult) sb.Append(h >= 0.75f ? " Her desire is strong right now." : h >= 0.4f ? " She feels some desire right now." : "");
        var entries = Diary.All();
        if (entries.Count > 0)
        {
            sb.Append("\nHer most recent diary entries (her own words, newest first):\n");
            foreach (var e in entries.Take(3))
            {
                string t = Regex.Replace(e.text ?? "", @"\s+", " ").Trim();
                if (t.Length > 600) t = t.Substring(0, 600) + "...";
                sb.Append("Day ").Append(e.day).Append(": ").Append(t).Append('\n');
            }
        }
        return sb.ToString();
    }

    static async Task Generate(Snap s)
    {
        string ctx = Context(s);
        string persona = Characters.For(s.Bot, "");
        var blocks = new StringBuilder();
        int id = 0, written = 0;
        foreach (var mo in moments)
        {
            string trig = mo.Name, what = mo.What.Replace("{bot}", s.Bot).Replace("{player}", s.Player);
            Status = "writing " + trig + " (" + (Done + 1) + " of " + Total + ")";
            bool lewd = mo.Lewd;
            var normal = await Lines(s, persona, ctx, what, false, lewd);
            Done++;
            List<string> missed = null;
            if (Array.IndexOf(LongingToo, trig) >= 0)
            {
                Status = "writing " + trig + " for when she misses him (" + (Done + 1) + " of " + Total + ")";
                missed = await Lines(s, persona, ctx, what, true, false);
                Done++;
            }
            if (normal.Count < 3) { Cfg.Log.LogWarning("Personality: " + trig + " got only " + normal.Count + " usable lines, kept the game's own"); continue; }
            blocks.Append(Block(trig, normal, missed, ref id));
            written++;
        }
        if (written == 0) { Status = "the model sent no usable lines"; return; }
        string folder = Save(s, blocks.ToString(), written);
        LastFolder = folder;
        Status = "done: " + written + " moments written. Restart the game, then buy and equip \"" + ItemName(s) + "\" (Lady Parts shop).";
        Cfg.Log.LogInfo("Personality: wrote " + written + " triggers to " + folder);
    }

    static string Inner(Exception ex) { while (ex.InnerException != null) ex = ex.InnerException; return ex.Message; }

    // one retry after a short pause: a single dropped connection should not throw away the sets already written
    static async Task<List<string>> Lines(Snap s, string persona, string ctx, string what, bool missedHim, bool lewd)
    {
        try { return await LinesOnce(s, persona, ctx, what, missedHim, lewd); }
        catch (HttpRequestException ex)
        {
            Cfg.Log.LogWarning("Personality: request failed (" + Inner(ex) + "), trying once more");
            await Task.Delay(1500);
            return await LinesOnce(s, persona, ctx, what, missedHim, lewd);
        }
    }

    static async Task<List<string>> LinesOnce(Snap s, string persona, string ctx, string what, bool missedHim, bool lewd)
    {
        string system = "You write short spoken lines for " + s.Bot + ", the robot girlfriend in the game My Dystopian Robot Girlfriend, for a personality mod. " +
            "Stay true to her: " + persona + "\n" + ctx +
            "\nRules: write exactly 8 different lines she could say out loud in this moment, one per line. Each line under 25 words. " +
            "No numbering, no quotes, no names before the line, no actions or narration, no *asterisks*, no emoji, no tags in brackets. " +
            (lewd ? "This is an intimate adult scene between two consenting adults; she is a willing, loving partner and the lines may be sexual and explicit, in her own voice. " : "") +
            "Call him " + s.Player + " or 'you'. Let her feelings and the diary colour the lines, but never quote the diary or mention stats.";
        string user = "The moment: " + what + "." + (missedHim ? " She has missed him a lot and is very happy to finally have him close again." : "");
        string body = Llm.Body(new object[] { new { role = "system", content = system }, new { role = "user", content = user } }, false, Math.Max(Cfg.MaxTokens.Value, 500));
        string url = Llm.ChatUrl();
        using (var resp = await Http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")))
        {
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception("the AI server returned " + (int)resp.StatusCode + " for " + url + " (check AI Url, Api and Model in the settings)");
            return Clean(Llm.FullText(json), s.Bot);
        }
    }

    // one usable game line per model line; anything that could break the Lua or the game's dialogue syntax is removed
    static List<string> Clean(string text, string bot)
    {
        var r = new List<string>();
        text = Regex.Replace(text ?? "", @"<think>[\s\S]*?</think>", "");
        foreach (var raw in text.Split('\n'))
        {
            string t = raw.Trim();
            t = Regex.Replace(t, @"^(\d+[\.\):]|[-•*])\s*", "");                       // 1. / - / *
            t = Regex.Replace(t, @"^(" + Regex.Escape(bot) + @"|Bot)\s*:\s*", "", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"\*[^*]{1,80}\*", "");                              // *smiles*
            t = Regex.Replace(t, @"\[[^\]]{0,30}\]", "");                             // [happy]
            t = t.Replace("]]", "").Replace("[[", "").Replace("\r", "").Trim().Trim('"', '\u201c', '\u201d').Trim();
            t = Regex.Replace(t, @"\s{2,}", " ");
            if (t.StartsWith("#")) t = t.TrimStart('#').Trim();
            if (t.Length < 3 || t.Length > 200) continue;
            if (Regex.IsMatch(t, @"^(here are|sure|okay,? here|lines?:)", RegexOptions.IgnoreCase)) continue;
            if (!r.Contains(t)) r.Add(t);
            if (r.Count >= 10) break;
        }
        return r;
    }

    static string Block(string trig, List<string> normal, List<string> missed, ref int id)
    {
        var sb = new StringBuilder();
        string var = "c_" + trig;
        sb.Append("\nlocal ").Append(var).Append(" = personality.PrepareContainer('").Append(trig).Append("')\n");
        if (missed != null && missed.Count >= 3)
        {
            sb.Append("local m_").Append(trig).Append(" = StoryBotDialogueBranch.__new([[\n#pickrandom\n");
            foreach (var l in missed) sb.Append("Bot: ").Append(l).Append('\n');
            sb.Append("#end\n]], CurrentModGuid, ").Append(id++).Append(")\n");
            sb.Append("m_").Append(trig).Append(".AddConditions(StoryCondition.Delegate(function() return (gv.longing or 0) >= 5 end))\n");
            sb.Append("m_").Append(trig).Append(".SetPriority(2)\n");
            sb.Append(var).Append(".AddBranch(m_").Append(trig).Append(")\n");
        }
        sb.Append("local n_").Append(trig).Append(" = StoryBotDialogueBranch.__new([[\n#pickrandom\n");
        foreach (var l in normal) sb.Append("Bot: ").Append(l).Append('\n');
        sb.Append("#end\n]], CurrentModGuid, ").Append(id++).Append(")\n");
        sb.Append("n_").Append(trig).Append(".SetPriority(1)\n");
        sb.Append(var).Append(".AddBranch(n_").Append(trig).Append(")\n");
        return sb.ToString();
    }

    static string ItemName(Snap s) { return s.Bot + "'s Heart (Jun Link)"; }

    static string Lua(string s) { return (s ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", " "); }

    static string Save(Snap s, string blocks, int count)
    {
        string root = !string.IsNullOrEmpty(GameModPath) && Directory.Exists(GameModPath) ? GameModPath : Path.Combine(Host.RootPath, "personality_mod");
        // the files are kept (and editable) in UserData\JunLink\personality_mod; the game loads mods as .zip files, so a zip goes into its mod folder
        string dir = Path.Combine(Host.RootPath, "personality_mod", FolderName);
        Directory.CreateDirectory(dir);
        // one id for this mod forever, so writing it again replaces it instead of adding a second one
        string guidFile = Path.Combine(Host.RootPath, "personality_guid.txt");
        string guid = File.Exists(guidFile) ? File.ReadAllText(guidFile).Trim() : "";
        if (!Guid.TryParse(guid, out _)) { guid = Guid.NewGuid().ToString(); File.WriteAllText(guidFile, guid); }

        string desc = s.Bot + "'s own words, written by your AI from her diary and how she feels about " + s.Player + " (day " + s.Day + ").";
        var json = new StringBuilder();
        json.Append("{\n    \"name\": \"PersonalityModule[JunLink_").Append(Regex.Replace(s.Bot, @"[^A-Za-z0-9]", "")).Append("_Heart]\",\n");
        json.Append("    \"description\": ").Append(System.Text.Json.JsonSerializer.Serialize(desc)).Append(",\n");
        json.Append("    \"OnGameStart\": {\n        \"luaScript\": \"\",\n        \"luaFiles\": [\n            \"script.lua\"\n        ]\n    },\n");
        json.Append("    \"targetVersion\": \"0.97.12\",\n");
        json.Append("    \"doNotChangeVariablesBelowThis\": {\n        \"timeCreated\": ").Append(DateTime.UtcNow.Ticks).Append(",\n");
        json.Append("        \"guid\": {\n            \"serializedGuid\": \"").Append(guid).Append("\"\n        }\n    }\n}\n");
        File.WriteAllText(Path.Combine(dir, "mod.json"), json.ToString(), new UTF8Encoding(false));

        var lua = new StringBuilder();
        lua.Append("-- ").Append(s.Bot).Append("'s Heart: written by Jun Link (github.com/orgnlflvr/JunLink) with the player's own local AI,\n");
        lua.Append("-- from her diary and brain state on day ").Append(s.Day).Append(". ").Append(count).Append(" moments; all others keep the game's own lines.\n");
        lua.Append("-- Write it again from the diary page to update it. Edit freely.\n\ndo\n");
        lua.Append("local item = ModUtilities.CreateItemPrefab()\n");
        lua.Append("item.Name = '").Append(Lua(ItemName(s))).Append("'\n");
        lua.Append("item.Description = '").Append(Lua(desc)).Append("'\n");
        lua.Append("item.Price = 1\nitem.SlotData = SlotEquipData.CreateInstance('PersonalityModule')\nitem.IsIllegal = false\nitem.HasQuality = false\n");
        lua.Append("item.ThrowingOutForbidden = false\nitem.IsPersonalityModule = true\nitem.IsStackable = false\nitem.Category = ItemCategory.Other\n");
        lua.Append("item.CanChangeColor = false\nitem.ColorSlots = {}\nitem.Partners = {}\nitem.ScratchType = ScratchTextureType.Universal\nitem.SusModifiers = {}\n\n");
        lua.Append("local itemId = ModUtilities.CreateNewItemAutoAssignId(CurrentModGuid, item)\n");
        lua.Append("ModUtilities.AddSingleBuyItemToShop('ladyparts.ic', itemId)\n\n");
        lua.Append("local personality = ModUtilities.PrepareNewPersonalityDefinition()\n");
        lua.Append(blocks);
        lua.Append("\nitem.TurnIntoPersonalityModule(itemId, personality)\nend\n");
        File.WriteAllText(Path.Combine(dir, "script.lua"), lua.ToString(), new UTF8Encoding(false));
        string zip = Path.Combine(root, FolderName + ".zip");
        if (File.Exists(zip)) File.Delete(zip);
        System.IO.Compression.ZipFile.CreateFromDirectory(dir, zip, System.IO.Compression.CompressionLevel.Optimal, false);
        // 1.2.0 test builds wrote a plain folder there; the game wants the zip, so that folder (only our two files) goes
        try
        {
            string old = Path.Combine(root, FolderName);
            if (Directory.Exists(old) && Directory.GetFiles(old).All(f => Path.GetFileName(f) == "mod.json" || Path.GetFileName(f) == "script.lua") && Directory.GetDirectories(old).Length == 0)
                Directory.Delete(old, true);
        }
        catch { }
        return zip;
    }
}
