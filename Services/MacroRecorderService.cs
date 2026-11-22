// MacroRecorderService -- install global low-level keyboard + mouse
// hooks, timestamp each event, convert to MacroStep records.
//
// Hooks run on the thread that installed them, so we Install() on the
// UI thread and the hook callbacks are invoked by that same message
// pump. Every callback is O(1) -- we just append to a list.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoMate.Models;
using AutoMate.Native;

namespace AutoMate.Services;

public sealed class MacroRecorderService : IDisposable
{
    private IntPtr   _kbdHook  = IntPtr.Zero;
    private IntPtr   _mouseHook = IntPtr.Zero;
    private HookProc? _kbdProc;
    private HookProc? _mouseProc;

    private Stopwatch _sw = new();
    private int       _lastMs;
    private readonly List<MacroStep> _steps = new();

    public bool IsRecording { get; private set; }

    public void Start()
    {
        if (IsRecording) return;
        _steps.Clear();
        _sw.Restart();
        _lastMs = 0;

        _kbdProc   = KbdHookProc;
        _mouseProc = MouseHookProc;

        var hMod = NativeMethods.GetModuleHandle(null);
        _kbdHook   = NativeMethods.SetWindowsHookEx(HookIds.WH_KEYBOARD_LL, _kbdProc, hMod, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(HookIds.WH_MOUSE_LL,    _mouseProc, hMod, 0);
        IsRecording = true;
    }

    public Macro Stop(string name)
    {
        if (_kbdHook != IntPtr.Zero)   { NativeMethods.UnhookWindowsHookEx(_kbdHook);   _kbdHook   = IntPtr.Zero; }
        if (_mouseHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(_mouseHook); _mouseHook = IntPtr.Zero; }
        _kbdProc = _mouseProc = null;
        IsRecording = false;
        return new Macro { Name = name, Steps = new List<MacroStep>(_steps) };
    }

    public void Dispose()
    {
        if (IsRecording) Stop("discarded");
    }

    private IntPtr KbdHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int msg  = wParam.ToInt32();
            bool down = msg == WM.WM_KEYDOWN || msg == WM.WM_SYSKEYDOWN;
            bool up   = msg == WM.WM_KEYUP   || msg == WM.WM_SYSKEYUP;
            if (down || up)
            {
                Append(new KeyStep { VkCode = data.vkCode, Down = down });
            }
        }
        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            int msg  = wParam.ToInt32();
            switch (msg)
            {
                case WM.WM_MOUSEMOVE:
                    Append(new MouseMoveStep { X = data.pt.X, Y = data.pt.Y });
                    break;
                case WM.WM_LBUTTONDOWN: Append(Btn(data, MouseButton.Left,   true));  break;
                case WM.WM_LBUTTONUP:   Append(Btn(data, MouseButton.Left,   false)); break;
                case WM.WM_RBUTTONDOWN: Append(Btn(data, MouseButton.Right,  true));  break;
                case WM.WM_RBUTTONUP:   Append(Btn(data, MouseButton.Right,  false)); break;
                case WM.WM_MBUTTONDOWN: Append(Btn(data, MouseButton.Middle, true));  break;
                case WM.WM_MBUTTONUP:   Append(Btn(data, MouseButton.Middle, false)); break;
                case WM.WM_MOUSEWHEEL:
                    short delta = (short)((data.mouseData >> 16) & 0xFFFF);
                    Append(new MouseWheelStep { Delta = delta, X = data.pt.X, Y = data.pt.Y });
                    break;
            }
        }
        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private static MouseButtonStep Btn(MSLLHOOKSTRUCT d, MouseButton b, bool down)
        => new() { Button = b, Down = down, X = d.pt.X, Y = d.pt.Y };

    private void Append(MacroStep step)
    {
        int now = (int)_sw.ElapsedMilliseconds;
        step.ElapsedMs = now - _lastMs;
        _lastMs = now;
        _steps.Add(step);
    }
}
