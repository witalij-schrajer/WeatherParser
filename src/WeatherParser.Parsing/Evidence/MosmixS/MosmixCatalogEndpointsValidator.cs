using System;
using System.IO;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Evidence.MosmixS;

/// <summary>
/// Validates <see cref="MosmixCatalogEndpoints"/> (SC-12, SC-18, SC-22).
/// </summary>
public sealed class MosmixCatalogEndpointsValidator : IMosmixCatalogEndpointsValidator
{
    private readonly IParserEndpointValidator _endpointValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixCatalogEndpointsValidator"/> class.
    /// </summary>
    /// <param name="endpointValidator">The endpoint validator to use.</param>
    public MosmixCatalogEndpointsValidator(IParserEndpointValidator endpointValidator)
    {
        ArgumentNullException.ThrowIfNull(endpointValidator);
        _endpointValidator = endpointValidator;
    }

    /// <inheritdoc />
    public void Validate(MosmixCatalogEndpoints options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Catalog and directory URIs (SC-12): absolute HTTP(S) through the shared endpoint validator.
        if (options.CatalogUri is null)
        {
            throw new ParserEndpointValidationException("The MOSMIX endpoint 'CatalogUri' is required.");
        }
        if (options.MosmixLSingleStationsDirectory is null)
        {
            throw new ParserEndpointValidationException("The MOSMIX endpoint 'MosmixLSingleStationsDirectory' is required.");
        }
        _endpointValidator.ValidateAndMap(new[]
        {
            new ParserEndpointDefinition("CatalogUri", options.CatalogUri),
            new ParserEndpointDefinition("MosmixLSingleStationsDirectory", options.MosmixLSingleStationsDirectory),
        });
        var directory = options.MosmixLSingleStationsDirectory;
        if (!directory.IsAbsoluteUri || !directory.AbsolutePath.EndsWith('/') || directory.Query.Length != 0 || directory.Fragment.Length != 0)
        {
            throw new ParserEndpointValidationException("The MOSMIX endpoint 'MosmixLSingleStationsDirectory' must be an absolute HTTP(S) directory URI whose path ends with '/' and that has no query and no fragment (contract SC-12).");
        }

        // Limits (SC-18).
        if (options.MaxEvidenceCandidates < 1 || options.MaxEvidenceCandidates > 10000)
        {
            throw new SelectionLimitException(SelectionLimitException.LimitInvalidErrorId, $"The option 'MaxEvidenceCandidates' is {options.MaxEvidenceCandidates}; the permitted range is 1 to 10000 (contract SC-18).");
        }
        if (options.SelectionDeadline <= TimeSpan.Zero || options.SelectionDeadline > TimeSpan.FromHours(1))
        {
            throw new SelectionLimitException(SelectionLimitException.LimitInvalidErrorId, "The option 'SelectionDeadline' must be greater than zero and at most 1 hour (contract SC-18).");
        }

        // MOSMIX_S evidence settings (SC-22).
        var allStations = options.MosmixSAllStationsUri;
        if (allStations is null || !allStations.IsAbsoluteUri || (allStations.Scheme != Uri.UriSchemeHttp && allStations.Scheme != Uri.UriSchemeHttps) || allStations.AbsolutePath.EndsWith('/'))
        {
            throw new EvidenceCacheException(EvidenceCacheException.SettingsInvalidErrorId, "The option 'MosmixSAllStationsUri' must be an absolute HTTP(S) file URI (contract SC-22).");
        }
        if (string.IsNullOrWhiteSpace(options.EvidenceCacheDirectory) || !Path.IsPathFullyQualified(options.EvidenceCacheDirectory))
        {
            throw new EvidenceCacheException(EvidenceCacheException.SettingsInvalidErrorId, "The option 'EvidenceCacheDirectory' must be an absolute local path (contract SC-22).");
        }
        if (options.MosmixSEvidenceMaxAge is not { } maxAge || maxAge < TimeSpan.FromMinutes(1) || maxAge > TimeSpan.FromDays(365))
        {
            throw new EvidenceCacheException(EvidenceCacheException.SettingsInvalidErrorId, "The option 'MosmixSEvidenceMaxAge' is required and must lie between 1 minute and 365 days (contract SC-22).");
        }
        if (options.MosmixSRefreshDeadline <= TimeSpan.Zero || options.MosmixSRefreshDeadline > TimeSpan.FromHours(1))
        {
            throw new EvidenceCacheException(EvidenceCacheException.SettingsInvalidErrorId, "The option 'MosmixSRefreshDeadline' must be greater than zero and at most 1 hour (contract SC-22).");
        }
    }
}
