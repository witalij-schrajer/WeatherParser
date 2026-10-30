namespace WeatherParser.Cli.Tests.Lifetime;

using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Lifetime;

/// <summary>
/// A shutdown signal the test raises itself; records the configured grace period and logger.
/// </summary>
internal sealed class TestShutdownSignal : IShutdownSignal
{
    private readonly CancellationTokenSource _source = new();

    public CancellationToken Token => _source.Token;

    public TimeSpan? GracePeriod { get; private set; }

    public ILogger? Logger { get; private set; }

    public void Raise() => _source.Cancel();

    public void Configure(TimeSpan gracePeriod, ILogger logger)
    {
        GracePeriod = gracePeriod;
        Logger = logger;
    }

    public void Dispose() => _source.Dispose();
}
