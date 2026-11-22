// MainViewModel -- composes the four tab VMs, wires global hotkeys +
// scheduler to the recorder + script runner.
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using AutoMate.Models;
using AutoMate.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoMate.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly MacroStorageService _store   = new();
    private readonly MacroPlayerService  _player  = new();
    private readonly ScriptRunnerService _script  = new();
    private readonly SchedulerService    _sched   = new();
    private HotkeyService?               _hotkeys;

    public LogViewModel      Log       { get; }
    public RecorderViewModel Recorder  { get; }
    public ScriptViewModel   Script    { get; }
    public HotkeysViewModel  Hotkeys   { get; }
    public ScheduleViewModel Schedule  { get; }

    public ObservableCollection<string> MacroNames  { get; } = new();
    public ObservableCollection<string> ScriptNames { get; } = new();

    [ObservableProperty] private string _statusText = "ready";

    public MainViewModel()
    {
        Log      = new LogViewModel();
        Recorder = new RecorderViewModel(_store, _player, Log, this);
        Script   = new ScriptViewModel(_store, _script, Log);
        Hotkeys  = new HotkeysViewModel(_store, this);
        Schedule = new ScheduleViewModel(_store, this);

        RefreshLists();
    }

    public void AttachHotkeys(IntPtr windowHandle)
    {
        _hotkeys = new HotkeyService(windowHandle);
        Hotkeys.AttachService(_hotkeys, OnHotkeyAction);
        _sched.OnFire = task => Dispatcher.CurrentDispatcher.BeginInvoke(() =>
        {
            Log.Append($"[sched] fire: {task.Cron} -> {task.Action} {task.Target}");
            OnHotkeyAction(task.Action, task.Target);
        });
        _sched.SetTasks(Schedule.Tasks);
        _sched.Start();
    }

    public void RefreshLists()
    {
        MacroNames.Clear();
        foreach (var n in _store.ListMacros()) MacroNames.Add(n);
        ScriptNames.Clear();
        foreach (var n in _store.ListScripts()) ScriptNames.Add(n);
    }

    public void SyncScheduler() => _sched.SetTasks(Schedule.Tasks);

    private CancellationTokenSource? _runCts;

    public void OnHotkeyAction(HotkeyAction action, string? target)
    {
        switch (action)
        {
            case HotkeyAction.PlayMacro when !string.IsNullOrEmpty(target):
                var m = _store.LoadMacro(target);
                if (m is null) { Log.Append($"[hotkey] macro not found: {target}"); return; }
                _runCts = new CancellationTokenSource();
                _ = _player.PlayAsync(m, _runCts.Token);
                Log.Append($"[hotkey] playing macro '{target}' ({m.Steps.Count} steps)");
                break;

            case HotkeyAction.RunScript when !string.IsNullOrEmpty(target):
                var src = _store.LoadScript(target);
                if (src is null) { Log.Append($"[hotkey] script not found: {target}"); return; }
                _runCts = new CancellationTokenSource();
                _ = _script.RunAsync(src, Log.Append, _runCts.Token)
                            .ContinueWith(t => Log.Append($"[script] {t.Result}"));
                Log.Append($"[hotkey] running script '{target}'");
                break;

            case HotkeyAction.StopPlayback:
                _runCts?.Cancel();
                Log.Append("[hotkey] stop requested");
                break;
        }
    }
}
