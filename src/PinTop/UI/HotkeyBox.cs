using System.ComponentModel;
using System.Drawing.Drawing2D;
using PinTop.Services;

namespace PinTop;

/// <summary>Read-only field that shows the current shortcut and focuses itself when clicked.</summary>
internal sealed class HotkeyBox : Control
{
    private string _value = string.Empty;
    private bool _hover;

    public HotkeyBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        TabStop = true;
        Height = 46;
        Cursor = Cursors.Hand;
        Font = Theme.Input;
        AccessibleRole = AccessibleRole.Text;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Value
    {
        get => _value;
        set
        {
            _value = value ?? string.Empty;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var palette = Theme.Current;
        var graphics = e.Graphics;

        graphics.Clear(BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f);
        Theme.Fill(graphics, bounds, 10f, palette.Field);

        var border = Focused
            ? palette.Accent
            : _hover
                ? Theme.Mix(palette.Border, palette.Accent, 0.45)
                : palette.Border;

        Theme.Stroke(graphics, bounds, 10f, border, Focused ? 1.6f : 1f);

        var empty = string.IsNullOrEmpty(_value);
        TextRenderer.DrawText(
            graphics,
            empty ? Strings.HotkeyNotSet : _value,
            Font,
            new Rectangle(16, 0, Width - 32, Height),
            empty ? palette.Muted : palette.Text,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
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
        Invalidate();
        base.OnMouseLeave(e);
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
}
