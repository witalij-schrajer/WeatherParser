namespace WeatherParser.Cli.Configuration;

public interface IFileModeReader
{
    bool HasGroupOrOtherAccess(string path);
}
