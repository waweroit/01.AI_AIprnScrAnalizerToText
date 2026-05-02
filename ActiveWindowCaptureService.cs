using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AIprnScrAnalizerToText;

public sealed class ActiveWindowCaptureService
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public byte[] CaptureActiveWindow()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) throw new InvalidOperationException("Nie znaleziono aktywnego okna.");
        if (IsIconic(hwnd)) throw new InvalidOperationException("Aktywne okno jest zminimalizowane.");
        if (!GetWindowRect(hwnd, out var r)) throw new InvalidOperationException("Nie udalo sie pobrac rozmiaru okna.");
        var w = Math.Max(1, r.Right - r.Left);
        var h = Math.Max(1, r.Bottom - r.Top);
        using var bmp = new Bitmap(w, h);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
