#requires -Version 5.1
<#
    End-to-end check: pins and unpins a Notepad window through PinTop's global hotkey.
    Requires an interactive desktop and the default Ctrl+Alt+T hotkey.
#>
param(
    [string]$ExePath = (Join-Path $PSScriptRoot '..\src\PinTop\bin\Debug\net10.0-windows\PinTop.exe')
)

$ErrorActionPreference = 'Stop'

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class WinTest
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [DllImport("user32.dll")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder name, int max);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    public static int ProbeHotkey()
    {
        // Returns 0 when Ctrl+Alt+T is still free, i.e. PinTop has not registered it yet.
        bool ok = RegisterHotKey(IntPtr.Zero, 4242, 0x4003, 0x54);
        int error = Marshal.GetLastWin32Error();
        if (ok) UnregisterHotKey(IntPtr.Zero, 4242);
        return ok ? 0 : error;
    }

    public static bool IsTopmost(IntPtr hWnd)
    {
        return (GetWindowLongPtr(hWnd, -20).ToInt64() & 0x8) != 0;
    }

    public static void Pin(IntPtr hWnd)
    {
        // HWND_TOP = 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE
        SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010);
    }

    public static void Unpin(IntPtr hWnd)
    {
        SetWindowPos(hWnd, new IntPtr(-2), 0, 0, 0, 0, 0x0002 | 0x0001 | 0x0010);
    }

    public static IntPtr RootAt(int x, int y)
    {
        POINT point;
        point.X = x;
        point.Y = y;
        return GetAncestor(WindowFromPoint(point), 2);
    }

    public static string ClassName(IntPtr hWnd)
    {
        var name = new StringBuilder(256);
        GetClassName(hWnd, name, 256);
        return name.ToString();
    }

    /// <summary>Finds a spot on the primary monitor that is not covered by any window.</summary>
    public static bool FindFreePoint(out int x, out int y)
    {
        int screenWidth = GetSystemMetrics(0);
        int screenHeight = GetSystemMetrics(1);

        for (int candidateY = 160; candidateY < screenHeight - 160; candidateY += 60)
        {
            for (int candidateX = 160; candidateX < screenWidth - 160; candidateX += 60)
            {
                POINT point;
                point.X = candidateX;
                point.Y = candidateY;
                IntPtr root = GetAncestor(WindowFromPoint(point), 2);
                if (root == IntPtr.Zero) continue;

                string className = ClassName(root);
                if (className == "Progman" || className == "WorkerW" || className == "SysListView32")
                {
                    x = candidateX;
                    y = candidateY;
                    return true;
                }
            }
        }

        x = 0;
        y = 0;
        return false;
    }

    public static void PlaceCentered(IntPtr hWnd, int x, int y)
    {
        const int width = 900;
        const int height = 640;
        int screenWidth = GetSystemMetrics(0);
        int screenHeight = GetSystemMetrics(1);
        int left = Math.Max(0, Math.Min(x - width / 2, screenWidth - width));
        int top = Math.Max(0, Math.Min(y - height / 2, screenHeight - height));
        MoveWindow(hWnd, left, top, width, height, true);
    }

    public static string Describe(IntPtr hWnd)
    {
        var text = new StringBuilder(400);
        GetWindowText(hWnd, text, 400);
        uint pid;
        GetWindowThreadProcessId(hWnd, out pid);
        return hWnd + " (pid " + pid + ") '" + text + "' topmost=" + IsTopmost(hWnd);
    }

    public static void PressCtrlAltT()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero);
        keybd_event(0x12, 0, 0, UIntPtr.Zero);
        keybd_event(0x54, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(80);
        keybd_event(0x54, 0, 2, UIntPtr.Zero);
        keybd_event(0x12, 0, 2, UIntPtr.Zero);
        keybd_event(0x11, 0, 2, UIntPtr.Zero);
    }
}
"@

$pintopExe = $ExePath
if (-not (Test-Path $pintopExe)) { throw "Not found: $pintopExe" }

$target = Get-Process notepad -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } |
    Select-Object -First 1

