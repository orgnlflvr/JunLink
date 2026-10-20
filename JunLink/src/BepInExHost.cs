using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

// The BepInEx 6 (IL2CPP) side. This is the only file that knows about BepInEx.

class BepConfig : HostConfig
{
    readonly ConfigFile file;
    public BepConfig(ConfigFile f) { file = f; }
    public override Setting<T> Bind<T>(string section, string key, T defaultValue, string description)
    {
        var e = file.Bind<T>(section, key, defaultValue, description);
        return new Setting<T>(() => e.Value, v => e.Value = v);
    }
}

class BepLog : HostLog
{
    readonly ManualLogSource src;
    public BepLog(ManualLogSource s) { src = s; }
    public override void LogInfo(object m) { src.LogInfo(m); }
    public override void LogWarning(object m) { src.LogWarning(m); }
    public override void LogError(object m) { src.LogError(m); }
}

[BepInPlugin("local.mdrgpanel", "Jun Link", "1.0.0")]   // GUID kept so the old BepInEx config file still applies
public class PanelPlugin : BasePlugin
{
    public override void Load()
    {
        Host.Init(new BepConfig(Config), new BepLog(Log), Paths.BepInExRootPath);
        AddComponent<PanelBehaviour>();
        Host.StartServer();
    }
}
