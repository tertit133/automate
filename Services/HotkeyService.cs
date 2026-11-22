// HotkeyService -- register global hotkeys via RegisterHotKey, pump
// WM_HOTKEY through a hidden WPF HwndSource hook, and invoke the
// per-binding Action on the UI thread.
using System;
using System.Collections.Generic;
using System.Windows.Interop;
using AutoMate.Models;
using AutoMate.Native;

namespace AutoMate.Services;

public sealed class HotkeyService : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly HwndSource _src;
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId = 1;

    public HotkeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _src  = HwndSource.FromHwnd(hwnd);
        _src?.AddHook(WndProc);
    }

    public int Register(HotkeyModifier mods, uint vk, Action action)
    {
        int id = _nextId++;
        _callbacks[id] = action;
        if (!NativeMethods.RegisterHotKey(_hwnd, id, (uint)mods, vk))
        {
            _callbacks.Remove(id);
            return 0;
        }
        return id;
    }

    public void Unregister(int id)
    {
        if (_callbacks.Remove(id))
            NativeMethods.UnregisterHotKey(_hwnd, id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _callbacks.Keys)
            NativeMethods.UnregisterHotKey(_hwnd, id);
        _callbacks.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        _src?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_callbacks.TryGetValue(id, out var cb))
            {
                try { cb(); } catch { /* keep hook alive */ }
                handled = true;
            }
        }
        return IntPtr.Zero;
    }
}
