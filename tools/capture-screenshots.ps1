#requires -Version 5.1
<#
    Renders the real PinTop windows and saves screenshots for the README.
    Development helper: requires a Debug build and an interactive desktop.
#>
param(
    [string]$Configuration = 'Debug',
    [string]$OutDirectory = (Join-Path $PSScriptRoot '..\docs')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

public static class Shooter
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out RECT value, int size);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

    public static IntPtr FindTopLevelWindow(uint pid)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
        {
            uint owner;
            GetWindowThreadProcessId(hWnd, out owner);
            if (owner != pid || !IsWindowVisible(hWnd) || GetWindowTextLength(hWnd) == 0)
            {
                return true;
            }

            found = hWnd;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    public static bool Capture(IntPtr hWnd, string path)
    {
        RECT rect;
        if (!GetWindowRect(hWnd, out rect)) return false;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return false;

        using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                PrintWindow(hWnd, hdc, 2);
                graphics.ReleaseHdc(hdc);
            }

            int middleY = height / 2;
            int right = width - 1;
            while (right > 0 && IsBlack(bitmap.GetPixel(right, middleY))) right--;

            int middleX = width / 2;
            int bottom = height - 1;
            while (bottom > 0 && IsBlack(bitmap.GetPixel(middleX, bottom))) bottom--;

            var crop = new Rectangle(0, 0, right + 1, bottom + 1);
            using (var result = bitmap.Clone(crop, PixelFormat.Format32bppArgb))
            {
                result.Save(path, ImageFormat.Png);
            }
        }

        return true;
    }

    private static bool IsBlack(Color pixel)
    {
        return pixel.R <= 8 && pixel.G <= 8 && pixel.B <= 8;
    }
}
"@

function Wait-ForWindow {
    param([int]$ProcessId, [int]$TimeoutSeconds = 15)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $handle = [Shooter]::FindTopLevelWindow([uint32]$ProcessId)
        if ($handle -ne [IntPtr]::Zero) {
            return $handle
        }
        Start-Sleep -Milliseconds 200
    }

    throw "Window for process $ProcessId did not appear"
}

$exe = Join-Path $PSScriptRoot "..\src\PinTop\bin\$Configuration\net10.0-windows\PinTop.exe"
if (-not (Test-Path $exe)) { throw "Build first: $exe not found" }
if (-not (Test-Path $OutDirectory)) { New-Item -ItemType Directory -Path $OutDirectory -Force | Out-Null }

Get-Process PinTop -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

# Pin Notepad directly so the window list shows a checked row.
$notepad = Get-Process notepad -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } |
    Select-Object -First 1

if ($notepad) {
    $noteHandle = $notepad.MainWindowHandle
    [Shooter]::ShowWindow($noteHandle, 9) | Out-Null
    [Shooter]::MoveWindow($noteHandle, 120, 160, 820, 560, $true) | Out-Null
    [Shooter]::SetWindowPos($noteHandle, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null
}

$env:PINTOP_UI_PREVIEW = 'windows'
$listProcess = Start-Process $exe -PassThru
$listHandle = Wait-ForWindow -ProcessId $listProcess.Id
Start-Sleep -Milliseconds 900
if (-not [Shooter]::Capture($listHandle, (Join-Path $OutDirectory 'screenshot-windows.png'))) { throw 'Capture failed (windows)' }
[Shooter]::PostMessage($listHandle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
Write-Host 'Saved screenshot-windows.png'

Start-Sleep -Milliseconds 800
$env:PINTOP_UI_PREVIEW = 'settings'
$settingsProcess = Start-Process $exe -PassThru
$settingsHandle = Wait-ForWindow -ProcessId $settingsProcess.Id
Start-Sleep -Milliseconds 900
if (-not [Shooter]::Capture($settingsHandle, (Join-Path $OutDirectory 'screenshot-settings.png'))) { throw 'Capture failed (settings)' }
[Shooter]::PostMessage($settingsHandle, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
Write-Host 'Saved screenshot-settings.png'

Start-Sleep -Milliseconds 600
Get-Process PinTop -ErrorAction SilentlyContinue | Stop-Process -Force

if ($notepad) {
    [Shooter]::SetWindowPos($notepad.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x0013) | Out-Null
}

Remove-Item Env:\PINTOP_UI_PREVIEW -ErrorAction SilentlyContinue
Write-Host 'Done.'

