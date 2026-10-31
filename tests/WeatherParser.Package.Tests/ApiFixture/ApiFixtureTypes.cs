using System;

namespace WeatherParser.Package.Tests.ApiFixture;

/// <summary>
/// A sample class for API inventory testing.
/// </summary>
public sealed class ApiSample
{
    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Describes something with a prefix and repetition count.
    /// </summary>
    /// <param name="prefix">The prefix to use.</param>
    /// <param name="times">How many times to repeat.</param>
    /// <returns>The concatenated string.</returns>
    public string Describe(string prefix, int times) => prefix + times;

    /// <summary>
    /// A hidden method.
    /// </summary>
    internal void Hidden() { }
}

/// <summary>
/// An enumeration for API inventory testing.
/// </summary>
public enum ApiColor { Red, Green }
