using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace PinTop;

internal enum FlatButtonKind
{
    Primary,
    Secondary,
    Ghost,
}

/// <summary>Flat rounded button drawn with GDI+ (no images, no visual-style rendering).</summary>
internal sealed class FlatButton : Button
{
    private bool _hover;
    private bool _pressed;

    public FlatButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Font = Theme.BodyStrong;
        Cursor = Cursors.Hand;
        Height = 36;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlatButtonKind Kind { get; set; } = FlatButtonKind.Secondary;

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = string.IsNullOrEmpty(Text) ? " " : Text;
        var measured = TextRenderer.MeasureText(
            text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

        return new Size(measured.Width + 36, Math.Max(36, measured.Height + 14));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var palette = Theme.Current;
        var graphics = e.Graphics;

        graphics.Clear(BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f);
        var radius = 9f;
        Color fill;
        Color text;
        Color? border = null;

        switch (Kind)
        {
            case FlatButtonKind.Primary when !Enabled:
                fill = Theme.Mix(palette.Accent, palette.Surface, 0.55);
                text = palette.AccentText;
                break;
            case FlatButtonKind.Primary:
                fill = _pressed ? palette.AccentPressed : _hover ? palette.AccentHover : palette.Accent;
                text = palette.AccentText;
                break;
            case FlatButtonKind.Ghost when !Enabled:
                fill = BackColor;
                text = palette.Muted;
                break;
            case FlatButtonKind.Ghost:
                fill = _hover || _pressed ? Theme.Mix(BackColor, palette.Accent, _pressed ? 0.16 : 0.09) : BackColor;
                text = _hover || _pressed ? palette.Text : palette.Muted;
                break;
            default:
                fill = _pressed
                    ? Theme.Mix(palette.Surface, palette.Accent, 0.16)
                    : _hover
                        ? Theme.Mix(palette.Surface, palette.Accent, 0.07)
                        : palette.Surface;
                text = Enabled ? palette.Text : palette.Muted;
                border = _hover ? Theme.Mix(palette.Border, palette.Accent, 0.38) : palette.Border;
                break;
        }

        Theme.Fill(graphics, bounds, radius, fill);

        if (border is { } borderColor)
        {
            Theme.Stroke(graphics, bounds, radius, borderColor);
        }

        if (Focused && ShowFocusCues)
        {
            Theme.Stroke(graphics, RectangleF.Inflate(bounds, -2.5f, -2.5f), radius - 2f, palette.Accent, 1.4f);
        }

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            new Rectangle(0, 0, Width, Height),
            text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(mevent);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }
}
