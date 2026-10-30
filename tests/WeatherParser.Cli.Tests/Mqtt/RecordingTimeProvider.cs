using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Cli.Tests.Mqtt;

/// <summary>
/// Records the due time of every finite timer and fires it at once on the thread pool, so timeouts expire and delays end immediately without real sleeping.
/// </summary>
internal sealed class RecordingTimeProvider : TimeProvider
{
    private readonly ConcurrentQueue<TimeSpan> _dueTimes = new();

    public IReadOnlyList<TimeSpan> DueTimes => _dueTimes.ToArray();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        if (dueTime != Timeout.InfiniteTimeSpan)
        {
            _dueTimes.Enqueue(dueTime);
            ThreadPool.QueueUserWorkItem(_ => callback(state));
        }

        return new InertTimer();
    }

    private sealed class InertTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
