namespace WeatherParser.Parsing.Observations;

using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

/// <summary>
/// Represents a single observation reading.
/// </summary>
public sealed class ObservationReading
{
    /// <summary>
    /// Gets or sets the kind of the observation reading.
    /// </summary>
    public QuantityKind? Kind { get; init; }

    /// <summary>
    /// Gets or sets the value of the observation reading.
    /// </summary>
    public double? Value { get; init; }

    /// <summary>
    /// Gets or sets the time of the observation reading.
    /// </summary>
    public NormalizedSourceTime? Time { get; init; }

    /// <summary>
    /// Gets or sets the averaging period of the observation reading.
    /// </summary>
    public AveragingPeriod? Period { get; init; }

    /// <summary>
    /// Gets or sets the quality of the observation reading.
    /// </summary>
    public string? Quality { get; init; }

    /// <summary>
    /// Gets or sets the pressure unit of the observation reading.
    /// </summary>
    public PressureUnit? PressureUnit { get; init; }

    /// <summary>
    /// Gets or sets the pressure reference of the observation reading.
    /// </summary>
    public PressureReference? PressureReference { get; init; }

    /// <summary>
    /// Gets or sets the wind speed unit of the observation reading.
    /// </summary>
    public WindSpeedUnit? WindSpeedUnit { get; init; }
}
