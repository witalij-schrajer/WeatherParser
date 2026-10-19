namespace WeatherParser.Coverage;

/// <summary>
/// List of changed files.
/// </summary>
/// <param name="Paths">List of file paths.</param>
public sealed record ChangedFileList(IReadOnlyList<string> Paths);
