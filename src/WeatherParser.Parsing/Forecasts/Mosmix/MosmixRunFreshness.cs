using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// The run age is at most the consumer's MaxRunAge (contract MF-09).
/// </summary>
public enum MosmixRunFreshness
{
    /// <summary>
    /// The run age is above the consumer's MaxRunAge; the run is still returned and marked (contract MF-09).
    /// </summary>
    Stale,

    /// <summary>
    /// The run age is at most the consumer's MaxRunAge (contract MF-09).
    /// </summary>
    Fresh
}
