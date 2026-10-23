using System.Text.Json;
namespace JunLinkPet;

class Settings
{
    public string Url { get; set; } = "http://127.0.0.1:8765";
    public int Height { get; set; } = 450;            // how tall she is on your desktop, in pixels
    public int X { get; set; } = -1;
    public int Y { get; set; } = -1;
    public bool TopMost { get; set; } = true;
    public bool Premultiplied { get; set; } = true;   // "fix dark edges": treat the picture's colours as already multiplied by alpha
    public bool Talk { get; set; } = true;            // she answers clicks with a short line
    public bool Paused { get; set; } = false;
    public bool LaunchGame { get; set; } = false;     // start the game (minimized) with the pet
    public string GameExe { get; set; } = "";
    public string GameArgs { get; set; } = "";

    static string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JunLinkPet", "settings.json"); } }
    static string OldPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MdrgPet", "settings.json"); } }   // before the rename
    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(File.Exists(FilePath) || !File.Exists(OldPath) ? FilePath : OldPath)) ?? new Settings(); } catch { return new Settings(); }
    }
    public void Save()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)); File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); } catch { }
    }
}
