namespace WeatherParser.Coverage;

/// <summary>
/// Pull request category for coverage evaluation.
/// </summary>
public enum PullRequestCategory
{
    /// <summary>
    /// Documentation-only changes.
    /// </summary>
    DocumentationOnly,

    /// <summary>
    /// Code changes.
    /// </summary>
    Code
}
