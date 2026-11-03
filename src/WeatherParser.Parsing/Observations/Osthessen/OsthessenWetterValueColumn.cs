namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// An exported OsthessenWetter value column with its documented unit (contract OW-06, A7).
/// </summary>
public enum OsthessenWetterValueColumn
{
    /// <summary>
    /// The overview column Temp.2m, primary temperature in degrees Celsius.
    /// </summary>
    Temperature,

    /// <summary>
    /// The overview column Luftfeuchte, in percent.
    /// </summary>
    RelativeHumidity,

    /// <summary>
    /// The wind table column Wind 10 Min., in kilometres per hour.
    /// </summary>
    MeanWindSpeed10Minutes
}
