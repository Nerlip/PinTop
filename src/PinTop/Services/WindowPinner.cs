using PinTop.Interop;

namespace PinTop.Services;

/// <summary>Toggles the WS_EX_TOPMOST style of top-level windows.</summary>
internal static class WindowPinner
{
    public static bool IsTopmost(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        return (style & NativeMethods.WS_EX_TOPMOST) != 0;
    }

    public static bool SetTopmost(IntPtr hwnd, bool topmost)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd))
        {
            return false;
        }

        var insertAfter = topmost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST;
        return NativeMethods.SetWindowPos(
            hwnd,
            insertAfter,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    /// <summary>Flips the topmost state and reports the resulting state.</summary>
    public static bool Toggle(IntPtr hwnd, out bool isPinned)
    {
        isPinned = !IsTopmost(hwnd);
        if (!SetTopmost(hwnd, isPinned))
        {
            isPinned = IsTopmost(hwnd);
            return false;
        }

        return true;
    }
}
