namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Raised when a quantity conversion is not defined, lacks a required averaging period or gives a result outside the decimal range (contract C-45).
/// </summary>
public sealed class QuantityConversionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuantityConversionException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public QuantityConversionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuantityConversionException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public QuantityConversionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
