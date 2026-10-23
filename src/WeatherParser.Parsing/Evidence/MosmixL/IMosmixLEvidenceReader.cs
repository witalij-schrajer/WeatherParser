using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Evidence.MosmixL;

/// <summary>
/// Reads one downloaded <c>MOSMIX_L_LATEST_&lt;id&gt;.kmz</c> from a caller-owned stream that stays open,
/// in memory only (nothing is written to disk),
/// under the SC-20 evidence-file contract:
/// exactly one <c>.kml</c> member;
/// at most 1 MiB compressed and 16 MiB uncompressed, enforced while reading;
/// DTD processing prohibited, no resolver;
/// required paths and cardinalities by namespace URI;
/// the placemark name equals <paramref name="mosmixId"/>;
/// token grammar and count.
/// Any violation throws <see cref="CatalogFormatException"/> with <see cref="CatalogFormatException.ErrorId"/> <c>MOSMIX-EVIDENCE-INVALID</c>;
/// <see cref="ArgumentNullException"/> for a null stream or ID.
/// </summary>
public interface IMosmixLEvidenceReader
{
    /// <summary>
    /// Reads one downloaded <c>MOSMIX_L_LATEST_&lt;id&gt;.kmz</c> from a caller-owned stream that stays open,
    /// in memory only (nothing is written to disk),
    /// under the SC-20 evidence-file contract:
    /// exactly one <c>.kml</c> member;
    /// at most 1 MiB compressed and 16 MiB uncompressed, enforced while reading;
    /// DTD processing prohibited, no resolver;
    /// required paths and cardinalities by namespace URI;
    /// the placemark name equals <paramref name="mosmixId"/>;
    /// token grammar and count.
    /// Any violation throws <see cref="CatalogFormatException"/> with <see cref="CatalogFormatException.ErrorId"/> <c>MOSMIX-EVIDENCE-INVALID</c>;
    /// <see cref="ArgumentNullException"/> for a null stream or ID.
    /// </summary>
    /// <param name="archive">The stream to read the archive from.</param>
    /// <param name="mosmixId">The MOSMIX ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated content of the MOSMIX_L file.</returns>
    /// <exception cref="CatalogFormatException">
    /// Thrown with <see cref="CatalogFormatException.ErrorId"/> <c>MOSMIX-EVIDENCE-INVALID</c> when the archive is not a valid ZIP archive,
    /// does not contain exactly one correctly named member,
    /// exceeds 1 MiB compressed or 16 MiB uncompressed,
    /// or violates a rule of SC-20.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the stream or ID is null.
    /// </exception>
    Task<MosmixLEvidence> ReadAsync(Stream archive, string mosmixId, CancellationToken cancellationToken);
}
