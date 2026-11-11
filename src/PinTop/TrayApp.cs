using PinTop.Interop;
using PinTop.Services;

namespace PinTop;

/// <summary>Owns the tray icon, the global hotkey and the shared window state.</summary>
internal sealed class TrayApp : ApplicationContext
{
    private const int MaxBalloonTextLength = 240;
    private const int MaxTitleLength = 60;

    private readonly PinnedWindowManager _pinnedWindows = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _pinItem;
    private readonly ToolStripLabel _headerItem;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly System.Windows.Forms.Timer _watcher;

    private AppSettings _settings;
    private WindowListForm? _windowList;
    private Icon? _trayIconImage;
    private IntPtr _lastActiveWindow;
    private bool _hotkeyRegistrationFailed;
    private bool _exiting;

    public TrayApp()
    {
        _settings = AppSettings.Load();

        _menu = new ContextMenuStrip
        {
            Renderer = new AppMenuRenderer(),
            ShowImageMargin = false,
            Font = Theme.Menu,
            ForeColor = Theme.Current.Text,
            BackColor = Theme.Current.Surface,
            Padding = new Padding(5),
        };

        _headerItem = new ToolStripLabel
        {
            Text = Strings.MenuHeader(0),
            Font = Theme.Small,
            ForeColor = Theme.Current.Muted,
            Enabled = false,
            Padding = new Padding(8, 3, 8, 3),
        };

        _pinItem = new ToolStripMenuItem
        {
            Font = new Font(_menu.Font, FontStyle.Bold),
            ForeColor = Theme.Current.Accent,
            Padding = new Padding(8, 7, 8, 7),
        };
        _pinItem.Click += (_, _) => ToggleTarget(fromTray: true);

        var listItem = new ToolStripMenuItem(Strings.WindowList)
        {
            Padding = new Padding(8, 7, 8, 7),
        };
        listItem.Click += (_, _) => ShowWindowList();

        var settingsItem = new ToolStripMenuItem(Strings.Settings)
        {
            Padding = new Padding(8, 7, 8, 7),
        };
        settingsItem.Click += (_, _) => ShowSettings();

        var aboutItem = new ToolStripMenuItem(Strings.About)
        {
            Padding = new Padding(8, 7, 8, 7),
        };
        aboutItem.Click += (_, _) => ShowAbout();

        var exitItem = new ToolStripMenuItem(Strings.Exit)
        {
            Padding = new Padding(8, 7, 8, 7),
        };
        exitItem.Click += (_, _) => ExitApplication();

        _menu.Items.AddRange(new ToolStripItem[]
        {
            _headerItem,
            new ToolStripSeparator(),
            _pinItem,
            listItem,
            settingsItem,
            new ToolStripSeparator(),
            aboutItem,
            exitItem,
        });

        _menu.Opening += (_, _) => UpdatePinItemText();

        _trayIconImage = AppIcon.LoadTrayIcon();
        _trayIcon = new NotifyIcon
        {
            Icon = _trayIconImage,
            Text = Strings.TrayTooltip(0),
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _trayIcon.DoubleClick += (_, _) => ToggleTarget(fromTray: true);

        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.HotkeyPressed += (_, _) => ToggleTarget();
        _hotkeyWindow.TaskbarCreated += (_, _) => RestoreTrayIcon();

        ApplyHotkey(notifyOnFailure: true);

        _watcher = new System.Windows.Forms.Timer { Interval = 1000 };
        _watcher.Tick += (_, _) => OnWatcherTick();
        _watcher.Start();

        _pinnedWindows.Changed += (_, _) => UpdateTooltip();

        UpdatePinItemText();
        UpdateTooltip();
        KeepStartupEntryFresh();

        if (!_settings.WelcomeShown)
        {
            _settings.WelcomeShown = true;
            _settings.Save();
            ShowBalloon(Strings.Welcome);
        }
    }

    private void OnWatcherTick()
    {
        _pinnedWindows.Prune();

        if (_hotkeyRegistrationFailed)
        {
            // Another program may have released the shortcut in the meantime.
            _hotkeyRegistrationFailed = !_hotkeyWindow.TryRegister(_settings.HotkeyModifiers, _settings.HotkeyKey);
        }

        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground != IntPtr.Zero && !WindowTargeting.IsOwnedByCurrentProcess(foreground) && WindowTargeting.IsEligible(foreground))
        {
            _lastActiveWindow = foreground;
        }
    }

