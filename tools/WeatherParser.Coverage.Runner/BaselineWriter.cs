using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Writes baseline metadata from a coverage report.
/// </summary>
public sealed class BaselineWriter : IBaselineWriter
{
    private readonly ICoverageReportReader _reportReader;
    private readonly IProvenanceAggregator _aggregator;
    private readonly IFileWriter _fileWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaselineWriter"/> class.
    /// </summary>
    /// <param name="reportReader">The coverage report reader.</param>
    /// <param name="aggregator">The provenance aggregator.</param>
    /// <param name="fileWriter">The file writer for writing the baseline JSON.</param>
    public BaselineWriter(ICoverageReportReader reportReader, IProvenanceAggregator aggregator, IFileWriter fileWriter)
    {
        _reportReader = reportReader ?? throw new ArgumentNullException(nameof(reportReader));
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
    }

    /// <inheritdoc/>
    public async Task WriteAsync(WriteBaselineCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var report = await _reportReader.ReadAsync(command.ReportPath, cancellationToken);
        var aggregated = _aggregator.Aggregate(report, command.Exclusions);

        var baseline = new BaselineMetadataDto(
            command.CommitId,
            command.ToolName,
            command.ToolVersion,
            "native-xml",
            command.Exclusions,
            aggregated.Covered,
            aggregated.NotCovered);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(baseline, options);
        await _fileWriter.WriteAllTextAsync(command.OutputPath, json, cancellationToken);
    }

    private sealed record BaselineMetadataDto(
        string CommitId,
        string ToolName,
        string ToolVersion,
        string Format,
        IReadOnlyList<string> Exclusions,
        long BlocksCovered,
        long BlocksNotCovered);
}
