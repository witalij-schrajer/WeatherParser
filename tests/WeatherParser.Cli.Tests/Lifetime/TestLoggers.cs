using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace WeatherParser.Cli.Tests.Lifetime;

internal sealed class BlockingLogger : ILogger
{
    public ManualResetEventSlim Entered { get; } = new();
    public ManualResetEventSlim Release { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entered.Set();
        Release.Wait();
    }
}

internal sealed class ThrowingLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        throw new ObjectDisposedException("standard error");
    }
}
