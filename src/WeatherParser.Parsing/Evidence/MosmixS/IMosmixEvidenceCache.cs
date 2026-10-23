using System;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Evidence.MosmixS;

/// <summary>
/// File-system store of the MOSMIX_S evidence index (SC-22).
/// </summary>
public interface IMosmixEvidenceCache
{
    /// <summary>
    /// Reads the index of the configured <see cref="MosmixCatalogEndpoints.MosmixSAllStationsUri"/> from <see cref="MosmixCatalogEndpoints.EvidenceCacheDirectory"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The index, or <c>null</c> ("missing") when the index file is missing or unreadable,
    /// larger than 8 MiB, not valid JSON, nested deeper than 8,
    /// of another <c>format_version</c>, bound to another source key,
    /// or violates a content rule (more than 20000 locations, ID or element-name grammar,
    /// more than 200 or duplicate names per location, timestamps not in the C-06 form).
    /// </returns>
    /// <exception cref="EvidenceCacheException">
    /// Never thrown for an unusable index (SC-22).
    /// </exception>
    Task<MosmixSEvidenceIndex?> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Writes the index to a temporary file in the same directory and atomically replaces
    /// the previous index of the same source key.
    /// </summary>
    /// <param name="index">The index to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    /// <exception cref="EvidenceCacheException">
    /// Thrown when the index violates a bound or content rule,
    /// the serialized index exceeds 8 MiB, or the directory is missing or not writable.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when index is null.
    /// </exception>
    Task WriteAsync(MosmixSEvidenceIndex index, CancellationToken cancellationToken);
}
