using System;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Live;

/// <summary>
/// The result of one resource check run: the file size, the placemarks count, the elements count, the steps count,
/// the MOSMIX ID, the parse steps count, the freshness, the duration, the working set before and the peak working set (contract MF-04).
/// </summary>
internal sealed record ResourceCheckResult(
    long FileBytes,
    int Placemarks,
    int Elements,
    int Steps,
    string MosmixId,
    int ParseSteps,
    MosmixRunFreshness Freshness,
    TimeSpan Duration,
    long WorkingSetBeforeBytes,
    long PeakWorkingSetBytes,
    long StartPeakWorkingSetBytes)
{
    /// <summary>
    /// Gets a value indicating whether the parse duration is within the deadline threshold.
    /// </summary>
    public bool WithinDeadline => Duration <= ResourceCheckRunner.DeadlineThreshold;

    /// <summary>
    /// Gets a value indicating whether the peak working set is within the threshold.
    /// </summary>
    public bool WithinPeak => PeakWorkingSetBytes <= ResourceCheckRunner.PeakWorkingSetThresholdBytes;
}
