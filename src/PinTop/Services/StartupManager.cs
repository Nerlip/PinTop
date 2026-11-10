using Microsoft.Win32;

namespace PinTop.Services;

/// <summary>Manages the "Run at Windows startup" registry value.</summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PinTop";

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (enabled)
            {
                var executable = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(executable))
                {
                    key.SetValue(ValueName, $"\"{executable}\"");
                }
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            // Silently ignore: startup registration is optional.
        }
    }
}
