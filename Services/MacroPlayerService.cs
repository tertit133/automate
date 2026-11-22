// MacroPlayerService -- replay a recorded Macro via SendInput. Honors
// a cancellation token so a global hotkey can stop playback mid-way.
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMate.Models;
using AutoMate.Native;

namespace AutoMate.Services;

public sealed class MacroPlayerService
{
    public Task PlayAsync(Macro macro, CancellationToken ct)
        => Task.Run(() =>
        {
            foreach (var step in macro.Steps)
            {
                if (ct.IsCancellationRequested) return;
                if (step.ElapsedMs > 0)
                {
                    try { Task.Delay(step.ElapsedMs, ct).Wait(); }
                    catch { return; }
                }

                switch (step)
                {
                    case KeyStep k:
                        Send(KeyInput(k.VkCode, k.Down));
                        break;
                    case MouseMoveStep m:
                        Send(MouseMoveAbs(m.X, m.Y));
                        break;
                    case MouseButtonStep b:
                        Send(MouseMoveAbs(b.X, b.Y));
                        Send(MouseButton(b.Button, b.Down));
                        break;
                    case MouseWheelStep w:
                        Send(MouseWheel(w.Delta));
                        break;
                    case DelayStep d:
                        try { Task.Delay(d.Ms, ct).Wait(); } catch { return; }
                        break;
                }
            }
        }, ct);

    // ---- SendInput helpers ------------------------------------------------
    private static void Send(INPUT inp)
        => NativeMethods.SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());

    private static INPUT KeyInput(uint vk, bool down)
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

    private static INPUT MouseMoveAbs(int x, int y)
    {
        int screenW = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        int screenH = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        return new INPUT
        {
            type = 0,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = (int)((x * 65535.0) / screenW),
                    dy = (int)((y * 65535.0) / screenH),
                    dwFlags = (uint)(MouseEventF.Move | MouseEventF.Absolute),
                }
            },
        };
    }

    private static INPUT MouseButton(MouseButton b, bool down)
    {
        MouseEventF f = (b, down) switch
        {
            (Models.MouseButton.Left,   true)  => MouseEventF.LeftDown,
            (Models.MouseButton.Left,   false) => MouseEventF.LeftUp,
            (Models.MouseButton.Right,  true)  => MouseEventF.RightDown,
            (Models.MouseButton.Right,  false) => MouseEventF.RightUp,
            (Models.MouseButton.Middle, true)  => MouseEventF.MiddleDown,
            (Models.MouseButton.Middle, false) => MouseEventF.MiddleUp,
            _ => MouseEventF.LeftDown,
        };
        return new INPUT
        {
            type = 0,
            U = new InputUnion { mi = new MOUSEINPUT { dwFlags = (uint)f } }
        };
    }

    private static INPUT MouseWheel(int delta)
        => new()
        {
            type = 0,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    mouseData = (uint)delta,
                    dwFlags   = (uint)MouseEventF.Wheel,
                }
            },
        };

    // local alias so the switch pattern reads nicely without the full ns
    private static class Marshal
    {
        public static int SizeOf<T>() => System.Runtime.InteropServices.Marshal.SizeOf<T>();
    }
}
