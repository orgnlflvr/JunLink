using Microsoft.Win32;
namespace JunLinkPet;

static class Program
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    // older versions were called MdrgPet: their autorun entry is moved over once
    static void MigrateAutorun()
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (k == null || k.GetValue("MdrgPet") == null) return;
                k.DeleteValue("MdrgPet", false);
                k.SetValue("JunLinkPet", "\"" + Environment.ProcessPath + "\"");
            }
        }
        catch { }
    }

    public static bool GetAutorun()
    {
        try { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue("JunLinkPet") != null; } catch { return false; }
    }
    public static void SetAutorun(bool on)
    {
        try
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) k.SetValue("JunLinkPet", "\"" + Environment.ProcessPath + "\""); else k.DeleteValue("JunLinkPet", false);
            }
        }
        catch { }
    }

    [STAThread]
    static void Main(string[] args)
    {
        bool created;
        using (var mutex = new System.Threading.Mutex(true, "JunLinkPet_single_instance", out created))
        {
            if (!created) return;                                      // already running (look in the tray)
            MigrateAutorun();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var s = Settings.Load();
            Application.Run(new PetForm(s));
        }
    }
}
