using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using PinTop.Services;

namespace PinTop;

/// <summary>Search field: borderless TextBox inside a rounded, focus-aware frame.</summary>
internal sealed class SearchBox : Control
{
    private readonly TextBox _box = new();
    private readonly Label _placeholder = new();

    public SearchBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        Height = 38;
        BackColor = Theme.Current.Page;
        Font = Theme.Body;

        _box.BorderStyle = BorderStyle.None;
        _box.Font = Theme.Body;
        _box.BackColor = Theme.Current.Field;
        _box.ForeColor = Theme.Current.Text;
        _box.TextChanged += (_, _) =>
        {
            UpdatePlaceholder();
            OnTextChanged(EventArgs.Empty);
        };
        _box.Enter += (_, _) =>
        {
            UpdatePlaceholder();
            Invalidate();
        };
        _box.Leave += (_, _) =>
        {
            UpdatePlaceholder();
            Invalidate();
        };

        _placeholder.AutoSize = false;
        _placeholder.Font = Theme.Body;
        _placeholder.ForeColor = Theme.Current.Muted;
        _placeholder.BackColor = Theme.Current.Field;
        _placeholder.TextAlign = ContentAlignment.MiddleLeft;
        _placeholder.Text = Strings.SearchPlaceholder;
        _placeholder.UseMnemonic = false;
        _placeholder.Cursor = Cursors.IBeam;
        _placeholder.Click += (_, _) => _box.Focus();

        Controls.Add(_placeholder);
        Controls.Add(_box);

        UpdatePlaceholder();
    }

    [AllowNull]
    public override string Text
    {
        get => _box.Text;
        set => _box.Text = value;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var palette = Theme.Current;
        var graphics = e.Graphics;

        graphics.Clear(BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f);
        Theme.Fill(graphics, bounds, 9f, palette.Field);
        Theme.Stroke(graphics, bounds, 9f, _box.Focused ? palette.Accent : palette.Border);

        // Magnifier glyph, drawn from two arcs and a line so no image resource is needed.
        var lens = new RectangleF(14f, (Height - 13f) / 2f, 10f, 10f);
        using (var pen = new Pen(palette.Muted, 1.3f))
        {
            graphics.DrawEllipse(pen, lens);
            graphics.DrawLine(pen, lens.Right - 0.5f, lens.Bottom - 0.5f, lens.Right + 3.5f, lens.Bottom + 3.5f);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        var height = _box.PreferredHeight;
        var top = Math.Max(0, (Height - height) / 2);

        _box.SetBounds(34, top, Math.Max(10, Width - 48), height);
        _placeholder.SetBounds(36, top, Math.Max(10, Width - 52), height);
    }

    private void UpdatePlaceholder()
    {
        _placeholder.Visible = _box.TextLength == 0;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        _box.Focus();
        Invalidate();
        base.OnGotFocus(e);
    }
}
