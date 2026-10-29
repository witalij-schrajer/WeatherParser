namespace WeatherParser.Cli.Arguments;

using System.Globalization;
using System.Text.RegularExpressions;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;

/// <summary>
/// Validates command line arguments using library services.
/// </summary>
public sealed class CommandRequestValidator : ICommandRequestValidator
{
    private readonly ICoordinateParser _coordinateParser;
    private readonly ISearchParameterFactory _searchParameterFactory;
    private static readonly Regex DecimalNumber = new(@"\A[+-]?[0-9]{1,15}(\.[0-9]{1,20})?\z", RegexOptions.CultureInvariant);

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRequestValidator"/> class.
    /// </summary>
    /// <param name="coordinateParser">The coordinate parser to use.</param>
    /// <param name="searchParameterFactory">The search parameter factory to use.</param>
    public CommandRequestValidator(ICoordinateParser coordinateParser, ISearchParameterFactory searchParameterFactory)
    {
        ArgumentNullException.ThrowIfNull(coordinateParser);
        ArgumentNullException.ThrowIfNull(searchParameterFactory);
        _coordinateParser = coordinateParser;
        _searchParameterFactory = searchParameterFactory;
    }

    /// <summary>
    /// Validates the option values of a parsed command line through the library and returns a <see cref="ValidatedCommand"/> or a <see cref="UsageError"/> (CL-03).
    /// </summary>
    /// <param name="parsed">The parsed command line to validate.</param>
    /// <returns>A <see cref="ValidatedCommand"/> if validation succeeded, otherwise a <see cref="UsageError"/>.</returns>
    public CommandLineResult Validate(ParsedCommandLine parsed)
    {
        ArgumentNullException.ThrowIfNull(parsed);

        var values = parsed.Values;
        var common = new CommonOptions(
            ParseOutput(values.GetValueOrDefault("Output")),
            values.GetValueOrDefault("OutputFile"),
            values.GetValueOrDefault("Config"),
            parsed.Flags.Contains("Verbose"));

        try
        {
            CommandRequest request = parsed.Command switch
            {
                CliCommand.SelectDwd => new SelectDwdRequest(common, CreateSearch(values)),
                CliCommand.SelectMosmix => new SelectMosmixRequest(common, CreateSearch(values), parsed.Flags.Contains("RefreshMosmixSEvidence")),
                CliCommand.ParseDwd => new ParseDwdRequest(common, values.GetValueOrDefault("StationID")),
                CliCommand.ParseOstHessen => new ParseOstHessenRequest(common, values["StationNamePattern"]),
                _ => new ParseMosmixRequest(common, string.Equals(values["Product"], "MOSMIX_S", StringComparison.OrdinalIgnoreCase) ? MosmixProduct.MosmixS : MosmixProduct.MosmixL, values["MOSMIXID"]),
            };

            return new ValidatedCommand(request);
        }
        catch (CoordinateFormatException exception)
        {
            return new UsageError(exception.ErrorId, exception.Message);
        }
        catch (SearchParameterException exception)
        {
            return new UsageError(exception.ErrorId, exception.Message);
        }
        catch (ArgumentValueException exception)
        {
            return new UsageError("CLI-ARGUMENT-INVALID", exception.Message);
        }
    }

    private static OutputFormat ParseOutput(string? value)
    {
        return value is null ? OutputFormat.Mqtt : Enum.Parse<OutputFormat>(value, ignoreCase: true);
    }

    private SearchParameters CreateSearch(IReadOnlyDictionary<string, string> values)
    {
        return _searchParameterFactory.Create(
            _coordinateParser.ParseLatitude(values["Latitude"]),
            _coordinateParser.ParseLongitude(values["Longitude"]),
            ParseDecimal(values, "Altitude"),
            ParseDecimal(values, "MaxDistance"),
            ParseDecimal(values, "MaxAltDifference"));
    }

    private static decimal ParseDecimal(IReadOnlyDictionary<string, string> values, string name)
    {
        var text = values[name];
        if (!DecimalNumber.IsMatch(text))
        {
            throw new ArgumentValueException($"Option '--{name}' requires a decimal number such as 300 or -12.5.");
        }

        return decimal.Parse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    }

    private sealed class ArgumentValueException(string message) : Exception(message);
}
