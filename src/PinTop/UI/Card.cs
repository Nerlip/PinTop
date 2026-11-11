using System.Drawing.Drawing2D;

namespace PinTop;

/// <summary>Rounded surface with a small caption, used instead of <see cref="GroupBox"/>.</summary>
internal sealed class Card : Panel
{
    public const int CaptionHeight = 46;

    public Card(string title = "")
    {
        Title = title;

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        BackColor = Theme.Current.Surface;
        ForeColor = Theme.Current.Text;
        Font = Theme.Body;
        Padding = string.IsNullOrEmpty(title)
            ? new Padding(9)
            : new Padding(18, CaptionHeight, 18, 18);
    }

    public string Title { get; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var palette = Theme.Current;
        var graphics = e.Graphics;
        graphics.Clear(Parent?.BackColor ?? palette.Page);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        Theme.Fill(graphics, bounds, 12f, palette.Surface);
        Theme.Stroke(graphics, bounds, 12f, palette.Border);

        if (string.IsNullOrEmpty(Title))
        {
            return;
        }

        TextRenderer.DrawText(
            graphics,
            Title.ToUpperInvariant(),
            Theme.CardTitle,
            new Point(19, 17),
            palette.Muted,
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }
}
