namespace WeatherParser.Cli.Commands;

using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Catalogs;

/// <summary>
/// Runs --SelectMOSMIX: optionally refreshes the MOSMIX_S evidence, selects MOSMIX locations and writes the standalone location list (CL-09, CL-17).
/// </summary>
public sealed class SelectMosmixHandler : ICommandHandler<SelectMosmixRequest>
{
    private readonly IMosmixLocationSelector _selector;
    private readonly IResultWriter _writer;
    private readonly IMosmixSEvidenceRefresher? _refresher;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectMosmixHandler"/> class.
    /// </summary>
    /// <param name="selector">The MOSMIX location selector.</param>
    /// <param name="writer">The result writer.</param>
    /// <param name="refresher">The MOSMIX_S evidence refresher; only registered when --RefreshMosmixSEvidence is given, otherwise null (CL-17).</param>
    public SelectMosmixHandler(IMosmixLocationSelector selector, IResultWriter writer, IMosmixSEvidenceRefresher? refresher)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _refresher = refresher;
    }

    /// <inheritdoc/>
    public async Task<CommandOutcome> HandleAsync(SelectMosmixRequest request, CommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.RefreshMosmixSEvidence)
        {
            // The refresh runs first; a failed refresh propagates and no selection runs, never a silent fallback to an old index (CL-17).
            var refresher = _refresher ?? throw new InvalidOperationException("The MOSMIX_S evidence refresher is not registered although --RefreshMosmixSEvidence was given.");
            await refresher.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }

        var list = await _selector.SelectAsync(request.Search, cancellationToken).ConfigureAwait(false);
        await _writer.WriteAsync(list, context, cancellationToken).ConfigureAwait(false);
        return new CommandOutcome(1, 0, false);
    }
}
