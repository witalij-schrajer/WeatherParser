using Microsoft.Extensions.Logging;

namespace WeatherParser.Cli.Diagnostics;

public interface ILogLineFormatter
{
    string Format(DateTimeOffset timestamp, LogLevel level, string? eventName, string command, string? source, string message, Exception? exception);
}
