using System.Drawing.Drawing2D;

namespace PinTop;

/// <summary>Check box drawn as a small iOS-style switch; no text, put a label next to it.</summary>
internal sealed class ToggleSwitch : CheckBox
{
    private const int SwitchWidth = 42;
    private const int SwitchHeight = 24;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        AutoSize = false;
        Size = new Size(SwitchWidth, SwitchHeight);
        MinimumSize = new Size(SwitchWidth, SwitchHeight);
        Cursor = Cursors.Hand;
        Text = string.Empty;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var palette = Theme.Current;
        var graphics = e.Graphics;

        graphics.Clear(BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var width = Math.Min(SwitchWidth, Width);
        var height = Math.Min(SwitchHeight, Height);
        var track = new RectangleF(
            (Width - width) / 2f,
            (Height - height) / 2f,
            width,
            height);

        var trackColor = Checked
            ? (Focused ? palette.AccentHover : palette.Accent)
            : (Focused ? Theme.Mix(palette.Track, palette.Text, 0.12) : palette.Track);

        Theme.Fill(graphics, track, height / 2f, trackColor);

        var knobSize = height - 6f;
        var knobX = Checked ? track.Right - knobSize - 3f : track.Left + 3f;
        var knob = new RectangleF(knobX, track.Top + 3f, knobSize, knobSize);

        Theme.Fill(graphics, knob, knobSize / 2f, Checked ? palette.Knob : palette.Surface);

        if (Focused && ShowFocusCues)
        {
            Theme.Stroke(graphics, RectangleF.Inflate(track, 1.5f, 1.5f), height / 2f + 1.5f, palette.Accent, 1.4f);
        }
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        Invalidate();
        base.OnCheckedChanged(e);
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

    protected override void OnMouseEnter(EventArgs e)
    {
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        Invalidate();
        base.OnMouseLeave(e);
    }
}
