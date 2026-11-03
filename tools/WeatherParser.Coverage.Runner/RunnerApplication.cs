using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Main runner application entry point.
/// </summary>
public sealed class RunnerApplication : IRunnerApplication
{
    private readonly ICommandLineParser _parser;
    private readonly ICoverageGate _gate;
    private readonly IBaselineWriter _baselineWriter;
    private readonly IResultWriter _resultWriter;
    private readonly IFileReader _fileReader;
    private readonly TextWriter _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnerApplication"/> class.
    /// </summary>
    /// <param name="parser">The command-line parser.</param>
    /// <param name="gate">The coverage gate evaluator.</param>
    /// <param name="baselineWriter">The baseline writer.</param>
    /// <param name="resultWriter">The result writer.</param>
    /// <param name="fileReader">The file reader for checking baseline existence.</param>
    /// <param name="error">The error output writer.</param>
    public RunnerApplication(
        ICommandLineParser parser,
        ICoverageGate gate,
        IBaselineWriter baselineWriter,
        IResultWriter resultWriter,
        IFileReader fileReader,
        TextWriter error)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _baselineWriter = baselineWriter ?? throw new ArgumentNullException(nameof(baselineWriter));
        _resultWriter = resultWriter ?? throw new ArgumentNullException(nameof(resultWriter));
        _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <inheritdoc/>
    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);

        var command = _parser.Parse(args);
        switch (command)
        {
            case EvaluateCommand evaluate:
                return await EvaluateAsync(evaluate, cancellationToken);
            case WriteBaselineCommand writeBaseline:
                return await WriteBaselineAsync(writeBaseline, cancellationToken);
            case UsageError usageError:
                await _error.WriteLineAsync($"Usage error: {usageError.Message}");
                await _error.WriteLineAsync("Expected a command: evaluate or write-baseline.");
                return 2;
            default:
                throw new InvalidOperationException($"Unexpected command type: {command.GetType().Name}");
        }
    }

    private async Task<int> EvaluateAsync(EvaluateCommand command, CancellationToken cancellationToken)
    {
        if (command.BaselinePath != null && !_fileReader.Exists(command.BaselinePath))
        {
            await _error.WriteLineAsync($"Baseline file '{command.BaselinePath}' was not found.");
            return 2;
        }

        var request = new CoverageGateRequest(
            command.ReportPath,
            command.BaselinePath,
            command.ChangedFilesPath,
            command.ExpectedBaselineCommit ?? string.Empty,
            command.ToolName,
            command.ToolVersion,
            command.Exclusions,
            command.Bootstrap,
            command.GatedInputs,
            command.GateName,
            command.ReconciliationPath,
            command.HeadRepository,
            command.HeadBranch,
            command.BaseRepository,
            command.BaseBranch);

        var result = await _gate.EvaluateAsync(request, cancellationToken);
        await _resultWriter.WriteAsync(result, cancellationToken);

        if (result.Passed)
        {
            return 0;
        }

        // Check for Input rule failure
        var inputRule = result.Rules.FirstOrDefault(r => r.RuleId == "Input");
        if (inputRule != null && !inputRule.Verdict)
        {
            foreach (var rule in result.Rules)
            {
                await _error.WriteLineAsync(rule.Diagnostic);
            }

            return 2;
        }

        // Report failed rules that are not Input
        foreach (var rule in result.Rules)
        {
            if (!rule.Verdict && rule.RuleId != "Input")
            {
                await _error.WriteLineAsync($"{rule.RuleId}: {rule.Diagnostic}");
            }
        }

        return 1;
    }

    private async Task<int> WriteBaselineAsync(WriteBaselineCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await _baselineWriter.WriteAsync(command, cancellationToken);
            return 0;
        }
        catch (CoverageInputException ex)
        {
            await _error.WriteLineAsync(ex.Message);
            return 2;
        }
    }
}
