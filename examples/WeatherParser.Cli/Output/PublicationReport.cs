namespace WeatherParser.Cli.Output;

/// <summary>
/// The result of publishing the queued MQTT messages of one invocation (CL-15).
/// </summary>
/// <param name="Published">The messages acknowledged by the broker.</param>
/// <param name="Failed">The messages whose publication failed after their attempts or was refused.</param>
/// <param name="NotAttempted">The messages never sent, for example after the connection was lost for good.</param>
public sealed record PublicationReport(int Published, int Failed, int NotAttempted);
