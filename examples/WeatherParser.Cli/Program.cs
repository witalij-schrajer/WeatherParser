using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Parsing.Geography;

namespace WeatherParser.Cli;

/// <summary>
/// Entry point of the example CLI <c>WeatherParser</c> (provisional until the composition root of P3.03 S2).
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs one invocation and returns the process exit code.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    public static int Main(string[] args)
    {
        return Run(args, Console.Out, Console.Error, new HelpWriter(), new CommandLineParser(), new CommandRequestValidator(new CoordinateParser(), new SearchParameterFactory()), new CommandExecutor(new ConfigurationLoader(new EnvironmentReader()), new ConfigurationRegistrar(), new ConfigurationValidator(new ReferenceStationResolver(), new CredentialProvider(new EnvironmentReader(), new FileModeReader()))));
    }

    /// <summary>
    /// Handles every invocation before the container exists: arguments, help and usage errors (CL-03, CL-07); valid commands report 'not implemented' until P3.05 to P3.07.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="standardOutput">The standard output writer.</param>
    /// <param name="standardError">The standard error writer.</param>
    /// <param name="helpWriter">The help writer.</param>
    /// <param name="parser">The command-line parser.</param>
    /// <param name="validator">The command request validator.</param>
    /// <param name="executor">The command executor.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args, TextWriter standardOutput, TextWriter standardError, IHelpWriter helpWriter, ICommandLineParser parser, ICommandRequestValidator validator, ICommandExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        ArgumentNullException.ThrowIfNull(helpWriter);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(executor);

        var result = parser.Parse(args);
        if (result is ParsedCommandLine parsed)
        {
            result = validator.Validate(parsed);
        }

        switch (result)
        {
            case HelpRequest { Command: null }:
                helpWriter.WriteHelp(standardOutput);
                return 0;
            case HelpRequest help:
                helpWriter.WriteCommandHelp(standardOutput, help.Command.Value);
                return 0;
            case UsageError error:
                standardError.WriteLine($"{error.ErrorId}: {error.Message}");
                helpWriter.WriteHelp(standardError);
                return 1;
            default:
                return executor.Execute(((ValidatedCommand)result).Request, standardOutput, standardError);
        }
    }
}
