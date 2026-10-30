namespace WeatherParser.Cli.Arguments;

using WeatherParser.Parsing.Export;

/// <summary>
/// Specifies the output format for weather data.
/// </summary>
/// <remarks>
/// CL-05: Configuration section for output format.
/// </remarks>
public enum OutputFormat 
{ 
    /// <summary>
    /// MQTT output format.
    /// </summary>
    Mqtt, 
    
    /// <summary>
    /// JSON output format.
    /// </summary>
    Json, 
    
    /// <summary>
    /// XML output format.
    /// </summary>
    Xml 
}

/// <summary>
/// Represents common options shared across all command requests.
/// </summary>
/// <param name="Output">The output format for weather data.</param>
/// <param name="OutputFile">The path to the output file, or null if not specified.</param>
/// <param name="ConfigPath">The path to the configuration file, or null if not specified.</param>
/// <param name="Verbose">Indicates whether verbose logging is enabled.</param>
public sealed record CommonOptions(OutputFormat Output, string? OutputFile, string? ConfigPath, bool Verbose);

/// <summary>
/// Base class for all command requests.
/// </summary>
/// <param name="Common">The common options shared across all commands.</param>
public abstract record CommandRequest(CommonOptions Common);

/// <summary>
/// Represents a request to select DWD data.
/// </summary>
/// <param name="Common">The common options shared across all commands.</param>
/// <param name="Search">The search parameters for selecting DWD data.</param>
public sealed record SelectDwdRequest(CommonOptions Common, SearchParameters Search) : CommandRequest(Common);

/// <summary>
/// Represents a request to select MOSMIX data.
/// </summary>
/// <param name="Common">The common options shared across all commands.</param>
/// <param name="Search">The search parameters for selecting MOSMIX data.</param>
/// <param name="RefreshMosmixSEvidence">Indicates whether to refresh MOSMIX S evidence.</param>
public sealed record SelectMosmixRequest(CommonOptions Common, SearchParameters Search, bool RefreshMosmixSEvidence) : CommandRequest(Common);

/// <summary>
/// Represents a request to parse DWD data.
/// </summary>
/// <param name="Common">The common options shared across all commands.</param>
/// <param name="StationId">The ID of the weather station to parse.</param>
public sealed record ParseDwdRequest(CommonOptions Common, string? StationId) : CommandRequest(Common);

/// <summary>
/// Represents a request to parse OST Hessen data.
/// </summary>
/// <param name="Common">The common options shared across all commands.</param>
/// <param name="StationNamePattern">The pattern to match station names.</param>
public sealed record ParseOstHessenRequest(CommonOptions Common, string StationNamePattern) : CommandRequest(Common);

/// <summary>
/// Represents a request to parse MOSMIX data.
/// </summary>
/// <param name="Common">The common options shared across all commands.</param>
/// <param name="Product">The MOSMIX product to parse.</param>
/// <param name="MosmixId">The ID of the MOSMIX data to parse.</param>
public sealed record ParseMosmixRequest(CommonOptions Common, MosmixProduct Product, string MosmixId) : CommandRequest(Common);

/// <summary>
/// Represents a validated command request.
/// </summary>
/// <param name="Request">The validated command request.</param>
public sealed record ValidatedCommand(CommandRequest Request) : CommandLineResult;
