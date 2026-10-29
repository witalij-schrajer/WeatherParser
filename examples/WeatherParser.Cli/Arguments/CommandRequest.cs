namespace WeatherParser.Cli.Arguments;

using WeatherParser.Parsing.Export;

public enum OutputFormat { Mqtt, Json, Xml }

public sealed record CommonOptions(OutputFormat Output, string? OutputFile, string? ConfigPath, bool Verbose);

public abstract record CommandRequest(CommonOptions Common);

public sealed record SelectDwdRequest(CommonOptions Common, SearchParameters Search) : CommandRequest(Common);

public sealed record SelectMosmixRequest(CommonOptions Common, SearchParameters Search, bool RefreshMosmixSEvidence) : CommandRequest(Common);

public sealed record ParseDwdRequest(CommonOptions Common, string? StationId) : CommandRequest(Common);

public sealed record ParseOstHessenRequest(CommonOptions Common, string StationNamePattern) : CommandRequest(Common);

public sealed record ParseMosmixRequest(CommonOptions Common, MosmixProduct Product, string MosmixId) : CommandRequest(Common);

public sealed record ValidatedCommand(CommandRequest Request) : CommandLineResult;
