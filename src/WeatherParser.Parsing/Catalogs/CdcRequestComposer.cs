using System;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Default implementation of <see cref="ICdcRequestComposer"/>; stateless.
/// </summary>
public sealed class CdcRequestComposer : ICdcRequestComposer
{
    /// <inheritdoc/>
    public IReadOnlyList<CdcListRequestTarget> StationLists(CdcCatalogEndpoints endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return [
            new("tu", "AirTemperatureStationList", endpoints.AirTemperatureStationList!),
            new("ff", "WindStationList", endpoints.WindStationList!),
            new("rr", "PrecipitationStationList", endpoints.PrecipitationStationList!),
            new("sd", "SolarStationList", endpoints.SolarStationList!),
        ];
    }

    /// <inheritdoc/>
    public CdcArchiveRequestTarget Archive(CdcCatalogEndpoints endpoints, string stationId, string productCode)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(stationId);
        ArgumentNullException.ThrowIfNull(productCode);

        var (name, directory, prefix) = productCode switch
        {
            "tu" => ("AirTemperatureNowDirectory", endpoints.AirTemperatureNowDirectory!, "10minutenwerte_TU_"),
            "ff" => ("WindNowDirectory", endpoints.WindNowDirectory!, "10minutenwerte_wind_"),
            "rr" => ("PrecipitationNowDirectory", endpoints.PrecipitationNowDirectory!, "10minutenwerte_nieder_"),
            "sd" => ("SolarNowDirectory", endpoints.SolarNowDirectory!, "10minutenwerte_SOLAR_"),
            _ => throw new ArgumentException($"Unknown product code '{productCode}'; expected tu, ff, rr or sd.", nameof(productCode)),
        };
        return new(name, new Uri(directory, prefix + Uri.EscapeDataString(stationId) + "_now.zip"));
    }
}
