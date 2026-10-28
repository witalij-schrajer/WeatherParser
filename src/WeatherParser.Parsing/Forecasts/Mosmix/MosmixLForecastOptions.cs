using System;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Represents the options for MOSMIX_L forecast sources (contract MF-05, MF-09).
/// </summary>
public sealed class MosmixLForecastOptions
{
    /// <summary>
    /// Gets or sets the parse deadline.
    /// The value must be greater than zero and at most one hour (contract MF-05).
    /// </summary>
    public TimeSpan ParseDeadline { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the maximum age of a run.
    /// If set, the value must be greater than zero and at most 240 hours (contract MF-09).
    /// </summary>
    public TimeSpan? MaxRunAge { get; set; }
}
