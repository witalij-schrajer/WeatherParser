using System.Text;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IStationNamePatternMatcher"/>; stateless.
/// </summary>
public sealed class StationNamePatternMatcher : IStationNamePatternMatcher
{
    /// <summary>
    /// The maximum pattern length in UTF-16 code units (contract OW-09).
    /// </summary>
    public const int MaxPatternLength = 128;

    private const int StarValue = '*';
    private const int QuestionValue = '?';

    /// <inheritdoc />
    public StationNamePattern Validate(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw InvalidPattern();
        }

        if (pattern.Length > MaxPatternLength)
        {
            throw InvalidPattern();
        }

        var normalized = pattern.Normalize(NormalizationForm.FormC);
        return new StationNamePattern(normalized);
    }

    /// <inheritdoc />
    public bool IsMatch(StationNamePattern pattern, string name) => IsMatch(pattern, name, out _);

    /// <summary>
    /// Matches and reports the number of comparison steps; a test seam for the O(pattern x name) bound (P6.03 S2).
    /// </summary>
    /// <param name="pattern">The pattern to match against.</param>
    /// <param name="name">The name to match.</param>
    /// <param name="steps">The number of comparison steps taken.</param>
    internal bool IsMatch(StationNamePattern pattern, string name, out long steps)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(name);

        // Fold both sides into arrays of Unicode scalars: culture-independent invariant case folding over Unicode scalars in normalization form C, contract OW-09
        var pat = Fold(pattern.Value);
        var text = Fold(name);

        // Backtracking glob matching with O(pattern x name) steps, contract OW-09
        steps = 0;
        int p = 0, n = 0, star = -1, mark = 0;
        while (n < text.Length)
        {
            steps++;
            if (p < pat.Length && pat[p].Value == StarValue) { star = p; mark = n; p++; }
            else if (p < pat.Length && (pat[p].Value == QuestionValue || pat[p] == text[n])) { p++; n++; }
            else if (star >= 0) { p = star + 1; mark++; n = mark; }
            else { return false; }
        }
        while (p < pat.Length && pat[p].Value == StarValue) { steps++; p++; }
        return p == pat.Length;
    }

    private static Rune[] Fold(string text)
    {
        return text.Normalize(NormalizationForm.FormC).EnumerateRunes().Select(r => Rune.ToUpperInvariant(r)).ToArray();
    }

    private static OsthessenWetterException InvalidPattern() => new(OsthessenWetterException.PatternInvalidErrorId, "The station name pattern must contain 1 to 128 characters and not only whitespace (contract OW-09).");
}
