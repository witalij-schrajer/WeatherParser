using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Writes coverage evaluation results as indented camelCase JSON to a TextWriter.
/// </summary>
public sealed class JsonResultWriter(TextWriter output) : IResultWriter
{
    private readonly TextWriter _output = output ?? throw new ArgumentNullException(nameof(output));
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc/>
    public async Task WriteAsync(CoverageResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);

        var dto = new ResultDto(
            result.Passed,
            result.Current is null ? null : new BlockCountersDto(result.Current.Covered, result.Current.NotCovered),
            result.Rules.Select(r => new RuleResultDto(r.RuleId, r.Verdict, r.Diagnostic)).ToArray());

        var json = JsonSerializer.Serialize(dto, Options);
        await _output.WriteLineAsync(json.AsMemory(), cancellationToken);
        await _output.FlushAsync(cancellationToken);
    }

    private sealed record ResultDto(bool Passed, BlockCountersDto? Current, RuleResultDto[] Rules);

    private sealed record BlockCountersDto(long Covered, long NotCovered);

    private sealed record RuleResultDto(string RuleId, bool Verdict, string Diagnostic);
}
