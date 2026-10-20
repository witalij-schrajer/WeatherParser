namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a required member that tracks whether a value has been set.
/// Reading an unassigned value throws, so no default is exported.
/// </summary>
/// <typeparam name="T">The type of the member.</typeparam>
internal readonly struct RequiredMember<T>
{
    private readonly T _value;
    private readonly bool _isSet;

    private RequiredMember(T value)
    {
        _value = value;
        _isSet = value is not null;
    }

    /// <summary>
    /// Creates a new instance of <see cref="RequiredMember{T}"/> with the specified value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A new instance of <see cref="RequiredMember{T}"/>.</returns>
    public static RequiredMember<T> Of(T value) => new(value);

    /// <summary>
    /// Gets the value if it has been set, otherwise throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <param name="fieldName">The name of the field for the exception message.</param>
    /// <returns>The value if it has been set.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the member has not been set.</exception>
    public T Get(string fieldName) => _isSet
        ? _value
        : throw new InvalidOperationException($"The required member '{fieldName}' is not set.");
}
