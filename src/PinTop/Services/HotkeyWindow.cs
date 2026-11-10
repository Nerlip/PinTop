using PinTop.Interop;

namespace PinTop.Services;

/// <summary>A hidden message-only window that owns the global hotkey.</summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 0x5049;

    private static readonly int TaskbarCreatedMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");

    private bool _registered;

    public event EventHandler? HotkeyPressed;

    public event EventHandler? TaskbarCreated;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "PinTop.HotkeyWindow",
            Parent = NativeMethods.HWND_MESSAGE,
        });
    }

    public bool TryRegister(uint modifiers, Keys key)
    {
        Unregister();

        if (key == Keys.None)
        {
            return true;
        }

        _registered = NativeMethods.RegisterHotKey(
            Handle,
            HotkeyId,
            modifiers | NativeMethods.MOD_NOREPEAT,
            (uint)key);

        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(Handle, HotkeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (TaskbarCreatedMessage != 0 && m.Msg == TaskbarCreatedMessage)
        {
            TaskbarCreated?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();

        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }
    }
}
