using Microsoft.Extensions.Logging;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Creates the loggers of the example CLI, which write single lines to standard error (CL-08).
/// </summary>
public sealed class StandardErrorLoggerProvider : ILoggerProvider
{
    private readonly TextWriter _standardError;
    private readonly TimeProvider _timeProvider;
    private readonly ILogLineFormatter _formatter;
    private readonly LogLevel _minimumLevel;
    private readonly string _command;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardErrorLoggerProvider"/> class.
    /// </summary>
    /// <param name="standardError">The text writer to write log entries to.</param>
    /// <param name="timeProvider">The time provider used to get the current UTC time.</param>
    /// <param name="formatter">The formatter used to format log entries.</param>
    /// <param name="minimumLevel">The minimum log level to include in output.</param>
    /// <param name="command">The command name associated with the logger.</param>
    public StandardErrorLoggerProvider(TextWriter standardError, TimeProvider timeProvider, ILogLineFormatter formatter, LogLevel minimumLevel, string command)
    {
        _standardError = standardError;
        _timeProvider = timeProvider;
        _formatter = formatter;
        _minimumLevel = minimumLevel;
        _command = command;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new StandardErrorLogger(_standardError, _timeProvider, _formatter, _minimumLevel, _command);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
