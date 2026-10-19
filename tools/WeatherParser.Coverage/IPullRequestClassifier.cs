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
}
