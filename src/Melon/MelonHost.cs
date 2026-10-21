using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

// The MelonLoader side (the only file that knows about MelonLoader). Built by Melon\MdrgPanel.Melon.csproj.
// One source, two mods:
//   -p:Mod=Story            -> Story Creator   StoryCreator.dll       stories, minigames, stage, story packs
//   (default)               -> Jun Link        JunLink.dll            AI chat with Jun, memory, greeting, diary, pet
//   (default) -p:Private=true -> Jun Link private  JunLink.Private.dll  + money/stat editor, never shared

#if STORY_MOD
[assembly: MelonInfo(typeof(ModEntry), "Story Creator", "1.0.0", "Original Flavour", "https://github.com/orgnlflvr/StoryCreator")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]
#elif PRIVATE_BUILD
[assembly: MelonInfo(typeof(ModEntry), "Jun Link (private)", "1.1.0", "Original Flavour", "https://github.com/orgnlflvr/JunLink")]
[assembly: System.Reflection.AssemblyVersion("1.1.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.1.0.0")]
#else
[assembly: MelonInfo(typeof(ModEntry), "Jun Link", "1.1.0", "Original Flavour", "https://github.com/orgnlflvr/JunLink")]
[assembly: System.Reflection.AssemblyVersion("1.1.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.1.0.0")]
#endif
[assembly: MelonGame("IncontinentCell", "My Dystopian Robot Girlfriend")]

class MelonConfig : HostConfig
{
    readonly Dictionary<string, MelonPreferences_Category> cats = new Dictionary<string, MelonPreferences_Category>();
    readonly string file;
    public MelonConfig(string file) { this.file = file; }

    // MelonLoader keeps one list of categories for every mod, so each mod needs its own prefix.
    // Jun Link keeps the old "MdrgPanel_" prefix so settings from MDRG Panel / Jun Link 1.0 carry over.
#if STORY_MOD
    const string Prefix = "StoryCreator_";
#else
    const string Prefix = "MdrgPanel_";
#endif

    public override Setting<T> Bind<T>(string section, string key, T defaultValue, string description)
    {
        MelonPreferences_Category cat;
        if (!cats.TryGetValue(section, out cat))
        {
            cat = MelonPreferences.CreateCategory(Prefix + section, section);
            cat.SetFilePath(file);              // one file for all sections of this mod
            cats[section] = cat;
        }
        var e = cat.CreateEntry<T>(key, defaultValue, key, description);
        return new Setting<T>(() => e.Value, v => e.Value = v);
    }

    public override void Save() { MelonPreferences.Save(); }
}

class MelonLog : HostLog
{
    public override void LogInfo(object m) { MelonLogger.Msg(m == null ? "" : m.ToString()); }
    public override void LogWarning(object m) { MelonLogger.Warning(m == null ? "" : m.ToString()); }
    public override void LogError(object m) { MelonLogger.Error(m == null ? "" : m.ToString()); }
}

public class ModEntry : MelonMod
{
    GameObject holder;
    bool off;   // true when a copy that would clash is in Mods: this one stays out of the way

    static string Mods(string f) { return Path.Combine(MelonEnvironment.ModsDirectory, f); }

    static Version FileVer(string path)
    {
        // Jun Link 1.0 had no version number in the DLL (reads as 0.0.0.0); 1.1 and newer have one
        try { return System.Reflection.AssemblyName.GetAssemblyName(path).Version ?? new Version(0, 0); }
        catch { return new Version(0, 0); }
    }

    // Copies that would fight over the same keys, pages or story player.
    static string Clash()
    {
        if (File.Exists(Mods("MdrgPanelML.dll"))) return "the old MDRG Panel (MdrgPanelML.dll) is in Mods; remove it";
#if STORY_MOD
        foreach (var f in new[] { "JunLink.dll", "JunLink.Private.dll" })
            if (File.Exists(Mods(f)) && FileVer(Mods(f)) < new Version(1, 1))
                return f + " is Jun Link 1.0, which still has the stories built in; update Jun Link to 1.1 or newer";
#elif !PRIVATE_BUILD
        if (File.Exists(Mods("JunLink.Private.dll"))) return "the private build (JunLink.Private.dll) is also in Mods; keep only one Jun Link DLL";
#endif
        return null;
    }

