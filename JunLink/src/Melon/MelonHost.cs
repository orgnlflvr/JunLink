using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

// The MelonLoader side (the only file that knows about MelonLoader). Built by Melon\MdrgPanel.Melon.csproj.
// Jun Link (formerly MDRG Panel). Public build = JunLink.dll; private build (-p:Private=true) = JunLink.Private.dll.

#if PRIVATE_BUILD
[assembly: MelonInfo(typeof(JunLinkMelon), "Jun Link (private)", "1.0.0", "Original Flavour", "https://github.com/orgnlflvr/JunLink")]
#else
[assembly: MelonInfo(typeof(JunLinkMelon), "Jun Link", "1.0.0", "Original Flavour", "https://github.com/orgnlflvr/JunLink")]
#endif
[assembly: MelonGame("IncontinentCell", "My Dystopian Robot Girlfriend")]

class MelonConfig : HostConfig
{
    readonly Dictionary<string, MelonPreferences_Category> cats = new Dictionary<string, MelonPreferences_Category>();
    readonly string file;
    public MelonConfig(string file) { this.file = file; }

    public override Setting<T> Bind<T>(string section, string key, T defaultValue, string description)
    {
        MelonPreferences_Category cat;
        if (!cats.TryGetValue(section, out cat))
        {
            cat = MelonPreferences.CreateCategory("MdrgPanel_" + section, section);
            cat.SetFilePath(file);              // one file for all sections: UserData\JunLink.cfg (category ids keep the old prefix so old settings carry over)
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

public class JunLinkMelon : MelonMod
{
    GameObject holder;
    bool off;   // true when another copy of the mod is in Mods: this one stays out of the way

    // Only one copy may run (both would fight over the web port and the game's dialogue box).
    static string OtherCopy()
    {
        string mods = MelonEnvironment.ModsDirectory;
        if (File.Exists(Path.Combine(mods, "MdrgPanelML.dll"))) return "the old MDRG Panel (MdrgPanelML.dll)";
#if !PRIVATE_BUILD
        if (File.Exists(Path.Combine(mods, "JunLink.Private.dll"))) return "the private build (JunLink.Private.dll)";
#endif
        return null;
    }

    // First start after the rename: copy the old MDRG Panel files and settings (the old ones stay as a backup).
    static void Migrate(string user)
    {
        try
        {
            string oldDir = Path.Combine(user, "MdrgPanel"), newDir = Path.Combine(user, "JunLink");
            if (Directory.Exists(oldDir) && !Directory.Exists(newDir))
            {
                foreach (var d in Directory.GetDirectories(oldDir, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(Path.Combine(newDir, Path.GetRelativePath(oldDir, d)));
                Directory.CreateDirectory(newDir);
                foreach (var f in Directory.GetFiles(oldDir, "*", SearchOption.AllDirectories))
                    File.Copy(f, Path.Combine(newDir, Path.GetRelativePath(oldDir, f)), false);
                MelonLogger.Msg("Jun Link: copied your files from UserData\\MdrgPanel to UserData\\JunLink (the old folder is kept as a backup).");
            }
            string oldCfg = Path.Combine(user, "MdrgPanel.cfg"), newCfg = Path.Combine(user, "JunLink.cfg");
            if (File.Exists(oldCfg) && !File.Exists(newCfg))
            {
                File.Copy(oldCfg, newCfg, false);
                MelonLogger.Msg("Jun Link: copied your settings from MdrgPanel.cfg to JunLink.cfg.");
            }
        }
        catch (Exception ex) { MelonLogger.Warning("Jun Link: could not copy the old MDRG Panel files: " + ex.Message); }
    }

    public override void OnInitializeMelon()
    {
        string other = OtherCopy();
        if (other != null)
        {
            off = true;
            MelonLogger.Error("Jun Link is switched off because " + other + " is also in the Mods folder. Keep only one of them in Mods.");
            return;
        }
        string user = MelonEnvironment.UserDataDirectory;
        Migrate(user);
        string root = Path.Combine(user, "JunLink");        // jun_stories, jun_memory.txt, chat_log.txt ... live here
        Directory.CreateDirectory(root);
#if PRIVATE_BUILD
        MelonLogger.Warning("Jun Link PRIVATE build (with the money/stat editor). Do not share this DLL.");
#endif
        Host.Init(new MelonConfig(Path.Combine(user, "JunLink.cfg")), new MelonLog(), root);
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
            holder = new GameObject("JunLink");
            UnityEngine.Object.DontDestroyOnLoad(holder);
            holder.hideFlags = HideFlags.HideAndDontSave;
            holder.AddComponent<PanelBehaviour>();
        }
        catch (Exception ex) { MelonLogger.Error("Jun Link could not start its update loop: " + ex); }
    }
}
