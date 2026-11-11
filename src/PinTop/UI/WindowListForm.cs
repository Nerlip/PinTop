using System.Drawing.Drawing2D;
using PinTop.Interop;
using PinTop.Services;

namespace PinTop;

/// <summary>Lists every top-level window and lets the user pin/unpin any of them.</summary>
internal sealed class WindowListForm : ToolForm
{
    private readonly PinnedWindowManager _pinnedWindows;
    private readonly WindowListView _list = new();
    private readonly SearchBox _search = new();
    private readonly Label _summary = new();
    private readonly FlatButton _refreshButton = new();
    private readonly FlatButton _unpinAllButton = new();
    private readonly FlatButton _closeButton = new() { Kind = FlatButtonKind.Primary };
    private readonly List<Row> _rows = new();

    private bool _updating;
    private int _hoverRow = -1;

    public WindowListForm(PinnedWindowManager pinnedWindows)
    {
        _pinnedWindows = pinnedWindows;

        Text = Strings.WindowListTitle;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        MinimizeBox = false;
        KeyPreview = true;
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(640, 420);

        var icon = AppIcon.LoadWindowIcon();
        if (icon is not null)
        {
            Icon = icon;
        }

        ConfigureList();

        _search.Dock = DockStyle.Top;
        _search.Margin = new Padding(0, 0, 0, 12);
        _search.TextChanged += (_, _) => ApplyFilter();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24, 20, 24, 18),
            BackColor = Theme.Current.Page,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var listCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0) };
        listCard.Controls.Add(_list);

        root.Controls.Add(new PageHeader(Strings.WindowListHeader, Strings.WindowListHint), 0, 0);
        root.Controls.Add(_search, 0, 1);
        root.Controls.Add(listCard, 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);

        Controls.Add(root);

        AcceptButton = _refreshButton;
        CancelButton = _closeButton;

        Load += (_, _) => Reload();
        Activated += (_, _) => Reload();

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void ConfigureList()
    {
        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.CheckBoxes = false;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.OwnerDraw = true;
        _list.ShowItemToolTips = true;
        _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _list.EmptyText = Strings.NoWindows;
        _list.Columns.Add(Strings.ColumnWindow, 420);
        _list.Columns.Add(Strings.ColumnProcess, 170);

        // A one-pixel image list is the cheapest way to give rows a comfortable height.
        _list.SmallImageList = new ImageList { ImageSize = new Size(1, 30) };
        _list.SmallImageList.Images.Add(new Bitmap(1, 30));

        _list.DrawColumnHeader += OnDrawColumnHeader;
        _list.DrawSubItem += OnDrawSubItem;
        _list.ItemActivate += OnItemActivate;
        _list.KeyDown += OnListKeyDown;
        _list.MouseDown += OnListMouseDown;
        _list.MouseMove += OnListMouseMove;
        _list.MouseLeave += (_, _) => SetHoverRow(-1);
        _list.Resize += (_, _) => AdjustColumnWidths();
    }

    private Control BuildFooter()
    {
        _summary.AutoSize = true;
        _summary.Anchor = AnchorStyles.Left;
        _summary.Font = Theme.Small;
        _summary.ForeColor = Theme.Current.Muted;
        _summary.Margin = new Padding(2, 0, 12, 0);

        _refreshButton.Text = Strings.Refresh;
        _refreshButton.AutoSize = true;
        _refreshButton.Margin = new Padding(0, 0, 8, 0);
        _refreshButton.Click += (_, _) => Reload();

        _unpinAllButton.Text = Strings.UnpinAll;
        _unpinAllButton.AutoSize = true;
        _unpinAllButton.Margin = new Padding(0, 0, 8, 0);
        _unpinAllButton.Enabled = false;
        _unpinAllButton.Click += (_, _) => UnpinEveryWindow();

        _closeButton.Text = Strings.Close;
        _closeButton.AutoSize = true;
        _closeButton.Margin = new Padding(0);
        _closeButton.Click += (_, _) => Close();

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Theme.Current.Page,
            Margin = new Padding(0, 12, 0, 0),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        footer.Controls.Add(_summary, 0, 0);
        footer.Controls.Add(_unpinAllButton, 2, 0);
        footer.Controls.Add(_refreshButton, 3, 0);
        footer.Controls.Add(_closeButton, 4, 0);
        return footer;
    }

    private void AdjustColumnWidths()
    {
        var available = _list.ClientSize.Width;
        if (available <= 0)
        {
            return;
        }

        var processWidth = Math.Min(200, Math.Max(120, available / 4));
        _list.Columns[0].Width = Math.Max(160, available - processWidth);

        if (_list.Columns.Count > 1)
        {
            _list.Columns[1].Width = Math.Max(80, available - _list.Columns[0].Width);
        }
    }

    private void Reload()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        _rows.Clear();
        foreach (var hwnd in WindowTargeting.EnumerateTopLevelWindows())
        {
            var title = WindowTargeting.GetTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            _rows.Add(new Row
            {
                Title = title,
                Process = WindowTargeting.GetProcessName(hwnd),
                Handle = hwnd,
                Pinned = WindowPinner.IsTopmost(hwnd),
            });
        }

        _rows.Sort(static (left, right) =>
        {
            var byPinned = right.Pinned.CompareTo(left.Pinned);
            return byPinned != 0
                ? byPinned
                : StringComparer.CurrentCultureIgnoreCase.Compare(left.Title, right.Title);
        });

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (_updating || IsDisposed || Disposing)
        {
            return;
        }

        var filter = _search.Text.Trim();

        _updating = true;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            SetHoverRow(-1);

            foreach (var row in _rows)
            {
                if (filter.Length > 0 &&
                    !row.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase) &&
                    !row.Process.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var item = new ListViewItem(row.Title)
                {
                    Tag = row,
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

        UpdateFooter();
    }

    private void UpdateFooter()
    {
        var pinned = _rows.Count(row => row.Pinned);
        _summary.Text = Strings.PinnedSummary(pinned, _rows.Count);
        _unpinAllButton.Enabled = pinned > 0;
    }

    private void ToggleRow(Row row)
    {
        var pinned = !row.Pinned;
        if (!WindowPinner.SetTopmost(row.Handle, pinned))
        {
            return;
        }

        row.Pinned = pinned;

        if (pinned)
        {
            _pinnedWindows.Track(row.Handle, row.Title);
        }
        else
        {
            _pinnedWindows.Untrack(row.Handle);
        }

        _list.Invalidate();
        UpdateFooter();
    }

    private void OnListMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var item = _list.GetItemAt(e.X, e.Y);
        if (item?.Tag is Row row && e.X <= 44)
        {
            ToggleRow(row);
        }
    }

    private void OnItemActivate(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0 || _list.SelectedItems[0].Tag is not Row row)
        {
            return;
        }

        if (NativeMethods.IsIconic(row.Handle))
        {
            NativeMethods.ShowWindow(row.Handle, NativeMethods.SW_RESTORE);
        }

        NativeMethods.SetForegroundWindow(row.Handle);
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Space || _list.SelectedItems.Count == 0)
        {
            return;
        }

        if (_list.SelectedItems[0].Tag is Row row)
        {
            ToggleRow(row);
            e.Handled = true;
        }
    }

    private void UnpinEveryWindow()
    {
        foreach (var row in _rows)
        {
            if (!row.Pinned)
            {
                continue;
            }

            WindowPinner.SetTopmost(row.Handle, false);
            _pinnedWindows.Untrack(row.Handle);
            row.Pinned = false;
        }

        _list.Invalidate();
        UpdateFooter();
    }

    private void OnListMouseMove(object? sender, MouseEventArgs e)
    {
        SetHoverRow(_list.GetItemAt(e.X, e.Y)?.Index ?? -1);
    }

    private void SetHoverRow(int index)
    {
        if (index == _hoverRow)
        {
            return;
        }

        var previous = _hoverRow;
        _hoverRow = index;

        InvalidateRow(previous);
        InvalidateRow(index);
    }

    private void InvalidateRow(int index)
    {
        if (index >= 0 && index < _list.Items.Count)
        {
            _list.Invalidate(_list.Items[index].Bounds);
        }
    }

    private void OnDrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        var palette = Theme.Current;
        var graphics = e.Graphics;

        using (var brush = new SolidBrush(palette.Surface))
        {
            graphics.FillRectangle(brush, e.Bounds);
        }

        var left = e.ColumnIndex == 0 ? e.Bounds.Left + 42 : e.Bounds.Left + 12;
        TextRenderer.DrawText(
            graphics,
            e.Header?.Text ?? string.Empty,
            Theme.SmallStrong,
            Rectangle.FromLTRB(left, e.Bounds.Top, e.Bounds.Right - 8, e.Bounds.Bottom),
            palette.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        using var pen = new Pen(palette.Border);
        graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void OnDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item is null || e.SubItem is null || e.Item.Tag is not Row row)
        {
            return;
        }

        var palette = Theme.Current;
        var graphics = e.Graphics;

        var rowBounds = e.Bounds;
        using (var brush = new SolidBrush(RowColor(e.Item)))
        {
            graphics.FillRectangle(brush, rowBounds);
        }

        var isFirst = e.ColumnIndex == 0;

        if (isFirst)
        {
            var box = new Rectangle(14, e.Bounds.Top + (e.Bounds.Height - 18) / 2, 18, 18);
            DrawCheckbox(graphics, row.Pinned, box);
        }

        var left = isFirst ? e.Bounds.Left + 42 : e.Bounds.Left + 12;

        TextRenderer.DrawText(
            graphics,
            e.SubItem.Text,
            isFirst ? Theme.Body : Theme.Small,
            Rectangle.FromLTRB(left, e.Bounds.Top, e.Bounds.Right - 10, e.Bounds.Bottom),
            isFirst ? palette.Text : palette.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private Color RowColor(ListViewItem item)
    {
        var palette = Theme.Current;

        if (item.Selected)
        {
            return palette.AccentSoft;
        }

        return item.Index == _hoverRow
            ? Theme.Mix(palette.Surface, palette.Text, 0.04)
            : palette.Surface;
    }

    private static void DrawCheckbox(Graphics graphics, bool isChecked, Rectangle box)
    {
        var palette = Theme.Current;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = Theme.Round(box, 5.5f);

        if (!isChecked)
        {
            using var fill = new SolidBrush(palette.Surface);
            graphics.FillPath(fill, path);

            using var border = new Pen(Theme.Mix(palette.Border, palette.Text, 0.3f), 1.4f);
            graphics.DrawPath(border, path);
            return;
        }

        using (var fill = new SolidBrush(palette.Accent))
        {
            graphics.FillPath(fill, path);
        }

        var centerX = box.X + box.Width / 2f;
        var centerY = box.Y + box.Height / 2f;

        using var check = new Pen(palette.AccentText, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        graphics.DrawLines(check, new[]
        {
            new PointF(centerX - 4.1f, centerY + 0.2f),
            new PointF(centerX - 1.3f, centerY + 3.1f),
            new PointF(centerX + 4.3f, centerY - 3.3f),
        });
    }

    private sealed class Row
    {
        public required string Title { get; init; }

        public required string Process { get; init; }

        public required IntPtr Handle { get; init; }

        public bool Pinned { get; set; }
    }
}
