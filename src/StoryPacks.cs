using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Story packs (v5.17, untested in game).
// A pack is a zip file with the extension .storypack, dropped into jun_stories. Inside:
//   pack.json      {"name": "...", "author": "...", "version": "1.0", "description": "..."}   (optional, but nice)
//   cover.png      a picture for the pack page (optional; the author's own art, never the game's)
//   *.story        the stories (any folder inside the zip)
// Pack stories show up in the F3 list and the story editor next to your own. Opening one in the editor and saving it
// makes your own copy; the pack itself is never changed. Make packs at http://127.0.0.1:8765/packs.
// Game pictures are never put into a pack: the page lists the backgrounds a pack needs, and players save those from
// their own game with tools\export_backgrounds.py.
public static class StoryPacks
{
    public const string Ext = ".storypack";
    public const string Prefix = "pack--";

    public class PackInfo
    {
        public string file { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string author { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public bool cover { get; set; }
        public List<StoryInfo> stories { get; set; }
        public List<string> backgrounds { get; set; }
        public List<string> missing { get; set; }
        public string error { get; set; }
    }

    public class StoryInfo
    {
        public string id { get; set; }
        public string title { get; set; }
    }

    class Meta { public string name { get; set; } public string author { get; set; } public string version { get; set; } public string description { get; set; } }

    static string Clean(string s) { return (StoryScript.SafeId(s) ?? "").Replace("--", "-").Trim('-'); }

    public static bool IsPackId(string id) { return id != null && id.StartsWith(Prefix, StringComparison.Ordinal); }

    static bool Split(string id, out string pack, out string story)
    {
        pack = story = "";
        if (!IsPackId(id)) return false;
        string rest = id.Substring(Prefix.Length);
        int i = rest.IndexOf("--", StringComparison.Ordinal);
        if (i <= 0) return false;
        pack = rest.Substring(0, i); story = rest.Substring(i + 2);
        return story.Length > 0;
    }

    // the part of a pack id that is used when you save your own copy
    public static string OwnCopyId(string id) { string p, s; return Split(id, out p, out s) ? s : id; }

    static IEnumerable<string> Files()
    {
        string[] f;
        try { f = Directory.GetFiles(StoryScript.Dir, "*" + Ext); } catch { f = new string[0]; }
        return f;
    }

    static string FileFor(string packId)
    {
        foreach (var f in Files()) if (Clean(Path.GetFileNameWithoutExtension(f)) == packId) return f;
        return null;
    }

    static string TitleOf(string text, string fallback)
    {
        foreach (var raw in (text ?? "").Split('\n'))
        {
            string t = raw.Trim();
            if (t.StartsWith("title:", StringComparison.OrdinalIgnoreCase)) { t = t.Substring(6).Trim(); return t.Length > 0 ? t : fallback; }
            if (t.StartsWith("#")) break;
        }
        return fallback;
    }

    static string ReadEntry(ZipArchiveEntry e)
    {
        using (var s = e.Open()) using (var r = new StreamReader(s, Encoding.UTF8)) return r.ReadToEnd();
    }

    // backgrounds named by @background lines in a story
    public static IEnumerable<string> BackgroundsIn(string text)
    {
        foreach (Match m in Regex.Matches(text ?? "", @"^\s*@background\s+(\S+)", RegexOptions.Multiline))
            yield return m.Groups[1].Value.Trim().ToLowerInvariant();
    }

    // the built-in places plus the pictures in jun_backgrounds; set by the game side
    public static Func<string, bool> BackgroundAvailable = n => true;

    public static List<PackInfo> List()
    {
        var list = new List<PackInfo>();
        foreach (var f in Files())
        {
            var p = new PackInfo
            {
                file = Path.GetFileName(f), id = Clean(Path.GetFileNameWithoutExtension(f)),
                name = Path.GetFileNameWithoutExtension(f), author = "", version = "", description = "",
                stories = new List<StoryInfo>(), backgrounds = new List<string>(), missing = new List<string>()
            };
            try
            {
                using (var z = ZipFile.OpenRead(f))
                {
                    foreach (var e in z.Entries)
                    {
                        string n = e.FullName.Replace('\\', '/');
                        string low = n.ToLowerInvariant();
                        if (low == "pack.json")
                        {
                            try
                            {
                                var m = JsonSerializer.Deserialize<Meta>(ReadEntry(e));
                                if (m != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(m.name)) p.name = m.name.Trim();
                                    p.author = (m.author ?? "").Trim(); p.version = (m.version ?? "").Trim(); p.description = (m.description ?? "").Trim();
                                }
                            }
                            catch { p.error = "pack.json could not be read"; }
                        }
                        else if (low == "cover.png") p.cover = true;
                        else if (low.EndsWith(".story") && e.Length < 2000000)
                        {
                            string sid = Clean(Path.GetFileNameWithoutExtension(n));
                            if (sid.Length == 0) continue;
                            string text = ReadEntry(e);
                            p.stories.Add(new StoryInfo { id = Prefix + p.id + "--" + sid, title = TitleOf(text, sid) });
                            foreach (var b in BackgroundsIn(text)) if (!p.backgrounds.Contains(b)) p.backgrounds.Add(b);
                        }
                    }
                }
            }
            catch (Exception ex) { p.error = "not a readable pack (" + ex.Message + ")"; }
            foreach (var b in p.backgrounds) { bool ok; try { ok = BackgroundAvailable(b); } catch { ok = true; } if (!ok) p.missing.Add(b); }
            p.stories.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));
            list.Add(p);
        }
        list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    public static string ReadStory(string id)
    {
        string pk, sid;
        if (!Split(id, out pk, out sid)) return null;
        string f = FileFor(pk);
        if (f == null) return null;
        try
        {
            using (var z = ZipFile.OpenRead(f))
                foreach (var e in z.Entries)
                {
                    string n = e.FullName.Replace('\\', '/');
                    if (n.ToLowerInvariant().EndsWith(".story") && Clean(Path.GetFileNameWithoutExtension(n)) == sid) return ReadEntry(e);
                }
        }
        catch { }
        return null;
    }

