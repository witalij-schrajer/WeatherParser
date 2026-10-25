namespace WeatherParser.Parsing.Tests.Catalogs;

internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> _timers = new();
    private DateTimeOffset _now;

    public ManualTimeProvider(DateTimeOffset start)
    {
        _now = start;
    }

    public int GetUtcNowCalls { get; private set; }
    public int CreatedTimers { get; private set; }

    public override DateTimeOffset GetUtcNow()
    {
        ++GetUtcNowCalls;
        return _now;
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_timers)
        {
            _timers.Add(timer);
            ++CreatedTimers;
        }
        timer.Change(dueTime, period);
        return timer;
    }

    public int ActiveTimers
    {
        get
        {
            lock (_timers)
            {
                return _timers.Count(t => !t.Disposed);
            }
        }
    }

    public void Advance(TimeSpan by)
    {
        _now += by;
        List<ManualTimer> due;
        lock (_timers)
        {
            due = _timers.Where(t => t.DueAt is { } at && at <= _now).ToList();
        }
        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        public DateTimeOffset? DueAt { get; private set; }
        public bool Disposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner._now + dueTime;
            return true;
        }

        public void Fire()
        {
            DueAt = null;
            callback(state);
        }

        public void Dispose()
        {
            DueAt = null;
            Disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
