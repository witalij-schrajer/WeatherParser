namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Parses runner command-line arguments into <see cref="EvaluateCommand"/>, <see cref="WriteBaselineCommand"/>
/// or a <see cref="UsageError"/>. Option names are matched case-sensitively; user input never causes an exception.
/// </summary>
public sealed class CommandLineParser : ICommandLineParser
{
    private const string Evaluate = "evaluate";
    private const string WriteBaseline = "write-baseline";
    private const string Bootstrap = "bootstrap";
    private const string Exclude = "exclude";
    private const string GatedInput = "gated-input";

    private static readonly HashSet<string> EvaluateOptions = new(StringComparer.Ordinal)
    {
        "report", "baseline", "changed-files", "expected-baseline-commit", "tool-name", "tool-version", Exclude, Bootstrap, GatedInput,
    };

    private static readonly HashSet<string> WriteBaselineOptions = new(StringComparer.Ordinal)
    {
        "report", "commit", "tool-name", "tool-version", Exclude, "output",
    };

    /// <inheritdoc/>
    public RunnerCommand Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Count == 0)
        {
            return new UsageError("Expected a command: evaluate or write-baseline.");
        }

        var command = args[0];
        var allowed = command switch
        {
            Evaluate => EvaluateOptions,
            WriteBaseline => WriteBaselineOptions,
            _ => null,
        };

        if (allowed is null)
        {
            return new UsageError($"Unknown command '{command}'. Expected a command: evaluate or write-baseline.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var exclusions = new List<string>();
        var gatedInputs = new List<string>();
        var bootstrap = false;

        var i = 1;
        while (i < args.Count)
        {
            var token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                return new UsageError($"Unexpected argument '{token}'.");
            }

            var name = token[2..];
            if (!allowed.Contains(name))
            {
                return new UsageError($"Unknown option '--{name}' for '{command}'.");
            }

            if (name == Bootstrap)
            {
                if (bootstrap)
                {
                    return new UsageError("Option '--bootstrap' is specified more than once.");
                }

                bootstrap = true;
                i += 1;
                continue;
            }

            if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                return new UsageError($"Option '--{name}' requires a value.");
            }

            var value = args[i + 1];
            i += 2;

            if (name == Exclude)
            {
                exclusions.Add(value);
                continue;
            }

            if (name == GatedInput)
            {
                gatedInputs.Add(value);
                continue;
            }

            if (!values.TryAdd(name, value))
            {
                return new UsageError($"Option '--{name}' is specified more than once.");
            }
        }

        return command == Evaluate
            ? CreateEvaluate(values, exclusions, bootstrap, gatedInputs)
            : CreateWriteBaseline(values, exclusions);
    }

    private static RunnerCommand CreateEvaluate(Dictionary<string, string> values, List<string> exclusions, bool bootstrap, List<string> gatedInputs)
    {
        if (FindMissing(values, Evaluate, "report", "changed-files", "tool-name", "tool-version") is { } missing)
        {
            return missing;
        }

        values.TryGetValue("baseline", out var baseline);
        values.TryGetValue("expected-baseline-commit", out var expectedCommit);

        if (baseline is not null && expectedCommit is null)
        {
            return new UsageError("Option '--expected-baseline-commit' is required when '--baseline' is specified.");
        }

        if (expectedCommit is not null && baseline is null)
        {
            return new UsageError("Option '--expected-baseline-commit' requires '--baseline'.");
        }

        if (bootstrap && baseline is not null)
        {
            return new UsageError("Options '--bootstrap' and '--baseline' cannot be combined.");
        }

        return new EvaluateCommand(
            values["report"],
            baseline,
            values["changed-files"],
            expectedCommit,
            values["tool-name"],
            values["tool-version"],
            exclusions,
            bootstrap,
            gatedInputs);
    }

    private static RunnerCommand CreateWriteBaseline(Dictionary<string, string> values, List<string> exclusions)
    {
        if (FindMissing(values, WriteBaseline, "report", "commit", "tool-name", "tool-version", "output") is { } missing)
        {
            return missing;
        }

        return new WriteBaselineCommand(
            values["report"],
            values["commit"],
            values["tool-name"],
            values["tool-version"],
            exclusions,
            values["output"]);
    }

    private static UsageError? FindMissing(Dictionary<string, string> values, string command, params string[] required)
    {
        foreach (var name in required)
        {
            if (!values.ContainsKey(name))
            {
                return new UsageError($"Option '--{name}' is required for '{command}'.");
            }
        }

        return null;
    }
}
