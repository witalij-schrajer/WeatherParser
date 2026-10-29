namespace WeatherParser.Cli.Arguments;

/// <summary>
/// A parser for command-line arguments.
/// </summary>
public sealed class CommandLineParser : ICommandLineParser
{
    private const string ArgumentInvalid = "CLI-ARGUMENT-INVALID";
    private const string Help = "Help";
    private const string Output = "Output";
    private const string OutputFile = "OutputFile";
    private const string Product = "Product";

    private static readonly IReadOnlyDictionary<string, CliCommand> Commands = new Dictionary<string, CliCommand>(StringComparer.OrdinalIgnoreCase)
    {
        ["SelectDWD"] = CliCommand.SelectDwd, ["ParseDWD"] = CliCommand.ParseDwd, ["ParseOstHessen"] = CliCommand.ParseOstHessen,
        ["SelectMOSMIX"] = CliCommand.SelectMosmix, ["ParseMOSMIX"] = CliCommand.ParseMosmix,
    };

    private static readonly IReadOnlyDictionary<CliCommand, string> CommandNames = Commands.ToDictionary(pair => pair.Value, pair => pair.Key);

    private static readonly string[] ValueOptions = { "Latitude", "Longitude", "Altitude", "MaxDistance", "MaxAltDifference", "StationID", "StationNamePattern", "Product", "MOSMIXID", "Output", "OutputFile", "Config" };
    private static readonly string[] FlagOptions = { "RefreshMosmixSEvidence", "Verbose", "Help" };
    private static readonly string[] GlobalOptions = { "Output", "OutputFile", "Config", "Verbose" };
    private static readonly string[] SearchOptions = { "Latitude", "Longitude", "Altitude", "MaxDistance", "MaxAltDifference" };

    // Per command: the required options and the optional (non-global) options.
    private static readonly IReadOnlyDictionary<CliCommand, (string[] Required, string[] Optional)> CommandOptions = new Dictionary<CliCommand, (string[], string[])>
    {
        [CliCommand.SelectDwd] = (SearchOptions, Array.Empty<string>()),
        [CliCommand.SelectMosmix] = (SearchOptions, new[] { "RefreshMosmixSEvidence" }),
        [CliCommand.ParseDwd] = (Array.Empty<string>(), new[] { "StationID" }),
        [CliCommand.ParseOstHessen] = (new[] { "StationNamePattern" }, Array.Empty<string>()),
        [CliCommand.ParseMosmix] = (new[] { "Product", "MOSMIXID" }, Array.Empty<string>()),
    };

    private static readonly string[] OutputFormats = { "mqtt", "json", "xml" };
    private static readonly string[] Products = { "MOSMIX_S", "MOSMIX_L" };

    /// <inheritdoc/>
    public CommandLineResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var commands = new List<CliCommand>();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < args.Count; i++)
        {
            var token = args[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
                return new UsageError(ArgumentInvalid, $"Unexpected argument '{token}'.");

            var name = token[2..];

            if (Commands.TryGetValue(name, out var command))
            {
                if (commands.Contains(command))
                    return new UsageError(ArgumentInvalid, $"Option '--{CommandNames[command]}' is specified more than once.");
                commands.Add(command);
                continue;
            }

            var canonical = ValueOptions.Concat(FlagOptions).FirstOrDefault(o => string.Equals(o, name, StringComparison.OrdinalIgnoreCase));
            if (canonical == null)
                return new UsageError(ArgumentInvalid, $"Unknown option '{token}'.");

            if (values.ContainsKey(canonical) || flags.Contains(canonical))
                return new UsageError(ArgumentInvalid, $"Option '--{canonical}' is specified more than once.");

            if (FlagOptions.Contains(canonical))
            {
                flags.Add(canonical);
                continue;
            }

            // Value option
            if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                return new UsageError(ArgumentInvalid, $"Option '--{canonical}' requires a value.");

            values[canonical] = args[i + 1];
            i++;
        }

        // Help handling
        if (flags.Contains(Help))
        {
            if (flags.Count == 1 && values.Count == 0 && commands.Count <= 1)
                return new HelpRequest(commands.Count == 1 ? commands[0] : null);
            else
                return new UsageError(ArgumentInvalid, "Option '--Help' is allowed alone or with exactly one command.");
        }

        if (commands.Count == 0)
            return new UsageError("CLI-COMMAND-MISSING", "No command given; use exactly one of --SelectDWD, --ParseDWD, --ParseOstHessen, --SelectMOSMIX, --ParseMOSMIX.");

        if (commands.Count > 1)
            return new UsageError("CLI-COMMAND-CONFLICT", "Only one command is allowed per invocation.");

        var selected = commands[0];
        var (required, optional) = CommandOptions[selected];
        var commandName = CommandNames[selected];

        // Check for invalid options
        var order = ValueOptions.Concat(FlagOptions).ToList();
        var notAllowed = values.Keys.Concat(flags)
            .OrderBy(order.IndexOf)
            .FirstOrDefault(option => !required.Contains(option) && !optional.Contains(option) && !GlobalOptions.Contains(option));
        if (notAllowed != null)
            return new UsageError(ArgumentInvalid, $"Option '--{notAllowed}' is not allowed with '--{commandName}'.");

        var missing = required.FirstOrDefault(option => !values.ContainsKey(option));
        if (missing != null)
            return new UsageError(ArgumentInvalid, $"Option '--{missing}' is required with '--{commandName}'.");

        var output = values.GetValueOrDefault(Output);
        if (output != null && !OutputFormats.Contains(output, StringComparer.OrdinalIgnoreCase))
            return new UsageError(ArgumentInvalid, $"Unknown output format '{output}'; expected mqtt, json or xml.");

        if (values.ContainsKey(OutputFile) && (output == null || string.Equals(output, "mqtt", StringComparison.OrdinalIgnoreCase)))
            return new UsageError(ArgumentInvalid, "Option '--OutputFile' requires '--Output json' or '--Output xml'.");

        var product = values.GetValueOrDefault(Product);
        if (product != null && !Products.Contains(product, StringComparer.OrdinalIgnoreCase))
            return new UsageError(ArgumentInvalid, $"Unknown product '{product}'; expected MOSMIX_S or MOSMIX_L.");

        return new ParsedCommandLine(selected, values, flags);
    }
}
