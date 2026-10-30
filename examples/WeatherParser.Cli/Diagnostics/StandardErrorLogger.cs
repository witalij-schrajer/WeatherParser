using Microsoft.Extensions.Logging;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Writes log entries of one category as single lines to standard error (CL-08); never to standard output (REQ-073).
/// </summary>
public sealed class StandardErrorLogger : ILogger
{
    private readonly TextWriter _standardError;
    private readonly TimeProvider _timeProvider;
    private readonly ILogLineFormatter _formatter;
    private readonly LogLevel _minimumLevel;
    private readonly string _command;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardErrorLogger"/> class.
    /// </summary>
    /// <param name="standardError">The text writer to write log entries to.</param>
    /// <param name="timeProvider">The time provider used to get the current UTC time.</param>
    /// <param name="formatter">The formatter used to format log entries.</param>
    /// <param name="minimumLevel">The minimum log level to include in output.</param>
    /// <param name="command">The command name associated with the logger.</param>
    public StandardErrorLogger(TextWriter standardError, TimeProvider timeProvider, ILogLineFormatter formatter, LogLevel minimumLevel, string command)
    {
        _standardError = standardError;
        _timeProvider = timeProvider;
        _formatter = formatter;
        _minimumLevel = minimumLevel;
        _command = command;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _minimumLevel;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var values = (state as IEnumerable<KeyValuePair<string, object?>> ?? Array.Empty<KeyValuePair<string, object?>>()).ToList();
        string? Value(string key) => values.FirstOrDefault(pair => pair.Key == key).Value?.ToString();
        var providerOrSource = Value("Source");

        // A complete C-50 source ("provider:station") wins; a provider-only value yields to the known station identifiers.
        var source = providerOrSource is { } complete && complete.Contains(':')
            ? complete
            : new[] { Value("StationId"), Value("MosmixId"), Value("StationName"), providerOrSource }.FirstOrDefault(value => !string.IsNullOrEmpty(value));

        var line = _formatter.Format(_timeProvider.GetUtcNow(), logLevel, eventId.Name, _command, source, formatter(state, exception), exception);
        lock (_standardError)
        {
            _standardError.WriteLine(line);
        }
    }
}
