using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Parses coordinate strings into decimal degrees (SC-07).
/// </summary>
public interface ICoordinateParser
{
    /// <summary>
    /// Parses the specified latitude string.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The latitude in decimal degrees.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    /// <exception cref="CoordinateFormatException">Thrown when the format is invalid or the value is out of range (contract SC-07, SC-17).</exception>
    decimal ParseLatitude(string text);

    /// <summary>
    /// Parses the specified longitude string.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The longitude in decimal degrees.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    /// <exception cref="CoordinateFormatException">Thrown when the format is invalid or the value is out of range (contract SC-07, SC-17).</exception>
    decimal ParseLongitude(string text);
}
