namespace PinTop.Services;

internal static class AppIcon
{
    private static Bitmap? _header;

    /// <summary>Larger copy of the app artwork for dialog headers (created once, ~6 KB).</summary>
    public static Bitmap? LoadHeaderImage(int size)
    {
        if (_header is not null)
        {
            return _header;
        }

        try
        {
            using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("PinTop.Resources.app.ico");
            if (stream is not null)
            {
                using var icon = new Icon(stream, size, size);
                _header = icon.ToBitmap();
                return _header;
            }
        }
        catch (ArgumentException)
        {
            // Fall back to no artwork.
        }

        return null;
    }

    /// <summary>Icon for the notification area, loaded from the embedded multi-size .ico.</summary>
    public static Icon LoadTrayIcon()
    {
        try
        {
            using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("PinTop.Resources.app.ico");
            if (stream is not null)
            {
                return new Icon(stream, SystemInformation.SmallIconSize);
            }
        }
        catch (ArgumentException)
        {
            // Fall back to the generic icon below.
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    /// <summary>Icon for regular windows; the exe already carries the same artwork.</summary>
    public static Icon? LoadWindowIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return Icon.ExtractAssociatedIcon(path);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // No icon is fine.
        }

        return null;
    }
}
