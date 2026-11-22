// AutomationApi -- the host object the scripting engine exposes.
// Scripts reference Mouse, Keyboard, Screen, Log through this one
// globals instance so the public surface stays tiny.
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoMate.Native;

namespace AutoMate.Services;

public sealed class AutomationApi
{
    public MouseApi    Mouse    { get; }
    public KeyboardApi Keyboard { get; }
    public ScreenApi   Screen   { get; }
    public Action<string> Log { get; }
    public CancellationToken Cancellation { get; }

    public AutomationApi(Action<string> log, CancellationToken ct)
    {
        Log          = log;
        Cancellation = ct;
        Mouse        = new MouseApi(log, ct);
        Keyboard     = new KeyboardApi(log, ct);
        Screen       = new ScreenApi();
    }

    public void Sleep(int ms)
    {
        try { Task.Delay(ms, Cancellation).Wait(); } catch { }
    }
}

public sealed class MouseApi
{
    private readonly Action<string> _log;
    private readonly CancellationToken _ct;
    internal MouseApi(Action<string> log, CancellationToken ct) { _log = log; _ct = ct; }

    public void Move(int x, int y) => Send(Inputs.MoveAbs(x, y));

    public void Click(int x, int y)
    {
        Move(x, y);
        Send(Inputs.ButtonEv(MouseEventF.LeftDown));
        Send(Inputs.ButtonEv(MouseEventF.LeftUp));
    }

    public void RightClick(int x, int y)
    {
        Move(x, y);
        Send(Inputs.ButtonEv(MouseEventF.RightDown));
        Send(Inputs.ButtonEv(MouseEventF.RightUp));
    }

    public void Scroll(int delta)
    {
        Send(new INPUT
        {
            type = 0,
            U = new InputUnion { mi = new MOUSEINPUT { mouseData = (uint)delta,
                                                        dwFlags = (uint)MouseEventF.Wheel } }
        });
    }

    public (int X, int Y) Position()
    {
        NativeMethods.GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    private void Send(INPUT inp)
    {
        _ct.ThrowIfCancellationRequested();
        NativeMethods.SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }
}

public sealed class KeyboardApi
{
    private readonly Action<string> _log;
    private readonly CancellationToken _ct;
    internal KeyboardApi(Action<string> log, CancellationToken ct) { _log = log; _ct = ct; }

    /// <summary>Types the string via Unicode synthesized keystrokes.</summary>
    public void Type(string text)
    {
        foreach (var ch in text)
        {
            _ct.ThrowIfCancellationRequested();
            Send(Inputs.UnicodeKey(ch, down: true));
            Send(Inputs.UnicodeKey(ch, down: false));
        }
    }

    public void Press(uint vkCode)
    {
        Send(Inputs.VkKey(vkCode, true));
        Send(Inputs.VkKey(vkCode, false));
    }

    public void Down(uint vkCode) => Send(Inputs.VkKey(vkCode, true));
    public void Up  (uint vkCode) => Send(Inputs.VkKey(vkCode, false));

    private void Send(INPUT inp)
    {
        _ct.ThrowIfCancellationRequested();
        NativeMethods.SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }
}

public sealed class ScreenApi
{
    public int Width  => NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
    public int Height => NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
}

// Tiny helpers so the two above classes stay noise-free.
internal static class Inputs
{
    public static INPUT MoveAbs(int x, int y)
    {
        int w = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        int h = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        return new INPUT
        {
            type = 0,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = (int)((x * 65535.0) / w),
                    dy = (int)((y * 65535.0) / h),
                    dwFlags = (uint)(MouseEventF.Move | MouseEventF.Absolute),
                }
            },
        };
    }

    public static INPUT ButtonEv(MouseEventF flags)
        => new() { type = 0, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = (uint)flags } } };

    public static INPUT VkKey(uint vk, bool down)
        => new()
        {
            type = 1,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk     = (ushort)vk,
                    dwFlags = down ? (uint)KeyEventF.KeyDown : (uint)KeyEventF.KeyUp,
                }
            },
        };

    public static INPUT UnicodeKey(char ch, bool down)
        => new()
        {
            type = 1,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wScan   = ch,
                    dwFlags = (down ? (uint)KeyEventF.KeyDown : (uint)KeyEventF.KeyUp)
                              | (uint)KeyEventF.Unicode,
                }
            },
        };
}