    private void ToggleTarget(bool fromTray = false)
    {
        var target = _settings.Target == PinTarget.ActiveWindow
            ? WindowTargeting.GetActiveWindow()
            : WindowTargeting.GetWindowUnderCursor();

        if (fromTray || (target != IntPtr.Zero && WindowTargeting.IsOwnedByCurrentProcess(target)))
        {
            if (!WindowTargeting.IsEligible(target))
            {
                target = _lastActiveWindow;
            }
        }

        if (!WindowTargeting.IsEligible(target))
        {
            ShowBalloon(Strings.NoSuitableWindow);
            return;
        }

        var title = DisplayTitle(target);

        if (!WindowPinner.Toggle(target, out var pinned))
        {
            ShowBalloon(Strings.NoSuitableWindow);
            return;
        }

        if (pinned)
        {
            _pinnedWindows.Track(target, title);
        }
        else
        {
            _pinnedWindows.Untrack(target);
        }

        ShowBalloon(pinned ? Strings.PinnedMessage(title) : Strings.UnpinnedMessage(title));
        UpdatePinItemText();
        UpdateTooltip();
    }

    private static string DisplayTitle(IntPtr hwnd)
    {
        var title = WindowTargeting.GetTitle(hwnd);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = WindowTargeting.GetProcessName(hwnd);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Window";
        }

        return title.Length > MaxTitleLength ? title[..(MaxTitleLength - 1)] + "…" : title;
    }

    private void ShowWindowList()
    {
        if (_windowList is { IsDisposed: false })
        {
            if (_windowList.WindowState == FormWindowState.Minimized)
            {
                _windowList.WindowState = FormWindowState.Normal;
            }

            _windowList.Activate();
            return;
        }

        _windowList = new WindowListForm(_pinnedWindows);
        _windowList.FormClosed += (_, _) => _windowList = null;
        _windowList.Show();
        _windowList.Activate();
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() == DialogResult.OK && form.Settings is { } updated)
        {
            var startupChanged = updated.RunAtStartup != _settings.RunAtStartup;
            updated.WelcomeShown = true;

            _settings = updated;
            _settings.Save();

            if (startupChanged)
            {
                StartupManager.SetEnabled(updated.RunAtStartup);
            }

            ApplyHotkey(notifyOnFailure: true);
            UpdatePinItemText();
            UpdateTooltip();
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            Strings.AboutText,
            $"{Strings.AppName} {Strings.Version}",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ApplyHotkey(bool notifyOnFailure)
    {
        _hotkeyRegistrationFailed = !_hotkeyWindow.TryRegister(_settings.HotkeyModifiers, _settings.HotkeyKey);

        if (_hotkeyRegistrationFailed && notifyOnFailure)
        {
            ShowBalloon(Strings.HotkeyRegistrationFailed(
                Strings.FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey)));
        }

        UpdatePinItemText();
    }

    private void UpdatePinItemText()
    {
        var target = _settings.Target == PinTarget.ActiveWindow
            ? WindowTargeting.GetActiveWindow()
            : WindowTargeting.GetWindowUnderCursor();

        var isPinned = WindowTargeting.IsEligible(target) && WindowPinner.IsTopmost(target);
        var targetLabel = _settings.Target == PinTarget.ActiveWindow
            ? Strings.ActiveWindow
            : Strings.UnderCursor;

        _pinItem.Text = $"{(isPinned ? Strings.UnpinWindow : Strings.PinWindow)} ({targetLabel})";
        _pinItem.ShortcutKeyDisplayString = _settings.HotkeyEnabled
            ? Strings.FormatHotkey(_settings.HotkeyModifiers, _settings.HotkeyKey)
            : null;
    }

    private void UpdateTooltip()
    {
        var count = _pinnedWindows.Count;
        _trayIcon.Text = Strings.TrayTooltip(count);
        _headerItem.Text = Strings.MenuHeader(count);
    }

#if DEBUG
    /// <summary>Used by tools/capture-screenshots.ps1 to render the real tray menu.</summary>
    internal void ShowMenuForPreview(Point location) => _menu.Show(location);
#endif

    private void ShowBalloon(string text)
    {
        if (!_settings.ShowNotifications)
        {
            return;
        }

        if (text.Length > MaxBalloonTextLength)
        {
            text = text[..(MaxBalloonTextLength - 1)] + "…";
        }

        _trayIcon.BalloonTipTitle = Strings.AppName;
        _trayIcon.BalloonTipText = text;
        _trayIcon.BalloonTipIcon = ToolTipIcon.None;
        _trayIcon.ShowBalloonTip(2500);
    }

    private void RestoreTrayIcon()
    {
        // Explorer restarted: the notification icon has to be added again.
        _trayIcon.Visible = true;
        UpdateTooltip();
    }

    private void KeepStartupEntryFresh()
    {
        if (_settings.RunAtStartup)
        {
            StartupManager.SetEnabled(true);
        }
    }

    private void ExitApplication()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        _watcher.Stop();

        _windowList?.Close();
        _trayIcon.Visible = false;
        _hotkeyWindow.Dispose();

        if (_settings.RestoreOnExit)
        {
            _pinnedWindows.UnpinAll();
        }

        _trayIcon.Dispose();
        _trayIconImage?.Dispose();
        ExitThread();
    }
}
