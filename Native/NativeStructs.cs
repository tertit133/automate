// Native constants + marshalled structures for AutoMate's input layer.
using System;
using System.Runtime.InteropServices;

namespace AutoMate.Native;

// SetWindowsHookEx hook types
public static class HookIds
{
    public const int WH_KEYBOARD_LL = 13;
    public const int WH_MOUSE_LL    = 14;
}

// Window-message constants we care about.
public static class WM
{
    public const int WM_HOTKEY      = 0x0312;

    public const int WM_KEYDOWN     = 0x0100;
    public const int WM_KEYUP       = 0x0101;
    public const int WM_SYSKEYDOWN  = 0x0104;
    public const int WM_SYSKEYUP    = 0x0105;

    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP   = 0x0202;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP   = 0x0205;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_MBUTTONUP   = 0x0208;
    public const int WM_MOUSEMOVE   = 0x0200;
    public const int WM_MOUSEWHEEL  = 0x020A;
}

[Flags]
public enum HotkeyMods : uint
{
    None     = 0,
    Alt      = 0x0001,
    Control  = 0x0002,
    Shift    = 0x0004,
    Win      = 0x0008,
    NoRepeat = 0x4000,
}

[Flags]
public enum MouseEventF : uint
{
    Absolute   = 0x8000,
    Move       = 0x0001,
    LeftDown   = 0x0002,
    LeftUp     = 0x0004,
    RightDown  = 0x0008,
    RightUp    = 0x0010,
    MiddleDown = 0x0020,
    MiddleUp   = 0x0040,
    Wheel      = 0x0800,
}

[Flags]
public enum KeyEventF : uint
{
    KeyDown    = 0x0000,
    ExtendedKey = 0x0001,
    KeyUp      = 0x0002,
    Unicode    = 0x0004,
    Scancode   = 0x0008,
}

[StructLayout(LayoutKind.Sequential)]
public struct KBDLLHOOKSTRUCT
{
    public uint vkCode;
    public uint scanCode;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct MSLLHOOKSTRUCT
{
    public POINT pt;
    public uint mouseData;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT { public int X; public int Y; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct INPUT
{
    public uint type;    // 0 = mouse, 1 = keyboard
    public InputUnion U;
}

[StructLayout(LayoutKind.Explicit)]
public struct InputUnion
{
    [FieldOffset(0)] public MOUSEINPUT    mi;
    [FieldOffset(0)] public KEYBDINPUT    ki;
    [FieldOffset(0)] public HARDWAREINPUT hi;
}

[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
    public int dx, dy;
    public uint mouseData, dwFlags, time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
    public ushort wVk, wScan;
    public uint dwFlags, time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }

public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
