using System.Diagnostics;
using System.Text;
using PinTop.Interop;

namespace PinTop.Services;

/// <summary>Finds the window the user is pointing at and filters out windows that make no sense to pin.</summary>
internal static class WindowTargeting
{
    private static readonly HashSet<string> IgnoredClasses = new(StringComparer.Ordinal)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd",
        "Windows.UI.Core.CoreWindow",
        "ApplicationManager_DesktopShellWindow",
        "XamlExplorerHostIslandWindow",
        "MultitaskingViewFrame",
        "ForegroundStaging",
        "TaskListThumbnailWnd",
        "TopLevelWindowForOverflowXamlIsland",
        "Windows.Internal.Shell.TabProxyWindow",
    };

    public static IntPtr GetWindowUnderCursor()
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return IntPtr.Zero;
        }

        var hwnd = NativeMethods.WindowFromPoint(point);
        return hwnd == IntPtr.Zero ? IntPtr.Zero : GetRootWindow(hwnd);
    }

    public static IntPtr GetActiveWindow() => GetRootWindow(NativeMethods.GetForegroundWindow());

    public static IntPtr GetRootWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var root = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
        return root != IntPtr.Zero ? root : hwnd;
    }

    public static bool IsEligible(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !NativeMethods.IsWindow(hwnd) || !NativeMethods.IsWindowVisible(hwnd))
        {
            return false;
        }

        if (hwnd == NativeMethods.GetShellWindow())
        {
            return false;
        }

        var className = GetClassName(hwnd);
        if (IgnoredClasses.Contains(className))
        {
            return false;
        }

        if (IsOwnedByCurrentProcess(hwnd) || IsCloaked(hwnd))
        {
            return false;
        }

        return true;
    }

    public static bool IsOwnedByCurrentProcess(IntPtr hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return processId == (uint)Environment.ProcessId;
    }

    public static bool IsCloaked(IntPtr hwnd)
    {
        var result = NativeMethods.DwmGetWindowAttribute(hwnd, NativeMethods.DWMWA_CLOAKED, out var cloaked, sizeof(int));
        return result == 0 && cloaked != 0;
    }

    public static string GetTitle(IntPtr hwnd)
    {
        var length = NativeMethods.GetWindowTextLength(hwnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(length + 1);
        var copied = NativeMethods.GetWindowText(hwnd, buffer, buffer.Capacity);
        return copied > 0 ? buffer.ToString(0, copied) : string.Empty;
    }

    public static string GetClassName(IntPtr hwnd)
    {
        var buffer = new StringBuilder(256);
        var copied = NativeMethods.GetClassName(hwnd, buffer, buffer.Capacity);
        return copied > 0 ? buffer.ToString(0, copied) : string.Empty;
    }

    public static string GetProcessName(IntPtr hwnd)
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return string.Empty;
        }
    }

    public static IReadOnlyList<IntPtr> EnumerateTopLevelWindows()
    {
        var result = new List<IntPtr>();

        // The delegate must stay alive for the duration of the enumeration.
        NativeMethods.EnumWindowsProc callback = (hwnd, _) =>
        {
            if (IsEligible(hwnd))
            {
                result.Add(hwnd);
            }

            return true;
        };

        NativeMethods.EnumWindows(callback, IntPtr.Zero);
        GC.KeepAlive(callback);
        return result;
    }
}
