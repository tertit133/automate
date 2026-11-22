// Models.cs -- the storage shape for macros + schedule entries.
// System.Text.Json (de)serializes these via a shared polymorphic
// discriminator on MacroStep.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoMate.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$k")]
[JsonDerivedType(typeof(DelayStep),      "delay")]
[JsonDerivedType(typeof(KeyStep),        "key")]
[JsonDerivedType(typeof(MouseMoveStep),  "move")]
[JsonDerivedType(typeof(MouseButtonStep),"mbtn")]
[JsonDerivedType(typeof(MouseWheelStep), "wheel")]
public abstract class MacroStep
{
    public int ElapsedMs { get; set; }
}

public sealed class DelayStep : MacroStep { public int Ms { get; set; } }

public sealed class KeyStep : MacroStep
{
    public uint VkCode { get; set; }
    public bool Down   { get; set; }     // false = up
}

public sealed class MouseMoveStep : MacroStep
{
    public int X { get; set; }
    public int Y { get; set; }
}

public enum MouseButton { Left, Right, Middle }

public sealed class MouseButtonStep : MacroStep
{
    public MouseButton Button { get; set; }
    public bool        Down   { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class MouseWheelStep : MacroStep
{
    public int Delta { get; set; }       // signed wheel delta
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class Macro
{
    public string              Name    { get; set; } = "untitled";
    public string?             Notes   { get; set; }
    public DateTime            Saved   { get; set; } = DateTime.UtcNow;
    public List<MacroStep>     Steps   { get; set; } = new();
}

// ---- hotkey + schedule --------------------------------------------------

[Flags]
public enum HotkeyModifier { None = 0, Alt = 1, Control = 2, Shift = 4, Win = 8 }

public enum HotkeyAction { PlayMacro, RunScript, StopPlayback }

public sealed class HotkeyBinding
{
    public int            Id        { get; set; }     // RegisterHotKey id
    public HotkeyModifier Modifiers { get; set; }
    public uint           VkCode    { get; set; }
    public HotkeyAction   Action    { get; set; }
    public string?        Target    { get; set; }     // macro / script name

    public string Display
        => $"{ModStr(Modifiers)}{(char)VkCode}  ->  {Action} {Target}";

    private static string ModStr(HotkeyModifier m)
    {
        var s = "";
        if ((m & HotkeyModifier.Control) != 0) s += "Ctrl+";
        if ((m & HotkeyModifier.Alt)     != 0) s += "Alt+";
        if ((m & HotkeyModifier.Shift)   != 0) s += "Shift+";
        if ((m & HotkeyModifier.Win)     != 0) s += "Win+";
        return s;
    }
}

public sealed class ScheduledTask
{
    public string        Cron    { get; set; } = "* * * * *";
    public HotkeyAction  Action  { get; set; } = HotkeyAction.PlayMacro;
    public string?       Target  { get; set; }
    public bool          Enabled { get; set; } = true;
}
