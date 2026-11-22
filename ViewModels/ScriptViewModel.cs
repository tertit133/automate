// ScriptViewModel -- edit / run / save a C# script that drives the
// AutomationApi globals.
using System.Threading;
using AutoMate.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMate.ViewModels;

public sealed partial class ScriptViewModel : ObservableObject
{
    private readonly MacroStorageService _store;
    private readonly ScriptRunnerService _run;
    private readonly LogViewModel        _log;
    private CancellationTokenSource?     _cts;

    [ObservableProperty] private string _name = "snippet";
    [ObservableProperty] private string _source = SampleSource;

    public ScriptViewModel(MacroStorageService store, ScriptRunnerService run, LogViewModel log)
    {
        _store = store;
        _run   = run;
        _log   = log;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task Run()
    {
        _cts = new CancellationTokenSource();
        _log.Append("[script] running...");
        var result = await _run.RunAsync(Source, _log.Append, _cts.Token);
        _log.Append("[script] " + result);
    }

    [RelayCommand] private void Stop()  { _cts?.Cancel(); }
    [RelayCommand] private void Save()  { _store.SaveScript(Name, Source); _log.Append($"[script] saved {Name}"); }
    [RelayCommand] private void Load()
    {
        var src = _store.LoadScript(Name);
        if (src is null) { _log.Append($"[script] missing {Name}"); return; }
        Source = src;
        _log.Append($"[script] loaded {Name}");
    }

    private const string SampleSource = @"// AutomationApi members available as globals:
//   Mouse.Move/Click/RightClick/Scroll/Position
//   Keyboard.Type/Press/Down/Up
//   Screen.Width/Height
//   Log(string)
//   Sleep(ms)

Log(string.Format(""screen is {0}x{1}"", Screen.Width, Screen.Height));
var (x, y) = Mouse.Position();
Log(string.Format(""mouse at {0},{1}"", x, y));
";
}