if (-not $target) {
    Start-Process notepad.exe | Out-Null
    $deadline = (Get-Date).AddSeconds(8)
    while (-not $target -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 300
        $target = Get-Process notepad -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowHandle -ne 0 } |
            Select-Object -First 1
    }
}

if (-not $target) { throw 'Could not find a Notepad window' }

$hwnd = $target.MainWindowHandle
[WinTest]::Unpin($hwnd) | Out-Null

# Pick a spot on the desktop that no window covers, then park Notepad on it. That way the
# cursor probe cannot accidentally land on the user's own windows on this live desktop.
$probeX = 0
$probeY = 0
if (-not [WinTest]::FindFreePoint([ref]$probeX, [ref]$probeY)) {
    throw 'Could not find an uncovered spot on the primary monitor.'
}

Write-Host "probe-point=${probeX}x${probeY}"
[WinTest]::ShowWindow($hwnd, 9) | Out-Null
[WinTest]::PlaceCentered($hwnd, $probeX, $probeY) | Out-Null
[WinTest]::SetForegroundWindow($hwnd) | Out-Null
[WinTest]::SetCursorPos($probeX, $probeY) | Out-Null
Start-Sleep -Milliseconds 600

$underCursor = [WinTest]::RootAt($probeX, $probeY)
Write-Host "target: $([WinTest]::Describe($hwnd))"
Write-Host "under cursor: $([WinTest]::Describe($underCursor))"

if ($underCursor -ne $hwnd) {
    throw 'Another window is under the probe point; it moved during setup. Retry the test.'
}

if (-not (Get-Process PinTop -ErrorAction SilentlyContinue)) {
    Start-Process $pintopExe | Out-Null
}

$deadline = (Get-Date).AddSeconds(20)
while ([WinTest]::ProbeHotkey() -eq 0 -and (Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 400
}

$hotkeyReady = [WinTest]::ProbeHotkey() -ne 0
Write-Host "pintop-running=$([bool](Get-Process PinTop -ErrorAction SilentlyContinue)) hotkey-registered=$hotkeyReady"
if (-not $hotkeyReady) {
    throw 'PinTop did not register Ctrl+Alt+T. Is another program using it?'
}

# Raise the target to the front right before pressing: the desktop is live and another
# window could have covered the probe point while we waited for PinTop to start.
[WinTest]::ShowWindow($hwnd, 9) | Out-Null
[WinTest]::SetForegroundWindow($hwnd) | Out-Null
[WinTest]::PlaceCentered($hwnd, $probeX, $probeY) | Out-Null
[WinTest]::SetCursorPos($probeX, $probeY) | Out-Null
Start-Sleep -Milliseconds 500

$underCursor = [WinTest]::RootAt($probeX, $probeY)
if ($underCursor -ne $hwnd) {
    throw "The probe point is covered by $([WinTest]::Describe($underCursor))."
}

# PinTop toggles, so start from the unpinned state and expect pin, then unpin.
$startedUnpinned = -not [WinTest]::IsTopmost($hwnd)
Write-Host "topmost-before-press=$(-not $startedUnpinned)"

[WinTest]::PressCtrlAltT()
Start-Sleep -Milliseconds 900
$pinned = [WinTest]::IsTopmost($hwnd)
Write-Host "topmost-after-first-press=$pinned"

[WinTest]::PressCtrlAltT()
Start-Sleep -Milliseconds 900
$unpinned = -not [WinTest]::IsTopmost($hwnd)
Write-Host "topmost-after-second-press=$(-not $unpinned)"

# Leave the desktop as we found it.
if ([WinTest]::IsTopmost($hwnd)) {
    [WinTest]::Unpin($hwnd) | Out-Null
}

$settingsPath = Join-Path $env:APPDATA 'PinTop\settings.json'
Write-Host "settings-file=$settingsPath exists=$([bool](Test-Path $settingsPath))"

if ($startedUnpinned -and $pinned -and $unpinned) {
    Write-Host 'SMOKE TEST PASSED'
} else {
    Write-Host 'SMOKE TEST FAILED'
    exit 1
}
