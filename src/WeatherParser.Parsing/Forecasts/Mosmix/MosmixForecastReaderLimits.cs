namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// The bounds of contracts MF-05 and MF-07.
/// </summary>
internal sealed record MosmixForecastReaderLimits(
    long SingleCompressedBytes,
    long SingleUnpackedBytes,
    long AllStationsCompressedBytes,
    long AllStationsUnpackedBytes,
    int MaxTimeSteps,
    int MaxForecastsPerPlacemark,
    int MaxPlacemarks)
{
    /// <summary>
    /// Gets the default limits defined by the contracts.
    /// </summary>
    internal static MosmixForecastReaderLimits Contract => new(1048576, 16777216, 67108864, 2147483648L, 1000, 200, 20000);
}
