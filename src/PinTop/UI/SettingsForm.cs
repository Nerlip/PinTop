using PinTop.Interop;
using PinTop.Services;

namespace PinTop;

/// <summary>Hotkey, target and startup options.</summary>
internal sealed class SettingsForm : ToolForm
{
    private readonly HotkeyBox _hotkeyBox = new();
    private readonly Label _hotkeyHint = new();
    private readonly SegmentedControl _targetBox = new();
    private readonly ToggleSwitch _notificationsBox = new();
    private readonly ToggleSwitch _restoreBox = new();
    private readonly ToggleSwitch _startupBox = new();
    private readonly FlatButton _saveButton = new() { Kind = FlatButtonKind.Primary };
    private readonly FlatButton _cancelButton = new();
    private readonly FlatButton _resetButton = new() { Kind = FlatButtonKind.Ghost };

    private uint _hotkeyModifiers;
    private Keys _hotkeyKey;

    public SettingsForm(AppSettings current)
    {
        _hotkeyModifiers = current.HotkeyModifiers;
        _hotkeyKey = current.HotkeyKey;

        Text = Strings.SettingsTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(540, 566);

        var icon = AppIcon.LoadWindowIcon();
        if (icon is not null)
        {
            Icon = icon;
        }

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(24, 20, 24, 18),
            BackColor = Theme.Current.Page,
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new PageHeader(Strings.AppName, Strings.SettingsSubtitle), 0, 0);
        root.Controls.Add(BuildHotkeyCard(), 0, 1);
        root.Controls.Add(BuildBehaviorCard(), 0, 2);
        root.Controls.Add(BuildFooter(), 0, 4);

        Controls.Add(root);

