namespace WeatherParser.Coverage;

/// <summary>
/// Classifies pull requests based on the changed files.
/// </summary>
public interface IPullRequestClassifier
{
    /// <summary>
    /// Determines whether the pull request is documentation-only or code.
    /// </summary>
    /// <param name="changes">List of changed files.</param>
    /// <returns>The category of the pull request.</returns>
    PullRequestCategory Classify(ChangedFileList changes);

    /// <summary>
    /// Determines whether any changed path is a gated input: equal to an exact gated path, or below a gated directory prefix ending with '/'.
    /// </summary>
    /// <param name="changes">List of changed files.</param>
    /// <param name="gatedInputs">Exact repository-relative paths and directory prefixes ending with '/'.</param>
    /// <returns>True if at least one changed path is a gated input.</returns>
    bool TouchesGatedInputs(ChangedFileList changes, IReadOnlyList<string> gatedInputs);
}
