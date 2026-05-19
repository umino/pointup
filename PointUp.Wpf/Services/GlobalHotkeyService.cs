using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using PointUp.Core.Interfaces;
using PointUp.Core.Models;

namespace PointUp.Wpf.Services;

public class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int MOD_ALT = 0x0001;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int MOD_WIN = 0x0008;
    private const int TOGGLE_ID = 9001;
    private const int CLEAR_ID = 9002;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private bool _registered;

    public event EventHandler? ToggleRequested;
    public event EventHandler? ClearRequested;

    public void Register(ShortcutDefinition toggle, ShortcutDefinition clear)
    {
        EnsureHwnd();
        Unregister();
        RegisterHotKey(_hwndSource!.Handle, TOGGLE_ID, ToModifiers(toggle), ToVk(toggle.Key));
        RegisterHotKey(_hwndSource!.Handle, CLEAR_ID, ToModifiers(clear), ToVk(clear.Key));
        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered || _hwndSource == null) return;
        UnregisterHotKey(_hwndSource.Handle, TOGGLE_ID);
        UnregisterHotKey(_hwndSource.Handle, CLEAR_ID);
        _registered = false;
    }

    private void EnsureHwnd()
    {
        if (_hwndSource != null) return;
        var p = new HwndSourceParameters("PointUpHotkeyWnd")
        {
            WindowStyle = 0,
            ExtendedWindowStyle = 0,
            PositionX = 0, PositionY = 0,
            Width = 0, Height = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };
        _hwndSource = new HwndSource(p);
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == TOGGLE_ID) ToggleRequested?.Invoke(this, EventArgs.Empty);
            else if (id == CLEAR_ID) ClearRequested?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static int ToModifiers(ShortcutDefinition d)
    {
        int m = 0;
        if (d.Ctrl) m |= MOD_CONTROL;
        if (d.Shift) m |= MOD_SHIFT;
        if (d.Alt) m |= MOD_ALT;
        if (d.Win) m |= MOD_WIN;
        return m;
    }

    private static int ToVk(string keyName)
    {
        if (Enum.TryParse<Key>(keyName, ignoreCase: true, out var key))
            return KeyInterop.VirtualKeyFromKey(key);
        return 0;
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
