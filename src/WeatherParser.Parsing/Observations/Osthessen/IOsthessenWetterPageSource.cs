using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Retrieves the consumer-supplied OsthessenWetter page once per call through the catalog downloader (contract OW-10).
/// </summary>
public interface IOsthessenWetterPageSource
{
    /// <summary>
    /// Sends one GET request for the page (no retries, at most 2 MiB) and passes the bounded body to the read function.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="read">The function that consumes the body asynchronously.</param>
    /// <param name="deadline">The token of the parse deadline.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The result of the read function.</returns>
    /// <exception cref="ArgumentNullException">When read is null.</exception>
    /// <exception cref="OsthessenWetterException">OSTHESSEN-PAGE-UNAVAILABLE for a non-success status, a transport failure, an oversized body or the downloader's request timeout; OSTHESSEN-DEADLINE-EXCEEDED when the deadline expires.</exception>
    /// <exception cref="OperationCanceledException">When the caller's token is cancelled.</exception>
    Task<T> ReadAsync<T>(Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken) where T : class;
}
