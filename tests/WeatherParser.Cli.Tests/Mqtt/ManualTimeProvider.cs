using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Cli.Tests.Mqtt;

internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<ManualTimer> _timers = new();
    private readonly DateTimeOffset _start = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private TimeSpan _elapsed;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _start + _elapsed;
        }
    }

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        lock (_gate)
        {
            return _elapsed.Ticks;
        }
    }

    public int ActiveTimers
    {
        get
        {
            lock (_gate)
            {
                return _timers.Count(t => t.Due is not null);
            }
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    // Moves the clock and fires every timer that became due, outside the lock.
    public void Advance(TimeSpan delta)
    {
        List<ManualTimer> due;
        lock (_gate)
        {
            _elapsed += delta;
            due = _timers.Where(t => t.Due is { } d && d <= _elapsed).ToList();
            foreach (var timer in due)
            {
                timer.Due = null;
            }
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    // Waits until the code under test has created at least the given number of pending timers.
    public async Task WaitForTimersAsync(int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (ActiveTimers < count)
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Expected {count} pending timer(s), found {ActiveTimers}.");
            }

            await Task.Delay(5);
        }
    }

    private sealed class ManualTimer : ITimer
    {
        private readonly ManualTimeProvider _owner;
        private readonly TimerCallback _callback;
        private readonly object? _state;

        public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
        {
            _owner = owner;
            _callback = callback;
            _state = state;
        }

        public TimeSpan? Due { get; set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (_owner._gate)
            {
                if (!_owner._timers.Contains(this))
                {
                    _owner._timers.Add(this);
                }

                Due = dueTime == Timeout.InfiniteTimeSpan ? null : _owner._elapsed + dueTime;
            }

            return true;
        }

        public void Fire() => _callback(_state);

        public void Dispose()
        {
            lock (_owner._gate)
            {
                Due = null;
                _owner._timers.Remove(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
