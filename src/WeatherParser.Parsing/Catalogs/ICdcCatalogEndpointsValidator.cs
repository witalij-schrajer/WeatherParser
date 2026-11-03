using System;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Validates CDC catalog endpoint options.
/// </summary>
public interface ICdcCatalogEndpointsValidator
{
    /// <summary>
    /// Validates only the eight station-list and now-directory URIs, without the selection limits, which do not apply to DWD observations (contract OC-09, SC-12).
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ParserEndpointValidationException">
    /// Thrown when a station list or directory URI is invalid.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when options is null.
    /// </exception>
    void ValidateEndpoints(CdcCatalogEndpoints options);

    /// <summary>
    /// Validates the specified CDC catalog endpoints options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ParserEndpointValidationException">
    /// Thrown when a station list or directory URI is invalid.
    /// </exception>
    /// <exception cref="SelectionLimitException">
    /// Thrown when the selection limit is invalid (SC-18).
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when options is null.
    /// </exception>
    void Validate(CdcCatalogEndpoints options);
}
