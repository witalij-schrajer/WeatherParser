using System;
using System.Collections.Generic;
using System.Linq;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// The result of one MOSMIX parse: the run, the builder's omissions, the not-exported and absent element names, the freshness and the run age (contract MF-02).
/// </summary>
public sealed class MosmixParseResult
{
    /// <summary>
    /// Null arguments throw ArgumentNullException; the three lists are copied.
    /// </summary>
    public MosmixParseResult(ForecastRun run, IReadOnlyList<OmittedForecastValue> omittedValues, IReadOnlyList<string> notExportedElements, IReadOnlyList<string> absentElements, MosmixRunFreshness freshness, TimeSpan runAge)
    {
        Run = run ?? throw new ArgumentNullException(nameof(run));
        OmittedValues = omittedValues?.ToArray() ?? throw new ArgumentNullException(nameof(omittedValues));
        NotExportedElements = notExportedElements?.ToArray() ?? throw new ArgumentNullException(nameof(notExportedElements));
        AbsentElements = absentElements?.ToArray() ?? throw new ArgumentNullException(nameof(absentElements));
        Freshness = freshness;
        RunAge = runAge;
    }

    /// <summary>
    /// Gets the forecast run.
    /// </summary>
    public ForecastRun Run { get; }

    /// <summary>
    /// Gets the builder's omissions.
    /// </summary>
    public IReadOnlyList<OmittedForecastValue> OmittedValues { get; }

    /// <summary>
    /// Gets the not-exported element names.
    /// </summary>
    public IReadOnlyList<string> NotExportedElements { get; }

    /// <summary>
    /// Gets the absent element names.
    /// </summary>
    public IReadOnlyList<string> AbsentElements { get; }

    /// <summary>
    /// Gets the freshness.
    /// </summary>
    public MosmixRunFreshness Freshness { get; }

    /// <summary>
    /// Gets the run age.
    /// </summary>
    public TimeSpan RunAge { get; }
}
