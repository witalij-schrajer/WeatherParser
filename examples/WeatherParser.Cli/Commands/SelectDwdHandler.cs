namespace WeatherParser.Cli.Commands;

using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Selection;

/// <summary>
/// Runs --SelectDWD: selects CDC stations and writes the standalone station list (CL-09).
/// </summary>
public sealed class SelectDwdHandler : ICommandHandler<SelectDwdRequest>
{
    private readonly ICdcStationSelector _selector;
    private readonly IResultWriter _writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectDwdHandler"/> class.
    /// </summary>
    /// <param name="selector">The CDC station selector.</param>
    /// <param name="writer">The result writer.</param>
    public SelectDwdHandler(ICdcStationSelector selector, IResultWriter writer)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <inheritdoc/>
    public async Task<CommandOutcome> HandleAsync(SelectDwdRequest request, CommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var list = await _selector.SelectAsync(request.Search, cancellationToken).ConfigureAwait(false);
        await _writer.WriteAsync(list, context, cancellationToken).ConfigureAwait(false);
        return new CommandOutcome(1, 0, false); // The selection is one requested item; an empty list is a complete success (REQ-023, REQ-101).
    }
}
