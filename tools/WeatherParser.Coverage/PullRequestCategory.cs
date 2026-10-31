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
    GatedInputsUnchanged,

    /// <summary>
    /// Code changes covered by a user-approved, recorded baseline reconciliation for this gate, target, head repository and head branch (REQ-086 amendment 2): equal coverage is accepted, a decrease fails.
    /// </summary>
    Reconciled
}
