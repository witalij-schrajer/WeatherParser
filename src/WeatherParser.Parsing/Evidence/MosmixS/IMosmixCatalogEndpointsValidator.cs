using System;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Evidence.MosmixS;

/// <summary>
/// Validates MOSMIX catalog endpoint options.
/// </summary>
public interface IMosmixCatalogEndpointsValidator
{
    /// <summary>
    /// Validates the specified MOSMIX catalog endpoints options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ParserEndpointValidationException">
    /// Thrown when a catalog or directory URI is invalid.
    /// </exception>
    /// <exception cref="SelectionLimitException">
    /// Thrown when the selection limit is invalid (SC-18).
    /// </exception>
    /// <exception cref="EvidenceCacheException">
    /// Thrown when the evidence cache settings are invalid (SC-22).
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when options is null.
    /// </exception>
    void Validate(MosmixCatalogEndpoints options);
}
