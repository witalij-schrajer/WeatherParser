using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Forecasts;

/// <summary>
/// Represents a series of forecast values.
/// </summary>
public sealed class ForecastSeries
{
    /// <summary>
    /// Gets or sets the kind of quantity for this series.
    /// </summary>
    public QuantityKind? Kind { get; init; }

    /// <summary>
    /// Gets or sets the values in this series.
    /// </summary>
    public IReadOnlyList<double?>? Values { get; init; }

    /// <summary>
    /// Gets or sets the averaging periods for this series.
    /// </summary>
    public IReadOnlyList<AveragingPeriod?>? Periods { get; init; }

    /// <summary>
    /// Gets or sets the pressure unit for this series.
    /// </summary>
    public PressureUnit? PressureUnit { get; init; }

    /// <summary>
    /// Gets or sets the pressure reference for this series.
    /// </summary>
    public PressureReference? PressureReference { get; init; }

    /// <summary>
    /// Gets or sets the wind speed unit for this series.
    /// </summary>
    public WindSpeedUnit? WindSpeedUnit { get; init; }

    /// <summary>
    /// Gets or sets the product for this series.
    /// </summary>
    public MosmixProduct? Product { get; init; }

    /// <summary>
    /// Gets or sets the MOSMIX ID for this series.
    /// </summary>
    public string? MosmixId { get; init; }

    /// <summary>
    /// Gets or sets the UTC time when the forecast was issued for this series.
    /// </summary>
    public DateTime? IssuedAtUtc { get; init; }
}
