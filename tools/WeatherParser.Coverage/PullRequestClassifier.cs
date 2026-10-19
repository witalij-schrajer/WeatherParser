namespace WeatherParser.Coverage;

/// <summary>
/// Classifies pull requests based on the changed files.
/// </summary>
public sealed class PullRequestClassifier : IPullRequestClassifier
{
    private static readonly IReadOnlyList<string> DocumentationExtensions = new[] { ".md", ".markdown", ".txt" };

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

    private static bool IsDocumentationPath(string path)
    {
        // Check for common documentation file extensions
        var lowerPath = path.ToLowerInvariant();
        foreach (var ext in DocumentationExtensions)
        {
            if (lowerPath.EndsWith(ext))
            {
                return true;
            }
        }

        // Also consider paths ending with .md or .markdown (case-insensitive)
        if (lowerPath.EndsWith(".md") || lowerPath.EndsWith(".markdown"))
        {
            return true;
        }

        // Consider common documentation-related file names
        var fileName = System.IO.Path.GetFileName(lowerPath);
        if (fileName == "readme.md" || fileName == "readme.markdown")
        {
            return true;
        }

        // Check for specific documentation-related paths
        if (lowerPath.Contains("/docs/") || lowerPath.Contains("/documentation/"))
        {
            return true;
        }

        return false;
    }
}
