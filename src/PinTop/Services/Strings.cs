using System.Globalization;
using System.Reflection;
using PinTop.Interop;

namespace PinTop.Services;

/// <summary>Tiny two-language string table (Russian / English), chosen from the OS UI language.</summary>
internal static class Strings
{
    private static readonly bool Ru =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);

    private static string T(string ru, string en) => Ru ? ru : en;

    public static string AppName => "PinTop";

    public static string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version is null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public static string AlreadyRunning => T(
        "PinTop уже запущен — его иконка находится в области уведомлений.",
        "PinTop is already running — look for its icon in the notification area.");

    public static string PinWindow => T("Закрепить окно", "Pin window");

    public static string UnpinWindow => T("Открепить окно", "Unpin window");

    public static string UnderCursor => T("под курсором", "under cursor");

    public static string ActiveWindow => T("активное", "active");

    public static string WindowList => T("Список окон…", "Window list…");

    public static string Settings => T("Настройки…", "Settings…");

    public static string About => T("О программе", "About");

    public static string Exit => T("Выход", "Exit");

    public static string PinnedMessage(string title) => T($"Закреплено: {title}", $"Pinned: {title}");

    public static string UnpinnedMessage(string title) => T($"Откреплено: {title}", $"Unpinned: {title}");

    public static string NoSuitableWindow => T(
        "Под указателем нет окна, которое можно закрепить.",
        "There is no window that can be pinned under the pointer.");

    public static string HotkeyRegistrationFailed(string hotkey) => T(
        $"Не удалось зарегистрировать {hotkey}: сочетание уже занято другой программой. Выберите другое в настройках.",
        $"Could not register {hotkey}: the shortcut is already used by another program. Pick another one in Settings.");

    public static string TrayTooltip(int pinnedCount) => T(
        pinnedCount == 0
            ? "PinTop — закреплённых окон нет"
            : $"PinTop — закреплено окон: {pinnedCount}",
        pinnedCount == 0
            ? "PinTop — no pinned windows"
            : $"PinTop — pinned windows: {pinnedCount}");

    public static string Welcome => T(
        "PinTop работает в области уведомлений. Нажмите горячую клавишу, чтобы закрепить окно поверх остальных.",
        "PinTop lives in the notification area. Press the hotkey to keep a window above the others.");

    public static string AboutText => T(
        $"PinTop {Version}\n\nЗакрепляет любое окно поверх остальных одним нажатием горячей клавиши.\n\nОткрытый исходный код, лицензия MIT.\n{RepositoryUrl}",
        $"PinTop {Version}\n\nKeeps any window above the others with a single hotkey press.\n\nOpen source, MIT license.\n{RepositoryUrl}");

    public static string WindowListTitle => T("Окна — PinTop", "Windows — PinTop");

    public static string ColumnWindow => T("Окно", "Window");

    public static string ColumnProcess => T("Процесс", "Process");

    public static string WindowListHint => T(
        "Отметьте окна, которые должны оставаться поверх остальных. Двойной щелчок переключает на окно.",
        "Tick the windows that should stay above the others. Double-click brings a window to the front.");

    public static string NoWindows => T("Нет окон для отображения.", "No windows to show.");

    public static string Refresh => T("Обновить", "Refresh");

    public static string UnpinAll => T("Открепить все", "Unpin all");

    public static string Close => T("Закрыть", "Close");

    public static string SettingsTitle => T("Настройки — PinTop", "Settings — PinTop");

    public static string GroupHotkey => T("Горячая клавиша", "Hotkey");

    public static string HotkeyLabel => T("Закрепить / открепить окно:", "Pin / unpin window:");

    public static string HotkeyHint => T(
        "Нажмите сочетание клавиш в поле. Backspace — отключить горячую клавишу.",
        "Press a key combination in the field. Backspace disables the hotkey.");

    public static string HotkeyNotSet => T("(не задано)", "(not set)");

    public static string HotkeyNeedsModifier => T(
        "Нужен хотя бы один модификатор (Ctrl, Alt, Shift) или функциональная клавиша F1–F24.",
        "Use at least one modifier (Ctrl, Alt, Shift) or a function key F1–F24.");

    public static string GroupBehavior => T("Поведение", "Behavior");

    public static string TargetLabel => T("Что закреплять:", "What to pin:");

    public static string TargetCursor => T("Окно под курсором", "Window under the cursor");

    public static string TargetActive => T("Активное окно", "Active window");

    public static string ShowNotifications => T("Показывать уведомления", "Show notifications");

    public static string RestoreOnExit => T("Снимать закрепление при выходе", "Unpin windows on exit");

    public static string RunAtStartup => T("Запускать при входе в Windows", "Start with Windows");

    public static string Save => T("Сохранить", "Save");

    public static string Cancel => T("Отмена", "Cancel");

    public static string Reset => T("Сброс", "Reset");

    public static string ErrorTitle => T("Ошибка", "Error");

    public static string RepositoryUrl => "https://github.com/burmalda/pintop";

    public static string FormatHotkey(uint modifiers, Keys key)
    {
        if (key == Keys.None)
        {
            return HotkeyNotSet;
        }

        var parts = new List<string>(4);
        if ((modifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & NativeMethods.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(FormatKey(key));
        return string.Join(" + ", parts);
    }

    private static string FormatKey(Keys key)
    {
        var code = (int)key;

        if (code is >= (int)Keys.D0 and <= (int)Keys.D9)
        {
            return (code - (int)Keys.D0).ToString(CultureInfo.InvariantCulture);
        }

        if (code is >= (int)Keys.NumPad0 and <= (int)Keys.NumPad9)
        {
            return $"Num {(code - (int)Keys.NumPad0).ToString(CultureInfo.InvariantCulture)}";
        }

        return key switch
        {
            Keys.Oemtilde => "`",
            Keys.OemMinus => "-",
            Keys.Oemplus => "+",
            Keys.OemOpenBrackets => "[",
            Keys.OemCloseBrackets => "]",
            Keys.OemPipe => "\\",
            Keys.OemSemicolon => ";",
            Keys.OemQuotes => "'",
            Keys.Oemcomma => ",",
            Keys.OemPeriod => ".",
            Keys.OemQuestion => "/",
            _ => key.ToString(),
        };
    }
}