        _targetBox.SelectedIndex = current.Target == PinTarget.ActiveWindow ? 1 : 0;
        _notificationsBox.Checked = current.ShowNotifications;
        _restoreBox.Checked = current.RestoreOnExit;
        _startupBox.Checked = current.RunAtStartup;

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        UpdateHotkeyUi();
        ActiveControl = _targetBox;
    }

    public AppSettings? Settings { get; private set; }

    private Control BuildHotkeyCard()
    {
        _hotkeyBox.Dock = DockStyle.Top;
        _hotkeyBox.AccessibleName = Strings.HotkeyLabel;
        _hotkeyBox.Margin = new Padding(0);
        _hotkeyBox.KeyDown += OnHotkeyKeyDown;

        _hotkeyHint.AutoSize = false;
        _hotkeyHint.Dock = DockStyle.Top;
        _hotkeyHint.Height = 28;
        _hotkeyHint.Font = Theme.Small;
        _hotkeyHint.ForeColor = Theme.Current.Muted;
        _hotkeyHint.TextAlign = ContentAlignment.TopLeft;
        _hotkeyHint.UseMnemonic = false;
        _hotkeyHint.Margin = new Padding(2, 10, 0, 0);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Current.Surface,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(_hotkeyBox, 0, 0);
        table.Controls.Add(_hotkeyHint, 0, 1);

        var card = new Card(Strings.GroupHotkey)
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 14),
        };
        card.Controls.Add(table);
        return card;
    }

    private Control BuildBehaviorCard()
    {
        _targetBox.Options = new[] { Strings.TargetCursor, Strings.TargetActive };
        _targetBox.AccessibleName = Strings.TargetLabel;
        _targetBox.Dock = DockStyle.Top;
        _targetBox.Margin = new Padding(0, 0, 0, 12);

        var targetLabel = new Label
        {
            Text = Strings.TargetLabel,
            Font = Theme.Small,
            ForeColor = Theme.Current.Muted,
            AutoSize = true,
            Margin = new Padding(2, 0, 0, 6),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Theme.Current.Surface,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        table.Controls.Add(targetLabel, 0, 0);
        table.SetColumnSpan(targetLabel, 2);
        table.Controls.Add(_targetBox, 0, 1);
        table.SetColumnSpan(_targetBox, 2);
        AddSwitchRow(table, 2, Strings.ShowNotifications, _notificationsBox);
        AddSwitchRow(table, 3, Strings.RestoreOnExit, _restoreBox);
        AddSwitchRow(table, 4, Strings.RunAtStartup, _startupBox);

        var card = new Card(Strings.GroupBehavior)
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
        };
        card.Controls.Add(table);
        return card;
    }

    private static void AddSwitchRow(TableLayoutPanel table, int row, string text, ToggleSwitch toggle)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Font = Theme.Body,
            Margin = new Padding(2, 0, 8, 0),
            Cursor = Cursors.Hand,
        };

        toggle.Anchor = AnchorStyles.Right;
        toggle.Margin = new Padding(0, 3, 4, 3);
        toggle.AccessibleName = text;

        label.Click += (_, _) => toggle.Checked = !toggle.Checked;

        table.Controls.Add(label, 0, row);
        table.Controls.Add(toggle, 1, row);
    }

    private Control BuildFooter()
    {
        _saveButton.Text = Strings.Save;
        _saveButton.AutoSize = true;
        _saveButton.Margin = new Padding(0, 0, 0, 0);
        _saveButton.Click += (_, _) => Save();

        _cancelButton.Text = Strings.Cancel;
        _cancelButton.AutoSize = true;
        _cancelButton.Margin = new Padding(0, 0, 8, 0);
        _cancelButton.DialogResult = DialogResult.Cancel;

        _resetButton.Text = Strings.Reset;
        _resetButton.AutoSize = true;
        _resetButton.Margin = new Padding(0);
        _resetButton.Click += (_, _) => ResetToDefaults();

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Current.Page,
            Margin = new Padding(0, 10, 0, 0),
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        footer.Controls.Add(_resetButton, 0, 0);
        footer.Controls.Add(_cancelButton, 2, 0);
        footer.Controls.Add(_saveButton, 3, 0);
        return footer;
    }

    private void Save()
    {
        if (!IsHotkeyValid())
        {
            UpdateHotkeyUi();
            _hotkeyBox.Focus();
            return;
        }

        Settings = new AppSettings
        {
            HotkeyKey = _hotkeyKey,
            HotkeyModifiers = _hotkeyKey == Keys.None ? 0 : _hotkeyModifiers,
            ShowNotifications = _notificationsBox.Checked,
            Target = _targetBox.SelectedIndex == 1 ? PinTarget.ActiveWindow : PinTarget.WindowUnderCursor,
            RunAtStartup = _startupBox.Checked,
            RestoreOnExit = _restoreBox.Checked,
            WelcomeShown = true,
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ResetToDefaults()
    {
        _hotkeyModifiers = AppSettings.DefaultHotkeyModifiers;
        _hotkeyKey = Keys.T;
        _notificationsBox.Checked = true;
        _restoreBox.Checked = true;
        _startupBox.Checked = false;
        _targetBox.SelectedIndex = 0;
        UpdateHotkeyUi();
    }

    private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        if (e.KeyCode is Keys.Back or Keys.Delete)
        {
            _hotkeyModifiers = 0;
            _hotkeyKey = Keys.None;
            UpdateHotkeyUi();
            return;
        }

        var modifiers = ModifiersFrom(e);

        if (IsModifierKey(e.KeyCode))
        {
            _hotkeyModifiers = modifiers;
            _hotkeyKey = Keys.None;
            UpdateHotkeyUi();
            return;
        }

        if (modifiers == 0 && !IsFunctionKey(e.KeyCode))
        {
            UpdateHotkeyUi();
            return;
        }

        _hotkeyModifiers = modifiers;
        _hotkeyKey = e.KeyCode;
        UpdateHotkeyUi();
    }

    private void UpdateHotkeyUi()
    {
        if (_hotkeyKey == Keys.None && _hotkeyModifiers != 0)
        {
            _hotkeyBox.Value = $"{FormatModifiers(_hotkeyModifiers)} + вЂ¦";
        }
        else
        {
            _hotkeyBox.Value = Strings.FormatHotkey(_hotkeyModifiers, _hotkeyKey);
        }

        var valid = IsHotkeyValid();
        _hotkeyHint.Text = valid ? Strings.HotkeyHint : Strings.HotkeyNeedsModifier;
        _hotkeyHint.ForeColor = valid ? Theme.Current.Muted : Theme.Current.Danger;
    }

    private bool IsHotkeyValid() =>
        _hotkeyKey == Keys.None || _hotkeyModifiers != 0 || IsFunctionKey(_hotkeyKey);

    private static uint ModifiersFrom(KeyEventArgs e)
    {
        uint modifiers = 0;
        if (e.Control) modifiers |= NativeMethods.MOD_CONTROL;
        if (e.Alt) modifiers |= NativeMethods.MOD_ALT;
        if (e.Shift) modifiers |= NativeMethods.MOD_SHIFT;
        return modifiers;
    }

    private static string FormatModifiers(uint modifiers)
    {
        var parts = new List<string>(4);
        if ((modifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & NativeMethods.MOD_WIN) != 0) parts.Add("Win");
        return string.Join(" + ", parts);
    }

    private static bool IsModifierKey(Keys key) => key is
        Keys.ControlKey or Keys.LControlKey or Keys.RControlKey or
        Keys.Menu or Keys.LMenu or Keys.RMenu or
        Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or
        Keys.LWin or Keys.RWin;

    private static bool IsFunctionKey(Keys key)
    {
        var code = (int)key;
        return code >= (int)Keys.F1 && code <= (int)Keys.F24;
    }
}
