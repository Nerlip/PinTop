using PinTop.Interop;
using PinTop.Services;

namespace PinTop;

/// <summary>Lists every top-level window and lets the user pin/unpin any of them.</summary>
internal sealed class WindowListForm : Form
{
    private readonly PinnedWindowManager _pinnedWindows;
    private readonly ListView _list = new();
    private readonly Label _hint = new();
    private readonly Button _refreshButton = new();
    private readonly Button _unpinAllButton = new();
    private readonly Button _closeButton = new();
    private bool _updating;

    public WindowListForm(PinnedWindowManager pinnedWindows)
    {
        _pinnedWindows = pinnedWindows;

        Text = Strings.WindowListTitle;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        MinimizeBox = false;
        KeyPreview = true;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(660, 460);
        MinimumSize = new Size(520, 320);

        var icon = AppIcon.LoadWindowIcon();
        if (icon is not null)
        {
            Icon = icon;
        }

        _hint.Text = Strings.WindowListHint;
        _hint.Dock = DockStyle.Fill;
        _hint.AutoSize = false;
        _hint.Padding = new Padding(14, 12, 14, 6);
        _hint.UseMnemonic = false;

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.CheckBoxes = true;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.ShowItemToolTips = true;
        _list.Columns.Add(Strings.ColumnWindow, 420);
        _list.Columns.Add(Strings.ColumnProcess, 170);
        _list.ItemChecked += OnItemChecked;
        _list.ItemActivate += OnItemActivate;
        _list.Resize += (_, _) => AdjustColumnWidths();
        _list.KeyDown += OnListKeyDown;

        _refreshButton.Text = Strings.Refresh;
        _refreshButton.AutoSize = true;
        _refreshButton.Padding = new Padding(8, 2, 8, 2);
        _refreshButton.Click += (_, _) => Reload();

        _unpinAllButton.Text = Strings.UnpinAll;
        _unpinAllButton.AutoSize = true;
        _unpinAllButton.Padding = new Padding(8, 2, 8, 2);
        _unpinAllButton.Enabled = false;
        _unpinAllButton.Click += (_, _) => UnpinEveryWindow();

        _closeButton.Text = Strings.Close;
        _closeButton.AutoSize = true;
        _closeButton.Padding = new Padding(8, 2, 8, 2);
        _closeButton.Click += (_, _) => Close();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10, 8, 10, 10),
            WrapContents = false,
        };
        buttons.Controls.Add(_closeButton);
        buttons.Controls.Add(_unpinAllButton);
        buttons.Controls.Add(_refreshButton);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(_hint, 0, 0);
        root.Controls.Add(_list, 0, 1);
        root.Controls.Add(buttons, 0, 2);

        Controls.Add(root);

        AcceptButton = _refreshButton;
        CancelButton = _closeButton;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };

        Load += (_, _) =>
        {
            AdjustColumnWidths();
            Reload();
        };
        Activated += (_, _) => Reload();
    }

    private void AdjustColumnWidths()
    {
        var available = _list.ClientSize.Width - 30;
        if (available <= 0)
        {
            return;
        }

        var processWidth = Math.Min(190, Math.Max(110, available / 4));
        _list.Columns[0].Width = Math.Max(150, available - processWidth);

        if (_list.Columns.Count > 1)
        {
            _list.Columns[1].Width = processWidth;
        }
    }

    private void Reload()
    {
        if (_updating || IsDisposed || Disposing)
        {
            return;
        }

        _updating = true;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();

            var rows = new List<(bool Pinned, string Title, string Process, IntPtr Handle)>();
            foreach (var hwnd in WindowTargeting.EnumerateTopLevelWindows())
            {
                var title = WindowTargeting.GetTitle(hwnd);
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                rows.Add((
                    WindowPinner.IsTopmost(hwnd),
                    title,
                    WindowTargeting.GetProcessName(hwnd),
                    hwnd));
            }

            var ordered = rows
                .OrderByDescending(row => row.Pinned)
                .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase);

            foreach (var row in ordered)
            {
                var item = new ListViewItem(row.Title)
                {
                    Tag = row.Handle,
                    Checked = row.Pinned,
                    ToolTipText = row.Title,
                };
                item.SubItems.Add(row.Process);
                _list.Items.Add(item);
            }
        }
        finally
        {
            _list.EndUpdate();
            _updating = false;
        }

        UpdateButtons();
    }

    private void OnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_updating || e.Item.Tag is not IntPtr hwnd)
        {
            return;
        }

        var pinned = e.Item.Checked;
        if (!WindowPinner.SetTopmost(hwnd, pinned))
        {
            _updating = true;
            e.Item.Checked = !pinned;
            _updating = false;
            return;
        }

        if (pinned)
        {
            _pinnedWindows.Track(hwnd, e.Item.Text);
        }
        else
        {
            _pinnedWindows.Untrack(hwnd);
        }

        UpdateButtons();
    }

    private void OnItemActivate(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0 || _list.SelectedItems[0].Tag is not IntPtr hwnd)
        {
            return;
        }

        if (NativeMethods.IsIconic(hwnd))
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
        }

        NativeMethods.SetForegroundWindow(hwnd);
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Space || _list.SelectedItems.Count == 0)
        {
            return;
        }

        var item = _list.SelectedItems[0];
        item.Checked = !item.Checked;
        e.Handled = true;
    }

    private void UnpinEveryWindow()
    {
        _updating = true;
        try
        {
            foreach (ListViewItem item in _list.Items)
            {
                if (!item.Checked || item.Tag is not IntPtr hwnd)
                {
                    continue;
                }

                WindowPinner.SetTopmost(hwnd, false);
                _pinnedWindows.Untrack(hwnd);
                item.Checked = false;
            }
        }
        finally
        {
            _updating = false;
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        _unpinAllButton.Enabled = _list.Items.Cast<ListViewItem>().Any(item => item.Checked);
    }
}
