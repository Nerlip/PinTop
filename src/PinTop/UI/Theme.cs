using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using Microsoft.Win32;
using PinTop.Interop;

namespace PinTop;

/// <summary>One colour set. Light and dark variants are built once; the rest of the app reads <see cref="Theme.Current"/>.</summary>
internal sealed class Palette
{
    public required bool IsDark { get; init; }

    public required Color Page { get; init; }
    public required Color Surface { get; init; }
    public required Color Field { get; init; }
    public required Color Border { get; init; }

    public required Color Text { get; init; }
    public required Color Muted { get; init; }

    public required Color Accent { get; init; }
    public required Color AccentHover { get; init; }
    public required Color AccentPressed { get; init; }
    public required Color AccentText { get; init; }
    public required Color AccentSoft { get; init; }

    public required Color Track { get; init; }
    public required Color Knob { get; init; }
    public required Color Danger { get; init; }
}

/// <summary>
/// Hand-drawn look for PinTop: flat rounded surfaces, one accent colour, system light/dark mode.
/// Everything is painted with GDI+ on demand — no bitmaps, no timers, no extra assemblies.
/// </summary>
internal static class Theme
{
    private static readonly Palette LightPalette = new()
    {
        IsDark = false,
        Page = Color.FromArgb(0xEC, 0xEE, 0xF3),
        Surface = Color.White,
        Field = Color.FromArgb(0xF5, 0xF6, 0xF9),
        Border = Color.FromArgb(0xE0, 0xE3, 0xEA),
        Text = Color.FromArgb(0x1A, 0x1D, 0x23),
        Muted = Color.FromArgb(0x6D, 0x75, 0x85),
        Accent = Color.FromArgb(0x3D, 0x63, 0xF0),
        AccentHover = Color.FromArgb(0x50, 0x74, 0xF6),
        AccentPressed = Color.FromArgb(0x2E, 0x50, 0xD6),
        AccentText = Color.White,
        AccentSoft = Color.FromArgb(0xE9, 0xEE, 0xFE),
        Track = Color.FromArgb(0xD3, 0xD8, 0xE2),
        Knob = Color.White,
        Danger = Color.FromArgb(0xC0, 0x3B, 0x3B),
    };

    private static readonly Palette DarkPalette = new()
    {
        IsDark = true,
        Page = Color.FromArgb(0x14, 0x16, 0x1B),
        Surface = Color.FromArgb(0x1B, 0x1E, 0x25),
        Field = Color.FromArgb(0x22, 0x26, 0x2F),
        Border = Color.FromArgb(0x2F, 0x34, 0x3E),
        Text = Color.FromArgb(0xE9, 0xEC, 0xF2),
        Muted = Color.FromArgb(0x96, 0xA0, 0xB2),
        Accent = Color.FromArgb(0x7C, 0x93, 0xFF),
        AccentHover = Color.FromArgb(0x8F, 0xA4, 0xFF),
        AccentPressed = Color.FromArgb(0x6B, 0x80, 0xEE),
        AccentText = Color.FromArgb(0x0E, 0x12, 0x20),
        AccentSoft = Color.FromArgb(0x26, 0x30, 0x4E),
        Track = Color.FromArgb(0x3A, 0x41, 0x4D),
        Knob = Color.FromArgb(0xF2, 0xF5, 0xFA),
        Danger = Color.FromArgb(0xFF, 0x7B, 0x72),
    };

    private static PropertyInfo? _doubleBuffered;

    public static Palette Current { get; } = Detect();

    private static readonly string UiFamily = PickFamily("Segoe UI Variable Text", "Segoe UI", "Tahoma");

    public static Font Body { get; } = CreateFont(9.75f, FontStyle.Regular);
    public static Font BodyStrong { get; } = CreateFont(9.75f, FontStyle.Bold);
    public static Font Small { get; } = CreateFont(8.75f, FontStyle.Regular);
    public static Font SmallStrong { get; } = CreateFont(8.75f, FontStyle.Bold);
    public static Font Title { get; } = CreateFont(15f, FontStyle.Bold);
    public static Font CardTitle { get; } = CreateFont(8.5f, FontStyle.Bold);
    public static Font Input { get; } = CreateFont(11.5f, FontStyle.Bold);
    public static Font Menu { get; } = CreateFont(9.75f, FontStyle.Regular);

    public static void StyleForm(Form form)
    {
        form.BackColor = Current.Page;
        form.ForeColor = Current.Text;
        form.Font = Body;
        form.AutoScaleMode = AutoScaleMode.Dpi;
    }

    /// <summary>Paints the native title bar dark; Windows 10 1809+ understands both attribute ids.</summary>
    public static void ApplyTitleBar(Form form)
    {
        if (!Current.IsDark)
        {
            return;
        }

        var enabled = 1;
        if (NativeMethods.DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int)) != 0)
        {
            NativeMethods.DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
        }
    }

    public static Color Mix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);
        return Color.FromArgb(
            (int)Math.Round(from.R + (to.R - from.R) * amount),
            (int)Math.Round(from.G + (to.G - from.G) * amount),
            (int)Math.Round(from.B + (to.B - from.B) * amount));
    }

    public static GraphicsPath Round(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();

        var diameter = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
        if (diameter < 2f)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    public static void Fill(Graphics graphics, RectangleF bounds, float radius, Color color)
    {
        using var path = Round(bounds, radius);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }

    public static void Stroke(Graphics graphics, RectangleF bounds, float radius, Color color, float width = 1f)
    {
        using var path = Round(bounds, radius);
        using var pen = new Pen(color, width);
        graphics.DrawPath(pen, path);
    }

    public static void EnableDoubleBuffer(Control control)
    {
        _doubleBuffered ??= typeof(Control).GetProperty(
            "DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        _doubleBuffered?.SetValue(control, true);
    }

    private static Palette Detect()
    {
#if DEBUG
        // Used by tools/capture-screenshots.ps1 to check both palettes; release builds never contain it.
        var forced = Environment.GetEnvironmentVariable("PINTOP_UI_THEME");
        if (string.Equals(forced, "light", StringComparison.OrdinalIgnoreCase))
        {
            return LightPalette;
        }

        if (string.Equals(forced, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return DarkPalette;
        }
#endif

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key?.GetValue("AppsUseLightTheme") is int useLight && useLight == 0)
            {
                return DarkPalette;
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // Registry access can be blocked by policy; light theme is a safe default.
        }

        return LightPalette;
    }

    private static string PickFamily(params string[] candidates)
    {
        try
        {
            using var installed = new InstalledFontCollection();
            var names = installed.Families.Select(family => family.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var candidate in candidates)
            {
                if (names.Contains(candidate))
                {
                    return candidate;
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            // Fall through to the system UI font below.
        }

        return SystemFonts.MessageBoxFont?.FontFamily.Name ?? "Segoe UI";
    }

    private static Font CreateFont(float size, FontStyle style)
    {
        try
        {
            return new Font(UiFamily, size, style, GraphicsUnit.Point);
        }
        catch (ArgumentException)
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }
    }
}
