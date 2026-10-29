using Microsoft.Extensions.DependencyInjection;

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
        return Run(args, Console.Out, Console.Error, new HelpWriter());
    }

    /// <summary>
    /// Handles the invocations that need no container: help and, provisionally until P3.04, every other invocation (CL-07).
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="standardOutput">The standard output writer.</param>
    /// <param name="standardError">The standard error writer.</param>
    /// <param name="helpWriter">The help writer.</param>
    /// <returns>The process exit code.</returns>
    public static int Run(string[] args, TextWriter standardOutput, TextWriter standardError, IHelpWriter helpWriter)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        ArgumentNullException.ThrowIfNull(helpWriter);

        if (args.Length == 1 && string.Equals(args[0], "--Help", StringComparison.OrdinalIgnoreCase))
        {
            helpWriter.WriteHelp(standardOutput);
            return 0;
        }

        if (args.Length == 0)
        {
            helpWriter.WriteHelp(standardError);
            return 1;
        }

        standardError.WriteLine("The command is not implemented yet.");
        return 1;
    }

    /// <summary>
    /// Builds the container, runs exactly one command through ICommandRunner and disposes the container afterwards (CL-07).
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="buildProvider">The function to build the service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string[] args, Func<ServiceProvider> buildProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(buildProvider);

        await using var provider = buildProvider();
        var runner = provider.GetRequiredService<ICommandRunner>();
        return await runner.RunAsync(args, cancellationToken);
    }
}