    static void CopyDir(string from, string to)
    {
        if (!Directory.Exists(from)) return;
        Directory.CreateDirectory(to);
        foreach (var d in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, d)));
        foreach (var f in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            string t = Path.Combine(to, Path.GetRelativePath(from, f));
            if (!File.Exists(t)) File.Copy(f, t, false);
        }
    }

    // First start: bring over what this mod owns from the older all-in-one folders (the old folders stay as they are).
    static void Migrate(string user)
    {
        try
        {
            string newDir = Path.Combine(user, ModFolder);
            if (Directory.Exists(newDir)) return;
#if STORY_MOD
            foreach (var old in new[] { "JunLink", "MdrgPanel" })
            {
                string o = Path.Combine(user, old);
                if (!Directory.Exists(o)) continue;
                CopyDir(Path.Combine(o, "jun_stories"), Path.Combine(newDir, "jun_stories"));
                CopyDir(Path.Combine(o, "jun_backgrounds"), Path.Combine(newDir, "jun_backgrounds"));
                CopyDir(Path.Combine(o, "tools"), Path.Combine(newDir, "tools"));
                if (File.Exists(Path.Combine(o, "logo.png"))) File.Copy(Path.Combine(o, "logo.png"), Path.Combine(newDir, "logo.png"), false);
                MelonLogger.Msg("Story Creator: copied your stories and backgrounds from UserData\\" + old + " (the old folder is kept as it is).");
                break;
            }
#else
            string oldDir = Path.Combine(user, "MdrgPanel");
            if (Directory.Exists(oldDir))
            {
                CopyDir(oldDir, newDir);
                MelonLogger.Msg("Jun Link: copied your files from UserData\\MdrgPanel to UserData\\JunLink (the old folder is kept as a backup).");
            }
            string oldCfg = Path.Combine(user, "MdrgPanel.cfg"), newCfg = Path.Combine(user, "JunLink.cfg");
            if (File.Exists(oldCfg) && !File.Exists(newCfg))
            {
                File.Copy(oldCfg, newCfg, false);
                MelonLogger.Msg("Jun Link: copied your settings from MdrgPanel.cfg to JunLink.cfg.");
            }
#endif
        }
        catch (Exception ex) { MelonLogger.Warning(ModName + ": could not copy the older files: " + ex.Message); }
    }

#if STORY_MOD
    const string ModName = "Story Creator", ModFolder = "StoryCreator";
#else
    const string ModName = "Jun Link", ModFolder = "JunLink";
#endif

    public override void OnInitializeMelon()
    {
        string clash = Clash();
        if (clash != null)
        {
            off = true;
            MelonLogger.Error(ModName + " is switched off because " + clash + ".");
            return;
        }
        string user = MelonEnvironment.UserDataDirectory;
        Migrate(user);
        string root = Path.Combine(user, ModFolder);
        Directory.CreateDirectory(root);
#if PRIVATE_BUILD && !STORY_MOD
        MelonLogger.Warning("Jun Link PRIVATE build (with the money/stat editor). Do not share this DLL.");
#endif
        Host.Init(new MelonConfig(Path.Combine(user, ModFolder + ".cfg")), new MelonLog(), root);
        try { MelonPreferences.Save(); } catch { }           // writes the file with the defaults on the first run
        Host.StartServer();
    }

    public override void OnLateInitializeMelon() { MakeHolder(); }
    public override void OnSceneWasInitialized(int buildIndex, string sceneName) { MakeHolder(); }

    void MakeHolder()
    {
        try
        {
            if (off || holder != null) return;
            holder = new GameObject(ModFolder);
            UnityEngine.Object.DontDestroyOnLoad(holder);
            holder.hideFlags = HideFlags.HideAndDontSave;
#if STORY_MOD
            holder.AddComponent<StoryCreatorBehaviour>();
#else
            holder.AddComponent<PanelBehaviour>();
#endif
        }
        catch (Exception ex) { MelonLogger.Error(ModName + " could not start its update loop: " + ex); }
    }
}
