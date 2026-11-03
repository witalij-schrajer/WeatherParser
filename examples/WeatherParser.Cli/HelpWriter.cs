namespace WeatherParser.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using WeatherParser.Cli.Arguments;

/// <summary>
/// Writes the help texts of the example CLI: commands, options with units, global options and examples (REQ-010, CL-03).
/// </summary>
public sealed class HelpWriter : IHelpWriter
{
    private static readonly string[] SearchOptionLines = {
        "--Latitude <value>          Latitude: decimal degrees or degrees, minutes and seconds with hemisphere (required)",
        "--Longitude <value>         Longitude: decimal degrees or degrees, minutes and seconds with hemisphere (required)",
        "--Altitude <metres>         Altitude of the search point in metres (required)",
        "--MaxDistance <kilometres>  Maximum distance in kilometres (required)",
        "--MaxAltDifference <metres> Maximum altitude difference in metres (required)"
    };

    private static readonly string[] GlobalOptionLines = {
        "--Output mqtt|json|xml       Output mode (default mqtt)",
        "--OutputFile <path>         Write the JSON or XML document to a new file (json and xml only)",
        "--Config <path>             Configuration file (otherwise WEATHERPARSER_CONFIG)",
        "--Verbose                   Also log debug messages",
        "--Help                      Show this help, alone or with one command"
    };

    private sealed record CommandHelp(string Option, string Purpose, string[] Options, string Example);

    private static readonly IReadOnlyDictionary<CliCommand, CommandHelp> Commands = new Dictionary<CliCommand, CommandHelp>
    {
        [CliCommand.SelectDwd] = new(
            "--SelectDWD",
            "Find suitable DWD stations.",
            SearchOptionLines,
            "WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200"),
        [CliCommand.ParseDwd] = new(
            "--ParseDWD",
            "Retrieve observations of one DWD station.",
            new[] { "--StationID <id>           DWD station ID, five digits with leading zeros (optional; otherwise the configured reference station)" },
            "WeatherParser --ParseDWD --StationID <DWD-STATION-ID>"),
        [CliCommand.ParseOstHessen] = new(
            "--ParseOstHessen",
            "Retrieve observations of matching OsthessenWetter stations.",
            new[] { "--StationNamePattern <p>   Station name pattern with * and ? (required)" },
            "WeatherParser --ParseOstHessen --StationNamePattern \"Fulda*\" --Output json"),
        [CliCommand.SelectMosmix] = new(
            "--SelectMOSMIX",
            "Find suitable MOSMIX forecast locations.",
            [.. SearchOptionLines, "--RefreshMosmixSEvidence     Refresh the stored MOSMIX_S evidence index before the selection (optional)"],
            "WeatherParser --SelectMOSMIX --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200"),
        [CliCommand.ParseMosmix] = new(
            "--ParseMOSMIX",
            "Retrieve the forecast of one MOSMIX location.",
            new[] {
                "--Product MOSMIX_S|MOSMIX_L    MOSMIX product (required)",
                "--MOSMIXID <id>             MOSMIX location ID (required)"
            },
            "WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID>")
    };

    /// <summary>
    /// Writes the complete help text to the writer.
    /// </summary>
    /// <param name="writer">The destination.</param>
    public void WriteHelp(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        
        writer.WriteLine("Usage: WeatherParser --<command> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands (exactly one per invocation):");
        foreach (var help in Commands.Values)
        {
            writer.WriteLine($"  {help.Option,-18}{help.Purpose}");
        }
        writer.WriteLine();
        writer.WriteLine("Command options:");
        foreach (var (command, help) in Commands)
        {
            writer.WriteLine($"  {help.Option}:");
            foreach (var option in help.Options)
            {
                writer.WriteLine($"    {option}");
            }
        }
        writer.WriteLine();
        writer.WriteLine("Global options:");
        foreach (var option in GlobalOptionLines)
        {
            writer.WriteLine($"  {option}");
        }
        writer.WriteLine();
        writer.WriteLine("Examples:");
        foreach (var help in Commands.Values)
        {
            writer.WriteLine($"  {help.Example}");
        }
        writer.WriteLine();
        writer.WriteLine("Use --Help with a command for that command's options.");
    }

    /// <inheritdoc />
    public void WriteCommandHelp(TextWriter writer, CliCommand command)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var help = Commands[command];
        writer.WriteLine($"Usage: WeatherParser {help.Option} [options]");
        writer.WriteLine();
        writer.WriteLine(help.Purpose);
        writer.WriteLine();
        writer.WriteLine("Options:");
        foreach (var option in help.Options)
        {
            writer.WriteLine($"  {option}");
        }
        foreach (var option in GlobalOptionLines)
        {
            writer.WriteLine($"  {option}");
        }
        writer.WriteLine();
        writer.WriteLine($"Example: {help.Example}");
    }
}
