namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Result of validating one source value: a valid decimal value or a rejection reason; rejected values are omitted (contract C-42).
/// </summary>
public sealed class ValidatedQuantity
{
    private readonly decimal? _value;
    private readonly QuantityRejectionReason? _rejectionReason;

    private ValidatedQuantity(decimal? value, QuantityRejectionReason? rejectionReason)
    {
        _value = value;
        _rejectionReason = rejectionReason;
    }

    /// <summary>
    /// Gets the validated decimal value, or null if the source was rejected.
    /// </summary>
    public decimal? Value => _value;

    /// <summary>
    /// Gets the reason why the source value was rejected, or null if valid.
    /// </summary>
    public QuantityRejectionReason? RejectionReason => _rejectionReason;

    /// <summary>
    /// Gets whether the source value was valid.
    /// </summary>
    public bool IsValid => _value.HasValue;

    /// <summary>
    /// Creates a validated quantity representing a valid value.
    /// </summary>
    /// <param name="value">The valid decimal value.</param>
    /// <returns>A validated quantity with the given value.</returns>
    public static ValidatedQuantity Valid(decimal value)
    {
        return new ValidatedQuantity(value, null);
    }

    /// <summary>
    /// Creates a validated quantity representing a rejected value.
    /// </summary>
    /// <param name="reason">The reason for rejection.</param>
    /// <returns>A validated quantity with the given rejection reason.</returns>
    public static ValidatedQuantity Rejected(QuantityRejectionReason reason)
    {
        return new ValidatedQuantity(null, reason);
    }
}
