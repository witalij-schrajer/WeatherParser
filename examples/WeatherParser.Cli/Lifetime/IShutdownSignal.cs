using System;
using Microsoft.Extensions.Logging;

namespace WeatherParser.Cli.Lifetime;

/// <summary>
/// Turns the first SIGINT or SIGTERM into a cancellation and ends the process on a second signal or after the grace period (CL-18).
/// </summary>
public interface IShutdownSignal : IDisposable
{
    /// <summary>
    /// Gets the token cancelled by the first shutdown signal.
    /// </summary>
    CancellationToken Token { get; }

    /// <summary>
    /// Sets the grace period and the logger used from now on; called once the configuration is validated (CL-18).
    /// </summary>
    /// <param name="gracePeriod">The time the command has to stop after the first signal.</param>
    /// <param name="logger">The logger for the signal events.</param>
    void Configure(TimeSpan gracePeriod, ILogger logger);

    /// <summary>
    /// Stops using the configured logger; called before the logger's output is closed (CL-18).
    /// </summary>
    void ReleaseLogger();
}
