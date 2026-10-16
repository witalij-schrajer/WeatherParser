using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// A single forecast weather quantity for one forecast time step, per REQ-105/REQ-106.
/// Unlike <see cref="MeasuredValue"/> it has no own measurement timestamp; instead it is
/// associated with the containing <see cref="ForecastStep"/>'s <c>valid_at</c>. Interval
/// quantities additionally carry the start and end of their reference period.
/// </summary>
[Serializable]
public sealed class ForecastValue
{
    /// <summary>The forecast value in the unit documented by the containing field (REQ-052/REQ-106).</summary>
    [JsonPropertyName("value")]
    public double Value { get; set; }

    /// <summary>
    /// UTC start of the reference period for interval quantities (e.g. accumulated rain amount).
    /// Null for instantaneous quantities.
    /// </summary>
    [JsonPropertyName("period_start")]
    public DateTime? PeriodStart { get; set; }

    /// <summary>
    /// UTC end of the reference period for interval quantities. Null for instantaneous quantities.
    /// </summary>
    [JsonPropertyName("period_end")]
    public DateTime? PeriodEnd { get; set; }
}
