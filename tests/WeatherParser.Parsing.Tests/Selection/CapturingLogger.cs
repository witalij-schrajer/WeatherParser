using Microsoft.Extensions.Logging;

namespace WeatherParser.Parsing.Tests.Selection;

/// <summary>
/// Records every log call (level, event name and formatted message) so tests can count notices.
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string? EventName, string Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (Entries)
        {
            Entries.Add((logLevel, eventId.Name, formatter(state, exception)));
        }
    }

    public int Count(string eventName) => Entries.Count(entry => entry.EventName == eventName);
}
