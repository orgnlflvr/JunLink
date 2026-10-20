using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

// Settings page (v5.16, untested in game): http://127.0.0.1:8765/settings lists every option of the mod and changes it live.
// Changes are applied on the game's main thread (Tick) and then saved to the loader's config file
// (BepInEx\config\local.mdrgpanel.cfg, or UserData\JunLink.cfg on MelonLoader). No game types here.
public static class SettingsPage
{
    static readonly ConcurrentQueue<KeyValuePair<SettingInfo, object>> pending = new ConcurrentQueue<KeyValuePair<SettingInfo, object>>();
    static readonly HashSet<string> needsRestart = new HashSet<string> { "Panel/Port" };

    // extra check from the game side (hotkey names); returns an error or null
    public static Func<SettingInfo, object, string> Validator;
    // called on the main thread after a value changed
    public static Action<SettingInfo> Changed;

    static SettingInfo Find(string section, string key)
    {
        foreach (var s in SettingsRegistry.All) if (s.Section == section && s.Key == key) return s;
        return null;
    }

    public static string Json()
    {
        var list = new List<object>();
        foreach (var s in SettingsRegistry.All)
        {
            object v; try { v = s.Get(); } catch { v = null; }
            list.Add(new
            {
                section = s.Section, key = s.Key, desc = s.Description, type = s.Type,
                value = v, def = s.Default, restart = needsRestart.Contains(s.Section + "/" + s.Key)
            });
        }
        return JsonSerializer.Serialize(new { settings = list });
    }

    // web thread: checks the value and queues it; returns an error or null
    public static string Set(string section, string key, string raw)
    {
        var s = Find(section ?? "", key ?? "");
        if (s == null) return "unknown setting " + section + "/" + key;
        raw = (raw ?? "").Trim();
        object v;
        var ci = CultureInfo.InvariantCulture;
        switch (s.Type)
        {
            case "bool":
                if (raw == "true") v = true; else if (raw == "false") v = false; else return "use true or false";
                break;
            case "int":
                int iv; if (!int.TryParse(raw, NumberStyles.Integer, ci, out iv)) return "needs a whole number";
                v = iv; break;
            case "float":
                float fv; if (!float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, ci, out fv)) return "needs a number like 0.8";
                v = fv; break;
            default:
                v = raw; break;
        }
        if (s.Type == "int" && (int)v < 0) return "cannot be negative";
        if (s.Type == "float" && ((float)v < 0f || float.IsNaN((float)v))) return "cannot be negative";
        if (s.Section == "Panel" && s.Key == "Port" && ((int)v < 1024 || (int)v > 65535)) return "use a port between 1024 and 65535";
        if (Validator != null) { string err = Validator(s, v); if (err != null) return err; }
        pending.Enqueue(new KeyValuePair<SettingInfo, object>(s, v));
        return null;
    }

    // main thread
    public static void Tick()
    {
        bool any = false;
        KeyValuePair<SettingInfo, object> p;
        while (pending.TryDequeue(out p))
        {
            try
            {
                p.Key.Set(p.Value);
                any = true;
                Cfg.Log.LogInfo("Settings: " + p.Key.Section + "/" + p.Key.Key + " = " + Short(p.Value));
                if (Changed != null) Changed(p.Key);
            }
            catch (Exception ex) { Cfg.Log.LogWarning("Settings: could not set " + p.Key.Key + ": " + ex.Message); }
        }
        if (any) { try { SettingsRegistry.Config.Save(); } catch (Exception ex) { Cfg.Log.LogWarning("Settings: could not save the config file: " + ex.Message); } }
    }

    static string Short(object v)
    {
        string t = Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
        return t.Length > 60 ? t.Substring(0, 60) + "..." : t;
    }
}
