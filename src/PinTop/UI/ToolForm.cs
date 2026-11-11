namespace PinTop;

/// <summary>Form wired to the current <see cref="Theme"/> (page colour, font, dark title bar).</summary>
internal class ToolForm : Form
{
    public ToolForm()
    {
        Theme.StyleForm(this);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.ApplyTitleBar(this);
    }
}
