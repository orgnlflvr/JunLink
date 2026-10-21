using System;
using Il2CppInterop.Runtime.Injection;

// Loader-neutral glue. Everything in MdrgPanel.cs talks to the mod loader ONLY through the types in this file,
// so the loader-specific part is one small file (BepInExHost.cs today; a MelonLoader one could sit next to it).

// A config value. Reads go through to the loader's own config entry, so editing the config file while the game runs still works.
public sealed class Setting<T>
{
    readonly Func<T> get; readonly Action<T> set;
    public Setting(Func<T> get, Action<T> set) { this.get = get; this.set = set; }
    public T Value { get { return get(); } set { set(value); } }
}

public abstract class HostConfig
{
    public abstract Setting<T> Bind<T>(string section, string key, T defaultValue, string description);
    public virtual void Save() { }      // BepInEx saves by itself when a value changes; MelonLoader needs MelonPreferences.Save()
}

// Every setting the mod binds, so the /settings page can list and change them (v5.16).
public sealed class SettingInfo
{
    public string Section, Key, Description, Type;
    public object Default;
    public Func<object> Get;
    public Action<object> Set;
}

public static class SettingsRegistry
{
    public static readonly System.Collections.Generic.List<SettingInfo> All = new System.Collections.Generic.List<SettingInfo>();
    public static HostConfig Config;
}

// wraps the loader's config and writes down each setting as it is bound
class RecordingConfig : HostConfig
{
    readonly HostConfig inner;
    public RecordingConfig(HostConfig inner) { this.inner = inner; }
    public override Setting<T> Bind<T>(string section, string key, T defaultValue, string description)
    {
        var s = inner.Bind<T>(section, key, defaultValue, description);
        string type = typeof(T) == typeof(bool) ? "bool" : typeof(T) == typeof(int) ? "int" : typeof(T) == typeof(float) ? "float" : "string";
        SettingsRegistry.All.Add(new SettingInfo
        {
            Section = section, Key = key, Description = description, Type = type, Default = defaultValue,
            Get = () => s.Value, Set = v => s.Value = (T)v
        });
        return s;
    }
    public override void Save() { inner.Save(); }
}

public abstract class HostLog
{
    public abstract void LogInfo(object message);
    public abstract void LogWarning(object message);
    public abstract void LogError(object message);
}

public static class Host
{
    // folder where the mod keeps its files (jun_stories, jun_memory.txt, chat_log.txt ...); the loader sets it
    public static string RootPath = ".";

    // how to name our folder and the log in messages and pages (v5.20f): MelonLoader keeps the mod's files in UserData\JunLink, BepInEx in BepInEx
    public static bool IsMelon
    {
        get
        {
            try { var d = new System.IO.DirectoryInfo(RootPath); return d.Parent != null && d.Parent.Name.Equals("UserData", StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
    }
    public static string RootLabel { get { return IsMelon ? "UserData\\" + Mod.Folder : "BepInEx"; } }
    public static string ConfigLabel { get { return IsMelon ? "UserData\\" + Mod.Folder + ".cfg" : "BepInEx\\config\\local.mdrgpanel.cfg"; } }
    public static string LogLabel { get { return IsMelon ? "MelonLoader\\Latest.log" : "BepInEx\\LogOutput.log"; } }

    // called by the loader file first thing in its start-up
    public static void Init(HostConfig config, HostLog log, string rootPath)
    {
        RootPath = rootPath;
        var rec = new RecordingConfig(config);
        SettingsRegistry.Config = rec;
        Cfg.Init(rec);
        Cfg.Log = log;
#if STORY_MOD
        ClassInjector.RegisterTypeInIl2Cpp<StoryCreatorBehaviour>();
#else
        ClassInjector.RegisterTypeInIl2Cpp<PanelBehaviour>();
#endif
    }

    // called by the loader file after it has put PanelBehaviour into the game (so that Update() runs)
    public static void StartServer() { PanelServer.Start(); }
}
