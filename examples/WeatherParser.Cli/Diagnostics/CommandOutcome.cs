namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Represents the outcome of a command execution.
/// </summary>
/// <param name="Succeeded">Number of succeeded items.</param>
/// <param name="Failed">Number of failed items.</param>
/// <param name="Cancelled">Whether the operation was cancelled.</param>
public sealed record CommandOutcome(int Succeeded, int Failed, bool Cancelled);
