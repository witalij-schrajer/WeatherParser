namespace WeatherParser.Cli.Output;

using System;
using System.Threading;

/// <summary>
/// Reports a publication run ended by cancellation together with the counts reached so far (CL-11, CL-15).
/// </summary>
public sealed class PublicationCancelledException : OperationCanceledException
{
    /// <summary>
    /// Gets the counts reached when the publication was cancelled.
    /// </summary>
    public PublicationReport Report { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicationCancelledException"/> class.
    /// </summary>
    /// <param name="report">The counts reached when the publication was cancelled.</param>
    /// <param name="innerException">The cancellation that ended the publication.</param>
    /// <param name="cancellationToken">The cancelled token.</param>
    public PublicationCancelledException(PublicationReport report, OperationCanceledException innerException, CancellationToken cancellationToken) 
        : base("The publication was cancelled.", innerException, cancellationToken)
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
    }
}
