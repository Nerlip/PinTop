using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace PinTop;

/// <summary>Two-or-more option picker drawn as a segmented pill (replaces a ComboBox in the settings dialog).</summary>
internal sealed class SegmentedControl : Control
{
    private string[] _options = Array.Empty<string>();
    private int _selectedIndex;
    private int _hoverIndex = -1;

    public SegmentedControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        TabStop = true;
        Height = 40;
        Font = Theme.Body;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.RadioButton;
    }

    public event EventHandler? SelectedIndexChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] Options
    {
        get => _options;
        set
        {
            _options = value ?? Array.Empty<string>();
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = _options.Length == 0 ? 0 : Math.Clamp(value, 0, _options.Length - 1);
            if (clamped == _selectedIndex)
            {
                return;
            }

            _selectedIndex = clamped;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var palette = Theme.Current;
        var graphics = e.Graphics;

        graphics.Clear(BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        Theme.Fill(graphics, bounds, 10f, palette.Field);
        Theme.Stroke(graphics, bounds, 10f, palette.Border);

        if (_options.Length == 0)
        {
            return;
        }

        var inset = 4f;
        var segmentWidth = (Width - inset * 2f) / _options.Length;
        var thumb = new RectangleF(
            inset + segmentWidth * _selectedIndex,
            inset,
            segmentWidth,
            Height - inset * 2f);

        Theme.Fill(graphics, thumb, 7f, palette.Surface);
        Theme.Stroke(graphics, thumb, 7f, Theme.Mix(palette.Border, palette.Text, 0.08));

        for (var index = 0; index < _options.Length; index++)
        {
            var cell = new RectangleF(inset + segmentWidth * index, inset, segmentWidth, Height - inset * 2f);
            var color = index == _selectedIndex ? palette.Text : palette.Muted;

            TextRenderer.DrawText(
                graphics,
                _options[index],
                Font,
                Rectangle.Round(cell),
                color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        if (Focused && ShowFocusCues && _hoverIndex < 0)
        {
            Theme.Stroke(graphics, RectangleF.Inflate(bounds, -2.5f, -2.5f), 8f, palette.Accent, 1.4f);
        }
    }

    protected override bool IsInputKey(Keys keyData) => keyData switch
    {
        Keys.Left or Keys.Right or Keys.Up or Keys.Down => true,
        _ => base.IsInputKey(keyData),
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Left or Keys.Up)
        {
            SelectedIndex--;
            e.Handled = true;
            return;
        }

        if (e.KeyCode is Keys.Right or Keys.Down)
        {
            SelectedIndex++;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var index = IndexAt(e.X);
        if (index != _hoverIndex)
        {
            _hoverIndex = index;
            Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverIndex = -1;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();

        var index = IndexAt(e.X);
        if (index >= 0)
        {
            SelectedIndex = index;
        }
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

    private int IndexAt(int x)
    {
        if (_options.Length == 0 || Width <= 0)
        {
            return -1;
        }

        var segmentWidth = Width / (float)_options.Length;
        var index = (int)(x / segmentWidth);
        return index >= 0 && index < _options.Length ? index : -1;
    }
}
