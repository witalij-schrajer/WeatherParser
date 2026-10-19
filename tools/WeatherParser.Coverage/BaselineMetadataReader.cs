using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Coverage;

/// <summary>
/// Reads baseline metadata from JSON files.
/// </summary>
public sealed class BaselineMetadataReader : IBaselineMetadataReader
{
    private readonly IFileReader _fileReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaselineMetadataReader"/> class.
    /// </summary>
    /// <param name="fileReader">The file reader to use for reading files.</param>
    public BaselineMetadataReader(IFileReader fileReader)
    {
        _fileReader = fileReader ?? throw new System.ArgumentNullException(nameof(fileReader));
    }

    /// <inheritdoc/>
    public async Task<BaselineMetadata?> ReadAsync(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path) || !_fileReader.Exists(path))
        {
            return null;
        }

        var json = await _fileReader.ReadAllTextAsync(path, cancellationToken);
        return ReadFromJson(json, path);
    }

    private BaselineMetadata ReadFromJson(string json, string path)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var document = JsonSerializer.Deserialize<BaselineMetadataDocument>(json, options);

            if (document == null)
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is malformed or empty.");
            }

            if (string.IsNullOrWhiteSpace(document.CommitId))
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is missing required 'commitId'.");
            }

            if (string.IsNullOrWhiteSpace(document.ToolName))
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is missing required 'toolName'.");
            }

            if (string.IsNullOrWhiteSpace(document.ToolVersion))
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is missing required 'toolVersion'.");
            }

            if (string.IsNullOrWhiteSpace(document.Format))
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is missing required 'format'.");
            }

            if (document.Exclusions == null)
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is missing required 'exclusions' array.");
            }

            if (!document.BlocksCovered.HasValue)
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is missing 'blocksCovered'.");
            }

            if (!document.BlocksNotCovered.HasValue)
            {
                throw new CoverageInputException($"Baseline metadata '{path}' is missing 'blocksNotCovered'.");
            }

            if (document.BlocksCovered.Value < 0 || document.BlocksNotCovered.Value < 0)
            {
                throw new CoverageInputException($"Baseline metadata '{path}' has negative block counters.");
            }

            if (document.BlocksCovered.Value > long.MaxValue - document.BlocksNotCovered.Value)
            {
                throw new CoverageInputException($"Baseline metadata '{path}' has block counters whose total exceeds the supported range.");
            }

            return new BaselineMetadata(
                document.CommitId,
                document.ToolName,
                document.ToolVersion,
                document.Format,
                document.Exclusions.ToArray(),
                new BlockCounters(document.BlocksCovered.Value, document.BlocksNotCovered.Value));
        }
        catch (JsonException ex)
        {
            throw new CoverageInputException($"Baseline metadata '{path}' is not valid JSON: {ex.Message}", ex);
        }
    }

    private sealed record BaselineMetadataDocument(
        string? CommitId,
        string? ToolName,
        string? ToolVersion,
        string? Format,
        System.Collections.Generic.IList<string>? Exclusions,
        long? BlocksCovered,
        long? BlocksNotCovered);
}
