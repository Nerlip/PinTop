using PinTop.Interop;
using PinTop.Services;

namespace PinTop;

/// <summary>Hotkey, target and startup options.</summary>
internal sealed class SettingsForm : Form
{
    private readonly TextBox _hotkeyBox = new();
    private readonly Label _hotkeyWarning = new();
    private readonly ComboBox _targetBox = new();
    private readonly CheckBox _notificationsBox = new();
    private readonly CheckBox _restoreBox = new();
    private readonly CheckBox _startupBox = new();
    private readonly Button _saveButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _resetButton = new();

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
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 400);

        var icon = AppIcon.LoadWindowIcon();
        if (icon is not null)
        {
            Icon = icon;
        }

        var hotkeyGroup = BuildHotkeyGroup();
        var behaviorGroup = BuildBehaviorGroup();
        var buttons = BuildButtons();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(hotkeyGroup, 0, 0);
        root.Controls.Add(behaviorGroup, 0, 1);
        root.Controls.Add(buttons, 0, 2);

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

    private GroupBox BuildHotkeyGroup()
    {
        _hotkeyBox.ReadOnly = true;
        _hotkeyBox.ShortcutsEnabled = false;
        _hotkeyBox.Cursor = Cursors.Default;
        _hotkeyBox.BackColor = SystemColors.Window;
        _hotkeyBox.ForeColor = SystemColors.WindowText;
        _hotkeyBox.Dock = DockStyle.Fill;
        _hotkeyBox.Font = new Font(Font.FontFamily, 10f, FontStyle.Bold);
        _hotkeyBox.KeyDown += OnHotkeyKeyDown;

        _hotkeyWarning.Text = Strings.HotkeyNeedsModifier;
        _hotkeyWarning.ForeColor = Color.Firebrick;
        _hotkeyWarning.AutoSize = true;
        _hotkeyWarning.MaximumSize = new Size(390, 0);
        _hotkeyWarning.Visible = false;
        _hotkeyWarning.Margin = new Padding(3, 6, 3, 0);

        var hint = new Label
        {
            Text = Strings.HotkeyHint,
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            MaximumSize = new Size(390, 0),
            Margin = new Padding(3, 6, 3, 0),
        };

        var label = new Label
        {
            Text = Strings.HotkeyLabel,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 8, 3),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 3,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.Controls.Add(label, 0, 0);
        table.Controls.Add(_hotkeyBox, 1, 0);
        table.Controls.Add(_hotkeyWarning, 1, 1);
        table.Controls.Add(hint, 1, 2);

        var group = new GroupBox
        {
            Text = Strings.GroupHotkey,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10, 6, 10, 10),
        };
        group.Controls.Add(table);
        return group;
    }

    private GroupBox BuildBehaviorGroup()
    {
        _targetBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _targetBox.Dock = DockStyle.Fill;
        _targetBox.Items.Add(Strings.TargetCursor);
        _targetBox.Items.Add(Strings.TargetActive);

        _notificationsBox.Text = Strings.ShowNotifications;
        _notificationsBox.AutoSize = true;

        _restoreBox.Text = Strings.RestoreOnExit;
        _restoreBox.AutoSize = true;

        _startupBox.Text = Strings.RunAtStartup;
        _startupBox.AutoSize = true;

        var label = new Label
        {
            Text = Strings.TargetLabel,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 8, 3),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 4,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.Controls.Add(label, 0, 0);
        table.Controls.Add(_targetBox, 1, 0);
        table.Controls.Add(_notificationsBox, 1, 1);
        table.Controls.Add(_restoreBox, 1, 2);
        table.Controls.Add(_startupBox, 1, 3);

        var group = new GroupBox
        {
            Text = Strings.GroupBehavior,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 6, 10, 10),
        };
        group.Controls.Add(table);
        return group;
    }

    private FlowLayoutPanel BuildButtons()
    {
        _saveButton.Text = Strings.Save;
        _saveButton.AutoSize = true;
        _saveButton.Padding = new Padding(10, 2, 10, 2);
        _saveButton.Click += (_, _) => Save();

        _cancelButton.Text = Strings.Cancel;
        _cancelButton.AutoSize = true;
        _cancelButton.Padding = new Padding(10, 2, 10, 2);
        _cancelButton.DialogResult = DialogResult.Cancel;

        _resetButton.Text = Strings.Reset;
        _resetButton.AutoSize = true;
        _resetButton.Padding = new Padding(10, 2, 10, 2);
        _resetButton.Click += (_, _) => ResetToDefaults();

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
        };
        panel.Controls.Add(_saveButton);
        panel.Controls.Add(_cancelButton);
        panel.Controls.Add(_resetButton);
        return panel;
    }

    private void Save()
    {
        if (!IsHotkeyValid())
        {
            _hotkeyWarning.Visible = true;
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
            _hotkeyWarning.Visible = true;
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
            _hotkeyBox.Text = $"{FormatModifiers(_hotkeyModifiers)} + …";
        }
        else
        {
            _hotkeyBox.Text = Strings.FormatHotkey(_hotkeyModifiers, _hotkeyKey);
        }

        _hotkeyWarning.Visible = !IsHotkeyValid();
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
