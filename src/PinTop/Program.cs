using PinTop.Services;

namespace PinTop;

internal static class Program
{
    private const string MutexName = @"Local\PinTop.SingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                Strings.AlreadyRunning,
                Strings.AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

#if DEBUG
        // Temporary hook used by tools/capture-screenshots.ps1 to render the real forms.
        switch (Environment.GetEnvironmentVariable("PINTOP_UI_PREVIEW"))
        {
            case "windows":
                Application.Run(new WindowListForm(new PinnedWindowManager()));
                return;
            case "settings":
                Application.Run(new SettingsForm(AppSettings.Load()));
                return;
            case "menu":
                var tray = new TrayApp();
                tray.ShowMenuForPreview(new Point(360, 260));
                Application.Run();
                return;
        }
#endif

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            MessageBox.Show(e.Exception.Message, $"{Strings.AppName} — {Strings.ErrorTitle}",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        Application.Run(new TrayApp());

        GC.KeepAlive(mutex);
    }
}
