using System.Globalization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a timestamp in suffix-free UTC wire format (yyyy-MM-dd'T'HH:mm:ss).
/// </summary>
internal readonly struct ExportTimestamp
{
    /// <summary>
    /// Gets the wire format string used for serialization.
    /// </summary>
    public const string WireFormat = "yyyy-MM-dd'T'HH:mm:ss";

    private readonly DateTime _utc;
    private readonly bool _isSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportTimestamp"/> struct.
    /// </summary>
    /// <param name="utc">The UTC date and time.</param>
    private ExportTimestamp(DateTime utc) => (_utc, _isSet) = (utc, true);

    /// <summary>
    /// Gets the UTC date and time value.
    /// </summary>
    public DateTime Utc => _utc;

    /// <summary>
    /// Creates an <see cref="ExportTimestamp"/> from a UTC <see cref="DateTime"/>.
    /// </summary>
    /// <param name="value">The UTC date and time.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <returns>An <see cref="ExportTimestamp"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not a UTC kind or contains fractional seconds.
    /// </exception>
    public static ExportTimestamp FromUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The timestamp must be a UTC value (DateTimeKind.Utc).", parameterName);
        if (value.Ticks % TimeSpan.TicksPerSecond != 0)
            throw new ArgumentException("The timestamp must not contain fractional seconds.", parameterName);
        return new ExportTimestamp(value);
    }

    /// <summary>
    /// Parses a wire-formatted string into an <see cref="ExportTimestamp"/>.
    /// </summary>
    /// <param name="value">The wire-formatted timestamp string.</param>
    /// <param name="parameterName">The name of the parameter being validated.</param>
    /// <returns>An <see cref="ExportTimestamp"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not in the expected wire format.
    /// </exception>
    public static ExportTimestamp ParseWire(string? value, string parameterName)
    {
        if (value is null || !DateTime.TryParseExact(value, WireFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            throw new ArgumentException($"The timestamp '{value}' is not in the form YYYY-MM-DDTHH:mm:ss.", parameterName);
        return new ExportTimestamp(parsed);
    }

    /// <summary>
    /// Converts the timestamp to its wire-formatted string representation.
    /// </summary>
    /// <param name="fieldName">The name of the field for error reporting.</param>
    /// <returns>The wire-formatted timestamp string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the timestamp is not set.
    /// </exception>
    public string ToWire(string fieldName)
    {
        if (!_isSet)
            throw new InvalidOperationException($"The required timestamp '{fieldName}' is not set.");
        return _utc.ToString(WireFormat, CultureInfo.InvariantCulture);
    }
}
