using System;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Raised when a MOSMIX forecast parse fails with a defined error (contract MF-14).
/// </summary>
public sealed class MosmixForecastException : Exception
{
    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Gets the product type of this exception.
    /// </summary>
    public MosmixProduct Product { get; }

    /// <summary>
    /// Gets the forecast values that were omitted during the build process.
    /// </summary>
    public IReadOnlyList<OmittedForecastValue> OmittedValues { get; }

    /// <summary>
    /// Gets the elements that were not exported.
    /// </summary>
    public IReadOnlyList<string> NotExportedElements { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixForecastException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="product">The product type.</param>
    /// <param name="message">The message that describes the error.</param>
    public MosmixForecastException(string errorId, MosmixProduct product, string message) : base(message)
    {
        if (!IsKnown(errorId))
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        if (!Enum.IsDefined(product))
        {
            throw new ArgumentException("Unknown product.", nameof(product));
        }

        ErrorId = errorId;
        Product = product;
        OmittedValues = Array.Empty<OmittedForecastValue>();
        NotExportedElements = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixForecastException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="product">The product type.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="omittedValues">The omitted values; copied.</param>
    /// <param name="notExportedElements">The not exported elements; copied.</param>
    /// <exception cref="ArgumentNullException">When omittedValues or notExportedElements is null.</exception>
    public MosmixForecastException(string errorId, MosmixProduct product, string message, IReadOnlyList<OmittedForecastValue> omittedValues, IReadOnlyList<string> notExportedElements) : this(errorId, product, message)
    {
        ArgumentNullException.ThrowIfNull(omittedValues);
        ArgumentNullException.ThrowIfNull(notExportedElements);

        OmittedValues = omittedValues.ToArray();
        NotExportedElements = notExportedElements.ToArray();
    }

    private static bool IsKnown(string id) => id == MosmixForecastErrorIds.FormatInvalid ||
        id == MosmixForecastErrorIds.IssueTimeMissing ||
        id == MosmixForecastErrorIds.LocationMismatch ||
        id == MosmixForecastErrorIds.IdUnknown ||
        id == MosmixForecastErrorIds.TooLarge ||
        id == MosmixForecastErrorIds.IdInvalid ||
        id == MosmixForecastErrorIds.RetrievalFailed ||
        id == MosmixForecastErrorIds.DeadlineExceeded ||
        id == MosmixForecastErrorIds.NoUsableValue ||
        id == MosmixForecastErrorIds.OptionsInvalid;
}
