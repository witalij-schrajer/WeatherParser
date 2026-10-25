namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Reads one downloaded <c>now</c> ZIP archive from a caller-supplied stream that stays open (SC-19).
/// </summary>
public interface ICdcNowEvidenceReader
{
    /// <summary>
    /// Reads one downloaded <c>now</c> ZIP archive from a caller-supplied stream that stays open (SC-19):
    /// exactly one member <c>produkt_zehn_now_&lt;code&gt;_&lt;yyyyMMdd&gt;_&lt;yyyyMMdd&gt;_&lt;id&gt;.txt</c>,
    /// at most 1 MiB compressed and 1 MiB uncompressed (enforced while reading),
    /// ISO-8859-1, CRLF or LF,
    /// at most 2000 data rows,
    /// the header and row rules of SC-19.
    /// Everything is read in memory and nothing is written to disk.
    /// Any violation throws <see cref="CatalogFormatException"/> with <see cref="CatalogFormatException.ErrorId"/> <c>CDC-EVIDENCE-INVALID</c>.
    /// An unknown product code or a station ID that is not five digits throws <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="archive">The stream to read the archive from.</param>
    /// <param name="stationId">The five-digit station ID.</param>
    /// <param name="productCode">The product code, one of "tu", "ff", "rr" or "sd".</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated rows with their UTC measurement times, the trimmed QN token (<see cref="CdcNowEvidenceRow.QualityLevel"/>) and the raw trimmed tokens of every data column of the header; only the product's own columns are validated (SC-19, OC-14).</returns>
    /// <exception cref="CatalogFormatException">
    /// Thrown with <see cref="CatalogFormatException.ErrorId"/> <c>CDC-EVIDENCE-INVALID</c> when the archive is not a valid ZIP archive,
    /// does not contain exactly one correctly named member,
    /// exceeds 1 MiB compressed or uncompressed,
    /// has more than 2000 data rows,
    /// or violates a header or row rule of SC-19.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the station ID is not five digits or the product code is not one of "tu", "ff", "rr" or "sd".
    /// </exception>
    Task<CdcNowEvidence> ReadAsync(Stream archive, string stationId, string productCode, CancellationToken cancellationToken);
}
