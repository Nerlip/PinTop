using System.Text.Json;
using System.Text.Json.Serialization;
using PinTop.Interop;

namespace PinTop.Services;

internal enum PinTarget
{
    WindowUnderCursor,
    ActiveWindow,
}

internal sealed class AppSettings
{
    public const uint DefaultHotkeyModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;

    public uint HotkeyModifiers { get; set; } = DefaultHotkeyModifiers;

    public Keys HotkeyKey { get; set; } = Keys.T;

    public bool ShowNotifications { get; set; } = true;

    public PinTarget Target { get; set; } = PinTarget.WindowUnderCursor;

    public bool RunAtStartup { get; set; }

    public bool RestoreOnExit { get; set; } = true;

    public bool WelcomeShown { get; set; }

    [JsonIgnore]
    public bool HotkeyEnabled => HotkeyKey != Keys.None;

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PinTop");

    public static string SettingsFile => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
                if (settings is not null)
                {
                    settings.Normalize();
                    return settings;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A broken settings file must never prevent the app from starting.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ignore: settings are a convenience, not a requirement.
        }
    }

    public AppSettings Copy() => (AppSettings)MemberwiseClone();

    private void Normalize()
    {
        var allowedModifiers = NativeMethods.MOD_ALT | NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_WIN;
        HotkeyModifiers &= allowedModifiers;

        if (HotkeyKey != Keys.None && !Enum.IsDefined(HotkeyKey))
        {
            HotkeyKey = Keys.T;
        }

        if (!Enum.IsDefined(Target))
        {
            Target = PinTarget.WindowUnderCursor;
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };
}
