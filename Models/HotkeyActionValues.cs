// A tiny helper so the XAML combos can reference the enum values from
// the DataGrid / ComboBox markup without x:Array ceremony.
using System;

namespace AutoMate.Models;

public static class HotkeyActionValues
{
    public static readonly Array All = Enum.GetValues(typeof(HotkeyAction));
}
