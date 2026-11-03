using System;
using System.IO;

namespace WeatherParser.SchemaGeneration.Tests;

/// <summary>
/// Locates the repository root, the first ancestor of the test output directory that contains WeatherParser.slnx.
/// </summary>
internal static class RepositoryRoot
{
    /// <summary>
    /// Finds the repository root directory containing WeatherParser.slnx.
    /// </summary>
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "WeatherParser.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("WeatherParser.slnx was not found above " + AppContext.BaseDirectory + ".");
    }
}
