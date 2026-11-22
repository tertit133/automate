// LogViewModel -- append-only scrolling log the UI binds to as text.
using System;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoMate.ViewModels;

public sealed partial class LogViewModel : ObservableObject
{
    private readonly StringBuilder _sb = new();

    [ObservableProperty] private string _text = "";

    public void Append(string line)
    {
        _sb.Append('[')
           .Append(DateTime.Now.ToString("HH:mm:ss"))
           .Append("] ")
           .AppendLine(line);
        // Keep the buffer from growing unbounded.
        if (_sb.Length > 200_000) _sb.Remove(0, 50_000);
        Text = _sb.ToString();
    }
}