    public static byte[] Cover(string packId)
    {
        string f = FileFor(Clean(packId ?? ""));
        if (f == null) return null;
        try
        {
            using (var z = ZipFile.OpenRead(f))
            {
                var e = z.GetEntry("cover.png");
                if (e == null || e.Length > 8000000) return null;
                using (var s = e.Open()) using (var ms = new MemoryStream()) { s.CopyTo(ms); return ms.ToArray(); }
            }
        }
        catch { return null; }
    }

    // builds <name>.storypack in jun_stories\made_packs from some of your own stories; returns the path, or throws with a reason
    public static string Make(string name, string author, string version, string description, List<string> storyIds, byte[] coverPng)
    {
        string pid = Clean(name ?? "");
        if (pid.Length == 0) throw new Exception("give the pack a name (letters, digits, spaces)");
        if (storyIds == null || storyIds.Count == 0) throw new Exception("pick at least one story");
        if (coverPng != null && (coverPng.Length < 8 || coverPng[0] != 0x89 || coverPng[1] != (byte)'P')) throw new Exception("the cover must be a PNG picture");
        if (coverPng != null && coverPng.Length > 4000000) throw new Exception("the cover is too big (keep it under 4 MB)");
        var texts = new List<KeyValuePair<string, string>>();
        foreach (var raw in storyIds)
        {
            string sid = StoryScript.SafeId(raw);
            if (sid.Length == 0 || IsPackId(sid)) continue;          // only your own stories go into a pack
            string text = StoryScript.ReadText(sid);
            if (text != null && !texts.Exists(x => x.Key == sid)) texts.Add(new KeyValuePair<string, string>(sid, text));
        }
        if (texts.Count == 0) throw new Exception("none of the picked stories could be read");
        string outDir = Path.Combine(StoryScript.Dir, "made_packs");
        Directory.CreateDirectory(outDir);
        string path = Path.Combine(outDir, pid + Ext);
        if (File.Exists(path)) File.Delete(path);
        using (var z = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var meta = JsonSerializer.Serialize(new { name = name.Trim(), author = (author ?? "").Trim(), version = string.IsNullOrWhiteSpace(version) ? "1.0" : version.Trim(), description = (description ?? "").Trim() }, new JsonSerializerOptions { WriteIndented = true });
            using (var w = new StreamWriter(z.CreateEntry("pack.json").Open(), new UTF8Encoding(false))) w.Write(meta);
            foreach (var kv in texts)
                using (var w = new StreamWriter(z.CreateEntry("stories/" + kv.Key + ".story").Open(), new UTF8Encoding(false))) w.Write(kv.Value);
            if (coverPng != null) using (var s = z.CreateEntry("cover.png").Open()) s.Write(coverPng, 0, coverPng.Length);
        }
        return path;
    }
}
