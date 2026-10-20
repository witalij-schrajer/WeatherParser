namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Validates source values for weather quantities.
/// </summary>
public interface IQuantityValidator
{
    /// <summary>
    /// Validates a source value against the specified quantity kind.
    /// </summary>
    /// <param name="kind">The kind of quantity to validate.</param>
    /// <param name="value">The source value, or null if missing.</param>
    /// <returns>A validated quantity result containing either a valid decimal value or a rejection reason.</returns>
    ValidatedQuantity Validate(QuantityKind kind, double? value);
}
