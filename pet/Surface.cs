using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace JunLinkPet;

// A 32-bit picture with per-pixel alpha that Windows can show directly as a see-through window.
sealed class Surface : IDisposable
{
    public readonly int W, H;
    readonly IntPtr hdcMem, hbmp, oldObj;
    public readonly Bitmap Bmp;
    public readonly Graphics G;

    public Surface(int w, int h)
    {
        W = w; H = h;
        var screen = Native.GetDC(IntPtr.Zero);
        hdcMem = Native.CreateCompatibleDC(screen);
        var bi = new Native.BITMAPINFOHEADER { biSize = 40, biWidth = w, biHeight = -h, biPlanes = 1, biBitCount = 32, biCompression = 0 };
        IntPtr bits;
        hbmp = Native.CreateDIBSection(hdcMem, ref bi, 0, out bits, IntPtr.Zero, 0);
        oldObj = Native.SelectObject(hdcMem, hbmp);
        Native.ReleaseDC(IntPtr.Zero, screen);
        Bmp = new Bitmap(w, h, w * 4, PixelFormat.Format32bppPArgb, bits);   // draws straight into the DIB's memory
        G = Graphics.FromImage(Bmp);
        G.SmoothingMode = SmoothingMode.AntiAlias;
        G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        G.InterpolationMode = InterpolationMode.HighQualityBicubic;
        G.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }

    public void Clear() { G.Clear(Color.Transparent); }

    public void Present(IntPtr hwnd, int x, int y)
    {
        var dst = new Native.POINT { x = x, y = y };
        var size = new Native.SIZE { cx = W, cy = H };
        var src = new Native.POINT();
        var bf = new Native.BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };   // AC_SRC_ALPHA
        var screen = Native.GetDC(IntPtr.Zero);
        Native.UpdateLayeredWindow(hwnd, screen, ref dst, ref size, hdcMem, ref src, 0, ref bf, 2);                      // ULW_ALPHA
        Native.ReleaseDC(IntPtr.Zero, screen);
    }

    public void Dispose()
    {
        G.Dispose(); Bmp.Dispose();
        Native.SelectObject(hdcMem, oldObj); Native.DeleteObject(hbmp); Native.DeleteDC(hdcMem);
    }
}
