namespace WeatherParser.Coverage;

/// <summary>
/// One user-approved, recorded baseline reconciliation (REQ-086 amendment 2): for the named gate, a code pull request from the named head repository and head branch against the exact target commit may keep equal coverage.
/// </summary>
/// <param name="Gate">Gate name: <c>library</c> or <c>cli</c>.</param>
/// <param name="TargetCommit">Full 40-character commit ID of the target the pull request is measured against.</param>
/// <param name="PullRequest">Pull request number.</param>
/// <param name="HeadRepository">Head repository of the pull request as <c>owner/name</c>.</param>
/// <param name="HeadBranch">Head branch of the pull request.</param>
/// <param name="ApprovedOn">Approval date as <c>yyyy-MM-dd</c>.</param>
/// <param name="Record">Repository-relative path (optionally with an anchor) of the record that holds the user's approval.</param>
public sealed record ReconciliationEntry(
    string Gate,
    string TargetCommit,
    int PullRequest,
    string HeadRepository,
    string HeadBranch,
    string ApprovedOn,
    string Record);
