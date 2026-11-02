using System;

namespace WeatherParser.Package.Tests.ApiFixture;

/// <summary>
/// Provides functionality for testing API shape.
/// </summary>
public interface IApiShape
{
    /// <summary>
    /// Gets the area of the shape.
    /// </summary>
    /// <returns>The area.</returns>
    int Area();
}

/// <summary>
/// Represents a base class for API samples.
/// </summary>
public abstract class ApiBase
{
    /// <summary>
    /// Gets the name of the base.
    /// </summary>
    public virtual string Name => "base";

    /// <summary>
    /// Draws the base.
    /// </summary>
    protected abstract void Draw();
}

/// <summary>
/// Represents a sample API class.
/// </summary>
public sealed class ApiSample : ApiBase, IApiShape
{
    /// <summary>
    /// Gets the limit value.
    /// </summary>
    public const int Limit = 5;

    /// <summary>
    /// Gets the tag value.
    /// </summary>
    public static readonly string Tag = "t";

    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets the label.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Describes the sample.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <param name="times">The times.</param>
    /// <returns>The description.</returns>
    public string Describe(string prefix, int times = 1) => prefix + times;

    /// <summary>
    /// Picks a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to pick.</param>
    /// <returns>The picked value.</returns>
    public T Pick<T>(T value) where T : class => value;

    /// <summary>
    /// Doubles a value.
    /// </summary>
    /// <param name="x">The value to double.</param>
    /// <returns>The doubled value.</returns>
    public static int Twice(int x) => 2 * x;

    /// <summary>
    /// Gets the area.
    /// </summary>
    /// <returns>The area.</returns>
    public int Area() => Count;

    /// <summary>
    /// Draws the sample.
    /// </summary>
    protected override void Draw() { }

    /// <summary>
    /// Hides the sample.
    /// </summary>
    internal void Hidden() { }
}

/// <summary>
/// Represents an API color.
/// </summary>
public enum ApiColor { Red, Green }
