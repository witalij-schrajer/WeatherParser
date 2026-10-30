namespace WeatherParser.Cli.Configuration;

public interface IReferenceStationResolver
{
    string Resolve(string? stationIdArgument, CliConfiguration configuration);
}
