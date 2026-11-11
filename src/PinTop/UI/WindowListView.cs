using System.ComponentModel;

namespace PinTop;

/// <summary>ListView that can paint a "nothing here" message over its empty client area.</summary>
internal sealed class WindowListView : ListView
{
    public WindowListView()
    {
        BorderStyle = BorderStyle.None;
        BackColor = Theme.Current.Surface;
        ForeColor = Theme.Current.Text;
        Font = Theme.Body;
        Theme.EnableDoubleBuffer(this);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? EmptyText { get; set; }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == 0x000F && Items.Count == 0 && !string.IsNullOrEmpty(EmptyText))
        {
            using var graphics = Graphics.FromHwnd(Handle);
            TextRenderer.DrawText(
                graphics,
                EmptyText,
                Theme.Body,
                ClientRectangle,
                Theme.Current.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }
    }
}
