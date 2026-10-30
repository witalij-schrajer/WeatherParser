namespace WeatherParser.Cli;

/// <summary>
/// Provides functionality for reading environment variables.
/// </summary>
public interface IEnvironmentReader
{
    /// <summary>
    /// Gets the value of an environment variable.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>The value of the environment variable, or null if not found.</returns>
    string? Get(string name);
}
