using System;

namespace WeatherParser.Cli;

/// <summary>
/// Reads environment variables.
/// </summary>
public sealed class EnvironmentReader : IEnvironmentReader
{
    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if not found.</returns>
    public string? Get(string name) => Environment.GetEnvironmentVariable(name);
}
