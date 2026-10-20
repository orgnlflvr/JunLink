using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Jun's diary (v5.15, untested in game).
// When the in-game day goes up by one, Jun writes a short private entry about the day that just ended, from the lines in
// jun_memory.txt for that day (story text you saw and your chats with her). Entries live in jun_diary.jsonl next to the memory file.
// Read them at http://127.0.0.1:8765/diary, where you can also ask her to write today's entry now.
// No game types are used here, so this file is the same for BepInEx and MelonLoader.
public static class Diary
{
    public class Entry
    {
        public int day { get; set; }
        public string written { get; set; }
        public string bot { get; set; }
        public string player { get; set; }
        public string text { get; set; }
    }

    static readonly object gate = new object();
    static List<Entry> entries;
    static int lastDay = -1;
    static volatile bool writing;
    public static volatile string Status = "";
    static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

    static string PathName { get { return Path.Combine(Host.RootPath, "jun_diary.jsonl"); } }
    static string MemoryPath { get { return Path.Combine(Host.RootPath, "jun_memory.txt"); } }
    public static bool Writing { get { return writing; } }

    static void Load()
    {
        entries = new List<Entry>();
        try
        {
            if (!File.Exists(PathName)) return;
            foreach (var l in File.ReadAllLines(PathName, Encoding.UTF8))
            {
                if (l.Trim().Length == 0) continue;
                try { var e = JsonSerializer.Deserialize<Entry>(l); if (e != null && !string.IsNullOrWhiteSpace(e.text)) entries.Add(e); } catch { }
            }
        }
        catch (Exception ex) { Cfg.Log.LogWarning("Diary: could not read " + PathName + ": " + ex.Message); }
    }

    // newest day first
    public static List<Entry> All()
    {
        lock (gate)
        {
            if (entries == null) Load();
            var l = new List<Entry>(entries);
            l.Sort((a, b) => b.day != a.day ? b.day.CompareTo(a.day) : string.CompareOrdinal(b.written ?? "", a.written ?? ""));
            return l;
        }
    }

    static bool Has(int day)
    {
        lock (gate)
        {
            if (entries == null) Load();
            foreach (var e in entries) if (e.day == day) return true;
            return false;
        }
    }

    // main thread, every frame: cheap. Writes yesterday's entry when the game moves to the next day.
    public static void Tick()
    {
        if (!Cfg.DiaryEnabled.Value) return;
        var s = Shared.Current;
        if (s == null || !s.Loaded || s.Day <= 0) return;
        if (lastDay < 0) { lastDay = s.Day; return; }
        if (s.Day == lastDay) return;
        int prev = lastDay;
        lastDay = s.Day;
        if (s.Day != prev + 1) return;                 // another save was loaded: not a new day
        if (Has(prev)) return;
        if (!Requirements.Ready) { Cfg.Log.LogInfo("Diary: day " + prev + " ended, no entry: " + Requirements.Why()); return; }
        string err = Write(prev);
        Cfg.Log.LogInfo("Diary: day " + prev + " ended, " + (err == null ? "writing the entry" : "not written: " + err));
    }

    // what happened on that day, from jun_memory.txt (shortened, newest lines kept)
    static string NotesFor(int day, out int count)
    {
        count = 0;
        var picked = new List<string>();
        try
        {
            if (!File.Exists(MemoryPath)) return "";
            string prefix = "Day " + day + " ";
            foreach (var l in File.ReadAllLines(MemoryPath, Encoding.UTF8))
            {
                if (!l.StartsWith(prefix, StringComparison.Ordinal)) continue;
                string t = Regex.Replace(l.Substring(prefix.Length), @"^\s*(\d+:00\s*)?\[[^\]]*\]\s*", m => m.Groups[1].Value);
                if (t.Length > 220) t = t.Substring(0, 220) + "...";
                picked.Add(t);
            }
        }
        catch { }
        count = picked.Count;
        int max = Math.Max(10, Cfg.DiaryNotes.Value);
        if (picked.Count > max) picked = picked.GetRange(picked.Count - max, max);
        return string.Join("\n", picked);
    }

    // starts writing the entry for that day in the background; returns null, or why it could not start
    public static string Write(int day)
    {
        if (day <= 0) return "no save is loaded";
        if (!Requirements.Ready) return Requirements.Why();
        if (writing) return "she is already writing";
        writing = true;
        Status = "writing day " + day + "...";
        var snap = Shared.Current;
        string bot = snap != null ? snap.Bot : "Jun", player = snap != null ? snap.Player : "Anon";
        Task.Run(async () =>
        {
            try
            {
                int n;
                string notes = NotesFor(day, out n);
                string text = await Generate(day, bot, player, notes);
                if (string.IsNullOrWhiteSpace(text)) { Status = "the model sent no text"; return; }
                var e = new Entry { day = day, written = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), bot = bot, player = player, text = text };
                lock (gate)
                {
                    if (entries == null) Load();
                    entries.RemoveAll(x => x.day == day);      // writing a day again replaces it
                    entries.Add(e);
                    var sb = new StringBuilder();
                    foreach (var x in entries) sb.Append(JsonSerializer.Serialize(x)).Append('\n');
                    File.WriteAllText(PathName, sb.ToString(), new UTF8Encoding(false));
                }
                Status = "wrote day " + day + " (" + n + " notes)";
                Cfg.Log.LogInfo("Diary: wrote day " + day + " from " + n + " memory lines (" + text.Length + " chars)");
            }
            catch (HttpRequestException) { Status = Llm.NotReachable; }
            catch (Exception ex) { Status = "failed: " + ex.Message; Cfg.Log.LogWarning("Diary: " + ex.Message); }
            finally { writing = false; }
        });
        return null;
    }

    static async Task<string> Generate(int day, string bot, string player, string notes)
    {
        int words = Math.Max(30, Cfg.DiaryWords.Value);
        string system = Cfg.DiaryPrompt.Value
            .Replace("{bot}", bot).Replace("{player}", player).Replace("{day}", day.ToString()).Replace("{words}", words.ToString())
            + Characters.For(bot, "");
        string user = notes.Length > 0
            ? "Notes of what happened on day " + day + " (oldest first):\n" + notes
            : "Nothing was written down on day " + day + ". It was a quiet day at home with " + player + ".";
        string body = Llm.Body(new object[] { new { role = "system", content = system }, new { role = "user", content = user } }, false, Math.Max(Cfg.MaxTokens.Value, words * 3));
        string url = Llm.ChatUrl();
        using (var resp = await Http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")))
        {
            string json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception("the AI server returned " + (int)resp.StatusCode + " for " + url + " (check AI Url, Api and Model in the settings)");
            string text = Llm.FullText(json);
            return Clean(text);
        }
    }

    static string Clean(string t)
    {
        t = Regex.Replace(t ?? "", @"<think>[\s\S]*?</think>", "");
        t = Regex.Replace(t, @"\[[a-zA-Z ]{1,20}\]", "");          // mood tags like [happy]
        t = Regex.Replace(t, @"\*[^*\n]{1,80}\*", "");             // *smiles*
        t = Regex.Replace(t, @"[ \t]{2,}", " ");
        return t.Trim();
    }

    public static string Json()
    {
        var s = Shared.Current;
        return JsonSerializer.Serialize(new
        {
            enabled = Cfg.DiaryEnabled.Value,
            day = s != null && s.Loaded ? s.Day : 0,
            bot = s != null ? s.Bot : "Jun",
            writing = writing,
            status = Status,
            entries = All()
        });
    }
}
