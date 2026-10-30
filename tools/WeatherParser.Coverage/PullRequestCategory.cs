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
    Code,

    /// <summary>
    /// Code changes that leave every gated input unchanged (REQ-086 amendment, CL-19 option C): equal coverage is accepted.
    /// </summary>
    GatedInputsUnchanged
}
