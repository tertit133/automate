// SchedulerService -- runs enabled ScheduledTask entries on a ticking
// timer. Resolution is one minute (cron's smallest unit); we tick every
// 20 seconds so a late system clock doesn't miss a slot entirely.
using System;
using System.Collections.Generic;
using System.Threading;
using AutoMate.Models;

namespace AutoMate.Services;

public sealed class SchedulerService : IDisposable
{
    private readonly object       _lock = new();
    private readonly List<(ScheduledTask Task, CronExpression Cron)> _entries = new();
    private readonly Dictionary<ScheduledTask, DateTime> _lastFired = new();

    private Timer? _timer;
    public Action<ScheduledTask>? OnFire { get; set; }

    public void SetTasks(IEnumerable<ScheduledTask> tasks)
    {
        lock (_lock)
        {
            _entries.Clear();
            _lastFired.Clear();
            foreach (var t in tasks)
            {
                if (!t.Enabled) continue;
                var cron = CronParser.Parse(t.Cron);
                if (cron is null) continue;
                _entries.Add((t, cron));
            }
        }
    }

    public void Start()
    {
        Stop();
        _timer = new Timer(_ => Tick(), null, TimeSpan.Zero, TimeSpan.FromSeconds(20));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose() => Stop();

    private void Tick()
    {
        DateTime now = DateTime.Now;
        DateTime minute = new(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        List<(ScheduledTask, CronExpression)> snap;
        lock (_lock) snap = new(_entries);

        foreach (var (task, cron) in snap)
        {
            if (!cron.Matches(minute)) continue;
            lock (_lock)
            {
                if (_lastFired.TryGetValue(task, out var last) && last == minute) continue;
                _lastFired[task] = minute;
            }
            try { OnFire?.Invoke(task); } catch { /* ignore handler errors */ }
        }
    }
}
