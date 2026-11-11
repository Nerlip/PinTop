using System.Drawing.Drawing2D;

namespace PinTop;

/// <summary>Matches the tray context menu to the dialog styling: flat surface, rounded accent highlight.</summary>
internal sealed class AppMenuRenderer : ToolStripProfessionalRenderer
{
    public AppMenuRenderer()
        : base(new AppMenuColors())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        if (!item.Selected || !item.Enabled)
        {
            return;
        }

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(4, 1, Math.Max(0, item.Width - 8), Math.Max(0, item.Height - 2));
        Theme.Fill(graphics, bounds, 7f, Theme.Current.AccentSoft);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? e.Item.ForeColor : Theme.Current.Muted;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(Theme.Current.Border);
        e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
    }

    private sealed class AppMenuColors : ProfessionalColorTable
    {
        private static Palette P => Theme.Current;

        public override Color ToolStripDropDownBackground => P.Surface;

        public override Color ImageMarginGradientBegin => P.Surface;

        public override Color ImageMarginGradientMiddle => P.Surface;

        public override Color ImageMarginGradientEnd => P.Surface;

        public override Color MenuBorder => P.Border;

        public override Color MenuItemBorder => Color.Transparent;

        public override Color MenuItemSelected => P.AccentSoft;

        public override Color MenuItemSelectedGradientBegin => P.AccentSoft;

        public override Color MenuItemSelectedGradientEnd => P.AccentSoft;

        public override Color MenuItemPressedGradientBegin => P.Field;

        public override Color MenuItemPressedGradientEnd => P.Field;

        public override Color SeparatorDark => P.Border;

        public override Color SeparatorLight => P.Surface;
    }
}
