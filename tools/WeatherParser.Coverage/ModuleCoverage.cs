namespace WeatherParser.Coverage;

/// <summary>
/// One &lt;module&gt; of the native report.
/// </summary>
/// <param name="Name">Module name.</param>
/// <param name="ModuleId">Module identifier.</param>
/// <param name="Path">Module file path.</param>
/// <param name="Blocks">Block counters for this module.</param>
public sealed record ModuleCoverage(string Name, string ModuleId, string Path, BlockCounters Blocks);
