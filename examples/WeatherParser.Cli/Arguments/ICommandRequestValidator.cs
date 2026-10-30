namespace WeatherParser.Cli.Arguments;

/// <summary>
/// Validates the option values of a parsed command line through the library and returns a <see cref="ValidatedCommand"/> or a <see cref="UsageError"/> (CL-03).
/// </summary>
public interface ICommandRequestValidator
{
    /// <summary>
    /// Validates the option values of a parsed command line through the library and returns a <see cref="ValidatedCommand"/> or a <see cref="UsageError"/> (CL-03).
    /// </summary>
    /// <param name="parsed">The parsed command line to validate.</param>
    /// <returns>A <see cref="ValidatedCommand"/> if validation succeeded, otherwise a <see cref="UsageError"/>.</returns>
    CommandLineResult Validate(ParsedCommandLine parsed);
}
