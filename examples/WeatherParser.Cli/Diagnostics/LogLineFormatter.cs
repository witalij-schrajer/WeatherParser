using System.Globalization;
using Microsoft.Extensions.Logging;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Formats one log entry as one line: UTC time, severity, ID, command, optional source and message (CL-08, REQ-072).
/// </summary>
public sealed class LogLineFormatter : ILogLineFormatter
{
    /// <inheritdoc />
    public string Format(DateTimeOffset timestamp, LogLevel level, string? eventName, string command, string? source, string message, Exception? exception)
    {
        var text = exception switch
        {
            null => message,
            AggregateException aggregate => $"{message} {nameof(AggregateException)}: {aggregate.InnerExceptions.Count} inner exception(s)",
            _ => $"{message} {exception.GetType().Name}: {exception.Message}",
        };
        var sourcePart = string.IsNullOrEmpty(source) ? string.Empty : $" source={source}";
        var id = string.IsNullOrEmpty(eventName) ? "-" : eventName;
        var time = timestamp.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        return $"{time} UTC {level.ToString().ToUpperInvariant()} {id} command={command}{sourcePart} {SingleLine(text)}";
    }

    private static string SingleLine(string text) => text.Replace('\r', ' ').Replace('\n', ' ');
}
