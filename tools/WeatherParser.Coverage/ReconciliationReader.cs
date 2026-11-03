using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Coverage;

/// <summary>
/// Reads recorded baseline reconciliations from JSON files.
/// </summary>
public sealed class ReconciliationReader : IReconciliationReader
{
    private readonly IFileReader _fileReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconciliationReader"/> class.
    /// </summary>
    /// <param name="fileReader">The file reader to use for reading files.</param>
    public ReconciliationReader(IFileReader fileReader)
    {
        _fileReader = fileReader ?? throw new System.ArgumentNullException(nameof(fileReader));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReconciliationEntry>> ReadAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path) || !_fileReader.Exists(path))
        {
            return Array.Empty<ReconciliationEntry>();
        }

        var json = await _fileReader.ReadAllTextAsync(path, cancellationToken);
        return ReadFromJson(json, path);
    }

    private IReadOnlyList<ReconciliationEntry> ReadFromJson(string json, string path)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var document = JsonSerializer.Deserialize<ReconciliationDocument>(json, options);

            if (document == null || document.Entries == null)
            {
                throw new CoverageInputException($"Reconciliation file '{path}' is missing the required 'entries' array.");
            }

            var entries = new List<ReconciliationEntry>(document.Entries.Count);
            for (int i = 0; i < document.Entries.Count; i++)
            {
                var entry = document.Entries[i];
                if (entry == null)
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} is null.");
                }

                if (string.IsNullOrWhiteSpace(entry.Gate))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} is missing required 'gate'.");
                }

                if (string.IsNullOrWhiteSpace(entry.TargetCommit))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} is missing required 'targetCommit'.");
                }

                if (entry.TargetCommit.Length != 40 || !System.Text.RegularExpressions.Regex.IsMatch(entry.TargetCommit, "^[0-9a-fA-F]{40}$"))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} has an invalid 'targetCommit'; expected a full 40-character commit ID.");
                }

                if (entry.PullRequest <= 0)
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} has an invalid 'pullRequest'; expected a number greater than 0.");
                }

                if (entry.Gate != "library" && entry.Gate != "cli")
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} has an unknown gate '{entry.Gate}'; expected library or cli.");
                }

                if (string.IsNullOrWhiteSpace(entry.HeadRepository))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} is missing required 'headRepository'.");
                }

                if (string.IsNullOrWhiteSpace(entry.HeadBranch))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} is missing required 'headBranch'.");
                }

                if (string.IsNullOrWhiteSpace(entry.ApprovedOn))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} is missing required 'approvedOn'.");
                }

                if (!DateOnly.TryParseExact(entry.ApprovedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} has an invalid 'approvedOn'; expected yyyy-MM-dd.");
                }

                if (string.IsNullOrWhiteSpace(entry.Record))
                {
                    throw new CoverageInputException($"Reconciliation file '{path}' entry {i} is missing required 'record'.");
                }

                entries.Add(new ReconciliationEntry(
                    entry.Gate,
                    entry.TargetCommit,
                    entry.PullRequest,
                    entry.HeadRepository,
                    entry.HeadBranch,
                    entry.ApprovedOn,
                    entry.Record));
            }

            return entries;
        }
        catch (JsonException ex)
        {
            throw new CoverageInputException($"Reconciliation file '{path}' is not valid JSON: {ex.Message}", ex);
        }
    }

    private sealed record ReconciliationDocument(IList<ReconciliationEntryDocument>? Entries);

    private sealed record ReconciliationEntryDocument(
        string? Gate,
        string? TargetCommit,
        int PullRequest,
        string? HeadRepository,
        string? HeadBranch,
        string? ApprovedOn,
        string? Record);
}
