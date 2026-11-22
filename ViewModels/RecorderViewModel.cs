// RecorderViewModel -- record / stop / save / play a Macro.
using System.Threading;
using AutoMate.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMate.ViewModels;

public sealed partial class RecorderViewModel : ObservableObject
{
    private readonly MacroStorageService _store;
    private readonly MacroPlayerService  _player;
    private readonly LogViewModel        _log;
    private readonly MainViewModel       _main;

    private readonly MacroRecorderService _rec = new();
    private CancellationTokenSource? _playCts;

    [ObservableProperty] private string _macroName = "untitled";
    [ObservableProperty] private string _notes     = "";
    [ObservableProperty] private bool   _isRecording;
    [ObservableProperty] private int    _stepCount;
    [ObservableProperty] private string? _selectedMacro;

    public RecorderViewModel(MacroStorageService store, MacroPlayerService player,
                              LogViewModel log, MainViewModel main)
    {
        _store  = store;
        _player = player;
        _log    = log;
        _main   = main;
    }

    [RelayCommand]
    private void StartRecording()
    {
        _rec.Start();
        IsRecording = true;
        _log.Append($"[rec] started: {MacroName}");
    }

    [RelayCommand]
    private void StopAndSave()
    {
        if (!_rec.IsRecording) return;
        var macro = _rec.Stop(MacroName);
        macro.Notes = Notes;
        _store.SaveMacro(macro);
        StepCount  = macro.Steps.Count;
        IsRecording = false;
        _main.RefreshLists();
        _log.Append($"[rec] saved {macro.Name}: {macro.Steps.Count} steps");
    }

    [RelayCommand]
    private void Play()
    {
        if (string.IsNullOrEmpty(SelectedMacro)) return;
        var m = _store.LoadMacro(SelectedMacro);
        if (m is null) { _log.Append($"[play] missing: {SelectedMacro}"); return; }
        _playCts = new CancellationTokenSource();
        _ = _player.PlayAsync(m, _playCts.Token);
        _log.Append($"[play] started '{m.Name}': {m.Steps.Count} steps");
    }

    [RelayCommand]
    private void Stop()
    {
        _playCts?.Cancel();
        _log.Append("[play] stopped");
    }

    [RelayCommand]
    private void Delete()
    {
        if (string.IsNullOrEmpty(SelectedMacro)) return;
        _store.DeleteMacro(SelectedMacro);
        _main.RefreshLists();
        _log.Append($"[rec] deleted {SelectedMacro}");
        SelectedMacro = null;
    }
}
