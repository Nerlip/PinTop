# Contributing to PinTop

Thanks for taking the time to contribute!

## Reporting bugs

Please include:

- Windows version (for example, Windows 11 24H2) and display scaling;
- the app version from the tray menu → *About*;
- what you did, what you expected, and what actually happened;
- which application the window belonged to;
- the contents of `%APPDATA%\PinTop\settings.json`, if relevant.

## Suggesting features

Keep in mind the scope of the project: a small, dependency-free tray utility with no telemetry and no
network access. Features that need an installer, a service or admin rights are unlikely to be accepted.

## Pull requests

1. Fork the repository and create a branch from `main`.
2. Build with `dotnet build PinTop.sln -c Release`.
3. Keep the existing code style: 4-space indent, file-scoped namespaces, nullable enabled.
4. The UI is painted by hand with GDI+ (see `src/PinTop/UI/Theme.cs`). Avoid timers, animations, extra
   assemblies or image assets — the app is meant to stay light on memory.
5. Describe the change and how you tested it (which Windows version, which apps).
6. If you change the UI, update the screenshots with `tools/capture-screenshots.ps1` when possible.

Small, focused commits with a clear message are appreciated.

## Code map

| Path | Responsibility |
| --- | --- |
| `src/PinTop/Interop/NativeMethods.cs` | Win32 P/Invoke declarations |
| `src/PinTop/Services/WindowTargeting.cs` | resolve the cursor/foreground window, filter unusable windows |
| `src/PinTop/Services/WindowPinner.cs` | toggle `WS_EX_TOPMOST` |
| `src/PinTop/Services/HotkeyWindow.cs` | message-only window + `RegisterHotKey` |
| `src/PinTop/Services/AppSettings.cs` | JSON settings in `%APPDATA%\PinTop` |
| `src/PinTop/Services/Strings.cs` | Russian/English UI strings |
| `src/PinTop/TrayApp.cs` | tray icon, menu and orchestration |
| `src/PinTop/UI/Theme.cs` | light/dark palettes, fonts and drawing helpers |
| `src/PinTop/UI/` | hand-drawn controls (cards, buttons, switches) and the two dialogs |

## License

By contributing you agree that your changes are released under the [MIT license](LICENSE).
