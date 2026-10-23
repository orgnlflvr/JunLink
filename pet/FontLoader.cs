using System.Drawing;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
namespace JunLinkPet;

// Loads the Montserrat fonts that are packed inside the exe (no installing, no internet). Falls back to Segoe UI if anything goes wrong.
static class FontLoader
{
    static readonly PrivateFontCollection pfc = new PrivateFontCollection();
    static bool loaded;

    static void Load()
    {
        if (loaded) return; loaded = true;
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) continue;
                using (var st = asm.GetManifestResourceStream(n))
                {
                    var data = new byte[st.Length]; st.Read(data, 0, data.Length);
                    IntPtr p = Marshal.AllocCoTaskMem(data.Length);          // must stay allocated for as long as the fonts are used
                    Marshal.Copy(data, 0, p, data.Length);
                    pfc.AddMemoryFont(p, data.Length);
                }
            }
        }
        catch { }
    }

    public static Font Get(string family, float points)
    {
        Load();
        try
        {
            foreach (var f in pfc.Families) if (f.Name == family) return new Font(f, points, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch { }
        return new Font("Segoe UI", points, family.Contains("Bold") ? FontStyle.Bold : FontStyle.Regular);
    }

    public static Font Body(float points) { return Get("Montserrat SemiBold", points); }
    public static Font Heavy(float points) { return Get("Montserrat ExtraBold", points); }
}
