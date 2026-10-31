using System;
using System.Diagnostics.CodeAnalysis;

namespace WeatherParser.Package.Tests.ApiAspects;

/// <summary>
/// Represents a type with a required property.
/// </summary>
public sealed class ApiRequired
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public required string Key { get; init; }
}

/// <summary>
/// Represents a type with a non-required property.
/// </summary>
public sealed class ApiNotRequired
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public string Key { get; init; } = string.Empty;
}

/// <summary>
/// Represents an outer type with nested types.
/// </summary>
public class ApiOuter
{
    /// <summary>
    /// Represents a protected inner type.
    /// </summary>
    protected class ApiProtectedInner { }

    /// <summary>
    /// Represents a public inner type.
    /// </summary>
    public class ApiPublicInner { }
}

/// <summary>
/// Represents a variant interface.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TArg">The argument type.</typeparam>
public interface IApiVariant<out TResult, in TArg>
{
    /// <summary>
    /// Runs the specified argument.
    /// </summary>
    /// <param name="arg">The argument.</param>
    /// <returns>The result.</returns>
    TResult Run(TArg arg);
}

/// <summary>
/// Represents a type with obsolete members.
/// </summary>
public sealed class ApiObsolete
{
    /// <summary>
    /// An old method.
    /// </summary>
    [Obsolete("Use Current.")]
    public void Old() { }

    /// <summary>
    /// A gone method.
    /// </summary>
    [Obsolete("Removed.", true)]
    public void Gone() { }

    /// <summary>
    /// A current method.
    /// </summary>
    public void Current() { }
}

/// <summary>
/// Represents extension methods.
/// </summary>
[SuppressMessage("Style", "IDE0130:Namespace does not match folder structure", Justification = "Test type")]
public static class ApiExtensions
{
    /// <summary>
    /// Doubles the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The doubled value.</returns>
    public static int Twice(this int value) => 2 * value;
}

/// <summary>
/// Represents a flags enumeration.
/// </summary>
[Flags]
public enum ApiFlags { None = 0, A = 1 }

/// <summary>
/// Represents a type with parameter directions.
/// </summary>
public sealed class ApiDirection
{
    /// <summary>
    /// Tries to get a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A boolean.</returns>
    public bool TryGet(out int value) { value = 1; return true; }

    /// <summary>
    /// Updates a value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void Update(ref int value) { value++; }

    /// <summary>
    /// Reads a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value.</returns>
    public int Read(in int value) => value;
}
