// CronParser -- plain 5-field cron ("minute hour dom month dow"), with
// *, step */n, lists a,b,c and ranges a-b supported for each field.
// Keep it dependency-free -- an entire NuGet package for this would be
// overkill.
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoMate.Services;

public sealed class CronExpression
{
    public ISet<int> Minute     { get; init; } = new HashSet<int>();
    public ISet<int> Hour       { get; init; } = new HashSet<int>();
    public ISet<int> DayOfMonth { get; init; } = new HashSet<int>();
    public ISet<int> Month      { get; init; } = new HashSet<int>();
    public ISet<int> DayOfWeek  { get; init; } = new HashSet<int>();

    public bool Matches(DateTime t)
        => Minute.Contains(t.Minute)
        && Hour.Contains(t.Hour)
        && DayOfMonth.Contains(t.Day)
        && Month.Contains(t.Month)
        && DayOfWeek.Contains((int)t.DayOfWeek);
}

public static class CronParser
{
    public static CronExpression? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5) return null;

        try
        {
            return new CronExpression
            {
                Minute     = ParseField(parts[0],  0, 59),
                Hour       = ParseField(parts[1],  0, 23),
                DayOfMonth = ParseField(parts[2],  1, 31),
                Month      = ParseField(parts[3],  1, 12),
                DayOfWeek  = ParseField(parts[4],  0,  6),
            };
        }
        catch { return null; }
    }

    private static ISet<int> ParseField(string spec, int min, int max)
    {
        var set = new HashSet<int>();
        foreach (var token in spec.Split(','))
        {
            int step = 1;
            string body = token;
            var stepIdx = token.IndexOf('/');
            if (stepIdx >= 0)
            {
                step = int.Parse(token[(stepIdx + 1)..]);
                body = token[..stepIdx];
            }
            int lo, hi;
            if (body == "*") { lo = min; hi = max; }
            else if (body.Contains('-'))
            {
                var r = body.Split('-');
                lo = int.Parse(r[0]);
                hi = int.Parse(r[1]);
            }
            else
            {
                lo = hi = int.Parse(body);
            }
            for (int i = lo; i <= hi; i += step)
            {
                if (i < min || i > max) continue;
                set.Add(i);
            }
        }
        return set;
    }
}
