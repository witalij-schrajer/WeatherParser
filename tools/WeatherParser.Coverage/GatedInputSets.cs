namespace WeatherParser.Coverage;

/// <summary>
/// Gated inputs per coverage gate (REQ-086 amendments of 2026-10-29 and 2026-10-31): exact repository-relative paths and directory prefixes ending with '/'.
/// </summary>
public static class GatedInputSets
{
    /// <summary>
    /// Gets the gated inputs for the library gate.
    /// </summary>
    public static IReadOnlyList<string> Library { get; } = Array.AsReadOnly(new[]
    {
        "src/",
        "Directory.Build.props",
        "Directory.Packages.props",
        "global.json",
        "NuGet.Config",
        ".editorconfig",
        "coverage.runsettings"
    });

    /// <summary>
    /// Gets the gated inputs for the CLI gate.
    /// </summary>
    public static IReadOnlyList<string> Cli { get; } = Array.AsReadOnly(new[]
    {
        "examples/",
        "tests/WeatherParser.Cli.Tests/",
        "tests/WeatherParser.Cli.SignalHost/",
        "src/",
        "Directory.Build.props",
        "Directory.Packages.props",
        "global.json",
        "NuGet.Config",
        ".editorconfig",
        "coverage.runsettings"
    });

    /// <summary>
    /// Returns the gated inputs of the named gate (<c>library</c> or <c>cli</c>), or null for an unknown gate name.
    /// </summary>
    /// <param name="gateName">The name of the gate.</param>
    /// <returns>The gated inputs for the specified gate, or null if the gate is unknown.</returns>
    public static IReadOnlyList<string>? ForGate(string gateName)
    {
        return gateName switch
        {
            "library" => Library,
            "cli" => Cli,
            _ => null
        };
    }
}
