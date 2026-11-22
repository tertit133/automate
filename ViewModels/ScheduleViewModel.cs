// ScheduleViewModel -- cron-driven schedule entries, persisted alongside
// the hotkey list in config.json.
using System.Collections.ObjectModel;
using AutoMate.Models;
using AutoMate.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMate.ViewModels;

public sealed partial class ScheduleViewModel : ObservableObject
{
    private readonly MacroStorageService _store;
    private readonly MainViewModel       _main;

    public ObservableCollection<ScheduledTask> Tasks { get; } = new();

    [ObservableProperty] private string       _editCron   = "0 9 * * 1-5";   // 09:00 Mon-Fri
    [ObservableProperty] private HotkeyAction _editAction = HotkeyAction.PlayMacro;
    [ObservableProperty] private string?      _editTarget;
    [ObservableProperty] private string       _lastParse  = "";

    public ScheduleViewModel(MacroStorageService store, MainViewModel main)
    {
        _store = store;
        _main  = main;
        foreach (var t in _store.LoadConfig().Schedule) Tasks.Add(t);
    }

    [RelayCommand]
    private void Add()
    {
        var parsed = CronParser.Parse(EditCron);
        if (parsed is null) { LastParse = "invalid cron"; return; }
        Tasks.Add(new ScheduledTask { Cron = EditCron, Action = EditAction, Target = EditTarget });
        Persist();
        _main.SyncScheduler();
        LastParse = "ok";
    }

    [RelayCommand]
    private void Remove(ScheduledTask? t)
    {
        if (t is null) return;
        Tasks.Remove(t);
        Persist();
        _main.SyncScheduler();
    }

    [RelayCommand]
    private void ToggleEnabled(ScheduledTask? t)
    {
        if (t is null) return;
        t.Enabled = !t.Enabled;
        Persist();
        _main.SyncScheduler();
    }

    private void Persist()
    {
        var cfg = _store.LoadConfig();
        cfg.Schedule = new System.Collections.Generic.List<ScheduledTask>(Tasks);
        _store.SaveConfig(cfg);
    }
}
