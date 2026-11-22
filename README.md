<div align="center">

# AutoMate

**Windows automation studio: macro recorder, C# scripting, global hotkeys, cron scheduler.**

[![build](https://github.com/tertit133/automate/actions/workflows/build.yml/badge.svg)](https://github.com/tertit133/automate/actions/workflows/build.yml)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8-512BD4.svg?logo=dotnet)](https://dotnet.microsoft.com/)
[![platform](https://img.shields.io/badge/platform-Windows%20x64-0078D6.svg?logo=windows)](https://learn.microsoft.com/en-us/windows/win32/)

WPF + CommunityToolkit.Mvvm * Roslyn Scripting * SendInput

</div>

> AutoHotkey's niche with a proper GUI, C# as the scripting language,
> and cron for scheduling. Record a macro with the real mouse +
> keyboard, replay it at the same tempo, bind it to a global hotkey,
> or trigger it on a cron entry. Scripts talk to a clean `Mouse` /
> `Keyboard` / `Screen` / `Log` globals surface, no `using` ceremony
> required.

## Features

- **Macro recorder** -- global low-level `WH_KEYBOARD_LL` + `WH_MOUSE_LL`
  hooks capture every keystroke, mouse move, button and wheel event
  with real timing. Replay goes through `SendInput` so applications see
  genuine input.
- **Macro library** -- macros persist as JSON under
  `%APPDATA%\AutoMate\macros\`. Delete, rename, play, pass to a hotkey
  binding.
- **C# scripting** -- Roslyn `CSharpScript` evaluates your snippet
  against an `AutomationApi` globals instance, so you can write
  `Mouse.Click(100, 200); Keyboard.Type("Hello"); Log("done");` without
  any using declarations. Supports `Sleep(ms)` and honors a cancellation
  token so a stop hotkey breaks out of a long run.
- **Global hotkeys** -- bind any (modifiers + VK) combo to:
  - `PlayMacro` -- fire a macro from the library,
  - `RunScript` -- execute a saved script,
  - `StopPlayback` -- cancel the current macro or script.
  Hotkeys use `RegisterHotKey` + a hidden `HwndSource` message hook, so
  they work even when AutoMate is minimised.
- **Cron scheduler** -- plain 5-field cron (`minute hour dom month dow`)
  with `*`, `*/n`, ranges and lists. Resolution is one minute; the
  scheduler ticks every 20 s so clock jitter can't miss a slot.
- **Dark theme** -- a tiny WPF resource dictionary; no 50 MB theme
  packs pulled in.

## Build

Prerequisites:

- Windows 10/11 x64
- .NET 8 SDK (`global.json` pins the feature band)

```bat
dotnet publish -c Release -r win-x64 --self-contained false -o publish
publish\AutoMate.exe
```

No admin needed -- low-level hooks and `SendInput` work from a normal
user account.

## Architecture

```
AutoMate/
+-- Native/
|   +-- NativeMethods.cs      P/Invoke: SetWindowsHookEx, SendInput,
|   |                          RegisterHotKey, GetSystemMetrics
|   +-- NativeStructs.cs      KBDLLHOOKSTRUCT, MSLLHOOKSTRUCT, INPUT,
|                              HotkeyMods, WM_* constants
+-- Models/
|   +-- Models.cs             MacroStep hierarchy (KeyStep / MouseMoveStep /
|   |                          MouseButtonStep / MouseWheelStep / DelayStep),
|   |                          Macro, HotkeyBinding, ScheduledTask
|   +-- HotkeyActionValues.cs helper for XAML combos
+-- Services/
|   +-- MacroStorageService.cs    JSON persistence under %APPDATA%\AutoMate
|   +-- MacroRecorderService.cs   global LL hooks -> timestamped MacroStep stream
|   +-- MacroPlayerService.cs     SendInput replay, respects CancellationToken
|   +-- AutomationApi.cs          host object scripts bind to (Mouse/Keyboard/Screen)
|   +-- ScriptRunnerService.cs    Roslyn CSharpScript.RunAsync + globals
|   +-- HotkeyService.cs          RegisterHotKey + HwndSource hook pump
|   +-- CronParser.cs             5-field cron with *, */n, lists, ranges
|   +-- SchedulerService.cs       timer-based evaluator; fires enabled tasks on match
+-- ViewModels/              CommunityToolkit.Mvvm [ObservableProperty] + [RelayCommand]
+-- Views/MainWindow.xaml    TabControl: Recorder / Script / Hotkeys / Schedule / Log
+-- App.xaml                 dark palette resources
+-- app.manifest             PerMonitorV2 DPI awareness
+-- global.json              8.0 SDK pinned
```

## Script API

```csharp
// Globals exposed to any script (AutomationApi members become top-level):
Log("hello");
Sleep(500);

// mouse
Mouse.Move(100, 200);
Mouse.Click(400, 350);
Mouse.RightClick(600, 100);
Mouse.Scroll(-120);            // one notch down
var (x, y) = Mouse.Position();

// keyboard
Keyboard.Type("Hello there");
Keyboard.Press(0x74);          // F5
Keyboard.Down(0xA2);           // Ctrl down
Keyboard.Up  (0xA2);

// screen
Log($"{Screen.Width}x{Screen.Height}");
```

Scripts run in-process, so they can `using` any .NET BCL namespace --
`System.IO`, `System.Net.Http`, `System.Text.Json` etc. all work
without extra references.

## Cron examples

```
* * * * *         every minute
*/5 * * * *       every 5 minutes
0 9 * * 1-5       every weekday at 09:00
0 */2 * * *       every 2 hours on the hour
30 22 * * 5       Fridays at 22:30
```

## License

MIT. See `LICENSE`.
