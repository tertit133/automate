// ScriptRunnerService -- compile + execute a C# script snippet via
// Roslyn's CSharpScript. The script sees the AutomationApi instance as
// its globals, so code can call Mouse.Click(...), Keyboard.Type(...),
// Log("..."), etc. without any `api.` prefix.
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace AutoMate.Services;

public sealed class ScriptRunnerService
{
    private readonly ScriptOptions _opts = ScriptOptions.Default
        .AddImports("System", "System.IO", "System.Linq",
                     "System.Threading", "System.Threading.Tasks",
                     "AutoMate.Services")
        .AddReferences(typeof(AutomationApi).Assembly);

    public async Task<string> RunAsync(string code, Action<string> log,
                                          CancellationToken ct)
    {
        var api = new AutomationApi(log, ct);
        try
        {
            var state = await CSharpScript.RunAsync(code, _opts, api,
                                                      typeof(AutomationApi), ct);
            return state.ReturnValue is null
                     ? "ok"
                     : "ok: " + state.ReturnValue;
        }
        catch (CompilationErrorException ce)
        {
            return "compile error: " + string.Join("\n", ce.Diagnostics);
        }
        catch (OperationCanceledException)
        {
            return "cancelled";
        }
        catch (Exception e)
        {
            return $"runtime error: {e.GetType().Name}: {e.Message}";
        }
    }
}
