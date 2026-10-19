namespace WeatherParser.Coverage;

/// <summary>
/// Parsed native report. Format is always "native-xml".
/// </summary>
/// <param name="Modules">List of module coverages.</param>
/// <param name="Format">Report format identifier.</param>
public sealed record CoverageReport(IReadOnlyList<ModuleCoverage> Modules, string Format);
