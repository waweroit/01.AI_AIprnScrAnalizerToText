using System.Runtime.InteropServices;

namespace AIprnScrAnalizerToText;

public sealed class HotkeyService : NativeWindow, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int MOD_ALT = 0x0001;
    private const int MOD_CONTROL = 0x0002;
    private const int VK_I = 0x49;
    private const int HOTKEY_ID = 1;
    public event EventHandler? HotkeyPressed;

    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    public void Register() { CreateHandle(new CreateParams()); if (!RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_I)) throw new InvalidOperationException("Nie udalo sie zarejestrowac skrótu CTRL+ALT+I."); }
    protected override void WndProc(ref Message m) { if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID) HotkeyPressed?.Invoke(this, EventArgs.Empty); base.WndProc(ref m); }
    public void Dispose() { if (Handle != IntPtr.Zero) UnregisterHotKey(Handle, HOTKEY_ID); DestroyHandle(); }
}
