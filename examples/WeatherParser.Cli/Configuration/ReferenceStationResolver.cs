namespace WeatherParser.Cli.Configuration;

public sealed class ReferenceStationResolver : IReferenceStationResolver
{
    public string Resolve(string? stationIdArgument, CliConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(stationIdArgument))
        {
            return stationIdArgument;
        }

        if (!string.IsNullOrEmpty(configuration.Dwd?.ReferenceStationId))
        {
            return configuration.Dwd.ReferenceStationId;
        }

        throw new CliException("CLI-DWD-STATION-MISSING", "No DWD station given; use --StationID <id> or the setting 'Dwd.ReferenceStationId'.");
    }
}
