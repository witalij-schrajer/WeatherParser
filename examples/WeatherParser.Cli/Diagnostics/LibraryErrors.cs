using System;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Provides a library errors mapping.
/// </summary>
public class LibraryErrors : ILibraryErrors
{
    /// <inheritdoc/>
    public bool TryGetErrorId(Exception exception, out string errorId)
    {
        errorId = exception switch
        {
            CatalogFormatException e => e.ErrorId,
            CatalogRetrievalException e => e.ErrorId,
            CoordinateFormatException e => e.ErrorId,
            DwdStationException e => e.ErrorId,
            EvidenceCacheException e => e.ErrorId,
            ObservationLimitException e => e.ErrorId,
            OsthessenWetterException e => e.ErrorId,
            SearchParameterException e => e.ErrorId,
            SelectionLimitException e => e.ErrorId,
            MosmixForecastException e => e.ErrorId,
            MosmixForecastFormatException e => e.ErrorId,
            MqttMessageException e => e.ErrorId,
            ParserEndpointValidationException e => e.ErrorId,
            _ => string.Empty,
        };

        return errorId.Length > 0;
    }
}
