namespace WeatherParser.Coverage;

/// <summary>
/// Result of a single rule evaluation.
/// </summary>
/// <param name="RuleId">Identifier of the rule that was evaluated.</param>
/// <param name="Verdict">True if the rule passed, false otherwise.</param>
/// <param name="Diagnostic">Human-readable explanation of the rule's outcome.</param>
public sealed record RuleResult(string RuleId, bool Verdict, string Diagnostic);
