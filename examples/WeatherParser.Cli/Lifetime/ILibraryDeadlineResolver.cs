namespace WeatherParser.Cli.Lifetime;

using System;
using WeatherParser.Cli.Arguments;

/// <summary>
/// Resolves the library deadline of the selected command from the validated container (CL-14).
/// </summary>
public interface ILibraryDeadlineResolver
{
    /// <summary>
    /// Returns the library deadline that bounds the selected command.
    /// </summary>
    /// <param name="services">The validated container.</param>
    /// <param name="request">The validated command request.</param>
    /// <returns>The library deadline of the command.</returns>
    TimeSpan Resolve(IServiceProvider services, CommandRequest request);
}
