using System;
using System.Collections.Generic;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Validates CDC catalog endpoint options.
/// </summary>
public sealed class CdcCatalogEndpointsValidator : ICdcCatalogEndpointsValidator
{
    private readonly IParserEndpointValidator _endpointValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcCatalogEndpointsValidator"/> class.
    /// </summary>
    /// <param name="endpointValidator">The endpoint validator to use.</param>
    public CdcCatalogEndpointsValidator(IParserEndpointValidator endpointValidator)
    {
        _endpointValidator = endpointValidator ?? throw new ArgumentNullException(nameof(endpointValidator));
    }

    /// <inheritdoc/>
    public void ValidateEndpoints(CdcCatalogEndpoints options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var endpoints = new (string Name, Uri? Uri, bool IsDirectory)[]
        {
            ("AirTemperatureStationList", options.AirTemperatureStationList, false),
            ("AirTemperatureNowDirectory", options.AirTemperatureNowDirectory, true),
            ("WindStationList", options.WindStationList, false),
            ("WindNowDirectory", options.WindNowDirectory, true),
            ("PrecipitationStationList", options.PrecipitationStationList, false),
            ("PrecipitationNowDirectory", options.PrecipitationNowDirectory, true),
            ("SolarStationList", options.SolarStationList, false),
            ("SolarNowDirectory", options.SolarNowDirectory, true),
        };
        var definitions = new List<ParserEndpointDefinition>();
        foreach (var (name, uri, _) in endpoints)
        {
            if (uri is null)
            {
                throw new ParserEndpointValidationException($"The CDC endpoint '{name}' is required.");
            }

            definitions.Add(new ParserEndpointDefinition(name, uri));
        }

        _endpointValidator.ValidateAndMap(definitions);

        foreach (var (name, uri, isDirectory) in endpoints)
        {
            if (isDirectory && (!uri!.AbsolutePath.EndsWith('/') || uri.Query.Length != 0 || uri.Fragment.Length != 0))
            {
                throw new ParserEndpointValidationException($"The CDC endpoint '{name}' must be an absolute HTTP(S) directory URI whose path ends with '/' and that has no query and no fragment (contract SC-12).");
            }
        }
    }

    /// <inheritdoc/>
    public void Validate(CdcCatalogEndpoints options)
    {
        ValidateEndpoints(options);

        if (options.MaxEvidenceCandidates < 1 || options.MaxEvidenceCandidates > 10000)
        {
            throw new SelectionLimitException(SelectionLimitException.LimitInvalidErrorId, $"The option 'MaxEvidenceCandidates' is {options.MaxEvidenceCandidates}; the permitted range is 1 to 10000 (contract SC-18).");
        }

        if (options.SelectionDeadline <= TimeSpan.Zero || options.SelectionDeadline > TimeSpan.FromHours(1))
        {
            throw new SelectionLimitException(SelectionLimitException.LimitInvalidErrorId, "The option 'SelectionDeadline' must be greater than zero and at most 1 hour (contract SC-18).");
        }
    }
}
