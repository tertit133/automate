// HotkeysViewModel -- list of HotkeyBinding entries, add / remove,
// re-register with the HotkeyService whenever the list changes.
using System;
using System.Collections.ObjectModel;
using AutoMate.Models;
using AutoMate.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoMate.ViewModels;

public sealed partial class HotkeysViewModel : ObservableObject
{
    private readonly MacroStorageService _store;
    private readonly MainViewModel       _main;
    private HotkeyService?               _svc;
    private Action<HotkeyAction, string?>? _dispatch;

    public ObservableCollection<HotkeyBinding> Bindings { get; } = new();

    [ObservableProperty] private HotkeyModifier _editMods = HotkeyModifier.Control | HotkeyModifier.Alt;
    [ObservableProperty] private uint           _editVk   = 0x74; // F5
    [ObservableProperty] private HotkeyAction   _editAction = HotkeyAction.PlayMacro;
    [ObservableProperty] private string?        _editTarget;

    public HotkeysViewModel(MacroStorageService store, MainViewModel main)
    {
        _store = store;
        _main  = main;
        foreach (var b in _store.LoadConfig().Hotkeys) Bindings.Add(b);
    }

    public void AttachService(HotkeyService svc, Action<HotkeyAction, string?> dispatch)
    {
        _svc = svc;
        _dispatch = dispatch;
        ReRegister();
    }

    [RelayCommand]
    private void Add()
    {
        var b = new HotkeyBinding
        {
            Modifiers = EditMods,
            VkCode    = EditVk,
            Action    = EditAction,
            Target    = EditTarget,
        };
        Bindings.Add(b);
        Persist();
        ReRegister();
    }

    [RelayCommand]
    private void Remove(HotkeyBinding? b)
    {
        if (b is null) return;
        Bindings.Remove(b);
        Persist();
        ReRegister();
    }

    private void Persist()
    {
        var cfg = _store.LoadConfig();
        cfg.Hotkeys = new System.Collections.Generic.List<HotkeyBinding>(Bindings);
        _store.SaveConfig(cfg);
    }

    private void ReRegister()
    {
        if (_svc is null || _dispatch is null) return;
        _svc.UnregisterAll();
        foreach (var b in Bindings)
        {
            var captured = b;
            int id = _svc.Register(b.Modifiers, b.VkCode,
                                     () => _dispatch!(captured.Action, captured.Target));
            b.Id = id;
        }
    }
}
