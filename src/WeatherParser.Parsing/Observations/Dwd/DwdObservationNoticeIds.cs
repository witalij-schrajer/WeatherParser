using System;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Log event names of the DWD observation parser (contract OC-10).
/// </summary>
public static class DwdObservationNoticeIds
{
    /// <summary>
    /// Warning: a product's archive is missing, OBSERVATION-PRODUCT-MISSING.
    /// </summary>
    public const string ProductMissing = "OBSERVATION-PRODUCT-MISSING";

    /// <summary>
    /// Warning: a reading was omitted from the dataset, OBSERVATION-READING-OMITTED.
    /// </summary>
    public const string ReadingOmitted = "OBSERVATION-READING-OMITTED";
}
