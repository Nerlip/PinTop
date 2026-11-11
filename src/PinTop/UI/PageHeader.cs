using PinTop.Services;

namespace PinTop;

/// <summary>Dialog header: app artwork, title and one-line description, laid out by hand for stable DPI behaviour.</summary>
internal sealed class PageHeader : Panel
{
    private readonly int _iconSize;
    private readonly PictureBox _artwork;
    private readonly Label _title;
    private readonly Label _subtitle;

    public PageHeader(string title, string subtitle, int iconSize = 44)
    {
        _iconSize = iconSize;

        BackColor = Theme.Current.Page;
        Dock = DockStyle.Top;
        Margin = new Padding(0, 0, 0, 18);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        _artwork = new PictureBox
        {
            Image = AppIcon.LoadHeaderImage(48),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Theme.Current.Page,
        };

        _title = new Label
        {
            Text = title,
            Font = Theme.Title,
            ForeColor = Theme.Current.Text,
            AutoSize = false,
            UseMnemonic = false,
        };

        _subtitle = new Label
        {
            Text = subtitle,
            Font = Theme.Small,
            ForeColor = Theme.Current.Muted,
            AutoSize = false,
            UseMnemonic = false,
        };

        Controls.Add(_artwork);
        Controls.Add(_title);
        Controls.Add(_subtitle);

        Resize += (_, _) => LayoutChildren();
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        _artwork.SetBounds(0, 0, _iconSize, _iconSize);

        var left = _iconSize + 14;
        var width = Math.Max(40, Width - left);

        var titleHeight = TextRenderer.MeasureText(
            _title.Text, _title.Font, new Size(width, 10000), TextFormatFlags.WordBreak).Height;
        _title.SetBounds(left, 0, width, titleHeight);

        var subtitleHeight = TextRenderer.MeasureText(
            _subtitle.Text, _subtitle.Font, new Size(width, 10000), TextFormatFlags.WordBreak).Height;
        _subtitle.SetBounds(left, titleHeight + 3, width, subtitleHeight);

        var height = Math.Max(_iconSize, titleHeight + 3 + subtitleHeight);
        if (Height != height)
        {
            Height = height;
        }
    }
}
