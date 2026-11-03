namespace WeatherParser.Coverage;

/// <summary>
/// Classifies pull requests based on the changed files.
/// A pull request is documentation-only when the list is non-empty and every changed path is a Markdown file;
/// any other change (source, projects, packages, lock files, build/test/coverage/CI configuration or exclusions)
/// makes it a code change. Folder names never grant the documentation-only classification.
/// </summary>
public sealed class PullRequestClassifier : IPullRequestClassifier
{
    /// <inheritdoc/>
    public PullRequestCategory Classify(ChangedFileList changes)
    {
        if (changes.Paths.Count == 0)
        {
            return PullRequestCategory.Code;
        }

        foreach (var path in changes.Paths)
        {
            if (!IsDocumentationPath(path))
            {
                return PullRequestCategory.Code;
            }
        }

        return PullRequestCategory.DocumentationOnly;
    }

    private static bool IsDocumentationPath(string path) =>
        path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool TouchesGatedInputs(ChangedFileList changes, IReadOnlyList<string> gatedInputs)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(gatedInputs);

        return changes.Paths.Any(path => gatedInputs.Any(input => input.EndsWith('/')
            ? path.StartsWith(input, StringComparison.Ordinal)
            : string.Equals(path, input, StringComparison.Ordinal)));
    }
}
