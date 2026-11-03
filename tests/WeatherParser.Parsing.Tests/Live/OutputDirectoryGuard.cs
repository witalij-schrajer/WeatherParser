using System;
using System.IO;

namespace WeatherParser.Parsing.Tests.Live;

internal static class OutputDirectoryGuard
{
    public static string? RepositoryRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        while (true)
        {
            if (Directory.Exists(Path.Combine(baseDir, ".git")) || File.Exists(Path.Combine(baseDir, ".git")))
            {
                return baseDir;
            }

            var parent = Directory.GetParent(baseDir);
            if (parent is null)
            {
                return null;
            }

            baseDir = parent.FullName;
        }
    }

    public static string EnsureAllowed(string directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

        var full = Path.GetFullPath(directory);

        var root = RepositoryRoot();
        if (root is null)
        {
            return full;
        }

        var relative = Path.GetRelativePath(root, full);
        // "." is the root itself; only the exact parent segment ".." (alone or followed by a separator) leaves the tree,
        // a child such as "..private" does not.
        var leavesTree = relative == ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
        var inside = !leavesTree && !Path.IsPathRooted(relative);
        if (!inside)
        {
            return full;
        }

        // The gitignore pattern "artifacts/" is case-sensitive on Linux; only Windows compares file names case-insensitively.
        var firstSegment = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (string.Equals(firstSegment, "artifacts", comparison))
        {
            return full;
        }

        throw new InvalidOperationException($"The output directory '{full}' lies inside the repository working tree '{root}'; only its gitignored 'artifacts' directory or a directory outside the repository is allowed.");
    }
}
