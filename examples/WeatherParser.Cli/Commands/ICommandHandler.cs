namespace WeatherParser.Cli.Commands;

using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;

/// <summary>
/// Handles command requests.
/// </summary>
/// <typeparam name="TRequest">The type of the command request.</typeparam>
public interface ICommandHandler<in TRequest> where TRequest : CommandRequest
{
    /// <summary>
    /// Runs one validated command through the library and writes its results (CL-09).
    /// </summary>
    /// <param name="request">The command request to handle.</param>
    /// <param name="context">The command context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The counts of succeeded and failed requested items.</returns>
    Task<CommandOutcome> HandleAsync(TRequest request, CommandContext context, CancellationToken cancellationToken);
}
