// MacroStorageService -- read / write Macro + settings under
// %APPDATA%\AutoMate\. JSON is human-readable and easy to diff.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AutoMate.Models;

namespace AutoMate.Services;

public sealed class MacroStorageService
{
    public string RootDir { get; }
    public string MacrosDir  => Path.Combine(RootDir, "macros");
    public string ScriptsDir => Path.Combine(RootDir, "scripts");
    public string ConfigFile => Path.Combine(RootDir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
    };

    public MacroStorageService()
    {
        RootDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoMate");
        Directory.CreateDirectory(MacrosDir);
        Directory.CreateDirectory(ScriptsDir);
    }

    // ---- macros ----------------------------------------------------------
    public void SaveMacro(Macro m)
    {
        var path = Path.Combine(MacrosDir, Safe(m.Name) + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(m, JsonOpts));
    }

    public Macro? LoadMacro(string name)
    {
        var path = Path.Combine(MacrosDir, Safe(name) + ".json");
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<Macro>(File.ReadAllText(path), JsonOpts); }
        catch { return null; }
    }

    public IReadOnlyList<string> ListMacros()
    {
        var list = new List<string>();
        foreach (var f in Directory.EnumerateFiles(MacrosDir, "*.json"))
            list.Add(Path.GetFileNameWithoutExtension(f));
        return list;
    }

    public void DeleteMacro(string name)
    {
        var path = Path.Combine(MacrosDir, Safe(name) + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    // ---- scripts (free-form .csx) ----------------------------------------
    public void SaveScript(string name, string source)
        => File.WriteAllText(Path.Combine(ScriptsDir, Safe(name) + ".csx"), source);

    public string? LoadScript(string name)
    {
        var path = Path.Combine(ScriptsDir, Safe(name) + ".csx");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public IReadOnlyList<string> ListScripts()
    {
        var list = new List<string>();
        foreach (var f in Directory.EnumerateFiles(ScriptsDir, "*.csx"))
            list.Add(Path.GetFileNameWithoutExtension(f));
        return list;
    }

    // ---- config (hotkeys + schedule) -------------------------------------
    public sealed class Config
    {
        public List<HotkeyBinding> Hotkeys   { get; set; } = new();
        public List<ScheduledTask> Schedule  { get; set; } = new();
    }

    public Config LoadConfig()
    {
        if (!File.Exists(ConfigFile)) return new Config();
        try { return JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFile), JsonOpts) ?? new Config(); }
        catch { return new Config(); }
    }

    public void SaveConfig(Config c)
        => File.WriteAllText(ConfigFile, JsonSerializer.Serialize(c, JsonOpts));

    // ---- helpers ---------------------------------------------------------
    private static string Safe(string name)
    {
        var bad = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (var c in name) sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
        return sb.ToString();
    }
}
