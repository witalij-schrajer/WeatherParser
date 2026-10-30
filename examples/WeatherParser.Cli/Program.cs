using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Parsing.Geography;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Commands;
using System.Text;
using WeatherParser.Cli.Lifetime;

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
        return Main(args, Console.OpenStandardOutput(), Console.OpenStandardError());
    }

    /// <summary>
    /// Runs one invocation with the given standard output and standard error streams, written as UTF-8 without byte-order mark (CL-12, REQ-068).
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="standardOutputStream">The standard output stream.</param>
    /// <param name="standardErrorStream">The standard error stream.</param>
    /// <returns>The process exit code.</returns>
    public static int Main(string[] args, Stream standardOutputStream, Stream standardErrorStream)
    {
        // Registered before anything else runs, so that a signal at any time cancels the command instead of ending the process (CL-18).
        using var shutdownSignal = new ShutdownSignal(new PosixSignalRegistrar(), new ProcessTerminator(), TimeProvider.System);
        return Main(args, standardOutputStream, standardErrorStream, shutdownSignal);
    }

    /// <summary>
    /// Runs one invocation with the given streams and shutdown signal; the seam through which in-process tests raise a signal on every platform (CL-18).
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="standardOutputStream">The standard output stream.</param>
    /// <param name="standardErrorStream">The standard error stream.</param>
    /// <param name="shutdownSignal">The shutdown signal whose token cancels the command (CL-18).</param>
    /// <returns>The process exit code.</returns>
    public static int Main(string[] args, Stream standardOutputStream, Stream standardErrorStream, IShutdownSignal shutdownSignal)
    {
        ArgumentNullException.ThrowIfNull(standardOutputStream);
        ArgumentNullException.ThrowIfNull(standardErrorStream);
        ArgumentNullException.ThrowIfNull(shutdownSignal);

        // Console.Out uses the console code page when redirected on Windows; the document and the log lines are UTF-8 without byte-order mark (CL-12, REQ-068).
        using var standardOutput = new StreamWriter(standardOutputStream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        using var standardError = new StreamWriter(standardErrorStream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };

        var environment = new EnvironmentReader();
        var exitCodePolicy = new ExitCodePolicy();
        var executor = new CommandExecutor(
            new ConfigurationLoader(environment),
            new ConfigurationRegistrar(),
            new CommandRegistrar(),
            new ConfigurationValidator(new ReferenceStationResolver(), new CredentialProvider(environment, new FileModeReader())),
            exitCodePolicy,
            new LibraryErrors(),
            new LogLineFormatter(),
            shutdownSignal,
            new LibraryDeadlineResolver(),
            TimeProvider.System);
        return Run(
            args,
            standardOutput,
            standardError,
            new HelpWriter(),
            new CommandLineParser(),
            new CommandRequestValidator(new CoordinateParser(), new SearchParameterFactory()),
            executor,
            exitCodePolicy);
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
    /// <param name="exitCodePolicy">The exit code policy.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args, TextWriter standardOutput, TextWriter standardError, IHelpWriter helpWriter, ICommandLineParser parser, ICommandRequestValidator validator, ICommandExecutor executor, IExitCodePolicy exitCodePolicy)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        ArgumentNullException.ThrowIfNull(helpWriter);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(exitCodePolicy);

        var command = "-";
        try
        {
            var result = parser.Parse(args);
            if (result is ParsedCommandLine parsed)
            {
                command = CommandName(parsed.Command);
                result = validator.Validate(parsed);
            }

            switch (result)
            {
                case HelpRequest { Command: null }:
                    helpWriter.WriteHelp(standardOutput);
                    return ExitCodes.Success;
                case HelpRequest help:
                    helpWriter.WriteCommandHelp(standardOutput, help.Command.Value);
                    return ExitCodes.Success;
                case UsageError error:
                    var commandForLog = error.Command is { } known ? CommandName(known) : command;
                    Log(standardError, commandForLog, logger => logger.LogError(new EventId(0, error.ErrorId), "{Message} Use --Help for the commands and options.", error.Message));
                    return exitCodePolicy.ForError(error.ErrorId);
                default:
                    return executor.Execute(((ValidatedCommand)result).Request, standardOutput, standardError);
            }
        }
        catch (Exception exception)
        {
            Log(standardError, command, logger => logger.LogError(new EventId(0, CliErrorIds.Unexpected), exception, "The command failed unexpectedly."));
            return ExitCodes.Failure;
        }
    }

    private static void Log(TextWriter standardError, string command, Action<ILogger> write)
    {
        using var provider = new StandardErrorLoggerProvider(standardError, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, command);
        write(provider.CreateLogger("WeatherParser.Cli"));
    }

    private static string CommandName(CliCommand command) => command switch
    {
        CliCommand.SelectDwd => "SelectDWD",
        CliCommand.ParseDwd => "ParseDWD",
        CliCommand.ParseOstHessen => "ParseOstHessen",
        CliCommand.SelectMosmix => "SelectMOSMIX",
        _ => "ParseMOSMIX",
    };
}
