using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Stable notice IDs of the MOSMIX parsers (contract MF-14).
/// </summary>
public static class MosmixNoticeIds
{
    /// <summary>
    /// The run is older than the age limit.
    /// </summary>
    public const string RunStale = "MOSMIX-RUN-STALE";

    /// <summary>
    /// A mapped element is absent from the file.
    /// </summary>
    public const string ElementAbsent = "MOSMIX-ELEMENT-ABSENT";

    /// <summary>
    /// A present element is not mapped and not exported.
    /// </summary>
    public const string ElementNotExported = "MOSMIX-ELEMENT-NOT-EXPORTED";

    /// <summary>
    /// The builder omitted one value.
    /// </summary>
    public const string ValueOmitted = "FORECAST-VALUE-OMITTED";
}
